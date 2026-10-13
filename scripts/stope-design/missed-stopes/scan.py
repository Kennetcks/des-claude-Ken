"""Missed-stope scan (Kennet 2026-10-13, 3CA4872): model ore >= 2.5 g/t that sits outside every live STOPE FINAL V3 stope,
grouped into connected bodies; each body gets a 2.2 m minimum-width stope shape along its own strike and a diluted grade.
Also reports the angle between the body's strike and the nearest drive (a vein crossing a drive is what the
drive-based block design cannot see)."""
import sys; sys.path.insert(0,'/home/claude/work')
import numpy as np, pickle, csv, collections
from scipy import ndimage
from design8 import inside_cols
O=np.array([7770.,62610.,5930.]); CUT=2.5; SG=2.6; MINW=2.2; STANDOFF=15.0
D='/mnt/user-data/uploads/work/stopes_mine/'
b=np.vstack([np.fromfile(D+f,dtype='<f4').reshape(-1,9) for f in ('st_blocks.f32','st_blocks_boxes.f32')]).astype(np.float64)
key=np.round(b[:,:3]*8).astype(np.int64); _,ix=np.unique(key[:,0]*10**12+key[:,1]*10**6+key[:,2],return_index=True); b=b[np.sort(ix)]
P=b[:,:3]+O; T=b[:,3]*b[:,4]*b[:,5]*b[:,7]; G=b[:,6]; MN=b[:,8]>0
print('model blocks',len(b),'%.2f Mt'%(T.sum()/1e6))
# live V3 stopes (incl. GROUP NO: designed, just not developed)
live={r['name']:r['GROUP'] for r in csv.DictReader(open('/home/claude/kd/v3_live_groups.csv'))}
S=[s for s in pickle.load(open('/home/claude/stp/final_v3/stope_geoms_v3.pkl','rb')) if s['name'] in live]
print('live stopes',len(S))
IN=np.zeros(len(P),bool); OWN=np.full(len(P),-1)
for k,s in enumerate(S):
    V=np.array(s['V'],float); F=np.array(s['F'],int); lo=V.min(0)-0.01; hi=V.max(0)+0.01
    c=np.nonzero(np.all((P>=lo)&(P<=hi),axis=1))[0]
    if len(c)==0: continue
    ins=inside_cols(V,F,P[c,0]+1e-4,P[c,1]+1e-4,P[c,2],np.zeros(1))[:,0]
    IN[c[ins]]=True; OWN[c[ins]]=k
print('in stopes %.0f kt @ %.2f'%(T[IN].sum()/1e3,(T[IN]*G[IN]).sum()/T[IN].sum()))
# drives: V2 strings (his DECLINE with Claude's edits)
geo=collections.defaultdict(list); act={}
rep={r['id']:r for r in csv.DictReader(open('/home/claude/kd/v2_attrs.csv'))}
for r in csv.DictReader(open('/home/claude/kd/v2_geom.csv')): geo[r['id']].append((float(r['x']),float(r['y']),float(r['z'])))
for r in csv.DictReader(open('/home/claude/kd/kd_strings.csv')):
    if r['handle'] in geo and r['handle'] in rep and rep[r['handle']]['action'] in ('replace','new'): continue
    if r['handle'] in rep and rep[r['handle']]['action']=='delete': continue
    geo.setdefault(r['handle'],[]).append((float(r['x']),float(r['y']),float(r['z'])))
for r in csv.DictReader(open('/home/claude/kd/kd_attrs.csv')): act[r['handle']]=r['ACT_TYPE']
for k,r in rep.items(): act[k]=r['ACT_TYPE']
def dens(p,st=1.0):
    p=np.array(p); out=[p[0]]
    for a,c in zip(p[:-1],p[1:]):
        n=max(1,int(np.linalg.norm(c-a)/st)); out+= [a+(c-a)*i/n for i in range(1,n+1)]
    return np.array(out)
DP=[];DD=[];DID=[]
for k,p in geo.items():
    if len(p)<2: continue
    q=dens(p); d=np.gradient(q[:,:2],axis=0); d/=np.maximum(np.linalg.norm(d,axis=1),1e-9)[:,None]
    DP.append(q); DD.append(d); DID+= [k]*len(q)
DP=np.vstack(DP); DD=np.vstack(DD); DID=np.array(DID); DACT=np.array([act.get(k,'') for k in DID])
from scipy.spatial import cKDTree
decT=cKDTree(DP[DACT=='DEC']); drvT=cKDTree(DP)
# ore outside stopes, not mined, clear of the ramp
dd,_=decT.query(P,distance_upper_bound=STANDOFF+1)
ore=(G>=CUT)&~IN&~MN&(T>0)
print('ore >= %.1f outside stopes %.0f kt @ %.2f (of which within %d m of the ramp %.0f kt)'%(CUT,T[ore].sum()/1e3,(T[ore]*G[ore]).sum()/T[ore].sum(),STANDOFF,T[ore&(dd<STANDOFF)].sum()/1e3))
ore&=dd>=STANDOFF
# connected bodies on a 1 m voxel grid (ore cells touching, 1 m gaps bridged)
V0=np.floor(P.min(0)); vi=np.floor(P-V0).astype(int); shp=vi.max(0)+1
grid=np.zeros(shp,bool); grid[tuple(vi[ore].T)]=True
lab,n=ndimage.label(ndimage.binary_dilation(grid,iterations=1),structure=np.ones((3,3,3)))
L=lab[tuple(vi.T)]*ore
tl=np.bincount(L,weights=T,minlength=n+1); ml=np.bincount(L,weights=T*G,minlength=n+1)
cand=[k for k in range(1,n+1) if tl[k]>=1500]
print('bodies',n,'with >= 1.5 kt ore',len(cand))
free=~IN&~MN
fT=cKDTree(P[free]); fidx=np.nonzero(free)[0]
rows=[]; pieces=[]
for k in cand:
    m=L==k; p=P[m]; w=T[m]
    c=(p*w[:,None]).sum(0)/w.sum(); X=p[:,:2]-c[:2]; C=(X*w[:,None]).T@X/w.sum(); ev,evec=np.linalg.eigh(C); st=evec[:,1]
    if st[1]<0: st=-st
    nu=np.array([-st[1],st[0]])
    s=X@st; u=X@nu; z=p[:,2]
    # columns 1 m along strike x 1 m high: walls = ore extent, padded to >= MINW
    si=np.floor(s).astype(int); zi=np.floor(z).astype(int); cols=collections.defaultdict(lambda:[1e9,-1e9])
    for a,bz,uu in zip(si,zi,u):
        q=cols[(a,bz)]; q[0]=min(q[0],uu-0.25); q[1]=max(q[1],uu+0.25)
    # all free model blocks in the body's box, kept when inside a padded column
    r=np.sqrt(((s.max()-s.min())/2)**2+((u.max()-u.min())/2+2)**2+((z.max()-z.min())/2)**2)+2
    j=fidx[fT.query_ball_point(c,r)]; q=P[j]; Xq=q[:,:2]-c[:2]; sq=Xq@st; uq=Xq@nu
    keep=np.zeros(len(j),bool)
    for i,(a,bz) in enumerate(zip(np.floor(sq).astype(int),np.floor(q[:,2]).astype(int))):
        if (a,bz) in cols:
            lo,hi=cols[(a,bz)]
            if hi-lo<MINW: mid=(lo+hi)/2; lo,hi=mid-MINW/2,mid+MINW/2
            keep[i]=lo<=uq[i]<=hi
    j=j[keep]; td=T[j].sum(); gd=(T[j]*G[j]).sum()/max(td,1)
    # stope-sized pieces: 40 m along strike x 20 m high, each with its own 2.2 m min-width shape
    sj=(P[j,:2]-c[:2])@st; zj=P[j,2]
    pk=collections.defaultdict(list)
    for jj,a,bz in zip(j,np.floor((sj-s.min())/40).astype(int),np.floor((zj-z.min())/20).astype(int)): pk[(a,bz)].append(jj)
    for (a,bz),q in pk.items():
        q=np.array(q); pt=T[q].sum(); pg=(T[q]*G[q]).sum()/max(pt,1); pc=(P[q]*T[q,None]).sum(0)/max(pt,1)
        dist2,di2=drvT.query(pc); ang2=np.degrees(np.arccos(min(1,abs(DD[di2]@st))))
        pieces.append(dict(body=k,x=round(pc[0],1),y=round(pc[1],1),RL=round(pc[2]),t=round(pt),g=round(pg,2),ore_t=round(T[q][G[q]>=CUT].sum()),
            strike_len=round(np.ptp(sj[np.isin(j,q)]),1),height=round(np.ptp(zj[np.isin(j,q)]),1),strike_az=round((np.degrees(np.arctan2(st[0],st[1])))%180),
            near_drive=DID[di2],near_act=DACT[di2],drive_dist=round(dist2,1),angle_to_drive=round(ang2)))
    # nearest drive + angle between the body strike and that drive
    dist,di=drvT.query(c); dv=DD[di]; ang=np.degrees(np.arccos(min(1,abs(dv@st))))
    near_st=[]
    if (OWN>=0).any():
        jj=np.nonzero(IN)[0]; d2=np.linalg.norm(P[jj]-c,axis=1); o=OWN[jj[d2<12]]
        near_st=sorted({S[x]['name'] for x in o})
    rows.append(dict(body=k,x=round(c[0],1),y=round(c[1],1),z=round(c[2],1),RL=round(c[2]),ore_t=round(tl[k]),ore_g=round(ml[k]/tl[k],2),
        strike_m=round(s.max()-s.min(),1),height_m=round(z.max()-z.min(),1),thick_m=round(np.median([v[1]-v[0] for v in cols.values()]),1),
        dil_t=round(td),dil_g=round(gd,2),strike_az=round((np.degrees(np.arctan2(st[0],st[1])))%180),
        near_drive=DID[di],near_act=DACT[di],drive_dist=round(dist,1),angle_to_drive=round(ang),stopes_within_12m=' '.join(near_st[:4])))
rows.sort(key=lambda r:-r['dil_t']*(r['dil_g']-SG))
out='/home/claude/work/miss/missed_bodies.csv'
with open(out,'w',newline='') as f:
    wr=csv.DictWriter(f,fieldnames=list(rows[0])); wr.writeheader(); wr.writerows(rows)
good=[r for r in rows if r['dil_g']>SG and r['dil_t']>=3000]
print('candidate bodies diluted > %.1f and >= 3 kt: %d, %.0f kt @ %.2f'%(SG,len(good),sum(r['dil_t'] for r in good)/1e3,sum(r['dil_t']*r['dil_g'] for r in good)/max(1,sum(r['dil_t'] for r in good))))

pieces=[p for p in pieces if p['g']>SG and p['t']>=2000]; pieces.sort(key=lambda p:-p['t']*(p['g']-SG))
with open('/home/claude/work/miss/missed_pieces.csv','w',newline='') as f:
    wr=csv.DictWriter(f,fieldnames=list(pieces[0])); wr.writeheader(); wr.writerows(pieces)
print('stope-sized pieces > %.1f diluted and >= 2 kt: %d, %.0f kt @ %.2f'%(SG,len(pieces),sum(p['t'] for p in pieces)/1e3,sum(p['t']*p['g'] for p in pieces)/max(1,sum(p['t'] for p in pieces))))
for p in pieces: print(p)
pickle.dump(dict(L=L,ore=ore,IN=IN),open('/home/claude/work/miss/scan.pkl','wb'))
