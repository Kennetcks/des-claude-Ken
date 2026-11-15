// For new tunnel tasks still without a resource: copy planner fields + assignment from the nearest old cut on the same source string (no distance cap).
const bool APPLY = false;
var ci = System.Globalization.CultureInfo.InvariantCulture;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
string C(object v) => v == null ? "" : (v is IFormattable fm ? fm.ToString(null, ci) : v.ToString());
double N(string s) { double v; return double.TryParse(s, System.Globalization.NumberStyles.Float, ci, out v) ? v : double.NaN; }
var sl = System.IO.File.ReadAllLines(@"C:\dswtmp\TaskSolids_Snapshot_20261113_084151.tsv"); var sh = sl[0].Split('\t').ToList();
int sP = sh.IndexOf("DIS Parent ID"), sN = sh.IndexOf("DIS Activity Number"), sT = sh.IndexOf("DIS Activity Type");
var oldBySrc = new Dictionary<string, List<(string key, double x, double y, double z)>>();
foreach (var l in sl.Skip(1)) { var c = l.Split('\t'); if (c[sT] != "Tunnel" || c[3] == "") continue; if (!oldBySrc.ContainsKey(c[sP])) oldBySrc[c[sP]] = new List<(string, double, double, double)>(); oldBySrc[c[sP]].Add((c[sP] + "|" + c[sN], (N(c[3]) + N(c[6])) / 2, (N(c[4]) + N(c[7])) / 2, (N(c[5]) + N(c[8])) / 2)); }
var tl = System.IO.File.ReadAllLines(@"C:\dswtmp\Schedule_Tasks_20261113_090226.tsv"); var th = tl[0].Split('\t').ToList();
int tP = th.IndexOf("DIS Parent ID"), tN = th.IndexOf("DIS Activity Number"), tT = th.IndexOf("DIS Activity Type");
var oldTask = new Dictionary<string, string[]>(); foreach (var l in tl.Skip(1)) { var c = l.Split('\t'); if (c[tT] == "Tunnel") oldTask[c[tP] + "|" + c[tN]] = c; }
var srcOf = new Dictionary<string, string>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (!f.Deleted && f.Layer != null && f.Layer.Name == "0\\06 SITE\\03 ENGINEERING\\01 DEV STRINGS\\COMBINED 12.11") srcOf[f.Handle] = A(f, "SOURCE_STRING");
var tasks = new Dictionary<string, Deswik.Scheduler.Engine.Task>();
foreach (var t in Schedule.Tasks) if (!t.Deleted && t.GetFieldValueAndCacheIt("DIS Activity Type") == "Tunnel" && t.GetFieldValueAndCacheIt("SCHEDULED") == "1" && string.IsNullOrEmpty(t.ResourceAssignmentDetails)) tasks[t.GetFieldValueAndCacheIt("DIS Parent ID") + "|" + t.GetFieldValueAndCacheIt("DIS Activity Number")] = t;
var res = new Dictionary<string, Deswik.Scheduler.Engine.Resource>(); foreach (var r in Schedule.GetResources().NonDeletedResources) res[r.Name] = r;
var fields = new[] { "Name", "LEVEL", "MINE", "DIRECTION", "DRIVE NUMBER", "COST_CENTRE", "Rate", "Priority", "Calendar" };
var groups = new Dictionary<string, List<Deswik.Scheduler.Engine.Task>>(); int done = 0, noDonor = 0; var dist = new List<double>(); var rep = new StringBuilder("NewTask\tNewName\tDonorOld\tDonorName\tDist\tAssign\n");
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || f.Layer.Name != "SCHEDULER\\TASK SOLIDS" || !f.IsPolyface) continue;
    var k = A(f, "DIS Parent ID") + "|" + A(f, "DIS Activity Number"); if (!tasks.TryGetValue(k, out var t)) continue;
    if (!srcOf.TryGetValue(A(f, "DIS Parent ID"), out var src) || !oldBySrc.TryGetValue(src, out var cand)) { noDonor++; continue; }
    var vl = f.asPolyface.GetType().GetProperty("VertexList")?.GetValue(f.asPolyface) as System.Collections.IEnumerable; double x0 = 1e18, y0 = 1e18, z0 = 1e18, x1 = -1e18, y1 = -1e18, z1 = -1e18;
    foreach (var q in vl) { var ty = q.GetType(); double x = Convert.ToDouble(ty.GetProperty("x").GetValue(q)), y = Convert.ToDouble(ty.GetProperty("y").GetValue(q)), z = Convert.ToDouble(ty.GetProperty("z").GetValue(q)); x0 = Math.Min(x0, x); y0 = Math.Min(y0, y); z0 = Math.Min(z0, z); x1 = Math.Max(x1, x); y1 = Math.Max(y1, y); z1 = Math.Max(z1, z); }
    double cx = (x0 + x1) / 2, cy = (y0 + y1) / 2, cz = (z0 + z1) / 2;
    var donor = cand.Where(o => oldTask.ContainsKey(o.key) && oldTask[o.key][th.IndexOf("ResourceAssignmentDetails")] != "").OrderBy(o => (o.x - cx) * (o.x - cx) + (o.y - cy) * (o.y - cy) + (o.z - cz) * (o.z - cz)).FirstOrDefault();
    if (donor.key == null) donor = cand.Where(o => oldTask.ContainsKey(o.key)).OrderBy(o => (o.x - cx) * (o.x - cx) + (o.y - cy) * (o.y - cy) + (o.z - cz) * (o.z - cz)).FirstOrDefault();
    if (donor.key == null) { noDonor++; continue; }
    var d = Math.Sqrt((donor.x - cx) * (donor.x - cx) + (donor.y - cy) * (donor.y - cy) + (donor.z - cz) * (donor.z - cz)); dist.Add(d);
    var o2 = oldTask[donor.key]; var asg = o2[th.IndexOf("ResourceAssignmentDetails")];
    rep.Append(t.Id + "\t" + t.Name + "\t" + o2[0] + "\t" + o2[th.IndexOf("Name")] + "\t" + d.ToString("F1", ci) + "\t" + asg + "\n");
    if (asg != "") { if (!groups.ContainsKey(asg)) groups[asg] = new List<Deswik.Scheduler.Engine.Task>(); groups[asg].Add(t); } done++;
    if (APPLY) foreach (var fn in fields) { var v = o2[th.IndexOf(fn)]; if (v != "") { t.SetFieldValue(fn, v, ci); if (fn == "Name" || fn == "LEVEL" || fn == "MINE" || fn == "DIRECTION" || fn == "DRIVE NUMBER" || fn == "COST_CENTRE") f.AttributeValueSet(fn, v); } }
}
int assigned = 0;
foreach (var g in groups)
{
    var p = g.Key.Split(':'); var type = p[1] == "E" ? Deswik.Scheduler.Engine.ResourceAssignmentType.Effort : p[1] == "R" ? Deswik.Scheduler.Engine.ResourceAssignmentType.Required : Deswik.Scheduler.Engine.ResourceAssignmentType.Driving;
    var u = double.Parse(p[2].Split('@')[0], ci); var rate = p[2].Split('@')[1];
    Out.AppendLine("  ASSIGN " + g.Value.Count + "\t" + g.Key);
    if (!APPLY || !res.ContainsKey(p[0])) continue; int ta = 0, ab = 0; string msg = ""; var col = System.Drawing.Color.Empty;
    Schedule.AssignResourcesToTasks(g.Value, new List<Deswik.Scheduler.Engine.Resource> { res[p[0]] }, type, rate, u, u, p[3], p[4], ref ta, ref ab, ref msg, ref col, true, true); assigned += ta;
}
if (APPLY) { Schedule.ScheduleCalculator.CalculateSchedule(true, false); Schedule.ScheduleCalculator.WaitForCalculationsToComplete(false, 300000); }
System.IO.File.WriteAllText(@"C:\dswtmp\Replace_FillRest" + (APPLY ? "_Applied" : "_DryRun") + ".tsv", rep.ToString(), new UTF8Encoding(true));
Out.AppendLine((APPLY ? "APPLIED" : "DRY RUN") + "  tasks without resource " + tasks.Count + "  filled " + done + "  no donor " + noDonor + "  assigned " + assigned);
if (dist.Count > 0) Out.AppendLine("  donor distance: median " + dist.OrderBy(x => x).ElementAt(dist.Count / 2).ToString("F1", ci) + " m, max " + dist.Max().ToString("F1", ci) + " m, >10 m " + dist.Count(x => x > 10));
return null;
