import sys, pickle, collections, numpy as np
sys.path.insert(0,'/home/claude/work'); import design4 as design
ORIGIN=design.ORIGIN
res=pickle.load(open('raw_blocks4.pkl','rb')); resv=pickle.load(open('raw_virtual4.pkl','rb'))
d=design.load_strings()
# existing stopes -> 1 m voxel ids (relative to origin), point-in-mesh by +z ray parity
def voxels_in_mesh(V,F):
    V=np.asarray(V)-ORIGIN; F=np.asarray(F)
    lo=np.floor(V.min(0)).astype(int); hi=np.ceil(V.max(0)).astype(int)
    gx,gy=np.meshgrid(np.arange(lo[0],hi[0])+0.5,np.arange(lo[1],hi[1])+0.5,indexing='ij'); gx=gx.ravel(); gy=gy.ravel()
    A,B,C=V[F[:,0]],V[F[:,1]],V[F[:,2]]
    zs=np.arange(lo[2],hi[2])+0.5; inside=np.zeros((len(gx),len(zs)),bool)
    for a,b,c in zip(A,B,C):
        den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
        if abs(den)<1e-12: continue
        w1=((b[1]-c[1])*(gx-c[0])+(c[0]-b[0])*(gy-c[1]))/den; w2=((c[1]-a[1])*(gx-c[0])+(a[0]-c[0])*(gy-c[1]))/den; w3=1-w1-w2
        hit=(w1>=0)&(w2>=0)&(w3>=0)
        if not hit.any(): continue
        zt=w1*a[2]+w2*b[2]+w3*c[2]
        inside[hit]^=(zs[None,:]<zt[hit,None])
    ii,kk=np.nonzero(inside)
    return (np.floor(gx[ii]).astype(np.int64)*2000+np.floor(gy[ii]).astype(np.int64))*2000+np.floor(zs[kk]).astype(np.int64)
exist=set(np.load('exist_vox.npy').tolist())
# decline points for standoff
dec=np.vstack([v['pts'] for v in d.values() if v['act']=='DEC'])
blocks=[b for pid,(out,st) in res.items() for b in (out or [])]+[b for pid,(out,st) in resv.items() for b in (out or [])]
for b in blocks: b['isod']=b['act']=='OD' and not b.get('virtual'); b['kind']='OD' if b['isod'] else ('NO DRIVE' if b.get('virtual') else b['act'])
order={'OD':0,'NO DRIVE':2}; blocks.sort(key=lambda b:(b['g']<=design.KEEP_G,order.get(b['kind'],1),-b['g']))   # ore blocks claim space before grey ones
claimed={}; kept=[]; why=collections.Counter()
for b in blocks:
    c=np.array([b['cx'],b['cy'],b['cz']])-ORIGIN
    near=np.abs(dec[:,2]-(c[2]+11))<15
    dmin=np.hypot(dec[near,0]-c[0],dec[near,1]-c[1]).min() if near.any() else 999
    b['dec_dist']=float(dmin)
    if dmin<15: why['within 15 m of decline '+b['kind']]+=1; continue
    vl=b['vox'].tolist(); ov=sum(claimed.get(v,b['pid'])!=b['pid'] for v in vl)/len(vl)   # blocks of the same drive never overlap; ignore shared boundary voxels
    if ov>0.25: why['overlaps a kept block '+b['kind']]+=1; continue
    b['overlap_kept']=float(ov); b['in_plan']=sum(v in exist for v in vl)/len(vl)
    for v in vl: claimed.setdefault(v,b['pid'])
    kept.append(b)
print(why)
c=collections.Counter(b['kind'] for b in kept); print('kept',len(kept),c)
T=sum(b['t'] for b in kept); print('total t %.0f g %.2f ore>=2.5 t %.0f'%(T,sum(b['t']*b['g'] for b in kept)/T,sum(b['ore_t'] for b in kept)))
print('in plan >50%%: %d, <10%%: %d'%(sum(b['in_plan']>0.5 for b in kept),sum(b['in_plan']<0.1 for b in kept)))
print('height', collections.Counter(round(b['height']) for b in kept).most_common(12)); print('B/T',sum(b['bt'] for b in kept),'split',collections.Counter((b['split'],b['target']) for b in kept))
for lo,hi in ((-1,0.01),(0.01,2),(2,2.6),(2.6,99)):
    s_=[b for b in kept if lo<b['g']<=hi]; tt=sum(b['t'] for b in s_); print('g %.1f-%.1f: %d blocks %.0f t @ %.2f'%(lo,hi,len(s_),tt,sum(b['t']*b['g'] for b in s_)/max(tt,1)))
print('drill fail',sum(not b['drill_ok'] for b in kept), 'mined>0', sum(b['mined']>0 for b in kept))
pickle.dump(kept,open('kept4.pkl','wb'))
