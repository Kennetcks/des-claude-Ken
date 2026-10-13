# Rasterise exported sub-blocks to a 0.5 x 1.25 x 0.625 m grid inside the corridor parent cells
import numpy as np, time
D='/mnt/user-data/uploads/work/stopes_mine/'
PX,PY,PZ=4.,10.,10.; NX,NY,NZ=213,120,54; SX,SY,SZ=0.5,1.25,0.625; RX,RY,RZ=8,8,16
def build(files=('corridor_blocks.f32',),pre='r'):
    t0=time.time()
    b=np.vstack([np.fromfile(D+f,dtype='<f4').reshape(-1,9) for f in files]).astype(np.float64)
    pi=np.floor(b[:,0]/PX).astype(np.int64); pj=np.floor(b[:,1]/PY).astype(np.int64); pk=np.floor(b[:,2]/PZ).astype(np.int64)
    pid=pk+NZ*(pj+NY*pi)
    up=np.unique(pid); comp=-np.ones(NX*NY*NZ,np.int64); comp[up]=np.arange(len(up))
    nsub=RX*RY*RZ; TD=np.zeros(len(up)*nsub,np.float32); AU=np.zeros(len(up)*nsub,np.float32); MN=np.zeros(len(up)*nsub,np.uint8)
    # sub-cell index range of each block inside its parent
    x0=np.rint((b[:,0]-b[:,3]/2-pi*PX)/SX).astype(int); y0=np.rint((b[:,1]-b[:,4]/2-pj*PY)/SY).astype(int); z0=np.rint((b[:,2]-b[:,5]/2-pk*PZ)/SZ).astype(int)
    nx=np.rint(b[:,3]/SX).astype(int); ny=np.rint(b[:,4]/SY).astype(int); nz=np.rint(b[:,5]/SZ).astype(int)
    base=comp[pid]*nsub
    for key in set(zip(nx,ny,nz)):
        m=(nx==key[0])&(ny==key[1])&(nz==key[2]); idx=np.nonzero(m)[0]
        a,bb,c=np.meshgrid(np.arange(key[0]),np.arange(key[1]),np.arange(key[2]),indexing='ij'); a=a.ravel();bb=bb.ravel();c=c.ravel()
        for s in range(0,len(idx),200000):
            ii=idx[s:s+200000]
            cell=base[ii,None]+((x0[ii,None]+a)*RY+(y0[ii,None]+bb))*RZ+(z0[ii,None]+c)
            TD[cell]=b[ii,7,None]; AU[cell]=b[ii,6,None]; MN[cell]=(b[ii,8,None]>0)
    np.save('/home/claude/work/%s_comp.npy'%pre,comp); np.save('/home/claude/work/%s_td.npy'%pre,TD); np.save('/home/claude/work/%s_au.npy'%pre,AU); np.save('/home/claude/work/%s_mn.npy'%pre,MN)
    print('raster cells',len(TD),'filled',(TD>0).mean(),'s',time.time()-t0)
class Model:
    def __init__(s,pre='r'):
        s.comp=np.load('/home/claude/work/%s_comp.npy'%pre); s.TD=np.load('/home/claude/work/%s_td.npy'%pre,mmap_mode='r'); s.AU=np.load('/home/claude/work/%s_au.npy'%pre,mmap_mode='r'); s.MN=np.load('/home/claude/work/%s_mn.npy'%pre,mmap_mode='r')
    def look(s,x,y,z):
        """x,y,z relative to model origin -> density, grade, mined (0 outside corridor)"""
        i=np.floor(x/PX).astype(np.int64); j=np.floor(y/PY).astype(np.int64); k=np.floor(z/PZ).astype(np.int64)
        ok=(i>=0)&(i<NX)&(j>=0)&(j<NY)&(k>=0)&(k<NZ)
        p=np.where(ok,k+NZ*(j+NY*i),0); c=np.where(ok,s.comp[p],-1); ok&=c>=0
        si=np.clip(((x-i*PX)/SX).astype(int),0,RX-1); sj=np.clip(((y-j*PY)/SY).astype(int),0,RY-1); sk=np.clip(((z-k*PZ)/SZ).astype(int),0,RZ-1)
        cell=np.where(ok,c*RX*RY*RZ+(si*RY+sj)*RZ+sk,0)
        return np.where(ok,s.TD[cell],0), np.where(ok,s.AU[cell],0), np.where(ok,s.MN[cell],0)
if __name__=='__main__':
    import sys
    if len(sys.argv)>1: build(sys.argv[2:],sys.argv[1])
    else: build()
