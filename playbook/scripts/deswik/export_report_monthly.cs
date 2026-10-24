// Read-only: export the active report view (Schedule Export 01 - Underground) row by row, monthly for the
// full life of mine, plus FY (July-June) and LOM totals, using Deswik's own row calculation (GetRowTotal).
// Honours the active task filter. Writes %TEMP%\dsw\lom_export.tsv. Changes nothing in the schedule.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
var views = ((System.Collections.IEnumerable)Schedule.GetType().GetField("m_crossTabViews", BF).GetValue(Schedule)).Cast<Deswik.Scheduler.Engine.ReportView>().ToList();
var view = views[(int)Schedule.GetType().GetField("m_activeCrossTabView", BF).GetValue(Schedule)];
if (view.Name != "Schedule Export 01 - Underground") { Out.AppendLine("Active report is '" + view.Name + "' - stopping."); return null; }
var data = (Deswik.Scheduler.Engine.ReportDataWithContainer)Schedule.GetType().GetMethod("GetReportData", BF, null, System.Type.EmptyTypes, null).Invoke(Schedule, null);

// Life-of-mine span from scheduled tasks.
var tasks = Schedule.GetTasks().Where(t => !t.Deleted).ToList();
var first = tasks.Min(t => t.Start); var last = tasks.Max(t => t.Finish);
var m0 = new System.DateTime(first.Year, first.Month, 1, 6, 0, 0); if (first < m0) m0 = m0.AddMonths(-1);
var periods = new List<(string label, System.DateTime a, System.DateTime b)>();
for (var m = m0; m < last; m = m.AddMonths(1)) periods.Add((m.ToString("MMM-yy"), m, m.AddMonths(1)));
int fy0 = m0.Month >= 7 ? m0.Year + 1 : m0.Year, fy1 = last.Month >= 7 ? last.Year + 1 : last.Year;
var fys = new List<(string label, System.DateTime a, System.DateTime b)>();
for (int y = fy0; y <= fy1; y++) fys.Add(("FY" + (y % 100).ToString("00"), new System.DateTime(y - 1, 7, 1, 6, 0, 0), new System.DateTime(y, 7, 1, 6, 0, 0)));

var sb = new System.Text.StringBuilder();
sb.Append("Row\tTitle\tField\tFilter\tCalculate\tFormat\tIsFormula\tLOM");
foreach (var f in fys) sb.Append('\t').Append(f.label);
foreach (var p in periods) sb.Append('\t').Append(p.label);
sb.Append('\n');
var lom = new Deswik.Scheduler.Engine.DateRangeF(m0, periods[periods.Count - 1].b);
System.Func<int, Deswik.Scheduler.Engine.DateRangeF, string> val = (i, r) => { try { var x = data.GetRowTotal(i, r); return x.HasValue ? x.Value.ToString("R", System.Globalization.CultureInfo.InvariantCulture) : ""; } catch { return "ERR"; } };
for (int i = 0; i < view.TotaledFields.Count; i++)
{
    var tf = view.TotaledFields[i];
    sb.Append(i).Append('\t').Append(tf.Title).Append('\t').Append(tf.Field).Append('\t').Append(tf.Filter).Append('\t').Append(tf.CalculateString)
      .Append('\t').Append(tf.Format).Append('\t').Append(tf.IsFormulaField).Append('\t').Append(val(i, lom));
    foreach (var f in fys) sb.Append('\t').Append(val(i, new Deswik.Scheduler.Engine.DateRangeF(f.a, f.b)));
    foreach (var p in periods) sb.Append('\t').Append(val(i, new Deswik.Scheduler.Engine.DateRangeF(p.a, p.b)));
    sb.Append('\n');
}
System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "dsw", "lom_export.tsv"), sb.ToString());
Out.AppendLine($"Exported {view.TotaledFields.Count} rows x {periods.Count} months ({periods[0].label} to {periods[periods.Count - 1].label}), {fys.Count} FYs. Schedule span {first:dd/MM/yyyy} - {last:dd/MM/yyyy}.");
return null;
