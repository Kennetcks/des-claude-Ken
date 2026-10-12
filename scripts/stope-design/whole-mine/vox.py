import numpy as np
import design
ORIGIN=design.ORIGIN
def voxels_in_mesh(V,F):
    V=np.asarray(V)-ORIGIN; F=np.asarray(F)
    lo=np.floor(V.min(0)).astype(int); hi=np.ceil(V.max(0)).astype(int)
    gx,gy=np.meshgrid(np.arange(lo[0],hi[0])+0.5,np.arange(lo[1],hi[1])+0.5,indexing='ij'); gx=gx.ravel(); gy=gy.ravel()
    A,B,C=V[F[:,0]],V[F[:,1]],V[F[:,2]]
    zs=np.arange(lo[2],hi[2])+0.5; inside=np.zeros((len(gx),len(zs)),bool)
    for a,b,c in zip(A,B,C):
        den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
        if abs(den)<1e-12: continue
        w1=((b[1]-c[1])*(gx-c[0])+(c[0]-b[0])*(gy-c[1]))/den; w2=((c[1]-a[1])*(gx-c[0])+(a[0]-c[0])*(gy-c[1]))/den; w3=1-w1-w2
        hit=(w1>=0)&(w2>=0)&(w3>=0)
        if not hit.any(): continue
        zt=w1*a[2]+w2*b[2]+w3*c[2]
        inside[hit]^=(zs[None,:]<zt[hit,None])
    ii,kk=np.nonzero(inside)
    return (np.floor(gx[ii]).astype(np.int64)*2000+np.floor(gy[ii]).astype(np.int64))*2000+np.floor(zs[kk]).astype(np.int64)

def read_existing():
    out=[]
    for line in open('/mnt/user-data/uploads/work/stopes_mine/existing_stopes_mesh.txt',encoding='utf-8-sig'):
        nm,v,f=line.rstrip('\n').split('|'); v=np.array(v.split(','),float).reshape(-1,3); f=np.array(f.split(','),int).reshape(-1,5)
        tris=[]
        for q in f:
            a,b,c,dd=q[:4]-1
            tris.append((a,b,c))
            if dd!=a and dd!=c and dd>=0: tris.append((a,c,dd))
        out.append((nm,v,tris))
    return out
