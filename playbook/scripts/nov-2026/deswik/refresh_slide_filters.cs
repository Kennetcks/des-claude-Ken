if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Tell Deswik the five "3MS S1x" filters were edited (Schedule.UpdateFilter, as the filter dialog does), then recount them.
// Filter definitions only; no task data changes; active filter unchanged. File not saved.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
object G(object o, string n) { var p = o.GetType().GetProperty(n, BF); if (p != null) return p.GetValue(o); return o.GetType().GetField(n, BF)?.GetValue(o); }
string FV(Deswik.Scheduler.Engine.Task t, string f) { try { return System.Convert.ToString(t.GetNullaleFieldFullValue(f)); } catch { return ""; } }
var fl = ((System.Collections.IEnumerable)G(Schedule, "<TaskFilters>k__BackingField")).Cast<object>().ToList();
var upd = Schedule.GetType().GetMethods(BF).First(m => m.Name == "UpdateFilter" && m.GetParameters().Length == 1);
var tif = Schedule.GetType().GetMethods(BF).First(m => m.Name == "TaskIsInFilter" && m.GetParameters().Length == 2);
var mine = fl.Where(x => System.Convert.ToString(G(x, "Name")).StartsWith("3MS S1")).ToList();
foreach (var f in mine) upd.Invoke(Schedule, new[] { f });
Schedule.GetType().GetMethod("HandleFiltersUpdated", BF, null, System.Type.EmptyTypes, null)?.Invoke(Schedule, null);
var cache = G(Schedule, "<matchingFilterCache>k__BackingField");
cache?.GetType().GetMethod("Update", BF, null, System.Type.EmptyTypes, null)?.Invoke(cache, null);
foreach (var f in mine) {
  var n = System.Convert.ToString(G(f, "Name"));
  var names = new HashSet<string>(System.Text.RegularExpressions.Regex.Matches(System.Convert.ToString(G(f, "where")), "Name=\"([^\"]+)\"").Cast<System.Text.RegularExpressions.Match>().Select(m => m.Groups[1].Value));
  int expect = Schedule.Tasks.Count(t => !t.Deleted && FV(t, "SCHEDULED") == "1" && names.Contains(t.Name));
  int hits = Schedule.Tasks.Count(t => !t.Deleted && (bool)tif.Invoke(Schedule, new object[] { t, n }));
  Out.AppendLine($"{n}: {hits} tasks match, expected {expect} ({names.Count} names)");
}
int act = (int)G(Schedule, "m_activeTaskFilter");
Out.AppendLine($"active filter still [{act}] {G(fl[act], "Name")}");
return null;
