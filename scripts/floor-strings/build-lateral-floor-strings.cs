// Rebuild DESIGN\FLOOR STRINGS: plain per-drive floor lines, ends cut where the line leaves its own solid, dips smoothed (replaces current strings)
// Body of: object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out)
// Usings: 

string OUTLAYER = "DESIGN\\FLOOR STRINGS";
var sol = new List<Deswik.Graphics.Primaries.Figure>(); var isV = new List<bool>(); var parent=new List<string>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) {
  if (f.Layer==null || f.Layer.Name!="SCHEDULER\\TASK SOLIDS" || f.asPolyface==null) continue;
  if (Convert.ToString(f.AttributeValueGet("STO_DEV",""))!="DEV") continue;
  sol.Add(f); isV.Add(Convert.ToString(f.AttributeValueGet("LAT_VERT",""))=="VERT"); parent.Add(Convert.ToString(f.AttributeValueGet("DIS Parent GUID","")));
}
int N=sol.Count;
var verts = new List<double[]>[N]; var cog = new double[N][]; var tris = new List<double[]>[N]; var bb = new double[N][];
var map = new Dictionary<string,List<int>>(); var keyPt = new Dictionary<string,double[]>();
for (int i=0;i<N;i++) {
  var pf=sol[i].asPolyface; var all=new List<double[]>();
  foreach (Deswik.Graphics.Geometry.Point v in pf.VertexList) all.Add(new double[]{v.x,v.y,v.z});
  verts[i]=new List<double[]>(); var seen=new HashSet<string>(); double sx=0,sy=0,sz=0;
  foreach (var p in all) {
    string k = Math.Round(p[0]*20)+","+Math.Round(p[1]*20)+","+Math.Round(p[2]*20);
    if (!seen.Add(k)) continue;
    verts[i].Add(p); sx+=p[0]; sy+=p[1]; sz+=p[2]; keyPt[k]=p;
    List<int> l; if (!map.TryGetValue(k,out l)) { l=new List<int>(); map[k]=l; } l.Add(i);
  }
  int c=verts[i].Count; cog[i]=new double[]{sx/c,sy/c,sz/c};
  bb[i]=new double[]{verts[i].Min(p=>p[0]),verts[i].Max(p=>p[0]),verts[i].Min(p=>p[1]),verts[i].Max(p=>p[1])};
  tris[i]=new List<double[]>();
  if (!isV[i]) {
    var fl=pf.FaceList; int cnt=pf.FaceListCount;
    for (int j=0;j+4<cnt;j+=5) { var u=new List<int>(); for (int q=0;q<4;q++){ int k2=Math.Abs(fl[j+q])-1; if(k2>=0&&k2<all.Count&&!u.Contains(k2)) u.Add(k2);} 
      for (int t=1;t+1<u.Count;t++) { var a=all[u[0]]; var b=all[u[t]]; var d=all[u[t+1]]; tris[i].Add(new double[]{a[0],a[1],a[2],b[0],b[1],b[2],d[0],d[1],d[2]}); } }
  }
}
Func<int,double,double,double[]> hitAt = (s,x,y) => {
  if (x<bb[s][0]||x>bb[s][1]||y<bb[s][2]||y>bb[s][3]) return null;
  double lo=double.NaN, hi=double.NaN;
  foreach (var t in tris[s]) {
    double x1=t[0],y1=t[1],x2=t[3],y2=t[4],x3=t[6],y3=t[7];
    double den=(y2-y3)*(x1-x3)+(x3-x2)*(y1-y3); if (Math.Abs(den)<1e-12) continue;
    double l1=((y2-y3)*(x-x3)+(x3-x2)*(y-y3))/den, l2=((y3-y1)*(x-x3)+(x1-x3)*(y-y3))/den, l3=1-l1-l2;
    if (l1<-1e-9||l2<-1e-9||l3<-1e-9) continue;
    double z=l1*t[2]+l2*t[5]+l3*t[8]; if (double.IsNaN(lo)||z<lo) lo=z; if (double.IsNaN(hi)||z>hi) hi=z; }
  if (double.IsNaN(lo) || hi-lo<0.2) return null; return new double[]{lo,hi}; };
double G=10; var sgrid=new Dictionary<string,List<int>>();
for (int i=0;i<N;i++) { if (isV[i]) continue; for (int gx=(int)Math.Floor(bb[i][0]/G); gx<=(int)Math.Floor(bb[i][1]/G); gx++) for (int gy=(int)Math.Floor(bb[i][2]/G); gy<=(int)Math.Floor(bb[i][3]/G); gy++) { string k=gx+","+gy; List<int> l; if(!sgrid.TryGetValue(k,out l)){l=new List<int>();sgrid[k]=l;} l.Add(i);} }
Func<double,double,double,double> dropToFloor = (x,y,z0) => {
  var hits=new List<double[]>(); List<int> cl; if (!sgrid.TryGetValue(Math.Floor(x/G)+","+Math.Floor(y/G), out cl)) return double.NaN;
  foreach (int s in cl) { var h=hitAt(s,x,y); if (h!=null) hits.Add(h); }
  if (hits.Count==0) return double.NaN;
  double[] start=null; double bestd=double.MaxValue;
  foreach (var h in hits) { double d = (z0>=h[0]-0.3 && z0<=h[1]+0.3) ? 0 : Math.Min(Math.Abs(z0-h[0]),Math.Abs(z0-h[1])); if (d<bestd){bestd=d;start=h;} }
  if (bestd>3) return double.NaN;
  double cur=start[0]; bool changed=true;
  while (changed) { changed=false; foreach (var h in hits) if (h[1]>=cur-0.3 && h[0]<cur-0.01) { cur=h[0]; changed=true; } }
  return cur; };
Func<List<double[]>,double[]> floorMid = (pts) => {
  double best=-1; double[] a=pts[0], b=pts[0];
  for (int x=0;x<pts.Count;x++) for (int y=x+1;y<pts.Count;y++) { double dx=pts[x][0]-pts[y][0], dy=pts[x][1]-pts[y][1]; double d=dx*dx+dy*dy; if(d>best){best=d;a=pts[x];b=pts[y];} }
  return new double[]{(a[0]+b[0])/2,(a[1]+b[1])/2,pts.Min(p=>p[2])}; };
Func<List<double[]>,double[]> mean = (pts) => new double[]{pts.Average(p=>p[0]),pts.Average(p=>p[1]),pts.Average(p=>p[2])};
var nodes=new List<double[]>(); var nodeSolids=new List<List<int>>(); var nodeV=new List<bool>(); var endOf=new Dictionary<int,int>();
var bounds=new List<int>[N]; for(int i=0;i<N;i++) bounds[i]=new List<int>();
var pairPts=new Dictionary<long,List<double[]>>();
foreach (var kv in map) { var l=kv.Value; for (int a=0;a<l.Count;a++) for (int b=a+1;b<l.Count;b++) { int x=l[a], y=l[b]; if (parent[x]!=parent[y] || isV[x]!=isV[y]) continue; long key=(long)Math.Min(x,y)*100000+Math.Max(x,y); List<double[]> s; if(!pairPts.TryGetValue(key,out s)){s=new List<double[]>();pairPts[key]=s;} s.Add(keyPt[kv.Key]); } }
foreach (var kv in pairPts) { if (kv.Value.Count<3) continue; int a=(int)(kv.Key/100000), b=(int)(kv.Key%100000);
  nodes.Add(isV[a]?mean(kv.Value):floorMid(kv.Value)); nodeSolids.Add(new List<int>{a,b}); nodeV.Add(isV[a]); int id=nodes.Count-1; bounds[a].Add(id); bounds[b].Add(id); }
var edges=new List<int[]>();
for (int i=0;i<N;i++) { var B=bounds[i];
  if (B.Count==2) edges.Add(new int[]{B[0],B[1]});
  else if (B.Count==1) { nodes.Add(new double[]{cog[i][0],cog[i][1],cog[i][2]}); nodeSolids.Add(new List<int>{i}); nodeV.Add(isV[i]); int id=nodes.Count-1; endOf[id]=i; edges.Add(new int[]{B[0],id}); }
  else if (B.Count>=3) { nodes.Add(new double[]{cog[i][0],cog[i][1],cog[i][2]}); nodeSolids.Add(new List<int>{i}); nodeV.Add(isV[i]); int h=nodes.Count-1; foreach(int b in B) edges.Add(new int[]{b,h}); }
  else { double[] s,e;
    if (isV[i]) { double lo=verts[i].Min(p=>p[2]), hi=verts[i].Max(p=>p[2]); s=mean(verts[i].Where(p=>p[2]<=lo+0.3).ToList()); e=mean(verts[i].Where(p=>p[2]>=hi-0.3).ToList()); }
    else { double best=-1; double[] a=null,b=null; foreach(var p in verts[i]) foreach(var q in verts[i]){double d=(p[0]-q[0])*(p[0]-q[0])+(p[1]-q[1])*(p[1]-q[1]); if(d>best){best=d;a=p;b=q;}} s=new double[]{a[0],a[1],cog[i][2]}; e=new double[]{b[0],b[1],cog[i][2]}; }
    nodes.Add(s); nodeSolids.Add(new List<int>{i}); nodeV.Add(isV[i]); int si=nodes.Count-1; nodes.Add(e); nodeSolids.Add(new List<int>{i}); nodeV.Add(isV[i]); int ei=nodes.Count-1; if(!isV[i]){endOf[si]=i; endOf[ei]=i;} edges.Add(new int[]{si,ei}); }
}
// interior floor heights
var nud=new double[]{0,0.05,0.15,0.3,0.6,1.0};
for (int n=0;n<nodes.Count;n++) { if (nodeV[n]||endOf.ContainsKey(n)) continue; double z=double.NaN;
  foreach (double nd in nud) { foreach (int s in nodeSolids[n]) { double dx=cog[s][0]-nodes[n][0], dy=cog[s][1]-nodes[n][1]; double L=Math.Sqrt(dx*dx+dy*dy); if(L<1e-6)L=1; double r=dropToFloor(nodes[n][0]+dx/L*nd,nodes[n][1]+dy/L*nd,nodes[n][2]); if(!double.IsNaN(r)&&(double.IsNaN(z)||r<z)) z=r; } if(!double.IsNaN(z)) break; }
  if (!double.IsNaN(z)) nodes[n][2]=z; }
var adj=new Dictionary<int,List<int>>(); for(int k=0;k<edges.Count;k++) foreach(int v in edges[k]){ List<int> l; if(!adj.TryGetValue(v,out l)){l=new List<int>();adj[v]=l;} l.Add(k);} 
var eUsed=new bool[edges.Count]; var chainsIdx=new List<List<int>>();
Func<int,int,List<int>> walk = (start,ek) => { var ch=new List<int>{start}; int cur=start; int e=ek;
  while(true){ eUsed[e]=true; int nx=edges[e][0]==cur?edges[e][1]:edges[e][0]; ch.Add(nx); cur=nx; if(adj[cur].Count!=2) break; int ne=adj[cur][0]==e?adj[cur][1]:adj[cur][0]; if(eUsed[ne]) break; e=ne; } return ch; };
foreach (var kv in adj) if (kv.Value.Count!=2) foreach(int e in kv.Value) if(!eUsed[e]) chainsIdx.Add(walk(kv.Key,e));
for(int e=0;e<edges.Count;e++) if(!eUsed[e]) chainsIdx.Add(walk(edges[e][0],e));
// place each lateral end: run the drive line on from the last inner point until it leaves the end solid
int endsSet=0, endsFallback=0;
foreach (var ch in chainsIdx) for (int side=0;side<2;side++) {
  int en = side==0?ch[0]:ch[ch.Count-1]; int es; if (!endOf.TryGetValue(en,out es)) continue; if (ch.Count<2) continue;
  int an = side==0?ch[1]:ch[ch.Count-2]; var A=nodes[an];
  double dx,dy;
  if (endOf.ContainsKey(an)) { // single-solid drive: use its long axis between the two ends
    dx=nodes[en][0]-A[0]; dy=nodes[en][1]-A[1]; }
  else if (ch.Count>=3) { var B=nodes[side==0?ch[2]:ch[ch.Count-3]]; dx=A[0]-B[0]; dy=A[1]-B[1]; double l0=Math.Sqrt(dx*dx+dy*dy); if (l0<0.5){ dx=cog[es][0]-A[0]; dy=cog[es][1]-A[1]; } }
  else { dx=cog[es][0]-A[0]; dy=cog[es][1]-A[1]; }
  double L=Math.Sqrt(dx*dx+dy*dy); if (L<1e-6) { endsFallback++; continue; } dx/=L; dy/=L;
  if (endOf.ContainsKey(an)) { // single solid: centre the line through the solid, ends where it exits
    var c0=cog[es]; double s0=0; while (s0<200 && hitAt(es,c0[0]+dx*(s0+0.1),c0[1]+dy*(s0+0.1))!=null) s0+=0.1;
    nodes[en]=new double[]{c0[0]+dx*s0,c0[1]+dy*s0,nodes[en][2]}; endsSet++; continue; }
  double s=0; bool any=false; while (s<60) { var h=hitAt(es,A[0]+dx*(s+0.1),A[1]+dy*(s+0.1)); if (h==null) break; s+=0.1; any=true; }
  if (!any || s<0.3) { // the line leaves the solid at once: keep a short stub along the line to the face centre projection
    var fc=floorMid(verts[es].Where(p=>(p[0]-A[0])*dx+(p[1]-A[1])*dy >= verts[es].Max(q=>(q[0]-A[0])*dx+(q[1]-A[1])*dy)-0.3).ToList());
    s=Math.Max(0,(fc[0]-A[0])*dx+(fc[1]-A[1])*dy); endsFallback++; }
  else endsSet++;
  nodes[en]=new double[]{A[0]+dx*s,A[1]+dy*s,A[2]};
}
// single-solid drives: second end needs the same treatment from the other side (handled above per end using its own direction)
// end heights: floor just inside the end, starting at the inner point's level
foreach (var kv in endOf) { int n=kv.Key, es=kv.Value; var E=nodes[n];
  // find neighbour
  List<int> el; if (!adj.TryGetValue(n,out el)) continue; var e=edges[el[0]]; int an=e[0]==n?e[1]:e[0]; var A=nodes[an];
  double dx=A[0]-E[0], dy=A[1]-E[1]; double L=Math.Sqrt(dx*dx+dy*dy); if (L<0.2) { E[2]=A[2]; continue; } dx/=L; dy/=L;
  double z=double.NaN; double zref = endOf.ContainsKey(an) ? cog[es][2] : A[2];
  foreach (double ins in new double[]{0.3,0.6,1.0}) { if (ins>L-0.1) break; double r=dropToFloor(E[0]+dx*ins,E[1]+dy*ins,zref); if(!double.IsNaN(r)&&(double.IsNaN(z)||r<z)) z=r; }
  if (double.IsNaN(z)) { var h=hitAt(es,E[0]+dx*Math.Min(0.3,L/2),E[1]+dy*Math.Min(0.3,L/2)); z = h!=null ? h[0] : A[2]; }
  E[2]=z; }
// single-solid drives: both ends were set from cog-based placement; recompute their heights from own floor
var chains=new List<List<double[]>>(); var chR=new List<bool>();
foreach (var ch in chainsIdx) { var pts=new List<double[]>(); foreach(int n in ch){ var p=nodes[n]; if(pts.Count>0){var q=pts[pts.Count-1]; if(Math.Abs(p[0]-q[0])<0.05&&Math.Abs(p[1]-q[1])<0.05&&Math.Abs(p[2]-q[2])<0.05) continue;} pts.Add(new double[]{p[0],p[1],p[2]}); }
  if (pts.Count<2) continue; chains.Add(pts); chR.Add(nodeV[ch[0]]); }
// smooth floor dips/bumps on laterals: interior point more than 0.75 m off its neighbours' line
int smoothed=0;
for (int c=0;c<chains.Count;c++) { if (chR[c]) continue; var p=chains[c];
  for (int it=0;it<2;it++) for (int k=1;k+1<p.Count;k++) { var a=p[k-1]; var b=p[k+1]; var m=p[k];
    double la=Math.Sqrt((m[0]-a[0])*(m[0]-a[0])+(m[1]-a[1])*(m[1]-a[1])), lb=Math.Sqrt((b[0]-m[0])*(b[0]-m[0])+(b[1]-m[1])*(b[1]-m[1])); if (la+lb<1e-6) continue;
    double zi=a[2]+(b[2]-a[2])*la/(la+lb); if (Math.Abs(m[2]-zi)>0.75) { m[2]=zi; smoothed++; } }
  // ends: if the end step is steeper than 1:4, carry the grade of the previous segment
  for (int side=0;side<2;side++) { if (p.Count<3) continue; var E= side==0?p[0]:p[p.Count-1]; var A= side==0?p[1]:p[p.Count-2]; var B= side==0?p[2]:p[p.Count-3];
    double l1=Math.Sqrt((E[0]-A[0])*(E[0]-A[0])+(E[1]-A[1])*(E[1]-A[1])), l2=Math.Sqrt((A[0]-B[0])*(A[0]-B[0])+(A[1]-B[1])*(A[1]-B[1]));
    if (l1>0.2 && Math.Abs(E[2]-A[2])/l1>0.25 && l2>0.5) { E[2]=A[2]+(A[2]-B[2])/l2*l1; smoothed++; } } }
Deswik.Graphics.Primaries.Layer lay=null; foreach (Deswik.Graphics.Primaries.Layer l in App.Layers) if (l.Name==OUTLAYER) lay=l;
if (lay==null) { App.Layers.Add(OUTLAYER,false,1); foreach (Deswik.Graphics.Primaries.Layer l in App.Layers) if (l.Name==OUTLAYER) lay=l; }
var old=new List<Deswik.Graphics.Primaries.Figure>(); foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (f.Layer!=null && f.Layer.Name==OUTLAYER) old.Add(f);
foreach (var f in old) App.ModelSpace.Entities.Remove(f);
foreach (var pts in chains) { var pl=new Deswik.Graphics.Figures.Polyline(); foreach(var p in pts) pl.AddVertex(p[0],p[1],p[2]); pl.Layer=lay; App.ModelSpace.Entities.AddItem(pl,null,null,true,false); }
App.Redraw(true);
Out.AppendLine(string.Format("Replaced {0}; wrote {1} strings ({2} raises). Ends cut at own solid: {3}; ends on face-centre fallback: {4}; floor points smoothed: {5}", old.Count, chains.Count, chR.Count(r=>r), endsSet, endsFallback, smoothed));
return null;