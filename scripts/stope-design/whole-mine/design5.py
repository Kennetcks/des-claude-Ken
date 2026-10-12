"""v5 (Kennet 2026-10-12): also split blocks >= 2.6 g/t when a top or bottom section is below 2.0 g/t ('be smart').
v3 (Kennet 2026-10-12): every block full height (no lowering), OD/trial blocks kept even with no grade,
v4: split with one horizontal cut (by height, every 2.5 m, pieces >= 5 m) for a piece > 2.6, else > 2.0.
v2: floor-to-floor (base at the drive floor, max 22 m, top = overhead drive floor when one is within 22 m),
no grade filter on OD/trial blocks, blocks < SPLIT_G split in two when one piece beats SPLIT_G.
5 m blocks along every candidate drive in the mine (whole-mine version of blocks_along_drive.py).
Rules: base 1 m below the drive back, top <= back + HMAX and never above the floor of a drive overhead,
walls shaped on >= CUT blocks, sections drillable (convex, >= MIN_DIP, <= 1 m change between 5 m sections),
top lowered in 2.5 m bands while the top band loses money (min height HMIN), keep a block only if its
diluted grade > KEEP_G."""
import sys, csv, json, collections, numpy as np
from raster import Model
ORIGIN=np.array([7770.,62610.,5930.])
CUT, MARG, ORE_SHARE, KEEP_G, SPLIT_G = 2.5, 2.5, 0.15, 1.5, 2.6
SPLIT_TARGETS = (2.6, 2.0)
LOW_G = 2.0   # v5: blocks >= 2.6 are also split when a top or bottom section (>= 5 m) is below this   # v3: split for a piece > 2.6 first, else for a piece > 2.0
KEEP_ALL = {'OD'}   # other drive types keep only blocks > KEEP_G (no stopes along waste accesses)
MIN_W, HMAX, HMIN, BASE_IN, SEC, NL, MAX_STEP, MIN_DIP = 2.2, 22.0, 7.5, 0.0, 5.0, 4, 1.0, 55.0
DS, DU, DH = 0.5, 0.25, 0.5
UG = np.arange(-14+DU/2, 14, DU); NU=len(UG); nmin=int(np.ceil(MIN_W/DU))
PH = {'OD':4.5,'SMP':4.5}; PH.update({k:5.0 for k in 'ACC RAD DDD EWD SUB PST'.split()}); PH.update({k:5.5 for k in 'DEC MLA ISP DSP'.split()})
M = None
MODEL_PRE = 'r'
def load_strings():
    d=collections.OrderedDict()
    for r in csv.DictReader(open('/mnt/user-data/uploads/work/stopes_mine/floor_strings.csv')):
        d.setdefault(r['pid'],{'pid':r['pid'],'act':r['act'],'lv':r['latvert'],'pts':[]})['pts'].append((float(r['x']),float(r['y']),float(r['z'])))
    for v in d.values(): v['pts']=np.array(v['pts'])-ORIGIN
    return d
class Drive:
    def __init__(s, pts):
        keep=[0]
        for i in range(1,len(pts)):
            if np.hypot(*(pts[i,:2]-pts[keep[-1],:2]))>0.05: keep.append(i)
        s.P=pts[keep]; s.seg=np.diff(s.P[:,:2],axis=0); s.L=np.hypot(s.seg[:,0],s.seg[:,1]); s.cum=np.r_[0,np.cumsum(s.L)]; s.len=s.cum[-1]
    def frame(s, S):
        S=np.clip(S,1e-3,s.len-1e-3); i=np.clip(np.searchsorted(s.cum,S,side='right')-1,0,len(s.L)-1)
        d=s.seg[i]/s.L[i,None]; p=s.P[i,:2]+(S-s.cum[i])[:,None]*d; z=s.P[i,2]+(S-s.cum[i])/s.L[i]*(s.P[i+1,2]-s.P[i,2])
        return p, np.c_[-d[:,1],d[:,0]], z
def overhead_floor(drv, S, back, others):
    """lowest floor of any other drive passing over the stope footprint (|u|<=7 m) above back+2"""
    p,_,_=drv.frame(S); out=np.full(len(S),np.inf)
    if len(others)==0: return out
    d=np.hypot(p[:,None,0]-others[None,:,0],p[:,None,1]-others[None,:,1])
    z=np.where((d<=7.0)&(others[None,:,2]>back[:,None]+2.0),others[None,:,2],np.inf)
    return z.min(axis=1)
def run(job):
    global M
    if M is None: M=Model(MODEL_PRE)
    pid, act, pts, others = job
    drv=Drive(pts)
    if drv.len<5: return pid, None, 'short'
    ph=PH.get(act,5.0)
    Sg=np.arange(DS/2, drv.len, DS); p,nrm,fz=drv.frame(Sg); back=fz+ph; ZB=fz.copy()
    ceil=overhead_floor(drv,Sg,back,others); Htop=np.minimum(HMAX, ceil-ZB)   # floor to floor, max 22 m
    BT=(ceil-ZB)<=HMAX+0.5                                                      # breaks through into a drive above
    if np.median(Htop)<HMIN: return pid, None, 'no room'
    HG=np.arange(DH/2, HMAX, DH); NH=len(HG)
    X=p[:,None,None,0]+UG[None,:,None]*nrm[:,None,None,0]; Y=p[:,None,None,1]+UG[None,:,None]*nrm[:,None,None,1]; Z=ZB[:,None,None]+HG[None,None,:]
    X,Y,Z=np.broadcast_arrays(X,Y,Z)
    td,au,mn=M.look(X.ravel(),Y.ravel(),Z.ravel()); sh=(len(Sg),NU,NH)
    T=(td*DS*DU*DH).reshape(sh); G=au.reshape(sh); MNd=mn.reshape(sh)
    T=np.where(HG[None,None,:]<=Htop[:,None,None],T,0)      # nothing above the overhead drive
    # quick reject: no 5 m stretch can reach KEEP_G even in the best 2.2-8 m window
    V=T*(G-MARG); MET=T*G
    # 1. walls per 1 m station and level
    S1=np.arange(0.5, Sg[-1]+1e-9, 1.0); LF=np.linspace(0,1,NL)
    FW=np.zeros((len(S1),NL)); HWl=np.zeros_like(FW); inside=np.abs(UG)<=ph/2
    ii,jj=np.meshgrid(np.arange(NU+1),np.arange(NU+1),indexing='ij'); valid=jj-ii>=nmin
    best_g=0
    for a,s0 in enumerate(S1):
        sm=np.abs(Sg-s0)<=2.5; H=np.median(Htop[sm])
        for l,f in enumerate(LF):
            band=np.abs(HG-f*H)<=H/(NL-1)/2+0.1
            prof=V[sm][:,:,band].sum(axis=(0,2))
            if l==0: prof=np.where(inside,prof,-1e9)
            cs=np.r_[0,np.cumsum(prof)]; val=np.where(valid,cs[jj]-cs[ii],-np.inf); k=np.argmax(val); i,j=np.unravel_index(k,val.shape)
            if val[i,j]<=0:
                mp=MET[sm][:,:,band].sum(axis=(0,2))
                if l==0: mp=np.where(inside,mp,-1e9)
                cm=np.r_[0,np.cumsum(mp)]; w=cm[nmin:]-cm[:-nmin]-1e-6*np.abs(UG[:len(cm)-nmin]+DU*nmin/2); i=int(np.argmax(w)); j=i+nmin
            FW[a,l]=UG[i]-DU/2; HWl[a,l]=UG[j-1]+DU/2
    def smooth(A,k=5):
        B=np.array([np.convolve(np.pad(A[:,l],k//2,mode='edge'),np.ones(k)/k,'valid') for l in range(A.shape[1])]).T
        C=B.copy(); C[:,1:-1]=(B[:,:-2]+2*B[:,1:-1]+B[:,2:])/4; return C
    FW=smooth(FW); HWl=smooth(HWl)
    # sections every SEC m
    ss=list(np.arange(0,drv.len,SEC))
    if drv.len-ss[-1]<SEC/2 and len(ss)>1: ss[-1]=drv.len
    else: ss.append(drv.len)
    ss=np.array(ss)
    fw=np.array([[np.interp(s,S1,FW[:,q]) for q in range(NL)] for s in ss]); hw=np.array([[np.interp(s,S1,HWl[:,q]) for q in range(NL)] for s in ss])
    Hs=np.array([np.interp(s,Sg,Htop) for s in ss])
    def max_out(H): return H/(NL-1)/np.tan(np.radians(MIN_DIP))
    def strike_limit(x):
        for _ in range(50):
            for k in list(range(1,len(x)))+list(range(len(x)-2,-1,-1)):
                for nb in (k-1,k+1):
                    if 0<=nb<len(x) and abs(x[k]-x[nb])>MAX_STEP: x[k]=(x[k]+x[nb])/2+np.sign(x[k]-x[nb])*MAX_STEP/2
        return x
    MO=max_out(Hs)
    def pava_dec(y):                            # least-squares non-increasing fit
        blocks=[[v,1.0] for v in y]; out=[]
        for v in blocks:
            out.append(v)
            while len(out)>1 and out[-2][0]<out[-1][0]:
                a,b=out.pop(); c,dd=out.pop(); out.append([(a*b+c*dd)/(b+dd),b+dd])
        r=[]
        for v,w in out: r+= [v]*int(round(w))
        return np.array(r)
    def convex(o,mo):                           # outward offsets up one wall -> drillable (steps <= mo, never growing)
        st=np.clip(pava_dec(np.diff(o)),-mo,mo); return np.r_[o[0],o[0]+np.cumsum(st)]
    def project():
        for si in range(len(ss)):
            fw[si]=-convex(-fw[si],MO[si]); hw[si]=convex(hw[si],MO[si])
            for q in range(NL):
                if hw[si,q]-fw[si,q]<MIN_W: c=(hw[si,q]+fw[si,q])/2; fw[si,q]=c-MIN_W/2; hw[si,q]=c+MIN_W/2
            shf=max(hw[si,0]-ph/2,0)-max(-ph/2-fw[si,0],0); fw[si]-=shf; hw[si]-=shf
    for _ in range(6):
        project()
        for A in (fw,hw):
            for q in range(NL): A[:,q]=strike_limit(A[:,q].copy())
    project()
    def section_ok(si):
        f,h=fw[si],hw[si]
        if np.any(h-f<MIN_W-1e-9) or f[0]<-ph/2-1e-9 or h[0]>ph/2+1e-9: return False
        for d in (np.diff(-f),np.diff(h)):
            if np.any(np.abs(d)>MO[si]+1e-9) or np.any(np.diff(d)>1e-9): return False
        return True
    def inmask(a,b):   # stations of Sg between sections a..b, boolean (n,NU,NH) of cells inside the shape
        sm=(Sg>ss[a])&(Sg<ss[b]); S=Sg[sm]
        H=np.interp(S,ss,Hs)
        f=np.array([np.interp(S,ss,fw[:,q]) for q in range(NL)]).T; h=np.array([np.interp(S,ss,hw[:,q]) for q in range(NL)]).T
        fr=np.clip(HG[None,:]/H[:,None],0,1)*(NL-1)
        fz=np.array([np.interp(fr[n],np.arange(NL),f[n]) for n in range(len(S))]); hz=np.array([np.interp(fr[n],np.arange(NL),h[n]) for n in range(len(S))])
        m=(UG[None,:,None]>=fz[:,None,:])&(UG[None,:,None]<=hz[:,None,:])&(HG[None,None,:]<=H[:,None,None])
        return sm,m
    def ev(a,b):
        sm,m=inmask(a,b); t=T[sm]*m; return np.array([t.sum(),(t*G[sm]).sum(),(t*(G[sm]>=CUT)).sum()])
    for si in range(len(ss)):
        if not section_ok(si):
            for A in (fw,hw):
                top=A[si,0]+np.clip(A[si,-1]-A[si,0],-MO[si]*(NL-1),MO[si]*(NL-1))
                A[si]=A[si,0]+(top-A[si,0])*np.arange(NL)/(NL-1)
    # nudge wall points, evaluated locally on the two neighbouring blocks
    for _ in range(16):
        changed=0
        for si in range(len(ss)):
            a,b=max(si-1,0),min(si+1,len(ss)-1)
            for q in range(NL):
                for A,sign in ((fw,-1),(hw,1)):
                    for step in (0.25,-0.25):
                        e0=ev(a,b); old=A[si,q]; A[si,q]=old+sign*step
                        if section_ok(si) and all(abs(A[si,q]-A[x,q])<=MAX_STEP+1e-9 for x in (si-1,si+1) if 0<=x<len(ss)):
                            e1=ev(a,b); dt,dm,do=e1-e0
                            good=dt!=0 and (dm/dt>=MARG or do/dt>=ORE_SHARE)
                            if (step>0 and dt>0 and good) or (step<0 and dt<0 and not good): changed+=1; continue
                        A[si,q]=old
        if not changed: break
    # pieces: chainage c0..c1, heights zlo..top (top None = full height of the section, i.e. floor to floor)
    def cells(c0,c1,zlo,top):
        sm=(Sg>c0)&(Sg<c1); S=Sg[sm]
        H=np.interp(S,ss,Hs)
        f=np.array([np.interp(S,ss,fw[:,q]) for q in range(NL)]).T; h=np.array([np.interp(S,ss,hw[:,q]) for q in range(NL)]).T
        fr=np.clip(HG[None,:]/H[:,None],0,1)*(NL-1)
        fz_=np.array([np.interp(fr[n],np.arange(NL),f[n]) for n in range(len(S))]); hz=np.array([np.interp(fr[n],np.arange(NL),h[n]) for n in range(len(S))])
        tp=H if top is None else np.minimum(top,H)
        m=(UG[None,:,None]>=fz_[:,None,:])&(UG[None,:,None]<=hz[:,None,:])&(HG[None,None,:]<=tp[:,None,None])&(HG[None,None,:]>zlo)
        return sm,m
    def stats(c0,c1,zlo,top):
        sm,m=cells(c0,c1,zlo,top); t=T[sm]*m; ton=t.sum()
        if ton<=0: return 0.,0.,0.,0.
        return ton,(t*G[sm]).sum()/ton,(t*(G[sm]>=CUT)).sum(),(t*MNd[sm]).sum()/ton
    def ring(s,zlo,ztop):
        pp,nn,zf=drv.frame(np.array([s])); Hsec=np.interp(s,ss,Hs)
        f=[np.interp(s,ss,fw[:,q]) for q in range(NL)]; h=[np.interp(s,ss,hw[:,q]) for q in range(NL)]
        hts=np.linspace(zlo,ztop,NL); fr=np.clip(hts/Hsec,0,1)*(NL-1)
        fwz=np.interp(fr,np.arange(NL),f); hwz=np.interp(fr,np.arange(NL),h)
        return [(pp[0,0]+u*nn[0,0],pp[0,1]+u*nn[0,1],zf[0]+z) for u,z in list(zip(fwz,hts))+list(zip(hwz[::-1],hts[::-1]))]
    def piece(bi,c0,c1,zlo,top,label,split,bt,target=0.):
        ton,grade,ore,mined=stats(c0,c1,zlo,top)
        if ton<=0: return None
        ta=np.interp(c0,ss,Hs) if top is None else top; tb=np.interp(c1,ss,Hs) if top is None else top
        V=np.array(ring(c0,zlo,ta)+ring(c1,zlo,tb)); R=2*NL
        F=[]
        for e in range(R): a1=(e+1)%R; F+=[(e,a1,a1+R),(e,a1+R,e+R)]
        for base,flip in ((0,True),(R,False)):
            for q in range(NL-1):
                tri=[(base+q,base+q+1,base+R-2-q),(base+q,base+R-2-q,base+R-1-q)]
                F+=[(x,z,y) for x,y,z in tri] if flip else tri
        if sum(np.dot(V[x],np.cross(V[y],V[z])) for x,y,z in F)<0: F=[(x,z,y) for x,y,z in F]
        vol=sum(np.dot(V[x],np.cross(V[y],V[z])) for x,y,z in F)/6
        sm,m=cells(c0,c1,zlo,top); Sx=Sg[sm]; U3,H3=np.meshgrid(UG,HG,indexing='ij'); pts=[]
        for n in range(len(Sx)):
            if not m[n].any(): continue
            pq,nq,zq=drv.frame(np.array([Sx[n]])); uu=U3[m[n]]; hh=H3[m[n]]
            pts.append(np.c_[pq[0,0]+uu*nq[0,0],pq[0,1]+uu*nq[0,1],zq[0]+hh])
        pts=np.vstack(pts); vox=np.unique((np.floor(pts[:,0]).astype(np.int64)*2000+np.floor(pts[:,1]).astype(np.int64))*2000+np.floor(pts[:,2]).astype(np.int64))
        hi=max(ta,tb)-zlo
        return dict(pid=pid,act=act,bi=bi,part=label,split=split,target=target,ch0=float(c0),ch1=float(c1),zlo=float(zlo),top=float(max(ta,tb)),
            height=float(hi),bt=bool(bt),height_above_back=float(max(ta,tb)-ph),
            t=float(ton),g=float(grade),ore_t=float(ore),mined=float(mined),w=float((hw[bi:bi+2]-fw[bi:bi+2]).mean()),vol=float(vol),
            cx=float(V[:,0].mean()+ORIGIN[0]),cy=float(V[:,1].mean()+ORIGIN[1]),cz=float(V[:,2].min()+ORIGIN[2]),
            drill_ok=bool(section_ok(bi) and section_ok(bi+1)),
            V=(V+ORIGIN).round(3).tolist(),F=[list(map(int,f)) for f in F],vox=vox)
    out=[]; keep_all=act in KEEP_ALL
    for bi in range(len(ss)-1):
        c0,c1=float(ss[bi]),float(ss[bi+1]); smb=(Sg>c0)&(Sg<c1)
        if not smb.any(): continue
        bt=bool(np.mean(BT[smb])>=0.5)
        top=None                                          # always full height: floor to floor (B/T) or 22 m (blind), never lowered
        ton,grade,_,_=stats(c0,c1,0.,top)
        if ton<=0 or (not keep_all and grade<=KEEP_G): continue   # OD/trial blocks kept even with no grade
        best=None
        Hmin=min(Hs[bi],Hs[bi+1])
        cand=[('HEIGHT',z) for z in np.arange(5.0,Hmin-5.0+1e-9,2.5)]   # horizontal cuts only (Kennet): one piece above the other, each >= 5 m
        for thr in SPLIT_TARGETS:                         # first try for a piece > 2.6, else a piece > 2.0
            if grade>=thr: break
            for kind,c in cand:
                A=stats(c0,c,0.,top) if kind=='STRIKE' else stats(c0,c1,0.,c)
                B=stats(c,c1,0.,top) if kind=='STRIKE' else stats(c0,c1,c,top)
                if A[0]<=0 or B[0]<=0: continue
                hi=A if A[1]>B[1] else B
                if hi[1]<=thr: continue
                sc=hi[0]*(hi[1]-thr)
                if best is None or sc>best[0]: best=(sc,kind,float(c),thr)
            if best: break
        if best is None and grade>=SPLIT_G:               # v5: an ore block with a low-grade top or bottom (< 2.0) is split too,
            for kind,c in cand:                           # so the high-grade and low-grade sections show separately
                A=stats(c0,c1,0.,c); B=stats(c0,c1,c,top)
                if A[0]<=0 or B[0]<=0: continue
                hi,lo=(A,B) if A[1]>B[1] else (B,A)
                if hi[1]<=SPLIT_G or lo[1]>=LOW_G: continue
                sc=hi[0]*(hi[1]-SPLIT_G)
                if best is None or sc>best[0]: best=(sc,'HEIGHT_LOW',float(c),SPLIT_G)
        if best is None:
            b=piece(bi,c0,c1,0.,top,'','',bt); 
            if b: out.append(b)
        else:
            _,kind,c,thr=best
            if kind=='STRIKE': ps=[piece(bi,c0,c,0.,top,'a',kind,bt,thr),piece(bi,c,c1,0.,top,'b',kind,bt,thr)]
            else: ps=[piece(bi,c0,c1,0.,c,'a',kind,False,thr),piece(bi,c0,c1,c,top,'b',kind,bt,thr)]   # HEIGHT or HEIGHT_LOW
            out+=[b for b in ps if b]
    return pid, out, 'ok'
