if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Export three report views for the November physicals, honouring the active task filter (Mine=A+B):
//   Schedule Export 01 - Underground : 13 months from 1 Nov 2026      -> %TEMP%\dsw\phys_main.tsv
//   Daily Report Dump               : 30 days of November 2026        -> %TEMP%\dsw\phys_daily.tsv
//   Blend Forecast                  : 12 months from 1 Oct 2026        -> %TEMP%\dsw\phys_blend.tsv
// Periods start at midnight. GetReportData only works on the active report, so the active report is switched
// to each view in turn and put back to the original at the end. No schedule data is changed.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
var fView = Schedule.GetType().GetField("m_activeCrossTabView", BF);
var views = ((System.Collections.IEnumerable)Schedule.GetType().GetField("m_crossTabViews", BF).GetValue(Schedule)).Cast<Deswik.Scheduler.Engine.ReportView>().ToList();
int orig = (int)fView.GetValue(Schedule);
// refresh Deswik's cached task-to-filter matches (field edits made through the API don't refresh it)
var cache = Schedule.GetType().GetField("<matchingFilterCache>k__BackingField", BF).GetValue(Schedule);
cache?.GetType().GetMethod("Update", BF, null, System.Type.EmptyTypes, null)?.Invoke(cache, null);
var mGet = Schedule.GetType().GetMethod("GetReportData", BF, null, System.Type.EmptyTypes, null);
var inv = System.Globalization.CultureInfo.InvariantCulture;
string Dump(string viewName, List<(string label, System.DateTime a, System.DateTime b)> periods, string file) {
  int idx = views.FindIndex(v => v.Name == viewName); if (idx < 0) return viewName + ": NOT FOUND";
  fView.SetValue(Schedule, idx);
  var data = (Deswik.Scheduler.Engine.ReportDataWithContainer)mGet.Invoke(Schedule, null);
  var view = views[idx]; var sb = new System.Text.StringBuilder("Row\tTitle\tField\tCalculate");
  foreach (var p in periods) sb.Append('\t').Append(p.label); sb.Append('\n');
  for (int i = 0; i < view.TotaledFields.Count; i++) { var tf = view.TotaledFields[i];
    sb.Append(i).Append('\t').Append(tf.Title).Append('\t').Append(tf.Field).Append('\t').Append(tf.CalculateString);
    foreach (var p in periods) { string s; try { var x = data.GetRowTotal(i, new Deswik.Scheduler.Engine.DateRangeF(p.a, p.b)); s = x.HasValue ? x.Value.ToString("R", inv) : ""; } catch { s = "ERR"; } sb.Append('\t').Append(s); }
    sb.Append('\n'); }
  System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "dsw", file), sb.ToString());
  return $"{viewName}: {view.TotaledFields.Count} rows x {periods.Count} periods -> {file}"; }
var months13 = Enumerable.Range(0, 13).Select(k => { var a = new System.DateTime(2026, 11, 1).AddMonths(k); return (a.ToString("yyyy-MM-dd"), a, a.AddMonths(1)); }).ToList();
var months12 = Enumerable.Range(0, 12).Select(k => { var a = new System.DateTime(2026, 10, 1).AddMonths(k); return (a.ToString("yyyy-MM-dd"), a, a.AddMonths(1)); }).ToList();
var days = Enumerable.Range(0, 30).Select(k => { var a = new System.DateTime(2026, 11, 1).AddDays(k); return (a.ToString("yyyy-MM-dd"), a, a.AddDays(1)); }).ToList();
try {
  Out.AppendLine(Dump("Schedule Export 01 - Underground", months13, "phys_main.tsv"));
  Out.AppendLine(Dump("Daily Report Dump", days, "phys_daily.tsv"));
  Out.AppendLine(Dump("Blend Forecast", months12, "phys_blend.tsv"));
} finally { fView.SetValue(Schedule, orig); Out.AppendLine("active report put back to: " + views[orig].Name); }
return null;
