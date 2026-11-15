if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Undo w390_44CC6AB_to_ma.cs: put the 10 string-44CC6AB cuts back to the assignment saved in
// C:\dswtmp\resource_change_backup_44CC6AB.tsv (MC Jumbo #1), then clear MA Jumbo #1 from them. Not levelled, not saved.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
var rows = System.IO.File.ReadAllLines(@"C:\dswtmp\resource_change_backup_44CC6AB.tsv").Where(l => l.Trim() != "").Select(l => l.Split('\t')).ToList();
var byUid = Schedule.Tasks.Where(t => !t.Deleted).ToDictionary(t => System.Convert.ToInt32(t.UniqueId));
var uids = rows.Select(r => int.Parse(r[1])).ToList(); var strs = rows.Select(r => r[3]).ToArray();
if (uids.Count != 10 || uids.Any(u => !byUid.ContainsKey(u)) || strs.Any(s => !s.StartsWith("MC Jumbo #1:"))) { Out.AppendLine("backup doesn't look right - nothing changed"); return null; }
var bu = Schedule.GetType().GetMethods(BF).First(m => m.Name == "BatchUpdateResourceAssignmentDetails" && m.GetParameters().Length == 3);
bu.Invoke(Schedule, new object[] { uids, strs, true });
var ma = Schedule.GetResources().NonDeletedResources.First(r => r.Name == "MA Jumbo #1");
foreach (var u in uids) { var t = byUid[u]; if (t.ResourceAssignmentDetails.Contains("MC Jumbo #1:") && t.ResourceAssignmentDetails.Contains("MA Jumbo #1:")) t.ClearResourceAssignments(ma); }
foreach (var u in uids) { var t = byUid[u]; Out.AppendLine($"  uid={u}\t{t.Start:dd/MM/yy HH:mm}-{t.Finish:dd/MM/yy HH:mm}\t{t.ResourceAssignmentDetails}"); }
return null;
