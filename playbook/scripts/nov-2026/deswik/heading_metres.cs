if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Read-only: lateral development metres (DEV_Lateral, Sch_metres) by task Name and MINE for Nov 2026 - Jan 2027,
// with ACT_TYPE and first cut (earliest start with metres). Writes C:\dswtmp\heading_metres.tsv.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
double Num(Deswik.Scheduler.Engine.Task t, string f) { var v = t.GetNullaleFieldFullValue(f); if (v == null) return 0; var m = v.GetType().GetMethod("AsDouble", System.Type.EmptyTypes); return System.Convert.ToDouble(m.Invoke(v, null)); }
string FV(Deswik.Scheduler.Engine.Task t, string f) { try { return System.Convert.ToString(t.GetNullaleFieldFullValue(f)); } catch { return ""; } }
var tif = Schedule.GetType().GetMethods(BF).First(m => m.Name == "TaskIsInFilter" && m.GetParameters().Length == 2);
var mo = new[] { new System.DateTime(2026, 11, 1), new System.DateTime(2026, 12, 1), new System.DateTime(2027, 1, 1), new System.DateTime(2027, 2, 1) };
double Frac(Deswik.Scheduler.Engine.Task t, System.DateTime a, System.DateTime b) {
  var dur = (t.Finish - t.Start).TotalSeconds;
  if (dur <= 0) return (t.Start >= a && t.Start < b) ? 1 : 0;
  var s = t.Start > a ? t.Start : a; var e = t.Finish < b ? t.Finish : b;
  return e > s ? (e - s).TotalSeconds / dur : 0;
}
var rows = new Dictionary<string, (string act, double[] m, System.DateTime first)>();
foreach (var t in Schedule.Tasks.Where(t => !t.Deleted && (bool)tif.Invoke(Schedule, new object[] { t, "DEV_Lateral" }))) {
  var met = Num(t, "Sch_metres"); if (met == 0) continue;
  var key = t.Name + "\t" + FV(t, "MINE");
  if (!rows.TryGetValue(key, out var r)) r = (FV(t, "ACT_TYPE"), new double[3], System.DateTime.MaxValue);
  for (int i = 0; i < 3; i++) r.m[i] += met * Frac(t, mo[i], mo[i + 1]);
  if (t.Start < r.first) r.first = t.Start;
  rows[key] = r;
}
var inv = System.Globalization.CultureInfo.InvariantCulture;
var sb = new StringBuilder("Name\tMINE\tACT_TYPE\tNov\tDec\tJan\tFirstCut\n");
foreach (var kv in rows.OrderBy(k => k.Key)) sb.AppendLine($"{kv.Key}\t{kv.Value.act}\t{kv.Value.m[0].ToString("F1", inv)}\t{kv.Value.m[1].ToString("F1", inv)}\t{kv.Value.m[2].ToString("F1", inv)}\t{kv.Value.first:dd/MM/yyyy}");
System.IO.File.WriteAllText(@"C:\dswtmp\heading_metres.tsv", sb.ToString());
Out.AppendLine($"rows {rows.Count}");
return null;
