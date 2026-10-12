import pickle, numpy as np
from scipy.spatial import ConvexHull, cKDTree
def surf_pts(p,step=1.0):
    h=ConvexHull(p); out=[]
    for s in h.simplices:
        a,b,c=p[s]; n=int(max(np.linalg.norm(b-a),np.linalg.norm(c-a),np.linalg.norm(c-b))/step)+1
        for i in range(n+1):
            for j in range(n+1-i):
                out.append(a+(b-a)*i/n+(c-a)*j/n)
    return np.array(out)
def build(ents):
    P=[surf_pts(np.array([v[:3] for v in e['verts']])) for e in ents]
    return cKDTree(np.vstack(P))
if __name__=='__main__':
    ka=pickle.load(open('keepaway.pkl','rb')); ab=pickle.load(open('stp_ab.pkl','rb'))
    pickle.dump((build(ka).data,build(ab).data),open('solid_pts.pkl','wb'))
    d=pickle.load(open('design3.pkl','rb'))
    Tk=cKDTree(pickle.load(open('solid_pts.pkl','rb'))[0]); Ta=cKDTree(pickle.load(open('solid_pts.pkl','rb'))[1])
    for nm,P in (('decline',d['nd']),('ramp',d['ramp'])):
        dk,ik=Tk.query(P); da,ia=Ta.query(P)
        print(nm,'min to keep-away %.1f at %s'%(dk.min(),P[np.argmin(dk)].round()),' min to A/B stopes %.1f at %s'%(da.min(),P[np.argmin(da)].round()))
