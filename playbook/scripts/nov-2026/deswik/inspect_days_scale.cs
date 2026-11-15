if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Read-only: compare the Days and Months time scales and list TimeScaling methods.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
var scales = ((System.Collections.IEnumerable)Schedule.GetType().GetProperty("TimeScales", BF).GetValue(Schedule)).Cast<object>().ToList();
foreach (var i in new[] { 1, 9 }) {
  var ts = scales[i];
  Out.AppendLine($"[{i}] " + ts.ToString().Replace("\n", " | "));
  var items = ts.GetType().GetProperty("Items", BF)?.GetValue(ts) as System.Collections.IEnumerable;
  if (items != null) foreach (var it in items) Out.AppendLine("    item: " + string.Join("; ", it.GetType().GetProperties(BF).Where(p => p.GetIndexParameters().Length == 0).Select(p => { object v = null; try { v = p.GetValue(it); } catch { } return p.Name + "=" + v; })));
}
Out.AppendLine("methods: " + string.Join(", ", scales[9].GetType().GetMethods(BF).Where(m => m.DeclaringType == scales[9].GetType()).Select(m => m.Name + "(" + string.Join(",", m.GetParameters().Select(p => p.ParameterType.Name)) + ")").Distinct()));
return null;
