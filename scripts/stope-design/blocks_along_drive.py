"""5 m stope blocks along a whole ore drive, walls following the >= cut-off lode (same wall method as design_stopes.py).
The engineer then picks stopes and pillars from the block grades.

Inputs (from export-blocks-around-drive-read-only.cs):
  blocks.csv        x,y,z,dx,dy,dz,au,dens,domain,mined
  floor_string.csv  x,y,z of the drive's floor string
Method: the wall shaping of design_stopes.py (v3), applied to the whole drive, no grade limit, then made
  drillable (convex sections, walls >= MIN_DIP, <= MAX_STEP change between sections; see stope-design.md):
  one shape from chainage 0 to the end, a section every 5 m, cut into one closed solid per 5 m
  (neighbours share their end section; a remainder under 2.5 m joins the last block).
Outputs: blocks.txt  ch_from,ch_to,tonnes,grade,width per block (Python estimate)
         geom.cs     stV/stF arrays (create-stopes-from-geometry.cs style)
         blocks_5m_geometry.txt  one line per block "chfrom,chto,t,g,w|x,y,z,...|i,j,k,..." (1-based faces)
                     for create-blocks-from-geometry.cs
Run: python3 -I blocks_along_drive.py <folder> [NL=4] [MARG=2.5] [ORE_SHARE=0.15]   (~2 min)
"""
import sys, collections, numpy as np
D = sys.argv[1].rstrip('/') + '/'
CUT, MIN_GRADE, MIN_W, MAX_L, PILLAR = 2.5, 2.6, 2.2, 40, 5
DRIVE_H, DRIVE_W, HEIGHT, BASE_BELOW_BACK = 4.5, 4.5, 20.0, 1.0
NL, SEC, SMOOTH_S, MAX_STEP = int(sys.argv[2]) if len(sys.argv)>2 else 4, 5.0, 5, 1.0
MARG = float(sys.argv[3]) if len(sys.argv)>3 else CUT   # marginal grade a wall strip must carry to be taken in
ORE_SHARE = float(sys.argv[4]) if len(sys.argv)>4 else 0.3   # ...or this share of its tonnes in >= CUT blocks
MIN_DIP = 55.0   # wall dip from horizontal: footwall >= 50 deg, 55 preferred (mine-design-standards.md)
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
V = T*(G-MARG); MET = T*G; nmin = int(np.ceil(MIN_W/0.25)); inside = np.abs(Ug) <= DRIVE_W/2
# 2. walls per 1 m station
S1 = np.arange(0.5, Sg[-1], 1.0); FW = np.zeros((len(S1), NL)); HW = np.zeros_like(FW)
for a, s in enumerate(S1):
    for l, z in enumerate(LZ):
        prof = V[np.abs(Sg-s) <= 2.5][:, :, np.abs(Zg-z) <= (ZT-ZB)/(NL-1)/2+0.1].sum(axis=(0, 2))
        if l == 0: prof = np.where(inside, prof, -1e9)
        cs = np.r_[0, np.cumsum(prof)]
        best, i, j = max((cs[j]-cs[i], i, j) for i in range(len(Ug)) for j in range(i+nmin, len(Ug)+1))
        if best <= 0:   # no economic window here: follow the lode's metal at minimum width, not the emptiest window
            mp = MET[np.abs(Sg-s) <= 2.5][:, :, np.abs(Zg-z) <= (ZT-ZB)/(NL-1)/2+0.1].sum(axis=(0, 2))
            if l == 0: mp = np.where(inside, mp, -1e9)
            cm = np.r_[0, np.cumsum(mp)]; _, i = max((cm[i+nmin]-cm[i]-1e-6*abs(Ug[i]+0.125*nmin), i) for i in range(len(Ug)-nmin+1)); j = i+nmin
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
    return t.sum(), (t*G[sm]).sum(), (t*(G[sm] >= CUT)).sum()
# Drillable (rods don't bend): holes are straight from the rig in the drive, so each section must be convex
# (no wall bending back in), each wall segment at least MIN_DIP, and each point moves <= MAX_STEP between sections.
DZ = (ZT-ZB)/(NL-1); MAX_OUT = DZ/np.tan(np.radians(MIN_DIP))
def section_ok(f, h):
    if np.any(h-f < MIN_W-1e-9) or f[0] < -DRIVE_W/2-1e-9 or h[0] > DRIVE_W/2+1e-9: return False
    for d in (np.diff(-f), np.diff(h)):         # outward steps up each wall
        if np.any(np.abs(d) > MAX_OUT+1e-9) or np.any(np.diff(d) > 1e-9): return False   # steps must not grow: convex
    return True
def strike_limit(x):                            # largest change MAX_STEP between neighbouring sections
    for _ in range(50):
        for k in list(range(1, len(x)))+list(range(len(x)-2, -1, -1)):
            for nb in (k-1, k+1):
                if 0 <= nb < len(x) and abs(x[k]-x[nb]) > MAX_STEP: x[k] = (x[k]+x[nb])/2+np.sign(x[k]-x[nb])*MAX_STEP/2
    return x
def straighten(fw, hw):                         # straight walls from base to top, dip and strike limited
    for A, sg in ((fw, -1), (hw, 1)):
        A[:, 0] = strike_limit(A[:, 0].copy()); top = A[:, -1].copy()
        top = A[:, 0]+np.clip(top-A[:, 0], -MAX_OUT*(NL-1), MAX_OUT*(NL-1)); A[:, -1] = strike_limit(top)
        for q in range(1, NL-1): A[:, q] = A[:, 0]+(A[:, -1]-A[:, 0])*q/(NL-1)
    for si in range(len(fw)):
        for q in range(NL):
            if hw[si, q]-fw[si, q] < MIN_W: c = (hw[si, q]+fw[si, q])/2; fw[si, q] = c-MIN_W/2; hw[si, q] = c+MIN_W/2
        sh = max(hw[si, 0]-DRIVE_W/2, 0)-max(-DRIVE_W/2-fw[si, 0], 0); fw[si] -= sh; hw[si] -= sh
# 3. one shape along the whole drive, a section every SEC m
i, j = 0, int(np.floor(cum[-1]))
# 4. sections every SEC m, nudge wall points
cs_out = ''
for a, c in ((i, j),):
    ss = list(np.arange(a, c, SEC))
    if c-ss[-1] < SEC/2: ss[-1] = c          # merge a short remainder into the last block
    else: ss.append(c)
    ss = np.array(ss, float)
    fw = np.array([[np.interp(s, S1, FW[:, q]) for q in range(NL)] for s in ss]); hw = np.array([[np.interp(s, S1, HW[:, q]) for q in range(NL)] for s in ss])
    straighten(fw, hw)
    t0, m0, o0 = evaluate(ss, fw, hw)
    for _ in range(12):
        changed = 0
        for si in range(len(ss)):
            for q in range(NL):
                for A, sign in ((fw, -1), (hw, 1)):
                    for step in (0.25, -0.25):
                        old = A[si, q]; A[si, q] = old+sign*step
                        ok = section_ok(fw[si], hw[si]) and all(abs(A[si, q]-A[x, q]) <= MAX_STEP+1e-9 for x in (si-1, si+1) if 0 <= x < len(ss))
                        if ok:
                            t1, m1, o1 = evaluate(ss, fw, hw); dt, dm, do = t1-t0, m1-m0, o1-o0
                            # take a strip in if it carries the marginal grade or is mostly >= cut-off blocks; drop it only if neither
                            good = dt != 0 and (dm/dt >= MARG or do/dt >= ORE_SHARE)
                            if ((step > 0 and dt > 0 and good) or (step < 0 and dt < 0 and not good)): t0, m0, o0 = t1, m1, o1; changed += 1; continue
                        A[si, q] = old
        if not changed: break
    bad = [si for si in range(len(ss)) if not section_ok(fw[si], hw[si])]
    print('sections failing the drill check:', bad)
    R = 2*NL; rows = []; geo = []
    for bi in range(len(ss)-1):                 # one closed solid per block, neighbours share their end sections
        Vx, F = [], []
        for si in (bi, bi+1):
            p, nrm = frame(ss[si])
            for u, z in [(fw[si, q], LZ[q]) for q in range(NL)]+[(hw[si, q], LZ[q]) for q in range(NL-1, -1, -1)]: Vx.append((*(p+u*nrm), z))
        for e in range(R): a1 = (e+1) % R; F += [(e, a1, a1+R), (e, a1+R, e+R)]
        for base, flip in ((0, True), (R, False)):
            for q in range(NL-1):
                tri = [(base+q, base+q+1, base+R-2-q), (base+q, base+R-2-q, base+R-1-q)]
                F += [(x, z, y) for x, y, z in tri] if flip else tri
        Vx = np.array(Vx)
        if sum(np.dot(Vx[x], np.cross(Vx[y], Vx[z])) for x, y, z in F) < 0: F = [(x, z, y) for x, y, z in F]
        tb, mb, _ = evaluate(ss[bi:bi+2], fw[bi:bi+2], hw[bi:bi+2])
        rows.append((ss[bi], ss[bi+1], tb, mb/max(tb, 1e-9), (hw[bi:bi+2]-fw[bi:bi+2]).mean()))
        geo.append('%.1f,%.1f,%.1f,%.3f,%.2f|' % rows[-1]+','.join('%.3f' % x for v in Vx for x in v)+'|'+','.join(str(x+1) for f in F for x in f))
        cs_out += 'stV.Add(new double[]{'+','.join('%.3f' % x for v in Vx for x in v)+'});\n'+'stF.Add(new int[]{'+','.join(str(x+1) for f in F for x in f)+'});\n'
    with open(D+'blocks.txt', 'w') as fo:
        for r in rows: fo.write('%.1f,%.1f,%.1f,%.3f,%.2f\n' % r); print('ch %5.1f-%5.1f  %6.0f t @ %5.2f g/t  width %.2f' % r)
    open(D+'blocks_5m_geometry.txt', 'w').write('\n'.join(geo)+'\n')
open(D+'geom.cs', 'w').write(cs_out)
