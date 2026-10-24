if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Add a task filter "Claude - G390 MLA + successors" (Name = G390 MLA - / G390 ISP - / G390 SMP S1 / W406 DEC -) and make it active.
// No task data changes. The previous active filter (SCH =1) is kept and can be re-selected. File not saved.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
var filters = Schedule.GetType().GetField("<TaskFilters>k__BackingField", BF).GetValue(Schedule);
var fl = ((System.Collections.IEnumerable)filters).Cast<object>().ToList();
object G(object o, string n) { var p = o.GetType().GetProperty(n, BF); if (p != null) return p.GetValue(o); return o.GetType().GetField(n, BF)?.GetValue(o); }
const string NAME = "Claude - G390 MLA + successors";
var prevIdx = (int)Schedule.GetType().GetField("m_activeTaskFilter", BF).GetValue(Schedule);
Out.AppendLine($"previous active filter: [{prevIdx}] {G(fl[prevIdx], "Name")}");
var f = fl.FirstOrDefault(x => System.Convert.ToString(G(x, "Name")) == NAME);
var ftype = fl[0].GetType(); var itype = ftype.GetField("mFilter", BF).FieldType.GetGenericArguments()[0];
if (f == null) {
  f = System.Activator.CreateInstance(ftype, new object[] { NAME });
  var items = (System.Collections.IList)ftype.GetField("mFilter", BF).GetValue(f);
  var ctor = itype.GetConstructor(new[] { typeof(string), typeof(string), typeof(string), typeof(string), typeof(string) });
  foreach (var n in new[] { "G390 MLA -", "G390 ISP -", "G390 SMP S1", "W406 DEC -" }) items.Add(ctor.Invoke(new object[] { "Name", "=", n, "Text", "Or" }));
  ftype.GetField("recalculateWhereClause", BF).SetValue(f, true);
  var add = filters.GetType().GetMethods().FirstOrDefault(m => m.Name == "Add" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType.IsAssignableFrom(ftype));
  if (add == null) { Out.AppendLine("Filters.Add(Filter) not found - nothing changed"); return null; }
  add.Invoke(filters, new[] { f });
}
var w = System.Convert.ToString(ftype.GetProperty("Where", BF)?.GetValue(f) ?? G(f, "where"));
Out.AppendLine($"filter '{NAME}' where: {w}");
fl = ((System.Collections.IEnumerable)filters).Cast<object>().ToList(); int idx = fl.IndexOf(f);
var setAct = Schedule.GetType().GetMethods(BF).Where(m => m.Name == "SetAsActiveTaskFilter").ToList();
Out.AppendLine("SetAsActiveTaskFilter overloads: " + string.Join(" | ", setAct.Select(m => "(" + string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name)) + ")")));
var m1 = setAct.FirstOrDefault(m => m.GetParameters().Length == 1);
if (m1 != null && m1.GetParameters()[0].ParameterType == typeof(int)) m1.Invoke(Schedule, new object[] { idx });
else if (m1 != null && m1.GetParameters()[0].ParameterType.IsAssignableFrom(ftype)) m1.Invoke(Schedule, new[] { f });
else Schedule.GetType().GetField("m_activeTaskFilter", BF).SetValue(Schedule, idx);
var cache = Schedule.GetType().GetField("<matchingFilterCache>k__BackingField", BF).GetValue(Schedule);
cache?.GetType().GetMethod("Update", BF, null, System.Type.EmptyTypes, null)?.Invoke(cache, null);
Out.AppendLine($"active filter now: [{Schedule.GetType().GetField("m_activeTaskFilter", BF).GetValue(Schedule)}] {NAME}");
int c = 0; foreach (var t in Schedule.GetTasks().Where(x => !x.Deleted)) { try { if ((bool)Schedule.GetType().GetMethod("TaskIsInFilter", BF).Invoke(Schedule, new object[] { t, f })) c++; } catch { c = -1; break; } }
Out.AppendLine(c >= 0 ? $"tasks matching: {c}" : "could not count matches (check the Gantt)");
return null;
