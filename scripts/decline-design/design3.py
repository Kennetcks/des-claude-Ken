import pickle, numpy as np, collections
dec,_=pickle.load(open('ents.pkl','rb')); stp=pickle.load(open('stp_ab.pkl','rb'))
K=[e for e in dec if e['props'].get('Transformation Details')=='Kennet new'][0]
k=np.array([v[:3] for v in K['verts']]); sK=np.r_[0,np.cumsum(np.linalg.norm(np.diff(k,axis=0),axis=1))]
GR=7.0; R=22.0; STEP=1.5; ZEND=6046.0
i300=int(np.searchsorted(sK,300)); keep=k[:i300+1]
# ---------------- stopes
S=[]
for e in stp:
    p=np.array([v[:3] for v in e['verts']]); zb=p[:,2].min(); base=p[p[:,2]<zb+3]
    q=base[:,:2]-base[:,:2].mean(0); w,v=np.linalg.eigh(q.T@q); ax=v[:,-1]
    S.append(dict(c=base[:,:2].mean(0),zb=zb,grp=e['props']['Transformation Details'][-1],t=float(e['props']['Tonnes']),ax=ax,len=np.ptp(q@ax)))
tiers=[]
for g in 'AB':
    ss=sorted([s for s in S if s['grp']==g],key=lambda s:s['zb']); cur=[ss[0]]
    for s in ss[1:]:
        if s['zb']-cur[-1]['zb']>8: tiers.append((g,cur)); cur=[s]
        else: cur.append(s)
    tiers.append((g,cur))
def clusters(ss):
    n=len(ss); par=list(range(n))
    def f(a):
        while par[a]!=a: a=par[a]
        return a
    for a in range(n):
        for b in range(a+1,n):
            d=ss[b]['c']-ss[a]['c']; ax=ss[a]['ax']+(ss[b]['ax'] if ss[a]['ax']@ss[b]['ax']>0 else -ss[b]['ax']); ax/=np.linalg.norm(ax)
            if abs(d@ax)<70 and abs(d@np.array([-ax[1],ax[0]]))<18: par[f(a)]=f(b)
    g=collections.defaultdict(list)
    for a in range(n): g[f(a)].append(ss[a])
    return list(g.values())
def od_line(gs):
    P=np.array([s['c'] for s in gs]); Z=np.array([s['zb'] for s in gs])
    if len(gs)>1: q=P-P.mean(0); w,v=np.linalg.eigh(q.T@q); ax=v[:,-1]
    else: ax=gs[0]['ax']
    o=np.argsort(P@ax); P=P[o]; Z=Z[o]; L=[gs[j]['len'] for j in o]
    pts=np.vstack([P[0]-ax*(L[0]/2+3),P,P[-1]+ax*(L[-1]/2+3)]); zz=np.r_[Z[0],Z,Z[-1]]
    ki=[0]
    for j in range(1,len(pts)):
        if np.linalg.norm(pts[j]-pts[ki[-1]])>4: ki.append(j)
    return np.c_[pts[ki],zz[ki]]
LV=[]
for g,ts in tiers:
    zb=float(np.median([s['zb'] for s in ts]))
    LV.append(dict(grp=g,zb=zb,lev=int(round(zb-6000)),t=sum(s['t'] for s in ts),cl=clusters(ts)))
# ---------------- decline: racetrack loops in local frame, drift applied smoothly afterwards
p0=keep[-1]; h=keep[-1,:2]-keep[-2,:2]; h/=np.linalg.norm(h)
th0=np.arctan2(-h[0],h[1]); ce=p0[:2]-R*np.array([np.cos(th0),np.sin(th0)])
import json, os
ACCL=json.load(open('acc_len.json')) if os.path.exists('acc_len.json') else {}
tA=sorted([L['zb']-max(0.6,ACCL.get(str(L['lev']),100)/50) for L in LV if L['grp']=='A' and L['zb']<p0[2]-3],reverse=True)
# revolution plan: list of west-apex target z (None = free rev)
plan=[]; prev=None
for z in tA:
    if prev is not None:
        n=max(1,int(round((prev-z)/22.1)))
        for m in range(1,n): plan.append(prev-(prev-z)*m/n)
    plan.append(z); prev=z
pts=[np.array([*p0[:2]])]
def arc(c,a0,a1,n=None):
    n=max(2,int(abs(a1-a0)*R/STEP)+1)
    for a in np.linspace(a0,a1,n)[1:]: pts.append(c+R*np.array([np.cos(a),np.sin(a)]))
def line(q1):
    q0=pts[-1]; L=np.linalg.norm(q1-q0); n=max(1,int(np.ceil(L/STEP)))
    for f in np.linspace(0,1,n+1)[1:]: pts.append(q0+(q1-q0)*f)
def plen(): return float(np.sum(np.linalg.norm(np.diff(np.array(pts),axis=0),axis=1)))
a=th0; apex_idx=[]
for i,zt in enumerate(plan):
    arc(ce,a,np.pi/2)
    zc=p0[2]-plen()/GR
    s=float(np.clip(GR*(zc-zt)-R*np.pi/2,0,60))
    cw=ce-np.array([s,0]); line(cw+np.array([0,R])); arc(cw,np.pi/2,np.pi); apex_idx.append(len(pts)-1)
    arc(cw,np.pi,1.5*np.pi)
    zc=p0[2]-plen()/GR
    if i+1<len(plan):
        need=GR*(zc-plan[i+1])-1.5*np.pi*R
        apx=cw[0]-R
        dr=5.0 if plan[i+1]>6290 else float(os.environ.get('DRL','-10'))
        XMIN=float(os.environ.get('XMIN','8105'))
        if apx+dr<XMIN: dr=XMIN-apx
        b=float(np.clip((need+dr)/2,0,60))
    else:
        b=float(GR*max(0,zc-ZEND))+1
    ce=cw+np.array([b,0]); line(ce+np.array([0,-R])); a=-np.pi/2
    if i+1==len(plan): break
P=np.array(pts)
# smooth northward drift (tangent-continuous)
u=np.r_[0,np.cumsum(np.linalg.norm(np.diff(P,axis=0),axis=1))]
import os
DYT=float(os.environ.get('DYT','50'))
P=P+np.outer(u/u[-1],[0.0,DYT])
u=np.r_[0,np.cumsum(np.linalg.norm(np.diff(P,axis=0),axis=1))]
z=p0[2]-u/GR; nd=np.c_[P,z]
cut=int(np.argmax(nd[:,2]<=ZEND)) if (nd[:,2]<=ZEND).any() else len(nd)-1; nd=nd[:cut+1]
apex=[nd[i] for i in apex_idx if i<len(nd)]
D=np.vstack([keep,nd[1:]]); NK=len(keep)
pickle.dump(dict(keep=keep,nd=nd,D=D,NK=NK,LV=LV,apex=apex,plan=plan),open('d3a.pkl','wb'))
# smoothness check over full decline
d2=np.diff(D[:,:2],axis=0); hd=np.degrees(np.arctan2(d2[:,0],d2[:,1])); dh=np.abs((np.diff(hd)+180)%360-180)
seg=np.linalg.norm(d2,axis=1)
print('max heading change per vertex %.1f deg (at %s), min seg %.2f, max seg %.2f'%(dh.max(),D[np.argmax(dh)+1].round(1),seg.min(),seg.max()))
print('new decline length',round(u[min(cut,len(u)-1)]),'end z',nd[-1,2].round(1))
for p,t in zip(apex,plan): print('apex z %.1f target %.1f xy %s'%(p[2],t,p[:2].round()))
