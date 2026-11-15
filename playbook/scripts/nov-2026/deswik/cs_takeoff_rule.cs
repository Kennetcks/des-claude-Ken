// Take-off rule: for every lateral tunnel string whose first cut sits on another lateral string (the parent) at junction cut j:
//   parent cut j+3  ->  child cut #1        (parent 3 cuts past the junction before the take-off starts)
//   child cut #2    ->  parent cut j+4      (take-off 2 cuts in before the parent carries on)
// Existing links from the parent into the child's first cut, and from the child back into the parent, are replaced.
// SCOPE: "new" = Replace_Plan2 strings only; "all" = every take-off. REPORT_ONLY lists the current pattern without proposing changes.
const bool APPLY = false; const string SCOPE = "all"; const bool REPORT_ONLY = false;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
var planSet = System.IO.File.ReadAllLines(@"C:\dswtmp\Replace_Plan2.tsv").Skip(1).Select(l => l.Split('\t')[0]).ToHashSet();
var cur = Schedule.Tasks.Where(t => !t.Deleted).ToList(); var uid = cur.ToDictionary(t => t.UniqueId); var byId = cur.ToDictionary(t => t.Id);
string F(Deswik.Scheduler.Engine.Task t, string f) => t.GetFieldValueAndCacheIt(f) ?? "";
int No(Deswik.Scheduler.Engine.Task t) { int v; return int.TryParse(F(t, "DIS Activity Number"), out v) ? v : 0; }
var tun = cur.Where(t => F(t, "DIS Activity Type") == "Tunnel" && F(t, "SCHEDULED") == "1").ToList();
var strings = tun.GroupBy(t => F(t, "DIS Parent ID")).ToDictionary(g => g.Key, g => g.OrderBy(No).ToList());
var cen = new Dictionary<string, double[]>(); var lat = new Dictionary<string, bool>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || f.Layer.Name != "SCHEDULER\\TASK SOLIDS" || !f.IsPolyface || A(f, "DIS Activity Type") != "Tunnel") continue;
    var vl = f.asPolyface.GetType().GetProperty("VertexList")?.GetValue(f.asPolyface) as System.Collections.IEnumerable; double x0 = 1e18, y0 = 1e18, z0 = 1e18, x1 = -1e18, y1 = -1e18, z1 = -1e18;
    foreach (var q in vl) { var ty = q.GetType(); double x = Convert.ToDouble(ty.GetProperty("x").GetValue(q)), y = Convert.ToDouble(ty.GetProperty("y").GetValue(q)), z = Convert.ToDouble(ty.GetProperty("z").GetValue(q)); x0 = Math.Min(x0, x); y0 = Math.Min(y0, y); z0 = Math.Min(z0, z); x1 = Math.Max(x1, x); y1 = Math.Max(y1, y); z1 = Math.Max(z1, z); }
    var k = A(f, "DIS Parent ID") + "|" + A(f, "DIS Activity Number"); cen[k] = new[] { (x0 + x1) / 2, (y0 + y1) / 2, z0 }; lat[A(f, "DIS Parent ID")] = A(f, "LAT_VERT") == "LAT";
}
double[] C(Deswik.Scheduler.Engine.Task t) => cen.TryGetValue(F(t, "DIS Parent ID") + "|" + F(t, "DIS Activity Number"), out var c) ? c : null;
double D(double[] a, double[] b) => Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2) + Math.Pow(a[2] - b[2], 2));
var deps = Schedule.Dependencies.Where(d => !d.Deleted && uid.ContainsKey(d.From) && uid.ContainsKey(d.To)).ToList();
var pred = deps.GroupBy(d => uid[d.To].Id).ToDictionary(g => g.Key, g => g.ToList());
// spatial index of cut centres by string
var allCuts = strings.SelectMany(kv => kv.Value.Where(t => C(t) != null).Select(t => (str: kv.Key, t))).ToList();
var succCur = deps.GroupBy(d => uid[d.From].Id).ToDictionary(g0 => g0.Key, g0 => g0.Select(d => uid[d.To].Id).ToList()); int skippedHole = 0;
bool ReachCur(string a, string b) { var st = new Stack<string>(); var seen = new HashSet<string>(); st.Push(a); while (st.Count > 0) { var x = st.Pop(); if (x == b) return true; if (!seen.Add(x) || !succCur.TryGetValue(x, out var ns)) continue; foreach (var y in ns) st.Push(y); } return false; }
var offsets = new Dictionary<int, int>(); int back2 = 0, backAny = 0, n = 0; var changes = new List<string>();
var toDel = new List<Deswik.Scheduler.Engine.Dependency>(); var toAdd = new List<(Deswik.Scheduler.Engine.Task a, Deswik.Scheduler.Engine.Task b, string why)>();
foreach (var kv in strings)
{
    if (!lat.TryGetValue(kv.Key, out var isLat) || !isLat || kv.Value.Count < 2) continue;
    if (SCOPE == "new" && !planSet.Contains(kv.Key)) continue;
    var c1 = kv.Value[0]; var cc = C(c1); if (cc == null) continue;
    if (F(c1, "ACT_TYPE") == "DEC") continue; // declines continue from the previous decline string - not a take-off
    // parent: nearest cut of another lateral string, within 6 m, that the child's first cut actually depends on (directly) or nearest overall
    var near = allCuts.Where(x => x.str != kv.Key && lat.TryGetValue(x.str, out var l2) && l2).Select(x => (x.str, x.t, d: D(C(x.t), cc))).Where(x => x.d <= 6).OrderBy(x => x.d).ToList();
    if (near.Count == 0) continue;
    // prefer a nearby string the first cut already depends on
    var linked = pred.TryGetValue(c1.Id, out var pl0) ? pl0.Select(d => F(uid[d.From], "DIS Parent ID")).ToHashSet() : new HashSet<string>();
    var parentStr = near.Any(x => linked.Contains(x.str)) ? near.First(x => linked.Contains(x.str)).str : near[0].str;
    var pcuts = strings[parentStr]; var j = near.Where(x => x.str == parentStr).OrderBy(x => x.d).First().t; int ji = pcuts.IndexOf(j);
    // skip if the "parent" junction cut already comes after the take-off (drive holing into this one, not a take-off)
    if (ReachCur(c1.Id, j.Id)) { skippedHole++; continue; }
    n++;
    var ps = pred.TryGetValue(c1.Id, out var l) ? l.Where(d => F(uid[d.From], "DIS Parent ID") == parentStr).ToList() : new List<Deswik.Scheduler.Engine.Dependency>();
    foreach (var d in ps) { var off = pcuts.IndexOf(uid[d.From]) - ji; offsets[off] = offsets.TryGetValue(off, out var z) ? z + 1 : 1; }
    var backs = deps.Where(d => F(uid[d.From], "DIS Parent ID") == kv.Key && F(uid[d.To], "DIS Parent ID") == parentStr).ToList();
    if (backs.Count > 0) backAny++; if (backs.Any(d => kv.Value.IndexOf(uid[d.From]) == 1)) back2++;
    if (REPORT_ONLY) continue;
    var pA = pcuts[Math.Min(ji + 3, pcuts.Count - 1)]; var pB = ji + 4 < pcuts.Count ? pcuts[ji + 4] : null; var c2 = kv.Value[1];
    foreach (var d in ps) if (uid[d.From].Id != pA.Id) toDel.Add(d);
    if (!ps.Any(d => uid[d.From].Id == pA.Id)) toAdd.Add((pA, c1, "3 ahead"));
    if (pB != null && !backs.Any(d => uid[d.From].Id == c2.Id && uid[d.To].Id == pB.Id)) toAdd.Add((c2, pB, "2 in"));
    changes.Add(c1.Name + " off " + pcuts[0].Name + " at #" + No(j) + ":  " + pA.Name + " #" + No(pA) + " -> " + c1.Name + " #1" + (pB != null ? ";  " + c2.Name + " #2 -> " + pB.Name + " #" + No(pB) : "  (parent ends, no resume link)"));
}
Out.AppendLine("take-offs found " + n + " (scope " + SCOPE + "), skipped as hole-throughs " + skippedHole);
System.IO.File.WriteAllLines(@"C:\dswtmp\Takeoff_Rule_" + (APPLY ? "Applied" : "DryRun") + ".txt", changes);
Out.AppendLine("current link from parent into first cut, offset from junction cut: " + string.Join(", ", offsets.OrderBy(k => k.Key).Select(k => (k.Key >= 0 ? "+" : "") + k.Key + ": " + k.Value)));
Out.AppendLine("take-offs with any link back into the parent: " + backAny + ", from the take-off's cut #2: " + back2);
if (REPORT_ONLY) return null;
foreach (var c in changes) Out.AppendLine("  " + c);
// loop check
var edges = new HashSet<string>(deps.Select(d => uid[d.From].Id + ">" + uid[d.To].Id)); foreach (var d in toDel) edges.Remove(uid[d.From].Id + ">" + uid[d.To].Id); foreach (var a in toAdd) edges.Add(a.a.Id + ">" + a.b.Id);
var g = new Dictionary<string, List<string>>(); foreach (var e in edges) { var p = e.Split('>'); if (!g.ContainsKey(p[0])) g[p[0]] = new List<string>(); g[p[0]].Add(p[1]); }
bool Reach(string a, string b) { var st = new Stack<string>(); var seen = new HashSet<string>(); foreach (var x in g.TryGetValue(a, out var l0) ? l0 : new List<string>()) st.Push(x); while (st.Count > 0) { var x = st.Pop(); if (x == b) return true; if (!seen.Add(x) || !g.TryGetValue(x, out var ns)) continue; foreach (var y in ns) st.Push(y); } return false; }
// add one at a time: start from the graph without the new links, skip any that would close a loop ("3 ahead" first, then "2 in")
foreach (var a in toAdd) { var p = a.a.Id; if (g.ContainsKey(p)) g[p].Remove(a.b.Id); }
var keep = new List<(Deswik.Scheduler.Engine.Task a, Deswik.Scheduler.Engine.Task b, string why)>(); var skipped = new List<string>();
foreach (var a in toAdd.OrderBy(x => x.why == "3 ahead" ? 0 : 1)) { if (Reach(a.b.Id, a.a.Id) || a.a.Id == a.b.Id) { skipped.Add(a.why + ": " + a.a.Name + " #" + No(a.a) + " -> " + a.b.Name + " #" + No(a.b)); continue; } keep.Add(a); if (!g.ContainsKey(a.a.Id)) g[a.a.Id] = new List<string>(); g[a.a.Id].Add(a.b.Id); }
System.IO.File.WriteAllLines(@"C:\dswtmp\Takeoff_Rule_Skipped.txt", skipped);
Out.AppendLine((APPLY ? "APPLIED" : "DRY RUN") + "  delete " + toDel.Distinct().Count() + "  add " + keep.Count + " (3 ahead " + keep.Count(k => k.why == "3 ahead") + ", 2 in " + keep.Count(k => k.why == "2 in") + ")  skipped for loops " + skipped.Count + " (3 ahead " + skipped.Count(s => s.StartsWith("3")) + ", 2 in " + skipped.Count(s => s.StartsWith("2")) + ")");
toAdd = keep;
if (!APPLY) return null;
var dm = Schedule.GetType().GetMethod("Delete", new[] { typeof(Deswik.Scheduler.Engine.Dependency), typeof(bool), typeof(bool) });
foreach (var d in toDel.Distinct()) dm.Invoke(Schedule, new object[] { d, true, false });
foreach (var a in toAdd) { var d = new Deswik.Scheduler.Engine.Dependency(-1, a.a.UniqueId, a.b.UniqueId); d.LinkType = Deswik.Scheduler.API.Contracts.LinkType.FinishStart; d.LayerId = 1; Schedule.Add(d, true, true, false); }
Schedule.ScheduleCalculator.CalculateSchedule(true, false); Schedule.ScheduleCalculator.WaitForCalculationsToComplete(false, 300000);
return null;
