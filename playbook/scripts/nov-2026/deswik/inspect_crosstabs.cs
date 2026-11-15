if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Read-only: list cross-tab report views and report-data methods that could produce or export their values.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
var rd = Schedule.GetType().GetProperty("reportData", BF).GetValue(Schedule);
foreach (var m in rd.GetType().GetMethods(BF).Where(m => System.Text.RegularExpressions.Regex.IsMatch(m.Name, "(?i)export|calc|build|generate|crosstab|refresh|update|getdata|rows")).Select(m => m.Name + "(" + string.Join(", ", m.GetParameters().Select(x => x.ParameterType.Name + " " + x.Name)) + ") : " + m.ReturnType.Name).Distinct())
  Out.AppendLine("M " + m);
foreach (var f in rd.GetType().GetFields(BF)) {
  object v = null; try { v = f.GetValue(rd); } catch { }
  var s = v is System.Collections.ICollection c ? $"[{c.Count}]" : "";
  Out.AppendLine($"F {f.Name} : {f.FieldType.Name} {s}");
  if (System.Text.RegularExpressions.Regex.IsMatch(f.Name, "(?i)crosstab|view") && v is System.Collections.IEnumerable en && !(v is string)) {
    int k = 0; foreach (var it in en) { var nm = it?.GetType().GetProperty("Name", BF)?.GetValue(it) ?? it?.GetType().GetProperty("Title", BF)?.GetValue(it); Out.AppendLine($"    [{k++}] {it?.GetType().Name}: {nm}"); }
  }
}
return null;
