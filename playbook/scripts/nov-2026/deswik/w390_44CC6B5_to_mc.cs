if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// W390 RAD W cuts uid 9901-9906 (handles 44CC6B5..44CC6BF): MA Jumbo #1 -> MC Jumbo #1, rate and units kept.
// Old assignment strings go to C:\dswtmp\resource_change_backup_44CC6B5.tsv. Not levelled, not saved.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
const string FROM = "MA Jumbo #1", TO = "MC Jumbo #1";
var want = new[] { 9901, 9902, 9903, 9904, 9905, 9906 };
var tasks = Schedule.Tasks.Where(t => !t.Deleted && want.Contains(System.Convert.ToInt32(t.UniqueId))).OrderBy(t => t.Start).ToList();
var sb = new StringBuilder(); var uids = new List<int>(); var strs = new List<string>();
foreach (var t in tasks) {
  if (t.Name != "W390 RAD W" || t.PhysicalResource != FROM) { Out.AppendLine($"  skipped {t.Name} uid={t.UniqueId}: on '{t.PhysicalResource}'"); continue; }
  var det = t.ResourceAssignmentDetails;
  sb.AppendLine($"{t.Id}\t{t.UniqueId}\t{t.Name}\t{det}");
  uids.Add(System.Convert.ToInt32(t.UniqueId)); strs.Add(det.Replace(FROM + ":", TO + ":"));
}
System.IO.Directory.CreateDirectory(@"C:\dswtmp");
System.IO.File.WriteAllText(@"C:\dswtmp\resource_change_backup_44CC6B5.tsv", sb.ToString());
var bu = Schedule.GetType().GetMethods(BF).First(m => m.Name == "BatchUpdateResourceAssignmentDetails" && m.GetParameters().Length == 3);
if (uids.Count > 0) bu.Invoke(Schedule, new object[] { uids, strs.ToArray(), true });
var fr = Schedule.GetResources().NonDeletedResources.First(r => r.Name == FROM); foreach (var t in tasks) if (t.ResourceAssignmentDetails.Contains(TO + ":") && t.ResourceAssignmentDetails.Contains(FROM + ":")) t.ClearResourceAssignments(fr);
foreach (var t in tasks) Out.AppendLine($"  {t.Name} uid={t.UniqueId}\t{t.Start:dd/MM/yy HH:mm}-{t.Finish:dd/MM/yy HH:mm}\t{t.ResourceAssignmentDetails}");
Out.AppendLine($"changed {uids.Count} of {tasks.Count} tasks to {TO}");
return null;
