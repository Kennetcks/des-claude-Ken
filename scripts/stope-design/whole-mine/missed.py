import sys, pickle, numpy as np
from scipy.spatial import cKDTree
exec(open('capture.py').read().split("for f in sys.argv[1:]:")[0])
k_=pickle.load(open(sys.argv[1],'rb')); V=np.unique(np.concatenate([b['vox'] for b in k_]))
m=np.isin(vid,V); mx=~m
P=np.c_[(V//2000)//2000,(V//2000)%2000,V%2000].astype(np.float32)+0.5
tr=cKDTree(P)
Q=np.c_[x[mx],y[mx],z[mx]].astype(np.float32); tm=t[mx]; gm=g[mx]
dd,_=tr.query(Q,distance_upper_bound=25,workers=4)
for lo,hi in ((0,2),(2,4),(4,6),(6,10),(10,25),(25,1e9)):
    s=(dd>lo)&(dd<=hi); print('missed ore %3.0f-%3.0f m from a block: %.3f Mt @ %.2f'%(lo,min(hi,999),tm[s].sum()/1e6,(tm[s]*gm[s]).sum()/max(tm[s].sum(),1)),flush=True)
# horizontal neighbours: same z band (+-1 m) within 4 m in plan
P2=P.copy(); P2[:,2]*=6.0; Q2=Q.copy(); Q2[:,2]*=6.0           # squash vertical so only near-horizontal matches count
d2,_=cKDTree(P2).query(Q2,distance_upper_bound=4.5,workers=4)
near=dd<=4
print('near (<=4 m): beside a block %.3f Mt, above/below only %.3f Mt'%(tm[near&(d2<=4.5)].sum()/1e6, tm[near&(d2>4.5)].sum()/1e6))
np.save('missed_pts.npy',np.c_[Q,tm,gm,dd])
