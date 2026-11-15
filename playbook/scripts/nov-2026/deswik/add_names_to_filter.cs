if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Add task names to an existing "Name list" filter (first line of C:\dswtmp\filter_add.txt = filter name, other lines = task names),
// rebuild its where clause as (SCHEDULED=1 And Name=...) Or ..., refresh it and keep it active. No task data changes. File not saved.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
object G(object o, string n) { var p = o.GetType().GetProperty(n, BF); if (p != null) return p.GetValue(o); return o.GetType().GetField(n, BF)?.GetValue(o); }
string FV(Deswik.Scheduler.Engine.Task t, string f) { try { return System.Convert.ToString(t.GetNullaleFieldFullValue(f)); } catch { return ""; } }
var lines = System.IO.File.ReadAllLines(@"C:\dswtmp\filter_add.txt").Select(l => l.Trim()).Where(l => l != "").ToList();
// Lines starting with "-" are names to remove from the filter.
var NAME = lines[0]; var add = lines.Skip(1).Where(l => !l.StartsWith("-")).ToList();
var remove = new HashSet<string>(lines.Skip(1).Where(l => l.StartsWith("-")).Select(l => l.Substring(1).Trim()));
var fl = ((System.Collections.IEnumerable)G(Schedule, "<TaskFilters>k__BackingField")).Cast<object>().ToList();
var f = fl.FirstOrDefault(x => System.Convert.ToString(G(x, "Name")) == NAME);
if (f == null) { Out.AppendLine($"filter '{NAME}' not found - nothing changed"); return null; }
var ftype = f.GetType(); var itype = ftype.GetField("mFilter", BF).FieldType.GetGenericArguments()[0];
var ctor = itype.GetConstructor(new[] { typeof(string), typeof(string), typeof(string), typeof(string), typeof(string) });
var existing = System.Text.RegularExpressions.Regex.Matches(System.Convert.ToString(G(f, "where")), "Name=\"([^\"]+)\"").Cast<System.Text.RegularExpressions.Match>().Select(m => m.Groups[1].Value).ToList();
var sched = new HashSet<string>(Schedule.Tasks.Where(t => !t.Deleted && FV(t, "SCHEDULED") == "1").Select(t => t.Name));
var missing = add.Where(n => !sched.Contains(n)).ToList();
var names = existing.Concat(add.Where(n => sched.Contains(n))).Where(n => !remove.Contains(n)).Distinct().OrderBy(n => n).ToList();
var removed = existing.Where(n => remove.Contains(n)).ToList();
if (removed.Count > 0) Out.AppendLine("removed: " + string.Join(", ", removed));
var notThere = remove.Where(n => !existing.Contains(n)).ToList();
if (notThere.Count > 0) Out.AppendLine("not in the filter, nothing to remove: " + string.Join(", ", notThere));
var items = (System.Collections.IList)ftype.GetField("mFilter", BF).GetValue(f); items.Clear();
for (int i = 0; i < names.Count; i++) {
  items.Add(ctor.Invoke(new object[] { "CustomField[\"SCHEDULED\"]", "=", "1", "Double", "And" }));
  items.Add(ctor.Invoke(new object[] { "Name", "=", names[i], "Text", i == names.Count - 1 ? "And" : "Or" }));
}
ftype.GetField("where", BF).SetValue(f, string.Join(" Or ", names.Select(n => $"(NumericCustomField[\"SCHEDULED\"]=1 And Name=\"{n}\")")));
ftype.GetField("recalculateWhereClause", BF).SetValue(f, false);
Schedule.GetType().GetMethods(BF).First(m => m.Name == "UpdateFilter" && m.GetParameters().Length == 1).Invoke(Schedule, new[] { f });
Schedule.GetType().GetMethod("SetAsActiveTaskFilter", BF, null, new[] { typeof(string) }, null).Invoke(Schedule, new object[] { NAME });
Schedule.GetType().GetMethod("HandleFiltersUpdated", BF, null, System.Type.EmptyTypes, null)?.Invoke(Schedule, null);
var cache = G(Schedule, "<matchingFilterCache>k__BackingField");
cache?.GetType().GetMethod("Update", BF, null, System.Type.EmptyTypes, null)?.Invoke(cache, null);
var tif = Schedule.GetType().GetMethods(BF).First(m => m.Name == "TaskIsInFilter" && m.GetParameters().Length == 2);
var hits = Schedule.Tasks.Where(t => !t.Deleted && (bool)tif.Invoke(Schedule, new object[] { t, NAME })).ToList();
int expect = Schedule.Tasks.Count(t => !t.Deleted && FV(t, "SCHEDULED") == "1" && names.Contains(t.Name));
Out.AppendLine($"{NAME}: {existing.Count} -> {names.Count} names; {hits.Count} tasks match (expected {expect}); active filter [{G(Schedule, "m_activeTaskFilter")}]");
if (missing.Count > 0) Out.AppendLine("not found as scheduled tasks: " + string.Join(", ", missing));
foreach (var g in hits.Where(t => add.Contains(t.Name)).GroupBy(t => t.Name + " (" + FV(t, "MINE") + ")"))
  Out.AppendLine($"  added {g.Key}: {g.Count()} tasks, {g.Min(t => t.Start):dd/MM/yy} - {g.Max(t => t.Finish):dd/MM/yy}");
return null;
