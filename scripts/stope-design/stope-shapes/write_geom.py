import pickle,collections,json,sys
out=pickle.load(open('stope_geoms.pkl','rb'))
cnt=collections.Counter(); lines=[]
out.sort(key=lambda o:(o['drive'],o['ch0']))
for o in out:
    cnt[o['drive']]+=1
    o['name']='ST%s %s S%02d'%(o['level'],o['drive'] if o['source']!='INTERSECTION' else 'INT '+o['drive'],cnt[o['drive']])
    meta={k:(round(float(o[k]),3) if not isinstance(o[k],str) else o[k]) for k in ['name','drive','level','group','source','ch0','ch1','L','H','t','g','dL','dR','angL','angR','blocks']}
    lines.append(json.dumps(meta)+'|'+','.join('%.3f'%x for x in o['V'].ravel())+'|'+','.join(str(int(x)+1) for x in o['F'].ravel()))
pickle.dump(out,open('stope_geoms.pkl','wb'))
open(sys.argv[1],'w').write('\n'.join(lines)+'\n'); print(len(lines))
