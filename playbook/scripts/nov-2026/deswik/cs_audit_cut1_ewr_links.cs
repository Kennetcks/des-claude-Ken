// 1) Headings whose cut #1 has no predecessor: external links that landed on a later cut are moved to cut #1.
// 2) New EWR tasks: EWD predecessors replaced by the EWD cut whose solid holds each raise end (top and bottom level).
// Dry run unless APPLY.
const bool APPLY = false;
string F(Deswik.Scheduler.Engine.Task t, string f) => t.GetFieldValueAndCacheIt(f) ?? "";
int No(Deswik.Scheduler.Engine.Task t) { int v; return int.TryParse(F(t, "DIS Activity Number"), out v) ? v : 0; }
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
var cur = Schedule.Tasks.Where(t => !t.Deleted).ToList(); var byUid = cur.ToDictionary(t => t.UniqueId);
var byKey = new Dictionary<string, Deswik.Scheduler.Engine.Task>(); foreach (var t in cur) if (F(t, "DIS Activity Type") == "Tunnel") byKey[F(t, "DIS Parent ID") + "|" + F(t, "DIS Activity Number")] = t;
var deps = Schedule.Dependencies.Where(d => !d.Deleted && byUid.ContainsKey(d.From) && byUid.ContainsKey(d.To)).ToList();
var toDel = new List<Deswik.Scheduler.Engine.Dependency>(); var toAdd = new List<(Deswik.Scheduler.Engine.Task a, Deswik.Scheduler.Engine.Task b, int layer)>();
// ---- 1
foreach (var p in new[] { "459361A", "459361B", "4593621", "4593622", "4593615", "4593616", "4593617" })
{
    var ts = cur.Where(t => F(t, "DIS Parent ID") == p && F(t, "DIS Activity Type") == "Tunnel").OrderBy(No).ToList(); if (ts.Count == 0) continue; var c1 = ts[0]; if (deps.Any(d => d.To == c1.UniqueId)) continue;
    foreach (var d in deps.Where(d => ts.Skip(1).Any(t => t.UniqueId == d.To) && F(byUid[d.From], "DIS Parent ID") != p))
    { toDel.Add(d); toAdd.Add((byUid[d.From], c1, d.LayerId)); Out.AppendLine($"move: {byUid[d.From].Name} #{No(byUid[d.From])} -> {byUid[d.To].Name} #{No(byUid[d.To])}  becomes -> #1"); }
}
// ---- 2: EWD solids (centre + bbox) and the new EWR strings' ends
var sol = new List<(string key, double[] lo, double[] hi)>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || f.Layer.Name != "SCHEDULER\\TASK SOLIDS" || !f.IsPolyface || A(f, "ACT_TYPE") != "EWD" || A(f, "MINE") != "W") continue;
    var vl = f.asPolyface.GetType().GetProperty("VertexList")?.GetValue(f.asPolyface) as System.Collections.IEnumerable; var lo = new[] { 1e18, 1e18, 1e18 }; var hi = new[] { -1e18, -1e18, -1e18 };
    foreach (var q in vl) { var ty = q.GetType(); var v = new[] { Convert.ToDouble(ty.GetProperty("x").GetValue(q)), Convert.ToDouble(ty.GetProperty("y").GetValue(q)), Convert.ToDouble(ty.GetProperty("z").GetValue(q)) }; for (int i = 0; i < 3; i++) { lo[i] = Math.Min(lo[i], v[i]); hi[i] = Math.Max(hi[i], v[i]); } }
    sol.Add((A(f, "DIS Parent ID") + "|" + A(f, "DIS Activity Number"), lo, hi));
}
double Gap(double[] q, double[] lo, double[] hi) { double s = 0; for (int i = 0; i < 3; i++) { double d = Math.Max(0, Math.Max(lo[i] - q[i], q[i] - hi[i])); s += d * d; } return Math.Sqrt(s); }
double Mid(double[] q, double[] lo, double[] hi) { double s = 0; for (int i = 0; i < 3; i++) s += Math.Pow(q[i] - (lo[i] + hi[i]) / 2, 2); return Math.Sqrt(s); }
var ewrs = new[] { "4593621", "4593622", "4593623", "4593624", "4593625", "4593626", "4593620", "4593627", "4593615", "4593616" };
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || !ewrs.Contains(f.Handle)) continue; var pl = f.asPolyline;
    var ends = new[] { pl.GetVertex(0), pl.GetVertex(pl.VertexCount - 1) }.Select(v => new[] { v.x, v.y, v.z }).ToList();
    var c1 = cur.Where(t => F(t, "DIS Parent ID") == f.Handle && F(t, "DIS Activity Type") == "Tunnel").OrderBy(No).FirstOrDefault(); if (c1 == null) { Out.AppendLine($"{f.Handle}: no task"); continue; }
    var want = new List<Deswik.Scheduler.Engine.Task>();
    foreach (var q in ends)
    {
        (string key, double[] lo, double[] hi) best = default; foreach (var dz in new[] { 2.5, -2.5 }) { var qq = new[] { q[0], q[1], q[2] + dz }; var b2 = sol.Where(s => Gap(qq, s.lo, s.hi) < 1.0 && byKey.ContainsKey(s.key)).OrderBy(s => Gap(qq, s.lo, s.hi)).ThenBy(s => Mid(qq, s.lo, s.hi)).FirstOrDefault(); if (b2.key != null && (best.key == null || Gap(qq, b2.lo, b2.hi) < 0.01)) best = b2; }
        if (best.key == null) { Out.AppendLine($"   {c1.Name}: no EWD cut holds the end at RL {q[2]:F1}"); continue; }
        want.Add(byKey[best.key]);
    }
    var have = deps.Where(d => d.To == c1.UniqueId && F(byUid[d.From], "ACT_TYPE") == "EWD").ToList();
    var wantU = new HashSet<int>(want.Select(t => t.UniqueId));
    if (want.Count == ends.Count) foreach (var d in have.Where(d => !wantU.Contains(d.From))) toDel.Add(d);
    foreach (var t in want.Where(t => !have.Any(d => d.From == t.UniqueId))) toAdd.Add((t, c1, 1));
    Out.AppendLine($"{c1.Name} ({f.Handle}): EWD preds {string.Join(" + ", have.Select(d => byUid[d.From].Name + "#" + No(byUid[d.From])))}  ->  {string.Join(" + ", want.Select(t => t.Name + "#" + No(t) + " (fin " + t.Finish.ToString("dd/MM/yy") + ")"))}");
}
// loop check
var g = new Dictionary<int, HashSet<int>>(); foreach (var d in deps) if (!toDel.Contains(d)) { if (!g.ContainsKey(d.From)) g[d.From] = new HashSet<int>(); g[d.From].Add(d.To); }
bool Reach(int a, int b) { var st = new Stack<int>(); var seen = new HashSet<int>(); st.Push(a); while (st.Count > 0) { var x = st.Pop(); if (x == b) return true; if (!seen.Add(x) || !g.TryGetValue(x, out var ns)) continue; foreach (var n in ns) st.Push(n); } return false; }
var ok = new List<(Deswik.Scheduler.Engine.Task a, Deswik.Scheduler.Engine.Task b, int layer)>();
foreach (var l in toAdd) { if (Reach(l.b.UniqueId, l.a.UniqueId)) { Out.AppendLine($"   SKIP loop {l.a.Name} -> {l.b.Name}"); continue; } ok.Add(l); if (!g.ContainsKey(l.a.UniqueId)) g[l.a.UniqueId] = new HashSet<int>(); g[l.a.UniqueId].Add(l.b.UniqueId); }
Out.AppendLine($"links to delete {toDel.Count}, to add {ok.Count}");
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }
var dm = Schedule.GetType().GetMethod("Delete", new[] { typeof(Deswik.Scheduler.Engine.Dependency), typeof(bool), typeof(bool) }); int e = 0;
foreach (var d in toDel) { try { dm.Invoke(Schedule, new object[] { d, true, false }); } catch { e++; } }
foreach (var l in ok) { try { var d = new Deswik.Scheduler.Engine.Dependency(-1, l.a.UniqueId, l.b.UniqueId); d.LinkType = Deswik.Scheduler.API.Contracts.LinkType.FinishStart; d.LayerId = l.layer; Schedule.Add(d, true, true, false); } catch { e++; } }
Schedule.ScheduleCalculator.CalculateSchedule(true, false); Schedule.ScheduleCalculator.WaitForCalculationsToComplete(false, 300000);
foreach (var p in new[] { "459361A", "459361B", "4593617" }.Concat(ewrs)) { var ts = cur.Where(t => F(t, "DIS Parent ID") == p && F(t, "DIS Activity Type") == "Tunnel").ToList(); if (ts.Count > 0) Out.AppendLine($"   {ts[0].Name}: starts {ts.Min(t => t.Start):dd/MM/yy}"); }
Out.AppendLine($"APPLIED, errors {e}"); return null;
