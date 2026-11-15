// Restore task settings from old tasks onto mapped new tunnel + derived tasks (Map2.tsv):
// resource assignments (exact, no splitting unless old allowed it), TaskRate, CanSplit, Priority, Calendar, Constraint + date, IsBookmarked.
const bool APPLY = false;
var ci = System.Globalization.CultureInfo.InvariantCulture;
string C(object v) => v == null ? "" : (v is IFormattable fm ? fm.ToString(null, ci) : v.ToString());
var tl = System.IO.File.ReadAllLines(@"C:\dswtmp\Schedule_Tasks_20261113_090226.tsv"); var th = tl[0].Split('\t').ToList();
var old = new Dictionary<string, string[]>(); foreach (var l in tl.Skip(1)) { var c = l.Split('\t'); old[c[0]] = c; }
string O(string[] r, string f) { int i = th.IndexOf(f); return i < 0 || i >= r.Length ? "" : r[i]; }
var map = System.IO.File.ReadAllLines(@"C:\dswtmp\Map2.tsv").Skip(1).Select(l => l.Split('\t')).Where(c => c[2] != "same").ToList();
var cur = Schedule.Tasks.Where(t => !t.Deleted).ToDictionary(t => t.Id);
var res = new Dictionary<string, Deswik.Scheduler.Engine.Resource>(); foreach (var r in Schedule.GetResources().NonDeletedResources) res[r.Name] = r;
string Norm(string s) => string.Join(";", s.Replace("{", "").Replace("}", "").Split(';').Select(x => x.Trim()).Where(x => x != "").Distinct().OrderBy(x => x));
var cnt = new Dictionary<string, int>(); void Inc(string k) => cnt[k] = cnt.TryGetValue(k, out var n) ? n + 1 : 1;
var groups = new Dictionary<string, List<Deswik.Scheduler.Engine.Task>>(); var post = new List<Action>(); int errs = 0; var em = new List<string>();
foreach (var m in map)
{
    if (!cur.TryGetValue(m[0], out var t) || !old.TryGetValue(m[1], out var o)) continue;
    // resources
    var on = Norm(O(o, "ResourceAssignmentDetails")); var nn = Norm(C(t.ResourceAssignmentDetails));
    bool split = O(o, "LevelingCanSplit") == "True", lev = O(o, "TaskShouldBeLeveled") != "False";
    if (on != nn)
    {
        Inc("resource");
        if (APPLY) t.RemoveAllResourceAssignments();
        foreach (var a in on.Split(';').Where(x => x != "")) { var key = a + "|" + split + "|" + lev; if (!groups.ContainsKey(key)) groups[key] = new List<Deswik.Scheduler.Engine.Task>(); groups[key].Add(t); }
    }
    // TaskRate
    var orate = O(o, "TaskRate"); if (orate != "" && orate != C(t.TaskRate)) { Inc("TaskRate"); if (APPLY) post.Add(() => t.TaskRate = orate); }
    // CanSplit
    if (t.CanSplit != split) { Inc("CanSplit"); if (APPLY) post.Add(() => t.CanSplit = split); }
    // Priority
    double op; if (double.TryParse(O(o, "Priority"), System.Globalization.NumberStyles.Float, ci, out op) && Math.Abs(op - t.Priority) > 1e-9) { Inc("Priority"); if (APPLY) post.Add(() => t.Priority = op); }
    // Calendar
    var ocal = O(o, "Calendar"); if (ocal != "" && ocal != C(t.GetFieldValueAndCacheIt("Calendar"))) { Inc("Calendar"); if (APPLY) post.Add(() => t.SetFieldValue("Calendar", ocal, ci)); }
    // Constraint
    var oc = O(o, "Constraint").Replace(" ", ""); Deswik.Scheduler.API.Contracts.TaskConstraintType ct;
    if (oc != "" && Enum.TryParse(oc, true, out ct) && ct != t.ConstraintType) { Inc("Constraint"); var od = O(o, "ConstraintDate"); DateTime cd; bool hasD = DateTime.TryParse(od, ci, System.Globalization.DateTimeStyles.None, out cd); if (APPLY) post.Add(() => { t.ConstraintType = ct; if (hasD) t.ConstraintDate = cd; }); }
    // Bookmark
    var ob = O(o, "IsBookmarked") == "True"; if (ob != t.IsBookmarked) { Inc("IsBookmarked"); if (APPLY) post.Add(() => t.IsBookmarked = ob); }
}
int assigned = 0;
foreach (var g in groups)
{
    var parts = g.Key.Split('|'); var p = parts[0].Split(':'); bool split = parts[1] == "True", lev = parts[2] == "True";
    var type = p[1] == "E" ? Deswik.Scheduler.Engine.ResourceAssignmentType.Effort : p[1] == "R" ? Deswik.Scheduler.Engine.ResourceAssignmentType.Required : Deswik.Scheduler.Engine.ResourceAssignmentType.Driving;
    var u = double.Parse(p[2].Split('@')[0], ci); var rate = p[2].Split('@')[1];
    Out.AppendLine("  ASSIGN " + g.Value.Count + "\t" + g.Key + (res.ContainsKey(p[0]) ? "" : "  RESOURCE NOT FOUND"));
    if (!APPLY || !res.ContainsKey(p[0])) continue; int ta = 0, ab = 0; string msg = ""; var col = System.Drawing.Color.Empty;
    Schedule.AssignResourcesToTasks(g.Value, new List<Deswik.Scheduler.Engine.Resource> { res[p[0]] }, type, rate, u, u, p[3], p[4], ref ta, ref ab, ref msg, ref col, split, lev); assigned += ta;
    if (ab > 0) Out.AppendLine("    aborted " + ab + " " + msg);
}
foreach (var a in post) { try { a(); } catch (Exception e) { errs++; if (em.Count < 5) em.Add(e.Message); } }
if (APPLY) { Schedule.ScheduleCalculator.CalculateSchedule(true, false); Schedule.ScheduleCalculator.WaitForCalculationsToComplete(false, 300000); }
Out.AppendLine((APPLY ? "APPLIED" : "DRY RUN") + "  mapped tasks " + map.Count + "  assigned " + assigned + "  errors " + errs);
foreach (var kv in cnt.OrderByDescending(k => k.Value)) Out.AppendLine("  " + kv.Key + "\t" + kv.Value);
foreach (var e in em) Out.AppendLine("  ERR " + e);
return null;
