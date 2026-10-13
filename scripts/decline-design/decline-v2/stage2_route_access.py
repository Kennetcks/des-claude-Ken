"""Stage 2: connect stopes with no ore drive (and level networks with no access) using Kennet's existing strings first."""
import pickle, numpy as np, pandas as pd
from collections import defaultdict
import sys; sys.path.insert(0,'/home/claude/kd'); from geomlib import cum,resample,normals,proj,dp
W='/home/claude/kd/'
D=pickle.load(open(W+'stage1.pkl','rb')); S=D['S']; ST=D['ST']; STn={s['name']:s for s in ST}
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

# ---- level networks: which OD strings are not connected to the decline? ----
def touches(P1,P2,tol=1.5):
    for e in (P1[0],P1[-1]):
        d,s,u,z,i,t=proj(P2,e[:2])
        if d<tol and abs(e[2]-z)<2.0: return True
    return False
hs=list(S); adj=defaultdict(set)
for a in hs:
    for b in hs:
        if a!=b and (touches(S[a]['P'],S[b]['P']) or touches(S[b]['P'],S[a]['P'])): adj[a].add(b)
decs=[h for h in hs if S[h]['t']=='DEC']; seen=set(decs); stack=list(decs)
while stack:
    x=stack.pop()
    for y in adj[x]:
        if y not in seen: seen.add(y); stack.append(y)
orphan=[h for h in hs if h not in seen]
print('not connected to the decline:',[(h,S[h]['t'],S[h]['lev']) for h in orphan])

# ---- tasks: missed stope groups + orphan OD networks ----
G=defaultdict(list)
for n in D['missed']:
    st=STn[n]; G[(int(round(st['z0'])),st['drive'])].append(st)
tasks=[]
for k,v in sorted(G.items()): tasks.append(dict(kind='stopes',key='%s %s'%k,stopes=v,P=path_of(v),z=min(s['z0'] for s in v)))
# orphan networks: connect from the endpoint nearest the decline
comp=[]; left=set(h for h in orphan if S[h]['t']!='RAR')
while left:
    x=left.pop(); c={x}; st=[x]
    while st:
        y=st.pop()
        for z in adj[y]:
            if z in left: left.remove(z); c.add(z); st.append(z)
    comp.append(sorted(c))
for c in comp:
    if not any(st.get('od') in c for st in ST): print('orphan without stopes, left as drawn:',c); continue
    ends=[(h,e,S[h]['P'][e]) for h in c for e in (0,-1)]
    tasks.append(dict(kind='orphan',key='orphan '+'/'.join(c),members=c,ends=ends,z=float(np.mean([S[h]['P'][:,2].mean() for h in c]))))

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
pickle.dump(dict(S=S,new=new,log=log,tasks=[{k:v for k,v in t.items() if k!='stopes'} for t in tasks]),open(W+'stage2.pkl','wb'))
