"""Design long-hole stopes along one ore drive from exported block model cells (v2 method).

Inputs (from export-blocks-around-drive-read-only.cs):
  blocks.csv        x,y,z,dx,dy,dz,au,dens,domain,mined
  floor_string.csv  x,y,z of the drive's floor string
Output: geom.cs with stV/stF arrays for create-stopes-from-geometry.cs
Method (Kennet, 2026-10):
  1. Sample the model on a 0.5 x 0.25 x 0.5 m grid in drive coordinates (chainage s, offset u, z).
  2. Each wall has NL points up its height. For every 1 m station and level, pick the offsets that
     maximise sum(tonnes * (grade - CUT)) with width >= MIN_W, so whole >= CUT blocks are taken in.
     The base level stays inside the drive.
  3. Smooth along strike and up the wall, then pick the chainages that maximise metal with every stope
     > MIN_GRADE, <= MAX_L long, PILLAR m apart, inside the main >= CUT run.
  4. Use a section every SEC m (like the existing stopes), then nudge each wall point 0.25 m out if
     the added strip is >= CUT, or in if the removed strip is < CUT, keeping neighbours within 1 m.
Run: python3 -I design_stopes.py <folder>
"""
import sys, collections, numpy as np
D = sys.argv[1].rstrip('/') + '/'
CUT, MIN_GRADE, MIN_W, MAX_L, PILLAR = 2.5, 2.6, 2.2, 40, 5
DRIVE_H, DRIVE_W, HEIGHT, BASE_BELOW_BACK = 4.5, 4.5, 20.0, 1.0
NL, SEC, SMOOTH_S, MAX_STEP = 5, 5.0, 5, 1.0
ORIGIN, PARENT = (7770, 62610, 5930), (4, 10, 10)
b = np.genfromtxt(D+'blocks.csv', delimiter=',', skip_header=1, usecols=range(8))
fs = np.genfromtxt(D+'floor_string.csv', delimiter=',', skip_header=1)
OX, OY, OZ = ORIGIN; PX, PY, PZ = PARENT
cells = collections.defaultdict(list)
for n, r in enumerate(b): cells[(int((r[0]-OX)//PX), int((r[1]-OY)//PY), int((r[2]-OZ)//PZ))].append(n)
def look(x, y, z):
    for n in cells.get((int((x-OX)//PX), int((y-OY)//PY), int((z-OZ)//PZ)), ()):
        r = b[n]
        if abs(x-r[0]) <= r[3]/2 and abs(y-r[1]) <= r[4]/2 and abs(z-r[2]) <= r[5]/2: return n
    return -1
P = fs[:, :2]; seg = np.diff(P, axis=0); L = np.hypot(seg[:, 0], seg[:, 1]); cum = np.r_[0, np.cumsum(L)]
def frame(s):
    s = min(max(s, 1e-3), cum[-1]-1e-3); i = min(np.searchsorted(cum, s, side='right')-1, len(L)-1)
    d = seg[i]/L[i]; return P[i]+(s-cum[i])/L[i]*seg[i], np.array([-d[1], d[0]])
floor = float(np.median(fs[:, 2])); ZB = floor+DRIVE_H-BASE_BELOW_BACK; ZT = floor+DRIVE_H+HEIGHT; LZ = np.linspace(ZB, ZT, NL)
# 1. grid
Sg = np.arange(0.25, cum[-1], 0.5); Ug = np.arange(-8+0.125, 8, 0.25); Zg = np.arange(ZB+0.25, ZT, 0.5)
T = np.zeros((len(Sg), len(Ug), len(Zg))); G = np.zeros_like(T)
for a, s in enumerate(Sg):
    p, nrm = frame(s)
    for c, u in enumerate(Ug):
        x, y = p+u*nrm
        for e, z in enumerate(Zg):
            k = look(x, y, z)
            if k >= 0: T[a, c, e] = 0.0625*b[k, 7]; G[a, c, e] = b[k, 6]
V = T*(G-CUT); nmin = int(np.ceil(MIN_W/0.25)); inside = np.abs(Ug) <= DRIVE_W/2
# 2. walls per 1 m station
S1 = np.arange(0.5, Sg[-1], 1.0); FW = np.zeros((len(S1), NL)); HW = np.zeros_like(FW)
for a, s in enumerate(S1):
    for l, z in enumerate(LZ):
        prof = V[np.abs(Sg-s) <= 2.5][:, :, np.abs(Zg-z) <= (ZT-ZB)/(NL-1)/2+0.1].sum(axis=(0, 2))
        if l == 0: prof = np.where(inside, prof, -1e9)
        cs = np.r_[0, np.cumsum(prof)]
        _, i, j = max((cs[j]-cs[i], i, j) for i in range(len(Ug)) for j in range(i+nmin, len(Ug)+1))
        FW[a, l] = Ug[i]-0.125; HW[a, l] = Ug[j-1]+0.125
def smooth(A, k):
    B = np.array([np.convolve(np.pad(A[:, l], k//2, mode='edge'), np.ones(k)/k, 'valid') for l in range(A.shape[1])]).T
    C = B.copy(); C[:, 1:-1] = (B[:, :-2]+2*B[:, 1:-1]+B[:, 2:])/4; return C
def fix(FW, HW):
    c = (HW+FW)/2; m = HW-FW < MIN_W; FW[m] = c[m]-MIN_W/2; HW[m] = c[m]+MIN_W/2
    sh = np.maximum(HW[:, 0]-DRIVE_W/2, 0)-np.maximum(-DRIVE_W/2-FW[:, 0], 0); FW[:, 0] -= sh; HW[:, 0] -= sh
FW = smooth(FW, SMOOTH_S); HW = smooth(HW, SMOOTH_S); fix(FW, HW)
def evaluate(ss, fw, hw):
    sm = (Sg > ss[0]) & (Sg < ss[-1]); S = Sg[sm]
    f = np.array([np.interp(S, ss, fw[:, q]) for q in range(NL)]); h = np.array([np.interp(S, ss, hw[:, q]) for q in range(NL)])
    f = np.array([[np.interp(z, LZ, f[:, a]) for z in Zg] for a in range(len(S))]); h = np.array([[np.interp(z, LZ, h[:, a]) for z in Zg] for a in range(len(S))])
    m = (Ug[None, :, None] >= f[:, None, :]) & (Ug[None, :, None] <= h[:, None, :]); t = T[sm]*m
    return t.sum(), (t*G[sm]).sum()
# 3. chainages (two stopes)
TM = np.array([evaluate(np.array([s-0.5, s+0.5]), np.array([FW[a], FW[a]]), np.array([HW[a], HW[a]])) for a, s in enumerate(S1)])
cT = np.r_[0, np.cumsum(TM[:, 0])]; cM = np.r_[0, np.cumsum(TM[:, 1])]; g = lambda i, j: (cM[j]-cM[i])/max(cT[j]-cT[i], 1e-9)
ore = np.where((G >= CUT).any(axis=(1, 2)))[0]; ore = np.unique((Sg[ore]//1).astype(int))
runs = [[ore[0]]]
for q in ore[1:]: (runs[-1].append(q) if q-runs[-1][-1] <= 5 else runs.append([q]))
run = max(runs, key=lambda r: cM[min(r[-1]+1, len(S1))]-cM[r[0]]); S0, E = run[0], min(run[-1]+1, len(S1))
best = None
for i in range(S0, E):
    for j in range(i+10, min(E, i+MAX_L)+1):
        if g(i, j) <= MIN_GRADE: continue
        k = j+PILLAR
        for l in range(k+10, min(E, k+MAX_L)+1):
            v = cM[j]-cM[i]+cM[l]-cM[k]
            if g(k, l) > MIN_GRADE and (best is None or v > best[0]): best = (v, i, j, k, l)
_, i, j, k, l = best
# 4. sections every SEC m, nudge wall points
cs_out = ''
for a, c in ((i, j), (k, l)):
    ss = np.array(list(np.arange(a, c, SEC))+[c], float)
    fw = np.array([[np.interp(s, S1, FW[:, q]) for q in range(NL)] for s in ss]); hw = np.array([[np.interp(s, S1, HW[:, q]) for q in range(NL)] for s in ss])
    t0, m0 = evaluate(ss, fw, hw)
    for _ in range(12):
        changed = 0
        for si in range(len(ss)):
            for q in range(NL):
                for A, sign in ((fw, -1), (hw, 1)):
                    for step in (0.25, -0.25):
                        old = A[si, q]; A[si, q] = old+sign*step
                        ok = hw[si, q]-fw[si, q] >= MIN_W-1e-9 and (q > 0 or (fw[si, 0] >= -DRIVE_W/2-1e-9 and hw[si, 0] <= DRIVE_W/2+1e-9))
                        ok = ok and all(abs(A[si, q]-v) <= MAX_STEP for v in [A[x, q] for x in (si-1, si+1) if 0 <= x < len(ss)]+[A[si, y] for y in (q-1, q+1) if 0 <= y < NL])
                        if ok:
                            t1, m1 = evaluate(ss, fw, hw); dt, dm = t1-t0, m1-m0
                            if (step > 0 and dt > 0 and dm/dt >= CUT) or (step < 0 and dt < 0 and dm/dt < CUT): t0, m0 = t1, m1; changed += 1; continue
                        A[si, q] = old
        if not changed: break
    Vx, F = [], []; R = 2*NL
    for si, s in enumerate(ss):
        p, nrm = frame(s)
        for u, z in [(fw[si, q], LZ[q]) for q in range(NL)]+[(hw[si, q], LZ[q]) for q in range(NL-1, -1, -1)]: Vx.append((*(p+u*nrm), z))
    n = len(ss)
    for q in range(n-1):
        for e in range(R): a0 = R*q+e; a1 = R*q+(e+1) % R; F += [(a0, a1, a1+R), (a0, a1+R, a0+R)]
    for base, flip in ((0, True), (R*(n-1), False)):
        for q in range(NL-1):
            tri = [(base+q, base+q+1, base+R-2-q), (base+q, base+R-2-q, base+R-1-q)]
            F += [(x, z, y) for x, y, z in tri] if flip else tri
    Vx = np.array(Vx)
    if sum(np.dot(Vx[x], np.cross(Vx[y], Vx[z])) for x, y, z in F) < 0: F = [(x, z, y) for x, y, z in F]
    print('chainage %d-%d (%d m): %.0f t @ %.2f g/t, width %.2f-%.2f m' % (a, c, c-a, t0, m0/t0, (hw-fw).min(), (hw-fw).max()))
    cs_out += 'stV.Add(new double[]{'+','.join('%.3f' % x for v in Vx for x in v)+'});\n'+'stF.Add(new int[]{'+','.join(str(x+1) for f in F for x in f)+'});\n'
open(D+'geom.cs', 'w').write(cs_out)
