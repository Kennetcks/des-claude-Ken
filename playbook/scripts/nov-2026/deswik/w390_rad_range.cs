// Read-only (MINE_UG_FY27): figures with handles 44B8F36..44B8F40, each one's task (matched on MINE/LEVEL/ACT_TYPE and
// Start/Finish, as in handles_tasks.cs) and its resource assignment; plus the jumbo resource names.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
string FV(Deswik.Scheduler.Engine.Task t, string f) { try { return System.Convert.ToString(t.GetNullaleFieldFullValue(f)); } catch { return ""; } }
string H(Deswik.Graphics.Primaries.Figure f) => System.Convert.ToString(f.GetType().GetProperty("Handle", BF)?.GetValue(f));
long lo = System.Convert.ToInt64("44B8F36", 16), hi = System.Convert.ToInt64("44B8F40", 16);
if (Schedule != null) Out.AppendLine("jumbos: " + string.Join(", ", ((System.Collections.IEnumerable)Schedule.GetResourceNames()).Cast<object>().Select(x => System.Convert.ToString(x)).Where(n => n.ToUpper().Contains("JUMBO"))));
var figs = new List<(long v, Deswik.Graphics.Primaries.Figure f)>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) { long v; if (long.TryParse(H(f), System.Globalization.NumberStyles.HexNumber, null, out v) && v >= lo && v <= hi) figs.Add((v, f)); }
foreach (var (v, fig) in figs.OrderBy(x => x.v)) {
  string A(string k) => System.Convert.ToString(fig.AttributeValueGet(k, ""));
  Out.AppendLine($"== {H(fig)} deleted={fig.Deleted} {A("MINE")} {A("LEVEL")} {A("ACT_TYPE")} '{A("Name")}' {A("Start")} - {A("Finish")} SCH={A("SCHEDULED")} layer={fig.Layer?.Name}");
  System.DateTime.TryParse(A("Start"), out var st); System.DateTime.TryParse(A("Finish"), out var fi);
  if (Schedule != null) foreach (var t in Schedule.Tasks.Where(t => !t.Deleted && FV(t, "MINE") == A("MINE") && FV(t, "LEVEL") == A("LEVEL") && FV(t, "ACT_TYPE") == A("ACT_TYPE") && System.Math.Abs((t.Start - st).TotalMinutes) < 2 && System.Math.Abs((t.Finish - fi).TotalMinutes) < 2))
    Out.AppendLine($"  TASK {t.Name} [{t.Id}] uid={t.UniqueId} {t.Start:dd/MM/yy HH:mm}-{t.Finish:dd/MM/yy HH:mm} res='{t.PhysicalResource}' det={t.ResourceAssignmentDetails}");
}
Out.AppendLine($"{figs.Count} figures in range");
return null;
