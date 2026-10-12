"""Exact-ish overlap check between block solids: for every pair whose boxes overlap, sample 0.25 m points in the
shared box and count points inside both meshes (vertical ray parity). Usage: python3 overlap_check.py kept.pkl [min_m3]"""
import sys, pickle, numpy as np, collections
from scipy.spatial import cKDTree
STEP=0.25
def inside(V,F,gx,gy,zs):
    A,B,C=V[F[:,0]],V[F[:,1]],V[F[:,2]]; out=np.zeros((len(gx),len(zs)),bool)
    for a,b,c in zip(A,B,C):
        den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
        if abs(den)<1e-12: continue
        w1=((b[1]-c[1])*(gx-c[0])+(c[0]-b[0])*(gy-c[1]))/den; w2=((c[1]-a[1])*(gx-c[0])+(a[0]-c[0])*(gy-c[1]))/den; w3=1-w1-w2
        hit=(w1>=0)&(w2>=0)&(w3>=0)
        if not hit.any(): continue
        zt=w1*a[2]+w2*b[2]+w3*c[2]; out[hit]^=(zs[None,:]<zt[hit,None])
    return out
def overlap(b1,b2):
    V1=np.asarray(b1['V'],float); V2=np.asarray(b2['V'],float)
    lo=np.maximum(V1.min(0),V2.min(0)); hi=np.minimum(V1.max(0),V2.max(0))
    if np.any(hi-lo<=STEP): return 0.0
    gx,gy=np.meshgrid(np.arange(lo[0]+STEP/2,hi[0],STEP),np.arange(lo[1]+STEP/2,hi[1],STEP),indexing='ij'); gx=gx.ravel()+1e-4; gy=gy.ravel()+1e-4
    zs=np.arange(lo[2]+STEP/2,hi[2],STEP)+1e-4
    return float((inside(V1,np.asarray(b1['F']),gx,gy,zs)&inside(V2,np.asarray(b2['F']),gx,gy,zs)).sum()*STEP**3)
def pairs(k):
    lo=np.array([np.asarray(b['V']).min(0) for b in k]); hi=np.array([np.asarray(b['V']).max(0) for b in k])
    c=(lo+hi)/2; r=np.linalg.norm(hi-lo,axis=1)/2; tr=cKDTree(c)
    for i,j in tr.query_pairs(r.max()*2):
        if np.all(np.minimum(hi[i],hi[j])-np.maximum(lo[i],lo[j])>STEP): yield i,j
if __name__=='__main__':
    k=pickle.load(open(sys.argv[1],'rb')); mn=float(sys.argv[2]) if len(sys.argv)>2 else 1.0
    res=[]
    for i,j in pairs(k):
        v=overlap(k[i],k[j])
        if v>=mn: res.append((v,i,j))
    same=[r for r in res if k[r[1]]['pid']==k[r[2]]['pid']]
    print('overlapping pairs >= %.1f m3: %d (same drive %d, different drives %d), total overlap %.0f m3'%(mn,len(res),len(same),len(res)-len(same),sum(r[0] for r in res)))
    for v,i,j in sorted(res,reverse=True)[:15]: print('  %.0f m3  %s %s/%s%s  vs  %s %s/%s%s'%(v,k[i]['pid'],k[i]['bi'],k[i]['part'],k[i].get('kind',''),k[j]['pid'],k[j]['bi'],k[j]['part'],k[j].get('kind','')))
    pickle.dump(res,open(sys.argv[1].replace('.pkl','_overlaps.pkl'),'wb'))
