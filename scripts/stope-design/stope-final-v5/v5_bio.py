"""V5 blocks (kept_stxi.pkl, python estimates) in the bio.load format, named like the Deswik export (drive = handle)."""
import pickle, collections, numpy as np, trimesh, sys, os
sys.path.insert(0,'/home/claude/work'); os.environ.setdefault('ST_FILE','/home/claude/work/st_final_v5/st_strings_moved.csv')
from st_common import load_st, ORIGIN
def load(fn='/home/claude/work/st_final_v5/kept_stxi.pkl'):
    k=pickle.load(open(fn,'rb')); S=load_st(); H={p:v['h'] for p,v in S.items()}
    dn=lambda pid:'/'.join(H.get(x,x) for x in pid.split('/'))
    by=collections.defaultdict(list)
    for b in k: by[b['pid']].append(b)
    def level_of(b):
        if b.get('kind')=='INTERSECTION': return level_of(by[b['lev_from']][0])
        if b['pid'].startswith('X'): return int(round(b['level']-6000))
        return int(round(np.median(S[b['pid']]['pts'][:,2])+ORIGIN[2]-6000))
    src={'OD':'ST ORE DRIVE','ACC':'ST ACCESS','EXTRA':'EXTRA STRING','INTERSECTION':'INTERSECTION'}
    out=[]
    for pid,bl in by.items():
        num={bi:n for n,bi in enumerate(sorted({b['bi'] for b in bl}),1)}
        for b in bl:
            m=trimesh.Trimesh(np.asarray(b['V'],float),np.asarray(b['F'],int),process=True)
            if m.volume<0: m.invert()
            lev=level_of(b)
            out.append(dict(layer='V5',name='%s B%02d%s'%(dn(pid),num[b['bi']],b['part']),source=src[b['kind']],level=str(lev),drive=dn(pid),
                block='%02d%s'%(num[b['bi']],b['part']),split=b['split'] or 'NO',group='',ch0=float(b['ch0']),ch1=float(b['ch1']),base=float(b['zlo']),
                ht=float(b['height']),thick=float(b['w']),t=float(b['t']),au=float(b['g']),dens=float(b['t']/max(b['vol'],1e-6)),vol=float(b['vol']),mesh=m))
    return out
