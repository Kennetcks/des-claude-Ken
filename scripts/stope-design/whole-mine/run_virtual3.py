import sys, json, time, pickle, numpy as np, multiprocessing as mp
sys.path.insert(0,'/home/claude/work'); import design3 as design
from run_all3 import dens
def init(): design.MODEL_PRE='v'
if __name__=='__main__':
    d=design.load_strings(); L=json.load(open('virtual_drives.json'))
    D=[dens(v['pts']) for v in d.values() if v['lv']=='LAT']
    VD=[dens(np.array(l['pts'])-design.ORIGIN) for l in L]
    jobs=[]
    for i,l in enumerate(L):
        p=np.array(l['pts'])-design.ORIGIN; c=p[:,:2].mean(0)
        others=np.vstack([x for x in D+[v for j,v in enumerate(VD) if j!=i] if np.hypot(*(x[:,:2].mean(0)-c))<400])
        jobs.append(('V%03d'%i,'OD',p,others))
    t=time.time(); res={}
    with mp.Pool(4,initializer=init) as pool:
        for pid,out,st in pool.imap_unordered(design.run,jobs): res[pid]=(out,st)
    for pid,(out,st) in res.items():
        for b in out or []: b['virtual']=True; b['level']=L[int(pid[1:])]['level']
    print('virtual drives',len(jobs),'s',round(time.time()-t),'blocks',sum(len(o or []) for o,s in res.values()))
    pickle.dump(res,open('raw_virtual3.pkl','wb'))
