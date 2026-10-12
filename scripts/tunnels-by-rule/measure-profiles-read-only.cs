// Measure profile width/height per ACT_TYPE from existing solids, and check Polyface building API (read only)
// Body of: object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out)
// Usings: 

var sol=new List<Deswik.Graphics.Primaries.Figure>(); var par=new List<string>(); var act=new List<string>(); var isV=new List<bool>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) { if (f.Layer==null||f.Layer.Name!="SCHEDULER\\TASK SOLIDS"||f.asPolyface==null) continue; if (Convert.ToString(f.AttributeValueGet("STO_DEV",""))!="DEV") continue;
  sol.Add(f); par.Add(Convert.ToString(f.AttributeValueGet("DIS Parent GUID",""))); act.Add(Convert.ToString(f.AttributeValueGet("ACT_TYPE",""))); isV.Add(Convert.ToString(f.AttributeValueGet("LAT_VERT",""))=="VERT"); }
var map=new Dictionary<string,List<int>>(); var kp=new Dictionary<string,double[]>();
for (int i=0;i<sol.Count;i++){ var seen=new HashSet<string>(); foreach (Deswik.Graphics.Geometry.Point v in sol[i].asPolyface.VertexList){ string k=Math.Round(v.x*20)+","+Math.Round(v.y*20)+","+Math.Round(v.z*20); if(!seen.Add(k)) continue; kp[k]=new double[]{v.x,v.y,v.z}; List<int> l; if(!map.TryGetValue(k,out l)){l=new List<int>();map[k]=l;} l.Add(i);} }
var faces=new Dictionary<long,List<double[]>>();
foreach (var kv in map){ var l=kv.Value; for(int a=0;a<l.Count;a++) for(int b=a+1;b<l.Count;b++){ if(par[l[a]]!=par[l[b]]) continue; long key=(long)Math.Min(l[a],l[b])*100000+Math.Max(l[a],l[b]); List<double[]> s; if(!faces.TryGetValue(key,out s)){s=new List<double[]>();faces[key]=s;} s.Add(kp[kv.Key]); } }
var W=new Dictionary<string,List<double[]>>(); var shape=new Dictionary<string,List<double[]>>();
foreach (var kv in faces){ var pts=kv.Value; if (pts.Count<6) continue; int a=(int)(kv.Key/100000); string t=act[a];
  if (isV[a]) { double cx=pts.Average(p=>p[0]), cy=pts.Average(p=>p[1]); double r=pts.Max(p=>Math.Sqrt((p[0]-cx)*(p[0]-cx)+(p[1]-cy)*(p[1]-cy))); double xr=pts.Max(p=>p[0])-pts.Min(p=>p[0]), yr=pts.Max(p=>p[1])-pts.Min(p=>p[1]); List<double[]> L; if(!W.TryGetValue(t,out L)){L=new List<double[]>();W[t]=L;} L.Add(new double[]{Math.Max(xr,yr),Math.Min(xr,yr),r,pts.Count}); continue; }
  double best=-1; double[] e1=null,e2=null; foreach(var p in pts) foreach(var q in pts){ double d=(p[0]-q[0])*(p[0]-q[0])+(p[1]-q[1])*(p[1]-q[1]); if(d>best){best=d;e1=p;e2=q;} }
  double w=Math.Sqrt(best); double z0=pts.Min(p=>p[2]), z1=pts.Max(p=>p[2]);
  double ux=(e2[0]-e1[0])/w, uy=(e2[1]-e1[1])/w; double mx=(e1[0]+e2[0])/2, my=(e1[1]+e2[1])/2;
  var loc=pts.Select(p=>new double[]{(p[0]-mx)*ux+(p[1]-my)*uy, p[2]-z0}).ToList();
  double wallH=loc.Where(p=>Math.Abs(p[0])>=w/2-0.15).Select(p=>p[1]).DefaultIfEmpty(0).Max();
  List<double[]> L2; if(!W.TryGetValue(t,out L2)){L2=new List<double[]>();W[t]=L2;} L2.Add(new double[]{w,z1-z0,wallH,pts.Count});
}
Func<List<double>,double> med=(l)=>{ var s=l.OrderBy(x=>x).ToList(); return s.Count==0?double.NaN:s[s.Count/2]; };
foreach (var kv in W.OrderBy(k=>k.Key)) { var L=kv.Value; Out.AppendLine(string.Format("{0}: n={1} width {2:0.00} height {3:0.00} wall/rad {4:0.00} (verts/face {5:0})", kv.Key, L.Count, med(L.Select(x=>x[0]).ToList()), med(L.Select(x=>x[1]).ToList()), med(L.Select(x=>x[2]).ToList()), med(L.Select(x=>x[3]).ToList()))); }
var pt=typeof(Deswik.Graphics.Figures.Polyface);
Out.AppendLine("Polyface ctors: "+string.Join(" ; ",pt.GetConstructors().Select(c=>"("+string.Join(",",c.GetParameters().Select(q=>q.ParameterType.Name+" "+q.Name))+")")));
Out.AppendLine("Polyface build methods: "+string.Join(", ",pt.GetMethods().Where(m=>m.Name.Contains("Add")||m.Name.Contains("Create")||m.Name.Contains("Sweep")||m.Name.Contains("Loft")).Select(m=>m.Name+"("+string.Join(",",m.GetParameters().Select(q=>q.ParameterType.Name))+")").Distinct()));
var ia=typeof(Deswik.Graphics.Geometry.Int32Array); Out.AppendLine("Int32Array: "+string.Join(", ",ia.GetMethods().Where(m=>m.Name.StartsWith("Add")).Select(m=>m.Name+"("+string.Join(",",m.GetParameters().Select(q=>q.ParameterType.Name))+")").Distinct())+" ctors "+string.Join(";",ia.GetConstructors().Select(c=>c.GetParameters().Length.ToString())));
var ps=typeof(Deswik.Graphics.Geometry.Points); Out.AppendLine("Points: "+string.Join(", ",ps.GetMethods().Where(m=>m.Name.StartsWith("Add")).Select(m=>m.Name+"("+string.Join(",",m.GetParameters().Select(q=>q.ParameterType.Name))+")").Distinct()));
Out.AppendLine("FaceList writable: "+pt.GetProperty("FaceList").CanWrite+", VertexList writable: "+pt.GetProperty("VertexList").CanWrite);
return null;