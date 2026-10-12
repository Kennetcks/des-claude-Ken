"""Last step: write the design to a new .dcf.
Usage: python write_dcf3.py "SITE - new design.dcf" "Strings decline.dcf"
"""
import pickle, numpy as np, struct, base64, uuid, zipfile, copy, sys
from lxml import etree
SRC=sys.argv[2]   # the Strings decline.dcf used as the template
import os
OLDP=os.path.basename(SRC); NEWP=os.path.basename(sys.argv[1])
d=pickle.load(open('design3.pkl','rb'))
COL={'MLA':'7','DEC':'6','ACC':'4','OD':'30','ISP':'200','RAD':'3','RAR':'1'}
zin=zipfile.ZipFile(SRC)
STNAME=[n for n in zin.namelist() if b'Kennet new' in zin.read(n) and n!=os.path.basename(SRC)][0]   # layer part holding the Kennet string
root=etree.fromstring(zin.read(STNAME), etree.XMLParser(huge_tree=True))
ents=[o for o in root.iter('O') if o.get('T')=='vdEntities'][0]
items=[o for o in ents if o.tag=='O' and o.get('F')=='_Item']
def stream(P):
    b=struct.pack('<i',len(P))+b''.join(struct.pack('<4d',p[0],p[1],p[2],0.0) for p in P)
    return base64.b64encode(b).decode()
def is_kennet(it):
    return any(p.get('V')=='Kennet new' for p in it.iter('P'))
tmpl=[it for it in items if not is_kennet(it) and len([x for x in it.find("O[@F='XProperties']") if x.tag=='O'])==27][0]
for it in items:
    if not is_kennet(it): ents.remove(it)
    else:
        for x in it.find("O[@F='XProperties']"):
            if x.tag=='O' and x.find("P[@F='Name']").get('V')=='ACT_TYPE': x.find("P[@F='PropValue']").set('V','DEC')
        it.find("O[@F='VertexList']").find('P').set('V',stream(d['keep']))
def _unused():
    pass
h=1000; n=0
for t,lev,P,fam in d['out']:
    e=copy.deepcopy(tmpl); h+=1; n+=1
    for p in e:
        F=p.get('F')
        if F=='Handle': p.set('V',str(h))
        elif F=='HandleGuid': p.set('V',str(uuid.uuid4()))
        elif F=='ToolTip': p.set('V',str(lev) if lev is not None else t)
        elif F=='PenColor': p.find('P').set('V',COL[t])
        elif F=='VertexList': p.find('P').set('V',stream(P))
        elif F=='PLineDrawFlag': p.set('V','1')
    xp=e.find("O[@F='XProperties']")
    vals={'ACT_TYPE':t,'STO_DEV':'DEV','LAT_VERT':'VERT' if t=='RAR' else 'LAT','LEVEL':'' if lev is None else str(lev),
          'MINE':'W','LODE':('GROUP '+fam) if fam in ('A','B') else '-','DESIGN':'IS design trial 261011','COST_CENTRE':'O' if t=='OD' else 'C'}
    for x in xp:
        if x.tag!='O': continue
        nm=x.find("P[@F='Name']").get('V'); pv=x.find("P[@F='PropValue']")
        if nm in vals:
            pv.set('V',vals[nm]); pv.attrib.pop('T',None)
    ents.append(e)
cnt=ents.find("P[@F='_Count']"); cnt.set('V',str(n+1))
newxml=b'\xef\xbb\xbf'+etree.tostring(root,xml_declaration=True,encoding='utf-8')
with zipfile.ZipFile(sys.argv[1],'w',zipfile.ZIP_DEFLATED) as zo:
    for info in zin.infolist():
        data=newxml if info.filename==STNAME else zin.read(info)
        ni=zipfile.ZipInfo(info.filename.replace(OLDP,NEWP,1),date_time=info.date_time); ni.compress_type=zipfile.ZIP_DEFLATED
        ni.comment=info.comment; ni.external_attr=info.external_attr
        zo.writestr(ni,data)
print('entities',n+1,'max handle',h)
