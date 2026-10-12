// Re-level drive ends from the floor 0.3-0.6 m inside the end (avoids reading the face wall)
// Body of: object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out)
// Usings: 

string OUTLAYER="DESIGN\\FLOOR STRINGS";
var figs=new List<Deswik.Graphics.Primaries.Figure>(); var S=new List<List<double[]>>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) {
  if (f.Layer==null || f.Layer.Name!=OUTLAYER || f.asPolyline==null) continue;
  var p=new List<double[]>(); foreach (Deswik.Graphics.Geometry.Point q in f.asPolyline.VertexList) p.Add(new double[]{q.x,q.y,q.z});
  figs.Add(f); S.Add(p);
}
var lat=new List<Deswik.Graphics.Primaries.Figure>(); var lbb=new List<double[]>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) {
  if (f.Layer==null||f.Layer.Name!="SCHEDULER\\TASK SOLIDS"||f.asPolyface==null) continue;
  if (Convert.ToString(f.AttributeValueGet("STO_DEV",""))!="DEV"||Convert.ToString(f.AttributeValueGet("LAT_VERT",""))!="LAT") continue;
  double a=1e18,b=-1e18,c=1e18,d=-1e18; foreach (Deswik.Graphics.Geometry.Point v in f.asPolyface.VertexList){a=Math.Min(a,v.x);b=Math.Max(b,v.x);c=Math.Min(c,v.y);d=Math.Max(d,v.y);} lat.Add(f); lbb.Add(new double[]{a,b,c,d});
}
double G=10; var grid=new Dictionary<string,List<int>>();
for (int i=0;i<lat.Count;i++) for (int gx=(int)Math.Floor(lbb[i][0]/G); gx<=(int)Math.Floor(lbb[i][1]/G); gx++) for (int gy=(int)Math.Floor(lbb[i][2]/G); gy<=(int)Math.Floor(lbb[i][3]/G); gy++) { string k=gx+","+gy; List<int> l; if(!grid.TryGetValue(k,out l)){l=new List<int>();grid[k]=l;} l.Add(i);} 
Func<double,double,double,double> drop = (x,y,z0) => {
  List<int> cl; if (!grid.TryGetValue(Math.Floor(x/G)+","+Math.Floor(y/G),out cl)) return double.NaN;
  var hits=new List<double[]>();
  foreach (int s in cl){ if (x<lbb[s][0]||x>lbb[s][1]||y<lbb[s][2]||y>lbb[s][3]) continue; double lo=double.MaxValue, hi=double.MaxValue;
    try{ lo=lat[s].asPolyface.GetMinZ(x,y,lat[s].BoundingBox); hi=lat[s].asPolyface.GetMaxZ(x,y,lat[s].BoundingBox);}catch{} if (lo>1e9||hi>1e9) continue; hits.Add(new double[]{Math.Min(lo,hi),Math.Max(lo,hi)}); }
  if (hits.Count==0) return double.NaN;
  double[] st=null; double bd=double.MaxValue;
  foreach (var h in hits){ double d=(z0>=h[0]-0.3&&z0<=h[1]+0.3)?0:Math.Min(Math.Abs(z0-h[0]),Math.Abs(z0-h[1])); if(d<bd){bd=d;st=h;} }
  if (bd>4) return double.NaN;
  double cur=st[0]; bool ch=true; while(ch){ ch=false; foreach (var h in hits) if (h[1]>=cur-0.3&&h[0]<cur-0.01){cur=h[0];ch=true;} }
  return cur; };
int fixedN=0;
for (int c=0;c<S.Count;c++) { var p=S[c]; if (p.Count<2) continue;
  double h=0,vv=0; for(int k=1;k<p.Count;k++){h+=Math.Sqrt((p[k][0]-p[k-1][0])*(p[k][0]-p[k-1][0])+(p[k][1]-p[k-1][1])*(p[k][1]-p[k-1][1]));vv+=Math.Abs(p[k][2]-p[k-1][2]);} if (vv>h) continue;
  for (int side=0;side<2;side++) {
    var E= side==0?p[0]:p[p.Count-1]; var A= side==0?p[1]:p[p.Count-2];
    double dx=A[0]-E[0], dy=A[1]-E[1]; double L=Math.Sqrt(dx*dx+dy*dy); if (L<0.4) continue; dx/=L; dy/=L;
    double z=double.NaN; double zref=A[2];
    foreach (double inset in new double[]{0.3,0.6,1.0}) { if (inset>L-0.1) break; double r=drop(E[0]+dx*inset,E[1]+dy*inset,zref); if(!double.IsNaN(r) && (double.IsNaN(z)||r<z)) z=r; }
    if (double.IsNaN(z)) continue;
    if (Math.Abs(z-E[2])>0.05) { E[2]=z; fixedN++; }
  }
}
foreach (var f in figs) App.ModelSpace.Entities.Remove(f);
Deswik.Graphics.Primaries.Layer lay=null; foreach (Deswik.Graphics.Primaries.Layer l in App.Layers) if (l.Name==OUTLAYER) lay=l;
foreach (var p in S) { var pl=new Deswik.Graphics.Figures.Polyline(); foreach (var q in p) pl.AddVertex(q[0],q[1],q[2]); pl.Layer=lay; App.ModelSpace.Entities.AddItem(pl,null,null,true,false); }
App.Redraw(true);
Out.AppendLine("Ends re-levelled: "+fixedN);
foreach (var p in S) if (p.Any(q=>Math.Abs(q[0]-8035)<0.5&&Math.Abs(q[1]-63078.9)<0.5)) Out.AppendLine("Sump: "+string.Join(" | ",p.Select(q=>string.Format("{0:0.0},{1:0.0},{2:0.00}",q[0],q[1],q[2]))));
foreach (var p in S) if (p.Any(q=>Math.Abs(q[0]-8065.8)<0.3&&Math.Abs(q[1]-63019.6)<0.3)) Out.AppendLine("ISP: "+string.Join(" | ",p.Skip(Math.Max(0,p.Count-4)).Select(q=>string.Format("{0:0.0},{1:0.0},{2:0.00}",q[0],q[1],q[2]))));
int steps=0, endSteps=0; var ex=new List<string>();
foreach (var p in S) { double h=0,vv=0; for(int k=1;k<p.Count;k++){h+=Math.Sqrt((p[k][0]-p[k-1][0])*(p[k][0]-p[k-1][0])+(p[k][1]-p[k-1][1])*(p[k][1]-p[k-1][1]));vv+=Math.Abs(p[k][2]-p[k-1][2]);} if (vv>h) continue;
  for(int k=1;k<p.Count;k++){ double hh=Math.Sqrt((p[k][0]-p[k-1][0])*(p[k][0]-p[k-1][0])+(p[k][1]-p[k-1][1])*(p[k][1]-p[k-1][1])); if (Math.Abs(p[k][2]-p[k-1][2])>Math.Max(1.0,0.3*hh)) { steps++; if (k==1||k==p.Count-1) endSteps++; if (ex.Count<6) ex.Add(string.Format("{0:0.0},{1:0.0}: {2:0.00} -> {3:0.00} over {4:0.0} m (vertex {5} of {6})",p[k][0],p[k][1],p[k-1][2],p[k][2],hh,k,p.Count)); } } }
Out.AppendLine("Lateral segments steeper than 30% and >1 m: "+steps+" (at an end: "+endSteps+")"); foreach (var s in ex) Out.AppendLine("  "+s);
return null;