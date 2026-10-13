// Export every string on DESIGN\DECLINE (vertices + all layer fields) to kd_strings.csv / kd_attrs.csv (read-only)
// Body of: object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out)
var inv=System.Globalization.CultureInfo.InvariantCulture;
string dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"Kennet.DeswikClaude","work","stopes_mine");
string LN="DESIGN\\DECLINE";
Deswik.Graphics.Primaries.Layer ly=null; foreach (Deswik.Graphics.Primaries.Layer l in App.Layers) if(l.Name==LN&&!l.Deleted) ly=l;
var flds=new List<string>(); foreach (var a in (System.Collections.IEnumerable)ly.Attributes) flds.Add(Convert.ToString(a.GetType().GetProperty("Name").GetValue(a,null)));
var rows=new List<string>{"handle,vi,x,y,z"}; var arow=new List<string>{"handle,type,color,"+string.Join(",",flds.Select(x=>"\""+x+"\""))};
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities){ if(f.Deleted||f.Layer!=ly) continue;
 var vals=flds.Select(x=>"\""+Convert.ToString(f.AttributeValueGet(x,"")).Replace("\"","'")+"\"");
 arow.Add(f.Handle+","+f.GetType().Name+",\"\","+string.Join(",",vals));
 if(f.asPolyline!=null){ int i=0; foreach (Deswik.Graphics.Geometry.Point q in f.asPolyline.VertexList) rows.Add(f.Handle+","+(i++)+","+q.x.ToString("0.###",inv)+","+q.y.ToString("0.###",inv)+","+q.z.ToString("0.###",inv)); } }
File.WriteAllLines(Path.Combine(dir,"kd_strings.csv"),rows.ToArray()); File.WriteAllLines(Path.Combine(dir,"kd_attrs.csv"),arow.ToArray());
Out.AppendLine("strings "+(arow.Count-1)+", vertices "+(rows.Count-1)); return null;
