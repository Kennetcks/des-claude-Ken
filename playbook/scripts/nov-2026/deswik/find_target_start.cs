if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Read-only: find where the schedule target start / status date lives.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
void Scan(object o, string path, int depth) {
  if (o == null || depth > 2) return;
  foreach (var p in o.GetType().GetProperties(BF)) {
    if (p.GetIndexParameters().Length > 0) continue;
    var n = p.Name;
    bool hit = System.Text.RegularExpressions.Regex.IsMatch(n, "(?i)target|status|projectstart|startdate|currentdate|datadate|levell?ing");
    object v = null; try { v = p.GetValue(o); } catch { continue; }
    if (hit) Out.AppendLine($"{path}.{n} ({p.PropertyType.Name}, set:{p.CanWrite}) = {v}");
    if (depth < 2 && v != null && (n.Contains("Setting") || n.Contains("Option") || n.Contains("Propert") || n.Contains("Level"))) Scan(v, path + "." + n, depth + 1);
  }
}
Scan(Schedule, "Schedule", 0);
return null;
