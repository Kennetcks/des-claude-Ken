import sys, pickle, collections, numpy as np
sys.path.insert(0,'/home/claude/work'); import design6 as design
ORIGIN=design.ORIGIN
kept6=pickle.load(open('kept6.pkl','rb')); resv=pickle.load(open('raw_virtual7.pkl','rb'))
d=design.load_strings(); exist=set(np.load('exist_vox.npy').tolist())
dec=np.vstack([v['pts'] for v in d.values() if v['act']=='DEC'])
claimed={}
for b in kept6:
    for v in b['vox'].tolist(): claimed.setdefault(v,b['pid'])
new=[b for pid,(out,st) in resv.items() for b in (out or [])]
for b in new: b['isod']=False; b['kind']='NO DRIVE'; b['trial2']=True
new.sort(key=lambda b:(b['g']<=design.KEEP_G,-b['g']))   # ore blocks claim space before grey ones
kept=[]; why=collections.Counter()
for b in new:
    c=np.array([b['cx'],b['cy'],b['cz']])-ORIGIN
    near=np.abs(dec[:,2]-(c[2]+11))<15
    dmin=np.hypot(dec[near,0]-c[0],dec[near,1]-c[1]).min() if near.any() else 999
    b['dec_dist']=float(dmin)
    if dmin<15: why['within 15 m of decline']+=1; continue
    vl=b['vox'].tolist(); ov=sum(claimed.get(v,b['pid'])!=b['pid'] for v in vl)/len(vl)
    if ov>0.25: why['overlaps a kept block']+=1; continue
    b['overlap_kept']=float(ov); b['in_plan']=sum(v in exist for v in vl)/len(vl)
    for v in vl: claimed.setdefault(v,b['pid'])
    kept.append(b)
print(why); print('new trial drive blocks kept',len(kept),'of',len(new),'drives with blocks',len({b['pid'] for b in kept}))
for lo,hi in ((-1,0.01),(0.01,2),(2,2.6),(2.6,99)):
    s_=[b for b in kept if lo<b['g']<=hi]; tt=sum(b['t'] for b in s_); print('new g %.1f-%.1f: %d blocks %.0f t @ %.2f'%(lo,hi,len(s_),tt,sum(b['t']*b['g'] for b in s_)/max(tt,1)))
print('drill fail',sum(not b['drill_ok'] for b in kept),'mined>0',sum(b['mined']>0 for b in kept))
pickle.dump(kept6+kept,open('kept7.pkl','wb'))
