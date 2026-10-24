if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// READ ONLY: what is 44ABFDE, its tasks, the predecessors of its first tasks (with finish), and the drive it takes off from.
string FV(Deswik.Scheduler.Engine.Task t, string fn) { try { return System.Convert.ToString(t.GetNullaleFieldFullValue(fn)); } catch { return ""; } }
string Line(Deswik.Scheduler.Engine.Task x) => $"{x.Name,-18} {FV(x, "ACT_TYPE"),-12} #{FV(x, "DIS Activity Number"),-3} {x.Id,-26} {x.Start:dd/MM/yy HH:mm} -> {x.Finish:dd/MM/yy HH:mm} prio {x.Priority} {x.Constraint} {(x.Constraint.ToString().Contains("No") ? x.ConstraintDate.ToString("dd/MM/yy") : "")} res {x.ResourceAssignmentDetails}";
void Preds(Deswik.Scheduler.Engine.Task t, string ind) {
  foreach (var part in (t.PredecessorDetails ?? "").Split(';')) { var b = part.Split(':'); if (b.Length < 2) continue; var p = Schedule.GetTask(b[0]);
    Out.AppendLine($"{ind}<- {p?.Name,-18} {FV(p, "ACT_TYPE"),-12} #{FV(p, "DIS Activity Number"),-3} {b[0],-26} {b[1]} lag {(b.Length > 2 ? b[2] : "")} finish {p?.Finish:dd/MM/yy HH:mm}"); } }
Deswik.Graphics.Primaries.Figure fig = null;
foreach (var e in App.ModelSpace.Entities) { var f = e as Deswik.Graphics.Primaries.Figure; if (f != null && !f.Deleted && $"{f.GetType().GetProperty("Handle")?.GetValue(f)}" == "44ABFDE") fig = f; }
if (fig == null) { Out.AppendLine("44ABFDE not found"); return null; }
var id = System.Convert.ToString(fig.AttributeValueGet("ID", ""));
Out.AppendLine($"44ABFDE: polyline={fig.IsPolyline} polyface={fig.IsPolyface} layer {fig.Layer?.Name} ID='{id}' ACT={fig.AttributeValueGet("ACT_TYPE", "")} Name={fig.AttributeValueGet("Name", "")} LEVEL={fig.AttributeValueGet("LEVEL", "")} GUID={fig.AttributeValueGet("GUID", "")}");
var guid = System.Convert.ToString(fig.AttributeValueGet("GUID", ""));
var t0 = id == "" ? null : Schedule.GetTask(id);
var par = t0 != null ? FV(t0, "DIS Parent GUID") : guid;
var chain = Schedule.GetTasks().Where(x => !x.Deleted && par != "" && FV(x, "DIS Parent GUID") == par).OrderBy(x => x.Start).ToList();
Out.AppendLine($"--- {chain.Count} tasks on this string (parent {par}), first 6 with predecessors:");
foreach (var x in chain.Take(6)) { Out.AppendLine(Line(x)); Preds(x, "     "); }
if (chain.Count > 6) Out.AppendLine("   ... last: " + Line(chain[chain.Count - 1]));
// the drive it takes off from: the predecessors' strings, listed around the take-off cut
var firstPreds = new List<Deswik.Scheduler.Engine.Task>();
foreach (var x in chain.Take(3)) foreach (var part in (x.PredecessorDetails ?? "").Split(';')) { var b = part.Split(':'); if (b.Length < 2) continue; var p = Schedule.GetTask(b[0]); if (p != null && FV(p, "DIS Parent GUID") != par) firstPreds.Add(p); }
foreach (var g in firstPreds.GroupBy(p => FV(p, "DIS Parent GUID"))) {
  var drive = Schedule.GetTasks().Where(x => !x.Deleted && FV(x, "DIS Parent GUID") == g.Key).OrderBy(x => x.Start).ToList();
  var k = drive.FindIndex(x => g.Any(p => p.Id == x.Id));
  Out.AppendLine($"--- feeding drive {drive.FirstOrDefault()?.Name} ({drive.Count} tasks), take-off link at index {k}; tasks around it:");
  foreach (var x in drive.Skip(System.Math.Max(0, k - 3)).Take(10)) { Out.AppendLine("   " + Line(x)); Preds(x, "        "); } }
return null;
