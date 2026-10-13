import csv,collections,numpy as np,pickle,sys
import matplotlib; matplotlib.use('Agg'); import matplotlib.pyplot as plt
S=collections.OrderedDict()
for r in csv.DictReader(open('strings.csv')): S.setdefault(r['id'],(r['act'],r['level'],[]))[2].append((float(r['x']),float(r['y']),float(r['z'])))
K=pickle.load(open('/home/claude/work/st/kept_stxi.pkl','rb'))
col={'DEC':'k','ACC':'tab:blue','OD':'tab:red','RAD':'tab:purple','RAR':'m','EWD':'tab:green','EWR':'g','ISP':'tab:orange','DSP':'tab:brown','SMP':'c'}
fig,ax=plt.subplots(1,2,figsize=(22,13))
for b in K:
  V=np.array(b['V']).reshape(-1,3); ax[0].plot(V[:,0].mean(),V[:,1].mean(),'s',color='0.85',ms=3)
  ax[1].plot([V[:,0].mean()]*2,[V[:,2].min(),V[:,2].max()],color='0.85',lw=2)
for i,(t,l,P) in S.items():
  P=np.array(P)
  if t in('RAR','EWR'): ax[0].plot(P[:,0],P[:,1],'^',color=col[t],ms=7)
  else: ax[0].plot(P[:,0],P[:,1],'-',color=col[t],lw=1.6 if t=='DEC' else 1)
  ax[1].plot(P[:,0],P[:,2],'-',color=col[t],lw=1.4 if t=='DEC' else 0.8)
D=np.array([[float(r['x']),float(r['y']),float(r['z'])] for r in csv.DictReader(open('/mnt/user-data/uploads/work/stopes_mine/decline_design.csv'))])
ax[0].plot(D[:,0],D[:,1],'k--',lw=2); ax[1].plot(D[:,0],D[:,2],'k--',lw=2)
for a in ax: a.set_aspect('equal'); a.grid(alpha=.3)
ax[0].set_title('plan'); ax[1].set_title('long section looking north (x vs RL)')
plt.tight_layout(); plt.savefig(sys.argv[1],dpi=60)
