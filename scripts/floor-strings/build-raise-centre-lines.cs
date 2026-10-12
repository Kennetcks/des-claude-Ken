// Redraw raise strings as centre lines through each raise's solids; tag all strings with LAT_VERT
// Body of: object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out)
// Usings: 

string OUTLAYER="DESIGN\\FLOOR STRINGS";
var vs=new Dictionary<string,List<Deswik.Graphics.Primaries.Figure>>(); var vbb=new List<double[]>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) {
  if (f.Layer==null||f.Layer.Name!="SCHEDULER\\TASK SOLIDS"||f.asPolyface==null) continue;
  if (Convert.ToString(f.AttributeValueGet("STO_DEV",""))!="DEV"||Convert.ToString(f.AttributeValueGet("LAT_VERT",""))!="VERT") continue;
  string k=Convert.ToString(f.AttributeValueGet("DIS Parent GUID","")); List<Deswik.Graphics.Primaries.Figure> l; if(!vs.TryGetValue(k,out l)){l=new List<Deswik.Graphics.Primaries.Figure>();vs[k]=l;} l.Add(f);
  double a=1e18,b=-1e18,c=1e18,d=-1e18,e=1e18,g=-1e18; foreach (Deswik.Graphics.Geometry.Point v in f.asPolyface.VertexList){a=Math.Min(a,v.x);b=Math.Max(b,v.x);c=Math.Min(c,v.y);d=Math.Max(d,v.y);e=Math.Min(e,v.z);g=Math.Max(g,v.z);} vbb.Add(new double[]{a-0.3,b+0.3,c-0.3,d+0.3,e-0.3,g+0.3});
}
Func<double[],bool> inV = (q)=>{ foreach (var b in vbb) if (q[0]>=b[0]&&q[0]<=b[1]&&q[1]>=b[2]&&q[1]<=b[3]&&q[2]>=b[4]&&q[2]<=b[5]) return true; return false; };
// identify existing raise strings: most of their points inside raise solids
var oldR=new List<Deswik.Graphics.Primaries.Figure>(); var lats=new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) {
  if (f.Layer==null||f.Layer.Name!=OUTLAYER||f.asPolyline==null) continue;
  int n=0,inn=0; foreach (Deswik.Graphics.Geometry.Point q in f.asPolyline.VertexList){ n++; if (inV(new double[]{q.x,q.y,q.z})) inn++; }
  if (n>0 && inn*2>n) oldR.Add(f); else lats.Add(f);
}
// new raise strings
var newR=new List<List<double[]>>(); var newPid=new List<string>();
foreach (var kv in vs) {
  var sols=kv.Value; var all=new List<double[]>(); var cg=new List<double[]>();
  foreach (var f in sols){ var pts=new List<double[]>(); foreach (Deswik.Graphics.Geometry.Point v in f.asPolyface.VertexList) pts.Add(new double[]{v.x,v.y,v.z}); all.AddRange(pts); var c=f.asPolyface.CenterOfGravity; cg.Add(new double[]{c.x,c.y,c.z}); }
  double mx=all.Average(p=>p[0]), my=all.Average(p=>p[1]), mz=all.Average(p=>p[2]);
  double[,] C=new double[3,3]; foreach (var p in all){ double[] d={p[0]-mx,p[1]-my,p[2]-mz}; for(int i=0;i<3;i++) for(int j=0;j<3;j++) C[i,j]+=d[i]*d[j]; }
  double[] ax={0,0,1}; for (int it=0;it<50;it++){ double[] nv=new double[3]; for(int i=0;i<3;i++) for(int j=0;j<3;j++) nv[i]+=C[i,j]*ax[j]; double L=Math.Sqrt(nv[0]*nv[0]+nv[1]*nv[1]+nv[2]*nv[2]); if(L<1e-12) break; ax=new double[]{nv[0]/L,nv[1]/L,nv[2]/L}; }
  if (ax[2]<0) ax=new double[]{-ax[0],-ax[1],-ax[2]};
  Func<double[],double> pr = (p)=>(p[0]-mx)*ax[0]+(p[1]-my)*ax[1]+(p[2]-mz)*ax[2];
  double t0=all.Min(p=>pr(p)), t1=all.Max(p=>pr(p));
  var b0=all.Where(p=>pr(p)<=t0+0.3).ToList(); var b1=all.Where(p=>pr(p)>=t1-0.3).ToList();
  var s=new double[]{b0.Average(p=>p[0]),b0.Average(p=>p[1]),b0.Average(p=>p[2])}; var e=new double[]{b1.Average(p=>p[0]),b1.Average(p=>p[1]),b1.Average(p=>p[2])};
  var line=new List<double[]>{s}; foreach (var c in cg.OrderBy(c=>pr(c))) line.Add(c); line.Add(e);
  // drop centre points that sit on the straight line anyway (keeps it clean)
  var clean=new List<double[]>{line[0]};
  for (int k=1;k+1<line.Count;k++){ var a=clean[clean.Count-1]; var b=line[k+1]; var m=line[k];
    double ex=b[0]-a[0],ey=b[1]-a[1],ez=b[2]-a[2]; double LL=Math.Sqrt(ex*ex+ey*ey+ez*ez); if (LL<1e-6) continue;
    double t=((m[0]-a[0])*ex+(m[1]-a[1])*ey+(m[2]-a[2])*ez)/(LL*LL); double fx=a[0]+ex*t-m[0], fy=a[1]+ey*t-m[1], fz=a[2]+ez*t-m[2];
    if (Math.Sqrt(fx*fx+fy*fy+fz*fz)>0.3) clean.Add(m); }
  clean.Add(line[line.Count-1]);
  newR.Add(clean); newPid.Add(Convert.ToString(sols[0].AttributeValueGet("DIS Parent ID","")));
}
foreach (var f in oldR) App.ModelSpace.Entities.Remove(f);
Deswik.Graphics.Primaries.Layer lay=null; foreach (Deswik.Graphics.Primaries.Layer l in App.Layers) if (l.Name==OUTLAYER) lay=l;
for (int i=0;i<newR.Count;i++){ var pl=new Deswik.Graphics.Figures.Polyline(); foreach (var q in newR[i]) pl.AddVertex(q[0],q[1],q[2]); pl.Layer=lay; App.ModelSpace.Entities.AddItem(pl,null,null,true,false); try{ pl.AttributeValueSet("LAT_VERT","VERT"); pl.AttributeValueSet("DIS Parent ID",newPid[i]); }catch{} }
int tagged=0; foreach (var f in lats){ try{ f.AttributeValueSet("LAT_VERT","LAT"); tagged++; }catch{} }
App.Redraw(true);
Out.AppendLine(string.Format("Raise drives {0}; old raise strings removed {1}; new raise strings {2}; lateral strings kept {3} (tagged {4})", vs.Count, oldR.Count, newR.Count, lats.Count, tagged));
foreach (var r in newR.Where(r=>r.Any(q=>Math.Abs(q[0]-8050)<6&&Math.Abs(q[1]-63172)<6))) Out.AppendLine("  e.g. "+string.Join(" | ",r.Select(q=>string.Format("{0:0.0},{1:0.0},{2:0.00}",q[0],q[1],q[2]))));
// flatness check on raises
int flat=0; foreach (var r in newR){ var a=r[0]; var b=r[r.Count-1]; double h=Math.Sqrt((a[0]-b[0])*(a[0]-b[0])+(a[1]-b[1])*(a[1]-b[1])); if (Math.Abs(a[2]-b[2])<h) flat++; }
Out.AppendLine("Raise strings flatter than 45 deg: "+flat);
return null;