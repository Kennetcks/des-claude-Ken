"""v7: intersection solids. Where two drive ends face each other across a junction (an access such as the MLA
comes in between them), the gap between the two end blocks has no solid. Fill it with the convex hull of the two
end faces (full height, same as the blocks). Usage: python3 intersections.py kept_in.pkl kept_out.pkl"""
import sys, pickle, collections, numpy as np
from scipy.spatial import ConvexHull, Delaunay
sys.path.insert(0,'/home/claude/work'); import design6 as design
from raster import Model
ORIGIN=design.ORIGIN; NL2=2*design.NL; GAP=(0.5,12.0)
kept=pickle.load(open(sys.argv[1],'rb')); M=Model('n'); S=design.load_strings()
acc_ends=np.array([p for v in S.values() if v['act']!='OD' and v['lv']=='LAT' for p in (v['pts'][0],v['pts'][-1])])+ORIGIN
claimed={}
for b in kept:
    for v in b['vox'].tolist(): claimed.setdefault(v,b['pid'])
by=collections.defaultdict(list)
for b in kept: by[b['pid']].append(b)
ends=[]   # (pid, face vertices (all height parts), block centre)
for pid,bl in by.items():
    for bi,sl in ((min(b['bi'] for b in bl),slice(0,NL2)),(max(b['bi'] for b in bl),slice(NL2,2*NL2))):
        parts=[b for b in bl if b['bi']==bi]
        F=np.vstack([np.array(b['V'])[sl] for b in parts]); C=np.mean([[b['cx'],b['cy'],b['cz']] for b in parts],0)
        ends.append(dict(pid=pid,F=F,fc=F.mean(0),C=C,zlo=F[:,2].min(),zhi=F[:,2].max(),parts=parts))
cand=[]
for i in range(len(ends)):
    for j in range(i+1,len(ends)):
        a,b=ends[i],ends[j]
        if a['pid']==b['pid'] or abs(a['zlo']-b['zlo'])>2: continue
        d=b['fc']-a['fc']; d[2]=0; L=np.linalg.norm(d)
        if not GAP[0]<L<GAP[1]: continue
        oa=a['fc']-a['C']; oa[2]=0; ob=b['fc']-b['C']; ob[2]=0
        if np.dot(oa,d)/(np.linalg.norm(oa)*L+1e-9)<0.6 or np.dot(ob,-d)/(np.linalg.norm(ob)*L+1e-9)<0.6: continue   # the two ends face each other
        cand.append((L,i,j))
used=set(); new=[]; why=collections.Counter()
for L,i,j in sorted(cand):
    if i in used or j in used: why['end already used']+=1; continue
    a,b=ends[i],ends[j]; V=np.vstack([a['F'],b['F']]); c=V.mean(0)
    h=ConvexHull(V-c); F=[]
    for s,eq in zip(h.simplices,h.equations):
        p,q,r=(V-c)[s]; F.append(list(s) if np.dot(np.cross(q-p,r-p),eq[:3])>0 else [s[0],s[2],s[1]])
    # 0.5 m sample points inside the hull for tonnes, grade and 1 m voxels
    lo=V.min(0); hi=V.max(0); g=np.mgrid[lo[0]+0.25:hi[0]:0.5,lo[1]+0.25:hi[1]:0.5,lo[2]+0.25:hi[2]:0.5].reshape(3,-1).T
    g=g[Delaunay(V).find_simplex(g)>=0]
    if len(g)==0: why['empty']+=1; continue
    rel=g-ORIGIN; td,au,mn=M.look(rel[:,0],rel[:,1],rel[:,2]); w=td*0.125
    vox=np.unique((np.floor(rel[:,0]).astype(np.int64)*2000+np.floor(rel[:,1]).astype(np.int64))*2000+np.floor(rel[:,2]).astype(np.int64))
    pid=a['pid']+'/'+b['pid']
    ov=sum(claimed.get(v,a['pid']) not in (a['pid'],b['pid']) for v in vox.tolist())/len(vox)
    if ov>0.25: why['overlaps another block']+=1; continue
    dacc=np.min(np.linalg.norm((acc_ends-c)*[1,1,0.3],axis=1)) if len(acc_ends) else 99
    t=float(w.sum()); gg=float((w*au).sum()/t) if t>0 else 0.0
    ref=a['parts'][0]
    nb=dict(pid=pid,act='INT',bi=0,part='',split='',target=0.0,ch0=0.0,ch1=round(L,1),zlo=0.0,top=None,height=float(V[:,2].max()-V[:,2].min()),bt=bool(ref['bt'] or b['parts'][0]['bt']),
            height_above_back=float(ref['height_above_back']),t=t,g=gg,ore_t=float(w[au>=design.CUT].sum()),mined=float((w*(mn>0)).sum()/t) if t>0 else 0.0,
            w=float(np.mean([p_['w'] for p_ in a['parts']+b['parts']])),vol=float(h.volume),cx=float(c[0]),cy=float(c[1]),cz=float(c[2]),drill_ok=True,
            isod=False,kind='INTERSECTION',dec_dist=float(min(p_['dec_dist'] for p_ in a['parts']+b['parts'])),overlap_kept=float(ov),in_plan=0.0,
            V=V.round(3).tolist(),F=[list(map(int,f)) for f in F],vox=vox,ends=(a['pid'],b['pid']),acc_dist=float(dacc),lev_from=a['pid'])
    for v in vox.tolist(): claimed.setdefault(v,pid)
    used|={i,j}; new.append(nb)
print('drive ends',len(ends),'facing pairs',len(cand),'intersection solids',len(new),dict(why))
print('with an access end within 6 m:',sum(b['acc_dist']<6 for b in new))
T=sum(b['t'] for b in new); print('tonnes %.0f @ %.2f, >2.6: %d solids %.0f t'%(T,sum(b['t']*b['g'] for b in new)/max(T,1),sum(b['g']>2.6 for b in new),sum(b['t'] for b in new if b['g']>2.6)))
for b in new:
    if set(b['ends'])=={'3000FBF','3000D19'}: print('example',b['pid'],round(b['t']),round(b['g'],2),'gap',b['ch1'],'acc',round(b['acc_dist'],1))
pickle.dump(kept+new,open(sys.argv[2],'wb'))
