if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Export one IS cross-tab report view (rows x months) to C:\dswtmp\view_<n>.tsv using the report's optional-view override, then clear the override.
// No task data changes. File not saved.
// C:\dswtmp\view_to_export.txt = "view,timeScaleId,yyyy-MM-dd from,yyyy-MM-dd to"
var A = System.IO.File.ReadAllText(@"C:\dswtmp\view_to_export.txt").Trim().Split(',');
int VIEW = int.Parse(A[0]), TS = int.Parse(A[1]); var from = System.DateTime.Parse(A[2]); var to = System.DateTime.Parse(A[3]);
var BF0 = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
var tsProp = Schedule.GetType().GetProperty("ActiveTimeScaleId", BF0); int prevTs = (int)tsProp.GetValue(Schedule);
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
object G(object o, string n) { if (o == null) return null; var p = o.GetType().GetProperty(n, BF); if (p != null && p.GetIndexParameters().Length == 0) return p.GetValue(o); return o.GetType().GetField(n, BF)?.GetValue(o); }
var rd = G(Schedule, "reportData");
var setter = Schedule.GetType().GetMethods(BF).FirstOrDefault(m => m.Name == "set_ActiveCrossTabView");
System.Reflection.FieldInfo opt = null; object owner = Schedule;
if (setter == null) {
  foreach (var f in Schedule.GetType().GetFields(BF)) if (f.FieldType == typeof(int) && f.Name.ToLower().Contains("activecrosstabview")) { opt = f; break; }
  if (opt == null) { var getterBody = string.Join(", ", Schedule.GetType().GetFields(BF).Where(f => f.Name.ToLower().Contains("crosstab")).Select(f => f.Name + ":" + f.FieldType.Name)); Out.AppendLine("no ActiveCrossTabView field; crosstab fields: " + getterBody); return null; }
}
var prevOpt = setter != null ? Schedule.GetType().GetProperty("ActiveCrossTabView", BF).GetValue(Schedule) : opt.GetValue(owner);
Out.AppendLine($"using {(setter != null ? "setter" : "field " + opt.Name)}");
var views = ((System.Collections.IEnumerable)G(Schedule, "CrossTabViews")).Cast<object>().ToList();
for (int i = 0; i < views.Count; i++) Out.AppendLine($"  view [{i}] {G(views[i], "Name")}: {((System.Collections.ICollection)G(views[i], "TotaledFields"))?.Count} rows");
var view = views[VIEW];
var names = new List<string>();
foreach (var tf in (System.Collections.IEnumerable)G(view, "TotaledFields")) names.Add(System.Convert.ToString(G(tf, "Name") ?? G(tf, "Title") ?? G(tf, "DisplayName") ?? tf));
Out.AppendLine($"exporting view [{VIEW}] {G(view, "Name")}: {names.Count} rows; optional view override was {prevOpt}");
string s;
try {
  if (setter != null) setter.Invoke(Schedule, new object[] { VIEW }); else opt.SetValue(owner, VIEW);
  if (TS != prevTs) {
    tsProp.SetValue(Schedule, TS);
    var sc = G(Schedule, "ActiveTimeScale");
    sc.GetType().GetMethod("set_TickPointsUpToDate", BF)?.Invoke(sc, new object[] { false });
    sc.GetType().GetMethod("UpdateMinutesPerPixel", BF, null, System.Type.EmptyTypes, null)?.Invoke(sc, null);
    var tp = sc.GetType().GetMethod("GetTickPointListBetweenDates", BF, null, new[] { typeof(System.DateTime), typeof(System.DateTime) }, null)?.Invoke(sc, new object[] { from, to }) as System.Collections.ICollection;
    Out.AppendLine($"tick points in range: {tp?.Count}");
  }
  Out.AppendLine($"time scale now {G(Schedule, "ActiveTimeScale")?.ToString().Split('\n')[0]}; report scale {G(rd, "ActiveTimeScaling")?.ToString().Split('\n')[0]}");
  var rdTsField = rd.GetType().GetFields(BF).Where(f => f.FieldType.Name == "TimeScaling").Select(f => f.Name);
  Out.AppendLine("report TimeScaling fields: " + string.Join(", ", rdTsField));
  var rdcTs = G(G(rd, "ReportDataContainer"), "ActiveTimeScaling");
  Out.AppendLine($"container scale {rdcTs?.ToString().Split('\n')[0]}");
  Out.AppendLine($"active view now {G(Schedule, "ActiveCrossTabView")} / report {G(rd, "ActiveCrossTabView")}");
  rd.GetType().GetMethod("SignalReportRowsChanged", BF)?.Invoke(rd, new object[] { true });
  rd.GetType().GetMethod("CalculateReportValues", BF, null, System.Type.EmptyTypes, null).Invoke(rd, null);
  var ens = rd.GetType().GetMethod("EnsureAllRowsCalculated", BF, null, System.Type.EmptyTypes, null);
  for (int k = 0; k < 60; k++) { if ((bool)ens.Invoke(rd, null)) { Out.AppendLine($"rows calculated after {k} checks"); break; } System.Threading.Thread.Sleep(1000); }
  bool fullTiers = A.Length > 4 && A[4] == "1";
  var args = new object[] { from, to, "", fullTiers, null, null, false, null, false, false };
  s = (string)rd.GetType().GetMethod("GetReportDataRangeForExport", BF).Invoke(rd, args);
} finally {
  if (setter != null) setter.Invoke(Schedule, new object[] { prevOpt }); else opt.SetValue(owner, prevOpt);
  if ((int)tsProp.GetValue(Schedule) != prevTs) tsProp.SetValue(Schedule, prevTs);
  rd.GetType().GetMethod("SignalReportRowsChanged", BF)?.Invoke(rd, new object[] { true });
}
var lines = (s ?? "").TrimEnd('\r', '\n').Split('\n').Select(l => l.TrimEnd('\r')).ToList();
var sb = new StringBuilder();
for (int i = 0; i < lines.Count; i++) sb.AppendLine((i == 0 ? "Name" : (i - 1 < names.Count ? names[i - 1] : "?")) + "\t" + lines[i]);
System.IO.File.WriteAllText($@"C:\dswtmp\view_{VIEW}.tsv", sb.ToString());
Out.AppendLine($"export lines {lines.Count} (header + {lines.Count - 1}); active view restored to {G(Schedule, "ActiveCrossTabView")}, time scale {tsProp.GetValue(Schedule)}");
foreach (var l in sb.ToString().Split('\n').Take(6)) Out.AppendLine(l.Length > 200 ? l.Substring(0, 200) : l);
return null;
