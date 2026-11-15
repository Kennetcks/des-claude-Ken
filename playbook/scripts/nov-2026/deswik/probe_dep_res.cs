if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Read-only: show the members of dependency and resource-assignment objects, and how MA Jumbo #1 is assigned on one task.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
string M(System.Type t) => string.Join(", ", t.GetProperties(BF).Where(p => p.GetIndexParameters().Length == 0).Select(p => p.Name + ":" + p.PropertyType.Name));
Out.AppendLine("Schedule members (dep/res): " + string.Join(", ", Schedule.GetType().GetProperties(BF).Where(p => System.Text.RegularExpressions.Regex.IsMatch(p.Name, "(?i)depend|resource|link")).Select(p => p.Name + ":" + p.PropertyType.Name)));
var t0 = Schedule.Tasks.First(t => t.Id == "W_412_RAD_W_6958ea77");
Out.AppendLine("Task members (dep/res/pred/succ): " + string.Join(", ", t0.GetType().GetProperties(BF).Where(p => System.Text.RegularExpressions.Regex.IsMatch(p.Name, "(?i)depend|resource|pred|succ|link|assign")).Select(p => p.Name + ":" + p.PropertyType.Name)));
Out.AppendLine("Task methods (res): " + string.Join(", ", t0.GetType().GetMethods(BF).Where(m => System.Text.RegularExpressions.Regex.IsMatch(m.Name, "(?i)resource|assign")).Select(m => m.Name + "(" + string.Join(",", m.GetParameters().Select(p => p.ParameterType.Name)) + ")").Distinct()));
return null;
