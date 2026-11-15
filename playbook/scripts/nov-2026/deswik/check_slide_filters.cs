if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Read-only: count tasks in each "3MS S1x" filter (after a cache refresh) and show its where clause start and Nov-Jan metres.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
object G(object o, string n) { var p = o.GetType().GetProperty(n, BF); if (p != null) return p.GetValue(o); return o.GetType().GetField(n, BF)?.GetValue(o); }
var cache = Schedule.GetType().GetField("<matchingFilterCache>k__BackingField", BF).GetValue(Schedule);
cache?.GetType().GetMethod("Update", BF, null, System.Type.EmptyTypes, null)?.Invoke(cache, null);
var fl = ((System.Collections.IEnumerable)Schedule.GetType().GetField("<TaskFilters>k__BackingField", BF).GetValue(Schedule)).Cast<object>().ToList();
var tif = Schedule.GetType().GetMethods(BF).First(m => m.Name == "TaskIsInFilter" && m.GetParameters().Length == 2);
int act = (int)Schedule.GetType().GetField("m_activeTaskFilter", BF).GetValue(Schedule);
Out.AppendLine($"active filter still [{act}] {G(fl[act], "Name")}");
foreach (var f in fl.Where(x => System.Convert.ToString(G(x, "Name")).StartsWith("3MS S1"))) {
  var n = System.Convert.ToString(G(f, "Name"));
  var hits = Schedule.Tasks.Where(t => !t.Deleted && (bool)tif.Invoke(Schedule, new object[] { t, n })).ToList();
  var w = System.Convert.ToString(G(f, "where"));
  Out.AppendLine($"[{fl.IndexOf(f)}] {n}: {hits.Count} tasks, {hits.Select(t => t.Name).Distinct().Count()} names; where: {(w.Length > 110 ? w.Substring(0, 110) + "..." : w)}");
}
return null;
