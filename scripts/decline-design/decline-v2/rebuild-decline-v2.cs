// Rebuild DESIGN\DECLINE V2 from a fresh copy of DESIGN\DECLINE (his layer is never edited), apply v2b_attrs.csv / v2b_geom.csv
// (action attrs|replace|new|delete), colour by ACT_TYPE (legend "Decline V2 ACT_TYPE", LW 100) and build/refresh the
// tunnels-by-rule solids on DESIGN\DECLINE V2\SOLIDS (FS sections from scripts/tunnels-by-rule). Never saves the drawing.
// Body of: object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out)
var inv=System.Globalization.CultureInfo.InvariantCulture;
string dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"Kennet.DeswikClaude","work","stopes_mine");
string SRC="DESIGN\\DECLINE", LN="DESIGN\\DECLINE V2", OL="DESIGN\\DECLINE V2\\SOLIDS", LEG="Decline V2 ACT_TYPE";
Func<string,List<string>> split=(line)=>{ var r=new List<string>(); var sb=new StringBuilder(); bool q=false; foreach(char ch in line){ if(ch=='"'){q=!q;continue;} if(ch==','&&!q){r.Add(sb.ToString());sb.Clear();} else sb.Append(ch);} r.Add(sb.ToString()); return r; };
var al=File.ReadAllLines(Path.Combine(dir,"v2b_attrs.csv")); var hdr=split(al[0]);
var rows=al.Skip(1).Where(x=>x.Trim()!="").Select(x=>{ var c=split(x); var d=new Dictionary<string,string>(); for(int i=0;i<hdr.Count;i++) d[hdr[i]]=i<c.Count?c[i]:""; return d; }).ToList();
var geo=new Dictionary<string,List<double[]>>();
foreach (var x in File.ReadAllLines(Path.Combine(dir,"v2b_geom.csv")).Skip(1)){ if(x.Trim()=="") continue; var c=x.Split(','); List<double[]> l; if(!geo.TryGetValue(c[0],out l)){l=new List<double[]>();geo[c[0]]=l;} l.Add(new[]{double.Parse(c[2],inv),double.Parse(c[3],inv),double.Parse(c[4],inv)}); }
Deswik.Graphics.Primaries.Layer src=null, ly=null; foreach (Deswik.Graphics.Primaries.Layer l in App.Layers){ if(l.Deleted) continue; if(l.Name==SRC) src=l; if(l.Name==LN) ly=l; }
if (ly==null){ App.Layers.Add(LN,false,1); foreach (Deswik.Graphics.Primaries.Layer l in App.Layers) if(l.Name==LN&&!l.Deleted) ly=l; }
// same attribute fields as his layer
var at=ly.Attributes; var have=new HashSet<string>(); foreach (var a in (System.Collections.IEnumerable)at) have.Add(Convert.ToString(a.GetType().GetProperty("Name").GetValue(a,null)));
var addM=at.GetType().GetMethods().First(m=>m.Name=="Add"&&m.GetParameters().Length==3&&m.GetParameters()[0].ParameterType==typeof(string));
foreach (var a in (System.Collections.IEnumerable)src.Attributes){ string n=Convert.ToString(a.GetType().GetProperty("Name").GetValue(a,null)); if(!have.Contains(n)) addM.Invoke(at,new object[]{n,Enum.Parse(addM.GetParameters()[1].ParameterType,"String"),""}); }
// 1. clear V2 strings and recopy his
var old=new List<Deswik.Graphics.Primaries.Figure>(); foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if(!f.Deleted&&f.Layer==ly) old.Add(f);
foreach (var f in old) App.ModelSpace.Entities.Remove(f);
var byOrig=new Dictionary<string,Deswik.Graphics.Primaries.Figure>();
var srcF=new List<Deswik.Graphics.Primaries.Figure>(); foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if(!f.Deleted&&f.Layer==src) srcF.Add(f);
foreach (var f in srcF){ var c=(Deswik.Graphics.Primaries.Figure)f.Clone(); c.Layer=ly; App.ModelSpace.Entities.AddItem(c,null,null,true,false); byOrig[f.Handle]=c; }
var fields=new List<string>(); foreach (var a in (System.Collections.IEnumerable)ly.Attributes) fields.Add(Convert.ToString(a.GetType().GetProperty("Name").GetValue(a,null)));
Deswik.Graphics.Primaries.Figure tmplOD=null, tmplACC=null;
foreach (var f in byOrig.Values){ var t=Convert.ToString(f.AttributeValueGet("ACT_TYPE","")); if(t=="OD"&&tmplOD==null) tmplOD=f; if(t=="ACC"&&tmplACC==null) tmplACC=f; }
Action<Deswik.Graphics.Primaries.Figure,Dictionary<string,string>> setA=(f,r)=>{
  f.AttributeValueSet("ACT_TYPE",r["ACT_TYPE"]); f.AttributeValueSet("LEVEL",r["LEVEL"]); f.AttributeValueSet("LODE",r["LODE"]); f.AttributeValueSet("COST_CENTRE",r["COST_CENTRE"]);
  f.AttributeValueSet("LAT_VERT",r["LAT_VERT"]); f.AttributeValueSet("STO_DEV",r["STO_DEV"]); f.AttributeValueSet("DESIGN",r["DESIGN"]); f.AttributeValueSet("Transformation Details",r["TD"]); };
var cnt=new Dictionary<string,int>();
foreach (var r in rows){
  string act=r["action"]; int c0; cnt.TryGetValue(act,out c0); cnt[act]=c0+1;
  Deswik.Graphics.Primaries.Figure o=null; byOrig.TryGetValue(r["id"],out o);
  if(act=="delete"){ App.ModelSpace.Entities.Remove(o); continue; }
  if(act=="attrs"){ setA(o,r); continue; }
  var s= act=="replace"?o:(r["ACT_TYPE"]=="OD"?tmplOD:tmplACC);
  var pl=new Deswik.Graphics.Figures.Polyline(); foreach (var p in geo[r["id"]]) pl.AddVertex(p[0],p[1],p[2]); pl.Layer=ly; App.ModelSpace.Entities.AddItem(pl,null,null,true,false);
  foreach (var fn in fields){ var v=s.AttributeValueGet(fn,null); if(v!=null) pl.AttributeValueSet(fn,v); }
  setA(pl,r); if(act=="replace") App.ModelSpace.Entities.Remove(o); }
Out.AppendLine(string.Join(", ",cnt.Select(k=>k.Key+"="+k.Value)));
// 2. colours by ACT_TYPE (own legend; Kennet's legends are never rebuilt)
var col=new Dictionary<string,System.Drawing.Color>{ {"DEC",System.Drawing.Color.FromArgb(0,112,255)}, {"ACC",System.Drawing.Color.FromArgb(0,200,255)}, {"OD",System.Drawing.Color.FromArgb(255,140,0)}, {"RAD",System.Drawing.Color.FromArgb(220,0,0)}, {"ISP",System.Drawing.Color.FromArgb(255,0,200)}, {"RAR",System.Drawing.Color.FromArgb(255,99,71)} };
var lg=App.Legends.Find(LEG);
if (lg==null){ var a0=App.Legends.Find("Act_Type"); lg=App.Legends.Add(LEG,a0.Filters,a0.GlobalConstants);
  Deswik.Common.Core.Legends.LegendItem tmpl=null; foreach (Deswik.Common.Core.Legends.LegendItem it in a0) { tmpl=it; break; }
  foreach (var kv in col){ var it=new Deswik.Common.Core.Legends.LegendItem(tmpl); it.Field="ACT_TYPE"; it.Value=kv.Key; it.Description=kv.Key; it.PenColor=kv.Value; it.LineWeight=100; it.Transparency=255; lg.Add(it); }
  lg.DefaultLineWeight=100; }
App.Legends.ApplyToLayer(LN, LEG);
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities){ if(f.Deleted||f.Layer!=ly) continue; string t=Convert.ToString(f.AttributeValueGet("ACT_TYPE","")); if(col.ContainsKey(t)) f.PenColor=new Deswik.Graphics.Objects.Color(col[t]); f.LineWeight=Deswik.Graphics.Primaries.Figure.ConstLineWeight.LW_100; }
// 3. solids by rule: first run creates the rules, later runs refresh them
bool exists=false; foreach (Deswik.Graphics.Primaries.Layer l in App.Layers) if(l.Name==OL&&!l.Deleted) exists=true;
var rr=new Deswik.Graphics.Tunnels.SectionAlongPolylineRules(App); bool ok;
if (exists) ok=rr.Refresh(App, OL);
else {
  var secs=new Dictionary<string,string>{{"DEC","FS 5.5W X 5.5H ARCH"},{"ISP","FS 5.5W X 5.5H ARCH"},{"ACC","FS 5.0W X 5.0H ARCH"},{"RAD","FS 5.0W X 5.0H ARCH"},{"OD","FS 4.5W X 4.5H ARCH"},{"RAR","FS 5.2M DIA"}};
  rr.InputLayers.Clear(); rr.InputLayers.Add(LN); rr.Filter="<No Filtering>"; rr.CreateSolids=true; rr.OutputSolidsLayer=OL; rr.CreateWalls=false; rr.OutputWallsLayer="";
  rr.AttributeName="ACT_TYPE"; rr.UseFixedAzimuth=false; rr.Clear();
  foreach (var kv in secs) rr.Add(new Deswik.Graphics.Tunnels.SectionAlongPolylineRule(kv.Key, kv.Value, -1));
  ok=rr.Execute(App); }
Out.AppendLine("solids ok="+ok+" errors="+(rr.Errors==null?0:rr.Errors.Count)); if(rr.Errors!=null) foreach (var e in rr.Errors.Take(10)) Out.AppendLine("  err: "+e);
App.Legends.ApplyToLayer(OL, LEG);
App.Redraw(true); return null;
