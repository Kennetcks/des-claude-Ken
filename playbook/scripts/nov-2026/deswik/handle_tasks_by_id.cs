// Read-only (MINE_UG_FY27): for each handle in HS, the figure's ID attribute -> its task (rate, resource, dates) and the
// task's predecessors and successors.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
string FV(Deswik.Scheduler.Engine.Task t, string f) { try { return System.Convert.ToString(t.GetNullaleFieldFullValue(f)); } catch { return ""; } }
string H(Deswik.Graphics.Primaries.Figure f) => System.Convert.ToString(f.GetType().GetProperty("Handle", BF)?.GetValue(f))?.ToUpper();
var HS = new[] { "44B8F38", "44ABDAA", "44ABDAC" };
var byId = Schedule.Tasks.Where(t => !t.Deleted).ToDictionary(t => t.Id);
var byUid = Schedule.Tasks.Where(t => !t.Deleted).ToDictionary(t => System.Convert.ToInt64(t.UniqueId));
string Links(Deswik.Scheduler.Engine.Task t, string prop, string end) => string.Join("; ", ((System.Collections.IEnumerable)t.GetType().GetProperty(prop, BF).GetValue(t)).Cast<object>()
  .Select(o => byUid.TryGetValue(System.Convert.ToInt64(o.GetType().GetProperty(end, BF).GetValue(o)), out var x) ? $"{x.Name} uid={x.UniqueId} {x.Start:dd/MM/yy HH:mm} ({o.GetType().GetProperty("Layer", BF)?.GetValue(o)})" : "?"));
var figs = new Dictionary<string, Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) { var h = H(f); if (h != null && HS.Contains(h)) figs[h] = f; }
foreach (var h in HS) {
  if (!figs.TryGetValue(h, out var fig)) { Out.AppendLine($"== {h}: figure not found"); continue; }
  var id = System.Convert.ToString(fig.AttributeValueGet("ID", ""));
  Out.AppendLine($"== {h} layer={fig.Layer?.Name} deleted={fig.Deleted} Name='{fig.AttributeValueGet("Name", "")}' ID={id}");
  if (!byId.TryGetValue(id, out var t)) { Out.AppendLine("   no task with that ID"); continue; }
  Out.AppendLine($"   TASK {t.Name} uid={t.UniqueId} {FV(t, "MINE")}/{FV(t, "LEVEL")}/{FV(t, "ACT_TYPE")} {t.Start:dd/MM/yy HH:mm}-{t.Finish:dd/MM/yy HH:mm} Rate={FV(t, "Rate")} m={FV(t, "Sch_metres")} {t.ResourceAssignmentDetails}");
  Out.AppendLine($"   preds: {Links(t, "Predecessors", "FromNumeric")}");
  Out.AppendLine($"   succs: {Links(t, "Successors", "ToNumeric")}");
}
return null;
