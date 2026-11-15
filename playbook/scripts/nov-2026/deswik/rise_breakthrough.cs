if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Read-only: for the rise named in C:\dswtmp\find_text.txt (e.g. "H360 EWR"), list its tasks with links, the rise solid's bottom,
// and the scheduled development solids whose bounding box comes closest to that bottom point (the drive below that breaks into it).
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
string FV(Deswik.Scheduler.Engine.Task t, string f) { try { return System.Convert.ToString(t.GetNullaleFieldFullValue(f)); } catch { return ""; } }
var name = System.IO.File.ReadAllText(@"C:\dswtmp\find_text.txt").Trim();
var byUid = Schedule.Tasks.Where(t => !t.Deleted).ToDictionary(t => System.Convert.ToInt64(t.UniqueId));
string Links(Deswik.Scheduler.Engine.Task t, string prop, string end) => string.Join("\n        ", ((System.Collections.IEnumerable)t.GetType().GetProperty(prop, BF).GetValue(t)).Cast<object>()
  .Select(o => byUid.TryGetValue(System.Convert.ToInt64(o.GetType().GetProperty(end, BF).GetValue(o)), out var x) ? $"{x.Name} [{FV(x, "ACT_TYPE")}] {x.Start:dd/MM/yy HH:mm}-{x.Finish:dd/MM/yy HH:mm} {o.GetType().GetProperty("LinkType", BF).GetValue(o)} lag={o.GetType().GetProperty("Lag", BF).GetValue(o)} ({o.GetType().GetProperty("Layer", BF).GetValue(o)})" : "?"));
var rise = Schedule.Tasks.Where(t => !t.Deleted && t.Name.StartsWith(name)).OrderBy(t => t.Start).ToList();
if (rise.Count == 0) { Out.AppendLine($"no task named like '{name}'"); return null; }
foreach (var t in rise) {
  Out.AppendLine($"== {t.Name} [{t.Id}] {FV(t, "ACT_TYPE")} SCH={FV(t, "SCHEDULED")} {t.Start:dd/MM/yy HH:mm}-{t.Finish:dd/MM/yy HH:mm} res={t.PhysicalResource} m={FV(t, "Sch_metres")} {t.ConstraintType}");
  Out.AppendLine("   preds: " + Links(t, "Predecessors", "FromNumeric"));
  Out.AppendLine("   succs: " + Links(t, "Successors", "ToNumeric"));
}
double[] Box(Deswik.Graphics.Primaries.Figure f) => System.Convert.ToString(f.BoundingBox).Split(',').Select(double.Parse).ToArray();
// Rise solids: same MINE/LEVEL/ACT_TYPE as the rise tasks.
var mine = FV(rise[0], "MINE"); var lvl = FV(rise[0], "LEVEL"); var act = FV(rise[0], "ACT_TYPE");
var riseFigs = new List<Deswik.Graphics.Primaries.Figure>(); var dev = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) {
  if (System.Convert.ToString(f.AttributeValueGet("SCHEDULED", "")) != "1") continue;
  var fm = System.Convert.ToString(f.AttributeValueGet("MINE", "")); var fl = System.Convert.ToString(f.AttributeValueGet("LEVEL", "")); var fa = System.Convert.ToString(f.AttributeValueGet("ACT_TYPE", ""));
  if (fm == mine && fl == lvl && fa == act) riseFigs.Add(f);
  else if (fm == mine && !fa.Contains("Stope") && fa != "SLOT" && !fa.StartsWith("PDR") && !fa.Contains("CBL") && fa != act) dev.Add(f);
}
if (riseFigs.Count == 0) { Out.AppendLine("no rise solids found"); return null; }
var rb = riseFigs.Select(Box).ToList();
double cx = rb.Average(b => (b[0] + b[3]) / 2), cy = rb.Average(b => (b[1] + b[4]) / 2), zmin = rb.Min(b => b[2]), zmax = rb.Max(b => b[5]);
Out.AppendLine($"rise solids: {riseFigs.Count}, centre E {cx:0.0} N {cy:0.0}, RL {zmin:0.0} to {zmax:0.0}");
double Dist(double[] b, double z) { double dx = System.Math.Max(0, System.Math.Max(b[0] - cx, cx - b[3])), dy = System.Math.Max(0, System.Math.Max(b[1] - cy, cy - b[4])), dz = System.Math.Max(0, System.Math.Max(b[2] - z, z - b[5])); return System.Math.Sqrt(dx * dx + dy * dy + dz * dz); }
Out.AppendLine("closest development solids to the rise BOTTOM:");
foreach (var x in dev.Select(f => (f, d: Dist(Box(f), zmin))).OrderBy(x => x.d).Take(10))
  Out.AppendLine($"  {x.d:0.0} m: {x.f.GetType().GetProperty("Handle", BF)?.GetValue(x.f)} {x.f.AttributeValueGet("MINE", "")}{x.f.AttributeValueGet("LEVEL", "")} {x.f.AttributeValueGet("ACT_TYPE", "")} parent={x.f.AttributeValueGet("DIS Parent ID", "")} #{x.f.AttributeValueGet("DIS Activity Number", "")} {x.f.AttributeValueGet("Start", "")} RL {Box(x.f)[2]:0.0}-{Box(x.f)[5]:0.0}");
Out.AppendLine("closest development solids to the rise TOP:");
foreach (var x in dev.Select(f => (f, d: Dist(Box(f), zmax))).OrderBy(x => x.d).Take(5))
  Out.AppendLine($"  {x.d:0.0} m: {x.f.GetType().GetProperty("Handle", BF)?.GetValue(x.f)} {x.f.AttributeValueGet("MINE", "")}{x.f.AttributeValueGet("LEVEL", "")} {x.f.AttributeValueGet("ACT_TYPE", "")} parent={x.f.AttributeValueGet("DIS Parent ID", "")} #{x.f.AttributeValueGet("DIS Activity Number", "")} {x.f.AttributeValueGet("Start", "")} RL {Box(x.f)[2]:0.0}-{Box(x.f)[5]:0.0}");
return null;
