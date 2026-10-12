// After batch-interrogate-solids-to-csv.cs (LAY = the blocks layer): write INT_ fields onto each block (matched by BLOCK),
// create a range legend on INT_AU from the drawing's "AU" legend items, apply it to the layer and show only that layer
// Body of: object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out)
// Usings: 

string LAY = "0\\06 SITE\\03 ENGINEERING\\DESIGN MASTER\\STOPES\\STOPE DESIGN\\5M BLOCKS", LEG="5m blocks AU";
string csv = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Kennet.DeswikClaude", "work", "stopes298", "interrogation_5m_blocks.csv");
var lines = File.ReadAllLines(csv);
// quoted CSV (the DESIGN text has commas)
Func<string,List<string>> split = s => { var r=new List<string>(); var sb=new StringBuilder(); bool q=false; foreach (char ch in s){ if(ch=='"'){q=!q;continue;} if(ch==','&&!q){r.Add(sb.ToString());sb.Clear();continue;} sb.Append(ch);} r.Add(sb.ToString()); return r; };
var hdr = split(lines[0].TrimStart('\uFEFF'));
int iB=hdr.IndexOf("BLOCK"), iName=hdr.IndexOf("Name");
var map = new Dictionary<string,List<string>>();
for (int i=1;i<lines.Length;i++){ var c=split(lines[i]); if (c.Count>=hdr.Count) map[c[iB]]=c; }
var cols = new string[][]{ new[]{"Volume","INT_VOLUME"}, new[]{"Void Volume","INT_VOID_VOLUME"}, new[]{"Model Tons","INT_TONNES"}, new[]{"Density","INT_DENSITY"}, new[]{"AU","INT_AU"}, new[]{"INT_ID","INT_ID"} };
Deswik.Graphics.Primaries.Layer ly=null; foreach (Deswik.Graphics.Primaries.Layer l in App.Layers) if (l.Name==LAY) ly=l;
var inv=System.Globalization.CultureInfo.InvariantCulture;
string model = "BLOCK_MODEL"; string date = DateTime.Now.ToString("yyyy-MM-dd");
var figs=new List<Deswik.Graphics.Primaries.Figure>(); int miss=0;
var res=new List<string>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) {
  if (f.Deleted||f.Layer!=ly) continue;
  string bk=Convert.ToString(f.AttributeValueGet("BLOCK",""));
  List<string> row; if (!map.TryGetValue(bk,out row)) { miss++; continue; }
  foreach (var c in cols) { double v; double.TryParse(row[hdr.IndexOf(c[0])], System.Globalization.NumberStyles.Float, inv, out v); f.AttributeValueSet(c[1], c[1]=="INT_ID"?(object)(int)v:(object)Math.Round(v, c[1]=="INT_AU"?3:2)); }
  f.AttributeValueSet("INT_MODEL", model); f.AttributeValueSet("INT_DATE", date);
  figs.Add(f);
  res.Add(string.Format(inv,"{0},{1},{2},{3:0},{4:0.00}", row[iName], row[hdr.IndexOf("CH_FROM")], row[hdr.IndexOf("CH_TO")], double.Parse(row[hdr.IndexOf("Model Tons")],inv), double.Parse(row[hdr.IndexOf("AU")],inv)));
}
// bands Kennet asked for: light blue 2-2.6, blue 2.6-3, then 3-5, 5-12 strong pink, 12+;
// below 2 and the high-grade colours come from the drawing's AU / Grade Bins legends
var bands = new object[][]{
  new object[]{0.0, 1.0, "0 to 1", System.Drawing.Color.FromArgb(128,128,128)},
  new object[]{1.0, 2.0, "1 to 2", System.Drawing.Color.FromArgb(0,255,0)},
  new object[]{2.0, 2.6, "2 to 2.6", System.Drawing.Color.FromArgb(0,176,255)},
  new object[]{2.6, 3.0, "2.6 to 3", System.Drawing.Color.FromArgb(0,0,255)},
  new object[]{3.0, 5.0, "3 to 5", System.Drawing.Color.FromArgb(255,0,0)},
  new object[]{5.0, 12.0, "5 to 12", System.Drawing.Color.FromArgb(255,0,255)},
  new object[]{12.0, 1000.0, "12 to", System.Drawing.Color.FromArgb(128,0,128)} };
var au=App.Legends.Find("AU");
Deswik.Common.Core.Legends.LegendItem tmpl=null, last=null; foreach (Deswik.Common.Core.Legends.LegendItem it in au) { if (tmpl==null) tmpl=it; last=it; }
var lg=App.Legends.Find(LEG); if (lg==null) lg=App.Legends.Add(LEG,au.Filters,au.GlobalConstants);
lg.Clear();
for (int k=0;k<bands.Length;k++) { var b=bands[k];
  // From/To are strings; clone the AU legend's open-ended last item for the top band (its To is "99")
  var it=new Deswik.Common.Core.Legends.LegendItem(k==bands.Length-1?last:tmpl);
  it.Field="INT_AU"; it.From=Convert.ToDouble(b[0]).ToString(inv); if (k<bands.Length-1) it.To=Convert.ToDouble(b[1]).ToString(inv); it.Description=(string)b[2];
  it.PenColor=(System.Drawing.Color)b[3]; it.FillColor=(System.Drawing.Color)b[3];
  lg.Add(it);
}
App.Legends.ApplyToLayer(LAY, LEG);
foreach (var f in figs) { double a=Convert.ToDouble(f.AttributeValueGet("INT_AU",0.0)); System.Drawing.Color c=(System.Drawing.Color)bands[0][3]; foreach (var b in bands) if (a>=Convert.ToDouble(b[0])) c=(System.Drawing.Color)b[3]; f.PenColor=new Deswik.Graphics.Objects.Color(c); }
try { App.Legends.Save(); } catch (Exception ex) { Out.AppendLine("legend save note: "+ex.Message); }
// show this one layer only; never change visibility in bulk
foreach (var pn in new[]{"Visible","Hidden"}) { var pi=ly.GetType().GetProperty(pn); if (pi!=null&&pi.CanWrite&&pi.PropertyType==typeof(bool)) { pi.SetValue(ly, pn!="Hidden", null); break; } }
App.Redraw(true);
Out.AppendLine("updated="+figs.Count+" missing="+miss+" layer legend="+ly.Legend);
res.Sort(); foreach (var r in res) Out.AppendLine(r);
File.WriteAllLines(Path.Combine(Path.GetDirectoryName(csv),"blocks_5m_grades.csv"), new[]{"block,ch_from,ch_to,tonnes,au"}.Concat(res).ToArray());
return null;
