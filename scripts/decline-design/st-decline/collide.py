import csv,collections,numpy as np,pickle
from matplotlib.path import Path as MP
from scipy.spatial import ConvexHull
K=pickle.load(open('/home/claude/work/st/kept_stxi.pkl','rb'))
B=[]
for b in K:
  V=np.array(b['V']).reshape(-1,3); h=ConvexHull(V[:,:2]); B.append((MP(V[h.vertices,:2]),V[:,2].min(),V[:,2].max(),b['pid']))
S=collections.OrderedDict()
for r in csv.DictReader(open('strings.csv')): S.setdefault(r['id'],(r['act'],r['level'],r['lode'],[]))[3].append((float(r['x']),float(r['y']),float(r['z'])))
own=collections.defaultdict(set)
for i,(t,l,lo,P) in S.items():
  if t=='OD':
    P=np.array(P)
    for b in K:
      pass
# own drives per group: OD strings of the group -> nearest pid via floor points
odpid={}
import json
def samp(P,st=1.0):
  P=np.array(P); o=[]
  for a,b in zip(P[:-1],P[1:]):
    n=max(int(np.linalg.norm(b-a)/st),1); o.append(a+(b-a)*np.linspace(0,1,n+1)[:,None])
  return np.vstack(o)
gz={}
for i,(t,l,lo,P) in S.items():
  if t=='OD': gz.setdefault(lo,[]).append(np.array(P))
hits=collections.Counter()
for i,(t,l,lo,P) in S.items():
  if t in ('OD',): continue
  Q=samp(P)[1:-1]
  ods=np.vstack(gz.get(lo,[np.zeros((1,3))]))
  for q in Q:
    for mp,z0,z1,pid in B:
      if z0-0.5<q[2]+2.5<z1 and mp.contains_point(q[:2]):
        # inside a stope sitting on this group's own ore drive (within 3 m of it) is the ore drive itself
        if np.min(np.linalg.norm(ods[:,:2]-q[:2],axis=1))<6 and abs(np.median(ods[:,2])-z0)<3: continue
        hits[(t,i,lo)]+=1; break
print(len(hits)); 
for k,v in sorted(hits.items(),key=lambda a:-a[1]): print(k,v)
