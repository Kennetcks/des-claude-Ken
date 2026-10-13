"""Where a new access leaves from the END of one of Kennet's ore drives, extend that ore drive instead (his rule: reuse/extend existing strings)."""
import pickle, numpy as np, sys
sys.path.insert(0,'/home/claude/kd'); from geomlib import dp
exec(open('stage2_route_access.py').read().split("# ---- level networks")[0])   # dec_conflict, stope_conflict, path_of
D2=pickle.load(open('stage2.pkl','rb')); S=D2['S']; new=D2['new']
out=[]; i=0; merged=[]
while i<len(new):
    n=new[i]
    if n['t']=='ACC' and n['attach'] in S and S[n['attach']]['t']=='OD' and i+1<len(new) and new[i+1]['t']=='OD':
        h=n['attach']; P=S[h]['P']; A=n['P'][0]
        e=0 if np.linalg.norm(P[0,:2]-A[:2])<2.5 else (-1 if np.linalg.norm(P[-1,:2]-A[:2])<2.5 else None)
        if e is not None:
            C=n['P']; Cs=np.vstack([C[0],C[2:]])            # try without the take-off kink
            if not stope_conflict(Cs,exclude=[],free=7.0) and not dec_conflict(Cs): C=Cs
            ext=np.vstack([C[1:],new[i+1]['P'][1:]]); ext[:,2]=P[e,2]
            S[h]['P']=np.vstack([ext[::-1],P]) if e==0 else np.vstack([P,ext]); S[h]['P']=dp(S[h]['P'],0.15); S[h]['changed']=True
            merged.append((h,n['why'],round(float(np.hypot(*np.diff(ext[:,:2],axis=0).T).sum()+np.linalg.norm(ext[0,:2]-P[e,:2])),1)))
            i+=2; continue
    out.append(n); i+=1
print('ore drives extended instead of new access:'); [print(' ',m) for m in merged]
D2['S']=S; D2['new']=out; D2['merged']=merged; pickle.dump(D2,open('stage2b.pkl','wb'))
