if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// The RAD cuts from 44ABDAC down to 44B8F38 go to a 50 m/mo task rate (Rate/TaskRate 50S_m/mo, MA Jumbo units 0.125):
// W412 RAD W uid 6418 (44ABDAC) and 6416 (44ABDAA), W390 RAD W uid 9834, 9836-9842. The W390 JSTP_CBL task is left alone.
// Then the W412 internal link 6418 -> 6416 is reversed to 6416 -> 6418, so 44ABDAC is mined after 44ABDAA.
// Old values to C:\dswtmp\w412_w390_rate50_backup.tsv. Schedule recalculated, not levelled, not saved.
string FV(Deswik.Scheduler.Engine.Task t, string f) { try { return System.Convert.ToString(t.GetNullaleFieldFullValue(f)); } catch { return ""; } }
var ci = System.Globalization.CultureInfo.InvariantCulture;
var uids = new long[] { 6418, 6416, 9834, 9836, 9837, 9838, 9839, 9840, 9841, 9842 };
var byUid = Schedule.Tasks.Where(t => !t.Deleted).ToDictionary(t => System.Convert.ToInt64(t.UniqueId));
if (uids.Any(u => !byUid.ContainsKey(u))) { Out.AppendLine("task(s) not found - nothing changed"); return null; }
var link = Schedule.Dependencies.FirstOrDefault(x => !x.Deleted && x.From == 6418 && x.To == 6416);
if (link == null) { Out.AppendLine("link 6418 -> 6416 not found - nothing changed"); return null; }
var sb = new StringBuilder("uid\tRate\tTaskRate\tAssignment\r\n");
foreach (var u in uids) { var t = byUid[u]; sb.AppendLine($"{u}\t{FV(t, "Rate")}\t{FV(t, "TaskRate")}\t{t.ResourceAssignmentDetails}"); }
sb.AppendLine($"link\t{link.From}\t{link.To}\t{link.LinkType}\t{link.Lag}\t{link.LayerId}");
System.IO.Directory.CreateDirectory(@"C:\dswtmp");
System.IO.File.WriteAllText(@"C:\dswtmp\w412_w390_rate50_backup.tsv", sb.ToString());
foreach (var u in uids) {
  var t = byUid[u];
  foreach (var fld in new[] { "Rate", "TaskRate" }) t.SetFieldValue(fld, "50S_m/mo", ci);
  foreach (Deswik.Scheduler.Engine.ResourceAssignment a in (System.Collections.Generic.IEnumerable<Deswik.Scheduler.Engine.ResourceAssignment>)t.ResourceAssignments) if (a.ResourceName == "MA Jumbo #1") a.Units = 0.125;
}
Schedule.Delete(link, false, false);
Schedule.Add(new Deswik.Scheduler.Engine.Dependency(-1, link.To, link.From) { LinkType = link.LinkType, Lag = link.Lag, LayerId = link.LayerId }, false, true, false);
Schedule.ScheduleCalculator.CalculateSchedule(true, false);
Schedule.ScheduleCalculator.WaitForCalculationsToComplete(false, 120000);
foreach (var u in new long[] { 6417, 6416, 6418, 9835, 9834, 9836, 9837, 9838, 9839, 9840, 9841, 9842, 9843 }) {
  var t = byUid[u];
  var preds = string.Join(",", Schedule.Dependencies.Where(x => !x.Deleted && x.To == u).Select(x => x.From));
  Out.AppendLine($"uid={u} {t.Name}\t{t.Start:dd/MM/yy HH:mm}-{t.Finish:dd/MM/yy HH:mm} ({(t.Finish - t.Start).TotalDays:F2} d)\tRate={FV(t, "Rate")}\t{t.ResourceAssignmentDetails}\tpreds={preds}");
}
Out.AppendLine("links causing cycles: " + Schedule.Dependencies.Count(x => !x.Deleted && x.CausesCycle));
return null;
