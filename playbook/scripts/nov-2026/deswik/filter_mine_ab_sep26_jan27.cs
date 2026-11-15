if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Add/refresh task filter "Claude - MA+MB Sep26-Jan27": MINE = W or MB*, SCHEDULED = 1, task active any time 01/09/2026 - 31/01/2027.
// Makes it active. No task data changes. File not saved. Previous active filter is printed so it can be re-selected.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
var filters = Schedule.GetType().GetField("<TaskFilters>k__BackingField", BF).GetValue(Schedule);
var fl = ((System.Collections.IEnumerable)filters).Cast<object>().ToList();
object G(object o, string n) { var p = o.GetType().GetProperty(n, BF); if (p != null) return p.GetValue(o); return o.GetType().GetField(n, BF)?.GetValue(o); }
string FV(Deswik.Scheduler.Engine.Task t, string fn) { try { return System.Convert.ToString(t.GetNullaleFieldFullValue(fn)); } catch { return ""; } }
const string NAME = "Claude - MA+MB Sep26-Jan27";
var prevIdx = (int)Schedule.GetType().GetField("m_activeTaskFilter", BF).GetValue(Schedule);
Out.AppendLine($"previous active filter: [{prevIdx}] {G(fl[prevIdx], "Name")}");
// Window and the StartF/FinishF scale, checked against a real task
var w0 = new System.DateTime(2026, 9, 1); var w1 = new System.DateTime(2027, 2, 1);
var probe = Schedule.Tasks.First(x => !x.Deleted && x.Start > new System.DateTime(2000, 1, 1));
long probeF = System.Convert.ToInt64(probe.GetType().GetProperty("StartF", BF)?.GetValue(probe) ?? -1L);
long secs = (long)(probe.Start - new System.DateTime(1970, 1, 1)).TotalSeconds;
long offset = probeF - secs;   // 0 if StartF = seconds since 1970 in schedule time
Out.AppendLine($"probe {probe.Name}: Start {probe.Start:dd/MM/yy HH:mm} StartF {probeF} secs {secs} offset {offset}");
long f0 = (long)(w0 - new System.DateTime(1970, 1, 1)).TotalSeconds + offset;
long f1 = (long)(w1 - new System.DateTime(1970, 1, 1)).TotalSeconds + offset;
// Mines in the schedule: W plus every MB area
var mines = Schedule.Tasks.Where(x => !x.Deleted).Select(x => FV(x, "MINE")).Where(m => m == "W" || m.StartsWith("MB")).Distinct().OrderBy(m => m).ToList();
Out.AppendLine("mines: " + string.Join(", ", mines));
var ftype = fl[0].GetType(); var itype = ftype.GetField("mFilter", BF).FieldType.GetGenericArguments()[0];
var ctor = itype.GetConstructor(new[] { typeof(string), typeof(string), typeof(string), typeof(string), typeof(string) });
var f = fl.FirstOrDefault(x => System.Convert.ToString(G(x, "Name")) == NAME);
if (f == null) {
  f = System.Activator.CreateInstance(ftype, new object[] { NAME });
  var add = filters.GetType().GetMethods().FirstOrDefault(m => m.Name == "Add" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType.IsAssignableFrom(ftype));
  if (add == null) { Out.AppendLine("Filters.Add(Filter) not found - nothing changed"); return null; }
  add.Invoke(filters, new[] { f });
}
// (MINE=m And SCHEDULED=1 And StartF<end And FinishF>start) Or ... for each mine
var items = (System.Collections.IList)ftype.GetField("mFilter", BF).GetValue(f); items.Clear();
var parts = new List<string>();
for (int i = 0; i < mines.Count; i++) {
  var last = i == mines.Count - 1;
  items.Add(ctor.Invoke(new object[] { "CustomField[\"MINE\"]", "=", mines[i], "Combo", "And" }));
  items.Add(ctor.Invoke(new object[] { "CustomField[\"SCHEDULED\"]", "=", "1", "Double", "And" }));
  items.Add(ctor.Invoke(new object[] { "StartF", "<", f1.ToString(), "Long", "And" }));
  items.Add(ctor.Invoke(new object[] { "FinishF", ">", f0.ToString(), "Long", last ? "And" : "Or" }));
  parts.Add($"(CustomField[\"MINE\"]=\"{mines[i]}\" And NumericCustomField[\"SCHEDULED\"]=1 And StartF<{f1} And FinishF>{f0})");
}
var where = string.Join(" Or ", parts);
ftype.GetField("where", BF).SetValue(f, where); ftype.GetField("recalculateWhereClause", BF).SetValue(f, false);
Schedule.GetType().GetMethod("SetAsActiveTaskFilter", BF, null, new[] { typeof(string) }, null).Invoke(Schedule, new object[] { NAME });
var cache = Schedule.GetType().GetField("<matchingFilterCache>k__BackingField", BF).GetValue(Schedule);
cache?.GetType().GetMethod("Update", BF, null, System.Type.EmptyTypes, null)?.Invoke(cache, null);
Out.AppendLine("where: " + G(f, "where"));
Out.AppendLine($"active filter now: [{Schedule.GetType().GetField("m_activeTaskFilter", BF).GetValue(Schedule)}] {NAME}");
// Independent check: tasks that should match, by MINE
var expect = Schedule.Tasks.Where(x => !x.Deleted && mines.Contains(FV(x, "MINE")) && FV(x, "SCHEDULED") == "1" && x.Start < w1 && x.Finish > w0).ToList();
Out.AppendLine($"expected matches: {expect.Count}  (" + string.Join(", ", expect.GroupBy(x => FV(x, "MINE")).OrderBy(g => g.Key).Select(g => $"{g.Key} {g.Count()}")) + ")");
object lst = null; try { var gf = Schedule.GetType().GetMethods(BF).FirstOrDefault(m => m.Name == "GetFilteredTasks" && m.GetParameters().Length == 0); lst = gf?.Invoke(Schedule, null); } catch (System.Exception ex) { Out.AppendLine("GetFilteredTasks: " + ex.InnerException?.Message); }
if (lst is System.Collections.IEnumerable en) {
  var ts = en.Cast<Deswik.Scheduler.Engine.Task>().Where(x => !x.Deleted).ToList();
  Out.AppendLine($"filter shows: {ts.Count} tasks, earliest start {ts.Min(x => x.Start):dd/MM/yy}, latest finish {ts.Max(x => x.Finish):dd/MM/yy}");
}
return null;
