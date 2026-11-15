if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Read-only: lateral development metres (filter DEV_Lateral, Sch_metres) grouped by task Name for Nov 2026, Dec 2026 and Jan 2027,
// each task spread over its Start-Finish span in proportion to time. Writes C:\dswtmp\dev_dump.tsv.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
double Num(Deswik.Scheduler.Engine.Task t, string f) { var v = t.GetNullaleFieldFullValue(f); if (v == null) return 0; var m = v.GetType().GetMethod("AsDouble", System.Type.EmptyTypes); return System.Convert.ToDouble(m.Invoke(v, null)); }
var tif = Schedule.GetType().GetMethods(BF).First(m => m.Name == "TaskIsInFilter" && m.GetParameters().Length == 2);
var months = new[] { new System.DateTime(2026, 11, 1), new System.DateTime(2026, 12, 1), new System.DateTime(2027, 1, 1), new System.DateTime(2027, 2, 1) };
double Frac(Deswik.Scheduler.Engine.Task t, System.DateTime a, System.DateTime b) {
  var dur = (t.Finish - t.Start).TotalSeconds;
  if (dur <= 0) return (t.Start >= a && t.Start < b) ? 1 : 0;
  var s = t.Start > a ? t.Start : a; var e = t.Finish < b ? t.Finish : b;
  return e > s ? (e - s).TotalSeconds / dur : 0;
}
var tasks = Schedule.Tasks.Where(t => !t.Deleted && t.Finish > months[0] && t.Start < months[3] && (bool)tif.Invoke(Schedule, new object[] { t, "DEV_Lateral" })).ToList();
var rows = new SortedDictionary<string, double[]>(System.StringComparer.Ordinal);
foreach (var t in tasks) {
  var m = Num(t, "Sch_metres"); if (m == 0) continue;
  if (!rows.TryGetValue(t.Name, out var r)) rows[t.Name] = r = new double[3];
  for (int i = 0; i < 3; i++) r[i] += m * Frac(t, months[i], months[i + 1]);
}
var inv = System.Globalization.CultureInfo.InvariantCulture;
var sb = new StringBuilder();
foreach (var kv in rows.Where(kv => kv.Value.Any(x => x > 1e-9))) sb.AppendLine(kv.Key + "\t" + string.Join("\t", kv.Value.Select(x => x.ToString("R", inv))));
System.IO.File.WriteAllText(@"C:\dswtmp\dev_dump.tsv", sb.ToString());
Out.AppendLine($"headings {rows.Count(kv => kv.Value.Any(x => x > 1e-9))}; totals Nov {rows.Values.Sum(r => r[0]):F2}, Dec {rows.Values.Sum(r => r[1]):F2}, Jan {rows.Values.Sum(r => r[2]):F2}");
return null;
