if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Read-only: find schedule-level caches that hold compiled task filters.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
foreach (var fi in Schedule.GetType().GetFields(BF)) {
  var tn = fi.FieldType.Name;
  if (!(System.Text.RegularExpressions.Regex.IsMatch(fi.Name, "(?i)filter|where|compiled|express|cache") || System.Text.RegularExpressions.Regex.IsMatch(fi.FieldType.ToString(), "(?i)filter|express|delegate|Func"))) continue;
  object v = null; try { v = fi.GetValue(Schedule); } catch { }
  Out.AppendLine($"F {fi.Name} : {fi.FieldType} {(v is System.Collections.ICollection c ? "[" + c.Count + "]" : "")}");
}
foreach (var m in Schedule.GetType().GetMethods(BF).Where(m => System.Text.RegularExpressions.Regex.IsMatch(m.Name, "(?i)filter") && System.Text.RegularExpressions.Regex.IsMatch(m.Name, "(?i)clear|reset|invalid|refresh|update|compile|rebuild|apply")).Select(m => m.Name + "(" + string.Join(",", m.GetParameters().Select(p => p.ParameterType.Name)) + ")").Distinct()) Out.AppendLine("M " + m);
return null;
