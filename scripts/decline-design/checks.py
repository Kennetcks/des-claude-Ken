import pickle,numpy as np
from scipy.spatial import cKDTree
d=pickle.load(open('design3.pkl','rb')); keep=d['keep']; ramp=d['ramp']; D=d['D']; ALL=np.vstack([D,ramp])
ka,ab=pickle.load(open('solid_pts.pkl','rb')); Tk=cKDTree(ka); Ta=cKDTree(ab)
def dens(P,step=1.0):
    out=[P[0]]
    for a,b in zip(P[:-1],P[1:]):
        n=max(1,int(np.linalg.norm(b-a)/step))
        for f in np.linspace(0,1,n+1)[1:]: out.append(a+(b-a)*f)
    return np.array(out)
def straight_len(P):
    d0=(P[1]-P[0])[:2]; d0/=np.linalg.norm(d0); L=0
    for a,b in zip(P[:-1],P[1:]):
        v=(b-a)[:2]; l=np.linalg.norm(v)
        if l<1e-6: continue
        if np.degrees(np.arccos(np.clip(v/l@d0,-1,1)))>1: break
        L+=l
    return L
def angle_to_parent(P,parent):
    q=P[0]; best=None
    for a,b in zip(parent[:-1],parent[1:]):
        ab=b-a; t=np.clip((q-a)@ab/(ab@ab),0,1); dd=np.linalg.norm(a+ab*t-q)
        if best is None or dd<best[0]: best=(dd,ab[:2]/np.linalg.norm(ab[:2]))
    v=(P[1]-P[0])[:2]; v/=np.linalg.norm(v); return np.degrees(np.arccos(abs(v@best[1])))
issues=[]
print('decline 3D min to keep-away %.1f m, to A/B stopes %.1f m'%(Tk.query(d['nd'])[0].min(),Ta.query(d['nd'])[0].min()))
print('ramp    3D min to keep-away %.1f m, to A/B stopes %.1f m'%(Tk.query(ramp)[0].min(),Ta.query(ramp)[0].min()))
print('ramp take-off angle to Kennet %.0f deg, straight before turning %.1f m'%(angle_to_parent(ramp,keep),straight_len(ramp)))
for t,lev,P,g in d['out']:
    if t=='ACC':
        par=ramp if np.min(np.linalg.norm(ramp-P[0],axis=1))<0.5 else D
        a=angle_to_parent(P,par); sl=straight_len(P)
        if a<85 or sl<10: issues.append(f'ACC {g}{lev}: take-off angle {a:.0f} deg, straight {sl:.1f} m')
# RAR vs ACC 3D clearance (ignore own RAD junction region)
rars=[(g,dens(P)) for t,lev,P,g in d['out'] if t=='RAR']
rads={(g,lev):P for t,lev,P,g in d['out'] if t=='RAD'}
for t,lev,P,g in d['out']:
    if t not in ('ACC','ISP','OD'): continue
    Q=dens(P)
    for gr,R_ in rars:
        dd=np.min(np.linalg.norm(Q[:,None,:]-R_[None,:,:],axis=2))
        if dd<10: issues.append(f'{t} {g}{lev} is {dd:.1f} m from RAR {gr}')
# development vs decline clashes
for t,lev,P,g in d['out']:
    if t=='DEC': continue
    Q=dens(P,2)
    for q in Q:
        if np.linalg.norm(q[:2]-P[0][:2])<14 and t in('ACC','ISP','RAD'): continue
        m=abs(ALL[:,2]-q[2])<6
        if m.any() and np.min(np.linalg.norm(ALL[m,:2]-q[:2],axis=1))<10: issues.append(f'{t} {g}{lev} within 10 m of decline at {q.round()}'); break
print('issues:',len(issues)); print('\n'.join(issues))
