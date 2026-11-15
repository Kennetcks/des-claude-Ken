// Restore planner inputs from the old tasks onto the matched new tunnel tasks (Replace_Map.tsv).
// Fields: Name, LEVEL, MINE, DIRECTION, DRIVE NUMBER, COST_CENTRE, Rate, Priority, Calendar, Constraint, ConstraintDate, IsMilestone + resource assignment.
// Attribute-type fields are also written on the matching task solid. APPLY=false = dry run. LIMIT caps tasks changed (0 = all).
const bool APPLY = false; const int LIMIT = 0;
var ci = System.Globalization.CultureInfo.InvariantCulture;
string C(object v) => v == null ? "" : (v is IFormattable fm ? fm.ToString(null, ci) : v.ToString());
var tl = System.IO.File.ReadAllLines(@"C:\dswtmp\Schedule_Tasks_20261113_090226.tsv"); var th = tl[0].Split('\t').ToList();
var old = new Dictionary<string, string[]>(); foreach (var l in tl.Skip(1)) { var c = l.Split('\t'); old[c[0]] = c; }
var map = System.IO.File.ReadAllLines(@"C:\dswtmp\Replace_Map.tsv").Skip(1).Select(l => l.Split('\t')).ToList();
var attrF = new[] { "Name", "LEVEL", "MINE", "DIRECTION", "DRIVE NUMBER", "COST_CENTRE" };
var schF = new[] { "Rate", "Priority", "Calendar", "Constraint", "ConstraintDate", "IsMilestone" };
var tasks = Schedule.Tasks.Where(t => !t.Deleted).ToDictionary(t => t.Id);
var cur = Schedule.ExportTaskData(th.Skip(1).ToList()).Select(r => ((IEnumerable<object>)r).Select(C).ToArray()).ToDictionary(r => r[0]);
var solid = new Dictionary<string, Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (!f.Deleted && f.Layer != null && f.Layer.Name == "SCHEDULER\\TASK SOLIDS") solid[(f.AttributeValueGet("DIS Parent ID", "") ?? "") + "|" + (f.AttributeValueGet("DIS Activity Number", "") ?? "")] = f;
var res = new Dictionary<string, Deswik.Scheduler.Engine.Resource>(); foreach (var r in Schedule.GetResources().NonDeletedResources) res[r.Name] = r;
var cnt = new Dictionary<string, int>(); var assignGroups = new Dictionary<string, List<Deswik.Scheduler.Engine.Task>>(); int nTask = 0, errs = 0; var errMsg = new List<string>();
foreach (var m in map)
{
    if (LIMIT > 0 && nTask >= LIMIT) break;
    if (!tasks.TryGetValue(m[0], out var t) || !old.TryGetValue(m[2], out var o) || !cur.TryGetValue(m[0], out var n)) continue;
    bool any = false;
    foreach (var f in attrF.Concat(schF))
    {
        int i = th.IndexOf(f); if (i < 0) continue; var ov = i < o.Length ? o[i] : ""; var nv = i < n.Length ? n[i] : "";
        if (ov == nv || (ov == "" && f != "ConstraintDate")) continue;
        cnt[f] = cnt.TryGetValue(f, out var k) ? k + 1 : 1; any = true;
        if (APPLY) { try { t.SetFieldValue(f, ov, ci); } catch (Exception e) { errs++; if (errMsg.Count < 5) errMsg.Add(f + " " + t.Id + ": " + e.Message); } }
        if (APPLY && attrF.Contains(f) && solid.TryGetValue(m[1], out var s)) s.AttributeValueSet(f, ov);
    }
    var oa = o[th.IndexOf("ResourceAssignmentDetails")]; var na = C(t.ResourceAssignmentDetails);
    if (oa != "" && oa != na) { if (!assignGroups.ContainsKey(oa)) assignGroups[oa] = new List<Deswik.Scheduler.Engine.Task>(); assignGroups[oa].Add(t); any = true; }
    if (any) nTask++;
}
Out.AppendLine((APPLY ? "APPLIED" : "DRY RUN") + "  tasks touched " + nTask + "  errors " + errs);
foreach (var kv in cnt.OrderByDescending(k => k.Value)) Out.AppendLine("  " + kv.Key + "\t" + kv.Value);
foreach (var e in errMsg) Out.AppendLine("  ERR " + e);
int assigned = 0;
foreach (var g in assignGroups)
{
    // e.g. "MA Jumbo #1:E:0.25@400/mo:<None>:<None>"
    var p = g.Key.Split(':'); var rn = p[0]; var type = p[1] == "E" ? Deswik.Scheduler.Engine.ResourceAssignmentType.Effort : p[1] == "R" ? Deswik.Scheduler.Engine.ResourceAssignmentType.Required : Deswik.Scheduler.Engine.ResourceAssignmentType.Driving;
    var u = double.Parse(p[2].Split('@')[0], ci); var rate = p[2].Split('@')[1];
    Out.AppendLine("  ASSIGN " + g.Value.Count + "\t" + g.Key + (res.ContainsKey(rn) ? "" : "  (RESOURCE NOT FOUND)"));
    if (!APPLY || !res.ContainsKey(rn)) continue;
    int ta = 0, ab = 0; string msg = ""; var col = System.Drawing.Color.Empty;
    Schedule.AssignResourcesToTasks(g.Value, new List<Deswik.Scheduler.Engine.Resource> { res[rn] }, type, rate, u, u, p[3], p[4], ref ta, ref ab, ref msg, ref col, true, true);
    assigned += ta; if (ab > 0 || !string.IsNullOrEmpty(msg)) Out.AppendLine("    aborted " + ab + " msg " + msg);
}
if (APPLY) { Schedule.ScheduleCalculator.CalculateSchedule(true, false); Schedule.ScheduleCalculator.WaitForCalculationsToComplete(false, 300000); Out.AppendLine("ASSIGNED " + assigned); }
if (APPLY && LIMIT > 0) foreach (var m in map.Take(LIMIT)) { var t = Schedule.Tasks.First(x => x.Id == m[0]); var o = old[m[2]]; Out.AppendLine("CHECK " + t.Id + " old=" + o[th.IndexOf("Name")] + " / " + o[th.IndexOf("PhysicalResourceAssignmentDetails")] + " / " + o[th.IndexOf("Rate")] + " / pri " + o[th.IndexOf("Priority")] + " / " + o[th.IndexOf("Calendar")] + "  NEW=" + t.Name + " / " + t.PhysicalResourceAssignmentDetails + " / " + C(t.GetFieldValueAndCacheIt("Rate")) + " / pri " + t.Priority + " / " + C(t.GetFieldValueAndCacheIt("Calendar"))); }
return null;
