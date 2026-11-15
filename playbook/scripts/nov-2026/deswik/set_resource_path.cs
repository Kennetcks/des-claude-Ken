if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Set PhysicalResource to the resource named on line 2 of C:\dswtmp\path_ends.txt for every task on the successor path
// between the two task Ids on line 1 ("fromId,toId") that currently has a resource. Old values go to C:\dswtmp\resource_change_backup.tsv. File not saved.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
var lines = System.IO.File.ReadAllLines(@"C:\dswtmp\path_ends.txt"); var ends = lines[0].Trim().Split(','); var RESNAME = lines[1].Trim();
var byId = Schedule.Tasks.Where(t => !t.Deleted).ToDictionary(t => t.Id);
var byUid = Schedule.Tasks.Where(t => !t.Deleted).ToDictionary(t => System.Convert.ToInt64(t.UniqueId));
IEnumerable<Deswik.Scheduler.Engine.Task> Succ(Deswik.Scheduler.Engine.Task t) {
  foreach (var o in (System.Collections.IEnumerable)t.GetType().GetProperty("Successors", BF).GetValue(t)) {
    var to = System.Convert.ToInt64(o.GetType().GetProperty("ToNumeric", BF).GetValue(o));
    if (byUid.ContainsKey(to)) yield return byUid[to];
  }
}
var prev = new Dictionary<string, string> { [ends[0]] = null }; var q = new Queue<string>(); q.Enqueue(ends[0]);
while (q.Count > 0 && !prev.ContainsKey(ends[1])) { var id = q.Dequeue(); foreach (var s in Succ(byId[id])) if (!prev.ContainsKey(s.Id)) { prev[s.Id] = id; q.Enqueue(s.Id); } }
if (!prev.ContainsKey(ends[1])) { Out.AppendLine("no path - nothing changed"); return null; }
var path = new List<Deswik.Scheduler.Engine.Task>(); for (var id = ends[1]; id != null; id = prev[id]) path.Insert(0, byId[id]);
var prop = typeof(Deswik.Scheduler.Engine.Task).GetProperty("PhysicalResource", BF);
// PhysicalResource is the resource name (string).
string RN(object r) => System.Convert.ToString(r ?? "");
var names = ((System.Collections.IEnumerable)Schedule.GetResourceNames()).Cast<object>().Select(x => System.Convert.ToString(x)).ToList();
if (!names.Contains(RESNAME)) { Out.AppendLine($"resource '{RESNAME}' not found - nothing changed"); return null; }
// Reference: how other W RAD tasks on the target resource are assigned.
var sample = Schedule.Tasks.FirstOrDefault(t => !t.Deleted && RN(prop.GetValue(t)) == RESNAME && t.Name.StartsWith("W") && t.Name.Contains(" RAD "));
if (sample != null) Out.AppendLine($"reference {sample.Name}: {sample.ResourceAssignmentDetails}");
// Swap only the resource name in each assignment string, so units and rate stay as they are.
var sb = new StringBuilder(); var uids = new List<int>(); var strs = new List<string>();
foreach (var t in path) {
  var old = RN(prop.GetValue(t)); if (old == "") { Out.AppendLine($"  skipped (no resource): {t.Name} {t.Start:dd/MM/yy HH:mm}"); continue; }
  if (old == RESNAME) continue;
  var det = t.ResourceAssignmentDetails;
  sb.AppendLine($"{t.Id}\t{t.UniqueId}\t{t.Name}\t{det}");
  uids.Add(System.Convert.ToInt32(t.UniqueId)); strs.Add(det.Replace(old + ":", RESNAME + ":"));
}
System.IO.File.WriteAllText(@"C:\dswtmp\resource_change_backup.tsv", sb.ToString());
var bu = Schedule.GetType().GetMethods(BF).First(m => m.Name == "BatchUpdateResourceAssignmentDetails" && m.GetParameters().Length == 3);
Out.AppendLine("ids param: " + bu.GetParameters()[0].ParameterType);
if (uids.Count > 0) bu.Invoke(Schedule, new object[] { uids, strs.ToArray(), true });
foreach (var t in path) Out.AppendLine($"  {t.Name}\t{t.Start:dd/MM/yy HH:mm}\t{t.ResourceAssignmentDetails}");
Out.AppendLine($"changed {uids.Count} tasks to {RESNAME}");
return null;
