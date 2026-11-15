if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Reverse string 44CC6AB (W390 RAD W, rounds #1..#16) so it mines #16 -> #1 uphill, add 44CC65D #21 -> 44CC6AB #16,
// and move the 16 rounds from MC Jumbo #1 to MA Jumbo #1 (same units and rate). Links into #1 are kept.
// Removed links go to C:\dswtmp\reverse_44CC6AB_backup.tsv. File not saved.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
var ids = new[] { "5ebbbed4", "226bfdb8", "72b4bc7e", "2a175d45", "b6259fdb", "7a17b617", "b1772610", "e8769fe6", "103a6c03", "0110c6b3", "ce5dbb72", "055146e6", "15371efa", "0b288dd7", "c1ed07b2", "5e22c7ae" }.Select(s => "W_390_RAD_W_" + s).ToList();
var byId = Schedule.Tasks.Where(t => !t.Deleted).ToDictionary(t => t.Id);
if (ids.Any(i => !byId.ContainsKey(i)) || !byId.ContainsKey("W_390_RAD_W_f2241577")) { Out.AppendLine("task(s) not found - nothing changed"); return null; }
var rounds = ids.Select(i => byId[i]).ToList(); var feeder = byId["W_390_RAD_W_f2241577"];
long U(Deswik.Scheduler.Engine.Task t) => System.Convert.ToInt64(t.UniqueId);
// Find the 15 internal links #k -> #k+1 before changing anything.
var links = new List<Deswik.Scheduler.Engine.Dependency>();
for (int k = 0; k < 15; k++) {
  var d = Schedule.Dependencies.FirstOrDefault(x => !x.Deleted && x.From == U(rounds[k]) && x.To == U(rounds[k + 1]));
  if (d == null) { Out.AppendLine($"link #{k + 1} -> #{k + 2} not found - nothing changed"); return null; }
  links.Add(d);
}
var defaultLayer = Schedule.Dependencies.First(x => !x.Deleted && x.To == U(rounds[0]) && x.From != U(rounds[0]) && !Schedule.Tasks.First(t => U(t) == x.From).Name.Contains("JSTP")).LayerId;
var sb = new StringBuilder("From\tTo\tLinkType\tLag\tLayerId\r\n");
foreach (var d in links) sb.AppendLine($"{d.From}\t{d.To}\t{d.LinkType}\t{d.Lag}\t{d.LayerId}");
System.IO.File.WriteAllText(@"C:\dswtmp\reverse_44CC6AB_backup.tsv", sb.ToString());
foreach (var d in links) {
  Schedule.Delete(d, false, false);
  var r = new Deswik.Scheduler.Engine.Dependency(-1, d.To, d.From) { LinkType = d.LinkType, Lag = d.Lag, LayerId = d.LayerId };
  Schedule.Add(r, false, true, false);
}
var feed = new Deswik.Scheduler.Engine.Dependency(-1, (int)U(feeder), (int)U(rounds[15])) { LinkType = Deswik.Scheduler.API.Contracts.LinkType.FinishStart, LayerId = defaultLayer };
Schedule.Add(feed, false, true, false);
// Resource: swap MC Jumbo #1 for MA Jumbo #1 in each assignment string, then clear the MC assignment.
var bu = Schedule.GetType().GetMethods(BF).First(m => m.Name == "BatchUpdateResourceAssignmentDetails" && m.GetParameters().Length == 3);
var mc = rounds.Where(t => t.ResourceAssignmentDetails.StartsWith("MC Jumbo #1:")).ToList();
System.IO.File.AppendAllText(@"C:\dswtmp\resource_change_backup.tsv", string.Concat(mc.Select(t => $"{t.Id}\t{t.UniqueId}\t{t.Name}\t{t.ResourceAssignmentDetails}\r\n")));
bu.Invoke(Schedule, new object[] { mc.Select(t => System.Convert.ToInt32(t.UniqueId)).ToList(), mc.Select(t => t.ResourceAssignmentDetails.Replace("MC Jumbo #1:", "MA Jumbo #1:")).ToArray(), true });
var clear = typeof(Deswik.Scheduler.Engine.Task).GetMethod("ClearResourceAssignments", BF);
foreach (var t in mc) {
  foreach (var r in ((System.Collections.IEnumerable)t.ResourceAssignments).Cast<object>().Select(a => a.GetType().GetProperty("AssignedResource", BF).GetValue(a)).Where(r => System.Convert.ToString(r.GetType().GetProperty("Name", BF)?.GetValue(r)) == "MC Jumbo #1").ToList()) clear.Invoke(t, new[] { r });
  Schedule.GetType().GetMethod("OnResourceAssignmentsChanged", BF)?.Invoke(Schedule, new object[] { System.Convert.ToInt32(t.UniqueId) });
}
var cyc = Schedule.Dependencies.Where(x => !x.Deleted && x.CausesCycle).Count();
Out.AppendLine($"reversed {links.Count} links, added 44CC65D #21 -> 44CC6AB #16 (layer {defaultLayer}), moved {mc.Count} rounds to MA Jumbo #1; links causing cycles: {cyc}");
return null;
