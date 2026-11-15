if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// String 44CC6AB (W390 RAD W #1..#16), currently all mined up #16 -> #1 on MA Jumbo #1. Change to:
// #16 -> #11 up on MC Jumbo #1, #10 -> #5 up on MA Jumbo #1, #1 -> #4 down from the top on MC Jumbo #1 (link #5 -> #4 removed; faces meet).
// Removed links appended to C:\dswtmp\reverse_44CC6AB_backup.tsv. File not saved.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
var ids = new[] { "5ebbbed4", "226bfdb8", "72b4bc7e", "2a175d45", "b6259fdb", "7a17b617", "b1772610", "e8769fe6", "103a6c03", "0110c6b3", "ce5dbb72", "055146e6", "15371efa", "0b288dd7", "c1ed07b2", "5e22c7ae" }.Select(s => "W_390_RAD_W_" + s).ToList();
var byId = Schedule.Tasks.Where(t => !t.Deleted).ToDictionary(t => t.Id);
var R = ids.Select(i => byId[i]).ToList(); // R[0] = #1 ... R[15] = #16
long U(Deswik.Scheduler.Engine.Task t) => System.Convert.ToInt64(t.UniqueId);
Deswik.Scheduler.Engine.Dependency L(int from, int to) => Schedule.Dependencies.FirstOrDefault(x => !x.Deleted && x.From == U(R[from - 1]) && x.To == U(R[to - 1]));
var flip = new[] { L(2, 1), L(3, 2), L(4, 3) }; var drop = L(5, 4);
if (flip.Any(d => d == null) || drop == null) { Out.AppendLine("expected reversed links not found - nothing changed"); return null; }
System.IO.File.AppendAllText(@"C:\dswtmp\reverse_44CC6AB_backup.tsv", "-- removed in split\r\n" + string.Concat(flip.Append(drop).Select(d => $"{d.From}\t{d.To}\t{d.LinkType}\t{d.Lag}\t{d.LayerId}\r\n")));
foreach (var d in flip) {
  Schedule.Delete(d, false, false);
  Schedule.Add(new Deswik.Scheduler.Engine.Dependency(-1, d.To, d.From) { LinkType = d.LinkType, Lag = d.Lag, LayerId = d.LayerId }, false, true, false);
}
Schedule.Delete(drop, false, false);
// Rounds #1-#4 and #11-#16 back to MC Jumbo #1.
var back = R.Take(4).Concat(R.Skip(10)).Where(t => t.ResourceAssignmentDetails.StartsWith("MA Jumbo #1:")).ToList();
var bu = Schedule.GetType().GetMethods(BF).First(m => m.Name == "BatchUpdateResourceAssignmentDetails" && m.GetParameters().Length == 3);
bu.Invoke(Schedule, new object[] { back.Select(t => System.Convert.ToInt32(t.UniqueId)).ToList(), back.Select(t => t.ResourceAssignmentDetails.Replace("MA Jumbo #1:", "MC Jumbo #1:")).ToArray(), true });
var clear = typeof(Deswik.Scheduler.Engine.Task).GetMethod("ClearResourceAssignments", BF);
foreach (var t in back) {
  foreach (var r in ((System.Collections.IEnumerable)t.ResourceAssignments).Cast<object>().Select(a => a.GetType().GetProperty("AssignedResource", BF).GetValue(a)).Where(r => System.Convert.ToString(r.GetType().GetProperty("Name", BF)?.GetValue(r)) == "MA Jumbo #1").ToList()) clear.Invoke(t, new[] { r });
  Schedule.GetType().GetMethod("OnResourceAssignmentsChanged", BF)?.Invoke(Schedule, new object[] { System.Convert.ToInt32(t.UniqueId) });
}
Out.AppendLine($"flipped #1-#4 back to downhill, removed #5 -> #4, moved {back.Count} rounds to MC Jumbo #1; cycles: {Schedule.Dependencies.Count(x => !x.Deleted && x.CausesCycle)}");
return null;
