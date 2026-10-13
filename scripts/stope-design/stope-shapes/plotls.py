import pickle,numpy as np,matplotlib; matplotlib.use('Agg'); import matplotlib.pyplot as plt, sys
from matplotlib.patches import Rectangle, Polygon
res=pickle.load(open('stopes.pkl','rb'))
def col(g):
    return '#bbbbbb' if g<2 else '#00e5ff' if g<2.6 else '#ffff00' if g<3 else '#ffa500' if g<5 else '#ff4040' if g<12 else '#a000ff'
def draw(ax,k):
    cols,sel=res[k]
    for c in cols:
        for p in c['pieces']:
            ax.add_patch(Rectangle((p['ch0'],p['base']),p['ch1']-p['ch0'],p['ht'],fc=col(p['au']),ec='k',lw=0.4))
    for s in sel:
        i,j=s['i'],s['j']; dL,dR=s['dL'],s['dR']
        top={i+q:h for q,h in enumerate(s['tops'])}
        sL=cols[i]['s0']; sR=cols[j]['s1']
        pts=[(sL+dL,0),(sR-dR,0),(sR,top[j])]
        for q in range(j,i-1,-1): pts+= [(cols[q]['s1'],top[q]),(cols[q]['s0'],top[q])]
        pts+=[(sL,top[i])]
        ax.add_patch(Polygon(pts,closed=True,fill=False,ec='r',lw=1.8))
        ax.text((sL+sR)/2,1,'%.0fm\n%.0ft\n%.2f'%(s['L'],s['t'],s['g']),ha='center',fontsize=6)
    ax.set_xlim(cols[0]['s0']-2,cols[-1]['s1']+2); ax.set_ylim(-1,max(c['H'] for c in cols)+1); ax.set_title(k,fontsize=8); ax.set_aspect('equal')
if __name__=="__main__":
  ks=sys.argv[2:]
  fig,axs=plt.subplots(len(ks),1,figsize=(14,3.2*len(ks)))
  for ax,k in zip(np.atleast_1d(axs),ks): draw(ax,k)
  plt.tight_layout(); plt.savefig(sys.argv[1],dpi=110)
