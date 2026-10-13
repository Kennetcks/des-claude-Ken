"""v8: design every drive (real, first trial lines V, v7 trial lines T) with design8 on the combined raster 'n'."""
import sys, json, time, pickle, numpy as np, multiprocessing as mp, collections
sys.path.insert(0,'/home/claude/work'); import design8 as design
from run_all6 import dens, SKIP
def init(): design.MODEL_PRE='n'
def jobs_all():
    d=design.load_strings(); L0=json.load(open('virtual_drives.json')); L2=json.load(open('virtual_drives2.json'))
    D={k:dens(v['pts']) for k,v in d.items() if v['lv']=='LAT'}
    V0={'V%03d'%i:dens(np.array(l['pts'])-design.ORIGIN) for i,l in enumerate(L0)}
    V2={'T%03d'%i:dens(np.array(l['pts'])-design.ORIGIN) for i,l in enumerate(L2)}
    jobs=[]; lev={}
    for k,v in d.items():
        if v['lv']!='LAT' or v['act'] in SKIP: continue
        c=v['pts'][:,:2].mean(0)
        jobs.append((k,v['act'],v['pts'],np.vstack([D[o] for o in D if o!=k and np.hypot(*(D[o][:,:2].mean(0)-c))<400])))
    allv=dict(V0); allv.update(V2)
    for L,pre in ((L0,'V'),(L2,'T')):
        for i,l in enumerate(L):
            k='%s%03d'%(pre,i); p=np.array(l['pts'])-design.ORIGIN; c=p[:,:2].mean(0); lev[k]=l['level']
            others=np.vstack([x for x in list(D.values())+[v for o,v in allv.items() if o!=k] if np.hypot(*(x[:,:2].mean(0)-c))<400])
            jobs.append((k,'OD',p,others))
    return jobs, lev
if __name__=='__main__':
    jobs,lev=jobs_all(); jobs.sort(key=lambda j:-len(j[2]))
    t=time.time(); res={}
    with mp.Pool(4,initializer=init) as pool:
        for n,(pid,out,st) in enumerate(pool.imap_unordered(design.run,jobs)):
            res[pid]=(out,st)
            if n%50==0: print(n,'of',len(jobs),round(time.time()-t),'s',flush=True)
    for pid,(out,st) in res.items():
        for b in out or []:
            if pid[0] in 'VT' and pid[1:].isdigit(): b['virtual']=True; b['level']=lev[pid]
    print('drives',len(jobs),'s',round(time.time()-t),collections.Counter(st for o,st in res.values()),'blocks',sum(len(o or []) for o,s in res.values()))
    pickle.dump(res,open('/home/claude/work/raw_all8.pkl','wb'))
