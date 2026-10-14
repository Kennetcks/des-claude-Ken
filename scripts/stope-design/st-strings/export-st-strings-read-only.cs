// ST strings: write Kennet's DESIGN\CREATING PLILLAR/STOPES\ST polylines to st_strings.csv (pid,handle,x,y,z) (read-only)
// Body of: object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out)

var inv=System.Globalization.CultureInfo.InvariantCulture;
string dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"Kennet.DeswikClaude","work","stopes_mine");
string nm="DESIGN\\CREATING PLILLAR/STOPES\\ST"; App.LoadLayer(nm,true); var rows=new List<string>{"pid,handle,x,y,z"}; int n=0;
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) {
  if(f.Deleted||f.Layer==null||f.Layer.Name!=nm||f.asPolyline==null) continue; n++;
  foreach (Deswik.Graphics.Geometry.Point q in f.asPolyline.VertexList) rows.Add("S"+n.ToString("000")+","+f.Handle+","+q.x.ToString("0.###",inv)+","+q.y.ToString("0.###",inv)+","+q.z.ToString("0.###",inv));
}
File.WriteAllLines(Path.Combine(dir,"st_strings.csv"),rows.ToArray());
Out.AppendLine("strings "+n+" vertices "+(rows.Count-1));
return null;
