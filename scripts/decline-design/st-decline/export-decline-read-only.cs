// Write the DESIGN\DECLINE string vertices to decline_design.csv (handle,x,y,z) (read-only)
// Body of: object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out)

var inv=System.Globalization.CultureInfo.InvariantCulture;
string dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"Kennet.DeswikClaude","work","stopes_mine");
var rows=new List<string>{"handle,x,y,z"};
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities){ if(f.Deleted||f.Layer==null||f.Layer.Name!="DESIGN\\DECLINE"||f.asPolyline==null) continue;
 foreach (Deswik.Graphics.Geometry.Point q in f.asPolyline.VertexList) rows.Add(f.Handle+","+q.x.ToString("0.###",inv)+","+q.y.ToString("0.###",inv)+","+q.z.ToString("0.###",inv)); }
File.WriteAllLines(Path.Combine(dir,"decline_design.csv"),rows.ToArray()); Out.AppendLine("vertices "+(rows.Count-1)); return null;
