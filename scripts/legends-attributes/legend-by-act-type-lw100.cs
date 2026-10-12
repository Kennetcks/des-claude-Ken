// Fill legend 'Floor Strings ACT_TYPE' (line weight 100), apply to DESIGN\FLOOR STRINGS and colour the strings
// Body of: object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out)
// Usings: 

string OUTLAYER="DESIGN\\FLOOR STRINGS", LEG="Floor Strings ACT_TYPE";
var strs=new List<Deswik.Graphics.Primaries.Figure>(); var counts=new Dictionary<string,int>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (f.Layer!=null&&f.Layer.Name==OUTLAYER&&f.asPolyline!=null) { strs.Add(f); string a=Convert.ToString(f.AttributeValueGet("ACT_TYPE","UNKNOWN")); int n; counts.TryGetValue(a,out n); counts[a]=n+1; }
var col=new Dictionary<string,System.Drawing.Color>{
 {"DEC",System.Drawing.Color.FromArgb(0,112,255)}, {"ACC",System.Drawing.Color.FromArgb(0,200,255)}, {"MLA",System.Drawing.Color.FromArgb(0,180,0)},
 {"OD",System.Drawing.Color.FromArgb(255,140,0)}, {"DDD",System.Drawing.Color.FromArgb(160,80,255)}, {"RAD",System.Drawing.Color.FromArgb(220,0,0)},
 {"ISP",System.Drawing.Color.FromArgb(255,0,200)}, {"DSP",System.Drawing.Color.FromArgb(255,105,180)}, {"SMP",System.Drawing.Color.FromArgb(139,69,19)},
 {"EWD",System.Drawing.Color.FromArgb(255,230,0)}, {"SUB",System.Drawing.Color.FromArgb(0,128,128)}, {"PST",System.Drawing.Color.FromArgb(128,128,128)},
 {"EWR",System.Drawing.Color.FromArgb(173,255,47)}, {"RAR",System.Drawing.Color.FromArgb(255,99,71)}, {"RBH",System.Drawing.Color.FromArgb(148,0,211)},
 {"RHB",System.Drawing.Color.FromArgb(70,130,180)}, {"SRH",System.Drawing.Color.FromArgb(255,215,140)}, {"UNKNOWN",System.Drawing.Color.FromArgb(255,255,255)} };
var lg=App.Legends.Find(LEG); if (lg==null) { var a0=App.Legends.Find("Act_Type"); lg=App.Legends.Add(LEG,a0.Filters,a0.GlobalConstants); }
lg.Clear();
Deswik.Common.Core.Legends.LegendItem tmpl=null; foreach (Deswik.Common.Core.Legends.LegendItem it in App.Legends.Find("Act_Type")) { tmpl=it; break; }
foreach (var kv in counts.OrderBy(k=>k.Key=="UNKNOWN"?"zzz":k.Key)) {
  var it=new Deswik.Common.Core.Legends.LegendItem(tmpl);
  it.Field="ACT_TYPE"; it.Value=kv.Key; it.Description=kv.Key; it.PenColor = col.ContainsKey(kv.Key)?col[kv.Key]:System.Drawing.Color.White; it.LineWeight=100; it.Transparency=255;
  lg.Add(it); }
lg.DefaultLineWeight=100;
App.Legends.ApplyToLayer(OUTLAYER, LEG);
foreach (var f in strs){ string at=Convert.ToString(f.AttributeValueGet("ACT_TYPE","UNKNOWN")); f.PenColor = new Deswik.Graphics.Objects.Color(col.ContainsKey(at)?col[at]:System.Drawing.Color.White); f.LineWeight = Deswik.Graphics.Primaries.Figure.ConstLineWeight.LW_100; }
try { App.Legends.Save(); } catch (Exception ex) { Out.AppendLine("legend save note: "+ex.Message); }
App.Redraw(true);
Deswik.Graphics.Primaries.Layer lay=null; foreach (Deswik.Graphics.Primaries.Layer l in App.Layers) if (l.Name==OUTLAYER) lay=l;
Out.AppendLine("Legend '"+LEG+"' items: "+lg.Count+"; layer legend: "+lay.Legend);
foreach (Deswik.Common.Core.Legends.LegendItem it in lg) Out.AppendLine(string.Format("  {0}: {1} strings, RGB {2},{3},{4}, LW {5}", it.Value, counts[it.Value], it.PenColor.R, it.PenColor.G, it.PenColor.B, it.LineWeight));
return null;