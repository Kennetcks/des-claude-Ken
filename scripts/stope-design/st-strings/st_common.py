"""Kennet's ST strings (DESIGN\\CREATING PLILLAR/STOPES\\ST, 2026-10-13): 5 m blocks along his level strings with the v8
rules, then extra conceptual strings for >= 2.6 g/t ore within 50 m of his strings that the blocks miss."""
import sys, csv, collections, time, pickle, numpy as np, multiprocessing as mp
sys.path.insert(0,'/home/claude/work'); import design8 as design
from run_all6 import dens
from overlap_check import overlap, pairs
ORIGIN=design.ORIGIN; MINV=0.5; MIN_G=1.5;   # Kennet 2026-10-13: first 0.5, then 1.5
W='/home/claude/work/st/'
import os
def init(): design.MODEL_PRE=os.environ.get('MODEL','s')
def load_st():
    S=collections.OrderedDict()
    for r in csv.DictReader(open('/mnt/user-data/uploads/work/stopes_mine/st_strings.csv')):
        S.setdefault(r['pid'],{'pid':r['pid'],'h':r['handle'],'pts':[]})['pts'].append((float(r['x']),float(r['y']),float(r['z'])))
    for v in S.values():
        p=np.array(v['pts'])-ORIGIN; v['pts']=p; L=np.hypot(*np.diff(p[:,:2],axis=0).T).sum(); grad=abs(p[-1,2]-p[0,2])/max(L,1e-6)
        v['len']=L; v['grad']=grad
        # his convention: ramps steeper than 1:12, accesses at about 1:50 off the ramp, ore drives flat
        v['act']='DEC' if grad>1/12 else ('ACC' if grad>0.005 else 'OD')
    return S
def clean(b_list, pid, dec, lev=None, extra=False):
    keep=[]
    for b in b_list or []:
        b['virtual']=extra; b['isod']=b['act']=='OD' and not extra; b['kind']='EXTRA' if extra else ('OD' if b['act']=='OD' else 'ACC')
        if extra: b['level']=lev
        c=np.array([b['cx'],b['cy'],b['cz']])-ORIGIN; near=np.abs(dec[:,2]-(c[2]+11))<15
        b['dec_dist']=float(np.hypot(dec[near,0]-c[0],dec[near,1]-c[1]).min()) if near.any() else 999.
        if b['dec_dist']<15: continue
        if not b['split'] and b['g']<MIN_G: continue   # Kennet 2026-10-13: a block < MIN_G is only kept when it splits into a piece > 2.0 and a lower piece
        keep.append(b)
    if extra and keep and max(b['g'] for b in keep)<=2.0: return []
    return keep
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
            if flat[i]['pid']==flat[j]['pid']: continue
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
        if flat[i]['pid']!=flat[j]['pid'] and overlap(flat[i],flat[j])>=MINV:
            drop.add(id(flat[j]) if beats(flat[i]['pid'],flat[j]['pid'],rank) else id(flat[i]))
    for p in blocks: blocks[p]=[b for b in blocks[p] if id(b) not in drop]
    log('  dropped %d blocks still overlapping'%len(drop)); return blocks
def self_fix(kept):
    """kinks: drop the lower-grade block of any same-drive pair overlapping > 2 m3"""
    drop=set()
    for i,j in pairs(kept):
        if kept[i]['pid']==kept[j]['pid'] and abs(kept[i]['bi']-kept[j]['bi'])>=1:
            v=overlap(kept[i],kept[j])
            if v>2 and i not in drop and j not in drop: drop.add(i if kept[i]['g']<kept[j]['g'] else j)
    return [b for n,b in enumerate(kept) if n not in drop], len(drop)
def summary(L,tag):
    T=sum(b['t'] for b in L); A=[b for b in L if b['g']>2.6]; ta=sum(b['t'] for b in A)
    return '%s: %d blocks %.0f t @ %.2f; >2.6 %d blocks %.0f t @ %.2f; ore>=2.5 %.0f t'%(tag,len(L),T,sum(b['t']*b['g'] for b in L)/max(T,1),len(A),ta,sum(b['t']*b['g'] for b in A)/max(ta,1),sum(b['ore_t'] for b in L))
