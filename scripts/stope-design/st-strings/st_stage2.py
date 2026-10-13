"""stage 2: >= 2.6 g/t unmined ore within 50 m of Kennet's ST strings that the ST blocks miss (> 4 m away) ->
conceptual extra strings (X000...) on the nearest ST ore drive's 22 m level series, blocks along them (ST blocks
always win any overlap), intersections."""
from st_common import *
import json, os
from scipy.spatial import cKDTree
from raster import PX,PY,PZ,NX,NY,NZ,SX,SY,SZ,RX,RY,RZ
CUTP=2.6; RAD=50.0; MIN_T=1000
MODEL=os.environ.get('MODEL','s')  # raster prefix; 's2' adds the target-box cells (Stope Final run)
# Stope Final (Kennet 2026-10-13): optional TARGET_BOXES json [[x0,y0,z0,x1,y1,z1],...] = areas his ST task-solid stopes reach with no ST string within 50 m; ore inside them counts too
BOX=np.array(json.load(open(os.environ['TARGET_BOXES']))) if os.environ.get('TARGET_BOXES') else np.zeros((0,6))
if __name__=='__main__':
    S=load_st(); kept=pickle.load(open(W+'kept_st.pkl','rb'))
    comp=np.load(MODEL+'_comp.npy'); TD=np.load(MODEL+'_td.npy',mmap_mode='r'); AU=np.load(MODEL+'_au.npy',mmap_mode='r'); MN=np.load(MODEL+'_mn.npy',mmap_mode='r')
    sel=np.nonzero((np.asarray(AU)>=CUTP)&(np.asarray(TD)>0)&(np.asarray(MN)==0))[0]
    inv=np.full(comp.max()+1,-1,np.int64); pidx=np.nonzero(comp>=0)[0]; inv[comp[pidx]]=pidx
    c=sel//(RX*RY*RZ); r=sel%(RX*RY*RZ); si=r//(RY*RZ); sj=(r//RZ)%RY; sk=r%RZ
    p=inv[c]; k=p%NZ; j=(p//NZ)%NY; i=p//(NZ*NY)
    P=np.c_[i*PX+(si+0.5)*SX, j*PY+(sj+0.5)*SY, k*PZ+(sk+0.5)*SZ]; t=np.asarray(TD)[sel]*SX*SY*SZ; g=np.asarray(AU)[sel]
    allst=np.vstack([dens(v['pts']) for v in S.values()]); d50,_=cKDTree(allst).query(P,distance_upper_bound=RAD+1,workers=4)
    inbox=np.zeros(len(P),bool)
    for bx in BOX: inbox|=np.all((P+ORIGIN>=bx[:3])&(P+ORIGIN<=bx[3:]),axis=1)
    if len(BOX): print('>=2.6 ore in the target boxes beyond 50 m: %.0f t'%t[inbox&(d50>RAD)].sum())
    m=(d50<=RAD)|inbox; P,t,g=P[m],t[m],g[m]
    print('>=2.6 ore within 50 m of the ST strings: %.3f Mt @ %.2f'%(t.sum()/1e6,(t*g).sum()/t.sum()))
    V=np.unique(np.concatenate([b['vox'] for b in kept])); Pv=np.c_[(V//2000)//2000,(V//2000)%2000,V%2000].astype(np.float32)+0.5
    dd,_=cKDTree(Pv).query(P.astype(np.float32),distance_upper_bound=1e4,workers=4); inb=dd<=0.9; far=dd>4.0
    print('  inside ST blocks %.3f Mt (%.0f%%); more than 4 m from any ST block %.3f Mt @ %.2f'%(t[inb].sum()/1e6,100*t[inb].sum()/t.sum(),t[far].sum()/1e6,(t[far]*g[far]).sum()/max(t[far].sum(),1)))
    cap0=(t[inb].sum(),t.sum())
    P,t,g=P[far],t[far],g[far]
    ods=[v['pts'] for v in S.values() if v['act']=='OD']; odpts=np.vstack([np.c_[q,np.full(len(q),np.median(q[:,2]))] for q in ods])
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
        F0=odpts[np.argmin(np.hypot(odpts[:,0]-cen[0],odpts[:,1]-cen[1])+np.abs(odpts[:,2]-cen[2])),3]
        zmin,zmax=P[mm0,2].min(),P[mm0,2].max()
        for kz in range(-12,13):
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
                lines.append(dict(pocket=pk,level=float(F+ORIGIN[2]),pts=(np.c_[a2,np.full(len(a2),F)]+ORIGIN).round(2).tolist(),ore_t=float(ton)))
    print(len(lines),'extra strings through %.0f t of missed ore'%sum(l['ore_t'] for l in lines))
    json.dump(lines,open(W+'extra_strings.json','w'))
    # design along the extra strings; ST blocks always win
    dec=np.vstack([dens(v['pts']) for v in S.values() if v['act']=='DEC'])
    D=[dens(v['pts']) for v in S.values()]; XD=[dens(np.array(l['pts'])-ORIGIN) for l in lines]
    J={}
    for i,l in enumerate(lines):
        pid='X%03d'%i; p=np.array(l['pts'])-ORIGIN; c=p[:,:2].mean(0)
        others=np.vstack([x for x in D+[v for jj,v in enumerate(XD) if jj!=i] if np.hypot(*(x[:,:2].mean(0)-c))<400])
        J[pid]=(pid,'OD',p,others,(lambda out,pid=pid,lev=l['level']:clean(out,pid,dec,lev,extra=True)))
    blocks={}
    with mp.Pool(4,initializer=init) as pool:
        for pid,out,st in pool.imap_unordered(design.run,[j[:4] for j in J.values()]): blocks[pid]=J[pid][4](out)
    print('extra strings with blocks:',sum(1 for b in blocks.values() if b))
    # ST drives as fixed winners: put their blocks in, with a no-op redesign entry (never chosen as loser)
    stb=collections.defaultdict(list)
    for b in kept: stb[b['pid']].append(b)
    for pid,bl in stb.items():
        blocks[pid]=bl; J[pid]=(pid,'OD',S[pid]['pts'],None,None)
    blocks=resolve(blocks,J,fixed=set(stb))
    allk=[b for p in blocks for b in blocks[p]]; allk,nd=self_fix(allk); print('kink drops',nd)
    X=[b for b in allk if b['pid'].startswith('X')]
    print(summary([b for b in allk if not b['pid'].startswith('X')],'ST blocks')); print(summary(X,'extra string blocks'))
    pickle.dump(allk,open(W+'kept_stx.pkl','wb'))
    # recovery within 50 m
    m=d50<=RAD
    print('capture of >=2.6 ore within 50 m: ST only %.0f%%'%(100*cap0[0]/cap0[1]))
