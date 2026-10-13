"""Decline extension and level development for Kennet's ST stopes (2026-10-13).
Extends DESIGN\\DECLINE from its last vertex at 1:7 in stacked 22 m loops, as central to the stope groups as the
standoffs allow, then per level group: access (90 deg take-off, >=10 m straight, ~1:50 up), ore drive on the stope floor,
level stockpile (ISP), RAD to a RAR per stope stack, escapeway drive (EWD) to an escapeway raise (EWR) per stack,
decline stockpiles (DSP) and sumps (SMP). Output: strings.csv (id,act,level,lode,x,y,z) and a report."""
import sys, csv, json, pickle, collections, numpy as np
from scipy.spatial import cKDTree
sys.path.insert(0, '/home/claude/work')

R = 22.0            # loop radius (Kennet)
G = 7.0             # 1:7
PITCH = 2 * np.pi * R / G   # 19.75 m drop per loop
D_STOPE = float(__import__('os').environ.get('D_STOPE', '30'))   # decline centreline to any stope (m)
DRIFT = 12.0        # max axis move per loop (m)
TAKE = 12.0         # straight take-off (>= 10 m)
R_ROUTE = R + TAKE  # accesses route round the loop on this radius (>= 10 m from the decline)
W = '/home/claude/work/dec/'

# ---------- stopes, ore drives, level groups ----------
K = pickle.load(open('/home/claude/work/st/kept_stxi.pkl', 'rb'))
stope_pts = np.vstack([np.array(b['V']).reshape(-1, 3) for b in K])
tree = cKDTree(stope_pts)
drv = collections.defaultdict(list)
for b in K:
    if b.get('kind') == 'INTERSECTION': continue
    drv[b['pid']].append(b)
OD = {}
for p, bs in drv.items():
    by = collections.defaultdict(list)
    for b in bs: by[b['bi']].append(b)
    pts = []
    for bi in sorted(by):
        V = np.vstack([np.array(b['V']).reshape(-1, 3) for b in by[bi]]); zmin = V[:, 2].min()
        bot = V[V[:, 2] <= zmin + 0.5]; pts.append([bot[:, 0].mean(), bot[:, 1].mean(), zmin])
    pts = np.array(pts)
    if len(pts) == 1:   # one block: short drive along its long axis
        V = np.vstack([np.array(b['V']).reshape(-1, 3) for b in bs]); c = V[:, :2].mean(0)
        u = np.linalg.svd(V[:, :2] - c)[2][0]; pts = np.array([[*(c - 2.5 * u), pts[0, 2]], [*(c + 2.5 * u), pts[0, 2]]])
    OD[p] = dict(pid=p, pts=pts, rl=float(np.median(pts[:, 2])), t=sum(b['t'] for b in bs),
                 extra=bs[0].get('kind') == 'EXTRA')
# level groups: drives on the same floor (+-5 m) within 40 m of each other share one access
keys = list(OD); par = {k: k for k in keys}
def find(a):
    while par[a] != a: par[a] = par[par[a]]; a = par[a]
    return a
for i, a in enumerate(keys):
    for b in keys[i + 1:]:
        if abs(OD[a]['rl'] - OD[b]['rl']) <= 5:
            d = np.min(np.linalg.norm(OD[a]['pts'][:, None, :2] - OD[b]['pts'][None, :, :2], axis=2))
            if d <= 40: par[find(a)] = find(b)
GR = collections.defaultdict(list)
for k in keys: GR[find(k)].append(k)
groups = []
for ks in GR.values():
    t = sum(OD[k]['t'] for k in ks); P = np.vstack([OD[k]['pts'] for k in ks])
    rl = float(np.average([OD[k]['rl'] for k in ks], weights=[OD[k]['t'] for k in ks]))
    groups.append(dict(drives=sorted(ks, key=lambda k: -OD[k]['t']), t=t, rl=rl, c=P[:, :2].mean(0)))
groups.sort(key=lambda g: -g['rl'])
for i, g in enumerate(groups): g['id'] = 'L%d-%d' % (round(g['rl']) - 6000, i)

# ---------- decline axis per loop (DP) ----------
Dold = np.array([[float(r['x']), float(r['y']), float(r['z'])] for r in csv.DictReader(open('/mnt/user-data/uploads/work/stopes_mine/decline_design.csv'))])
E = Dold[-1]; h = Dold[-1] - Dold[-2]; h = h[:2] / np.linalg.norm(h[:2])
zbot = min(g['rl'] for g in groups) - 6.0
NL = int(np.ceil((E[2] - zbot) / PITCH)) + 1
xs = np.arange(7990, 8360, 5.0); ys = np.arange(63150, 63600, 5.0)
GX, GY = np.meshgrid(xs, ys, indexing='ij'); S = np.c_[GX.ravel(), GY.ravel()]
th = np.linspace(0, 2 * np.pi, 36, endpoint=False)

def loop_clear(ax, z0):
    """min 3D distance from a loop (axis ax, top z0, one turn down) to the stopes"""
    P = np.c_[ax[0] + R * np.cos(th), ax[1] + R * np.sin(th), z0 - PITCH * th / (2 * np.pi)]
    return tree.query(P)[0].min()

best = None
for sense in (1, -1):    # 1 = counter-clockwise (left turns)
    nrm = np.array([-h[1], h[0]]) * sense
    a0 = E[:2] + R * nrm
    cost = {}; back = []
    # loop 0 axis is fixed by the joint (tangent to the old decline)
    prev = {tuple(a0): (0.0 if loop_clear(a0, E[2]) >= D_STOPE else 1e9)}
    for k in range(1, NL):
        z0 = E[2] - k * PITCH; zlo, zhi = z0 - PITCH - 10, z0 + 10
        gs = [g for g in groups if zlo <= g['rl'] <= zhi]
        cur = {}; bk = {}
        Pk = np.array(list(prev.keys())); Vk = np.array([prev[tuple(p)] for p in Pk])
        tp = cKDTree(Pk)
        for s in S:
            idx = tp.query_ball_point(s, DRIFT)
            if not idx: continue
            j = min(idx, key=lambda i: Vk[i]); v0 = Vk[j]
            if v0 >= 1e9: continue
            cl = loop_clear(s, z0)
            pen = 0 if cl >= D_STOPE else 1e9
            c = sum(g['t'] * max(np.linalg.norm(s - g['c']) - R, 0) for g in gs) / 1000.0
            cur[tuple(s)] = v0 + c + pen; bk[tuple(s)] = tuple(Pk[j])
        prev = cur; back.append(bk)
        if not prev: break
    if not prev: continue
    last = min(prev, key=prev.get)
    if prev[last] >= 1e9: continue
    axes = [last]
    for bk in reversed(back): axes.append(bk[axes[-1]])
    axes = axes[::-1]
    if best is None or prev[last] < best[0]: best = (prev[last], sense, axes)
assert best, 'no decline path keeps the standoff'
_, SENSE, AXES = best; AXES = np.array(AXES)

# ---------- decline centreline ----------
def decline_path():
    pts = []; a0 = AXES[0]
    ang0 = np.arctan2(E[1] - a0[1], E[0] - a0[0])
    n = NL * 72
    for i in range(n + 1):
        u = i / 72.0                                  # loops done
        k = min(int(u), NL - 1); f = u - k
        ax = AXES[k] if k + 1 >= NL else AXES[k] * (1 - f) + AXES[k + 1] * f
        ang = ang0 + SENSE * 2 * np.pi * u
        pts.append([ax[0] + R * np.cos(ang), ax[1] + R * np.sin(ang)])
    pts = np.array(pts); pts[0] = E[:2]
    s = np.r_[0, np.cumsum(np.linalg.norm(np.diff(pts, axis=0), axis=1))]
    z = E[2] - s / G
    P = np.c_[pts, z]; P = P[P[:, 2] >= zbot - 2]
    return P
DEC = decline_path()
sDEC = np.r_[0, np.cumsum(np.linalg.norm(np.diff(DEC, axis=0), axis=1))]
def axis_at(z):
    u = (E[2] - z) / PITCH; k = min(int(max(u, 0)), NL - 1); f = u - k
    return AXES[k] if k + 1 >= NL else AXES[k] * (1 - f) + AXES[k + 1] * f



# ---------- central RAR and escapeway raise beside the decline (one each, follow the loop axis as it steps) ----------
ZTOP = max(g['rl'] for g in groups) + 5.0; ZBOT = min(g['rl'] for g in groups) - 1.0
def raise_line(az, dist):
    zz = np.arange(ZBOT, ZTOP + 0.1, 2.0); off = dist * np.array([np.cos(az), np.sin(az)])
    # straight raise: best-fit line through the loop axis as it steps down, offset to one side
    A = np.array([axis_at(min(z, E[2])) for z in zz]); cx = np.polyfit(zz, A[:, 0], 1); cy = np.polyfit(zz, A[:, 1], 1)
    return np.c_[np.polyval(cx, zz) + off[0], np.polyval(cy, zz) + off[1], zz]
best_r = None
for az1 in np.radians(np.arange(0, 360, 10)):
    for dist in (55.0, 60.0, 65.0):
        L1 = raise_line(az1, dist); c1 = tree.query(L1)[0].min()
        if c1 < 15 or cKDTree(DEC).query(L1)[0].min() < 20: continue
        for az2 in az1 + np.radians(np.arange(120, 241, 10)):
            L2 = raise_line(az2, dist); c2 = tree.query(L2)[0].min()
            if c2 < 15 or cKDTree(DEC).query(L2)[0].min() < 20: continue
            sc = min(c1, c2)
            if best_r is None or sc > best_r[0]: best_r = (sc, L1, L2, az1, az2, dist)
assert best_r, 'no clear raise positions'
_, RARL, EWRL, AZR, AZE, DR = best_r
def raise_xy(L, z): return np.array([np.interp(z, L[:, 2], L[:, 0]), np.interp(z, L[:, 2], L[:, 1])])

# ---------- grid router for accesses (avoids stopes on the level and keeps 10 m off the decline) ----------
import heapq
GS = 2.0
gx0, gy0 = 7950.0, 63050.0; NX, NY = int(450 / GS), int(600 / GS)
BLK = [(np.array(b['V']).reshape(-1, 3), b) for b in K]
BLK_Z = np.array([[V[:, 2].min(), V[:, 2].max()] for V, _ in BLK])
from matplotlib.path import Path as MPath
def footprint_mask(V):
    from scipy.spatial import ConvexHull
    try: hull = ConvexHull(V[:, :2]); poly = V[hull.vertices, :2]
    except Exception: return None
    lo = np.floor((poly.min(0) - [gx0, gy0]) / GS).astype(int) - 2; hi = np.ceil((poly.max(0) - [gx0, gy0]) / GS).astype(int) + 2
    lo = np.maximum(lo, 0); hi = np.minimum(hi, [NX - 1, NY - 1])
    ii, jj = np.meshgrid(np.arange(lo[0], hi[0] + 1), np.arange(lo[1], hi[1] + 1), indexing='ij')
    P = np.c_[gx0 + (ii.ravel() + .5) * GS, gy0 + (jj.ravel() + .5) * GS]
    # dilate the footprint by 2.5 m (half a drive width)
    c = poly.mean(0); pp = c + (poly - c) * (1 + 2.5 / np.maximum(np.linalg.norm(poly - c, axis=1, keepdims=True), 1))
    m = MPath(pp).contains_points(P)
    return ii.ravel()[m], jj.ravel()[m]
FP = [footprint_mask(V) for V, _ in BLK]
def obstacles(z, own_pids, s0xy):
    occ = np.zeros((NX, NY), bool)
    sel = np.nonzero((BLK_Z[:, 0] < z + 6.5) & (BLK_Z[:, 1] > z - 1.0))[0]
    for i in sel:
        if BLK[i][1]['pid'] in own_pids or FP[i] is None: continue
        occ[FP[i]] = True
    near = DALL[np.abs(DALL[:, 2] - z) < 8]
    near = np.vstack([near] + [np.r_[raise_xy(L, z), z][None] for L in (RARL, EWRL)])   # keep 10 m off the raises too
    if len(near):
        ii, jj = np.meshgrid(np.arange(NX), np.arange(NY), indexing='ij')
        P = np.c_[gx0 + (ii.ravel() + .5) * GS, gy0 + (jj.ravel() + .5) * GS]
        d = cKDTree(near[:, :2]).query(P)[0].reshape(NX, NY)
        occ |= d < 10.5
        pass   # the take-off end (12 m out) is already outside the 10.5 m buffer
    return occ
def cell(p): return (int((p[0] - gx0) / GS), int((p[1] - gy0) / GS))
def route(a, b, occ):
    sa, sb = cell(a), cell(b); occ = occ.copy(); occ[sa] = occ[sb] = False
    # free a small pocket round the goal (the ore drive end sits under its own stope)
    for di in range(-2, 3):
        for dj in range(-2, 3):
            q = (sb[0] + di, sb[1] + dj)
            if 0 <= q[0] < NX and 0 <= q[1] < NY: occ[q] = False
    D = {sa: 0.0}; prev = {}; hq = [(0.0, 0.0, sa)]
    nb = [(1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1), (2, 1), (1, 2), (-2, 1), (-1, 2), (2, -1), (1, -2), (-2, -1), (-1, -2)]
    while hq:
        _, d, u = heapq.heappop(hq)
        if u == sb: break
        if d > D.get(u, 1e18): continue
        for di, dj in nb:
            v = (u[0] + di, u[1] + dj)
            if not (0 <= v[0] < NX and 0 <= v[1] < NY) or occ[v]: continue
            nd = d + np.hypot(di, dj) * GS
            if nd < D.get(v, 1e18):
                D[v] = nd; prev[v] = u; heapq.heappush(hq, (nd + np.hypot(v[0] - sb[0], v[1] - sb[1]) * GS * 0.999, nd, v))
    if sb not in D: return None
    path = [sb]
    while path[-1] != sa: path.append(prev[path[-1]])
    P = np.array([[gx0 + (i + .5) * GS, gy0 + (j + .5) * GS] for i, j in path[::-1]]); P[0] = a[:2]; P[-1] = b[:2]
    # string pulling
    def clear(p, q):
        n = max(int(np.linalg.norm(q - p) / 1.0), 1)
        for t in np.linspace(0, 1, n + 1):
            c = cell(p + (q - p) * t)
            if 0 <= c[0] < NX and 0 <= c[1] < NY and occ[c] and c != sb and np.linalg.norm(p + (q - p) * t - b[:2]) > 6: return False
        return True
    out = [P[0]]; i = 0
    while i < len(P) - 1:
        j = len(P) - 1
        while j > i + 1 and not clear(P[i], P[j]): j -= 1
        out.append(P[j]); i = j
    return np.array(out)

# ---------- accesses ----------
out = [('DEC', None, 'DECLINE', DEC)]
used_s = []   # chainages of take-offs on the decline
def arc(ax, r, a1, a2, z1, z2, sense):
    d = (a2 - a1) % (2 * np.pi) if sense > 0 else -((a1 - a2) % (2 * np.pi))
    n = max(int(abs(d) * r / 3), 1); aa = a1 + d * np.linspace(0, 1, n + 1)
    return np.c_[ax[0] + r * np.cos(aa), ax[1] + r * np.sin(aa), np.linspace(z1, z2, n + 1)]
report = []
DALL = np.vstack([Dold[:-1], DEC]); nOLD = len(Dold) - 1     # take-offs may also come off the existing decline
for g in groups:
    target_rl = g['rl']
    ax = axis_at(min(target_rl, E[2]))
    cand = [(np.linalg.norm(OD[k]['pts'][:, :2] - ax, axis=1).min(), k) for k in g['drives']]
    _, k0 = min(cand); P0 = OD[k0]['pts']; j = np.argmin(np.linalg.norm(P0[:, :2] - ax, axis=1)); T = P0[j].copy()
    L = max(np.linalg.norm(T[:2] - ax) - R, 20)
    for _ in range(5):
        zs = target_rl - L / 50.0
        i = int(np.argmin(np.abs(DALL[:, 2] - zs) + 0.0 * np.arange(len(DALL)))); S0 = DALL[i]
        t_ = DALL[min(i + 1, len(DALL) - 1)] - DALL[max(i - 1, 0)]; t_ = t_[:2] / np.linalg.norm(t_[:2])
        pts = [S0]
        if i < nOLD:     # straight part of the existing decline: perpendicular on the side facing the ore
            nrm = np.array([-t_[1], t_[0]]); nrm = nrm if np.dot(nrm, T[:2] - S0[:2]) > 0 else -nrm
        else:            # loop: radial out = perpendicular to the decline (never cut into the loop)
            axz = axis_at(S0[2]); nrm = (S0[:2] - axz) / np.linalg.norm(S0[:2] - axz)
        A1 = np.r_[S0[:2] + nrm * TAKE, 0]; pts.append(A1)
        occ = obstacles(S0[2], set(g['drives']), S0[:2])
        rt = route(A1, T, occ)
        if rt is None: g['blocked'] = True; rt = np.array([A1[:2], T[:2]])
        pts += [np.r_[q, 0] for q in rt[1:-1]]
        pts.append(T)
        pts = np.array(pts, float); s_ = np.r_[0, np.cumsum(np.linalg.norm(np.diff(pts[:, :2], axis=0), axis=1))]
        Lnew = s_[-1]
        pts[:, 2] = S0[2] + (T[2] - S0[2]) * s_ / s_[-1]
        if abs(Lnew - L) < 1: break
        L = Lnew
    sD = np.r_[0, np.cumsum(np.linalg.norm(np.diff(DALL, axis=0), axis=1))][i] - np.r_[0, np.cumsum(np.linalg.norm(np.diff(DALL, axis=0), axis=1))][nOLD]
    g['acc'] = pts; g['s_dec'] = sD; used_s.append(sD)
    out.append(('ACC', g['rl'], g['id'], pts))
    # ore drives of the group (whole drive), crosscuts between drives of the group (MST)
    for k in g['drives']: out.append(('OD', OD[k]['rl'], g['id'], OD[k]['pts']))
    con = [k0]; rest = [k for k in g['drives'] if k != k0]
    while rest:
        bestc = None
        for a in con:
            for b in rest:
                Da = OD[a]['pts']; Db = OD[b]['pts']; M = np.linalg.norm(Da[:, None, :2] - Db[None, :, :2], axis=2)
                ia, ib = np.unravel_index(M.argmin(), M.shape)
                if bestc is None or M[ia, ib] < bestc[0]: bestc = (M[ia, ib], a, b, Da[ia], Db[ib])
        _, a, b, pa, pb = bestc
        if np.linalg.norm(pa[:2] - pb[:2]) > 1: out.append(('ACC', g['rl'], g['id'], np.array([pa, pb])))
        con.append(b); rest.remove(b)
    rise = (T[2] - S0[2]) / max(L, 1)
    if g.get('blocked'): report.append(dict(group=g['id'], problem='no route clear of stopes; straight line used'))
    report.append(dict(group=g['id'], rl=round(g['rl'], 1), kt=round(g['t'] / 1000, 1), drives=len(g['drives']),
                       acc_len=round(L), grade='1:%d' % round(1 / rise) if rise > 1e-3 else 'flat/down', clear_dec_take=TAKE))

# ---------- RAD / EWD from each access to the central raises ----------
def seg_pts(P, step=2.0):
    o = []
    for a_, b_ in zip(P[:-1], P[1:]):
        n = max(int(np.linalg.norm(b_ - a_) / step), 1); o.append(a_ + (b_ - a_) * np.linspace(0, 1, n + 1)[:, None])
    return np.vstack(o)
DECT = cKDTree(DEC[:, :2])
raises = []
for kind, L in (('RAR', RARL), ('EWR', EWRL)):
    conn = 'RAD' if kind == 'RAR' else 'EWD'
    for g in groups:
        A = seg_pts(g['acc']); sA = np.r_[0, np.cumsum(np.linalg.norm(np.diff(A[:, :2], axis=0), axis=1))]
        p = raise_xy(L, A[0, 2]); dd = np.linalg.norm(A[:, :2] - p, axis=1); dd[sA < TAKE + 3] = 1e9
        occ = obstacles(A[0, 2], set(), A[0, :2])
        def seg_clear(B, Q):
            for t in np.linspace(0, 1, max(int(np.linalg.norm(Q[:2] - B[:2])), 2)):
                q = B[:2] + (Q[:2] - B[:2]) * t
                if np.linalg.norm(q - Q[:2]) < 11 or np.linalg.norm(q - B[:2]) < 4: continue
                c = cell(q)
                if 0 <= c[0] < NX and 0 <= c[1] < NY and occ[c]: return False
            return True
        conn_pts = None
        for j in np.argsort(dd)[:60]:
            if dd[j] >= 1e9: break
            B = A[j]; Q = np.r_[raise_xy(L, B[2]), B[2] + (0.5 if kind == 'RAR' else 0)]
            if seg_clear(B, Q): conn_pts = np.array([B, Q]); break
        if conn_pts is None:      # route round the stopes
            j = int(dd.argmin()); B = A[j]; Q = np.r_[raise_xy(L, B[2]), B[2] + (0.5 if kind == 'RAR' else 0)]
            oc2 = occ.copy(); ii, jj = np.meshgrid(np.arange(NX), np.arange(NY), indexing='ij')
            oc2[np.hypot(gx0 + (ii + .5) * GS - Q[0], gy0 + (jj + .5) * GS - Q[1]) < 11] = False
            rt = route(B, Q, oc2)
            if rt is None: conn_pts = np.array([B, Q]); report.append(dict(group=g['id'], problem=conn + ' crosses stopes'))
            else:
                zz = np.linspace(B[2], Q[2], len(rt)); conn_pts = np.c_[rt, zz]
        g[conn] = conn_pts; out.append((conn, g['rl'], g['id'], conn_pts))
    top = L[-1]
    out.append((kind, None, 'CENTRAL', np.array([L[0], L[-1]])))
    raises.append((kind, 'CENTRAL', L[0], len(groups)))
# ---------- level stockpiles (ISP): one per access, 15 m stub off the access, clear of the decline, raises, RAD/EWD and drives ----------
LATP = np.vstack([seg_pts(np.asarray(P)) for t, _, _, P in out if t in ('ACC', 'OD', 'RAD', 'EWD')])
for g in groups:
    A = seg_pts(g['acc']); sA = np.r_[0, np.cumsum(np.linalg.norm(np.diff(A[:, :2], axis=0), axis=1))]
    z = A[0, 2]; band = LATP[np.abs(LATP[:, 2] - z) < 6]; bt = cKDTree(band[:, :2])
    dz = DALL[np.abs(DALL[:, 2] - z) < 8]; dt = cKDTree(dz[:, :2]) if len(dz) else None
    done = False
    for s_at in np.arange(25, max(sA[-1] - 10, 26), 5):
        j = min(int(np.searchsorted(sA, s_at)), len(A) - 2); B = A[j]; t_ = A[j + 1] - A[max(j - 1, 0)]; t_ = t_[:2] / np.linalg.norm(t_[:2])
        for sgn in (1, -1):
            nrm = np.array([-t_[1], t_[0]]) * sgn; Q = np.r_[B[:2] + nrm * 15, B[2] + 0.3]
            mid = B[:2] + nrm * 7.5
            ok = bt.query(Q[:2])[0] >= 7 and bt.query(mid)[0] >= 4 and (dt is None or dt.query(Q[:2])[0] >= 10) \
                 and min(np.linalg.norm(Q[:2] - raise_xy(L, z)) for L in (RARL, EWRL)) >= 10 and tree.query(Q)[0] >= 5
            if ok: out.append(('ISP', g['rl'], g['id'], np.array([B, Q]))); done = True; break
        if done: break
    if not done: report.append(dict(group=g['id'], problem='no clear spot for the level stockpile'))

# ---------- decline stockpiles (DSP, outside of the loop, one per loop) and sumps (SMP, inside, ~30 m vertical) ----------
def stub(s_at, outward, length, grade):
    i = int(np.searchsorted(sDEC, s_at)); i = min(max(i, 1), len(DEC) - 2); B = DEC[i]
    axz = axis_at(B[2]); n = (B[:2] - axz) / np.linalg.norm(B[:2] - axz) * (1 if outward else -1)
    return np.array([B, np.r_[B[:2] + n * length, B[2] + grade * length]])
Ltot = sDEC[-1]; s = 40.0; nd = 0
while s < Ltot - 20:
    ok = all(abs(s - u) > 25 for u in used_s)
    if ok: out.append(('DSP', None, 'DECLINE', stub(s, True, 22, 1 / 50.))); nd += 1; s += 150
    else: s += 5
z_last = E[2]; s = 20.0; ns = 0
while s < Ltot:
    i = int(np.searchsorted(sDEC, s)); z = DEC[min(i, len(DEC) - 1), 2]
    if z_last - z >= 30 and all(abs(s - u) > 20 for u in used_s):
        out.append(('SMP', None, 'DECLINE', stub(s, False, 10, -1 / 7.))); z_last = z; ns += 1
    s += 5
out.append(('SMP', None, 'DECLINE', np.array([DEC[-1], DEC[-1] + np.r_[(DEC[-1] - DEC[-2])[:2] / np.linalg.norm((DEC[-1] - DEC[-2])[:2]) * 10, -10 / 7.]])))

# ---------- checks ----------
def mind(P, Q): return cKDTree(Q).query(P)[0].min()
dec_stope = tree.query(DEC)[0].min()
ch = dict(decline_len=round(sDEC[-1]), decline_bottom=round(DEC[-1, 2], 1), loops=NL, sense='anticlockwise' if SENSE > 0 else 'clockwise',
          decline_min_dist_to_stopes=round(float(dec_stope), 1), axes=AXES.round(0).tolist(), groups=len(groups), dsp=nd, smp=ns + 1,
          raises=[(k, s_, int(n)) for k, s_, p, n in raises])
# accesses within 10 m (plan) of the decline on their level, away from their own take-off
bad = []
for g in groups:
    A = seg_pts(g['acc']); sA = np.r_[0, np.cumsum(np.linalg.norm(np.diff(A[:, :2], axis=0), axis=1))]
    near = DEC[np.abs(DEC[:, 2] - g['acc'][0, 2]) < 6]
    if len(near):
        d = cKDTree(near[:, :2]).query(A[sA > TAKE, :2])[0]
        if len(d) and d.min() < 10: bad.append((g['id'], round(float(d.min()), 1)))
    # access through stopes
    ins = tree.query(A)[0]
ch['access_within_10m_of_decline'] = bad
for nm, L in (('RAR', RARL), ('EWR', EWRL)):
    d = L[-1] - L[0]; ch[nm] = dict(bottom=L[0].round(1).tolist(), top=L[-1].round(1).tolist(), dip=round(float(np.degrees(np.arctan2(d[2], np.linalg.norm(d[:2])))), 1),
                                    min_to_stopes=round(float(tree.query(L)[0].min()), 1), min_to_decline=round(float(cKDTree(DEC).query(L)[0].min()), 1))
ch['raise_spacing'] = round(float(np.linalg.norm(RARL[0, :2] - EWRL[0, :2])), 1)
json.dump(ch, open(W + 'checks.json', 'w'), indent=1)
with open(W + 'strings.csv', 'w') as f:
    f.write('id,act,level,lode,x,y,z\n')
    for n, (t, lev, lode, P) in enumerate(out):
        for q in np.asarray(P):
            f.write('%s%03d,%s,%s,%s,%.3f,%.3f,%.3f\n' % (t, n, t, '' if lev is None else int(round(lev)) - 6000, lode, q[0], q[1], q[2]))
with open(W + 'report.csv', 'w') as f:
    ks = ['group', 'rl', 'kt', 'drives', 'acc_len', 'grade', 'clear_dec_take', 'problem']
    f.write(','.join(ks) + '\n')
    for r in report: f.write(','.join(str(r.get(k, '')) for k in ks) + '\n')
print(json.dumps(ch)[:3000])
print(collections.Counter(t for t, *_ in out))
