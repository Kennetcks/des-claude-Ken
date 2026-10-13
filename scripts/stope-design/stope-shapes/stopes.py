import numpy as np, collections, math, re, json, pickle, sys
from bio import load
CUT=2.4; AVG=2.5; LMAX=50.0; LPREF=40.0; LMIN=10.0; PIL=5.0; TANA=math.tan(math.radians(37)); DS=0.5; DZ=0.5; DSTEP=2.5; TMIN=1500
B=load()
drives=collections.defaultdict(list)
for b in B: drives[b['drive']].append(b)

def columns(bl):
    cols=collections.OrderedDict()
    for b in sorted(bl,key=lambda b:(b['ch0'],b['base'])):
        cols.setdefault((round(b['ch0'],2),round(b['ch1'],2)),[]).append(b)
    out=[]
    for (s0,s1),ps in cols.items():
        ps=sorted(ps,key=lambda b:b['base'])
        c=dict(s0=s0,s1=s1,pieces=ps,H=max(p['base']+p['ht'] for p in ps),
               ha=(ps[0]['base']+ps[0]['ht']) if len(ps)>1 else None, split=len(ps)>1)
        out.append(c)
    return out

def grid(cols):
    s0=cols[0]['s0']; s1=cols[-1]['s1']; ns=int(round((s1-s0)/DS)); H=max(c['H'] for c in cols); nz=int(math.ceil(H/DZ))
    T=np.zeros((ns,nz)); G=np.zeros((ns,nz)); colof=np.full(ns,-1)
    sc=s0+DS*(np.arange(ns)+0.5); zc=DZ*(np.arange(nz)+0.5)
    for ci,c in enumerate(cols):
        si=(sc>=c['s0'])&(sc<c['s1']); colof[si]=ci
        for p in c['pieces']:
            zi=(zc>=p['base'])&(zc<p['base']+p['ht'])
            n=si.sum()*zi.sum()
            if n==0: continue
            T[np.ix_(si,zi)]=p['t']/n; G[np.ix_(si,zi)]=p['au']
    return dict(s0=s0,sc=sc,zc=zc,T=T,G=G,colof=colof)

HOPT=np.arange(7.5,40.01,2.5)
def mask(gr,cols,i,j,tops,dL,dR):
    sc,zc,colof=gr['sc'],gr['zc'],gr['colof']
    sL=cols[i]['s0']; sR=cols[j]['s1']
    insc=(colof>=i)&(colof<=j)
    tp=np.zeros(len(sc)); tp[insc]=np.array(tops)[colof[insc]-i]
    M=insc[:,None]&(zc[None,:]<tp[:,None]-1e-6)
    ztL=tops[0]; ztR=tops[-1]
    if dL>0: M&= sc[:,None] >= sL+dL*(1-zc[None,:]/ztL)
    if dR>0: M&= sc[:,None] <= sR-dR*(1-zc[None,:]/ztR)
    return M

def evalm(gr,M,cut):
    t=gr['T'][M].sum()
    if t<=0: return 0,0,-1e9
    g=(gr['T'][M]*gr['G'][M]).sum()/t
    return t,g,t*(g-cut)

def best_tops(gr,cols,i,j,dL,dR,cut):
    n=j-i+1; Hs=np.array([cols[k]['H'] for k in range(i,j+1)])
    # value per column for each top option (index 0 = own H, then HOPT), with chamfers computed on full-height tops
    full=mask(gr,cols,i,j,list(Hs),dL,dR)
    W=gr['T']*(gr['G']-cut)*full
    opts=[None]+list(HOPT)
    V=np.full((n,len(opts)),-1e18)
    for c in range(n):
        si=gr['colof']==i+c
        for o,h in enumerate(opts):
            hh=Hs[c] if h is None else h
            if h is not None and h>=Hs[c]-0.01: continue
            V[c,o]=W[np.ix_(si,gr['zc']<hh)].sum()
    best=(-1e18,None)
    # single flat top for all, or H on [p,q] with flat hL left / hR right
    for o in range(len(opts)):
        if np.all(V[:,o]>-1e17):
            v=V[:,o].sum()
            if v>best[0]: best=(v,[o]*n)
    def side(cs):
        if not cs: return 0.0,[]
        b=(-1e18,None)
        for o in range(1,len(opts)):
            if np.all(V[cs,o]>-1e17): 
                v=V[cs,o].sum()
                if v>b[0]: b=(v,[o]*len(cs))
        v0=V[cs,0].sum()
        if all(cols[i+c]['H']<=HOPT[0] for c in cs) or b[1] is None: return v0,[0]*len(cs)
        return b
    for p in range(n):
        for q in range(p,n):
            vl,ol=side(list(range(0,p))); vr,orr=side(list(range(q+1,n)))
            if ol is None or orr is None: continue
            v=V[p:q+1,0].sum()+vl+vr
            if v>best[0]: best=(v,ol+[0]*(q-p+1)+orr)
    o=best[1]
    return [Hs[c] if opts[o[c]] is None else opts[o[c]] for c in range(n)]

def best_shape(gr,cols,i,j,cut):
    length=cols[j]['s1']-cols[i]['s0']
    dL=dR=0.0
    for it in range(2):
        tops=best_tops(gr,cols,i,j,dL,dR,cut)
        for side in (1,2):
            zt=tops[0] if side==1 else tops[-1]; dmax=TANA*zt if zt>=12.5 else 0.0
            bv=None
            for d in np.arange(0,dmax+1e-6,DSTEP):
                a,b=(d,dR) if side==1 else (dL,d)
                if a+b>length-DS: continue
                t,g,v=evalm(gr,mask(gr,cols,i,j,tops,a,b),cut)
                if bv is None or v>bv[0]+1e-9: bv=(v,float(d))
            if side==1: dL=bv[1]
            else: dR=bv[1]
    t,g,v=evalm(gr,mask(gr,cols,i,j,tops,dL,dR),cut)
    return dict(i=i,j=j,tops=[float(x) for x in tops],dL=dL,dR=dR,t=t,g=g,v=v)

def design_drive(key):
    cols=columns(drives[key]); gr=grid(cols); n=len(cols)
    cands=[]
    for i in range(n):
        for j in range(i,n):
            if j>i and abs(cols[j]['s0']-cols[j-1]['s1'])>0.01: break
            L=cols[j]['s1']-cols[i]['s0']
            if L>LMAX+0.01: break
            if L<LMIN-0.01: continue
            s=best_shape(gr,cols,i,j,CUT)
            if s['g']<AVG:
                s=best_shape(gr,cols,i,j,AVG)
                if s['g']<AVG: continue
                s['v']=s['t']*(s['g']-CUT)
            if s['v']<=0 or s['t']<TMIN: continue
            s['score']=s['v']*(0.95 if L>LPREF+0.01 else 1.0); s['L']=L
            cands.append(s)
    # DP over chainage: choose non-overlapping with >=PIL between top ends
    cands.sort(key=lambda s:cols[s['j']]['s1'])
    best=[0.0]; choice=[[]]
    ends=[]
    for k,s in enumerate(cands):
        sL=cols[s['i']]['s0']
        # best among previous chosen sets ending <= sL-PIL
        bi=0
        for m in range(k):
            if cols[cands[m]['j']]['s1']<=sL-PIL+0.01 and best[m+1]>best[bi]: bi=m+1
        # also allow not taking: handled by carrying max
        val=best[bi]+s['score']; ch=choice[bi]+[k]
        prevmax=max(range(k+1),key=lambda q:best[q])
        best.append(val); choice.append(ch)
    kbest=max(range(len(best)),key=lambda q:best[q])
    sel=[cands[k] for k in choice[kbest]]
    return cols,gr,sel

if __name__=='__main__':
    res={}
    keys=[k for k in drives if '/' not in k]
    for n,k in enumerate(keys):
        cols,gr,sel=design_drive(k); res[k]=(cols,sel)
    pickle.dump(res,open('stopes.pkl','wb'))
    T=sum(s['t'] for c,sel in res.values() for s in sel); M=sum(s['t']*s['g'] for c,sel in res.values() for s in sel)
    ns=sum(len(sel) for c,sel in res.values())
    print('stopes',ns,'t',round(T),'g',round(M/T,2))
    Ls=[s['L'] for c,sel in res.values() for s in sel]; print('L pct',np.percentile(Ls,[0,25,50,75,100]))
    ch=[s for c,sel in res.values() for s in sel if s['dL']>0 or s['dR']>0]; print('with chamfer',len(ch))
