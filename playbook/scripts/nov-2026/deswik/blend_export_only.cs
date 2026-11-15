// Read-only: export ONLY the Blend Forecast report view (12 months from 1 Oct 2026) to %TEMP%\dsw\phys_blend_fix.tsv.
// The 2 Nov export (phys_export.cs) read the Blend Forecast right after the Daily Report Dump, and from Nov-26 on its
// rows came back holding other rows' numbers. Here the view is switched, the filter cache refreshed, the report data
// fetched fresh, and each month checked: All ore oz = HG+MG+LG+(0.5-1) oz and All ore t = sum of bins. Active report restored.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
var fView = Schedule.GetType().GetField("m_activeCrossTabView", BF);
var views = ((System.Collections.IEnumerable)Schedule.GetType().GetField("m_crossTabViews", BF).GetValue(Schedule)).Cast<Deswik.Scheduler.Engine.ReportView>().ToList();
int orig = (int)fView.GetValue(Schedule);
var inv = System.Globalization.CultureInfo.InvariantCulture;
int idx = views.FindIndex(v => v.Name == "Blend Forecast"); if (idx < 0) { Out.AppendLine("Blend Forecast NOT FOUND"); return null; }
Out.AppendLine("active report before: " + views[orig].Name);
try
{
    fView.SetValue(Schedule, idx);
    var cache = Schedule.GetType().GetField("<matchingFilterCache>k__BackingField", BF).GetValue(Schedule);
    cache?.GetType().GetMethod("Update", BF, null, System.Type.EmptyTypes, null)?.Invoke(cache, null);
    var mGet = Schedule.GetType().GetMethod("GetReportData", BF, null, System.Type.EmptyTypes, null);
    var data = (Deswik.Scheduler.Engine.ReportDataWithContainer)mGet.Invoke(Schedule, null);
    var view = views[idx]; int n = view.TotaledFields.Count;
    var months = Enumerable.Range(0, 12).Select(k => new System.DateTime(2026, 10, 1).AddMonths(k)).ToList();
    var val = new double?[n, 12];
    for (int i = 0; i < n; i++) for (int c = 0; c < 12; c++) { try { val[i, c] = data.GetRowTotal(i, new Deswik.Scheduler.Engine.DateRangeF(months[c], months[c].AddMonths(1))); } catch { val[i, c] = null; } }
    var sb = new System.Text.StringBuilder("Row\tTitle\tField\tCalculate"); foreach (var m in months) sb.Append('\t').Append(m.ToString("yyyy-MM-dd")); sb.Append('\n');
    for (int i = 0; i < n; i++) { var tf = view.TotaledFields[i]; sb.Append(i).Append('\t').Append(tf.Title).Append('\t').Append(tf.Field).Append('\t').Append(tf.CalculateString); for (int c = 0; c < 12; c++) sb.Append('\t').Append(val[i, c].HasValue ? val[i, c].Value.ToString("R", inv) : ""); sb.Append('\n'); }
    System.IO.Directory.CreateDirectory(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "dsw"));
    System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "dsw", "phys_blend_fix.tsv"), sb.ToString());
    int R(string t) { for (int i = 0; i < n; i++) if ((view.TotaledFields[i].Title ?? "").Trim() == t) return i; return -1; }
    double V(int r, int c) => r < 0 ? double.NaN : (val[r, c] ?? 0);
    int[] tR = { R("HG T >3g/t"), R("MG T 2-3g/t"), R("LG T 1-2g/t"), R("MG T 0.5-1g/t") }, oR = { R("HG OZ >3g/t"), R("MG OZ 2-3g/t"), R("LG OZ 1-2g/t"), R("MG OZ 0.5-1g/t") };
    int aT = R("All ore - Tonnes"), aO = R("All ore - Au Ounces"), dT = R("Dev ore - Tonnes"), sT = R("Stope ore - Tonnes");
    Out.AppendLine($"{n} rows; month | bins t | all ore t | dev t + stope t | bins oz | all ore oz");
    for (int c = 0; c < 12; c++)
        Out.AppendLine($"  {months[c]:MMM-yy} | {tR.Sum(r => V(r, c)):F0} | {V(aT, c):F0} | {V(dT, c) + V(sT, c):F0} | {oR.Sum(r => V(r, c)):F1} | {V(aO, c):F1}");
}
finally { fView.SetValue(Schedule, orig); Out.AppendLine("active report put back to: " + views[orig].Name); }
return null;
