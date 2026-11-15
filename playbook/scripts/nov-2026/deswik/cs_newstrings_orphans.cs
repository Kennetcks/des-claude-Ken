// Fix new tasks (on Replace_Plan2 strings) that have no predecessor:
//  - derived: copy the predecessors of the old derived task of the same ACT_TYPE on the replaced string (mapped to current tasks);
//             for a brand-new string, use the string's donor task.
//  - tunnel:  copy the predecessors of the old first cut of the replaced string.
// Predecessors that were removed are mapped to their replacement (same string, nearest by order: last cut).
const bool APPLY = false;
var ci = System.Globalization.CultureInfo.InvariantCulture;
var tl = System.IO.File.ReadAllLines(@"C:\dswtmp\Schedule_Tasks_20261113_100751.tsv"); var th = tl[0].Split('\t').ToList();
string O(string[] r, string f) => r[th.IndexOf(f)];
var old = tl.Skip(1).Select(l => l.Split('\t')).ToDictionary(c => c[0]);
var plan = System.IO.File.ReadAllLines(@"C:\dswtmp\Replace_Plan2.tsv").Skip(1).Select(l => l.Split('\t')).ToList();
var newOf = new Dictionary<string, string>(); foreach (var p in plan) if (p[3] != "new") foreach (var o in p[1].Split(',')) if (!newOf.ContainsKey(o)) newOf[o] = p[0];
var dl = System.IO.File.ReadAllLines(@"C:\dswtmp\Schedule_Dependencies_20261113_100751.tsv").Skip(1).Select(l => l.Split('\t')).ToList();
var oPred = dl.GroupBy(c => c[6]).ToDictionary(g => g.Key, g => g.ToList());
var cur = Schedule.Tasks.Where(t => !t.Deleted).ToList(); var byId = cur.ToDictionary(t => t.Id); var uid = cur.ToDictionary(t => t.UniqueId, t => t.Id);
string F(Deswik.Scheduler.Engine.Task t, string f) => t.GetFieldValueAndCacheIt(f) ?? "";
int No(Deswik.Scheduler.Engine.Task t) { int v; return int.TryParse(F(t, "DIS Activity Number"), out v) ? v : 0; }
var have = new HashSet<string>(); var hasPred = new HashSet<string>();
foreach (var d in Schedule.Dependencies) if (!d.Deleted && uid.ContainsKey(d.From) && uid.ContainsKey(d.To)) { have.Add(uid[d.From] + ">" + uid[d.To]); hasPred.Add(uid[d.To]); }
// map an old task id to a current one
string Map(string o)
{
    if (byId.ContainsKey(o)) return o; if (!old.TryGetValue(o, out var r)) return null;
    var par = O(r, "DIS Parent ID"); if (!newOf.TryGetValue(par, out var np)) return null;
    var ty = O(r, "DIS Activity Type"); var cand = cur.Where(t => F(t, "DIS Parent ID") == np && F(t, "DIS Activity Type") == ty).ToList();
    if (ty == "Derived") return cand.FirstOrDefault(t => F(t, "ACT_TYPE") == O(r, "ACT_TYPE"))?.Id;
    return cand.OrderBy(No).LastOrDefault()?.Id;
}
var adds = new List<(string a, string b, string lag, string layer, string why)>();
foreach (var t in cur.Where(t => plan.Any(p => p[0] == F(t, "DIS Parent ID")) && !hasPred.Contains(t.Id)))
{
    var pl = plan.First(p => p[0] == F(t, "DIS Parent ID")); var ty = F(t, "DIS Activity Type");
    if (pl[3] == "new")
    {   // donor task
        string donor = null; foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (!f.Deleted && f.Handle == pl[2]) { var k = (f.AttributeValueGet("DIS Parent ID", "") ?? "") + "|" + (f.AttributeValueGet("DIS Activity Number", "") ?? ""); donor = cur.FirstOrDefault(x => F(x, "DIS Parent ID") + "|" + F(x, "DIS Activity Number") == k)?.Id; }
        if (donor != null) adds.Add((donor, t.Id, "0d", "1", "new string donor"));
        continue;
    }
    // old equivalent
    var oldTasks = pl[1].Split(',').SelectMany(p => old.Values.Where(r => O(r, "DIS Parent ID") == p)).ToList();
    string[] oe = ty == "Derived" ? oldTasks.FirstOrDefault(r => O(r, "DIS Activity Type") == "Derived" && O(r, "ACT_TYPE") == F(t, "ACT_TYPE"))
                                  : oldTasks.Where(r => O(r, "DIS Activity Type") == "Tunnel").OrderBy(r => int.Parse(O(r, "DIS Activity Number"))).FirstOrDefault();
    if (oe == null || !oPred.TryGetValue(oe[0], out var ps)) { Out.AppendLine("  no old preds for " + t.Name + " [" + ty + "]"); continue; }
    foreach (var c in ps) { var a = Map(c[1]); if (a == null || a == t.Id) { Out.AppendLine("  can't map pred " + c[2] + " of " + t.Name); continue; } adds.Add((a, t.Id, c[11], c[12], "old pred of " + O(oe, "Name"))); }
}
var uniq = adds.GroupBy(x => x.a + ">" + x.b).Select(g => g.First()).Where(x => !have.Contains(x.a + ">" + x.b)).ToList();
foreach (var x in uniq) Out.AppendLine("ADD " + byId[x.a].Name + " -> " + byId[x.b].Name + " [" + F(byId[x.b], "DIS Activity Type") + "] lag " + x.lag + "  (" + x.why + ")");
Out.AppendLine((APPLY ? "APPLIED " : "DRY RUN ") + uniq.Count + " links");
if (!APPLY) return null;
foreach (var x in uniq) { var d = new Deswik.Scheduler.Engine.Dependency(-1, byId[x.a].UniqueId, byId[x.b].UniqueId); d.LinkType = Deswik.Scheduler.API.Contracts.LinkType.FinishStart; double lg = double.Parse(x.lag.TrimEnd('d'), ci); if (lg != 0) d.Lag = new Deswik.Scheduler.Engine.Lag(lg, Deswik.Scheduler.Engine.TimeUnit.d, null); int ly; if (int.TryParse(x.layer, out ly)) d.LayerId = ly; Schedule.Add(d, true, true, false); }
Schedule.ScheduleCalculator.CalculateSchedule(true, false); Schedule.ScheduleCalculator.WaitForCalculationsToComplete(false, 300000);
Out.AppendLine("deps now " + Schedule.Dependencies.Count(d => !d.Deleted));
return null;
