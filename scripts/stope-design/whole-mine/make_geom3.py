import pickle, json, csv, collections, numpy as np, sys
sys.path.insert(0,'/home/claude/work'); import design3 as design
k=pickle.load(open('kept3.pkl','rb'))
d=design.load_strings(); L=json.load(open('virtual_drives.json'))
ex=list(csv.DictReader(open('/mnt/user-data/uploads/work/stopes_mine/existing_stopes.csv')))
ec=np.array([[(float(e['minx'])+float(e['maxx']))/2,(float(e['miny'])+float(e['maxy']))/2,float(e['minz'])] for e in ex]); en=[e['name'] for e in ex]
def level_of(b):
    if b.get('virtual'): return int(round(b['level']-6000))
    return int(round(np.median(d[b['pid']]['pts'][:,2])+design.ORIGIN[2]-6000))
by=collections.defaultdict(list)
for b in k: by[b['pid']].append(b)
out=[]; summ=[]
for pid,bl in sorted(by.items()):
    bl.sort(key=lambda b:(b['bi'],b['part']))
    num={bi:n for n,bi in enumerate(sorted({b['bi'] for b in bl}),1)}
    for b in bl:
        n=num[b['bi']]
        c=np.array([b['cx'],b['cy'],b['cz']]); dist=np.linalg.norm(ec-c,axis=1); j=int(np.argmin(dist))
        lode=en[j][0] if dist[j]<40 else ''
        lev=level_of(b); name='%s%d %s B%02d%s'%(lode or 'X',lev,pid,n,b['part'])
        meta=dict(name=name,lode=lode,level=str(lev),drive=pid,source=b['kind'],block='%02d%s'%(n,b['part']),split=(b['split']+' %.1f'%b['target']) if b['split'] else 'NO',bt='Y' if b['bt'] else 'N',ht=round(b['height'],1),zlo=round(b['zlo'],1),ch0=round(b['ch0'],1),ch1=round(b['ch1'],1),
                  hab=round(b['height_above_back'],1),w=round(b['w'],2),t=round(b['t']),g=round(b['g'],3),inplan=round(100*b['in_plan']),mined=round(100*b['mined']),dec=round(b['dec_dist']),drill='Y' if b['drill_ok'] else 'N')
        out.append(json.dumps(meta,separators=(',',':'))+'|'+','.join('%.3f'%x for v in b['V'] for x in v)+'|'+','.join(str(x+1) for f in b['F'] for x in f))
        summ.append(meta)
open('/mnt/project-files/deswik-5m-blocks-mine-v3/blocks_5m_mine_v3_geometry.txt','w').write('\n'.join(out)+'\n')
with open('/mnt/project-files/deswik-5m-blocks-mine-v3/virtual_drive_lines_v3.csv','w') as f:
    f.write('drive,level,ore_t,x,y,z\n')
    for i,l in enumerate(L):
        if any(b['pid']=='V%03d'%i for b in k):
            for p in l['pts']: f.write('V%03d,%d,%.0f,%.2f,%.2f,%.2f\n'%(i,round(l['level']-6000),l['ore_t'],*p))
print(len(out),'blocks', collections.Counter(m['source'] for m in summ), 'lodes',collections.Counter(m['lode'] for m in summ))
