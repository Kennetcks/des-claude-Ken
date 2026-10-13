import numpy as np
def cum(P): return np.r_[0,np.cumsum(np.hypot(*np.diff(P[:,:2],axis=0).T))]
def resample(P,step=1.0):
    c=cum(P); s=np.r_[np.arange(0,c[-1],step),c[-1]]
    Q=np.c_[np.interp(s,c,P[:,0]),np.interp(s,c,P[:,1]),np.interp(s,c,P[:,2])]
    return s,Q
def normals(Q):
    T=np.gradient(Q[:,:2],axis=0); T/=np.linalg.norm(T,axis=1)[:,None]+1e-12
    return T,np.c_[-T[:,1],T[:,0]]
def proj(P,p):
    """nearest point on polyline P (2D) to p: (dist, s, signed u, z, seg i, t)"""
    c=cum(P); best=None
    for i in range(len(P)-1):
        a=P[i,:2]; b=P[i+1,:2]; ab=b-a; L2=max(ab@ab,1e-12); t=np.clip((p-a)@ab/L2,0,1); q=a+t*ab
        dd=np.linalg.norm(p-q)
        if best is None or dd<best[0]:
            n=np.array([-ab[1],ab[0]])/np.sqrt(L2); best=(dd,c[i]+t*np.sqrt(L2),(p-q)@n,P[i,2]+t*(P[i+1,2]-P[i,2]),i,t)
    return best
def dp(P,tol=0.15,keep=()):
    """Douglas-Peucker in 3D, keeping indices in keep"""
    def rec(i,j):
        if j<=i+1: return [i]
        a=P[i];b=P[j];ab=b-a;L=np.linalg.norm(ab)
        k=np.arange(i+1,j); X=P[k]-a
        dd=np.linalg.norm(np.cross(X,ab),axis=1)/L if L>1e-9 else np.linalg.norm(X,axis=1)
        must=[x for x in k if x in keep]
        m=int(k[np.argmax(dd)]) if dd.max()>tol else (must[0] if must else None)
        if m is None: return [i]
        return rec(i,m)+rec(m,j)
    idx=rec(0,len(P)-1)+[len(P)-1]; return P[idx]

