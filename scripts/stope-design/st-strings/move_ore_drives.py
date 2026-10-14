"""Stope Final v2 (Kennet 2026-10-13): move each ST ore drive string onto the ore at the drive's own level so the stope
rises from a drive that sits in the orebody (less dilution); 'the ore drive can be moved after'. Accesses and ramp stay."""
import sys, csv, numpy as np
sys.path.insert(0,'/home/claude/work')
from st_common import load_st, ORIGIN
from raster import Model
import os
OUT=os.environ.get('OUT','/home/claude/work/st_final_v2/')
M=Model(os.environ.get('MODEL','s2'))
U=np.arange(-12,12.01,0.5); SS=np.arange(-2,2.01,1.0); ZS=np.arange(0.3,22,1.25)
def lode_u(p,d,n,F):
    """per z-slice centre of the best >=2.5 lode across the station; returns list of (z, u, tonnes)"""
    uu,ss,zz=np.meshgrid(U,SS,ZS,indexing='ij'); xy=p+uu[...,None]*n+ss[...,None]*d
    td,au,mn=M.look(xy[...,0].ravel(),xy[...,1].ravel(),(F+zz).ravel()); sh=uu.shape
    td=td.reshape(sh); au=au.reshape(sh); mn=mn.reshape(sh); ore=(au>=2.5)&(mn==0)&(td>0)
    T=(td*ore).sum(1); Mt=(td*np.where(ore,au-2.5,0)).sum(1)   # (NU,NZ)
    out=[]; prev=None
    for k in range(len(ZS)):
        t=T[:,k]; idx=np.nonzero(t>0)[0]
        if not len(idx): continue
        runs=[[idx[0],idx[0]]]
        for i in idx[1:]:
            if i-runs[-1][1]<=2: runs[-1][1]=i
            else: runs.append([i,i])
        best=None
        for a,b in runs:
            uc=np.average(U[a:b+1],weights=t[a:b+1]); m=Mt[a:b+1,k].sum()
            sc=m*np.exp(-abs(uc-(prev if prev is not None else 0))/(5 if prev is not None else 10))
            if best is None or sc>best[0]: best=(sc,uc,t[a:b+1].sum())
        if best[0]>0: out.append((ZS[k],best[1],best[2])); prev=best[1]
    return out
S=load_st(); rows=[]; rep=[]
for pid,v in S.items():
    p=v['pts']
    if v['act']!='OD' or v['len']<10:
        rows+= [(pid,v['h'],*(q+ORIGIN)) for q in p]; continue
    seg=np.hypot(*np.diff(p[:,:2],axis=0).T); cs=np.r_[0,np.cumsum(seg)]
    st=np.r_[np.arange(0,cs[-1],5.0),cs[-1]]
    P=np.c_[np.interp(st,cs,p[:,0]),np.interp(st,cs,p[:,1]),np.interp(st,cs,p[:,2])]
    off=np.full(len(st),np.nan)
    for i in range(len(st)):
        a=P[max(i-1,0),:2]; b=P[min(i+1,len(st)-1),:2]; d=(b-a)/np.linalg.norm(b-a); n=np.array([-d[1],d[0]])
        L=lode_u(P[i,:2],d,n,P[i,2])
        low=[x for x in L if x[0]<=10]          # ore in the drive or within 10 m above its floor
        if low: off[i]=low[0][1] if low[0][0]<=5 else np.average([x[1] for x in low[:3]],weights=[x[2] for x in low[:3]])
    if np.all(np.isnan(off)): off[:]=0
    else:
        k=np.arange(len(off)); g=~np.isnan(off); off=np.interp(k,k[g],off[g])
    off=np.array([np.median(off[max(0,i-1):i+2]) for i in range(len(off))])
    off=np.convolve(np.pad(off,2,mode='edge'),np.ones(5)/5,'valid')
    for _ in range(50):                         # drive turns gently: <= 2 m sideways per 5 m
        for i in range(1,len(off)):
            if abs(off[i]-off[i-1])>2: off[i]=off[i-1]+np.sign(off[i]-off[i-1])*2
    off[np.abs(off)<1.0]=0
    Q=P.copy()
    for i in range(len(st)):
        a=P[max(i-1,0),:2]; b=P[min(i+1,len(st)-1),:2]; d=(b-a)/np.linalg.norm(b-a); n=np.array([-d[1],d[0]]); Q[i,:2]=P[i,:2]+off[i]*n
    if np.abs(off).max()<1.5: Q=p      # not worth moving
    rows+= [(pid,v['h'],*(q+ORIGIN)) for q in Q]
    rep.append((pid,v['h'],round(float(np.abs(off).mean()),1),round(float(np.abs(off).max()),1),bool(np.abs(off).max()>=1.5)))
with open(OUT+'st_strings_moved.csv','w') as f:
    f.write('pid,handle,x,y,z\n')
    for r in rows: f.write('%s,%s,%.3f,%.3f,%.3f\n'%r)
mv=[r for r in rep if r[4]]
print('ore drives',len(rep),'moved',len(mv),'mean move of moved %.1f m'%np.mean([r[2] for r in mv]),'max %.1f'%max(r[3] for r in mv))
import json; json.dump(rep,open(OUT+'moves.json','w'))
for r in sorted(rep,key=lambda r:-r[3])[:15]: print(r)
print([r for r in rep if r[1]=='3CA0D1B'])
