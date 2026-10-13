// ST strings: after write-grades-st-blocks.cs, remove unsplit blocks whose interrogated INT_AU is below MIN and drop them from st_blocks_grades.csv.
// Split blocks (a piece > 2.0 plus a lower piece) are always kept. Kennet 2026-10-13: first MIN = 0.5, then "delete, using the same logic, what is below 1.5g/t", so MIN = 1.5 is current.
// Body of: object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out)

string BASE="DESIGN\\CREATING PLILLAR/STOPES\\5M BLOCKS"; double MIN=1.5, t=0;
string dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"Kennet.DeswikClaude","work","stopes_mine");
var del=new List<Deswik.Graphics.Primaries.Figure>(); var names=new HashSet<string>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) {
  if (f.Deleted||f.Layer==null||!f.Layer.Name.StartsWith(BASE)||f.asPolyface==null) continue;
  string sp=Convert.ToString(f.AttributeValueGet("SPLIT","")); double au=Convert.ToDouble(f.AttributeValueGet("INT_AU",0.0));
  if ((sp==""||sp=="NO") && au<MIN) { del.Add(f); names.Add(Convert.ToString(f.AttributeValueGet("Name",""))); t+=Convert.ToDouble(f.AttributeValueGet("INT_TONNES",0.0)); }
}
foreach (var f in del) App.ModelSpace.Entities.Remove(f);
var path=Path.Combine(dir,"st_blocks_grades.csv"); var lines=File.ReadAllLines(path).ToList();
var keep=new List<string>{lines[0]}; keep.AddRange(lines.Skip(1).Where(l=>!names.Contains(l.Split(',')[0])));
File.WriteAllLines(path,keep.ToArray());
App.Redraw(true); Out.AppendLine("removed "+del.Count+" ("+string.Join(", ",names)+"), "+t.ToString("0")+" t; csv rows "+(keep.Count-1)); return null;
