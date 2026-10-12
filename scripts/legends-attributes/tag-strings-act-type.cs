// Tag strings with ACT_TYPE, create legend 'Floor Strings ACT_TYPE' with line weight 100, apply to DESIGN\FLOOR STRINGS
// Body of: object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out)
// Usings: 

string OUTLAYER="DESIGN\\FLOOR STRINGS", LEG="Floor Strings ACT_TYPE";
var sol=new List<Deswik.Graphics.Primaries.Figure>(); var sbb=new List<double[]>(); var sat=new List<string>(); var sv=new List<bool>(); var spid=new List<string>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) {
  if (f.Layer==null||f.Layer.Name!="SCHEDULER\\TASK SOLIDS"||f.asPolyface==null) continue;
  if (Convert.ToString(f.AttributeValueGet("STO_DEV",""))!="DEV") continue;
  double a=1e18,b=-1e18,c=1e18,d=-1e18,e=1e18,g=-1e18; foreach (Deswik.Graphics.Geometry.Point v in f.asPolyface.VertexList){a=Math.Min(a,v.x);b=Math.Max(b,v.x);c=Math.Min(c,v.y);d=Math.Max(d,v.y);e=Math.Min(e,v.z);g=Math.Max(g,v.z);}
  sol.Add(f); sbb.Add(new double[]{a,b,c,d,e,g}); sat.Add(Convert.ToString(f.AttributeValueGet("ACT_TYPE",""))); sv.Add(Convert.ToString(f.AttributeValueGet("LAT_VERT",""))=="VERT"); spid.Add(Convert.ToString(f.AttributeValueGet("DIS Parent ID","")));
}
double G=10; var grid=new Dictionary<string,List<int>>();
for (int i=0;i<sol.Count;i++) for (int gx=(int)Math.Floor(sbb[i][0]/G); gx<=(int)Math.Floor(sbb[i][1]/G); gx++) for (int gy=(int)Math.Floor(sbb[i][2]/G); gy<=(int)Math.Floor(sbb[i][3]/G); gy++) { string k=gx+","+gy; List<int> l; if(!grid.TryGetValue(k,out l)){l=new List<int>();grid[k]=l;} l.Add(i);} 
var strs=new List<Deswik.Graphics.Primaries.Figure>(); foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (f.Layer!=null&&f.Layer.Name==OUTLAYER&&f.asPolyline!=null) strs.Add(f);
var counts=new Dictionary<string,int>(); int unknown=0;
foreach (var f in strs) {
  bool isRaise=Convert.ToString(f.AttributeValueGet("LAT_VERT",""))=="VERT";
  var votes=new Dictionary<string,int>(); var pid=new Dictionary<string,int>();
  var p=new List<double[]>(); foreach (Deswik.Graphics.Geometry.Point q in f.asPolyline.VertexList) p.Add(new double[]{q.x,q.y,q.z});
  var samples=new List<double[]>(); for (int k=1;k<p.Count;k++) samples.Add(new double[]{(p[k][0]+p[k-1][0])/2,(p[k][1]+p[k-1][1])/2,(p[k][2]+p[k-1][2])/2});
  foreach (var s in samples) { List<int> l; if(!grid.TryGetValue(Math.Floor(s[0]/G)+","+Math.Floor(s[1]/G),out l)) continue;
    foreach (int i in l) { if (sv[i]!=isRaise) continue; var b=sbb[i]; if (s[0]<b[0]||s[0]>b[1]||s[1]<b[2]||s[1]>b[3]) continue; if (s[2]<b[4]-0.5||s[2]>b[5]+0.5) continue;
      if (!isRaise) { double z=double.MaxValue; try{ z=sol[i].asPolyface.GetMinZ(s[0],s[1],sol[i].BoundingBox);}catch{} if (z>1e9||Math.Abs(z-s[2])>1.5) continue; }
      int n; votes.TryGetValue(sat[i],out n); votes[sat[i]]=n+1; pid.TryGetValue(spid[i],out n); pid[spid[i]]=n+1; } }
  string at = votes.Count>0 ? votes.OrderByDescending(v=>v.Value).First().Key : "UNKNOWN";
  if (at=="UNKNOWN") unknown++;
  f.AttributeValueSet("ACT_TYPE",at); if (pid.Count>0) f.AttributeValueSet("DIS Parent ID",pid.OrderByDescending(v=>v.Value).First().Key);
  int c0; counts.TryGetValue(at,out c0); counts[at]=c0+1;
}
var col=new Dictionary<string,System.Drawing.Color>{
 {"DEC",System.Drawing.Color.FromArgb(0,112,255)}, {"ACC",System.Drawing.Color.FromArgb(0,200,255)}, {"MLA",System.Drawing.Color.FromArgb(0,180,0)},
 {"OD",System.Drawing.Color.FromArgb(255,140,0)}, {"DDD",System.Drawing.Color.FromArgb(160,80,255)}, {"RAD",System.Drawing.Color.FromArgb(220,0,0)},
 {"ISP",System.Drawing.Color.FromArgb(255,0,200)}, {"DSP",System.Drawing.Color.FromArgb(255,105,180)}, {"SMP",System.Drawing.Color.FromArgb(139,69,19)},
 {"EWD",System.Drawing.Color.FromArgb(255,230,0)}, {"SUB",System.Drawing.Color.FromArgb(0,128,128)}, {"PST",System.Drawing.Color.FromArgb(128,128,128)},
 {"EWR",System.Drawing.Color.FromArgb(173,255,47)}, {"RAR",System.Drawing.Color.FromArgb(255,99,71)}, {"RBH",System.Drawing.Color.FromArgb(148,0,211)},
 {"RHB",System.Drawing.Color.FromArgb(70,130,180)}, {"SRH",System.Drawing.Color.FromArgb(255,215,140)}, {"UNKNOWN",System.Drawing.Color.FromArgb(255,255,255)} };
var existing=App.Legends.Find(LEG); if (existing!=null) App.Legends.Remove(LEG);
var actLeg=App.Legends.Find("Act_Type");
var lg=App.Legends.Add(LEG, actLeg.Filters, actLeg.GlobalConstants);
var addM=lg.GetType().GetMethods().First(m=>m.Name=="Add"&&m.GetParameters().Length==1&&!m.GetParameters()[0].ParameterType.IsByRef);
var itemT=addM.GetParameters()[0].ParameterType;
var ctor=itemT.GetConstructors(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).First(c=>{ var ps=c.GetParameters(); return ps.Length==5 && ps[2].Name=="value"; });
foreach (var kv in counts.OrderBy(k=>k.Key=="UNKNOWN"?"zzz":k.Key)) {
  var c=col.ContainsKey(kv.Key)?col[kv.Key]:System.Drawing.Color.White;
  var it=ctor.Invoke(new object[]{kv.Key,"ACT_TYPE",kv.Key,c,(byte)255});
  var lwp=itemT.GetProperty("LineWeight"); if (lwp!=null&&lwp.CanWrite) lwp.SetValue(it,100,null);
  addM.Invoke(lg,new object[]{it}); }
try { lg.GetType().GetProperty("DefaultLineWeight").SetValue(lg,100,null); } catch {}
App.Legends.ApplyToLayer(OUTLAYER, LEG);
foreach (var f in strs){ string at=Convert.ToString(f.AttributeValueGet("ACT_TYPE","UNKNOWN")); f.PenColor = new Deswik.Graphics.Objects.Color(col.ContainsKey(at)?col[at]:System.Drawing.Color.White); f.LineWeight = Deswik.Graphics.Primaries.Figure.ConstLineWeight.LW_100; }
try { App.Legends.Save(); } catch (Exception ex) { Out.AppendLine("legend save note: "+ex.Message); }
App.Redraw(true);
Out.AppendLine("Strings tagged: "+strs.Count+" (unknown "+unknown+")");
foreach (var kv in counts.OrderBy(k=>k.Key)) { var c=col.ContainsKey(kv.Key)?col[kv.Key]:System.Drawing.Color.White; Out.AppendLine(string.Format("  {0}: {1} strings, RGB {2},{3},{4}",kv.Key,kv.Value,c.R,c.G,c.B)); }
Deswik.Graphics.Primaries.Layer lay=null; foreach (Deswik.Graphics.Primaries.Layer l in App.Layers) if (l.Name==OUTLAYER) lay=l;
Out.AppendLine("Layer legend now: "+lay.Legend+"; legend items: "+lg.GetType().GetProperty("Count").GetValue(lg,null));
return null;