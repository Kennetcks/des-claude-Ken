import pickle, numpy as np, trimesh
def load(fn='/home/claude/work/st_final_v4/kept_v4.pkl'):
    out=[]
    for b in pickle.load(open(fn,'rb')):
        m=trimesh.Trimesh(np.asarray(b['V'],float),np.asarray(b['F'],int),process=True)
        if m.volume<0: m.invert()
        lev=str(int(round(b['level']-6000)))
        out.append(dict(layer='VEIN',name='%s %d%s'%(b['pid'],b['bi'],b['part']),source='VEIN STRING',level=lev,drive=b['pid'],block='%02d%s'%(b['bi']+1,b['part']),
            split=b['split'] or 'NO',group='',ch0=float(b['ch0']),ch1=float(b['ch1']),base=float(b['zlo']),ht=float(b['height']),thick=float(b['w']),
            t=float(b['t']),au=float(b['g']),dens=float(b['t']/max(b['vol'],1e-6)),vol=float(b['vol']),mesh=m))
    return out
