"""Kennet 2026-10-13 (3CA5752, 3CA5758): a drive-end block past the end of the string above gets the blind height (HBLIND=26)
while its neighbours stop at the floor above (~21.5 m), so the stope shows a spike at its top end. Find those end blocks
and trim the stope top flat at the neighbour's height (manifold trim_by_plane); then replace the stopes in Deswik and
re-interrogate them."""
import sys, collections, pickle, json, numpy as np, trimesh, manifold3d as mf
sys.path.insert(0,'/home/claude/work/st_final_v5/stp'); from v5_bio import load
B=load(); by=collections.defaultdict(list)
for b in B: by[b['drive']].append(b)
fix={}
for d,L in by.items():
    if '/' in d: continue
    cols=collections.defaultdict(list)
    for b in L: cols[round(b['ch0'],2)].append(b)
    ks=sorted(cols); top=lambda k:max(b['base']+b['ht'] for b in cols[k])
    if len(ks)<2: continue
    for e,nb in ((ks[0],ks[1]),(ks[-1],ks[-2])):
        if top(e)>top(nb)+0.5: fix[(d,e)]=top(nb)
A=pickle.load(open(sys.argv[1],'rb')); lines=[]
for (d,e),h in fix.items():
    for s in A:
        if s['drive']==d and s['ch0']-0.01<=e<=s['ch1']:
            V=np.asarray(s['V'],float); o=V.mean(0)
            M=mf.Manifold(mf.Mesh(vert_properties=np.asarray(V-o,np.float32),tri_verts=np.asarray(s['F'],np.uint32))).trim_by_plane((0,0,-1),-(V[:,2].min()+0.1+h-o[2]))
            mm=M.to_mesh(); W=np.asarray(mm.vert_properties,float)[:,:3]+o
            lines.append(json.dumps(dict(name=s['name'],H=round(h,1)))+'|'+','.join('%.3f'%x for x in W.ravel())+'|'+','.join(str(int(x)+1) for x in np.asarray(mm.tri_verts).ravel()))
open(sys.argv[2],'w').write('\n'.join(lines)+'\n'); print(len(lines),'stopes trimmed')
