if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// W390 RAD W cuts on the path 44B8F32 -> 44CC6B3 (uid 9840-9853, 9896, 9898-9900) to a 60 m/mo task rate:
// Rate/TaskRate 60S_m/mo, jumbo units 60/400 = 0.15. Each cut keeps its jumbo. Old values to C:\dswtmp\w390_rate60_backup.tsv.
// Schedule recalculated, not levelled, not saved.
string FV(Deswik.Scheduler.Engine.Task t, string f) { try { return System.Convert.ToString(t.GetNullaleFieldFullValue(f)); } catch { return ""; } }
var ci = System.Globalization.CultureInfo.InvariantCulture;
var uids = Enumerable.Range(9840, 14).Select(i => (long)i).Concat(new long[] { 9896, 9898, 9899, 9900 }).ToList();
var byUid = Schedule.Tasks.Where(t => !t.Deleted).ToDictionary(t => System.Convert.ToInt64(t.UniqueId));
if (uids.Any(u => !byUid.ContainsKey(u) || byUid[u].Name != "W390 RAD W")) { Out.AppendLine("task(s) not found - nothing changed"); return null; }
var sb = new StringBuilder("uid\tRate\tTaskRate\tAssignment\r\n");
foreach (var u in uids) { var t = byUid[u]; sb.AppendLine($"{u}\t{FV(t, "Rate")}\t{FV(t, "TaskRate")}\t{t.ResourceAssignmentDetails}"); }
System.IO.Directory.CreateDirectory(@"C:\dswtmp");
System.IO.File.WriteAllText(@"C:\dswtmp\w390_rate60_backup.tsv", sb.ToString());
foreach (var u in uids) {
  var t = byUid[u];
  foreach (var fld in new[] { "Rate", "TaskRate" }) t.SetFieldValue(fld, "60S_m/mo", ci);
  foreach (Deswik.Scheduler.Engine.ResourceAssignment a in (System.Collections.Generic.IEnumerable<Deswik.Scheduler.Engine.ResourceAssignment>)t.ResourceAssignments) a.Units = 0.15;
}
Schedule.ScheduleCalculator.CalculateSchedule(true, false);
Schedule.ScheduleCalculator.WaitForCalculationsToComplete(false, 120000);
foreach (var u in uids) { var t = byUid[u]; Out.AppendLine($"uid={u}\t{t.Start:dd/MM/yy HH:mm}-{t.Finish:dd/MM/yy HH:mm} ({(t.Finish - t.Start).TotalDays:F2} d)\tRate={FV(t, "Rate")}\t{t.ResourceAssignmentDetails}"); }
return null;
