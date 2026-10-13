"""STOPE FINAL V4 (Kennet 2026-10-13, after 3CA4872): veins the drive-based blocks missed (e.g. HB0143 crossing the
ST drive B73 at ~90 deg). >= 2.6 g/t unmined ore outside the live STOPE FINAL V3 stopes -> pockets -> one vein string
per 22 m level band along the pocket's OWN strike (PCA), snapped onto the lode -> 5 m blocks with the same design8 v9
rules, the live V3 stopes as forbidden space (fixed winners), no overlaps between the new strings."""
import sys; sys.path.insert(0,'/home/claude/work')
from st_common import *
import json, os, csv
from scipy.spatial import cKDTree
from raster import PX,PY,PZ,NX,NY,NZ,SX,SY,SZ,RX,RY,RZ
from st_stage2v3 import snap_line
from design8 import inside_cols
CUTP=2.6; MIN_T=1000; GAP=2.0
W='/home/claude/work/st_final_v4/'
def v3_live():
    live={r['name']:r['GROUP'] for r in csv.DictReader(open('/home/claude/kd/v3_live_groups.csv'))}
    return [s for s in pickle.load(open('/home/claude/stp/final_v3/stope_geoms_v3.pkl','rb')) if s['name'] in live], live
def v2_strings():
    geo=collections.defaultdict(list); act={}
    rep={r['id']:r for r in csv.DictReader(open('/home/claude/kd/v2_attrs.csv'))}
    for r in csv.DictReader(open('/home/claude/kd/v2_geom.csv')): geo[r['id']].append((float(r['x']),float(r['y']),float(r['z'])))
    for r in csv.DictReader(open('/home/claude/kd/kd_strings.csv')):
        h=r['handle']
        if h in rep and rep[h]['action'] in ('replace','new','delete'): continue
        geo[h].append((float(r['x']),float(r['y']),float(r['z'])))
    for r in csv.DictReader(open('/home/claude/kd/kd_attrs.csv')): act[r['handle']]=r['ACT_TYPE']
    for k,r in rep.items(): act[k]=r['ACT_TYPE']
    return {k:(act.get(k,''),np.array(v)-ORIGIN) for k,v in geo.items() if len(v)>=2}
def resolve(blocks, J, fixed=(), rounds=5, log=print):
    """no overlaps between drives: the loser of each clash is designed again with the winners' blocks as forbidden space.
    stacked (floors > 5 m apart): upper wins; side by side: more t*(g-2.6) wins; drives in `fixed` always win."""
    FZ={p:float(np.median(J[p][2][:,2])) for p in J}
    value=lambda p:sum(b['t']*max(b['g']-2.6,0) for b in blocks[p])+1e-3*sum(b['ore_t'] for b in blocks[p])
    def beats(q,p,rank):
        if (q in fixed)!=(p in fixed): return q in fixed
        if abs(FZ[q]-FZ[p])>5: return FZ[q]>FZ[p]
        return rank[q]<rank[p]
    def conflicts():
        flat=[b for p in blocks for b in blocks[p]]; out=collections.defaultdict(float)
        for i,j in pairs(flat):
            if flat[i]['pid']==flat[j]['pid'] or (flat[i]['pid'] in fixed and flat[j]['pid'] in fixed): continue
            v=overlap(flat[i],flat[j])
            if v>=MINV: out[tuple(sorted((flat[i]['pid'],flat[j]['pid'])))]+=v
        return out
    for it in range(rounds):
        C=conflicts(); log('  round %d: %d drive pairs overlap, %.0f m3'%(it,len(C),sum(C.values())))
        if not C: return blocks
        order=sorted(blocks,key=value,reverse=True); rank={p:i for i,p in enumerate(order)}
        worse={(b if beats(a,b,rank) else a) for a,b in C}
        redo=[]
        for p in worse:
            P=J[p][2]+ORIGIN; lo=P.min(0)-np.r_[19,19,3]; hi=P.max(0)+np.r_[19,19,25]; F=[]
            for q,bl in blocks.items():
                if q==p or not beats(q,p,rank): continue
                for b in bl:
                    V=np.asarray(b['V'])
                    if np.all(V.max(0)>=lo) and np.all(V.min(0)<=hi): F.append((V-ORIGIN,np.asarray(b['F'])))
            redo.append(tuple(J[p][:4])+(F,))
        with mp.Pool(4,initializer=init) as pool:
            for pid,out,st in pool.imap_unordered(design.run,redo): blocks[pid]=J[pid][4](out)
    order=sorted(blocks,key=value,reverse=True); rank={p:i for i,p in enumerate(order)}
    flat=[b for p in blocks for b in blocks[p]]; drop=set()
    for i,j in pairs(flat):
        if flat[i]['pid']!=flat[j]['pid'] and not (flat[i]['pid'] in fixed and flat[j]['pid'] in fixed) and overlap(flat[i],flat[j])>=MINV:
            drop.add(id(flat[j]) if beats(flat[i]['pid'],flat[j]['pid'],rank) else id(flat[i]))
    for p in blocks: blocks[p]=[b for b in blocks[p] if id(b) not in drop]
    log('  dropped %d blocks still overlapping'%len(drop)); return blocks

if __name__=='__main__':
    S=load_st(); STOPES,live=v3_live(); V2=v2_strings()
    print('live V3 stopes',len(STOPES),'V2 strings',len(V2))
    comp=np.load('s2_comp.npy'); TD=np.load('s2_td.npy',mmap_mode='r'); AU=np.load('s2_au.npy',mmap_mode='r'); MN=np.load('s2_mn.npy',mmap_mode='r')
    sel=np.nonzero((np.asarray(AU)>=CUTP)&(np.asarray(TD)>0)&(np.asarray(MN)==0))[0]
    inv=np.full(comp.max()+1,-1,np.int64); pidx=np.nonzero(comp>=0)[0]; inv[comp[pidx]]=pidx
    c=sel//(RX*RY*RZ); r=sel%(RX*RY*RZ); si=r//(RY*RZ); sj=(r//RZ)%RY; sk=r%RZ
    p=inv[c]; k=p%NZ; j=(p//NZ)%NY; i=p//(NZ*NY)
    P=np.c_[i*PX+(si+0.5)*SX, j*PY+(sj+0.5)*SY, k*PZ+(sk+0.5)*SZ]; t=np.asarray(TD)[sel]*SX*SY*SZ; g=np.asarray(AU)[sel]
    # outside the live V3 stopes (+ GAP m skin so a new stope never sits wall to wall with one)
    FORB=[(np.asarray(s['V'],float)-ORIGIN,np.asarray(s['F'],int)) for s in STOPES]
    IN=np.zeros(len(P),bool)
    for V,F in FORB:
        lo=V.min(0)-GAP; hi=V.max(0)+GAP; cc=np.nonzero(np.all((P>=lo)&(P<=hi),axis=1))[0]
        if len(cc)==0: continue
        ins=np.zeros(len(cc),bool)
        for dx,dy in ((0,0),(GAP,0),(-GAP,0),(0,GAP),(0,-GAP)):
            ins|=inside_cols(V,F,P[cc,0]+dx+1e-4,P[cc,1]+dy+1e-4,P[cc,2],np.zeros(1))[:,0]
        IN[cc[ins]]=True
    print('>=2.6 unmined ore %.0f kt @ %.2f; inside/next to live V3 stopes %.0f kt; outside %.0f kt @ %.2f'%(t.sum()/1e3,(t*g).sum()/t.sum(),t[IN].sum()/1e3,t[~IN].sum()/1e3,(t[~IN]*g[~IN]).sum()/t[~IN].sum()))
    P,t,g=P[~IN],t[~IN],g[~IN]
    ods=[v for a,v in V2.values() if a=='OD']; odpts=np.vstack([np.c_[q,np.full(len(q),np.median(q[:,2]))] for q in ods])
    key=np.floor(P/10).astype(int); kt=collections.defaultdict(float)
    for kk,tt in zip(map(tuple,key),t): kt[kk]+=tt
    good={kk for kk,v in kt.items() if v>=150}; lab={}; n=0
    for kk in good:
        if kk in lab: continue
        st=[kk]; lab[kk]=n
        while st:
            cc=st.pop()
            for dx in (-1,0,1):
                for dy in (-1,0,1):
                    for dz in (-1,0,1):
                        q=(cc[0]+dx,cc[1]+dy,cc[2]+dz)
                        if q in good and q not in lab: lab[q]=n; st.append(q)
        n+=1
    pl=np.array([lab.get(kk,-1) for kk in map(tuple,key)]); lines=[]
    for pk in range(n):
        mm0=pl==pk
        if t[mm0].sum()<MIN_T: continue
        cen=np.average(P[mm0],axis=0,weights=t[mm0])
        F0=odpts[np.argmin(np.hypot(odpts[:,0]-cen[0],odpts[:,1]-cen[1])+np.abs(odpts[:,2]-cen[2])),3]   # V2 level series
        zmin,zmax=P[mm0,2].min(),P[mm0,2].max()
        for kz in range(-14,15):
            F=F0+22*kz
            if F+22<zmin or F>zmax: continue
            mm=mm0&(P[:,2]>=F)&(P[:,2]<=F+22)
            if t[mm].sum()<MIN_T: continue
            xy=P[mm,:2]; w=t[mm]; mu=np.average(xy,axis=0,weights=w); C=np.cov((xy-mu).T,aweights=w); ev,evec=np.linalg.eigh(C); ax=evec[:,-1]
            s=(xy-mu)@ax; bins=np.floor(s/5).astype(int); pts=[]
            for bb in range(bins.min(),bins.max()+1):
                q=bins==bb
                if w[q].sum()>=20: pts.append((bb,*np.average(xy[q],axis=0,weights=w[q])))
            runs=[[pts[0]]] if pts else []
            for pp in pts[1:]: (runs[-1].append(pp) if pp[0]-runs[-1][-1][0]<=2 else runs.append([pp]))
            for rr in runs:
                if len(rr)<2: continue
                a=np.array([pp[1:] for pp in rr]); a2=a.copy()
                if len(a)>2: a2[1:-1]=(a[:-2]+a[1:-1]+a[2:])/3
                e0=a2[0]+(a2[0]-a2[1])/np.linalg.norm(a2[0]-a2[1])*2.5; e1=a2[-1]+(a2[-1]-a2[-2])/np.linalg.norm(a2[-1]-a2[-2])*2.5
                a2=np.vstack([e0,a2,e1]); ton=t[mm][np.isin(bins,[pp[0] for pp in rr])].sum()
                if ton<MIN_T: continue
                lines.append(dict(pocket=pk,level=float(F+ORIGIN[2]),pts=(np.c_[a2,np.full(len(a2),F)]+ORIGIN).round(2).tolist(),ore_t=float(ton),az=float(np.degrees(np.arctan2(ax[0],ax[1]))%180)))
    from raster import Model as _M; Mo=_M('s2'); mv=[]
    for l in lines:
        p0=np.array(l['pts'])-ORIGIN; p1=snap_line(Mo,p0,p0[0,2]); mv.append(np.hypot(*(p1[:,:2]-p0[:,:2]).T).mean()); l['pts']=(p1+ORIGIN).round(2).tolist()
    print('vein strings',len(lines),'through %.0f t of missed ore; snap mean move %.1f m'%(sum(l['ore_t'] for l in lines),np.mean(mv)))
    json.dump(lines,open(W+'vein_strings.json','w'))
    dec=np.vstack([dens(v['pts']) for v in S.values() if v['act']=='DEC']+[dens(v) for a,v in V2.values() if a=='DEC'])
    D=[dens(v['pts']) for v in S.values()]+[dens(v) for a,v in V2.values()]; XD=[dens(np.array(l['pts'])-ORIGIN) for l in lines]
    J={}; jobs=[]
    for i,l in enumerate(lines):
        pid='V%03d'%i; p=np.array(l['pts'])-ORIGIN; c=p[:,:2].mean(0)
        others=np.vstack([x for x in D+[v for jj,v in enumerate(XD) if jj!=i] if np.hypot(*(x[:,:2].mean(0)-c))<400])
        J[pid]=(pid,'OD',p,others,(lambda out,pid=pid,lev=l['level']:clean(out,pid,dec,lev,extra=True)))
        lo=p.min(0)-np.r_[19,19,3]; hi=p.max(0)+np.r_[19,19,30]
        jobs.append((pid,'OD',p,others,[(V,F) for V,F in FORB if np.all(V.max(0)>=lo) and np.all(V.min(0)<=hi)]))
    blocks={}
    with mp.Pool(4,initializer=init) as pool:
        for pid,out,st in pool.imap_unordered(design.run,jobs): blocks[pid]=J[pid][4](out)
    print('vein strings with blocks:',sum(1 for b in blocks.values() if b))
    # live V3 stopes as fixed winners
    fixed=set()
    for n_,s in enumerate(STOPES):
        pid='V3_%d'%n_; V=np.asarray(s['V'],float); fixed.add(pid)
        blocks[pid]=[dict(pid=pid,V=V,F=np.asarray(s['F'],int),t=s['t'],g=s['g'],ore_t=0,bi=0,part='')]
        J[pid]=(pid,'OD',np.array([[V[:,0].mean(),V[:,1].mean(),V[:,2].min()]])-ORIGIN,None,None)
    pickle.dump({p:v for p,v in blocks.items()},open(W+'pre_resolve.pkl','wb'))
    blocks=resolve(blocks,J,fixed=fixed)
    allk=[b for p in blocks for b in blocks[p] if p not in fixed]; allk,nd=self_fix(allk); print('kink drops',nd)
    print(summary(allk,'vein string blocks'))
    pickle.dump(allk,open(W+'kept_v4.pkl','wb'))
