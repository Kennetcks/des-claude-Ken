if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Read-only: export the active IS cross-tab report (monthly, Nov 2026 - Nov 2027) to a text file for the Physicals workbook.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
var rd = Schedule.GetType().GetProperty("reportData", BF).GetValue(Schedule);
Out.AppendLine($"active cross-tab view: {rd.GetType().GetProperty("ActiveCrossTabView", BF).GetValue(rd)}");
rd.GetType().GetMethod("CalculateReportValues", BF, null, System.Type.EmptyTypes, null).Invoke(rd, null);
var m = rd.GetType().GetMethod("GetReportDataRangeForExport", BF);
var args = new object[] { new System.DateTime(2026, 11, 1), new System.DateTime(2027, 12, 1), "", false, null, null, false, null, false, false };
var s = (string)m.Invoke(rd, args);
System.IO.File.WriteAllText(@"C:\dswtmp\report_export.txt", s ?? "");
var lines = (s ?? "").Split('\n');
Out.AppendLine($"lines: {lines.Length}, chars: {s?.Length}");
foreach (var l in lines.Take(8)) Out.AppendLine(l.Length > 300 ? l.Substring(0, 300) : l);
return null;
