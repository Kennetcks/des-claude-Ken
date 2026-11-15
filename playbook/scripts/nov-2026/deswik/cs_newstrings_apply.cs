// After Create Tunnels on COMBINED 12.11\NEW: map new tasks to the old tasks they replace (Replace_Plan2.tsv), copy settings + resources,
// re-add the old links that touched the removed tasks, and set up brand-new strings off their donor cut.
// Old state = snapshots 20261113_100751 (tasks/deps) and TaskSolids 20261113_100807.
const bool APPLY = false;
var ci = System.Globalization.CultureInfo.InvariantCulture;
string C(object v) => v == null ? "" : (v is IFormattable fm ? fm.ToString(null, ci) : v.ToString());
double N(string s) { double v; return double.TryParse(s, System.Globalization.NumberStyles.Float, ci, out v) ? v : double.NaN; }
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
double D3(double[] a, double[] b) => Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2) + Math.Pow(a[2] - b[2], 2));
// ---- old data
var tl = System.IO.File.ReadAllLines(@"C:\dswtmp\Schedule_Tasks_20261113_100751.tsv"); var th = tl[0].Split('\t').ToList();
string O(string[] r, string f) { int i = th.IndexOf(f); return i < 0 || i >= r.Length ? "" : r[i]; }
var old = new Dictionary<string, string[]>(); var oldByKey = new Dictionary<string, string>();
foreach (var l in tl.Skip(1)) { var c = l.Split('\t'); old[c[0]] = c; if (O(c, "DIS Activity Type") == "Tunnel") oldByKey[O(c, "DIS Parent ID") + "|" + O(c, "DIS Activity Number")] = c[0]; }
var sl = System.IO.File.ReadAllLines(@"C:\dswtmp\TaskSolids_Snapshot_20261113_100807.tsv"); var sh = sl[0].Split('\t').ToList(); int sP = sh.IndexOf("DIS Parent ID"), sN = sh.IndexOf("DIS Activity Number");
var oldCuts = new Dictionary<string, List<(string tid, int no, double[] c)>>();
foreach (var l in sl.Skip(1)) { var c = l.Split('\t'); if (c[3] == "") continue; var k = c[sP] + "|" + c[sN]; if (!oldByKey.TryGetValue(k, out var tid)) continue; if (!oldCuts.ContainsKey(c[sP])) oldCuts[c[sP]] = new List<(string, int, double[])>(); oldCuts[c[sP]].Add((tid, int.Parse(c[sN]), new[] { (N(c[3]) + N(c[6])) / 2, (N(c[4]) + N(c[7])) / 2, (N(c[5]) + N(c[8])) / 2 })); }
// old tasks without a solid: add by activity number only (no position)
foreach (var kv in oldByKey) { var p = kv.Key.Split('|'); if (!oldCuts.ContainsKey(p[0])) oldCuts[p[0]] = new List<(string, int, double[])>(); if (!oldCuts[p[0]].Any(x => x.tid == kv.Value)) oldCuts[p[0]].Add((kv.Value, int.Parse(p[1]), null)); }
var plan = System.IO.File.ReadAllLines(@"C:\dswtmp\Replace_Plan2.tsv").Skip(1).Select(l => l.Split('\t')).ToList();
// ---- current
var cur = Schedule.Tasks.Where(t => !t.Deleted).ToList(); var byId = cur.ToDictionary(t => t.Id); var uid = cur.ToDictionary(t => t.UniqueId, t => t.Id);
string F(Deswik.Scheduler.Engine.Task t, string f) => t.GetFieldValueAndCacheIt(f) ?? "";
int No(Deswik.Scheduler.Engine.Task t) { int v; return int.TryParse(F(t, "DIS Activity Number"), out v) ? v : 0; }
var solC = new Dictionary<string, double[]>(); // parent|no -> centre
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || f.Layer.Name != "SCHEDULER\\TASK SOLIDS" || !f.IsPolyface) continue; var p = A(f, "DIS Parent ID"); if (!plan.Any(x => x[0] == p)) continue;
    var vl = f.asPolyface.GetType().GetProperty("VertexList")?.GetValue(f.asPolyface) as System.Collections.IEnumerable; double x0 = 1e18, y0 = 1e18, z0 = 1e18, x1 = -1e18, y1 = -1e18, z1 = -1e18;
    foreach (var q in vl) { var ty = q.GetType(); double x = Convert.ToDouble(ty.GetProperty("x").GetValue(q)), y = Convert.ToDouble(ty.GetProperty("y").GetValue(q)), z = Convert.ToDouble(ty.GetProperty("z").GetValue(q)); x0 = Math.Min(x0, x); y0 = Math.Min(y0, y); z0 = Math.Min(z0, z); x1 = Math.Max(x1, x); y1 = Math.Max(y1, y); z1 = Math.Max(z1, z); }
    solC[p + "|" + A(f, "DIS Activity Number")] = new[] { (x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2 };
}
var n2o = new Dictionary<string, string>(); var o2n = new Dictionary<string, List<string>>(); var newTasks = new HashSet<string>(); var report = new List<string>(); var existingTargets = new HashSet<string>();
void O2N(string o, string n) { if (!o2n.ContainsKey(o)) o2n[o] = new List<string>(); if (!o2n[o].Contains(n)) o2n[o].Add(n); }
foreach (var pl in plan)
{
    var nts = cur.Where(t => F(t, "DIS Parent ID") == pl[0] && F(t, "DIS Activity Type") == "Tunnel").OrderBy(No).ToList();
    if (nts.Count == 0) { report.Add("NO TASKS CREATED for " + pl[0]); continue; }
    if (pl[3] == "existing")
    {   // existing heading keeps its tasks/settings/links; old cuts map onto it only as predecessors (outgoing links)
        foreach (var p in pl[1].Split(',')) if (oldCuts.TryGetValue(p, out var ocs)) foreach (var oc in ocs) { O2N(oc.tid, nts.Last().Id); existingTargets.Add(oc.tid); }
        report.Add(pl[0] + " " + nts[0].Name + ": existing heading, takes over outgoing links of " + pl[1]); continue;
    }
    foreach (var t in nts) newTasks.Add(t.Id);
    if (pl.Length > 4 && pl[4] != "") foreach (var p in pl[4].Split(',')) if (oldCuts.TryGetValue(p, out var ocs2)) foreach (var oc in ocs2) { O2N(oc.tid, nts.Last().Id); existingTargets.Add(oc.tid); }
    if (pl[3] == "new") { report.Add(pl[0] + " " + nts[0].Name + ": NEW, " + nts.Count + " cuts"); continue; }
    var parents = pl[1].Split(','); var oc0 = oldCuts.TryGetValue(parents[0], out var x0l) ? x0l.OrderBy(x => x.no).ToList() : new List<(string tid, int no, double[] c)>();
    for (int i = 0; i < nts.Count; i++)
    {
        var t = nts[i]; string o = null;
        if (pl[3] == "geom") { solC.TryGetValue(pl[0] + "|" + No(t), out var c); var cand = parents.SelectMany(p => oldCuts.TryGetValue(p, out var l) ? l : new List<(string tid, int no, double[] c)>()).Where(x => x.c != null).ToList(); if (c != null && cand.Count > 0) o = cand.OrderBy(x => D3(x.c, c)).First().tid; }
        if (o == null && oc0.Count > 0) o = oc0[nts.Count == 1 ? 0 : (int)Math.Round(i * (oc0.Count - 1) / (double)(nts.Count - 1))].tid;
        if (o != null) n2o[t.Id] = o;
    }
    // every old cut of every replaced parent -> a new cut (for links)
    foreach (var p in parents) { if (!oldCuts.TryGetValue(p, out var ocs)) continue; var oc = ocs.OrderBy(x => x.no).ToList();
        for (int j = 0; j < oc.Count; j++) { string n = null;
            if (pl[3] == "geom" && oc[j].c != null) { var best = nts.Select(t => (t, d: solC.TryGetValue(pl[0] + "|" + No(t), out var c) ? D3(c, oc[j].c) : 1e9)).OrderBy(z => z.d).First(); if (best.d < 1e8) n = best.t.Id; }
            if (n == null) n = nts[oc.Count == 1 ? 0 : (int)Math.Round(j * (nts.Count - 1) / (double)(oc.Count - 1))].Id;
            O2N(oc[j].tid, n); } }
    report.Add(pl[0] + " " + nts[0].Name + ": " + pl[3] + " replace of " + pl[1] + "  new cuts " + nts.Count + ", old cuts " + parents.Sum(p => oldCuts.TryGetValue(p, out var l) ? l.Count : 0));
}
// derived: old tunnel's derived -> new tunnel's derived of same ACT_TYPE
char[] sep = { ',', ';', '|', ' ' };
foreach (var nid in newTasks.ToList())
{
    if (!n2o.TryGetValue(nid, out var oid)) continue; var nds = F(byId[nid], "DerivedTasksDetails").Split(sep, StringSplitOptions.RemoveEmptyEntries).Where(byId.ContainsKey).ToList();
    var ods = O(old[oid], "DerivedTasksDetails").Split(sep, StringSplitOptions.RemoveEmptyEntries).Where(old.ContainsKey).ToList();
    foreach (var nd in nds) { newTasks.Add(nd); var od = ods.FirstOrDefault(x => O(old[x], "ACT_TYPE") == F(byId[nd], "ACT_TYPE")); if (od != null) { n2o[nd] = od; O2N(od, nd); } }
}
// also derived of any old parent cut not chosen as n2o target
foreach (var o in o2n.Keys.ToList()) { var ods = O(old[o], "DerivedTasksDetails").Split(sep, StringSplitOptions.RemoveEmptyEntries).Where(old.ContainsKey); foreach (var od in ods) { if (o2n.ContainsKey(od)) continue; var tgt = o2n[o].SelectMany(n => F(byId[n], "DerivedTasksDetails").Split(sep, StringSplitOptions.RemoveEmptyEntries)).Where(byId.ContainsKey).FirstOrDefault(nd => F(byId[nd], "ACT_TYPE") == O(old[od], "ACT_TYPE")); if (tgt != null) O2N(od, tgt); } }
foreach (var r in report) Out.AppendLine(r);
Out.AppendLine("new tasks " + newTasks.Count + "  mapped to an old task " + n2o.Count + "  old tasks mapped " + o2n.Count);
// ---- settings
var res = new Dictionary<string, Deswik.Scheduler.Engine.Resource>(); foreach (var r in Schedule.GetResources().NonDeletedResources) res[r.Name] = r;
string Norm(string s) => string.Join(";", s.Replace("{", "").Replace("}", "").Split(';').Select(x => x.Trim()).Where(x => x != "").Distinct().OrderBy(x => x));
var cnt = new Dictionary<string, int>(); void Inc(string k) => cnt[k] = cnt.TryGetValue(k, out var n) ? n + 1 : 1;
var groups = new Dictionary<string, List<Deswik.Scheduler.Engine.Task>>(); var post = new List<Action>();
void CopyFrom(Deswik.Scheduler.Engine.Task t, string[] o, bool names)
{
    bool split = O(o, "LevelingCanSplit") == "True", lev = O(o, "TaskShouldBeLeveled") != "False";
    var on = Norm(O(o, "ResourceAssignmentDetails")); if (on != Norm(C(t.ResourceAssignmentDetails))) { Inc("resource"); if (APPLY) t.RemoveAllResourceAssignments(); foreach (var a in on.Split(';').Where(x => x != "")) { var key = a + "|" + split + "|" + lev; if (!groups.ContainsKey(key)) groups[key] = new List<Deswik.Scheduler.Engine.Task>(); groups[key].Add(t); } }
    var orate = O(o, "TaskRate"); if (orate != "" && orate != C(t.TaskRate)) { Inc("TaskRate"); if (APPLY) post.Add(() => t.TaskRate = orate); }
    if (t.CanSplit != split) { Inc("CanSplit"); if (APPLY) post.Add(() => t.CanSplit = split); }
    double op; if (double.TryParse(O(o, "Priority"), System.Globalization.NumberStyles.Float, ci, out op) && Math.Abs(op - t.Priority) > 1e-9) { Inc("Priority"); if (APPLY) post.Add(() => t.Priority = op); }
    var ocal = O(o, "Calendar"); if (ocal != "" && !ocal.Contains("{") && ocal != F(t, "Calendar")) { Inc("Calendar"); if (APPLY) post.Add(() => t.SetFieldValue("Calendar", ocal, ci)); }
    var oc = O(o, "Constraint").Replace(" ", ""); Deswik.Scheduler.API.Contracts.TaskConstraintType ct; if (oc != "" && Enum.TryParse(oc, true, out ct) && ct != t.ConstraintType) { Inc("Constraint"); DateTime cd; bool hd = DateTime.TryParse(O(o, "ConstraintDate"), ci, System.Globalization.DateTimeStyles.None, out cd); if (APPLY) post.Add(() => { t.ConstraintType = ct; if (hd) t.ConstraintDate = cd; }); }
    if (names) foreach (var fn in new[] { "Name", "LEVEL", "MINE", "DIRECTION", "DRIVE NUMBER", "COST_CENTRE" }) { var v = O(o, fn); if (v != "" && v != F(t, fn)) { Inc(fn); if (APPLY) post.Add(() => t.SetFieldValue(fn, v, ci)); } }
}
foreach (var kv in n2o) CopyFrom(byId[kv.Key], old[kv.Value], F(byId[kv.Key], "DIS Activity Type") == "Tunnel");
// brand-new strings: settings from the donor cut's task, link donor -> first cut
var want = new List<(string a, string b, string lag, string layer, string src)>();
foreach (var pl in plan.Where(p => p[3] == "new"))
{
    var nts = cur.Where(t => F(t, "DIS Parent ID") == pl[0] && F(t, "DIS Activity Type") == "Tunnel").OrderBy(No).ToList(); if (nts.Count == 0) continue;
    // donor solid -> its task
    var donorSolid = pl[2]; string donorTask = null;
    foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (!f.Deleted && f.Handle == donorSolid) { var k = A(f, "DIS Parent ID") + "|" + A(f, "DIS Activity Number"); donorTask = cur.FirstOrDefault(t => F(t, "DIS Parent ID") + "|" + F(t, "DIS Activity Number") == k)?.Id; }
    if (donorTask == null) { Out.AppendLine("  NEW " + nts[0].Name + ": donor task not found"); continue; }
    // settings: take resource/calendar from the donor, priority/rate from the most common of same ACT_TYPE + MINE
    var at = F(nts[0], "ACT_TYPE"); var mine = F(nts[0], "MINE");
    var peers = old.Values.Where(r => O(r, "ACT_TYPE") == at && O(r, "MINE") == mine && O(r, "DIS Activity Type") == "Tunnel").ToList();
    if (peers.Count == 0) peers = old.Values.Where(r => O(r, "ACT_TYPE") == at && O(r, "DIS Activity Type") == "Tunnel").ToList();
    Out.AppendLine("  NEW " + nts[0].Name + ": MINE '" + mine + "', " + peers.Count + " " + at + " peers");
    string Common(string f) => peers.GroupBy(r => O(r, f)).OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault() ?? "";
    var donorRow = old.TryGetValue(donorTask, out var dr) ? (string[])dr.Clone() : null;
    if (donorRow != null) { int ip = th.IndexOf("Priority"), ir = th.IndexOf("TaskRate"); if (Common("Priority") != "") donorRow[ip] = Common("Priority"); if (Common("TaskRate") != "") donorRow[ir] = Common("TaskRate"); foreach (var t in nts) CopyFrom(t, donorRow, false); }
    want.Add((donorTask, nts[0].Id, "0d", "1", "new-start"));
    Out.AppendLine("  NEW " + nts[0].Name + ": pred " + byId[donorTask].Name + " (" + donorTask + "), priority " + Common("Priority") + ", rate " + Common("TaskRate") + ", resource from donor " + (donorRow == null ? "?" : O(donorRow, "ResourceAssignmentDetails")));
}
// ---- links: old links touching removed tasks, mapped
var dl = System.IO.File.ReadAllLines(@"C:\dswtmp\Schedule_Dependencies_20261113_100751.tsv").Skip(1).Select(l => l.Split('\t')).ToList();
string Last(string o) => o2n[o].OrderBy(n => No(byId[n])).Last(); string First(string o) => o2n[o].OrderBy(n => No(byId[n])).First();
string Map(string o, bool pred) => byId.ContainsKey(o) ? o : (!pred && existingTargets.Contains(o)) ? null : o2n.ContainsKey(o) ? (pred ? Last(o) : First(o)) : null;
int lost = 0;
foreach (var c in dl) { bool touched = !byId.ContainsKey(c[1]) || !byId.ContainsKey(c[6]); if (!touched) continue; var a = Map(c[1], true); var b = Map(c[6], false); if (a == null || b == null) { lost++; Out.AppendLine("  LOST " + O(old[c[1]], "Name") + " (" + O(old[c[1]], "DIS Activity Type") + (a == null ? ", no new task" : "") + ") -> " + O(old[c[6]], "Name") + " (" + O(old[c[6]], "DIS Activity Type") + (b == null ? (existingTargets.Contains(c[6]) ? ", into old W346 EWD - dropped by design" : ", no new task") : "") + ")"); continue; } if (a != b) want.Add((a, b, c[11], c[12], "old")); }
// chains in new strings
foreach (var g in cur.Where(t => newTasks.Contains(t.Id) && F(t, "DIS Activity Type") == "Tunnel").GroupBy(t => F(t, "DIS Parent ID"))) { var s = g.OrderBy(No).ToList(); for (int i = 1; i < s.Count; i++) want.Add((s[i - 1].Id, s[i].Id, "0d", "1", "chain")); }
var wantK = new Dictionary<string, (string lag, string layer, string src)>(); foreach (var w in want) { var k = w.a + ">" + w.b; if (!wantK.ContainsKey(k)) wantK[k] = (w.lag, w.layer, w.src); }
var curDeps = Schedule.Dependencies.Where(d => !d.Deleted && uid.ContainsKey(d.From) && uid.ContainsKey(d.To)).ToList(); var have = new Dictionary<string, Deswik.Scheduler.Engine.Dependency>(); foreach (var d in curDeps) have[uid[d.From] + ">" + uid[d.To]] = d;
// delete only links on new tasks that aren't wanted
var toDel = have.Where(kv => { var p = kv.Key.Split('>'); return (newTasks.Contains(p[0]) || newTasks.Contains(p[1])) && !wantK.ContainsKey(kv.Key); }).ToList();
var toAdd = wantK.Where(kv => !have.ContainsKey(kv.Key)).ToList();
// loop check
var g2 = new Dictionary<string, HashSet<string>>(); foreach (var k in have.Keys.Where(k => !toDel.Any(d => d.Key == k)).Concat(toAdd.Select(a => a.Key))) { var p = k.Split('>'); if (!g2.ContainsKey(p[0])) g2[p[0]] = new HashSet<string>(); g2[p[0]].Add(p[1]); }
bool Reach(string a, string b) { var st = new Stack<string>(); var seen = new HashSet<string>(); st.Push(a); while (st.Count > 0) { var x = st.Pop(); if (x == b) return true; if (!seen.Add(x) || !g2.TryGetValue(x, out var ns)) continue; foreach (var n in ns) st.Push(n); } return false; }
var loops = toAdd.Where(kv => { var p = kv.Key.Split('>'); g2[p[0]].Remove(p[1]); bool r = Reach(p[1], p[0]); g2[p[0]].Add(p[1]); return r; }).ToList();
Out.AppendLine("links: old touching removed " + dl.Count(c => !byId.ContainsKey(c[1]) || !byId.ContainsKey(c[6])) + " (lost " + lost + ")  to add " + toAdd.Count + "  to delete " + toDel.Count + "  would loop " + loops.Count);
foreach (var l in loops.Take(10)) { var p = l.Key.Split('>'); Out.AppendLine("  LOOP " + byId[p[0]].Name + " -> " + byId[p[1]].Name + " (" + l.Value.src + ")"); }
foreach (var g in toAdd.GroupBy(k => k.Value.src)) Out.AppendLine("  ADD " + g.Key + " " + g.Count());
foreach (var kv in cnt.OrderByDescending(k => k.Value)) Out.AppendLine("  SET " + kv.Key + " " + kv.Value);
foreach (var g in groups) Out.AppendLine("  ASSIGN " + g.Value.Count + " " + g.Key);
if (!APPLY) return null;
if (loops.Count > 0) { Out.AppendLine("NOT APPLIED: loops"); return null; }
int assigned = 0; foreach (var g in groups) { var parts = g.Key.Split('|'); var p = parts[0].Split(':'); var type = p[1] == "E" ? Deswik.Scheduler.Engine.ResourceAssignmentType.Effort : p[1] == "R" ? Deswik.Scheduler.Engine.ResourceAssignmentType.Required : Deswik.Scheduler.Engine.ResourceAssignmentType.Driving; if (!res.ContainsKey(p[0])) continue; int ta = 0, ab = 0; string msg = ""; var col = System.Drawing.Color.Empty; Schedule.AssignResourcesToTasks(g.Value, new List<Deswik.Scheduler.Engine.Resource> { res[p[0]] }, type, p[2].Split('@')[1], double.Parse(p[2].Split('@')[0], ci), double.Parse(p[2].Split('@')[0], ci), p[3], p[4], ref ta, ref ab, ref msg, ref col, parts[1] == "True", parts[2] == "True"); assigned += ta; }
int e = 0; foreach (var a in post) { try { a(); } catch { e++; } }
var dm = Schedule.GetType().GetMethod("Delete", new[] { typeof(Deswik.Scheduler.Engine.Dependency), typeof(bool), typeof(bool) });
foreach (var kv in toDel) { try { dm.Invoke(Schedule, new object[] { kv.Value, true, false }); } catch { e++; } }
foreach (var kv in toAdd) { try { var p = kv.Key.Split('>'); var d = new Deswik.Scheduler.Engine.Dependency(-1, byId[p[0]].UniqueId, byId[p[1]].UniqueId); d.LinkType = Deswik.Scheduler.API.Contracts.LinkType.FinishStart; double lg = double.Parse(kv.Value.lag.TrimEnd('d'), ci); if (lg != 0) d.Lag = new Deswik.Scheduler.Engine.Lag(lg, Deswik.Scheduler.Engine.TimeUnit.d, null); int ly; if (int.TryParse(kv.Value.layer, out ly)) d.LayerId = ly; Schedule.Add(d, true, true, false); } catch { e++; } }
Schedule.ScheduleCalculator.CalculateSchedule(true, false); Schedule.ScheduleCalculator.WaitForCalculationsToComplete(false, 300000);
Out.AppendLine("APPLIED  assigned " + assigned + "  errors " + e + "  tasks " + Schedule.Tasks.Count(t => !t.Deleted) + "  deps " + Schedule.Dependencies.Count(d => !d.Deleted));
return null;
