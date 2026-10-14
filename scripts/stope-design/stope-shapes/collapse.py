import numpy as np, trimesh
def _ok(V,F):
    m=trimesh.Trimesh(V,F,process=False)
    return m.is_watertight and m.is_winding_consistent
def collapse(t,tol=0.03,atol=0.002):
    V=np.array(t.vertices,float); F=np.array(t.faces)
    # merge exact duplicates first
    m=trimesh.Trimesh(V,F,process=True); V=np.array(m.vertices); F=np.array(m.faces)
    for it in range(500):
        E=np.vstack([F[:,[0,1]],F[:,[1,2]],F[:,[2,0]]]); L=np.linalg.norm(V[E[:,0]]-V[E[:,1]],axis=1)
        order=np.argsort(L); done=False
        for k in order:
            if L[k]>=tol: break
            a,b=E[k]
            for keep,drop,pos in ((a,b,(V[a]+V[b])/2),(a,b,V[a]),(b,a,V[b])):
                F2=F.copy(); F2[F2==drop]=keep
                good=(F2[:,0]!=F2[:,1])&(F2[:,1]!=F2[:,2])&(F2[:,0]!=F2[:,2]); F2=F2[good]
                V2=V.copy(); V2[keep]=pos
                if _ok(V2,F2):
                    V,F=V2,F2; done=True; break
            if done: break
        if not done: break
    # needle triangles: flip their longest edge if that improves
    for it in range(500):
        m=trimesh.Trimesh(V,F,process=False); A=m.area_faces
        idx=np.where(A<atol)[0]
        if len(idx)==0: break
        changed=False
        for f in idx:
            tri=F[f]; el=[np.linalg.norm(V[tri[i]]-V[tri[(i+1)%3]]) for i in range(3)]; i=int(np.argmax(el))
            a,b,c=tri[i],tri[(i+1)%3],tri[(i+2)%3]
            # neighbour face with edge (b,a)
            nb=[g for g in range(len(F)) if g!=f and any(F[g][j]==b and F[g][(j+1)%3]==a for j in range(3))]
            if len(nb)!=1: continue
            g=nb[0]; d=[x for x in F[g] if x not in (a,b)][0]
            F2=F.copy(); F2[f]=[c,a,d]; F2[g]=[c,d,b]
            if _ok(V,F2) and trimesh.Trimesh(V,F2,process=False).area_faces[[f,g]].min()>A[f]:
                F=F2; changed=True; break
        if not changed: break
    m=trimesh.Trimesh(V,F,process=False); m.remove_unreferenced_vertices()
    return m
