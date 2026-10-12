// Build ACT_TYPE tunnel sections and a rule-based tunnel solids layer from the floor strings
// Body of: object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out)
// Usings: 

string SLAYER="DESIGN\\FLOOR STRINGS", OLAYER="DESIGN\\FLOOR STRINGS\\SOLIDS";
double ARCH=0.93;
// ACT_TYPE -> section name, and section definitions
var lat = new Dictionary<string,double>{{"DEC",5.5},{"MLA",5.5},{"ISP",5.5},{"DSP",5.5},{"ACC",5.0},{"RAD",5.0},{"DDD",5.0},{"EWD",5.0},{"SUB",5.0},{"PST",5.0},{"OD",4.5},{"SMP",4.5}};
var ver = new Dictionary<string,double>{{"RAR",5.2},{"RBH",4.5},{"RHB",4.5},{"EWR",1.1},{"SRH",0.5}};
var map = new Dictionary<string,string>();
var made = new List<string>();
Func<string,bool> addOrReplace = null;
foreach (var kv in lat) {
  double W=kv.Value,H=kv.Value; string nm="FS "+W.ToString("0.0")+"W X "+H.ToString("0.0")+"H ARCH";
  map[kv.Key]=nm; if (made.Contains(nm)) continue; made.Add(nm);
  if (App.Sections.Exists(nm)) App.Sections.Remove(nm);
  var s = new Deswik.Graphics.Tunnels.Section(nm);
  double rad=(W*W/4+ARCH*ARCH)/(2*ARCH), cz=H-rad, hw=H-ARCH, a0=Math.Asin((W/2)/rad);
  s.Add(-W/2,0); s.Add(W/2,0); s.Add(W/2,hw);
  for (int i=1;i<10;i++){ double a=a0-2*a0*i/10.0; s.Add(rad*Math.Sin(a), cz+rad*Math.Cos(a)); }
  s.Add(-W/2,hw);
  App.Sections.Add(s);
}
foreach (var kv in ver) {
  double D=kv.Value; string nm="FS "+D.ToString("0.0")+"M DIA";
  map[kv.Key]=nm; if (made.Contains(nm)) continue; made.Add(nm);
  if (App.Sections.Exists(nm)) App.Sections.Remove(nm);
  var c = new Deswik.Graphics.Tunnels.CircleTunnelInformation(); c.Radius=D/2; c.CircleVerticesCount=24;
  var s = new Deswik.Graphics.Tunnels.Section(c.GetMyType(), c); s.Name=nm;
  App.Sections.Add(s);
}
try { App.Sections.ValuesSaveToDocumentTempLayer(App); Out.AppendLine("sections saved"); } catch (Exception e) { Out.AppendLine("section save: "+e.Message); }
foreach (var nm in made) { var s=App.Sections.FindName(nm); Out.AppendLine(nm+" n="+s.Count+" valid="+s.Valid()); }

// clear the old script-built solids
var old=new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (f.Layer!=null && f.Layer.Name==OLAYER) old.Add(f);
foreach (var f in old) App.ModelSpace.Entities.Remove(f);
Out.AppendLine("removed old solids: "+old.Count);

// rules
var r = new Deswik.Graphics.Tunnels.SectionAlongPolylineRules(App);
r.InputLayers.Clear(); r.InputLayers.Add(SLAYER);
r.Filter="<No Filtering>"; r.CreateSolids=true; r.OutputSolidsLayer=OLAYER; r.CreateWalls=false; r.OutputWallsLayer="";
r.AttributeName="ACT_TYPE"; r.UseFixedAzimuth=false;
r.Clear();
foreach (var kv in map) r.Add(new Deswik.Graphics.Tunnels.SectionAlongPolylineRule(kv.Key, kv.Value, -1));
bool ok = r.Execute(App);
Out.AppendLine("execute ok="+ok+" errors="+(r.Errors==null?0:r.Errors.Count));
if (r.Errors!=null) foreach (var e in r.Errors.Take(15)) Out.AppendLine("  err: "+e);

Deswik.Graphics.Primaries.Layer ol=null; foreach (Deswik.Graphics.Primaries.Layer l in App.Layers) if (l.Name==OLAYER) ol=l;
Out.AppendLine("layer type="+ol.Type);
foreach (var o in (System.Collections.IEnumerable)ol.XProperties) { var n=Convert.ToString(o.GetType().GetProperty("Name").GetValue(o,null)); if (n.Contains("SectionAlong")||n.Contains("LayerType")) Out.AppendLine("  "+n+" = "+Convert.ToString(o.GetType().GetProperty("Value").GetValue(o,null))); }
int cnt=0; double tv=0; var by=new Dictionary<string,double>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) { if (f.Layer!=ol||f.asPolyface==null) continue; cnt++; double v=Math.Abs(f.asPolyface.Volume); tv+=v; var at=Convert.ToString(f.AttributeValueGet("ACT_TYPE","?")); double x; by.TryGetValue(at,out x); by[at]=x+v; }
var ts=new Dictionary<string,double>(); double tsv=0;
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) { if (f.Layer==null||f.Layer.Name!="SCHEDULER\\TASK SOLIDS"||f.asPolyface==null) continue; if (Convert.ToString(f.AttributeValueGet("STO_DEV",""))!="DEV") continue; var at=Convert.ToString(f.AttributeValueGet("ACT_TYPE","?")); double v=Math.Abs(f.asPolyface.Volume); double x; ts.TryGetValue(at,out x); ts[at]=x+v; tsv+=v; }
Out.AppendLine("solids="+cnt+" vol="+tv.ToString("0")+" task="+tsv.ToString("0")+" ("+(tv/tsv*100).ToString("0.0")+"%)");
foreach (var k in by.Keys.OrderBy(k=>k)) { double x; ts.TryGetValue(k,out x); Out.AppendLine("  "+k+" "+by[k].ToString("0")+" / "+x.ToString("0")+" = "+(x>0?(by[k]/x*100).ToString("0"):"-")+"%"); }
App.Redraw(true);
return null;