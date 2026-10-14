"""V5 vs V3: overlap by 1 m point sampling; GROUP from the V3 stope it overlaps most (live), drop V5 stopes that only sit in
V3 stopes Kennet deleted; per live V3 stope the V5 stopes that replace it (old vs new t/g)."""
import pickle,csv,numpy as np,trimesh,collections,json
from scipy.spatial import cKDTree
v5=pickle.load(open('/home/claude/work/st_final_v5/stp/stope_geoms.pkl','rb'))
v3=pickle.load(open('/home/claude/stp/final_v3/stope_geoms_v3.pkl','rb'))
live={r['name']:r['GROUP'] for r in csv.DictReader(open('/home/claude/kd/v3_live_groups.csv'))}
def pts(s,st=1.0):
    m=trimesh.Trimesh(np.asarray(s['V'],float),np.asarray(s['F'],int),process=True)
    lo,hi=m.bounds; g=np.mgrid[lo[0]+st/2:hi[0]:st,lo[1]+st/2:hi[1]:st,lo[2]+st/2:hi[2]:st].reshape(3,-1).T
    return g[m.contains(g)] if len(g) else g
P5=[pts(s) for s in v5]; P3=[pts(s) for s in v3]
key=lambda p:set(map(tuple,np.round(p).astype(int)))
K5=[key(p) for p in P5]; K3=[key(p) for p in P3]
c3=np.array([np.asarray(s['V'],float).mean(0) for s in v3]); T3=cKDTree(c3)
rows=[]; keep=[]
for i,s in enumerate(v5):
    ov={j:len(K5[i]&K3[j]) for j in T3.query_ball_point(np.asarray(s['V'],float).mean(0),80) if K5[i]&K3[j]}
    ovl={j:n for j,n in ov.items() if v3[j]['name'] in live}; ovd={j:n for j,n in ov.items() if v3[j]['name'] not in live}
    if ovl: j=max(ovl,key=ovl.get); grp=live[v3[j]['name']]; st='overlaps live'
    elif ovd: grp=None; st='only in deleted V3 stopes'
    else:
        d,js=T3.query(np.asarray(s['V'],float).mean(0),k=10); js=[j for j in js if v3[j]['name'] in live]
        grp=live[v3[js[0]]['name']] if js else 'NO'; st='new ground'
    s['GROUP']=grp; s['status']=st; s['ov']=ov
    if grp is not None: keep.append(i)
print('V5 stopes',len(v5),'kept',len(keep),collections.Counter(s['status'] for s in v5))
kt=sum(v5[i]['t'] for i in keep); print('kept %.0f kt @ %.2f'%(kt/1e3,sum(v5[i]['t']*v5[i]['g'] for i in keep)/kt))
L3=[j for j,s in enumerate(v3) if s['name'] in live]; t3=sum(v3[j]['t'] for j in L3)
print('V3 live',len(L3),'%.0f kt @ %.2f'%(t3/1e3,sum(v3[j]['t']*v3[j]['g'] for j in L3)/t3))
# per live V3 stope
out=[]
for j in L3:
    o=v3[j]; reps=[i for i in keep if j in v5[i]['ov']]
    nt=sum(v5[i]['t'] for i in reps); ng=sum(v5[i]['t']*v5[i]['g'] for i in reps)/nt if nt else 0
    share=sum(v5[i]['ov'][j] for i in reps)/max(1,len(K3[j]))
    un=set().union(*[K5[i] for i in reps]) if reps else set(); iou=len(un&K3[j])/max(1,len(un|K3[j]))
    out.append(dict(v3_stope=o['name'],GROUP=live[o['name']],v3_t=round(o['t']),v3_g=round(o['g'],2),v5_stopes=' + '.join(v5[i]['name'] for i in reps),
        v5_t=round(nt),v5_g=round(ng,2),v3_kept_pct=round(100*share),iou_pct=round(100*iou),
        change='same' if iou>=0.7 else ('gone' if not reps else 'changed')))
for i in keep:
    if v5[i]['status']=='new ground':
        s=v5[i]; out.append(dict(v3_stope='',GROUP=s['GROUP'],v3_t=0,v3_g=0,v5_stopes=s['name'],v5_t=round(s['t']),v5_g=round(s['g'],2),v3_kept_pct=0,iou_pct=0,change='new'))
print(collections.Counter(r['change'] for r in out))
with open('/home/claude/work/st_final_v5/cmp/v3_vs_v5.csv','w',newline='') as f:
    w=csv.DictWriter(f,fieldnames=list(out[0])); w.writeheader(); w.writerows(out)
pickle.dump(dict(keep=keep,group={v5[i]['name']:v5[i]['GROUP'] for i in keep}),open('/home/claude/work/st_final_v5/cmp/keep.pkl','wb'))
for r in out:
    if r['change']!='same': print(r)
