import re, base64, struct, glob, os
from lxml import etree
def b64(s): return base64.b64decode(s)
def parse_verts(s):
    b=b64(s); n=struct.unpack_from('<i',b,0)[0]
    rec=(len(b)-4)//n if n else 0
    out=[]
    for i in range(n):
        vals=struct.unpack_from('<'+'d'*(rec//8),b,4+i*rec); out.append(vals)
    return out, rec
def entities(path):
    data=open(path,'rb').read()
    root=etree.fromstring(data, etree.XMLParser(huge_tree=True,recover=True))
    res=[]
    for it in root.iter('O'):
        if it.get('F')!='_Item': continue
        T=it.get('T')
        if T not in ('dwPolyline','dwPolyface','vdPolyline','vdPolyface'): continue
        e={'type':T,'props':{}}
        for c in it:
            F=c.get('F')
            if c.tag=='P' and F in ('Handle','ToolTip'): e[F]=c.get('V')
            elif c.tag=='H' and F=='Layer': e['layer']=c.get('V')
            elif c.tag=='O' and F=='XProperties':
                for xp in c:
                    if xp.tag=='O':
                        nm=pv=None
                        for p in xp:
                            if p.get('F')=='Name': nm=p.get('V')
                            if p.get('F')=='PropValue': pv=p.get('V')
                        e['props'][nm]=pv
            elif c.tag=='O' and F=='VertexList':
                s=c.find('P'); e['verts'],e['vrec']=parse_verts(s.get('V'))
            elif c.tag=='O' and F=='FaceList':
                s=c.find('P'); e['faceraw']=s.get('V') if s is not None else None
            elif c.tag=='O' and F=='PenColor':
                p=c.find('P'); e['color']=p.get('V') if p is not None else None
        res.append(e)
    return res
def layers(path):
    t=open(path,encoding='utf-8-sig').read()
    out={}
    for m in re.finditer(r'<O F="_Item" T="vdLayer">(.*?)</O>',t):
        pass
    root=etree.fromstring(open(path,'rb').read(), etree.XMLParser(huge_tree=True,recover=True))
    for it in root.iter('O'):
        if it.get('T')=='vdLayer':
            h=n=None
            for c in it:
                if c.get('F')=='Handle': h=c.get('V')
                if c.get('F')=='Name': n=c.get('V')
            out[h]=n
    return out
