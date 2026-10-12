"""Rebuild blocks Deswik reports as invalid (Polyface.Volume == -1, end sections crossing on a tight bend)
as convex hulls of their 16 vertices. Usage: python3 hull_invalid_blocks.py geometry.txt out.txt "NAME 1" "NAME 2" ..."""
import sys, json, numpy as np
from scipy.spatial import ConvexHull
src, dst, bad = sys.argv[1], sys.argv[2], set(sys.argv[3:])
out = []
for line in open(src):
    m, v, f = line.rstrip('\n').split('|')
    if json.loads(m)['name'] not in bad: continue
    V = np.array(v.split(','), float).reshape(-1, 3); c = V.mean(0); h = ConvexHull(V - c); F = []
    for s, eq in zip(h.simplices, h.equations):
        a, b, cc = (V - c)[s]
        F.append(list(s) if np.dot(np.cross(b - a, cc - a), eq[:3]) > 0 else [s[0], s[2], s[1]])
    out.append(m + '|' + v + '|' + ','.join(str(x + 1) for t in F for x in t))
open(dst, 'w').write('\n'.join(out) + '\n')
