if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Read-only: show the structure of the IS report data (Report tab) and the current report view.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
void Dump(string label, object o) {
  if (o == null) { Out.AppendLine(label + " = null"); return; }
  Out.AppendLine($"{label} : {o.GetType().FullName}");
  foreach (var p in o.GetType().GetProperties(BF)) {
    if (p.GetIndexParameters().Length > 0) continue;
    object v = null; try { v = p.GetValue(o); } catch { continue; }
    var s = v is System.Collections.ICollection c ? $"[{c.Count}] {v.GetType().Name}" : System.Convert.ToString(v);
    if (s != null && s.Length > 120) s = s.Substring(0, 120);
    Out.AppendLine($"   {p.Name} ({p.PropertyType.Name}) = {s}");
  }
}
object SP(string n) => Schedule.GetType().GetProperty(n, BF)?.GetValue(Schedule);
Dump("reportData", SP("reportData"));
Dump("currentReportView", SP("currentReportView"));
var cals = (System.Collections.IList)SP("ReportCalendars");
Dump("activeCalendar", cals[(int)SP("ActiveReportCalendar")]);
return null;
