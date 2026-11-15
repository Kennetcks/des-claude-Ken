if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Read-only: count tasks the active filter lets through, by MINE, with date span.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
string FV(Deswik.Scheduler.Engine.Task t, string fn) { try { return System.Convert.ToString(t.GetNullaleFieldFullValue(fn)); } catch { return ""; } }
var fl = ((System.Collections.IEnumerable)Schedule.GetType().GetField("<TaskFilters>k__BackingField", BF).GetValue(Schedule)).Cast<object>().ToList();
var idx = (int)Schedule.GetType().GetField("m_activeTaskFilter", BF).GetValue(Schedule); var f = fl[idx];
Out.AppendLine($"active filter: [{idx}] {f.GetType().GetProperty("Name", BF)?.GetValue(f)}");
var mi = Schedule.GetType().GetMethods(BF).Where(m => m.Name == "TaskIsInFilter").ToList();
Out.AppendLine("TaskIsInFilter overloads: " + string.Join(" | ", mi.Select(m => "(" + string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name)) + ")")));
var m2 = mi.FirstOrDefault(m => m.GetParameters().Length == 2);
var hits = new List<Deswik.Scheduler.Engine.Task>();
foreach (var t in Schedule.Tasks.Where(x => !x.Deleted)) { if ((bool)m2.Invoke(Schedule, new object[] { t, System.Convert.ToString(f.GetType().GetProperty("Name", BF)?.GetValue(f)) })) hits.Add(t); }
Out.AppendLine($"tasks in filter: {hits.Count}  (" + string.Join(", ", hits.GroupBy(x => FV(x, "MINE")).OrderBy(g => g.Key).Select(g => $"{g.Key} {g.Count()}")) + ")");
if (hits.Count > 0) Out.AppendLine($"earliest start {hits.Min(x => x.Start):dd/MM/yy}, latest finish {hits.Max(x => x.Finish):dd/MM/yy}; starting before 01/09/26: {hits.Count(x => x.Start < new System.DateTime(2026,9,1))}; finishing after 31/01/27: {hits.Count(x => x.Finish > new System.DateTime(2027,2,1))}");
return null;
