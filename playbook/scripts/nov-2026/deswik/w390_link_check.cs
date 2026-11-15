// Read-only (MINE_UG_FY27): every attribute on the 44B8F36..44B8F4C task graphics whose name or value looks like a task link
// (Id / Unique / Task / Resource), the currently selected figures, and which task each figure's id field points at.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
string H(Deswik.Graphics.Primaries.Figure f) => System.Convert.ToString(f.GetType().GetProperty("Handle", BF)?.GetValue(f));
long lo = System.Convert.ToInt64("44B8F36", 16), hi = System.Convert.ToInt64("44B8F4C", 16);
var ids = Schedule.Tasks.Where(t => !t.Deleted).ToDictionary(t => t.Id);
bool first = true;
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) {
  long v; if (!long.TryParse(H(f), System.Globalization.NumberStyles.HexNumber, null, out v) || v < lo || v > hi || f.Layer?.Name != "SCHEDULER\\TASK SOLIDS") continue;
  var names = f.Layer.Attributes.Cast<Deswik.Common.Core.Objects.Attribute>().Select(a => a.Name).ToList();
  if (first) { Out.AppendLine("all fields on TASK SOLIDS: " + string.Join(", ", names)); first = false; }
  var pick = names.Where(n => System.Text.RegularExpressions.Regex.IsMatch(n, "ID|Id|UNIQUE|Unique|TASK|Task|Resource|RESOURCE|Start|Finish")).ToList();
  Out.AppendLine($"== {H(f)}: " + string.Join(" | ", pick.Select(n => $"{n}={f.AttributeValueGet(n, "")}")));
  foreach (var n in pick) { var val = System.Convert.ToString(f.AttributeValueGet(n, "")); if (ids.TryGetValue(val, out var t)) Out.AppendLine($"   {n} -> task uid={t.UniqueId} {t.Start:dd/MM/yy HH:mm} res={t.PhysicalResource} det={t.ResourceAssignmentDetails}"); }
}
return null;
