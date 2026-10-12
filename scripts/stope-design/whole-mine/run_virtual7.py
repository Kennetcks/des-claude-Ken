import sys, json, time, pickle, numpy as np, multiprocessing as mp
sys.path.insert(0,'/home/claude/work'); import design6 as design
from run_all6 import dens
def init(): design.MODEL_PRE='n'
if __name__=='__main__':
    d=design.load_strings(); L0=json.load(open('virtual_drives.json')); L=json.load(open('virtual_drives2.json'))
    D=[dens(v['pts']) for v in d.values() if v['lv']=='LAT']
    V0=[dens(np.array(l['pts'])-design.ORIGIN) for l in L0]
    VD=[dens(np.array(l['pts'])-design.ORIGIN) for l in L]
    jobs=[]
    for i,l in enumerate(L):
        p=np.array(l['pts'])-design.ORIGIN; c=p[:,:2].mean(0)
        others=np.vstack([x for x in D+V0+[v for j,v in enumerate(VD) if j!=i] if np.hypot(*(x[:,:2].mean(0)-c))<400])
        jobs.append(('T%03d'%i,'OD',p,others))
    t=time.time(); res={}
    with mp.Pool(4,initializer=init) as pool:
        for pid,out,st in pool.imap_unordered(design.run,jobs): res[pid]=(out,st)
    for pid,(out,st) in res.items():
        for b in out or []: b['virtual']=True; b['level']=L[int(pid[1:])]['level']
    print('trial drives v7',len(jobs),'s',round(time.time()-t),'blocks',sum(len(o or []) for o,s in res.values()))
    pickle.dump(res,open('raw_virtual7.pkl','wb'))
