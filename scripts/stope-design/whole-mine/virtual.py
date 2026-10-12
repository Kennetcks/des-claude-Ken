"""Virtual ore drives through ore that no drive reaches: on the nearest ore drive's level series (22 m),
follow the >= cut-off ore centroid along strike, one line per level and ore run."""
import sys, collections, json, numpy as np
sys.path.insert(0,'/home/claude/work'); import design
ORIGIN=design.ORIGIN
o=np.fromfile('/mnt/user-data/uploads/work/stopes_mine/ore_outside.f32',dtype='<f4').reshape(-1,9).astype(float)
t=o[:,3]*o[:,4]*o[:,5]*o[:,7]; P=o[:,:3]
d=design.load_strings()
ods=[(np.median(v['pts'][:,2]),v['pts']) for v in d.values() if v['act']=='OD']
odpts=np.vstack([np.c_[p,np.full(len(p),z)] for z,p in ods])
lat=np.vstack([v['pts'] for v in d.values() if v['lv']=='LAT'])
# pockets: 10 m cells >= 200 t, 26-connected
key=np.floor(P/10).astype(int); kt=collections.defaultdict(float)
for k,tt in zip(map(tuple,key),t): kt[k]+=tt
good={k for k,v in kt.items() if v>=200}; lab={}; n=0
for k in good:
    if k in lab: continue
    st=[k]; lab[k]=n
    while st:
        c=st.pop()
        for dx in (-1,0,1):
            for dy in (-1,0,1):
                for dz in (-1,0,1):
                    q=(c[0]+dx,c[1]+dy,c[2]+dz)
                    if q in good and q not in lab: lab[q]=n; st.append(q)
    n+=1
pl=np.array([lab.get(k,-1) for k in map(tuple,key)])
lines=[]
for pk in range(n):
    m=pl==pk
    if t[m].sum()<2000: continue
    c=np.average(P[m],axis=0,weights=t[m])
    dd=np.hypot(odpts[:,0]-c[0],odpts[:,1]-c[1])+np.abs(odpts[:,2]-c[2])
    F0=odpts[np.argmin(dd),3]
    zmin,zmax=P[m,2].min(),P[m,2].max()
    for k in range(-12,13):
        F=F0+22*k
        if F+22<zmin or F+3.5>zmax: continue
        mm=m&(P[:,2]>=F+3.5)&(P[:,2]<=F+22)
        if t[mm].sum()<2000: continue
        xy=P[mm,:2]; w=t[mm]; mu=np.average(xy,axis=0,weights=w); C=np.cov((xy-mu).T,aweights=w); ev,evec=np.linalg.eigh(C); ax=evec[:,-1]
        s=(xy-mu)@ax; bins=np.floor(s/5).astype(int); pts=[]
        for bb in range(bins.min(),bins.max()+1):
            q=bins==bb
            if w[q].sum()>=30: pts.append((bb,*np.average(xy[q],axis=0,weights=w[q])))
        runs=[[pts[0]]] if pts else []
        for p in pts[1:]: (runs[-1].append(p) if p[0]-runs[-1][-1][0]<=2 else runs.append([p]))
        for r in runs:
            if len(r)<3: continue
            a=np.array([p[1:] for p in r]); a2=a.copy(); a2[1:-1]=(a[:-2]+a[1:-1]+a[2:])/3
            e0=a2[0]+(a2[0]-a2[1])/np.linalg.norm(a2[0]-a2[1])*2.5; e1=a2[-1]+(a2[-1]-a2[-2])/np.linalg.norm(a2[-1]-a2[-2])*2.5
            a2=np.vstack([e0,a2,e1]); pts3=np.c_[a2,np.full(len(a2),F)]
            near=np.abs(lat[:,2]-F)<6; dmin=min(np.hypot(lat[near,0]-p[0],lat[near,1]-p[1]).min() for p in a2) if near.any() else 999
            ton=t[mm][np.isin(bins,[p[0] for p in r])].sum()
            lines.append(dict(pocket=pk,level=float(F+ORIGIN[2]),pts=(pts3+ORIGIN).round(2).tolist(),ore_t=float(ton),near_drive=float(dmin)))
print(len(lines),'virtual drives, ore t %.0f'%sum(l['ore_t'] for l in lines))
for l in sorted(lines,key=lambda l:-l['ore_t'])[:12]: print(l['pocket'],l['level'],len(l['pts']),'%.0f t'%l['ore_t'],'nearest drive %.0f m'%l['near_drive'])
json.dump(lines,open('virtual_drives.json','w'))
