if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Read-only: list the cross-tab (report) views with their row counts and first row names.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
object G(object o, string n) { if (o == null) return null; var p = o.GetType().GetProperty(n, BF); if (p != null && p.GetIndexParameters().Length == 0) return p.GetValue(o); return o.GetType().GetField(n, BF)?.GetValue(o); }
Out.AppendLine($"ActiveCrossTabView {G(Schedule, "ActiveCrossTabView")}, ActiveCrossTabSheet {G(Schedule, "ActiveCrossTabSheet")}");
var views = ((System.Collections.IEnumerable)G(Schedule, "CrossTabViews")).Cast<object>().ToList();
if (views.Count > 0) Out.AppendLine("view type " + views[0].GetType().FullName + ": " + string.Join(", ", views[0].GetType().GetProperties(BF).Where(p => p.GetIndexParameters().Length == 0).Select(p => p.Name)));
for (int i = 0; i < views.Count; i++) {
  var v = views[i];
  var rows = v as System.Collections.IEnumerable;
  int n = 0; string first = "";
  if (rows != null) foreach (var r in rows) { if (n < 4) first += " | " + (G(r, "Name") ?? G(r, "Title") ?? G(r, "Description")); n++; }
  Out.AppendLine($"[{i}] {G(v, "Name")} rows={n}{first}");
}
var sheets = ((System.Collections.IEnumerable)G(Schedule, "CrossTabSheets")).Cast<object>().ToList();
for (int i = 0; i < sheets.Count; i++) Out.AppendLine($"sheet [{i}] {G(sheets[i], "Name")}");
return null;
