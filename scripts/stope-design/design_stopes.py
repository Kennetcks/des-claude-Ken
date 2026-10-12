"""Design long-hole stopes along one ore drive from exported block model cells.

Inputs (exported by export-blocks-around-drive-read-only.cs):
  blocks.csv        x,y,z,dx,dy,dz,au,dens,domain,mined  (sub-blocks around the drive)
  floor_string.csv  x,y,z of the drive's floor string
Output:
  geom.cs           stV/stF arrays to paste into create-stopes-from-geometry.cs
Rules used in the 2026-10 session (change at the top):
  ore = blocks >= CUT g/t; stope walls follow the ore, never narrower than MIN_W;
  base BASE_BELOW_BACK m below the drive back (inside the drive, cut later);
  top = back + HEIGHT; each stope <= MAX_L long, PILLAR m between stopes;
  each stope's diluted grade (all blocks inside) > MIN_GRADE.
Run: python3 -I design_stopes.py <folder with the csv files>
"""
import sys, collections, numpy as np
D = sys.argv[1].rstrip('/') + '/'
# --- rules and site values ---------------------------------------------------
CUT, MIN_GRADE, MIN_W, MAX_L, PILLAR, N_STOPES = 2.5, 2.6, 2.2, 40, 5, 2
FLOOR, DRIVE_H, DRIVE_W = None, 4.5, 4.5    # FLOOR None = median floor-string z
HEIGHT, BASE_BELOW_BACK = 20.0, 1.0
ORIGIN, PARENT = (7770, 62610, 5930), (4, 10, 10)   # block model origin and parent block size
# -----------------------------------------------------------------------------
b = np.genfromtxt(D + 'blocks.csv', delimiter=',', skip_header=1, usecols=range(8))
fs = np.genfromtxt(D + 'floor_string.csv', delimiter=',', skip_header=1)
OX, OY, OZ = ORIGIN; PX, PY, PZ = PARENT
cells = collections.defaultdict(list)
for n, r in enumerate(b): cells[(int((r[0]-OX)//PX), int((r[1]-OY)//PY), int((r[2]-OZ)//PZ))].append(n)
def look(x, y, z):
    for n in cells.get((int((x-OX)//PX), int((y-OY)//PY), int((z-OZ)//PZ)), ()):
        r = b[n]
        if abs(x-r[0]) <= r[3]/2 and abs(y-r[1]) <= r[4]/2 and abs(z-r[2]) <= r[5]/2: return n
    return -1
P = fs[:, :2]; seg = np.diff(P, axis=0); L = np.hypot(seg[:, 0], seg[:, 1]); cum = np.r_[0, np.cumsum(L)]
def frame(s):                      # point, along-strike unit vector, left normal at chainage s
    s = min(max(s, 1e-3), cum[-1]-1e-3); i = min(np.searchsorted(cum, s, side='right')-1, len(L)-1)
    d = seg[i]/L[i]; return P[i] + (s-cum[i])/L[i]*seg[i], d, np.array([-d[1], d[0]])
floor = float(np.median(fs[:, 2])) if FLOOR is None else FLOOR
ZB = floor + DRIVE_H - BASE_BELOW_BACK; ZT = floor + DRIVE_H + HEIGHT; ZM = (ZB+ZT)/2
S = np.arange(0, int(cum[-1])) + 0.5          # 1 m slices
def extent(s, z0, z1):
    p, d, nrm = frame(s); us = [u for u in np.arange(-5, 5.01, 0.25)
        if np.mean([b[look(*(p+u*nrm), z), 6] for z in np.arange(z0+0.5, z1, 1.0)]) >= CUT]
    return (min(us), max(us)) if us else None
def series(z0, z1):
    e = [extent(s, z0, z1) for s in S]
    c = np.array([np.nan if x is None else (x[0]+x[1])/2 for x in e]); w = np.array([0 if x is None else x[1]-x[0]+0.25 for x in e])
    ok = ~np.isnan(c); c = np.interp(np.arange(len(c)), np.where(ok)[0], c[ok]); k = np.ones(5)/5
    sm = lambda a: np.convolve(np.pad(a, 2, mode='edge'), k, 'valid')
    return sm(c), np.maximum(sm(w), MIN_W), ok
cB, wB, okB = series(ZB, ZM); cT, wT, okT = series(ZM, ZT)
cB = np.clip(cB, -DRIVE_W/2 + wB/2, DRIVE_W/2 - wB/2)        # base stays inside the drive
shape = lambda s: [np.interp(s, S, a) for a in (cB, wB, cT, wT)]
def sample(s0, s1, keep=lambda z, g: True):
    t = m = 0
    for s in np.arange(s0+0.25, s1, 0.5):
        p, d, nrm = frame(s); c0, w0, c1, w1 = shape(s)
        for z in np.arange(ZB+0.25, ZT, 0.5):
            f = (z-ZB)/(ZT-ZB); c = c0+(c1-c0)*f; w = w0+(w1-w0)*f
            for u in np.linspace(c-w/2, c+w/2, 9)[:-1] + w/16:
                k = look(*(p+u*nrm), z)
                if k >= 0 and keep(z, b[k, 6]): tt = 0.5*0.5*w/8*b[k, 7]; t += tt; m += tt*b[k, 6]
    return t, m
TM = np.array([sample(s-0.5, s+0.5) for s in S]); cT_ = np.r_[0, np.cumsum(TM[:, 0])]; cM_ = np.r_[0, np.cumsum(TM[:, 1])]
g = lambda i, j: (cM_[j]-cM_[i])/(cT_[j]-cT_[i])
ore = np.where(okB | okT)[0]                                  # stopes stay inside the main >= CUT run (gaps <= 5 m)
runs = [[ore[0]]]
for q in ore[1:]:
    (runs[-1].append(q) if q - runs[-1][-1] <= 5 else runs.append([q]))
run = max(runs, key=lambda r: cM_[r[-1]+1]-cM_[r[0]]); S0, E = run[0], run[-1]+1
best = None                                                    # two stopes, max metal (extend the loop for N_STOPES>2)
for i in range(S0, E):
    for j in range(i+10, min(E, i+MAX_L)+1):
        if g(i, j) <= MIN_GRADE: continue
        k = j + PILLAR
        for l in range(k+10, min(E, k+MAX_L)+1):
            if g(k, l) > MIN_GRADE and (best is None or cM_[j]-cM_[i]+cM_[l]-cM_[k] > best[0]): best = (cM_[j]-cM_[i]+cM_[l]-cM_[k], i, j, k, l)
_, i, j, k, l = best
def solid(a, c):
    V, F = [], []; secs = np.arange(a, c+1e-3, 1.0)
    for s in secs:
        p, d, nrm = frame(s); c0, w0, c1, w1 = shape(s)
        for u, z in ((c0-w0/2, ZB), (c0+w0/2, ZB), (c1+w1/2, ZT), (c1-w1/2, ZT)): V.append((*(p+u*nrm), z))
    n = len(secs)
    for q in range(n-1):
        for e in range(4):
            a0 = 4*q+e; a1 = 4*q+(e+1) % 4; F += [(a0, a1, a1+4), (a0, a1+4, a0+4)]
    e = 4*(n-1); F += [(0, 2, 1), (0, 3, 2), (e, e+1, e+2), (e, e+2, e+3)]
    V = np.array(V); vol = sum(np.dot(V[x], np.cross(V[y], V[z])) for x, y, z in F)/6
    if vol < 0: F = [(x, z, y) for x, y, z in F]
    return V, F, abs(vol)
cs = ''
for a, c in ((i, j), (k, l)):
    V, F, vol = solid(a, c); t, m = sample(a, c); ta, ma = sample(a, c, lambda z, gr: z > floor+DRIVE_H); tg, mg = sample(a, c, lambda z, gr: gr >= CUT)
    print('chainage %d-%d L %d m: %.0f m3, %.0f t @ %.2f g/t | above back %.2f g/t | >=%.1f part %.0f t @ %.2f' % (a, c, c-a, vol, t, m/t, ma/ta, CUT, tg, mg/tg))
    cs += 'stV.Add(new double[]{' + ','.join('%.3f' % x for v in V for x in v) + '});\n'
    cs += 'stF.Add(new int[]{' + ','.join(str(x+1) for f in F for x in f) + '});\n'
open(D + 'geom.cs', 'w').write(cs)
