if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Read-only: rebuild the "Daily Report Dump" report by day for Nov 2026. Each row uses the same field and filter as the report view;
// each task's quantity is spread over its Start-Finish span in proportion to time (days split at midnight).
// "Average" rows (grades) are tonnes-weighted. Writes C:\dswtmp\view_5_daily.tsv (30 days + month total).
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
object G(object o, string n) { if (o == null) return null; var p = o.GetType().GetProperty(n, BF); if (p != null && p.GetIndexParameters().Length == 0) return p.GetValue(o); return o.GetType().GetField(n, BF)?.GetValue(o); }
var numErr = new HashSet<string>();
double Num(Deswik.Scheduler.Engine.Task t, string f) {
  try { var v = t.GetNullaleFieldFullValue(f); if (v == null) return 0; var m = v.GetType().GetMethod("AsDouble", System.Type.EmptyTypes); return m != null ? System.Convert.ToDouble(m.Invoke(v, null)) : double.Parse(v.ToString(), System.Globalization.CultureInfo.InvariantCulture); }
  catch (System.Exception ex) { numErr.Add(f + ": " + (ex.InnerException ?? ex).Message); return 0; } }
var view = ((System.Collections.IEnumerable)G(Schedule, "CrossTabViews")).Cast<object>().ToList()[5];
var tif = Schedule.GetType().GetMethods(BF).First(m => m.Name == "TaskIsInFilter" && m.GetParameters().Length == 2);
var day0 = new System.DateTime(2026, 11, 1); int nDays = 30; var dayN = day0.AddDays(nDays);
var live = Schedule.Tasks.Where(t => !t.Deleted && t.Finish > day0 && t.Start < dayN).ToList();
var cache = new Dictionary<string, List<Deswik.Scheduler.Engine.Task>>();
List<Deswik.Scheduler.Engine.Task> InFilter(string f) {
  if (!cache.TryGetValue(f, out var l)) { l = live.Where(t => f == "<No Filtering>" || (bool)tif.Invoke(Schedule, new object[] { t, f })).ToList(); cache[f] = l; }
  return l;
}
double Frac(Deswik.Scheduler.Engine.Task t, System.DateTime a, System.DateTime b) {
  var dur = (t.Finish - t.Start).TotalSeconds;
  if (dur <= 0) return (t.Start >= a && t.Start < b) ? 1 : 0;
  var s = t.Start > a ? t.Start : a; var e = t.Finish < b ? t.Finish : b;
  return e > s ? (e - s).TotalSeconds / dur : 0;
}
var sb = new StringBuilder(); sb.Append("Name"); for (int d = 0; d < nDays; d++) sb.Append("\t" + day0.AddDays(d).ToString("yyyy-MM-dd")); sb.AppendLine("\tMonth");
var inv = System.Globalization.CultureInfo.InvariantCulture;
foreach (var tf in (System.Collections.IEnumerable)G(view, "TotaledFields")) {
  var name = System.Convert.ToString(G(tf, "Name") ?? "");
  var field = System.Convert.ToString(G(tf, "Field") ?? "");
  sb.Append(name);
  if (field == "" || System.Convert.ToString(G(tf, "RowType")) == "Blank") { for (int d = 0; d <= nDays; d++) sb.Append("\t0"); sb.AppendLine(); continue; }
  var tasks = InFilter(System.Convert.ToString(G(tf, "Filter")));
  bool avg = System.Convert.ToString(G(tf, "Calculate")) == "Average";
  double V(System.DateTime a, System.DateTime b) {
    double sum = 0, w = 0;
    foreach (var t in tasks) {
      var f = Frac(t, a, b); if (f == 0) continue;
      if (avg) { var tw = Num(t, "Sch_Diluted tonnes") * f; sum += Num(t, field) * tw; w += tw; }
      else sum += Num(t, field) * f;
    }
    return avg ? (w > 0 ? sum / w : 0) : sum;
  }
  for (int d = 0; d < nDays; d++) sb.Append("\t" + V(day0.AddDays(d), day0.AddDays(d + 1)).ToString("R", inv));
  sb.AppendLine("\t" + V(day0, dayN).ToString("R", inv));
}
System.IO.File.WriteAllText(@"C:\dswtmp\view_5_daily.tsv", sb.ToString());
foreach (var e in numErr.Take(5)) Out.AppendLine("field read error " + e);
Out.AppendLine($"tasks overlapping Nov: {live.Count}; filters: {string.Join(", ", cache.Select(kv => kv.Key + "=" + kv.Value.Count))}");
return null;
