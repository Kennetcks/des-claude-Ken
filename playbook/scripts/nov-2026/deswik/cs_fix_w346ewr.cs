// W346 EWR - (string 45931DF): links from the drives at its top (W368 EWD W, 45933B9) and bottom (G350 DSP, 458EDDC) into cut #1,
// junction cut = nearest cut centre to the raise's top / bottom cut. Settings copied from the old W346 EWR (snapshot 100751, parent 458EE37).
const bool APPLY = false;
var ci = System.Globalization.CultureInfo.InvariantCulture;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
var cur = Schedule.Tasks.Where(t => !t.Deleted).ToList(); var uid = cur.ToDictionary(t => t.UniqueId);
string F(Deswik.Scheduler.Engine.Task t, string f) => t.GetFieldValueAndCacheIt(f) ?? "";
int No(Deswik.Scheduler.Engine.Task t) { int v; return int.TryParse(F(t, "DIS Activity Number"), out v) ? v : 0; }
List<Deswik.Scheduler.Engine.Task> Cuts(string h) => cur.Where(t => F(t, "DIS Parent ID") == h && F(t, "DIS Activity Type") == "Tunnel").OrderBy(No).ToList();
var cen = new Dictionary<string, double[]>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || f.Layer.Name != "SCHEDULER\\TASK SOLIDS" || !f.IsPolyface) continue; var p = A(f, "DIS Parent ID"); if (p != "45931DF" && p != "45933B9" && p != "458EDDC") continue;
    var vl = f.asPolyface.GetType().GetProperty("VertexList")?.GetValue(f.asPolyface) as System.Collections.IEnumerable; double x0 = 1e18, y0 = 1e18, z0 = 1e18, x1 = -1e18, y1 = -1e18, z1 = -1e18;
    foreach (var q in vl) { var ty = q.GetType(); double x = Convert.ToDouble(ty.GetProperty("x").GetValue(q)), y = Convert.ToDouble(ty.GetProperty("y").GetValue(q)), z = Convert.ToDouble(ty.GetProperty("z").GetValue(q)); x0 = Math.Min(x0, x); y0 = Math.Min(y0, y); z0 = Math.Min(z0, z); x1 = Math.Max(x1, x); y1 = Math.Max(y1, y); z1 = Math.Max(z1, z); }
    cen[p + "|" + A(f, "DIS Activity Number") + "|" + A(f, "DIS Activity Type")] = new[] { (x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2, z0, z1 };
}
double[] C(Deswik.Scheduler.Engine.Task t) => cen[F(t, "DIS Parent ID") + "|" + F(t, "DIS Activity Number") + "|Tunnel"];
double D(double[] a, double x, double y, double z) => Math.Sqrt(Math.Pow(a[0] - x, 2) + Math.Pow(a[1] - y, 2) + Math.Pow(a[2] - z, 2));
var ewr = Cuts("45931DF"); var top = ewr.OrderByDescending(t => C(t)[4]).First(); var bot = ewr.OrderBy(t => C(t)[3]).First();
var tc = C(top); var bc = C(bot);
var jTop = Cuts("45933B9").OrderBy(t => D(C(t), tc[0], tc[1], tc[4])).First(); var jBot = Cuts("458EDDC").OrderBy(t => D(C(t), bc[0], bc[1], bc[3])).First();
var first = ewr.First();
Out.AppendLine("W346 EWR cuts: " + string.Join(", ", ewr.Select(t => "#" + No(t) + " RL " + C(t)[3].ToString("F0") + "-" + C(t)[4].ToString("F0"))) + "  (first cut #" + No(first) + ")");
var have = new HashSet<string>(Schedule.Dependencies.Where(d => !d.Deleted && uid.ContainsKey(d.From) && uid.ContainsKey(d.To)).Select(d => uid[d.From].Id + ">" + uid[d.To].Id));
var adds = new[] { (jTop, "top: " + jTop.Name + " #" + No(jTop)), (jBot, "bottom: " + jBot.Name + " #" + No(jBot)) }.Where(x => !have.Contains(x.Item1.Id + ">" + first.Id)).ToList();
foreach (var a in adds) Out.AppendLine("ADD " + a.Item2 + " -> W346 EWR #" + No(first));
// settings from the old W346 EWR
var tl = System.IO.File.ReadAllLines(@"C:\dswtmp\Schedule_Tasks_20261113_100751.tsv"); var th = tl[0].Split('\t').ToList();
var o = tl.Skip(1).Select(l => l.Split('\t')).First(c => c[th.IndexOf("DIS Parent ID")] == "458EE37" && c[th.IndexOf("DIS Activity Type")] == "Tunnel");
string O(string f) => o[th.IndexOf(f)];
Out.AppendLine("old W346 EWR: rate " + O("TaskRate") + ", priority " + O("Priority") + ", calendar " + O("Calendar") + ", split " + O("LevelingCanSplit") + ", resource '" + O("ResourceAssignmentDetails") + "', constraint " + O("Constraint"));
Out.AppendLine("new now:      rate " + first.TaskRate + ", priority " + first.Priority + ", calendar " + F(first, "Calendar") + ", split " + first.CanSplit + ", resource '" + first.ResourceAssignmentDetails + "'");
if (!APPLY) return null;
foreach (var a in adds) { var d = new Deswik.Scheduler.Engine.Dependency(-1, a.Item1.UniqueId, first.UniqueId); d.LinkType = Deswik.Scheduler.API.Contracts.LinkType.FinishStart; d.LayerId = 1; Schedule.Add(d, true, true, false); }
double pr; double.TryParse(O("Priority"), System.Globalization.NumberStyles.Float, ci, out pr); bool split = O("LevelingCanSplit") == "True";
foreach (var t in ewr) { if (O("TaskRate") != "") t.TaskRate = O("TaskRate"); t.Priority = pr; t.CanSplit = split; if (O("Calendar") != "") t.SetFieldValue("Calendar", O("Calendar"), ci); }
Schedule.ScheduleCalculator.CalculateSchedule(true, false); Schedule.ScheduleCalculator.WaitForCalculationsToComplete(false, 300000);
Out.AppendLine("APPLIED");
return null;
