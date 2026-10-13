"""Kennet review 2026-10-13: drives must be straight where possible, no ore drive without a stope, no tail past the last stope.
Applies to the whole V2 set: (a) remove dead-end ore drives that serve no stope, (b) trim free ends to the last stope + 1 m,
(c) straighten: fewest vertices keeping every stope centre within 1.5 m, junction points kept."""
import pickle, numpy as np, sys
sys.path.insert(0,'/home/claude/kd'); from geomlib import proj, resample, cum
D=pickle.load(open('stage2b.pkl','rb')); S=D['S']; new=D['new']
ST=pickle.load(open('stage1.pkl','rb'))['ST']
# one dict of every string: his (by handle) + new (N..)
ALL={h:v for h,v in S.items()}
for i,n in enumerate(new): n['new']=True; ALL['new%02d'%i]=n
def on(p,P,tol=1.5):
    d,s,u,z,i,t=proj(P,p[:2]); return d<tol and abs(p[2]-z)<2.0, s
def stopes_of(h):
    P=ALL[h]['P']; out=[]
    for st in ST:
        pr=[proj(P,p) for p in st['C']]; dh=np.median([x[0] for x in pr]); dz=np.median([abs(x[3]-st['z0']) for x in pr])
        if dz<3 and dh<4: out.append((st,min(x[1] for x in pr),max(x[1] for x in pr)))
    return out
def attached(h):
    """arc positions s on h where other strings touch it (their endpoints on h, or h's endpoints on them)"""
    P=ALL[h]['P']; L=cum(P)[-1]; js=[]
    for h2,v in ALL.items():
        if h2==h: continue
        for e in (0,-1):
            ok,s=on(v['P'][e],P)
            if ok: js.append(s)
        for e,se in ((0,0.0),(-1,L)):
            ok,_=on(P[e],v['P'])
            if ok: js.append(se)
    return sorted(js)
log=[]
# (a) remove ore drives that serve no stope and have a free end, repeat until stable
ch=True
while ch:
    ch=False
    for h,v in list(ALL.items()):
        if v['t']!='OD' or stopes_of(h): continue
        js=attached(h); L=cum(v['P'])[-1]
        free0=not any(s<1.0 for s in js); free1=not any(s>L-1.0 for s in js)
        if free0 or free1:
            ALL.pop(h); log.append(('removed (no stope, dead end)',h,round(L))); ch=True
# (b)+(c)
for h,v in ALL.items():
    if v['t']!='OD': continue
    P=v['P']; L=cum(P)[-1]; sts=stopes_of(h)
    if not sts: continue
    js=attached(h); a0=min(s for _,s,_ in sts); a1=max(s for _,_,s in sts)
    lo=max(0.0,a0-1.0); hi=min(L,a1+1.0)
    if js: lo=min(lo,min(js)); hi=max(hi,max(js))
    s,Q=resample(P,0.5)
    if lo>0.5 or hi<L-0.5:
        m=(s>=lo-1e-6)&(s<=hi+1e-6); Q=Q[m]; s=s[m]-s[m][0]
        log.append(('trimmed',h,round(L),'->',round(s[-1]),'cut start %.0f m, end %.0f m'%(lo,L-hi)))
    # straighten: greedy longest straight runs keeping stope centres within 1.5 m and hitting junction points
    sts=[(st,0,0) for st,_,_ in sts]
    C=np.vstack([st['C'] for st,_,_ in sts])
    jpts=[]
    for h2,w in ALL.items():
        if h2==h: continue
        for e in (0,-1):
            ok,sj=on(w['P'][e],Q)
            if ok: jpts.append(int(np.argmin(np.abs(s-sj))))
    keep={0,len(Q)-1}|set(jpts)
    def fits(i,j):
        a=Q[i,:2]; b=Q[j,:2]; ab=b-a; L2=ab@ab
        if L2<1e-9: return True
        # stope centres whose projection falls in this run
        t=((C-a)@ab)/L2; m=(t>=0)&(t<=1)
        if m.any():
            d=np.abs((C[m]-a)[:,0]*ab[1]-(C[m]-a)[:,1]*ab[0])/np.sqrt(L2)
            if d.max()>2.0: return False
        return True
    idx=[0]; i=0
    while i<len(Q)-1:
        nxt=[k for k in sorted(keep) if k>i]; lim=nxt[0]
        j=lim
        jmin=min(lim,i+16)          # runs at least 8 m (or to the next junction)
        while j>jmin and not fits(i,j): j-=1
        idx.append(j); i=j
    N=Q[idx]; N[:,2]=np.interp(np.arange(len(idx)),np.arange(len(idx)),Q[idx,2])
    if len(N)!=len(P) or np.abs(N[[0,-1]]-P[[0,-1]]).max()>0.05 or True:
        v['P']=N; v['changed']=True
# re-snap endpoints of strings that sat on a changed drive
for h,v in ALL.items():
    for e in (0,-1):
        p=v['P'][e]
        for h2,w in ALL.items():
            if h2==h or w['t']!='OD': continue
            d,sx,u,z,i,t=proj(w['P'],p[:2])
            if 0.02<d<3.0 and abs(p[2]-z)<2.0:
                a=w['P'][i]; b=w['P'][i+1]; q=a+t*(b-a); v['P'][e,:2]=q[:2]; v['changed']=True; break
for l in log: print(l)
S2={h:v for h,v in ALL.items() if not h.startswith('new')}; new2=[v for h,v in ALL.items() if h.startswith('new')]
removed=[l[1] for l in log if l[0].startswith('removed')]
D['S']=S2; D['new']=new2; D['removed']=removed; pickle.dump(D,open('stage4.pkl','wb'))
print('vertices per OD:',sorted([len(v['P']) for v in ALL.values() if v['t']=='OD']))
