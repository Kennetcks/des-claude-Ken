import pickle,numpy as np,trimesh,math,collections,json
from trimesh.intersections import slice_mesh_plane
from stopes import drives, columns, TANA
res=pickle.load(open('stopes.pkl','rb'))
def cent(c):
    V=np.vstack([p['mesh'].vertices for p in c['pieces']]); return V[:,:2].mean(0), V[:,2].min()
def clip(m,n,o):
    if m is None or len(m.faces)==0: return None
    d=(m.vertices-o)@n
    if d.min()>=-1e-6: return m
    if d.max()<=1e-6: return None
    r=slice_mesh_plane(m,plane_normal=n,plane_origin=o,cap=True)
    return r if r is not None and len(r.faces)>0 and abs(r.volume)>0.01 else None
import manifold3d as mf
from collapse import collapse
def tom(t): return mf.Manifold(mf.Mesh(vert_properties=np.asarray(t.vertices,dtype=np.float32),tri_verts=np.asarray(t.faces,dtype=np.uint32)))
def munion(parts):
    u=trimesh.boolean.union(parts,engine='manifold') if len(parts)>1 else parts[0]
    c=collapse(u)
    if not c.is_watertight: print('collapse broke', u.is_watertight)
    return c
out=[]; bad=[]
for key,(cols,sel) in res.items():
    C=[cent(c) for c in cols]
    def dirn(k):
        a=C[max(k-1,0)][0]; b=C[min(k+1,len(C)-1)][0]
        if np.linalg.norm(b-a)<1e-3:
            V=np.vstack([p['mesh'].vertices[:,:2] for p in cols[k]['pieces']]); V=V-V.mean(0); w,vec=np.linalg.eigh(V.T@V); d=vec[:,-1]
        else: d=(b-a)
        return d/np.linalg.norm(d)
    for n_,s in enumerate(sel):
        i,j=s['i'],s['j']; parts=[]
        di=dirn(i); dj=dirn(j)
        sL=cols[i]['s0']; sR=cols[j]['s1']
        def P(k,ss,d): mid=(cols[k]['s0']+cols[k]['s1'])/2; return C[k][0]+d*(ss-mid)
        for q in range(i,j+1):
            c=cols[q]; top=s['tops'][q-i]; zf=C[q][1]
            for p in c['pieces']:
                if p['base']>=top-0.01: continue
                m=p['mesh'].copy()
                if p['base']+p['ht']>top+0.01: m=clip(m,np.array([0,0,-1.0]),np.array([0,0,zf+top]))
                for side in ('L','R'):
                    d=s['d'+side]
                    if d<=0 or m is None: continue
                    k=i if side=='L' else j; dv=di if side=='L' else dj; sg=1 if side=='L' else -1
                    zt=s['tops'][0] if side=='L' else s['tops'][-1]; z0=C[k][1]
                    e=sL if side=='L' else sR
                    A=np.r_[P(k,e+sg*d,dv),z0]; Bt=np.r_[P(k,e,dv),z0+zt]; w=np.r_[-dv[1],dv[0],0]
                    nn=np.cross(Bt-A,w); nn/=np.linalg.norm(nn)
                    if nn@np.r_[sg*dv,0]<0: nn=-nn
                    m=clip(m,nn,A)
                if m is not None:
                    dv=dirn(q); V=np.array(m.vertices); c0=V.mean(0)
                    pr=(V[:,:2]-c0[:2])@dv; V[:,:2]+=np.outer(np.sign(pr)*0.05,dv)
                    V[:,2]+=np.sign(V[:,2]-c0[2])*0.05
                    parts.append(trimesh.Trimesh(V,m.faces,process=False))
        u=munion(parts)
        bl=[p['block'] for q in range(i,j+1) for p in cols[q]['pieces']]
        dens=np.mean([p['dens'] for q in range(i,j+1) for p in cols[q]['pieces']])
        p0=cols[i]['pieces'][0]
        out.append(dict(drive=key,level=p0['level'],group=p0['group'],source=p0['source'],layer=p0['layer'],ch0=sL,ch1=sR,L=sR-sL,H=max(s['tops']),t=s['t'],g=s['g'],dL=s['dL'],dR=s['dR'],
            angL=math.degrees(math.atan2(s['dL'],s['tops'][0])),angR=math.degrees(math.atan2(s['dR'],s['tops'][-1])),blocks=' '.join(sorted(set(bl))),
            vol=u.volume,estvol=s['t']/dens,watertight=u.is_watertight,V=u.vertices,F=u.faces))
# intersections >=2.5 as their own stopes
for key,bl in drives.items():
    if '/' not in key: continue
    for p in bl:
        if p['au']>=2.5:
            m=munion([p['mesh']]); out.append(dict(drive=key,level=p['level'],group=p['group'],source='INTERSECTION',layer=p['layer'],ch0=0,ch1=p['ch1'],L=p['ch1'],H=p['ht'],t=p['t'],g=p['au'],dL=0,dR=0,angL=0,angR=0,blocks=p['block'],vol=m.volume,estvol=p['vol'],watertight=m.is_watertight,V=m.vertices,F=m.faces))
pickle.dump(out,open('stope_geoms.pkl','wb'))
r=np.array([o['vol']/o['estvol'] for o in out]); print('n',len(out),'bad',len(bad),bad[:3]); print('vol/est pct',np.percentile(r,[0,1,5,50,95,99,100]).round(3)); print('not watertight',sum(not o['watertight'] for o in out))
print('max angle',max(max(o['angL'],o['angR']) for o in out))
