if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Read-only: find report / indicator definitions held by the schedule.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
foreach (var p in Schedule.GetType().GetProperties(BF)) {
  if (!System.Text.RegularExpressions.Regex.IsMatch(p.Name, "(?i)report|indicator|pivot|summary|period")) continue;
  object v = null; try { v = p.GetValue(Schedule); } catch { }
  var cnt = v is System.Collections.ICollection c ? $" count={c.Count}" : "";
  Out.AppendLine($"P {p.Name} : {p.PropertyType.FullName}{cnt}");
  if (v is System.Collections.IEnumerable en && !(v is string)) { int k = 0; foreach (var it in en) { if (k++ >= 40) break; var nm = it?.GetType().GetProperty("Name", BF)?.GetValue(it); Out.AppendLine($"    - {it?.GetType().Name}: {nm}"); } }
}
foreach (var m in Schedule.GetType().GetMethods(BF).Where(m => System.Text.RegularExpressions.Regex.IsMatch(m.Name, "(?i)report|indicator|pivot")).Select(m => m.Name + "(" + string.Join(", ", m.GetParameters().Select(x => x.ParameterType.Name)) + ")").Distinct())
  Out.AppendLine("M " + m);
return null;
