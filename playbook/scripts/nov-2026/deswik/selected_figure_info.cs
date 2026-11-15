if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Read-only: show the selected CAD figure(s) - layer, handle/id and all attribute values - and search figures on TASK SOLIDS for "44A92F2".
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
string Dump(object f) {
  var parts = new List<string>();
  foreach (var p in f.GetType().GetProperties(BF)) {
    if (p.GetIndexParameters().Length > 0) continue;
    if (!System.Text.RegularExpressions.Regex.IsMatch(p.Name, "(?i)handle|id$|guid|name|attrib")) continue;
    object v = null; try { v = p.GetValue(f); } catch { continue; }
    if (v is System.Collections.IEnumerable en && !(v is string)) { var s = new List<string>(); foreach (var a in en) { var nm = a?.GetType().GetProperty("Name", BF)?.GetValue(a); var val = a?.GetType().GetProperty("Value", BF)?.GetValue(a); s.Add(nm != null ? $"{nm}={val}" : System.Convert.ToString(a)); } parts.Add($"{p.Name}: {string.Join(", ", s.Take(60))}"); }
    else parts.Add($"{p.Name}={v}");
  }
  return string.Join(" | ", parts);
}
foreach (Deswik.Graphics.Primaries.Figure f in App.Selections.MainSelection) Out.AppendLine($"SELECTED on {f.Layer.Name}: {Dump(f)}");
int n = 0;
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) {
  if (n >= 5) break;
  var d = Dump(f);
  if (d.IndexOf("44A92F2", System.StringComparison.OrdinalIgnoreCase) >= 0) { Out.AppendLine($"MATCH on {f.Layer.Name}: {(d.Length > 1500 ? d.Substring(0, 1500) : d)}"); n++; }
}
Out.AppendLine($"figures matching 44A92F2: {n}");
return null;
