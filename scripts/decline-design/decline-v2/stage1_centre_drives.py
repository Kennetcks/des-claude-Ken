"""Kennet 2026-10-13: fix his decline/level design (DESIGN\\DECLINE, copied to DESIGN\\DECLINE V2):
centre the ore drives under the STOPE FINAL V3 stopes, extend them where stopes run past the end,
and add access to stopes with no ore drive. Always reuse/extend his strings; rules from decline-and-level-design."""
import pickle, json, numpy as np, pandas as pd, trimesh, re
W='/home/claude/kd/'
geo=pickle.load(open('/home/claude/stp/final_v3/stope_geoms.pkl','rb'))
keep=set(pd.read_csv('/home/claude/kd/v3_live.csv').name)   # stopes on STOPE FINAL V3 now (Kennet deleted 12)
geo=[g for g in geo if g['name'] in keep]
A=pd.read_csv(W+'kd_attrs.csv').set_index('handle'); SS=pd.read_csv(W+'kd_strings.csv')
S={}
for h,g in SS.groupby('handle',sort=False):
    S[h]=dict(t=A.loc[h,'ACT_TYPE'],lev=A.loc[h,'LEVEL'],lode=A.loc[h,'LODE'],P=g.sort_values('vi')[['x','y','z']].values.astype(float),new=False,changed=False)
DROP=['3CA4CFA']                     # straight 160 m sketch climbing 31 m (Kennet ok'd redoing it)
for h in DROP: S.pop(h)
# attributes for the strings he left blank
S['3CA4CF2'].update(lev=347.0,lode='GROUP A'); S['3CA4CF8'].update(lev=397.0,lode='GROUP A')
DEC=np.vstack([S['3CA4C7C']['P'],S['3CA4C7D']['P'][1:]])

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

# ---- stope floor centrelines (V3) ----
ST=[]
for g in geo:
    V=g['V']; z0=V[:,2].min(); fl=V[V[:,2]<z0+1.0][:,:2]; c=fl.mean(0)
    u,sv,vt=np.linalg.svd(fl-c); ax=vt[0]
    s_=(fl-c)@ax; nr=np.array([-ax[1],ax[0]]); n_=(fl-c)@nr; pts=[]
    for x in np.arange(s_.min()+0.5,s_.max()-0.49,1.0):
        m=np.abs(s_-x)<1.5
        if m.sum()>=2: pts.append(c+x*ax+0.5*(n_[m].min()+n_[m].max())*nr)
    C=np.array(pts)
    if len(C)>=5:  # smooth the centreline a little
        C=np.c_[np.convolve(np.pad(C[:,0],2,mode='edge'),np.ones(5)/5,'valid'),np.convolve(np.pad(C[:,1],2,mode='edge'),np.ones(5)/5,'valid')]
    m=re.match(r'ST(\d+) (.+) S(\d+)$',g['name'])
    ST.append(dict(name=g['name'],lev=int(m.group(1)),drive=m.group(2),z0=z0,C=C,t=g['t'],grp=g['group'],
                   mesh=trimesh.Trimesh(V,np.asarray(g['F'])-(1 if np.asarray(g['F']).min()==1 else 0),process=False),bb=(V.min(0),V.max(0))))
print('stopes',len(ST))
ODS=[h for h,v in S.items() if v['t']=='OD']
# ---- assign each stope to the ore drive it sits on ----
def match(st,P):
    pr=[proj(P,p) for p in st['C']]
    dh=np.array([x[0] for x in pr]); dz=np.median([abs(x[3]-st['z0']) for x in pr])
    return np.median(dh),dz,(dh<3).mean()
asg={}; missed=[]
for st in ST:
    best=None
    for h in ODS:
        dm,dz,cov=match(st,S[h]['P'])
        if dz<3 and (best is None or dm<best[0]): best=(dm,h,cov)
    if best is not None and best[0]<10: asg.setdefault(best[1],[]).append(st); st['od']=best[1]; st['off0']=best[0]
    else: missed.append(st); st['od']=None
print('assigned',sum(len(v) for v in asg.values()),'missed',len(missed))

# ---- original junctions: endpoint e of string h lies on string h2 ----
REL=[]
for h,v in S.items():
    for e in (0,-1):
        p=v['P'][e]
        for h2,v2 in S.items():
            if h2==h: continue
            dd,sc,u,z,i,t=proj(v2['P'],p[:2])
            if dd<1.5 and abs(p[2]-z)<2.0: REL.append((h,e,h2))
ENDS_ON={(h,e) for h,e,h2 in REL}
# ---- endpoints of other strings touching each OD (junctions move with the drive) ----
def ends(h): P=S[h]['P']; return [(h,0,P[0]),(h,-1,P[-1])]
# ---- recentre / extend ----
report=[]
for h,sts in asg.items():
    P=S[h]['P']; s,Q=resample(P,1.0); T,N=normals(Q); L=s[-1]
    tgt=np.full(len(s),np.nan); acc=[[] for _ in s]; ext0=[]; ext1=[]
    for st in sts:
        for p in st['C']:
            dd,sc,u,z,i,t=proj(P,p)
            a=P[0,:2]; b=P[1,:2]; ta=(b-a)/np.linalg.norm(b-a)
            a1=P[-1,:2]; b1=P[-2,:2]; tb=(a1-b1)/np.linalg.norm(a1-b1)
            if sc<0.01 and (p-a)@ta<-0.5: ext0.append(((p-a)@ta,p)); continue
            if sc>L-0.01 and (p-a1)@tb>0.5: ext1.append(((p-a1)@tb,p)); continue
            k=int(round(sc)); k=min(max(k,0),len(s)-1)
            # signed offset in the station normal frame
            acc[k].append((p-Q[k,:2])@N[k])
    for k in range(len(s)):
        if acc[k]: tgt[k]=np.mean(acc[k])
    cov=~np.isnan(tgt)
    if cov.any():
        # away from stopes the drive stays where Kennet drew it: anchor 0 beyond 8 m from covered stations
        ci=np.nonzero(cov)[0]; far=np.array([np.min(np.abs(ci-k))>8 for k in range(len(s))])
        kk=np.arange(len(s)); anc=cov|far; vals=np.where(cov,tgt,0.0)
        off=np.interp(kk,kk[anc],vals[anc])
        off=np.convolve(np.pad(off,3,mode='edge'),np.ones(7)/7,'valid')
        for _ in range(200):           # turns gently: <= 0.4 m sideways per 1 m (2 m per 5 m)
            ch=False
            for i in list(range(1,len(off)))+list(range(len(off)-2,-1,-1)):
                j=i-1 if i>0 else i+1
                if abs(off[i]-off[j])>0.4+1e-9: off[i]=off[j]+np.sign(off[i]-off[j])*0.4; ch=True
            if not ch: break
        off[np.abs(off)<0.3]=0
    else: off=np.zeros(len(s))
    Q2=Q.copy(); Q2[:,:2]+=off[:,None]*N
    # junctions: other strings' endpoints that touched this drive move with it
    moved_j=[]
    for h2,v2 in S.items():
        if h2==h or v2['t'] in('DEC','RAR'): continue
        for e in (0,-1):
            p=v2['P'][e]
            dd,sc,u,z,i,t=proj(P,p[:2])
            if dd<1.0 and abs(p[2]-z)<1.5:
                k=int(round(sc)); k=min(max(k,0),len(s)-1)
                if np.linalg.norm(Q2[k,:2]-Q[k,:2])>0.05:
                    v2['P'][e,:2]=Q2[k,:2]; v2['changed']=True; moved_j.append((h2,e))
    # extensions along the stope centrelines past the drive ends (flat, at the drive's RL)
    pre=[];post=[]
    if (h,0) in ENDS_ON: ext0=[]       # this end sits on another drive: don't push through it
    if (h,-1) in ENDS_ON: ext1=[]
    if ext0:
        ext0.sort(key=lambda x:x[0]); pre=[np.r_[p,Q2[0,2]] for _,p in ext0[::-1]]
    if ext1:
        ext1.sort(key=lambda x:x[0]); post=[np.r_[p,Q2[-1,2]] for _,p in ext1]
    newP=np.vstack([np.array(pre).reshape(-1,3),Q2,np.array(post).reshape(-1,3)])
    newP=dp(newP,0.15)
    S[h]['P']=newP; S[h]['changed']=bool(np.abs(off).max()>0 or pre or post)
    report.append(dict(handle=h,level=S[h]['lev'],stopes=len(sts),max_move=round(float(np.abs(off).max()),1),
                       mean_move=round(float(np.abs(off[cov]).mean()) if cov.any() else 0,1),
                       ext_start=round(ext0[-1][0]*-1,1) if ext0 else 0,ext_end=round(ext1[-1][0],1) if ext1 else 0,junctions_moved=len(moved_j)))
# re-snap every original junction onto the moved geometry
snapped=0
for h,e,h2 in REL:
    if h not in S or h2 not in S: continue
    p=S[h]['P'][e]; dd,sc,u,z,i,t=proj(S[h2]['P'],p[:2])
    if dd>0.05:
        a=S[h2]['P'][i]; b=S[h2]['P'][i+1]; q=a+t*(b-a); S[h]['P'][e]=[q[0],q[1],p[2]]; S[h]['changed']=True; snapped+=1
print('junctions re-snapped',snapped,'of',len(REL))
rep=pd.DataFrame(report).sort_values('level',ascending=False); print(rep.to_string())
pickle.dump(dict(S=S,ST=ST,missed=[m['name'] for m in missed],rep=rep),open(W+'stage1.pkl','wb'))
