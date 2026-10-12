"""v7: >= 2.6 g/t unmined ore that no v6 block reaches -> new trial ore drives (same method as virtual.py)."""
import sys, collections, json, pickle, numpy as np
from scipy.spatial import cKDTree
sys.path.insert(0,'/home/claude/work'); import design6 as design
from raster import PX,PY,PZ,NX,NY,NZ,SX,SY,SZ,RX,RY,RZ
ORIGIN=design.ORIGIN; CUTP=2.6
kept=pickle.load(open('kept6.pkl','rb')); V=np.unique(np.concatenate([b['vox'] for b in kept]))
P_=np.c_[(V//2000)//2000,(V//2000)%2000,V%2000].astype(np.float32)+0.5; tree=cKDTree(P_)
# 1) corridor raster cells
comp=np.load('r_comp.npy'); TD=np.load('r_td.npy',mmap_mode='r'); AU=np.load('r_au.npy',mmap_mode='r'); MN=np.load('r_mn.npy',mmap_mode='r')
sel=np.nonzero((np.asarray(AU)>=CUTP)&(np.asarray(TD)>0)&(np.asarray(MN)==0))[0]
inv=np.full(comp.max()+1,-1,np.int64); pidx=np.nonzero(comp>=0)[0]; inv[comp[pidx]]=pidx
c=sel//(RX*RY*RZ); r=sel%(RX*RY*RZ); si=r//(RY*RZ); sj=(r//RZ)%RY; sk=r%RZ
p=inv[c]; k=p%NZ; j=(p//NZ)%NY; i=p//(NZ*NY)
A=np.c_[i*PX+(si+0.5)*SX, j*PY+(sj+0.5)*SY, k*PZ+(sk+0.5)*SZ]; tA=np.asarray(TD)[sel]*SX*SY*SZ; gA=np.asarray(AU)[sel]
# 2) sub-blocks outside the corridor (>= 2.5 export)
o=np.fromfile('/mnt/user-data/uploads/work/stopes_mine/ore_outside.f32',dtype='<f4').reshape(-1,9).astype(float)
o=o[(o[:,6]>=CUTP)&(o[:,8]<=0)]
B=o[:,:3]; tB=o[:,3]*o[:,4]*o[:,5]*o[:,7]; gB=o[:,6]
P=np.vstack([A,B]); t=np.r_[tA,tB]; g=np.r_[gA,gB]
dd,_=tree.query(P.astype(np.float32),distance_upper_bound=1e4,workers=4)
far=dd>4.0
print('>=2.6 ore not reached (>4 m from any block): %.3f Mt @ %.2f'%(t[far].sum()/1e6,(t[far]*g[far]).sum()/t[far].sum()))
P=P[far]; t=t[far]; g=g[far]
d=design.load_strings()
ods=[(np.median(v['pts'][:,2]),v['pts']) for v in d.values() if v['act']=='OD']
odpts=np.vstack([np.c_[q,np.full(len(q),z)] for z,q in ods])
lat=np.vstack([v['pts'] for v in d.values() if v['lv']=='LAT'])
old=json.load(open('virtual_drives.json')); oldF=np.vstack([np.array(l['pts'])-ORIGIN for l in old])
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
pl=np.array([lab.get(kk,-1) for kk in map(tuple,key)])
lines=[]; MIN_T=1000
for pk in range(n):
    m=pl==pk
    if t[m].sum()<MIN_T: continue
    cen=np.average(P[m],axis=0,weights=t[m])
    ddd=np.hypot(odpts[:,0]-cen[0],odpts[:,1]-cen[1])+np.abs(odpts[:,2]-cen[2])
    F0=odpts[np.argmin(ddd),3]
    zmin,zmax=P[m,2].min(),P[m,2].max()
    for kz in range(-12,13):
        F=F0+22*kz
        if F+22<zmin or F>zmax: continue
        mm=m&(P[:,2]>=F)&(P[:,2]<=F+22)
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
            a2=np.vstack([e0,a2,e1]); pts3=np.c_[a2,np.full(len(a2),F)]
            ton=t[mm][np.isin(bins,[pp[0] for pp in rr])].sum()
            if ton<MIN_T: continue
            lines.append(dict(pocket=pk,level=float(F+ORIGIN[2]),pts=(pts3+ORIGIN).round(2).tolist(),ore_t=float(ton)))
print(len(lines),'new trial drives, ore t %.0f'%sum(l['ore_t'] for l in lines))
json.dump(lines,open('virtual_drives2.json','w'))
with open('/mnt/project-files/deswik-5m-blocks-mine-v7/virtual_drives2.csv','w') as f:
    f.write('drive,x,y,z\n')
    for i,l in enumerate(lines):
        for q in l['pts']: f.write('T%03d,%.2f,%.2f,%.2f\n'%(i,*q))
