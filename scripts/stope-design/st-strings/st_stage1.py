"""stage 1: blocks along Kennet's ST level strings (ODs keep all blocks, accesses only > 1.5 g/t), no overlaps"""
from st_common import *
if __name__=='__main__':
    S=load_st(); dec=np.vstack([dens(v['pts']) for v in S.values() if v['act']=='DEC'])
    D={k:dens(v['pts']) for k,v in S.items()}
    J={}
    for k,v in S.items():
        if v['act']=='DEC' or v['len']<5: continue
        c=v['pts'][:,:2].mean(0); others=np.vstack([D[o] for o in D if o!=k and np.hypot(*(D[o][:,:2].mean(0)-c))<400])
        J[k]=(k,v['act'],v['pts'],others,(lambda out,k=k:clean(out,k,dec)))
    t=time.time(); blocks={}
    with mp.Pool(4,initializer=init) as pool:
        for pid,out,st in pool.imap_unordered(design.run,[j[:4] for j in sorted(J.values(),key=lambda j:-len(j[2]))]): blocks[pid]=J[pid][4](out)
    print('designed',len(J),'strings in',round(time.time()-t),'s',collections.Counter(v['act'] for v in S.values()))
    blocks=resolve(blocks,J)
    kept=[b for p in blocks for b in blocks[p]]; kept,nd=self_fix(kept); print('kink drops',nd)
    print(summary(kept,'ST blocks'))
    pickle.dump(kept,open(W+'kept_st.pkl','wb'))
