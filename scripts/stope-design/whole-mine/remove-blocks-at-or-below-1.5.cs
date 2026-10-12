// Remove the 21 blocks at or below 1.5 g/t and the one degenerate block, update the grades CSV
// Body of: object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out)

string BASE="0\\06 SITE\\03 ENGINEERING\\DESIGN MASTER\\STOPES\\STOPE DESIGN\\5M BLOCKS MINE";
string dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"Kennet.DeswikClaude","work","stopes_mine");
var del=new List<Deswik.Graphics.Primaries.Figure>(); var names=new HashSet<string>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) {
  if (f.Deleted||f.Layer==null||!f.Layer.Name.StartsWith(BASE)||f.asPolyface==null) continue;
  if (Convert.ToString(f.AttributeValueGet("INT_DATE",""))==""||Convert.ToDouble(f.AttributeValueGet("INT_AU",0.0))<=1.5) { del.Add(f); names.Add(Convert.ToString(f.AttributeValueGet("Name",""))); }
}
foreach (var f in del) App.ModelSpace.Entities.Remove(f);
var path=Path.Combine(dir,"blocks_5m_mine_grades.csv"); var lines=File.ReadAllLines(path).ToList();
var keep=new List<string>{lines[0]}; keep.AddRange(lines.Skip(1).Where(l=>!names.Contains(l.Split(',')[0])));
File.WriteAllLines(path,keep.ToArray());
int left=0; double T=0,M=0; foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (!f.Deleted&&f.Layer!=null&&f.Layer.Name.StartsWith(BASE)&&f.asPolyface!=null){ left++; double t=Convert.ToDouble(f.AttributeValueGet("INT_TONNES",0.0)); T+=t; M+=t*Convert.ToDouble(f.AttributeValueGet("INT_AU",0.0)); }
App.Redraw(true);
Out.AppendLine("removed "+del.Count+", blocks left "+left+", "+T.ToString("0")+" t @ "+(M/T).ToString("0.00")+", csv rows "+(keep.Count-1));
return null;
