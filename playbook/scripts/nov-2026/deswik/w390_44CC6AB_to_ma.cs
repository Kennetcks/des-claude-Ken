if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// W390 RAD W cuts from string 44CC6AB, uid 9896-9900 and 9907-9912: MC Jumbo #1 -> MA Jumbo #1, rate and units kept.
// Old assignment strings go to C:\dswtmp\resource_change_backup_44CC6AB.tsv. Not levelled, not saved.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
const string FROM = "MC Jumbo #1", TO = "MA Jumbo #1";
var want = new[] { 9896, 9898, 9899, 9900, 9907, 9908, 9909, 9910, 9911, 9912 };
var tasks = Schedule.Tasks.Where(t => !t.Deleted && want.Contains(System.Convert.ToInt32(t.UniqueId))).OrderBy(t => t.Start).ToList();
var sb = new StringBuilder(); var uids = new List<int>(); var strs = new List<string>();
foreach (var t in tasks) {
  if (t.Name != "W390 RAD W" || t.PhysicalResource != FROM) { Out.AppendLine($"  skipped {t.Name} uid={t.UniqueId}: on '{t.PhysicalResource}'"); continue; }
  var det = t.ResourceAssignmentDetails;
  sb.AppendLine($"{t.Id}\t{t.UniqueId}\t{t.Name}\t{det}");
  uids.Add(System.Convert.ToInt32(t.UniqueId)); strs.Add(det.Replace(FROM + ":", TO + ":"));
}
System.IO.Directory.CreateDirectory(@"C:\dswtmp");
System.IO.File.WriteAllText(@"C:\dswtmp\resource_change_backup_44CC6AB.tsv", sb.ToString());
var bu = Schedule.GetType().GetMethods(BF).First(m => m.Name == "BatchUpdateResourceAssignmentDetails" && m.GetParameters().Length == 3);
if (uids.Count > 0) bu.Invoke(Schedule, new object[] { uids, strs.ToArray(), true });
var mc = Schedule.GetResources().NonDeletedResources.First(r => r.Name == FROM); foreach (var t in tasks) if (t.ResourceAssignmentDetails.Contains(TO + ":") && t.ResourceAssignmentDetails.Contains(FROM + ":")) t.ClearResourceAssignments(mc);
string FV(Deswik.Scheduler.Engine.Task t, string f) { try { return System.Convert.ToString(t.GetNullaleFieldFullValue(f)); } catch { return ""; } }
foreach (var t in tasks) Out.AppendLine($"  Rate={FV(t, "Rate")}");
foreach (var t in tasks) Out.AppendLine($"  {t.Name} uid={t.UniqueId}\t{t.Start:dd/MM/yy HH:mm}-{t.Finish:dd/MM/yy HH:mm}\t{t.ResourceAssignmentDetails}");
Out.AppendLine($"changed {uids.Count} of {tasks.Count} tasks to {TO}");
return null;
