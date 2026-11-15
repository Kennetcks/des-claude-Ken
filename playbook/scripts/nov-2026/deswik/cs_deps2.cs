// Rebuild dependencies to match the old schedule (Map2.tsv).
// Desired = old links mapped (pred end -> last new cut of that old cut, succ end -> first new cut), with links through old tasks
// that have no new task bridged pred->succ, plus the within-string cut chain of each new string. Removes current links not desired.
const bool APPLY = false;
var ci = System.Globalization.CultureInfo.InvariantCulture;
var dm = Schedule.GetType().GetMethod("Delete", new[] { typeof(Deswik.Scheduler.Engine.Dependency), typeof(bool), typeof(bool) });
Out.AppendLine("Delete(" + string.Join(", ", dm.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name)) + ")");
var cur = Schedule.Tasks.Where(t => !t.Deleted).ToList(); var byId = cur.ToDictionary(t => t.Id); var uid = cur.ToDictionary(t => t.UniqueId, t => t.Id);
string F(Deswik.Scheduler.Engine.Task t, string f) => t.GetFieldValueAndCacheIt(f) ?? "";
int ActNo(string id) { int v; return int.TryParse(F(byId[id], "DIS Activity Number"), out v) ? v : 0; }
var map = System.IO.File.ReadAllLines(@"C:\dswtmp\Map2.tsv").Skip(1).Select(l => l.Split('\t')).ToList();
var o2n = new Dictionary<string, List<string>>(); foreach (var m in map) { if (!byId.ContainsKey(m[0])) continue; if (!o2n.ContainsKey(m[1])) o2n[m[1]] = new List<string>(); o2n[m[1]].Add(m[0]); }
string Last(string o) => o2n[o].OrderBy(ActNo).Last(); string First(string o) => o2n[o].OrderBy(ActNo).First();
var dl = System.IO.File.ReadAllLines(@"C:\dswtmp\Schedule_Dependencies_20261113_090226.tsv").Skip(1).Select(l => l.Split('\t')).ToList();
var oSucc = new Dictionary<string, List<string[]>>(); var oPred = new Dictionary<string, List<string[]>>();
foreach (var c in dl) { if (!oSucc.ContainsKey(c[1])) oSucc[c[1]] = new List<string[]>(); oSucc[c[1]].Add(c); if (!oPred.ContainsKey(c[6])) oPred[c[6]] = new List<string[]>(); oPred[c[6]].Add(c); }
// desired: key "a>b" -> (lag, layer, source)
var want = new Dictionary<string, (string lag, string layer, string src)>();
void Want(string a, string b, string lag, string layer, string src) { if (a == b) return; var k = a + ">" + b; if (!want.ContainsKey(k)) want[k] = (lag, layer, src); }
int bridged = 0, dropped = 0;
foreach (var c in dl)
{
    bool ma = o2n.ContainsKey(c[1]), mb = o2n.ContainsKey(c[6]);
    if (ma && mb) { Want(Last(c[1]), First(c[6]), c[11], c[12], "old"); continue; }
    if (ma && !mb)
    {   // walk forward through unmapped old tasks to mapped successors
        var seen = new HashSet<string>(); var st = new Stack<string>(); st.Push(c[6]); bool any = false;
        while (st.Count > 0) { var x = st.Pop(); if (!seen.Add(x)) continue; if (o2n.ContainsKey(x)) { Want(Last(c[1]), First(x), c[11], c[12], "bridge"); any = true; continue; } if (oSucc.TryGetValue(x, out var ns)) foreach (var n in ns) st.Push(n[6]); }
        if (any) bridged++; else dropped++;
    }
    else if (!ma && mb)
    {   // walk backward through unmapped old tasks to mapped predecessors
        var seen = new HashSet<string>(); var st = new Stack<string>(); st.Push(c[1]); bool any = false;
        while (st.Count > 0) { var x = st.Pop(); if (!seen.Add(x)) continue; if (o2n.ContainsKey(x)) { Want(Last(x), First(c[6]), c[11], c[12], "bridge"); any = true; continue; } if (oPred.TryGetValue(x, out var ps)) foreach (var p in ps) st.Push(p[1]); }
        if (any) bridged++; else dropped++;
    }
    else dropped++;
}
// within-string chains (only where they don't contradict the old-link order)
int chain = 0, chainSkip = 0;
var wg = new Dictionary<string, HashSet<string>>(); foreach (var k in want.Keys) { var p = k.Split('>'); if (!wg.ContainsKey(p[0])) wg[p[0]] = new HashSet<string>(); wg[p[0]].Add(p[1]); }
bool Reach(string a, string b) { var st = new Stack<string>(); var seen = new HashSet<string>(); st.Push(a); while (st.Count > 0) { var x = st.Pop(); if (x == b) return true; if (!seen.Add(x) || !wg.TryGetValue(x, out var ns)) continue; foreach (var n in ns) st.Push(n); } return false; }
foreach (var g in cur.Where(t => F(t, "DIS Activity Type") == "Tunnel").GroupBy(t => F(t, "DIS Parent ID")))
{ var s = g.Select(t => t.Id).OrderBy(ActNo).ToList(); for (int i = 1; i < s.Count; i++) { var k = s[i - 1] + ">" + s[i]; if (want.ContainsKey(k)) continue; if (Reach(s[i], s[i - 1])) { chainSkip++; continue; } want[k] = ("0d", "1", "chain"); chain++; if (!wg.ContainsKey(s[i - 1])) wg[s[i - 1]] = new HashSet<string>(); wg[s[i - 1]].Add(s[i]); } }
Out.AppendLine("chain links skipped (old order says otherwise) " + chainSkip);
// current
var curDeps = Schedule.Dependencies.Where(d => !d.Deleted && uid.ContainsKey(d.From) && uid.ContainsKey(d.To)).ToList();
var have = new Dictionary<string, Deswik.Scheduler.Engine.Dependency>(); foreach (var d in curDeps) have[uid[d.From] + ">" + uid[d.To]] = d;
var toDel = have.Where(kv => !want.ContainsKey(kv.Key)).ToList();
var toAdd = want.Where(kv => !have.ContainsKey(kv.Key)).ToList();
// lag fixes on kept links
int lagFix = 0; var lagList = new List<(Deswik.Scheduler.Engine.Dependency d, double days)>();
foreach (var kv in want) if (have.TryGetValue(kv.Key, out var d)) { double w = double.Parse(kv.Value.lag.TrimEnd('d'), ci); var cl = d.Lag == null ? "0d" : d.Lag.ToString(); double h; if (!double.TryParse(cl.TrimEnd('d'), System.Globalization.NumberStyles.Float, ci, out h)) h = -999; if (Math.Abs(h - w) > 1e-6) { lagFix++; lagList.Add((d, w)); } }
// loop check on final graph
var succ = new Dictionary<string, List<string>>(); foreach (var k in want.Keys) { var p = k.Split('>'); if (!succ.ContainsKey(p[0])) succ[p[0]] = new List<string>(); succ[p[0]].Add(p[1]); }
var color = new Dictionary<string, int>(); int cycles = 0; var cyc = new List<string>();
foreach (var start in succ.Keys) { if (color.ContainsKey(start)) continue; var st = new Stack<(string n, int i)>(); st.Push((start, 0)); color[start] = 1;
  while (st.Count > 0) { var (n, i) = st.Pop(); var ns = succ.TryGetValue(n, out var l) ? l : new List<string>(); if (i < ns.Count) { st.Push((n, i + 1)); var m = ns[i]; if (!color.ContainsKey(m)) { color[m] = 1; st.Push((m, 0)); } else if (color[m] == 1) { cycles++; if (cyc.Count < 10) cyc.Add(byId[n].Name + " -> " + byId[m].Name + " (" + want[n + ">" + m].src + ")"); } } else color[n] = 2; } }
Out.AppendLine("old links " + dl.Count + "  bridged " + bridged + "  lost (no new task either side) " + dropped);
Out.AppendLine("desired " + want.Count + " (chain-only " + chain + ")  current " + have.Count + "  to delete " + toDel.Count + "  to add " + toAdd.Count + "  lag fixes " + lagFix + "  cycles in desired " + cycles);
foreach (var c in cyc) Out.AppendLine("  CYCLE " + c);
string Ty(string id) => F(byId[id], "DIS Activity Type");
foreach (var g in toDel.GroupBy(kv => Ty(kv.Key.Split('>')[0]) + "->" + Ty(kv.Key.Split('>')[1]))) Out.AppendLine("  DEL " + g.Key + "\t" + g.Count());
foreach (var g in toAdd.GroupBy(kv => kv.Value.src + " " + Ty(kv.Key.Split('>')[0]) + "->" + Ty(kv.Key.Split('>')[1]))) Out.AppendLine("  ADD " + g.Key + "\t" + g.Count());
var rep = new StringBuilder("Action\tFrom\tFromName\tTo\tToName\tLag\tSrc\n");
foreach (var kv in toDel) { var p = kv.Key.Split('>'); rep.Append("DEL\t" + p[0] + "\t" + byId[p[0]].Name + "\t" + p[1] + "\t" + byId[p[1]].Name + "\t\t\n"); }
foreach (var kv in toAdd) { var p = kv.Key.Split('>'); rep.Append("ADD\t" + p[0] + "\t" + byId[p[0]].Name + "\t" + p[1] + "\t" + byId[p[1]].Name + "\t" + kv.Value.lag + "\t" + kv.Value.src + "\n"); }
System.IO.File.WriteAllText(@"C:\dswtmp\Deps2_" + (APPLY ? "Applied" : "DryRun") + ".tsv", rep.ToString(), new UTF8Encoding(true));
if (!APPLY || cycles > 0) { if (cycles > 0) Out.AppendLine("NOT APPLIED: cycles"); return null; }
int e = 0; var em = new List<string>();
foreach (var kv in toDel) { try { dm.Invoke(Schedule, new object[] { kv.Value, true, false }); } catch (Exception x) { e++; if (em.Count < 5) em.Add("del " + (x.InnerException ?? x).Message); } }
foreach (var kv in toAdd) { try { var p = kv.Key.Split('>'); var d = new Deswik.Scheduler.Engine.Dependency(-1, byId[p[0]].UniqueId, byId[p[1]].UniqueId); d.LinkType = Deswik.Scheduler.API.Contracts.LinkType.FinishStart; double lg = double.Parse(kv.Value.lag.TrimEnd('d'), ci); if (lg != 0) d.Lag = new Deswik.Scheduler.Engine.Lag(lg, Deswik.Scheduler.Engine.TimeUnit.d, null); int ly; if (int.TryParse(kv.Value.layer, out ly)) d.LayerId = ly; Schedule.Add(d, true, true, false); } catch (Exception x) { e++; if (em.Count < 5) em.Add("add " + (x.InnerException ?? x).Message); } }
foreach (var (d, w) in lagList) { try { d.Lag = new Deswik.Scheduler.Engine.Lag(w, Deswik.Scheduler.Engine.TimeUnit.d, null); } catch (Exception x) { e++; if (em.Count < 5) em.Add("lag " + x.Message); } }
Schedule.ScheduleCalculator.CalculateSchedule(true, false); Schedule.ScheduleCalculator.WaitForCalculationsToComplete(false, 300000);
Out.AppendLine("APPLIED  errors " + e + "  deps now " + Schedule.Dependencies.Count(d => !d.Deleted)); foreach (var x in em) Out.AppendLine("  ERR " + x);
return null;
