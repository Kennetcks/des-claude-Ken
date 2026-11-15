if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Read-only: show how each row of the "Daily Report Dump" report view is defined.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
object G(object o, string n) { if (o == null) return null; var p = o.GetType().GetProperty(n, BF); if (p != null && p.GetIndexParameters().Length == 0) return p.GetValue(o); return o.GetType().GetField(n, BF)?.GetValue(o); }
var view = ((System.Collections.IEnumerable)G(Schedule, "CrossTabViews")).Cast<object>().ToList()[5];
int k = 0;
foreach (var tf in (System.Collections.IEnumerable)G(view, "TotaledFields")) {
  var parts = new List<string>();
  foreach (var p in tf.GetType().GetProperties(BF)) {
    if (p.GetIndexParameters().Length > 0) continue;
    object v = null; try { v = p.GetValue(tf); } catch { continue; }
    if (v == null) continue; var s = System.Convert.ToString(v);
    if (s == "" || s == "0" || s == "False" || s == "-1" || s.StartsWith("System.") || s.StartsWith("Deswik.")) continue;
    if (System.Text.RegularExpressions.Regex.IsMatch(p.Name, "(?i)colou?r|font|width|height|format|decimal|style|chart|line|visible|unique|saveid|locked|active$|master|slave|favourite|rename|priority")) continue;
    parts.Add($"{p.Name}={(s.Length > 160 ? s.Substring(0, 160) : s)}");
  }
  Out.AppendLine($"[{k++}] " + string.Join("; ", parts));
}
return null;
