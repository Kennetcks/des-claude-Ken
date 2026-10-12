"""Share of the >= 2.6 g/t (unmined) ore in the exported model that sits inside the kept blocks' 1 m voxels."""
import sys, pickle, numpy as np
from raster import PX,PY,PZ,NX,NY,NZ,SX,SY,SZ,RX,RY,RZ
comp=np.load('v_comp.npy'); TD=np.load('v_td.npy',mmap_mode='r'); AU=np.load('v_au.npy',mmap_mode='r'); MN=np.load('v_mn.npy',mmap_mode='r')
sel=np.nonzero((np.asarray(AU)>=2.6)&(np.asarray(TD)>0)&(np.asarray(MN)==0))[0]
inv=np.full(comp.max()+1,-1,np.int64); pidx=np.nonzero(comp>=0)[0]; inv[comp[pidx]]=pidx
c=sel//(RX*RY*RZ); r=sel%(RX*RY*RZ); si=r//(RY*RZ); sj=(r//RZ)%RY; sk=r%RZ
p=inv[c]; k=p%NZ; j=(p//NZ)%NY; i=p//(NZ*NY)
x=i*PX+(si+0.5)*SX; y=j*PY+(sj+0.5)*SY; z=k*PZ+(sk+0.5)*SZ
t=np.asarray(TD)[sel]*SX*SY*SZ; g=np.asarray(AU)[sel]
vid=(np.floor(x).astype(np.int64)*2000+np.floor(y).astype(np.int64))*2000+np.floor(z).astype(np.int64)
for f in sys.argv[1:]:
    k_=pickle.load(open(f,'rb')); V=np.unique(np.concatenate([b['vox'] for b in k_]))
    m=np.isin(vid,V)
    print('%s: ore >=2.6 in model %.2f Mt @ %.2f; inside blocks %.2f Mt @ %.2f (%.0f%%)'%(f,t.sum()/1e6,(t*g).sum()/t.sum(),t[m].sum()/1e6,(t[m]*g[m]).sum()/t[m].sum(),100*t[m].sum()/t.sum()))
