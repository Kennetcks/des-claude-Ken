"""DECLINE V3 (Kennet 2026-10-13): DECLINE V2 as approved + ore drives and accesses for the 20 STOPE FINAL V4 vein stopes.
Each vein stope group gets an ore drive on its floor centreline (1 m past the stope ends), connected to the V2 network with the
stage-2 router (same level OD/ACC first, decline +40 m penalty, 10 m / 7.5 m take-off, <= 1:33, clear of the decline and stopes)."""
import pickle, numpy as np, pandas as pd, csv, re, trimesh
from collections import defaultdict
import sys; sys.path.insert(0,'/home/claude/kd'); from geomlib import cum,resample,normals,proj,dp
W='/home/claude/kd/'
D4=pickle.load(open(W+'stage4.pkl','rb')); S={h:v for h,v in D4['S'].items() if h not in D4.get('removed',[])}
for i,n in enumerate(D4['new']): S['N%03d'%(i+1)]=dict(t=n['t'],lev=n['lev'],lode=n['lode'],P=n['P'],new=True,changed=True)
def strec(name,V,F,group):
    z0=V[:,2].min(); fl=V[V[:,2]<z0+1.0][:,:2]; c=fl.mean(0)
    u,sv,vt=np.linalg.svd(fl-c); ax=vt[0]
    s_=(fl-c)@ax; nr=np.array([-ax[1],ax[0]]); n_=(fl-c)@nr; pts=[]
    for x in np.arange(s_.min()+0.5,s_.max()-0.49,1.0):
        m=np.abs(s_-x)<1.5
        if m.sum()>=2: pts.append(c+x*ax+0.5*(n_[m].min()+n_[m].max())*nr)
    C=np.array(pts); m=re.match(r'ST(\d+) (.+) S(\d+)$',name)
    F=np.asarray(F); F=F-(1 if F.min()==1 else 0)
    return dict(name=name,lev=int(m.group(1)),drive=m.group(2),z0=z0,C=C,grp=group,mesh=trimesh.Trimesh(V,F,process=False),bb=(V.min(0),V.max(0)),
                s_rng=(s_.min(),s_.max()),ax=ax,c=c)
live={r['name']:r['GROUP'] for r in csv.DictReader(open(W+'v3_live_groups.csv'))}
ST=[strec(g['name'],np.asarray(g['V'],float),g['F'],live[g['name']]) for g in pickle.load(open('/home/claude/stp/final_v3/stope_geoms_v3.pkl','rb')) if g['name'] in live]
keepv=set(pd.read_csv('/mnt/project-files/deswik-missed-stopes/stope_final_v4_vein_stopes.csv').name)
VEIN=[strec(g['name'],np.asarray(g['V'],float),g['F'],'') for g in pickle.load(open('/home/claude/work/st_final_v4/stope_geoms.pkl','rb')) if g['name'] in keepv]
ST=ST+VEIN; STn={s['name']:s for s in ST}
print('network strings',len(S),'stopes',len(ST),'vein stopes',len(VEIN))
DECP=np.vstack([S['3CA4C7C']['P'],S['3CA4C7D']['P'][1:]]); sD,DQ=resample(DECP,1.0)
TD,ND=normals(DQ)
LOOPC=None
def dec_conflict(Q,skip=0):
    """sample points vs decline passes: horizontal < 10 m and vertical < 10 m (skip the first `skip` m = take-off)"""
    s,_=resample(Q,1.0) if len(Q)>1 else (np.array([0]),Q)
    _,R=resample(Q,1.0)
    for k,p in enumerate(R):
        if k<skip: continue
        m=np.abs(DQ[:,2]-p[2])<10
        if m.any() and np.min(np.hypot(*(DQ[m,:2]-p[:2]).T))<10: return True
    return False
def stope_conflict(Q,exclude=(),lat=3.0,free=0.0):
    _,R=resample(Q,2.0)
    T,N=normals(R) if len(R)>2 else (None,np.tile([[0,1]],(len(R),1)))
    m=np.hypot(*(R[:,:2]-Q[0,:2]).T)>=free; R=R[m]; N=N[m]
    if len(R)<1: return None
    pts=[]
    for p,n in zip(R,N):
        for u in (-lat,0,lat):
            for dz in (0.5,3.0,5.5): pts.append([p[0]+u*n[0],p[1]+u*n[1],p[2]+dz])
    pts=np.array(pts)
    for st in ST:
        if st['name'] in exclude: continue
        lo,hi=st['bb']; m=np.all((pts>=lo-0.1)&(pts<=hi+0.1),axis=1)
        if m.any() and st['mesh'].contains(pts[m]).any(): return st['name']
    return None
def path_of(stopes):
    """one ore drive along the stopes' floor centrelines, ordered along strike, at the lowest floor RL"""
    C=np.vstack([s['C'] for s in stopes]); c=C.mean(0); u,sv,vt=np.linalg.svd(C-c); ax=vt[0]
    pts=sorted([p for p in C],key=lambda p:(p-c)@ax); z=min(s['z0'] for s in stopes)
    P=np.array([[p[0],p[1],z] for p in pts]); return dp(P,0.15)

def connector(A,tA,E,t_kind,skip_dec):
    """from attach point A (with tangent tA of the string there) to path end E: 10 m perpendicular take-off then straight"""
    v=E[:2]-A[:2]; n=np.array([-tA[1],tA[0]]); n=n if v@n>0 else -n
    L0=10.0 if t_kind=='DEC' else 7.5
    B=A[:2]+L0*n
    L=L0+np.linalg.norm(E[:2]-B); 
    if np.linalg.norm(E[:2]-B)<3: B=A[:2]+(E[:2]-A[:2])*0.5; L=np.linalg.norm(E[:2]-A[:2])
    zB=A[2]+(E[2]-A[2])*L0/L
    return np.array([A,[B[0],B[1],zB],E]),L


def path_of(stopes):
    """ore drive on the vein stopes' floor centreline, ends 1 m past the stope solids (Kennet's rule), at the lowest floor RL"""
    C=np.vstack([s['C'] for s in stopes]); c=C.mean(0); u,sv,vt=np.linalg.svd(C-c); ax=vt[0]
    allfl=np.vstack([s['mesh'].vertices[s['mesh'].vertices[:,2]<s['z0']+1.0][:,:2] for s in stopes]); sp=(allfl-c)@ax
    pts=sorted([p for p in C],key=lambda p:(p-c)@ax); z=min(s['z0'] for s in stopes)
    P=[c+(sp.min()-1.0)*ax]+pts+[c+(sp.max()+1.0)*ax]
    P=np.array([[p[0],p[1],z] for p in P]); P=dp(P,0.5)
    return P
G=defaultdict(list)
for st in VEIN: G[(int(round(st['z0'])),st['drive'])].append(st)
tasks=[dict(kind='stopes',key='%s %s'%k,stopes=v,P=path_of(v),z=min(s['z0'] for s in v)) for k,v in sorted(G.items())]
new=[]; log=[]
import heapq
def lev_of(z): return int(round(z-6000))
def polylen(Q): return float(np.hypot(*np.diff(Q[:,:2],axis=0).T).sum())
def build(A,tA,kind,E,W=None):
    """A -> perpendicular take-off -> (waypoint) -> E, z linear in length"""
    tgt=(W if W is not None else E[:2])-A[:2]; n=np.array([-tA[1],tA[0]]); n=n if tgt@n>0 else -n
    L0=10.0 if kind=='DEC' else 7.5
    xy=[A[:2],A[:2]+L0*n]+([W] if W is not None else [])+[E[:2]]
    xy=np.array(xy); c=np.r_[0,np.cumsum(np.hypot(*np.diff(xy,axis=0).T))]
    z=A[2]+(E[2]-A[2])*c/c[-1]
    return np.c_[xy,z],c[-1]
def turn_ok(Q):
    for i in range(1,len(Q)-1):
        u=Q[i,:2]-Q[i-1,:2]; v=Q[i+1,:2]-Q[i,:2]
        if np.linalg.norm(u)<1e-6 or np.linalg.norm(v)<1e-6: return False
        if u@v/np.linalg.norm(u)/np.linalg.norm(v)<np.cos(np.radians(100)): return False
    return True
for T in tasks:
    z0=T['z']; best=None
    if T['kind']=='stopes':
        P=T['P']; Es=[P[0],P[-1]]; excl=[s['name'] for s in T['stopes']]
    else:
        Es=[e[2] for e in T['ends']]; excl=[st['name'] for st in ST if st.get('od') in T['members']]
    cands=[]
    for h,v in S.items():
        if v['t'] not in('OD','ACC') or (T['kind']=='orphan' and h in T['members']): continue
        if abs(np.median(v['P'][:,2])-z0)>3: continue
        s_,Q=resample(v['P'],2.0); Tt,_=normals(Q)
        for q,t in zip(Q,Tt): cands.append((h,v['t'],q,t))
    for i in range(0,len(DQ),2):
        if -4.5<=z0-DQ[i,2]<=0.5: cands.append(('DEC','DEC',DQ[i],TD[i]))
    # candidate routes in order of (penalised) length; first feasible wins
    heap=[]
    allE=np.array([e[:2] for e in Es]); lo=np.minimum(allE.min(0),np.array([c[2][:2] for c in cands]).min(0))-60; hi=np.maximum(allE.max(0),np.array([c[2][:2] for c in cands]).max(0))+60
    gx=np.arange(lo[0],hi[0],8.0); gy=np.arange(lo[1],hi[1],8.0); WP=np.array([[x,y] for x in gx for y in gy])
    cnt=0
    for ci,(h,kind,A,tA) in enumerate(cands):
        for ei,E in enumerate(Es):
            pen=40 if kind=='DEC' else 0
            d0=np.linalg.norm(E[:2]-A[:2])
            heapq.heappush(heap,(d0+pen,cnt,ci,ei,-1)); cnt+=1
            dw=np.linalg.norm(WP-A[:2],axis=1)+np.linalg.norm(WP-E[:2],axis=1)
            for wi in np.nonzero(dw<d0*1.8+40)[0]:
                heapq.heappush(heap,(dw[wi]+pen+5,cnt,ci,ei,int(wi))); cnt+=1
    tries=0
    while heap and tries<6000:
        lb,_,ci,ei,wi=heapq.heappop(heap); tries+=1
        h,kind,A,tA=cands[ci]; E=Es[ei]
        Cn,L=build(A,tA,kind,E,None if wi<0 else WP[wi])
        if L<2 or not turn_ok(Cn): continue
        pen=L+(40 if kind=='DEC' else 0)+(5 if wi>=0 else 0)
        if best is not None and pen>=best[0]: continue
        if heap and pen>heap[0][0]+1e-6:   # true length larger than the bound: requeue
            heapq.heappush(heap,(pen,cnt,ci,ei,wi)); cnt+=1; continue
        gr=(E[2]-A[2])/L
        if kind=='DEC' and not (0.0<=gr<=0.03): continue
        if kind!='DEC' and abs(gr)>0.03: continue
        if dec_conflict(Cn,skip=12 if kind=='DEC' else 0): continue
        if stope_conflict(Cn,exclude=excl,free=0 if kind=='DEC' else 7.0): continue
        best=(pen,h,kind,Cn,L,ei); break
    if best is None:
        log.append((T['key'],'NO FEASIBLE ROUTE')); print(T['key'],'NO FEASIBLE ROUTE'); continue
    pen,h,kind,Cn,L,ei=best
    lev=lev_of(z0) if kind=='DEC' else int(S[h]['lev']) if h in S and not pd.isna(S[h]['lev']) else lev_of(z0)
    if T['kind']=='orphan': lev=int(S[T['members'][0]]['lev'])
    lode=S[h]['lode'] if h in S else (S[T['members'][0]]['lode'] if T['kind']=='orphan' else 'GROUP A')
    if T['kind']=='stopes': lode={'B':'GROUP B'}.get(T['stopes'][0]['grp'],lode) if False else lode
    acc_type='ACC'
    new.append(dict(t=acc_type,lev=lev,lode=lode,P=Cn,why='access to '+T['key'],attach=h))
    if T['kind']=='stopes':
        P=T['P'] if ei==0 else T['P'][::-1]
        P=P.copy(); P[:,2]=Cn[-1,2]
        new.append(dict(t='OD',lev=lev,lode=lode,P=P,why='ore drive '+T['key'],attach=None))
    if kind=='DEC':   # new level: stockpile 15 m off the access, ~20 m from the decline, +1:50
        s,Q=resample(Cn,1.0); k=min(20,len(Q)-2); t=(Q[k+1,:2]-Q[k-1,:2]); t/=np.linalg.norm(t); n=np.array([-t[1],t[0]])
        for sg in (1,-1):
            Isp=np.array([Q[k],[Q[k,0]+sg*15*n[0],Q[k,1]+sg*15*n[1],Q[k,2]+0.3]])
            if not dec_conflict(Isp,skip=0) and not stope_conflict(Isp): break
        new.append(dict(t='ISP',lev=lev,lode=lode,P=Isp,why='stockpile '+T['key'],attach=None))
    log.append((T['key'],kind,h,round(L,1)))
    print(T['key'],'<-',kind,h,'L=%.0f'%L)
pickle.dump(dict(S=S,new=new,log=log),open(W+'v3dec.pkl','wb'))
