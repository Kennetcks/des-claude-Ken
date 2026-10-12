import pickle, numpy as np
exec(open('design3.py').read().split('# ---------------- decline')[0])
import json,os
ACCL=json.load(open('acc_len.json')) if os.path.exists('acc_len.json') else {}
A=pickle.load(open('d3a.pkl','rb')); keep,nd,D,NK,apex=A['keep'],A['nd'],A['D'],A['NK'],A['apex']
GR=7.0; RF=22.0
def fillet_path(P,r=RF,step=1.5):
    P=[np.asarray(p,float) for p in P]; out=[P[0]]
    for a,b,c in zip(P[:-2],P[1:-1],P[2:]):
        u=(b-a)/np.linalg.norm(b-a); v=(c-b)/np.linalg.norm(c-b); cosang=np.clip(u@v,-1,1); th=np.arccos(cosang)
        if th<1e-3: out.append(b); continue
        t=r*np.tan(th/2); p1=b-u*t; p2=b+v*t
        sgn=np.sign(u[0]*v[1]-u[1]*v[0]); nrm=np.array([-u[1],u[0]])*sgn; cen=p1+nrm*r
        a0=np.arctan2(*(p1-cen)[::-1]); a1=np.arctan2(*(p2-cen)[::-1])
        da=(a1-a0+np.pi)%(2*np.pi)-np.pi
        out.append(p1)
        for f in np.linspace(0,1,max(2,int(abs(da)*r/step)+1))[1:]: out.append(cen+r*np.array([np.cos(a0+da*f),np.sin(a0+da*f)]))
    out.append(P[-1])
    # resample straights
    res=[out[0]]
    for q in out[1:]:
        L=np.linalg.norm(q-res[-1]); n=max(1,int(np.ceil(L/step)))
        for f in np.linspace(0,1,n+1)[1:]: res.append(res[-1]+(q-res[-1])*f if False else None) if False else None
        base=res[-1]
        for f in np.linspace(0,1,n+1)[1:]: res.append(base+(q-base)*f)
    return np.array(res)
# ---------- top ramp (incline 1:7 up) branching tangentially off the Kennet section
sK=np.r_[0,np.cumsum(np.linalg.norm(np.diff(keep,axis=0),axis=1))]
i0=int(np.searchsorted(sK,226)); J0=keep[i0]; hK=(keep[i0+1]-keep[i0-1])[:2]; hK/=np.linalg.norm(hK)
eR=np.array([hK[1],-hK[0]])   # right-hand perpendicular (east)
wps=[J0[:2],J0[:2]+eR*(12+RF),np.array([8268.,63400.]),np.array([8145.,63440.])]
rp=fillet_path(wps)
zs=J0[2]
ul=np.r_[0,np.cumsum(np.linalg.norm(np.diff(rp,axis=0),axis=1))]; ramp=np.c_[rp,zs+ul/GR]
# snap ramp start exactly onto the Kennet string
best=None
for j in range(len(keep)-1):
    a_,b_=keep[j],keep[j+1]; ab=b_[:2]-a_[:2]; tt=np.clip((ramp[0,:2]-a_[:2])@ab/(ab@ab),0,1); q=a_+(b_-a_)*tt
    dd=np.linalg.norm(q[:2]-ramp[0,:2])
    if best is None or dd<best[0]: best=(dd,q)
ramp[0]=best[1]
ZTOP=6421.8; ramp=ramp[:int(np.argmax(ramp[:,2]>=ZTOP))+1] if (ramp[:,2]>=ZTOP).any() else ramp
print('ramp len',round(ul[len(ramp)-1]),'z',ramp[0,2].round(1),'->',ramp[-1,2].round(1))
# ---------- helpers
ALL=np.vstack([D,ramp])
def tangent(P,i):
    a=P[max(0,i-2)]; b=P[min(len(P)-1,i+2)]; t=(b-a)[:2]; return t/np.linalg.norm(t)
def nearest_on(poly,pt):
    best=None
    for a,b in zip(poly[:-1],poly[1:]):
        ab=b[:2]-a[:2]; t=np.clip((pt-a[:2])@ab/(ab@ab+1e-9),0,1); q=a+(b-a)*t; d=np.linalg.norm(q[:2]-pt)
        if best is None or d<best[0]: best=(d,q)
    return best
def seg_clear(A_,B_,z,skip=12):
    near=ALL[abs(ALL[:,2]-z)<7]
    if not len(near): return True
    for f in np.linspace(0,1,50):
        q=A_[:2]+(B_[:2]-A_[:2])*f
        if np.linalg.norm(q-A_[:2])<skip: continue
        if np.min(np.linalg.norm(near[:,:2]-q,axis=1))<14: return False
    return True
def loop_centre(z):
    m=abs(nd[:,2]-z)<11
    return nd[m,:2].mean(0) if m.sum()>10 else None
def route(P0,T,z):
    if seg_clear(P0,T,z): return [P0[:2],T[:2]]
    c=loop_centre(z)
    if c is None: return [P0[:2],T[:2]]
    near=nd[abs(nd[:,2]-z)<11]; rr=np.max(np.linalg.norm(near[:,:2]-c,axis=1))+20
    a0=np.arctan2(*(P0[:2]-c)[::-1]); a1=np.arctan2(*(T[:2]-c)[::-1]); best=None
    for sgn in (1,-1):
        da=((a1-a0)%(2*np.pi)) if sgn>0 else -((a0-a1)%(2*np.pi))
        angs=np.linspace(a0,a0+da,max(3,int(abs(da)/0.5)+2))[1:-1]
        pts=[P0[:2]]+[c+rr*np.array([np.cos(a),np.sin(a)]) for a in angs]+[T[:2]]
        L=sum(np.linalg.norm(b-a) for a,b in zip(pts[:-1],pts[1:]))
        if best is None or L<best[0]: best=(L,pts)
    return best[1]
def pz(pts,z0,z1):
    pts=np.array(pts); cum=np.r_[0,np.cumsum(np.linalg.norm(np.diff(pts,axis=0),axis=1))]
    return np.c_[pts,z0+(z1-z0)*cum/max(cum[-1],1e-9)]

def fillet_keep(P,r=15.0,keep0=10.0,step=1.5):
    P=[np.asarray(p,float) for p in P]
    if len(P)<3: return P
    out=[P[0]]; prev_t=0.0
    for k_,(a,b,c) in enumerate(zip(P[:-2],P[1:-1],P[2:])):
        u=(b-a)/np.linalg.norm(b-a); v=(c-b)/np.linalg.norm(c-b); th=np.arccos(np.clip(u@v,-1,1))
        if th<1e-3: out.append(b); continue
        L1=np.linalg.norm(b-a)-(keep0 if k_==0 else prev_t); L2=np.linalg.norm(c-b)/2
        rr=min(r,max(L1,0.5)/np.tan(th/2),L2/np.tan(th/2)); t=rr*np.tan(th/2)
        p1=b-u*t; p2=b+v*t; sgn=np.sign(u[0]*v[1]-u[1]*v[0]); cen=p1+np.array([-u[1],u[0]])*sgn*rr
        a0=np.arctan2(*(p1-cen)[::-1]); a1=np.arctan2(*(p2-cen)[::-1]); da=(a1-a0+np.pi)%(2*np.pi)-np.pi
        out.append(p1)
        for f in np.linspace(0,1,max(2,int(abs(da)*rr/step)+1))[1:]: out.append(cen+rr*np.array([np.cos(a0+da*f),np.sin(a0+da*f)]))
        prev_t=t
    out.append(P[-1]); return out

# ---------- levels
out=[('DEC',None,nd,'DEC'),('DEC',None,ramp,'RAMP')]
radraw={'A':[],'B':[]}; info=[]
apz=np.array([p[2] for p in apex])
for L in sorted(LV,key=lambda L:-L['zb']):
    g,zb,lev=L['grp'],L['zb'],L['lev']
    ods=[od_line(c) for c in L['cl']]
    for o in ods: o[:,2]=zb; tw=np.array([sum(s['t'] for s in c) for c in L['cl']])
    cen=np.average([o[:,:2].mean(0) for o in ods],axis=0,weights=tw)
    # candidate take-off points on decline + ramp
    Lg=min(ACCL.get(str(lev),100),140)
    def gather(zlo,zhi,fmin,clr):
        cand=[]
        for name,P in srcs:
            for i in range(2,len(P)-1):
                if P[i,2]<zlo or P[i,2]>zhi: continue
                if name=='D' and i<NK and zt<6386: continue
                t=tangent(P,i); n=np.array([t[1],-t[0]])
                if not (name=='D' and i>=NK): n=n if (cen-P[i,:2])@n>0 else -n
                dirc=(cen-P[i,:2])/np.linalg.norm(cen-P[i,:2]); face=dirc@n
                if face<fmin: continue
                if clr and not seg_clear(P[i],np.r_[P[i,:2]+n*45,P[i,2]],P[i,2],skip=14): continue
                cand.append((abs(P[i,2]-zt)*8+np.linalg.norm(cen-P[i,:2])*0.3-face*10,name,i,n))
        return cand
    zt=zb-Lg/50
    srcs=(('R',ramp),) if zt>6386 else (('D',D),('R',ramp))
    cand=gather(zt-6,zb+Lg/30,0.35,True) or gather(zt-8,zb+Lg/25,0.15,True) or gather(zt-10,zb+Lg/20,0.0,False)
    if not cand:
        i=len(ramp)-1; name='R'; t=tangent(ramp,i); n=np.array([t[1],-t[0]]); n=n if (cen-ramp[i,:2])@n>0 else -n
    else:
        cost,name,i,n=min(cand,key=lambda c:c[0])
    P=D if name=='D' else ramp; J=P[i]
    # first leg straight out 25 m to clear the loop, then to the nearest ore drive
    H=J[:2]+n*25
    dists=[nearest_on(o,H) for o in ods]; m=int(np.argmin([d[0] for d in dists])); T=dists[m][1]
    path=route(np.r_[H,J[2]],T,J[2]); acc=pz(fillet_keep([J[:2]]+path,15.0,10.0),J[2],T[2])
    out.append(('ACC',lev,acc,g))
    for o in ods: out.append(('OD',lev,o,g))
    # link other clusters with an ore-drive link from the end of the access
    for j,o in enumerate(ods):
        if j==m: continue
        d,Q=nearest_on(o,T[:2]); lk=pz(route(T,Q,T[2]),T[2],Q[2]); out.append(('OD',lev,lk,g))
    # ISP and RAD on the first leg
    u=n; perp=np.array([-u[1],u[0]]); rside=perp if (perp@np.array([0,1]) if g=='A' else perp@np.array([1,0]))>=0 else -perp
    B_=np.r_[J[:2]+u*(24 if name=='R' else 12),J[2]]
    nearA=ALL[abs(ALL[:,2]-J[2])<6]
    sd=-rside if np.min(np.linalg.norm(nearA[:,:2]-(B_[:2]-rside*15),axis=1))>=np.min(np.linalg.norm(nearA[:,:2]-(B_[:2]+rside*15),axis=1)) else rside
    out.append(('ISP',lev,np.array([B_,[*(B_[:2]+sd*15),B_[2]+0.3]]),g))
    C_=np.r_[J[:2]+u*20,J[2]]; radraw[g].append((lev,C_,np.r_[C_[:2]+rside*14,C_[2]+0.3],name))
    info.append(dict(J=J.copy(),n=n.copy(),rside=rside.copy(),grp=g,lev=lev,zj=round(J[2],1),src=name,acc=round(float(np.sum(np.linalg.norm(np.diff(acc[:,:2],axis=0),axis=1)))),ncl=len(ods),t=round(L['t'])))
for g,lst in radraw.items():
    lst=[x for x in lst if x[3]=='D' and x[1][2]<6350]
    E=np.array([x[2] for x in lst]); fx=np.polyfit(E[:,2],E[:,0],1); fy=np.polyfit(E[:,2],E[:,1],1); rr=[]
    for lev,C_,e,_ in lst:
        q=e if g=='A' else np.array([np.polyval(fx,e[2]),np.polyval(fy,e[2]),e[2]]); out.append(('RAD',lev,np.array([C_,q]),g)); rr.append(q)
    out.append(('RAR',None,np.array(sorted(rr,key=lambda p:-p[2])),g))
    dd=np.diff(np.array(sorted(rr,key=lambda p:-p[2])),axis=0)
    print(g,'RAD lengths',[round(np.linalg.norm(np.array([np.polyval(fx,e[2]),np.polyval(fy,e[2])])-C_[:2])) for lev,C_,e,_ in lst],'RAR dip',round(np.degrees(np.arctan2(-dd[:,2],np.linalg.norm(dd[:,:2],axis=1))).mean()))
for x in info: print(x)
json.dump({str(x['lev']):x['acc'] for x in info},open('acc_len.json','w'))
pickle.dump(dict(out=out,keep=keep,D=D,nd=nd,ramp=ramp,info=info),open('design3.pkl','wb'))
