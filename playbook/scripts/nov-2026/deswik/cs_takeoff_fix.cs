// Fix take-offs: (string handle of the new drive, handle of the drive it comes off). Junction cut = the drive's cut whose centre is nearest
// to the new string's first-cut centre. Removes links from the new drive to cuts of its parent drive / named wrong-way targets.
const bool APPLY = false;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
var fixes = new List<(string child, string parent)> { ("45933C4", "458EE89"), ("45933B9", "458EDFA"), ("45931DF", "45933B9"), ("45931E0", "45931E1") };
var removeTo = new Dictionary<string, string[]> { { "45933C4", new[] { "458EE89", "459329F", "458EF99" } } }; // W390 EWD W no longer feeds W390 RAD W or the decline
var cur = Schedule.Tasks.Where(t => !t.Deleted).ToList(); var uid = cur.ToDictionary(t => t.UniqueId); var byId = cur.ToDictionary(t => t.Id);
string F(Deswik.Scheduler.Engine.Task t, string f) => t.GetFieldValueAndCacheIt(f) ?? "";
int No(Deswik.Scheduler.Engine.Task t) { int v; return int.TryParse(F(t, "DIS Activity Number"), out v) ? v : 0; }
List<Deswik.Scheduler.Engine.Task> Cuts(string h) => cur.Where(t => F(t, "DIS Parent ID") == h && F(t, "DIS Activity Type") == "Tunnel").OrderBy(No).ToList();
var cen = new Dictionary<string, double[]>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || f.Layer.Name != "SCHEDULER\\TASK SOLIDS" || !f.IsPolyface) continue;
    var vl = f.asPolyface.GetType().GetProperty("VertexList")?.GetValue(f.asPolyface) as System.Collections.IEnumerable; double x0 = 1e18, y0 = 1e18, z0 = 1e18, x1 = -1e18, y1 = -1e18, z1 = -1e18;
    foreach (var q in vl) { var ty = q.GetType(); double x = Convert.ToDouble(ty.GetProperty("x").GetValue(q)), y = Convert.ToDouble(ty.GetProperty("y").GetValue(q)), z = Convert.ToDouble(ty.GetProperty("z").GetValue(q)); x0 = Math.Min(x0, x); y0 = Math.Min(y0, y); z0 = Math.Min(z0, z); x1 = Math.Max(x1, x); y1 = Math.Max(y1, y); z1 = Math.Max(z1, z); }
    cen[A(f, "DIS Parent ID") + "|" + A(f, "DIS Activity Number") + "|" + A(f, "DIS Activity Type")] = new[] { (x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2 };
}
double[] C(Deswik.Scheduler.Engine.Task t) => cen.TryGetValue(F(t, "DIS Parent ID") + "|" + F(t, "DIS Activity Number") + "|Tunnel", out var c) ? c : null;
double D(double[] a, double[] b) => Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2) + Math.Pow(a[2] - b[2], 2));
var deps = Schedule.Dependencies.Where(d => !d.Deleted && uid.ContainsKey(d.From) && uid.ContainsKey(d.To)).ToList();
var edges = new HashSet<string>(deps.Select(d => uid[d.From].Id + ">" + uid[d.To].Id));
var del = new List<Deswik.Scheduler.Engine.Dependency>(); var add = new List<(string a, string b)>();
foreach (var kv in removeTo) { var mine = Cuts(kv.Key).Select(t => t.Id).ToHashSet(); foreach (var d in deps) { var a = uid[d.From]; var b = uid[d.To]; if (mine.Contains(a.Id) && kv.Value.Contains(F(b, "DIS Parent ID"))) { del.Add(d); edges.Remove(a.Id + ">" + b.Id); Out.AppendLine("DEL " + a.Name + " #" + No(a) + " -> " + b.Name + " #" + No(b) + " [" + F(b, "DIS Parent ID") + "]"); } } }
foreach (var (child, parent) in fixes)
{
    var c1 = Cuts(child).First(); var cc = C(c1); var pcs = Cuts(parent).Where(t => C(t) != null).ToList();
    var j = pcs.OrderBy(t => D(C(t), cc)).First();
    if (!edges.Contains(j.Id + ">" + c1.Id)) { add.Add((j.Id, c1.Id)); edges.Add(j.Id + ">" + c1.Id); Out.AppendLine("ADD " + j.Name + " #" + No(j) + " [" + parent + "] -> " + c1.Name + " #1 [" + child + "]  (junction " + D(C(j), cc).ToString("F1") + " m from first cut)"); }
}
// W390 RAD W (458EE89) #1 must keep an access
var r1 = Cuts("458EE89").First(); var hasOther = edges.Any(e => e.EndsWith(">" + r1.Id));
if (!hasOther) { var w395 = Cuts("459329B").Last(); add.Add((w395.Id, r1.Id)); edges.Add(w395.Id + ">" + r1.Id); Out.AppendLine("ADD " + w395.Name + " #" + No(w395) + " -> " + r1.Name + " #1 [458EE89] (access after removing the EWD link)"); }
else Out.AppendLine("W390 RAD W [458EE89] #1 keeps another predecessor: " + string.Join(", ", edges.Where(e => e.EndsWith(">" + r1.Id)).Select(e => byId[e.Split('>')[0]].Name)));
// loop check
var g = new Dictionary<string, List<string>>(); foreach (var e in edges) { var p = e.Split('>'); if (!g.ContainsKey(p[0])) g[p[0]] = new List<string>(); g[p[0]].Add(p[1]); }
bool Reach(string a, string b) { var st = new Stack<string>(); var seen = new HashSet<string>(); foreach (var n in g.TryGetValue(a, out var l) ? l : new List<string>()) st.Push(n); while (st.Count > 0) { var x = st.Pop(); if (x == b) return true; if (!seen.Add(x) || !g.TryGetValue(x, out var ns)) continue; foreach (var n in ns) st.Push(n); } return false; }
var loops = add.Where(a => Reach(a.b, a.a)).ToList(); foreach (var l in loops) Out.AppendLine("LOOP " + byId[l.a].Name + " -> " + byId[l.b].Name);
Out.AppendLine((APPLY ? "APPLIED" : "DRY RUN") + "  delete " + del.Count + "  add " + add.Count + "  loops " + loops.Count);
if (!APPLY || loops.Count > 0) return null;
var dm = Schedule.GetType().GetMethod("Delete", new[] { typeof(Deswik.Scheduler.Engine.Dependency), typeof(bool), typeof(bool) });
foreach (var d in del) dm.Invoke(Schedule, new object[] { d, true, false });
foreach (var (a, b) in add) { var d = new Deswik.Scheduler.Engine.Dependency(-1, byId[a].UniqueId, byId[b].UniqueId); d.LinkType = Deswik.Scheduler.API.Contracts.LinkType.FinishStart; d.LayerId = 1; Schedule.Add(d, true, true, false); }
Schedule.ScheduleCalculator.CalculateSchedule(true, false); Schedule.ScheduleCalculator.WaitForCalculationsToComplete(false, 300000);
return null;
