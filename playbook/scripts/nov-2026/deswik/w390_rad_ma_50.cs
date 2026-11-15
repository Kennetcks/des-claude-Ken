if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// W390 RAD W cuts uid 9843-9853 (handles 44B8F38..44B8F4C): MC Jumbo #1 -> MA Jumbo #1 at a 50 m/mo task rate
// (units 50/400 = 0.125; Rate/TaskRate fields 90S_m/mo -> 50S_m/mo). Old values to C:\dswtmp\w390_rad_ma_50_backup.tsv.
// The batch update adds MA next to MC, so MC is then cleared. Not levelled, not saved.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
string FV(Deswik.Scheduler.Engine.Task t, string f) { try { return System.Convert.ToString(t.GetNullaleFieldFullValue(f)); } catch { return ""; } }
const string FROM = "MC Jumbo #1", TO = "MA Jumbo #1", NEWRATE = "50S_m/mo"; const double UNITS = 50.0 / 400.0;
var tasks = Schedule.Tasks.Where(t => !t.Deleted && t.Name == "W390 RAD W" && System.Convert.ToInt32(t.UniqueId) >= 9843 && System.Convert.ToInt32(t.UniqueId) <= 9853).OrderBy(t => t.UniqueId).ToList();
var sb = new StringBuilder(); var uids = new List<int>(); var strs = new List<string>();
foreach (var t in tasks) {
  sb.AppendLine($"{t.Id}\t{t.UniqueId}\t{t.ResourceAssignmentDetails}\t{FV(t, "Rate")}\t{FV(t, "TaskRate")}");
  uids.Add(System.Convert.ToInt32(t.UniqueId)); strs.Add($"{TO}:E:{UNITS.ToString(System.Globalization.CultureInfo.InvariantCulture)}@400/mo:<None>:<None>");
}
System.IO.Directory.CreateDirectory(@"C:\dswtmp");
System.IO.File.WriteAllText(@"C:\dswtmp\w390_rad_ma_50_backup.tsv", sb.ToString());
var bu = Schedule.GetType().GetMethods(BF).First(m => m.Name == "BatchUpdateResourceAssignmentDetails" && m.GetParameters().Length == 3);
bu.Invoke(Schedule, new object[] { uids, strs.ToArray(), true });
var mc = Schedule.GetResources().NonDeletedResources.First(r => r.Name == FROM);
foreach (var t in tasks) {
  if (t.ResourceAssignmentDetails.Contains(TO + ":") && t.ResourceAssignmentDetails.Contains(FROM + ":")) t.ClearResourceAssignments(mc);
  foreach (var fld in new[] { "Rate", "TaskRate" }) { try { t.SetCustomFieldValue(fld, NEWRATE); } catch (System.Exception e) { Out.AppendLine($"  {fld} not set on {t.UniqueId}: {e.Message}"); } }
}
foreach (var t in tasks) Out.AppendLine($"  uid={t.UniqueId}\t{t.Start:dd/MM/yy HH:mm}-{t.Finish:dd/MM/yy HH:mm} ({(t.Finish - t.Start).TotalDays:F2} d)\tRate={FV(t, "Rate")} TaskRate={FV(t, "TaskRate")}\t{t.ResourceAssignmentDetails}");
Out.AppendLine($"{tasks.Count} tasks");
return null;
