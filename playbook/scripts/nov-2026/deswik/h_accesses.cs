if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Read-only: every scheduled heading on the H side that is not the decline. For each, its first task (no same-name
// predecessor), that task's links to other headings, and the H DEC round whose CAD solid is nearest to the first round's solid.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
string FV(Deswik.Scheduler.Engine.Task t, string f) { try { return System.Convert.ToString(t.GetNullaleFieldFullValue(f)); } catch { return ""; } }
var live = Schedule.Tasks.Where(t => !t.Deleted && FV(t, "SCHEDULED") == "1").ToList();
var byUid = live.ToDictionary(t => System.Convert.ToInt64(t.UniqueId));
long V(object o, string p) => System.Convert.ToInt64(o.GetType().GetProperty(p, BF).GetValue(o));
object P(object o, string p) => o.GetType().GetProperty(p, BF).GetValue(o);
IEnumerable<(Deswik.Scheduler.Engine.Task x, object o)> Lk(Deswik.Scheduler.Engine.Task t, string prop, string end) =>
  ((System.Collections.IEnumerable)t.GetType().GetProperty(prop, BF).GetValue(t)).Cast<object>().Where(o => byUid.ContainsKey(V(o, end))).Select(o => (byUid[V(o, end)], o));
// Figures keyed by DIS Parent ID + activity number, to place tasks in space.
var fig = new Dictionary<string, double[]>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) {
  var k = System.Convert.ToString(f.AttributeValueGet("DIS Parent ID", "")) + "#" + System.Convert.ToString(f.AttributeValueGet("DIS Activity Number", ""));
  if (k.Length > 1 && !fig.ContainsKey(k)) fig[k] = System.Convert.ToString(f.BoundingBox).Split(',').Select(double.Parse).ToArray();
}
double[] Box(Deswik.Scheduler.Engine.Task t) => fig.TryGetValue(FV(t, "DIS Parent ID") + "#" + FV(t, "DIS Activity Number"), out var b) ? b : null;
Out.AppendLine($"field check: DIS Parent ID='{FV(live[0], "DIS Parent ID")}' Act#='{FV(live[0], "DIS Activity Number")}' figs={fig.Count}");
var dec = live.Where(t => t.Name.StartsWith("H") && t.Name.Contains(" DEC ")).OrderBy(t => t.Start).ToList();
double D(double[] a, double[] b) { double cx(double[] q, int i) => (q[i] + q[i + 3]) / 2; return System.Math.Sqrt(System.Math.Pow(cx(a, 0) - cx(b, 0), 2) + System.Math.Pow(cx(a, 1) - cx(b, 1), 2) + System.Math.Pow(cx(a, 2) - cx(b, 2), 2)); }
foreach (var g in live.Where(t => t.Name.StartsWith("H") && !t.Name.Contains(" DEC ")).GroupBy(t => t.Name).OrderBy(g => g.Min(t => t.Start))) {
  var first = g.Where(t => !Lk(t, "Predecessors", "FromNumeric").Any(l => l.x.Name == t.Name)).OrderBy(t => t.Start).ToList();
  Out.AppendLine($"=== {g.Key} [{FV(g.First(), "ACT_TYPE")}] {g.Count()} tasks {g.Min(t => t.Start):dd/MM/yy}-{g.Max(t => t.Finish):dd/MM/yy} res={string.Join(",", g.Select(t => t.PhysicalResource).Distinct())}");
  foreach (var t in first) {
    var b = Box(t);
    var near = b == null ? "no solid" : string.Join("; ", dec.Where(d => Box(d) != null).Select(d => (d, m: D(b, Box(d)))).OrderBy(x => x.m).Take(2).Select(x => $"{x.d.Name} [{x.d.Id}] {x.m:0}m {x.d.Start:dd/MM/yy}"));
    Out.AppendLine($"  start {t.Id} {t.Start:dd/MM/yy} {t.ConstraintType} | nearest H DEC: {near}");
    foreach (var l in Lk(t, "Predecessors", "FromNumeric").Where(l => l.x.Name != t.Name)) Out.AppendLine($"    <- {l.x.Name} [{l.x.Id}] {l.x.Finish:dd/MM/yy} {P(l.o, "LinkType")} L{P(l.o, "Layer")}");
    foreach (var l in Lk(t, "Successors", "ToNumeric").Where(l => l.x.Name != t.Name)) Out.AppendLine($"    -> {l.x.Name} [{l.x.Id}] {l.x.Start:dd/MM/yy} {P(l.o, "LinkType")} L{P(l.o, "Layer")}");
  }
}
return null;
