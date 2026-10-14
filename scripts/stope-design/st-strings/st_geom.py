"""write the ST 5 m blocks + extra strings for Deswik (same text format as the whole-mine runs)"""
import json, collections, pickle, numpy as np, sys
sys.path.insert(0,'/home/claude/work'); from st_common import load_st, W, ORIGIN
k=pickle.load(open(W+'kept_stxi.pkl','rb')); S=load_st(); L=json.load(open(W+'extra_strings.json'))
H={p:v['h'] for p,v in S.items()}
def drive_name(pid): return '/'.join(H.get(x,x) for x in pid.split('/'))
def level_of(b):
    if b.get('kind')=='INTERSECTION': return level_of(by[b['lev_from']][0])
    if b['pid'].startswith('X'): return int(round(b['level']-6000))
    return int(round(np.median(S[b['pid']]['pts'][:,2])+ORIGIN[2]-6000))
by=collections.defaultdict(list)
for b in k: by[b['pid']].append(b)
src={'OD':'ST ORE DRIVE','ACC':'ST ACCESS','EXTRA':'EXTRA STRING','INTERSECTION':'INTERSECTION'}
out=[]
for pid,bl in sorted(by.items()):
    bl.sort(key=lambda b:(b['bi'],b['part'])); num={bi:n for n,bi in enumerate(sorted({b['bi'] for b in bl}),1)}
    for b in bl:
        lev=level_of(b); dn=drive_name(pid)
        name=('ST%d INT %s'%(lev,dn)) if b['kind']=='INTERSECTION' else 'ST%d %s B%02d%s'%(lev,dn,num[b['bi']],b['part'])
        meta=dict(name=name,lode='',level=str(lev),drive=dn,source=src[b['kind']],block='%02d%s'%(num[b['bi']],b['part']),split=('HEIGHT 2.6 LOW<2' if b['split']=='HEIGHT_LOW' else b['split']+' %.1f'%b['target']) if b['split'] else 'NO',bt='Y' if b['bt'] else 'N',ht=round(b['height'],1),zlo=round(b['zlo'],1),ch0=round(b['ch0'],1),ch1=round(b['ch1'],1),
                  hab=round(b['height_above_back'],1),w=round(b['w'],2),t=round(b['t']),g=round(b['g'],3),inplan=0,mined=round(100*b['mined']),dec=round(b['dec_dist']),drill='Y' if b['drill_ok'] else 'N')
        out.append(json.dumps(meta,separators=(',',':'))+'|'+','.join('%.3f'%x for v in b['V'] for x in v)+'|'+','.join(str(x+1) for f in b['F'] for x in f))
open('/mnt/project-files/deswik-st-stopes/st_blocks_geometry.txt','w').write('\n'.join(out)+'\n')
with open('/mnt/project-files/deswik-st-stopes/st_extra_strings.csv','w') as f:
    f.write('drive,level,ore_t,x,y,z\n')
    for i,l in enumerate(L):
        if by.get('X%03d'%i):
            for p in l['pts']: f.write('X%03d,%d,%.0f,%.2f,%.2f,%.2f\n'%(i,round(l['level']-6000),l['ore_t'],*p))
print(len(out),'blocks',collections.Counter(b['kind'] for b in k))
