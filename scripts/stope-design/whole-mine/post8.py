"""v8 post: decline standoff, weak trial drives dropped, then NO overlaps between drives. Drives are ranked by value
(tonnes x grade above 2.6); a drive whose blocks overlap a better drive's blocks is designed again with the better
drive's blocks as forbidden space, until no two blocks overlap. Usage: python3 post8.py"""
import sys, time, pickle, collections, numpy as np, multiprocessing as mp
sys.path.insert(0,'/home/claude/work'); import design8 as design
from run_all8 import jobs_all, init
from overlap_check import overlap, pairs
ORIGIN=design.ORIGIN; MINV=0.5
res=pickle.load(open('raw_all8.pkl','rb')); d=design.load_strings()
dec=np.vstack([v['pts'] for v in d.values() if v['act']=='DEC'])
jobs,lev=jobs_all(); J={j[0]:j for j in jobs}
def clean(pid,out):
    keep=[]
    for b in out or []:
        b['virtual']=pid[0] in 'VT' and pid[1:].isdigit()
        if b['virtual']: b['level']=lev[pid]
        b['isod']=b['act']=='OD' and not b['virtual']; b['kind']='OD' if b['isod'] else ('NO DRIVE' if b['virtual'] else b['act'])
        c=np.array([b['cx'],b['cy'],b['cz']])-ORIGIN; near=np.abs(dec[:,2]-(c[2]+11))<15
        b['dec_dist']=float(np.hypot(dec[near,0]-c[0],dec[near,1]-c[1]).min()) if near.any() else 999.
        if b['dec_dist']<15: continue
        keep.append(b)
    if pid[0]=='T' and keep and max(b['g'] for b in keep)<=2.0: return []   # v7 rule: a new trial drive needs a block > 2.0
    return keep
blocks={pid:clean(pid,out) for pid,(out,st) in res.items()}
value=lambda pid:sum(b['t']*max(b['g']-2.6,0) for b in blocks[pid])+1e-3*sum(b['ore_t'] for b in blocks[pid])
def conflicts():
    flat=[b for p in blocks for b in blocks[p]]; out=collections.defaultdict(float)
    for i,j in pairs(flat):
        if flat[i]['pid']==flat[j]['pid']: continue
        v=overlap(flat[i],flat[j])
        if v>=MINV: out[tuple(sorted((flat[i]['pid'],flat[j]['pid'])))]+=v
    return out
def near_better(pid,rank):
    """blocks of better-ranked drives whose boxes come within 2 m of this drive's corridor"""
    p=J[pid][2]+ORIGIN; lo=p.min(0)-np.r_[19,19,3]; hi=p.max(0)+np.r_[19,19,25]; F=[]
    for q,bl in blocks.items():
        if q==pid or not beats(q,pid,rank): continue
        for b in bl:
            V=np.asarray(b['V']); 
            if np.all(V.max(0)>=lo) and np.all(V.min(0)<=hi): F.append((V-ORIGIN,np.asarray(b['F'])))
    return F
FZ={p:float(np.median(J[p][2][:,2])) for p in J}
def beats(q,p,rank):
    """stacked drives (floors > 5 m apart): the upper drive's blocks win and the lower block's top is cut under them;
    side by side: the drive with more value above 2.6 wins"""
    if abs(FZ[q]-FZ[p])>5: return FZ[q]>FZ[p]
    return rank[q]<rank[p]
t0=time.time()
for it in range(5):
    C=conflicts(); tot=sum(C.values())
    print('round',it,'drive pairs overlapping',len(C),'overlap %.0f m3'%tot,round(time.time()-t0),'s',flush=True)
    if not C: break
    order=sorted(blocks,key=value,reverse=True); rank={p:i for i,p in enumerate(order)}
    worse={(b if beats(a,b,rank) else a) for a,b in C}
    redo=[(J[p][0],J[p][1],J[p][2],J[p][3],near_better(p,rank)) for p in sorted(worse,key=lambda x:rank[x])]
    with mp.Pool(4,initializer=init) as pool:
        for pid,out,st in pool.imap_unordered(design.run,redo): blocks[pid]=clean(pid,out)
else:
    C=conflicts(); order=sorted(blocks,key=value,reverse=True); rank={p:i for i,p in enumerate(order)}
    flat=[b for p in blocks for b in blocks[p]]; drop=set()
    for i,j in pairs(flat):
        if flat[i]['pid']!=flat[j]['pid'] and overlap(flat[i],flat[j])>=MINV:
            drop.add(id(flat[j]) if beats(flat[i]['pid'],flat[j]['pid'],rank) else id(flat[i]))
    for p in blocks: blocks[p]=[b for b in blocks[p] if id(b) not in drop]
    print('dropped',len(drop),'blocks still overlapping after 5 rounds')
kept=[b for p in blocks for b in blocks[p]]
exist=set(np.load('exist_vox.npy').tolist())
for b in kept: b['in_plan']=sum(v in exist for v in b['vox'].tolist())/len(b['vox']); b['overlap_kept']=0.0
c=collections.Counter(b['kind'] for b in kept); print('kept',len(kept),c)
T=sum(b['t'] for b in kept); print('total t %.0f g %.2f ore>=2.5 t %.0f'%(T,sum(b['t']*b['g'] for b in kept)/T,sum(b['ore_t'] for b in kept)))
for lo,hi in ((-1,0.01),(0.01,2),(2,2.6),(2.6,99)):
    s_=[b for b in kept if lo<b['g']<=hi]; tt=sum(b['t'] for b in s_); print('g %.1f-%.1f: %d blocks %.0f t @ %.2f'%(lo,hi,len(s_),tt,sum(b['t']*b['g'] for b in s_)/max(tt,1)))
print('inverted',sum(b['vol']<=0 for b in kept),'drill fail',sum(not b['drill_ok'] for b in kept))
pickle.dump(kept,open('kept8.pkl','wb'))
