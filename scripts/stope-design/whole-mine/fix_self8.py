"""v8: on very sharp kinks of trial lines, blocks further apart than neighbours can still cross; drop the lower-grade
block of any same-drive pair overlapping > 2 m3. Usage: python3 fix_self8.py in.pkl out.pkl"""
import sys, pickle
k=pickle.load(open(sys.argv[1],'rb')); res=pickle.load(open(sys.argv[1].replace('.pkl','_overlaps.pkl'),'rb'))
drop=set()
for v,i,j in sorted(res,reverse=True):
    if v<=2 or i in drop or j in drop: continue
    drop.add(i if k[i]['g']<k[j]['g'] else j)
print('dropped',len(drop),[ (k[i]['pid'],k[i]['bi'],round(k[i]['g'],2)) for i in drop])
pickle.dump([b for n,b in enumerate(k) if n not in drop],open(sys.argv[2],'wb'))
