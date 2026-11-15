if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Make a task filter active (name read from C:\dswtmp\filter_to_activate.txt). No task data changes. File not saved.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
object G(object o, string n) { var p = o.GetType().GetProperty(n, BF); if (p != null) return p.GetValue(o); return o.GetType().GetField(n, BF)?.GetValue(o); }
var NAME = System.IO.File.ReadAllText(@"C:\dswtmp\filter_to_activate.txt").Trim();
var fl = ((System.Collections.IEnumerable)G(Schedule, "<TaskFilters>k__BackingField")).Cast<object>().ToList();
int prev = (int)G(Schedule, "m_activeTaskFilter");
var f = fl.FirstOrDefault(x => System.Convert.ToString(G(x, "Name")) == NAME);
if (f == null) { Out.AppendLine($"filter '{NAME}' not found - nothing changed"); return null; }
Schedule.GetType().GetMethod("SetAsActiveTaskFilter", BF, null, new[] { typeof(string) }, null).Invoke(Schedule, new object[] { NAME });
var cache = G(Schedule, "<matchingFilterCache>k__BackingField");
cache?.GetType().GetMethod("Update", BF, null, System.Type.EmptyTypes, null)?.Invoke(cache, null);
// Refresh what the Gantt and CAD show (same calls the filter dialog triggers); list what was available.
var refreshed = new List<string>();
var upd = Schedule.GetType().GetMethods(BF).FirstOrDefault(m => m.Name == "UpdateFilter" && m.GetParameters().Length == 1);
if (upd != null) { upd.Invoke(Schedule, new[] { f }); refreshed.Add("UpdateFilter"); }
foreach (var mn in new[] { "HandleFiltersUpdated", "TaskFilterDateRangeUpdated" }) {
  var m = Schedule.GetType().GetMethod(mn, BF, null, System.Type.EmptyTypes, null);
  if (m != null) { m.Invoke(Schedule, null); refreshed.Add(mn); }
}
Out.AppendLine("refresh: " + string.Join(", ", refreshed) + "; filtered-task methods available: " + string.Join(", ", Schedule.GetType().GetMethods(BF).Where(m => System.Text.RegularExpressions.Regex.IsMatch(m.Name, "(?i)filteredtask|applyfilter|refilter")).Select(m => m.Name + "(" + m.GetParameters().Length + ")").Distinct()));
int now = (int)G(Schedule, "m_activeTaskFilter");
var tif = Schedule.GetType().GetMethods(BF).First(m => m.Name == "TaskIsInFilter" && m.GetParameters().Length == 2);
var hits = Schedule.Tasks.Where(t => !t.Deleted && (bool)tif.Invoke(Schedule, new object[] { t, NAME })).ToList();
string FN(int i) => i >= 0 && i < fl.Count ? System.Convert.ToString(G(fl[i], "Name")) : "(none)";
Out.AppendLine($"previous active filter: [{prev}] {FN(prev)}");
Out.AppendLine($"active filter now: [{now}] {FN(now)} - {hits.Count} tasks");
foreach (var g in hits.GroupBy(t => t.Name).OrderBy(g => g.Min(t => t.Start)))
  Out.AppendLine($"  {g.Key}: {g.Count()} tasks, {g.Min(t => t.Start):dd/MM/yy} - {g.Max(t => t.Finish):dd/MM/yy}");
return null;
