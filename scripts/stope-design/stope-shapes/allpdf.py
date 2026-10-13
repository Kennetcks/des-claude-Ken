import pickle,numpy as np,matplotlib; matplotlib.use('Agg'); import matplotlib.pyplot as plt
from matplotlib.backends.backend_pdf import PdfPages
import plotls
res=plotls.res
keys=sorted([k for k,(c,s) in res.items() if s], key=lambda k:-float(res[k][0][0]['pieces'][0]['level']))
with PdfPages('/mnt/project-files/deswik-st-stopes/stope_shapes_long_sections.pdf') as pdf:
    for i in range(0,len(keys),5):
        ks=keys[i:i+5]; fig,axs=plt.subplots(len(ks),1,figsize=(16,3.2*len(ks)))
        for ax,k in zip(np.atleast_1d(axs),ks):
            plotls.draw(ax,k); ax.set_title('L%s  %s'%(res[k][0][0]['pieces'][0]['level'],k),fontsize=9)
        plt.tight_layout(); pdf.savefig(fig); plt.close(fig)
print(len(keys))
