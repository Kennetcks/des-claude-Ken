import sys, time, pickle, numpy as np, multiprocessing as mp
sys.path.insert(0,'/home/claude/work'); import design3 as design
SKIP={'DEC','SMP','EWD','SUB','PST','DSP'}
def dens(p):
    out=[p[0]]
    for a,b in zip(p[:-1],p[1:]):
        n=max(1,int(np.hypot(*(b[:2]-a[:2]))));  out+= [a+(b-a)*k/n for k in range(1,n+1)]
    return np.array(out)
if __name__=='__main__':
    d=design.load_strings()
    D={k:dens(v['pts']) for k,v in d.items() if v['lv']=='LAT'}
    jobs=[]
    for k,v in d.items():
        if v['lv']!='LAT' or v['act'] in SKIP: continue
        c=v['pts'][:,:2].mean(0)
        others=np.vstack([D[o] for o in D if o!=k and np.hypot(*(D[o][:,:2].mean(0)-c))<400])
        jobs.append((k,v['act'],v['pts'],others))
    jobs.sort(key=lambda j:-len(j[2]))
    t=time.time(); res={}
    with mp.Pool(4) as pool:
        for pid,out,st in pool.imap_unordered(design.run,jobs): res[pid]=(out,st)
    print('drives',len(jobs),'s',round(time.time()-t))
    pickle.dump(res,open('./raw_blocks3.pkl','wb'))
    import collections; c=collections.Counter(); n=collections.Counter()
    for pid,(out,st) in res.items():
        c[st]+=1
        for b in out or []: n[b['act']]+=1
    print(c, n)
