import pickle,numpy as np, itertools
from scipy.spatial import cKDTree

d=pickle.load(open('design3.pkl','rb')); D=d['D']; ramp=d['ramp']; ALL=np.vstack([D,ramp])
def dens(P,step=1.0):
    out=[P[0]]
    for a,b in zip(P[:-1],P[1:]):
        n=max(1,int(np.linalg.norm(b-a)/step))
        for f in np.linspace(0,1,n+1)[1:]: out.append(a+(b-a)*f)
    return np.array(out)
out=[o for o in d['out'] if o[0] not in ('RAD','RAR','ISP')]
devs={}
for t,lev,P,g in out:
    if t in('ACC','OD'): devs.setdefault((g,lev),[]).append(dens(P))
allpts=np.vstack([np.vstack(v) for v in devs.values()])
lvl={g:[x for x in d['info'] if x['grp']==g and x['src']=='D' and x['zj']<6350] for g in 'AB'}
newout=list(out); report={}
for g,L in lvl.items():
    L=sorted(L,key=lambda x:-x['zj'])
    best=None
    for side in (1,-1):
        base=[]
        for x in L:
            C=x['J']+np.r_[x['n']*20,0]; base.append((x,C,C[:2]+side*x['rside']*16))
        E=np.array([b[2] for b in base]); Z=np.array([b[1][2] for b in base])
        fx=np.polyfit(Z,E[:,0],1); fy=np.polyfit(Z,E[:,1],1)
        z0,z1=Z.max(),Z.min()
        mask=np.ones(len(allpts),bool)
        for x,C,_ in base: mask&=np.linalg.norm(allpts-C,axis=1)>14
        TD=cKDTree(allpts[mask]); TA=cKDTree(ALL)
        grid=np.arange(-12,13,4)
        for dx0,dy0,dx1,dy1 in itertools.product(grid,grid,grid,grid):
            p0=np.array([np.polyval(fx,z0)+dx0,np.polyval(fy,z0)+dy0]); p1=np.array([np.polyval(fx,z1)+dx1,np.polyval(fy,z1)+dy1])
            pos=lambda z: p0+(p1-p0)*(z0-z)/(z0-z1)
            rl=[np.linalg.norm(pos(C[2])-C[:2]) for x,C,_ in base]
            if min(rl)<8 or max(rl)>32: continue
            rar=np.array([np.r_[pos(C[2]),C[2]+0.3] for x,C,_ in base]); R_=dens(rar,2)
            # clearance to all ACC/OD except own level near RAD start
            mind=99
            for (x,C,_) in base:
                pass
            cl=TD.query(R_)[0].min(); cd=TA.query(R_)[0].min()
            score=min(cl,cd)
            if best is None or (score,-sum(rl))>(best[0],-best[1]): best=(score,sum(rl),side,rar,base,pos)
    score,_,side,rar,base,pos=best
    for (x,C,_),q in zip(base,rar):
        newout.append(('RAD',x['lev'],np.array([C,q]),g))
    newout.append(('RAR',None,rar,g)); report[g]=(round(score,1),side,[round(np.linalg.norm(q[:2]-C[:2])) for (x,C,_),q in zip(base,rar)])
    # ISPs on the opposite side of the RAD, 12 m along the access
    sd={x['lev']:-side for x in L}
for x in d['info']:
    g=x['grp']; side=report.get(g,(0,1))[1] if x['src']=='D' else 1
    J=x['J']; B_=J+np.r_[x['n']*12,0]; sd=-side*x['rside']
    if x['src']=='R':
        near=ALL[abs(ALL[:,2]-J[2])<6]
        sd=x['rside'] if np.min(np.linalg.norm(near[:,:2]-(B_[:2]+x['rside']*15),axis=1))>np.min(np.linalg.norm(near[:,:2]-(B_[:2]-x['rside']*15),axis=1)) else -x['rside']
        B_=J+np.r_[x['n']*24,0]
    newout.append(('ISP',x['lev'],np.array([B_,np.r_[B_[:2]+sd*15,B_[2]+0.3]]),g))
d['out']=newout; pickle.dump(d,open('design3.pkl','wb'))
for g,r in report.items(): print(g,'min clearance RAR to accesses/ODs/decline %.1f m, side %d, RAD lengths %s'%(r[0],r[1],r[2]))
