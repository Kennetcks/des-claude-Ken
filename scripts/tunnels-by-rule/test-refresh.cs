// Clear the tunnel solids layer, refresh twice and check the solid count stays the same
// Body of: object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out)
// Usings: 

string OLAYER="DESIGN\\FLOOR STRINGS\\SOLIDS";
Deswik.Graphics.Primaries.Layer ol=null; foreach (Deswik.Graphics.Primaries.Layer l in App.Layers) if (l.Name==OLAYER) ol=l;
Func<int> count = () => { int c=0; foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (f.Layer==ol && f.asPolyface!=null) c++; return c; };
var old=new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (f.Layer==ol) old.Add(f);
foreach (var f in old) App.ModelSpace.Entities.Remove(f);
Out.AppendLine("cleared "+old.Count+" -> "+count());
new Deswik.Graphics.Tunnels.SectionAlongPolylineRules(App).Refresh(App, OLAYER);
Out.AppendLine("after refresh 1: "+count());
new Deswik.Graphics.Tunnels.SectionAlongPolylineRules(App).Refresh(App, OLAYER);
Out.AppendLine("after refresh 2: "+count());
Out.AppendLine("legend='"+ol.Legend+"'");
// sample a solid's xprops to see generation tags
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (f.Layer==ol) { foreach (var o in (System.Collections.IEnumerable)f.XProperties) Out.AppendLine("  "+o.GetType().GetProperty("Name").GetValue(o,null)+" = "+o.GetType().GetProperty("Value").GetValue(o,null)); break; }
App.Redraw(true);
return null;