// Take-off check for the strings in Replace_Plan2: for each string, find the cuts of OTHER strings that touch its first cut
// (the start of mining) and its last cut; report whether the first cut already depends (directly or through other tasks) on a touching cut.
// APPLY adds a link: touching cut (the one the drive reaches first) -> first cut, when no such dependency exists.
const bool APPLY = false;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
var plan = System.IO.File.ReadAllLines(@"C:\dswtmp\Replace_Plan2.tsv").Skip(1).Select(l => l.Split('\t')[0]).ToHashSet();
var cur = Schedule.Tasks.Where(t => !t.Deleted).ToList(); var uid = cur.ToDictionary(t => t.UniqueId); var byKey = new Dictionary<string, Deswik.Scheduler.Engine.Task>();
foreach (var t in cur) if (t.GetFieldValueAndCacheIt("DIS Activity Type") == "Tunnel") byKey[t.GetFieldValueAndCacheIt("DIS Parent ID") + "|" + t.GetFieldValueAndCacheIt("DIS Activity Number")] = t;
var pred = new Dictionary<string, List<string>>(); foreach (var d in Schedule.Dependencies) if (!d.Deleted && uid.ContainsKey(d.From) && uid.ContainsKey(d.To)) { var k = uid[d.To].Id; if (!pred.ContainsKey(k)) pred[k] = new List<string>(); pred[k].Add(uid[d.From].Id); }
// solids as boxes
var sol = new List<(Deswik.Graphics.Primaries.Figure f, string par, int no, double[] lo, double[] hi, Deswik.Scheduler.Engine.Task t)>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || f.Layer.Name != "SCHEDULER\\TASK SOLIDS" || !f.IsPolyface || A(f, "DIS Activity Type") != "Tunnel") continue;
    var vl = f.asPolyface.GetType().GetProperty("VertexList")?.GetValue(f.asPolyface) as System.Collections.IEnumerable; double x0 = 1e18, y0 = 1e18, z0 = 1e18, x1 = -1e18, y1 = -1e18, z1 = -1e18;
    foreach (var q in vl) { var ty = q.GetType(); double x = Convert.ToDouble(ty.GetProperty("x").GetValue(q)), y = Convert.ToDouble(ty.GetProperty("y").GetValue(q)), z = Convert.ToDouble(ty.GetProperty("z").GetValue(q)); x0 = Math.Min(x0, x); y0 = Math.Min(y0, y); z0 = Math.Min(z0, z); x1 = Math.Max(x1, x); y1 = Math.Max(y1, y); z1 = Math.Max(z1, z); }
    int no; int.TryParse(A(f, "DIS Activity Number"), out no); byKey.TryGetValue(A(f, "DIS Parent ID") + "|" + A(f, "DIS Activity Number"), out var tk);
    sol.Add((f, A(f, "DIS Parent ID"), no, new[] { x0, y0, z0 }, new[] { x1, y1, z1 }, tk));
}
double BoxGap((Deswik.Graphics.Primaries.Figure f, string par, int no, double[] lo, double[] hi, Deswik.Scheduler.Engine.Task t) a, (Deswik.Graphics.Primaries.Figure f, string par, int no, double[] lo, double[] hi, Deswik.Scheduler.Engine.Task t) b) { double s = 0; for (int i = 0; i < 3; i++) { double g = Math.Max(0, Math.Max(a.lo[i] - b.hi[i], b.lo[i] - a.hi[i])); s += g * g; } return Math.Sqrt(s); }
bool Reach(string from, string to) { var st = new Stack<string>(); var seen = new HashSet<string>(); st.Push(to); while (st.Count > 0) { var x = st.Pop(); if (x == from) return true; if (!seen.Add(x) || !pred.TryGetValue(x, out var ps)) continue; foreach (var p in ps) st.Push(p); } return false; }
var adds = new List<(Deswik.Scheduler.Engine.Task a, Deswik.Scheduler.Engine.Task b)>();
foreach (var h in plan)
{
    var mine = sol.Where(s => s.par == h && s.t != null).OrderBy(s => s.no).ToList(); if (mine.Count == 0) continue;
    var first = mine[0]; var last = mine[mine.Count - 1];
    var touchF = sol.Where(s => s.par != h && s.t != null && BoxGap(s, first) <= 0.5).ToList();
    var touchL = mine.Count > 1 ? sol.Where(s => s.par != h && s.t != null && BoxGap(s, last) <= 0.5).ToList() : new List<(Deswik.Graphics.Primaries.Figure f, string par, int no, double[] lo, double[] hi, Deswik.Scheduler.Engine.Task t)>();
    bool ok = touchF.Any(s => Reach(s.t.Id, first.t.Id));
    // a touching cut that is itself after our first cut doesn't count as a take-off
    var cand = touchF.Where(s => !Reach(first.t.Id, s.t.Id)).OrderBy(s => s.t.Finish).ToList();
    Out.AppendLine(first.t.Name + " [" + h + "] first cut starts " + first.t.Start.ToString("d/MM/yy") + (ok ? "  OK - waits on a touching drive" : "  ** NO TAKE-OFF LINK") );
    Out.AppendLine("   touching first cut: " + (touchF.Count == 0 ? "none" : string.Join("; ", touchF.Select(s => s.t.Name + " #" + s.no + " fin " + s.t.Finish.ToString("d/MM/yy") + (Reach(first.t.Id, s.t.Id) ? " (after us)" : "")))));
    if (touchL.Count > 0) Out.AppendLine("   touching last cut:  " + string.Join("; ", touchL.Select(s => s.t.Name + " #" + s.no + " fin " + s.t.Finish.ToString("d/MM/yy"))));
    if (!ok && cand.Count > 0) { var c = cand.First(); adds.Add((c.t, first.t)); Out.AppendLine("   -> add " + c.t.Name + " #" + c.no + " -> " + first.t.Name + " #1"); }
}
Out.AppendLine((APPLY ? "APPLIED " : "DRY RUN ") + adds.Count + " take-off links");
if (!APPLY) return null;
foreach (var (a, b) in adds) { var d = new Deswik.Scheduler.Engine.Dependency(-1, a.UniqueId, b.UniqueId); d.LinkType = Deswik.Scheduler.API.Contracts.LinkType.FinishStart; d.LayerId = 1; Schedule.Add(d, true, true, false); }
Schedule.ScheduleCalculator.CalculateSchedule(true, false); Schedule.ScheduleCalculator.WaitForCalculationsToComplete(false, 300000);
return null;
