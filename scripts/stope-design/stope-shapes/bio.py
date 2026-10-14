import numpy as np, trimesh, collections
K=['layer','name','source','level','drive','block','split','group','ch0','ch1','base','ht','thick','t','au','dens','vol']
def load(fn='st_blocks_export.tsv'):
    out=[]
    for l in open(fn):
        r=l.rstrip('\n').split('\t'); d=dict(zip(K,r[:17]))
        for k in ['ch0','ch1','base','ht','thick','t','au','dens','vol']: d[k]=float(d[k] or 0)
        V=np.array(list(map(float,r[17].split(',')))).reshape(-1,3); F=np.array(list(map(int,r[18].split(','))))
        tris=[]
        for i in range(0,len(F)-4,5):
            g=F[i:i+5]
            if g[0]==0: break
            a,b,c,dd=g[:4]-1
            tris.append([a,b,c])
            if dd!=a and dd>=0: tris.append([a,c,dd])
        m=trimesh.Trimesh(V,np.array(tris),process=True)
        if m.volume<0: m.invert()
        d['mesh']=m; out.append(d)
    return out
