"""Stage 3: final string set for DESIGN\\DECLINE V2 with corrected attributes -> CSVs for the C# writer."""
import pickle, numpy as np, pandas as pd, sys
sys.path.insert(0,'/home/claude/kd'); from geomlib import proj
D2=pickle.load(open('stage4.pkl','rb')); S=D2['S']; new=D2['new']
A=pd.read_csv('kd_attrs.csv').set_index('handle'); SS=pd.read_csv('kd_strings.csv')
O={h:g.sort_values('vi')[['x','y','z']].values for h,g in SS.groupby('handle')}
MAP=dict(pd.read_csv('/mnt/user-data/uploads/work/stopes_mine/kd_v2_map.csv').values)
TAG='Claude V2 261013'
def lode_near(P):
    best=None
    for h,v in S.items():
        if v['t']!='OD' or str(v['lode']) not in('GROUP A','GROUP B'): continue
        d=min(np.hypot(*(v['P'][:,:2]-p[:2]).T).min() for p in P[::max(1,len(P)//5)])
        if best is None or d<best[0]: best=(d,v['lode'])
    return best[1]
def clean(P):
    K=[P[0]]
    for p in P[1:]:
        if np.linalg.norm(p[:2]-K[-1][:2])>0.05: K.append(p)
    K=np.array(K); ch=True
    while ch and len(K)>2:          # drop a vertex where the string doubles back on itself
        ch=False
        for i in range(1,len(K)-1):
            if (K[i,:2]-K[i-1,:2])@(K[i+1,:2]-K[i,:2])<0: K=np.delete(K,i,0); ch=True; break
    K2=[K[0]]
    for p in K[1:]:
        if np.linalg.norm(p[:2]-K2[-1][:2])>0.05: K2.append(p)
    return np.array(K2)
FIXED=[]
for h,v in S.items():
    c=clean(v['P'])
    if len(c)!=len(v['P']): v['P']=c; v['changed']=True; FIXED.append(h)
print('cleaned (duplicate vertex / backtrack):',FIXED)
rows=[]; geo=[]; i=0
for h,v in S.items():
    t=v['t']; ch=bool(v['changed']) or (len(v['P'])!=len(O[h])) or np.abs(v['P']-O[h]).max()>0.01
    lev='' if t in('DEC','RAR') or pd.isna(v['lev']) else str(int(v['lev']))
    lode='-' if t=='DEC' else (v['lode'] if str(v['lode']) in('GROUP A','GROUP B') else lode_near(v['P']))
    des=A.loc[h,'DESIGN'] if isinstance(A.loc[h,'DESIGN'],str) else ''
    if h=='3CA4C7C': des='Kennet new'
    if ch: des=(des+' | ' if des else '')+TAG+' centred on STOPE FINAL V3'
    rows.append(dict(id=h,v2=MAP[h],action='replace' if ch else 'attrs',ACT_TYPE=t,LEVEL=lev,LODE=lode,COST_CENTRE='O' if t=='OD' else 'C',
                     LAT_VERT='VERT' if t in('RAR','EWR') else 'LAT',STO_DEV='DEV',DESIGN=des,TD='MGA94 Z51 // DMG26'))
    if ch: geo+= [(h,k,*p) for k,p in enumerate(v['P'])]
for h in ['3CA4CFA']+D2.get('removed',[]): rows.append(dict(id=h,v2=MAP[h],action='delete'))
for n in new:
    i+=1; nid='N%03d'%i; t=n['t']; lode=n['lode'] if str(n['lode']) in('GROUP A','GROUP B') else lode_near(n['P'])
    rows.append(dict(id=nid,v2='',action='new',ACT_TYPE=t,LEVEL=str(int(n['lev'])),LODE=lode,COST_CENTRE='O' if t=='OD' else 'C',LAT_VERT='LAT',STO_DEV='DEV',
                     DESIGN=TAG+' '+n['why'],TD='MGA94 Z51 // DMG26'))
    geo+= [(nid,k,*p) for k,p in enumerate(n['P'])]
R=pd.DataFrame(rows); R.to_csv('v2_attrs.csv',index=False)
pd.DataFrame(geo,columns=['id','vi','x','y','z']).to_csv('v2_geom.csv',index=False,float_format='%.3f')
print(R.action.value_counts().to_dict()); print(R[R.action=='new'].groupby('ACT_TYPE').size().to_dict())
print(R[R.action!='delete'].groupby(['ACT_TYPE','COST_CENTRE']).size())
print(R[(R.action!='delete')&((R.LEVEL=='')|(~R.LODE.isin(['GROUP A','GROUP B','-'])))][['id','ACT_TYPE','LEVEL','LODE']])
