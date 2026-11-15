if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Read-only: shortest successor path between the two task Ids in C:\dswtmp\path_ends.txt ("fromId,toId"),
// with each task's PhysicalResource, plus how resources are spread over scheduled the mine development.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
string FV(Deswik.Scheduler.Engine.Task t, string f) { try { return System.Convert.ToString(t.GetNullaleFieldFullValue(f)); } catch { return ""; } }
string RES(Deswik.Scheduler.Engine.Task t) { var v = t.GetType().GetProperty("PhysicalResource", BF)?.GetValue(t); return v == null ? "" : (System.Convert.ToString(v.GetType().GetProperty("Name", BF)?.GetValue(v) ?? v)); }
var ends = System.IO.File.ReadAllText(@"C:\dswtmp\path_ends.txt").Trim().Split(',');
var byId = Schedule.Tasks.Where(t => !t.Deleted).ToDictionary(t => t.Id);
// DependencyInformation.To is the successor's UniqueId.
var byUid = Schedule.Tasks.Where(t => !t.Deleted).ToDictionary(t => System.Convert.ToInt64(t.UniqueId));
IEnumerable<Deswik.Scheduler.Engine.Task> Succ(Deswik.Scheduler.Engine.Task t) {
  foreach (var o in (System.Collections.IEnumerable)t.GetType().GetProperty("Successors", BF).GetValue(t)) {
    var to = System.Convert.ToInt64(o.GetType().GetProperty("ToNumeric", BF).GetValue(o));
    if (byUid.ContainsKey(to)) yield return byUid[to];
  }
}
Dictionary<string, string> Bfs(string a, string b) {
  var p = new Dictionary<string, string> { [a] = null }; var q = new Queue<string>(); q.Enqueue(a);
  while (q.Count > 0 && !p.ContainsKey(b)) { var id = q.Dequeue(); foreach (var s in Succ(byId[id])) if (!p.ContainsKey(s.Id)) { p[s.Id] = id; q.Enqueue(s.Id); } }
  return p;
}
var prev = Bfs(ends[0], ends[1]);
if (!prev.ContainsKey(ends[1])) { Out.AppendLine($"no path {ends[0]} -> {ends[1]}; trying reverse"); prev = Bfs(ends[1], ends[0]); if (prev.ContainsKey(ends[0])) ends = new[] { ends[1], ends[0] }; }
if (!prev.ContainsKey(ends[1])) { Out.AppendLine($"no successor path either way"); }
else {
  var path = new List<string>(); for (var id = ends[1]; id != null; id = prev[id]) path.Insert(0, id);
  Out.AppendLine($"path: {path.Count} tasks");
  foreach (var id in path) { var t = byId[id]; Out.AppendLine($"  {t.Name}\t{FV(t, "MINE")}/{FV(t, "LEVEL")}/{FV(t, "ACT_TYPE")}\t{t.Start:dd/MM/yy HH:mm}-{t.Finish:dd/MM/yy HH:mm}\tSCH={FV(t, "SCHEDULED")}\tres={RES(t)}"); }
}
foreach (var g in Schedule.Tasks.Where(t => !t.Deleted && FV(t, "SCHEDULED") == "1" && FV(t, "MINE") == "W" && FV(t, "STO_DEV") == "DEV").GroupBy(t => RES(t)))
  Out.AppendLine($"W DEV tasks with resource '{g.Key}': {g.Count()}");
return null;
