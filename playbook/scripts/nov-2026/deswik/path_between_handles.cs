// Read-only (MINE_UG_FY27): tasks of handles A and B (via the graphic's ID), and the successor path between them (either way).
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
string FV(Deswik.Scheduler.Engine.Task t, string f) { try { return System.Convert.ToString(t.GetNullaleFieldFullValue(f)); } catch { return ""; } }
string H(Deswik.Graphics.Primaries.Figure f) => System.Convert.ToString(f.GetType().GetProperty("Handle", BF)?.GetValue(f))?.ToUpper();
const string A = "44B8F32", B = "44CC6B3";
var byId = Schedule.Tasks.Where(t => !t.Deleted).ToDictionary(t => t.Id);
var byUid = Schedule.Tasks.Where(t => !t.Deleted).ToDictionary(t => System.Convert.ToInt64(t.UniqueId));
var ends = new Dictionary<string, Deswik.Scheduler.Engine.Task>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) {
  var h = H(f); if (f.Deleted || (h != A && h != B)) continue;
  var id = System.Convert.ToString(f.AttributeValueGet("ID", ""));
  Out.AppendLine($"{h} {f.Layer?.Name} Name={f.AttributeValueGet("Name", "")} parent={f.AttributeValueGet("DIS Parent ID", "")} ID={id}");
  if (byId.TryGetValue(id, out var t)) ends[h] = t;
}
if (ends.Count < 2) { Out.AppendLine("one or both handles have no task"); return null; }
IEnumerable<Deswik.Scheduler.Engine.Task> Succ(Deswik.Scheduler.Engine.Task t) {
  foreach (var o in (System.Collections.IEnumerable)t.GetType().GetProperty("Successors", BF).GetValue(t)) {
    var to = System.Convert.ToInt64(o.GetType().GetProperty("ToNumeric", BF).GetValue(o)); if (byUid.ContainsKey(to)) yield return byUid[to];
  }
}
List<Deswik.Scheduler.Engine.Task> Path(Deswik.Scheduler.Engine.Task s0, Deswik.Scheduler.Engine.Task e0) {
  var prev = new Dictionary<string, string> { [s0.Id] = null }; var q = new Queue<string>(); q.Enqueue(s0.Id);
  while (q.Count > 0 && !prev.ContainsKey(e0.Id)) { var id = q.Dequeue(); foreach (var s in Succ(byId[id])) if (!prev.ContainsKey(s.Id)) { prev[s.Id] = id; q.Enqueue(s.Id); } }
  if (!prev.ContainsKey(e0.Id)) return null;
  var p = new List<Deswik.Scheduler.Engine.Task>(); for (var id = e0.Id; id != null; id = prev[id]) p.Insert(0, byId[id]); return p;
}
var path = Path(ends[A], ends[B]) ?? Path(ends[B], ends[A]);
if (path == null) { Out.AppendLine("no successor path between them"); return null; }
foreach (var t in path) Out.AppendLine($"  uid={t.UniqueId} {t.Name} {t.Start:dd/MM/yy HH:mm}-{t.Finish:dd/MM/yy HH:mm} Rate={FV(t, "Rate")} m={FV(t, "Sch_metres")} {t.ResourceAssignmentDetails}");
Out.AppendLine($"{path.Count} tasks on the path");
return null;
