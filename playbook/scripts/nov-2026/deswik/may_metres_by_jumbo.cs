if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Read-only: May-2027 development metres (Sch_metres, proportional by time) by resource and filter group,
// the jumbo resources' capacity settings, and the effective metres per unit of jumbo time.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
double Num(Deswik.Scheduler.Engine.Task t, string f) { try { var v = t.GetNullaleFieldFullValue(f); if (v == null) return 0; var m = v.GetType().GetMethod("AsDouble", System.Type.EmptyTypes); return System.Convert.ToDouble(m.Invoke(v, null)); } catch { return 0; } }
string FV(Deswik.Scheduler.Engine.Task t, string f) { try { return System.Convert.ToString(t.GetNullaleFieldFullValue(f)); } catch { return ""; } }
var tif = Schedule.GetType().GetMethods(BF).First(m => m.Name == "TaskIsInFilter" && m.GetParameters().Length == 2);
var a = new System.DateTime(2027, 5, 1); var b = new System.DateTime(2027, 6, 1);
double Frac(Deswik.Scheduler.Engine.Task t) { var dur = (t.Finish - t.Start).TotalSeconds; if (dur <= 0) return (t.Start >= a && t.Start < b) ? 1 : 0; var s = t.Start > a ? t.Start : a; var e = t.Finish < b ? t.Finish : b; return e > s ? (e - s).TotalSeconds / dur : 0; }
var tasks = Schedule.Tasks.Where(t => !t.Deleted && FV(t, "SCHEDULED") == "1" && Frac(t) > 0 && Num(t, "Sch_metres") > 0).ToList();
bool Lat(Deswik.Scheduler.Engine.Task t) => (bool)tif.Invoke(Schedule, new object[] { t, "DEV_Lateral" });
Out.AppendLine($"May-27 metres, all scheduled tasks with Sch_metres: {tasks.Sum(t => Num(t, "Sch_metres") * Frac(t)):0}; DEV_Lateral filter: {tasks.Where(Lat).Sum(t => Num(t, "Sch_metres") * Frac(t)):0}");
foreach (var g in tasks.Where(Lat).GroupBy(t => t.PhysicalResource + " | " + FV(t, "MINE")).OrderByDescending(g => g.Sum(t => Num(t, "Sch_metres") * Frac(t))))
  Out.AppendLine($"  {g.Key}: {g.Sum(t => Num(t, "Sch_metres") * Frac(t)):0} m, {g.Select(t => t.Name).Distinct().Count()} headings");
foreach (var g in tasks.Where(t => !Lat(t)).GroupBy(t => FV(t, "ACT_TYPE") + " | " + t.PhysicalResource)) Out.AppendLine($"  (not DEV_Lateral) {g.Key}: {g.Sum(t => Num(t, "Sch_metres") * Frac(t)):0} m");
// Jumbo time actually used in May: sum of units x hours in May, as jumbo-months.
double hrs = (b - a).TotalHours;
foreach (var j in new[] { "MA Jumbo #1", "MC Jumbo #1" }) {
  var jt = tasks.Where(t => t.PhysicalResource == j).ToList();
  var unitHrs = jt.Sum(t => t.FirstAssignmentUnits * (t.Finish - t.Start).TotalHours * Frac(t));
  var met = jt.Sum(t => Num(t, "Sch_metres") * Frac(t));
  Out.AppendLine($"{j}: {met:0} m, jumbo time used {unitHrs / hrs:0.00} jumbo-months -> {met / (unitHrs / hrs):0} m per full jumbo-month; rates {string.Join(", ", jt.Select(t => t.ResourceAssignmentDetails.Split(':').Skip(2).FirstOrDefault()).Distinct())}");
}
return null;
