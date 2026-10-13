"""v8 (Kennet 2026-10-12, on v7): stopes must never overlap ("otherwise I am counting the grade twice"), and where
lodes sit close together a stope takes both when the combined grade is > 2.6, otherwise the better one.
- close lodes: at each station and height band the walls take the span of lodes (runs >= 2.5 g/t) with the most ore
  whose diluted grade is > 2.6 (max 20 m wide); when no span reaches 2.6, the old best window (max t*(g-2.5)).
- bends: on the inside of a bend the walls stop 0.3 m short of where two section lines cross, so blocks of the same
  drive never overlap and no solid comes out inverted (no convex-hull rebuilds).
- forbid: optional list of solids (other drives' accepted blocks); their space counts as heavy waste for the walls,
  walls are then pushed out of it, and any piece still touching it is dropped.
v6 (Kennet 2026-10-12): the stope base is no longer tied to the drive ('I could always move the ore drive later'),
walls range +-17 m, 50 deg footwall minimum, 5 wall points, to recover ore left behind.
v5 (Kennet 2026-10-12): also split blocks >= 2.6 g/t when a top or bottom section is below 2.0 g/t ('be smart').
v3 (Kennet 2026-10-12): every block full height (no lowering), OD/trial blocks kept even with no grade,
v4: split with one horizontal cut (by height, every 2.5 m, pieces >= 5 m) for a piece > 2.6, else > 2.0.
v2: floor-to-floor (base at the drive floor, max 22 m, top = overhead drive floor when one is within 22 m),
no grade filter on OD/trial blocks, blocks < SPLIT_G split in two when one piece beats SPLIT_G.
5 m blocks along every candidate drive in the mine (whole-mine version of blocks_along_drive.py).
Rules: base 1 m below the drive back, top <= back + HMAX and never above the floor of a drive overhead,
walls shaped on >= CUT blocks, sections drillable (convex, >= MIN_DIP, <= 1 m change between 5 m sections),
top lowered in 2.5 m bands while the top band loses money (min height HMIN), keep a block only if its
diluted grade > KEEP_G."""
import os, sys, csv, json, collections, numpy as np
from raster import Model
ORIGIN=np.array([7770.,62610.,5930.])
CUT, MARG, ORE_SHARE, KEEP_G, SPLIT_G = 2.5, 2.5, 0.15, 1.5, 2.6
SPLIT_TARGETS = (2.6, 2.0)
LOW_G = 2.0   # v5: blocks >= 2.6 are also split when a top or bottom section (>= 5 m) is below this   # v3: split for a piece > 2.6 first, else for a piece > 2.0
KEEP_ALL = {'OD'}   # other drive types keep only blocks > KEEP_G (no stopes along waste accesses)
MAX_W, SPAN_G = 20.0, 2.6
SHAPE_DRIFT = float(os.environ.get('SHAPE_DRIFT','0.5'))   # v10c: max centre drift per m along strike
SHAPE_REF = float(os.environ.get('SHAPE_REF','2.5'))   # v10c: grade the column shape pays against (metal above it)
SPAN_PICK = os.environ.get('SPAN_PICK','ore')   # 'ore' = v8/v9 (most ore tonnes), 'value' = v10 (most metal above cut-off), 'track' = v10b (one lode track per drive), 'shape' = v10c (one column shape per station)
MIN_W, HMAX, HMIN, BASE_IN, SEC, NL, MAX_STEP, MIN_DIP = 2.2, 22.0, 7.5, 0.0, 5.0, 5, 1.0, 50.0   # v6: 5 wall points, 50 deg (standard minimum footwall)
DS, DU, DH = 0.5, 0.25, 0.5
UG = np.arange(-17+DU/2, 17, DU); NU=len(UG); nmin=int(np.ceil(MIN_W/DU))   # v6: +-17 m (edge of the exported corridor)
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
def inside_cols(V,F,gx,gy,z0,hs):
    """columns (gx,gy) with heights z0+hs -> bool (ncol,nh) inside the closed mesh V,F (vertical ray parity)"""
    A,B,C=V[F[:,0]],V[F[:,1]],V[F[:,2]]; out=np.zeros((len(gx),len(hs)),bool)
    for a,b,c in zip(A,B,C):
        den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
        if abs(den)<1e-12: continue
        w1=((b[1]-c[1])*(gx-c[0])+(c[0]-b[0])*(gy-c[1]))/den; w2=((c[1]-a[1])*(gx-c[0])+(a[0]-c[0])*(gy-c[1]))/den; w3=1-w1-w2
        hit=(w1>=0)&(w2>=0)&(w3>=0)
        if not hit.any(): continue
        zt=w1*a[2]+w2*b[2]+w3*c[2]; out[hit]^=((z0[hit,None]+hs[None,:])<zt[hit,None])
    return out
# Kennet 2026-10-13 (ST strings): a stope under another level goes up to that level's string RL, even past 22 m, so two
# stacked stopes can later be combined into a double lift. HUP = tallest floor-to-floor stope, CEIL_R = plan radius to the
# string above. Defaults keep the v8 whole-mine behaviour (7 m, max 22 m).
CEIL_R=float(os.environ.get('CEIL_R','7')); HUP=float(os.environ.get('HUP',str(HMAX)))
# HBLIND = height of a blind stope (no string above): Kennet 2026-10-13 uses 26 m, ~22 m once the development drive is taken out
HBLIND=float(os.environ.get('HBLIND',str(HMAX)))
def overhead_floor(drv, S, back, others):
    """lowest floor of any other drive passing over the stope footprint (|u|<=CEIL_R, 7 m by default) above back+2"""
    p,_,_=drv.frame(S); out=np.full(len(S),np.inf)
    if len(others)==0: return out
    d=np.hypot(p[:,None,0]-others[None,:,0],p[:,None,1]-others[None,:,1])
    z=np.where((d<=CEIL_R)&(others[None,:,2]>back[:,None]+2.0),others[None,:,2],np.inf)
    return z.min(axis=1)
def run(job):
    global M
    if M is None: M=Model(MODEL_PRE)
    pid, act, pts, others = job[:4]; forbid = job[4] if len(job)>4 else []
    drv=Drive(pts)
    if drv.len<5: return pid, None, 'short'
    ph=PH.get(act,5.0)
    Sg=np.arange(DS/2, drv.len, DS); p,nrm,fz=drv.frame(Sg); back=fz+ph; ZB=fz.copy()
    ceil=overhead_floor(drv,Sg,back,others); gap=ceil-ZB; Htop=np.where(gap<=HUP, gap, HBLIND)   # floor to floor (up to HUP), else blind height (22 m default)
    BT=gap<=HUP+0.5                                                             # breaks through into a drive above
    if np.median(Htop)<HMIN: return pid, None, 'no room'
    HG=np.arange(DH/2, max(HMAX,HUP,HBLIND), DH); NH=len(HG)
    X=p[:,None,None,0]+UG[None,:,None]*nrm[:,None,None,0]; Y=p[:,None,None,1]+UG[None,:,None]*nrm[:,None,None,1]; Z=ZB[:,None,None]+HG[None,None,:]
    X,Y,Z=np.broadcast_arrays(X,Y,Z)
    td,au,mn=M.look(X.ravel(),Y.ravel(),Z.ravel()); sh=(len(Sg),NU,NH)
    T=(td*DS*DU*DH).reshape(sh); G=au.reshape(sh); MNd=mn.reshape(sh)
    T=np.where(HG[None,None,:]<=Htop[:,None,None],T,0)      # nothing above the overhead drive
    FB=np.zeros(sh,bool)                                     # v8: space taken by other drives' accepted blocks
    if forbid:
        Xc=X[:,:,0].ravel(); Yc=Y[:,:,0].ravel(); Zc=np.repeat(ZB,NU)
        for Vf,Ff in forbid:
            lo=Vf.min(0); hi=Vf.max(0); c=(Xc>=lo[0])&(Xc<=hi[0])&(Yc>=lo[1])&(Yc<=hi[1])
            if not c.any(): continue
            ins=np.zeros((len(Xc),NH),bool); ins[c]=inside_cols(Vf,Ff,Xc[c]+1e-4,Yc[c]+1e-4,Zc[c],HG); FB|=ins.reshape(sh)
        T=np.where(FB,0,T)
    # quick reject: no 5 m stretch can reach KEEP_G even in the best 2.2-8 m window
    V=T*(G-MARG); MET=T*G
    PEN=1e3*DS*DU*DH; V=np.where(FB,-PEN,V); MET=np.where(FB,-PEN,MET)
    # 1. walls per 1 m station and level
    S1=np.arange(0.5, Sg[-1]+1e-9, 1.0); LF=np.linspace(0,1,NL)
    FW=np.zeros((len(S1),NL)); HWl=np.zeros_like(FW); inside=np.ones(NU,bool)   # v6: base free, the drive moves to the stope
    ii,jj=np.meshgrid(np.arange(NU+1),np.arange(NU+1),indexing='ij'); valid=jj-ii>=nmin
    def lode_span(tb,mb):
        """v8: lodes = runs of >= CUT cells (gaps <= 0.5 m bridged). Of all spans from one lode to another (<= MAX_W wide,
        >= MIN_W), take the one with the most ore whose diluted grade > SPAN_G; None when no span reaches SPAN_G"""
        g=np.where(tb>0,mb/np.maximum(tb,1e-12),0); ore=(g>=CUT)&(tb>0)
        idx=np.nonzero(ore)[0]; runs=[]
        for k in idx:
            if runs and k-runs[-1][1]<=2: runs[-1][1]=k+1          # bridge gaps <= 0.5 m
            else: runs.append([k,k+1])
        if not runs: return None
        ct=np.r_[0,np.cumsum(tb)]; cm=np.r_[0,np.cumsum(mb)]; co=np.r_[0,np.cumsum(np.where(ore,tb,0))]; best=None
        for x in range(len(runs)):
            for y in range(x,len(runs)):
                a,b=runs[x][0],runs[y][1]
                if (b-a)*DU>MAX_W: break
                if b-a<nmin:
                    c=(a+b)//2; a=max(0,min(NU-nmin,c-nmin//2)); b=a+nmin
                t=ct[b]-ct[a]
                if t<=0: continue
                gr=(cm[b]-cm[a])/t; o=co[b]-co[a]
                if SPAN_PICK=='value': o=(cm[b]-cm[a])-CUT*t   # v10 (Kennet 2026-10-13, 3CA5476): metal above cut-off, not ore tonnes
                if gr>SPAN_G and (best is None or o>best[0]+1e-9 or (abs(o-best[0])<=1e-9 and gr>best[1])): best=(o,gr,a,b)
        return None if best is None else (best[2],best[3])
    def lode_cands(tb,mb):
        """v10: every span from one lode to another (<= MAX_W, >= MIN_W) diluting above SPAN_G: (a, b, metal above cut-off, grade)"""
        g=np.where(tb>0,mb/np.maximum(tb,1e-12),0); ore=(g>=CUT)&(tb>0)
        idx=np.nonzero(ore)[0]; runs=[]
        for k in idx:
            if runs and k-runs[-1][1]<=2: runs[-1][1]=k+1
            else: runs.append([k,k+1])
        ct=np.r_[0,np.cumsum(tb)]; cm=np.r_[0,np.cumsum(mb)]; out=[]
        for x in range(len(runs)):
            for y in range(x,len(runs)):
                a,b=runs[x][0],runs[y][1]
                if (b-a)*DU>MAX_W: break
                if b-a<nmin:
                    c=(a+b)//2; a=max(0,min(NU-nmin,c-nmin//2)); b=a+nmin
                t=ct[b]-ct[a]
                if t<=0: continue
                gr=(cm[b]-cm[a])/t
                if gr>SPAN_G: out.append((a,b,(cm[b]-cm[a])-CUT*t,gr))
        return out
    best_g=0; WEAK=np.zeros(FW.shape,bool)
    CANDS=[[None]*NL for _ in S1]; FALL=[[None]*NL for _ in S1]
    for a,s0 in enumerate(S1):
        sm=np.abs(Sg-s0)<=2.5; H=np.median(Htop[sm])
        for l,f in enumerate(LF):
            band=np.abs(HG-f*H)<=H/(NL-1)/2+0.1
            prof=V[sm][:,:,band].sum(axis=(0,2))
            if l==0: prof=np.where(inside,prof,-1e9)
            cs=np.r_[0,np.cumsum(prof)]; val=np.where(valid,cs[jj]-cs[ii],-np.inf); k=np.argmax(val); i,j=np.unravel_index(k,val.shape)
            if SPAN_PICK=='track':
                CANDS[a][l]=lode_cands(T[sm][:,:,band].sum(axis=(0,2)),MET[sm][:,:,band].sum(axis=(0,2)))
                if l==0: CANDS[a][l]=[c for c in CANDS[a][l] if inside[c[0]:c[1]].all()]
            span=lode_span(T[sm][:,:,band].sum(axis=(0,2)),MET[sm][:,:,band].sum(axis=(0,2)))
            if span is not None: i,j=span
            elif val[i,j]<=0:
                WEAK[a,l]=True
                mp=MET[sm][:,:,band].sum(axis=(0,2))
                if l==0: mp=np.where(inside,mp,-1e9)
                cm=np.r_[0,np.cumsum(mp)]; w=cm[nmin:]-cm[:-nmin]-1e-6*np.abs(UG[:len(cm)-nmin]+DU*nmin/2); i=int(np.argmax(w)); j=i+nmin
            FW[a,l]=UG[i]-DU/2; HWl[a,l]=UG[j-1]+DU/2
    if SPAN_PICK=='shape':
        # v10c (Kennet 2026-10-13, 3CA5476 "you chose two lodes, and there is one higher grade"): pick ONE stope shape per station
        # for all levels at once - a width W (2.2-14 m) and a centre that may lean up to 1.2 m per level (dip) - maximising the
        # metal above cut-off of the whole column, t*(g-2.5); along strike the centre drifts <= 0.75 m per m (Viterbi). A wide
        # span that only pays in one band no longer drags the walls off the higher-grade lode below/above it.
        from scipy.ndimage import maximum_filter1d
        # v10d: the centre follows a straight dip line up the column (c0 + d*level, |d| <= 1.2 m per level) so the section stays
        # one coherent lode, and the Viterbi state is (centre, width): the width may change one step per m along strike only.
        WS=[MIN_W,3.0,4.0,5.5,7.5,10.0,14.0]; nA=len(S1); nU=len(UG); nW=len(WS)
        DIPS=np.linspace(-1.2,1.2,9)
        BEST=np.full((nA,nW,nU),-1e9); DARG=np.zeros((nA,nW,nU),int)
        for a,s0 in enumerate(S1):
            sm=np.abs(Sg-s0)<=2.5; H=np.median(Htop[sm])
            for wi,Wd in enumerate(WS):
                nw=max(1,int(round(Wd/DU))); VL=[]
                for l,f in enumerate(LF):
                    band=np.abs(HG-f*H)<=H/(NL-1)/2+0.1
                    pv=(MET[sm][:,:,band]-SHAPE_REF*T[sm][:,:,band]).sum(axis=(0,2)); cs=np.r_[0,np.cumsum(pv)]
                    st=np.arange(nU)-nw//2; ok=(st>=0)&(st+nw<=nU)
                    v=np.full(nU,-1e9); v[ok]=cs[st[ok]+nw]-cs[st[ok]]; VL.append(v)
                tot=np.zeros((len(DIPS),nU))
                for di,d in enumerate(DIPS):
                    for l in range(NL):
                        sh=int(round(d*l/DU)); idx=np.arange(nU)+sh; ok=(idx>=0)&(idx<nU)
                        tot[di]+=np.where(ok,VL[l][np.clip(idx,0,nU-1)],-1e9)
                DARG[a,wi]=tot.argmax(0); BEST[a,wi]=tot.max(0)
        if BEST.max()>0:
            # Viterbi on 5 m bins (the section spacing): centre moves <= MAX_STEP and width one step per bin, the same
            # limits the drillable sections hold later, so the path is never smeared by strike_limit/smoothing
            nb=int(np.ceil(nA/SEC)); bi=np.minimum((np.arange(nA)//int(SEC)),nb-1)
            BB=np.zeros((nb,nW,nU)); np.add.at(BB,bi,BEST)
            DUc=np.abs(UG[:,None]-UG[None,:])<=SHAPE_DRIFT*SEC+1e-6
            acc=BB[0].copy(); bp=np.zeros((nb,nW,nU,2),int)
            for a in range(1,nb):
                nxt=np.full((nW,nU),-1e18)
                for wi in range(nW):
                    for wj in range(max(0,wi-1),min(nW,wi+2)):
                        m=np.where(DUc,acc[wj][None,:],-1e18); k=m.argmax(1); val=m[np.arange(nU),k]
                        bt=val>nxt[wi]; nxt[wi]=np.where(bt,val,nxt[wi]); bp[a,wi,bt,0]=wj; bp[a,wi,bt,1]=k[bt]
                acc=nxt+BB[a]
            wi,k=np.unravel_index(int(np.argmax(acc)),acc.shape); bw=np.zeros(nb,int); bu=np.zeros(nb,int)
            for a in range(nb-1,-1,-1):
                bw[a]=wi; bu[a]=k
                if a>0: wi,k=bp[a,wi,k]
            # dip per bin: best dip of the bin's (w,u) summed over its stations
            bc=np.array([S1[bi==q].mean() for q in range(nb)])
            for a in range(nA):
                q=bi[a]; wi=bw[q]; k=bu[q]
                c0=np.interp(S1[a],bc,UG[bu]); Wd=np.interp(S1[a],bc,[WS[w] for w in bw])
                dd=[DIPS[DARG[x,bw[q],bu[q]]] for x in np.nonzero(bi==q)[0]]; d=float(np.median(dd))
                for l in range(NL):
                    c=c0+d*l; FW[a,l]=c-Wd/2; HWl[a,l]=c+Wd/2; WEAK[a,l]=BB[q,wi,k]<=0
            pu=np.array([bu[bi[a]] for a in range(nA)]); pw=np.array([bw[bi[a]] for a in range(nA)])
            if os.environ.get('DBG_SHAPE'):
                for a in range(0,nA,3):
                    ks=[np.argmin(np.abs(UG-x)) for x in np.arange(-8,8.1,1.0)]; print('   prof W2.2', ' '.join('%5.0f'%BEST[a,0,k] for k in ks))
                for a in range(0,nA,3): print('st %.1f c0 %.1f W %.1f dip %.1f sc %.0f'%(S1[a],UG[pu[a]],WS[pw[a]],DIPS[DARG[a,pw[a],pu[a]]],BEST[a,pw[a],pu[a]]))
    if SPAN_PICK=='track':
        # v10b (Kennet 2026-10-13, 3CA5476): one lode track per drive. Viterbi over the 1 m stations: the track centre may drift
        # <= 0.75 m per m along strike, each station scores the best span containing the centre at every level (metal above
        # cut-off). Then every station/level takes the best span on the track, so the walls follow one lode (or both lodes where
        # the span holding both is worth more) instead of jumping between lodes and being smoothed into the waste between them.
        UC=UG[::2]; nA=len(S1); SC=np.zeros((nA,len(UC)))
        for a in range(nA):
            for l in range(NL):
                cl=CANDS[a][l] or []
                if not cl: continue
                lo=np.array([UG[c[0]]-DU/2 for c in cl]); hi=np.array([UG[c[1]-1]+DU/2 for c in cl]); v=np.array([c[2] for c in cl])
                ins=(UC[:,None]>=lo[None,:])&(UC[:,None]<=hi[None,:])
                SC[a]+=np.where(ins,v[None,:],0).max(axis=1)
        if SC.max()>0:
            D=np.abs(UC[:,None]-UC[None,:]); PEN=np.where(D<=0.75,0,-1e3*(D-0.75))
            acc=SC[0].copy(); bp=np.zeros((nA,len(UC)),int)
            for a in range(1,nA):
                m=acc[None,:]+PEN; bp[a]=np.argmax(m,axis=1); acc=m[np.arange(len(UC)),bp[a]]+SC[a]
            k=int(np.argmax(acc)); trk=np.zeros(nA)
            for a in range(nA-1,-1,-1): trk[a]=UC[k]; k=bp[a,k]
            for a in range(nA):
                for l in range(NL):
                    cl=CANDS[a][l] or []
                    on=[c for c in cl if UG[c[0]]-DU/2<=trk[a]<=UG[c[1]-1]+DU/2]
                    if not on: on=[c for c in cl if abs((UG[c[0]]+UG[c[1]-1])/2-trk[a])<=2.0]
                    if on:
                        c=max(on,key=lambda c:c[2]); FW[a,l]=UG[c[0]]-DU/2; HWl[a,l]=UG[c[1]-1]+DU/2; WEAK[a,l]=False
                    else:
                        WEAK[a,l]=True; FW[a,l]=trk[a]-MIN_W/2; HWl[a,l]=trk[a]+MIN_W/2
    def smooth(A,k=5):
        B=np.array([np.convolve(np.pad(A[:,l],k//2,mode='edge'),np.ones(k)/k,'valid') for l in range(A.shape[1])]).T
        C=B.copy(); C[:,1:-1]=(B[:,:-2]+2*B[:,1:-1]+B[:,2:])/4; return C
    # v9 (Kennet 2026-10-13, 3CA0D1B B46): a station with no ore window, or whose window jumps > 3 m off its
    # neighbours (another lode, a drive end), no longer drags the walls along strike: it takes the walls
    # interpolated from the good stations at that level.
    for l in range(NL):
        c=(FW[:,l]+HWl[:,l])/2; med=np.array([np.median(c[max(0,a-5):a+6]) for a in range(len(c))])
        bad=WEAK[:,l]|(np.abs(c-med)>3.0)
        if bad.any() and (~bad).sum()>=2:
            for A in (FW,HWl): A[bad,l]=np.interp(S1[bad],S1[~bad],A[~bad,l])
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
    # v8: inside of a bend, walls stop 0.3 m short of where two neighbouring section lines cross
    HWCAP=np.full(len(ss),np.inf); FWCAP=np.full(len(ss),-np.inf)
    P_,N_,_=drv.frame(ss)
    for q in range(len(ss)-1):
        n1,n2=N_[q],N_[q+1]; det=-n1[0]*n2[1]+n2[0]*n1[1]
        if abs(det)<1e-4: continue
        dp=P_[q+1]-P_[q]; u1=(dp[0]*(-n2[1])-(-n2[0])*dp[1])/det; u2=(n1[0]*dp[1]-n1[1]*dp[0])/det
        if abs(u1)>40: continue
        if u1>0: HWCAP[q]=min(HWCAP[q],u1-0.3); HWCAP[q+1]=min(HWCAP[q+1],u2-0.3)
        else: FWCAP[q]=max(FWCAP[q],u1+0.3); FWCAP[q+1]=max(FWCAP[q+1],u2+0.3)
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
            if np.any(hw[si]>HWCAP[si]): hw[si]=np.minimum(hw[si],HWCAP[si]); fw[si]=np.minimum(fw[si],hw[si]-MIN_W)
            if np.any(fw[si]<FWCAP[si]): fw[si]=np.maximum(fw[si],FWCAP[si]); hw[si]=np.maximum(hw[si],fw[si]+MIN_W)
    for _ in range(6):
        project()
        for A in (fw,hw):
            for q in range(NL): A[:,q]=strike_limit(A[:,q].copy())
    project()
    def section_ok(si):
        f,h=fw[si],hw[si]
        if np.any(h-f<MIN_W-1e-9): return False
        if np.any(h>HWCAP[si]+1e-9) or np.any(f<FWCAP[si]-1e-9): return False
        for d in (np.diff(-f),np.diff(h)):
            if np.any(np.abs(d)>MO[si]+1e-9) or np.any(np.diff(d)>1e-9): return False
        return True
    TOPC=np.full(len(ss),np.inf)   # v8: top cut under another drive's block above (shape kept, top cut flat)
    def inmask(a,b):   # stations of Sg between sections a..b, boolean (n,NU,NH) of cells inside the shape
        sm=(Sg>ss[a])&(Sg<ss[b]); S=Sg[sm]
        H=np.interp(S,ss,Hs); cut=min(TOPC[a],TOPC[b])
        f=np.array([np.interp(S,ss,fw[:,q]) for q in range(NL)]).T; h=np.array([np.interp(S,ss,hw[:,q]) for q in range(NL)]).T
        fr=np.clip(HG[None,:]/H[:,None],0,1)*(NL-1)
        fz=np.array([np.interp(fr[n],np.arange(NL),f[n]) for n in range(len(S))]); hz=np.array([np.interp(fr[n],np.arange(NL),h[n]) for n in range(len(S))])
        m=(UG[None,:,None]>=fz[:,None,:])&(UG[None,:,None]<=hz[:,None,:])&(HG[None,None,:]<=np.minimum(H,cut)[:,None,None])
        return sm,m
    def ev(a,b):
        sm,m=inmask(a,b); t=T[sm]*m; return np.array([t.sum(),(t*G[sm]).sum(),(t*(G[sm]>=CUT)).sum()])
    for si in range(len(ss)):
        if not section_ok(si):
            for A in (fw,hw):
                top=A[si,0]+np.clip(A[si,-1]-A[si,0],-MO[si]*(NL-1),MO[si]*(NL-1))
                A[si]=A[si,0]+(top-A[si,0])*np.arange(NL)/(NL-1)
    # nudge walls, evaluated locally on the two neighbouring blocks. v6: besides single wall points, move the whole wall
    # (translate) or tilt it about its base or top, so a wall can follow ore without breaking the convex/dip rules
    PATS=[np.eye(NL)[q] for q in range(NL)]+[np.ones(NL),np.linspace(0,1,NL),np.linspace(1,0,NL)]
    for _ in range(24):
        changed=0
        for si in range(len(ss)):
            a,b=max(si-1,0),min(si+1,len(ss)-1)
            for P in PATS:
                for A,sign in ((fw,-1),(hw,1)):
                    for step in (0.5,0.25,-0.25):
                        e0=ev(a,b); old=A[si].copy(); A[si]=old+sign*step*P
                        if section_ok(si) and all(np.all(np.abs(A[si]-A[x])<=MAX_STEP+1e-9) for x in (si-1,si+1) if 0<=x<len(ss)):
                            e1=ev(a,b); dt,dm,do=e1-e0
                            good=dt!=0 and (dm/dt>=MARG or do/dt>=ORE_SHARE)
                            if SPAN_PICK=='shape': good=dt!=0 and dm/dt>=SHAPE_REF   # v10d: a nudge adds only slices that pay on their own grade
                            if (step>0 and dt>0 and good) or (step<0 and dt<0 and not good): changed+=1; break
                        A[si]=old
        if not changed: break
    # v8: push walls out of other drives' blocks (whole-wall moves); blocks that can't clear are dropped below
    if FB.any():
        for _ in range(80):
            moved=0
            for si in range(len(ss)-1):
                sm,m=inmask(si,si+1); hitm=m&FB[sm]
                if not hitm.any(): continue
                hz=HG[np.nonzero(hitm)[2]]; Hb=min(Hs[si],Hs[si+1],TOPC[si],TOPC[si+1])
                if hz.min()>0.4*Hb and hz.min()-DH>=HMIN:   # another drive's block sits above: cut the top under it
                    TOPC[si]=TOPC[si+1]=min(TOPC[si],TOPC[si+1],hz.min()-DH); moved+=1; continue
                uc=UG[np.nonzero(hitm)[1]].mean(); mid=(fw[si:si+2].mean()+hw[si:si+2].mean())/2
                for x in (si,si+1):
                    if uc<mid: fw[x]=fw[x]+0.25
                    else: hw[x]=hw[x]-0.25
                moved+=1
            if not moved: break
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
        if FB.any():
            sm_,m_=cells(c0,c1,zlo,top)
            if (m_&FB[sm_]).any() or np.any(hw[bi:bi+2]-fw[bi:bi+2]<MIN_W-1e-9): return None   # v8: never overlaps another drive's block
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
        top=None if not np.isfinite(min(TOPC[bi],TOPC[bi+1])) else float(min(TOPC[bi],TOPC[bi+1]))   # full height, or cut flat under a block above (v8)
        ton,grade,_,_=stats(c0,c1,0.,top)
        if ton<=0 or (not keep_all and grade<=KEEP_G): continue   # OD/trial blocks kept even with no grade
        best=None
        Hmin=min(Hs[bi],Hs[bi+1],top if top is not None else np.inf)
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
