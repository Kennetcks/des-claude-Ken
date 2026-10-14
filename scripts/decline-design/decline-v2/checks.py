import pickle, numpy as np, pandas as pd, sys
sys.path.insert(0,'.'); from geomlib import proj, resample
exec(open('stage2_route_access.py').read().split("# ---- level networks")[0])
a=pd.read_csv('v2_attrs.csv'); g=pd.read_csv('v2_geom.csv'); SS=pd.read_csv('kd_strings.csv')
O={h:x.sort_values('vi')[['x','y','z']].values for h,x in SS.groupby('handle')}
F={}
for _,r in a.iterrows():
    if r.action=='delete': continue
    P=g[g.id==r.id][['x','y','z']].values if r.action in('replace','new') else O[r.id]
    F[r.id]=dict(t=r.ACT_TYPE,lev=r.LEVEL,P=P,new=r.action=='new',ch=r.action=='replace',des=r.DESIGN)
D1=pickle.load(open('stage1.pkl','rb')); STs=D1['ST']
def plen(P): return float(np.hypot(*np.diff(P[:,:2],axis=0).T).sum()) if len(P)>1 else 0
rows=[]
for st in STs:
    best=None
    for h,v in F.items():
        if v['t']!='OD': continue
        pr=[proj(v['P'],p) for p in st['C']]; dh=np.array([x[0] for x in pr]); dz=np.median([abs(x[3]-st['z0']) for x in pr])
        if dz<3 and (best is None or np.median(dh)<best[0]): best=(np.median(dh),h,dh)
    rows.append(dict(stope=st['name'],t=round(st['t']),od=best[1] if best else '',off_median=round(best[0],2) if best else None,off_max=round(best[2].max(),1) if best else None,
                     pct_within_1_5m=round((best[2]<1.5).mean()*100) if best else 0))
R=pd.DataFrame(rows); R.to_csv('/mnt/project-files/deswik-decline-v2/stope_vs_ore_drive_v2.csv',index=False)
print('stopes',len(R),'with drive <=2 m median', (R.off_median<=2).sum(),' all centre within 1.5 m:',(R.pct_within_1_5m==100).sum(),' worst:'); print(R.sort_values('off_median',ascending=False).head(8).to_string())
# metres before/after
A0=pd.read_csv('kd_attrs.csv').set_index('handle')
b=pd.Series({h:plen(P) for h,P in O.items()}).groupby(A0.ACT_TYPE).sum().round()
af=pd.Series({h:plen(v['P']) for h,v in F.items()}).groupby(pd.Series({h:v['t'] for h,v in F.items()})).sum().round()
print(pd.DataFrame({'before_m':b,'after_m':af}).to_string())
# new access checks
chk=[]
for h,v in F.items():
    if not v['new'] or v['t']!='ACC': continue
    P=v['P']; L=plen(P); gr=(P[-1,2]-P[0,2])/L
    fromdec='DEC' in v['des'] or abs(np.linalg.norm(P[1,:2]-P[0,:2])-10)<0.1
    chk.append(dict(id=h,why=v['des'].replace('Claude V2 261013 ',''),level=v['lev'],len_m=round(L),grade='1:%d'%round(1/gr) if abs(gr)>1e-3 else 'flat',
                    takeoff_m=round(np.linalg.norm(P[1,:2]-P[0,:2]),1),dec_clear='ok' if not dec_conflict(P,skip=12) else 'CHECK',stope_clip=stope_conflict(P,exclude=[st['name'] for st in STs if (v['des'].split()[-1] in st['name']) or ('orphan' in v['des'] and st['lev']==int(float(v['lev'])))],free=7.0) or ''))
C=pd.DataFrame(chk); print(C.to_string())
C.to_csv('/mnt/project-files/deswik-decline-v2/new_access_checks.csv',index=False)
