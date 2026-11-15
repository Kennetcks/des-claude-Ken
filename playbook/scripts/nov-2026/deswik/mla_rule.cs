if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Read-only. the mine decline rule at each access, measured in decline rounds from the round that releases the access (UG Dev-74 link, round k):
//   decline round k+a -> access JSTP_CBL (FS)      and      access first round -> decline round k+b (FS, holds the decline back).
// Prints a and b for every W access, then for every H DEC access lists what exists and the H links the same rule would add.
// MLA-ONLY variant: on the H side only accesses with ACT_TYPE MLA get the links, and every round of those MLA headings is set to Priority 1.
// Set C:\dswtmp\decline_rule.txt to "apply" to add the H links (layer <Default>, lag 0, with undo); anything else = report only.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
string FV(Deswik.Scheduler.Engine.Task t, string f) { try { return System.Convert.ToString(t.GetNullaleFieldFullValue(f)); } catch { return ""; } }
var apply = System.IO.File.Exists(@"C:\dswtmp\decline_rule.txt") && System.IO.File.ReadAllText(@"C:\dswtmp\decline_rule.txt").Trim() == "apply";
var live = Schedule.Tasks.Where(t => !t.Deleted).ToList();
var byUid = live.ToDictionary(t => System.Convert.ToInt64(t.UniqueId));
long V(object o, string p) => System.Convert.ToInt64(o.GetType().GetProperty(p, BF).GetValue(o));
string Lay(object o) => System.Convert.ToString(o.GetType().GetProperty("Layer", BF).GetValue(o));
List<(Deswik.Scheduler.Engine.Task x, object o)> Lk(Deswik.Scheduler.Engine.Task t, string prop, string end) =>
  ((System.Collections.IEnumerable)t.GetType().GetProperty(prop, BF).GetValue(t)).Cast<object>().Where(o => byUid.ContainsKey(V(o, end))).Select(o => (byUid[V(o, end)], o)).ToList();
bool IsDec(Deswik.Scheduler.Engine.Task t, char side) => FV(t, "ACT_TYPE") == "DEC" && t.Name.StartsWith(side.ToString()) && t.Name.Contains(" DEC -");
List<Deswik.Scheduler.Engine.Task> Chain(char side) {
  var all = live.Where(t => IsDec(t, side) && FV(t, "SCHEDULED") == "1").ToList();
  var head = all.Where(t => !Lk(t, "Predecessors", "FromNumeric").Any(l => IsDec(l.x, side))).OrderBy(t => t.Start).ToList();
  List<Deswik.Scheduler.Engine.Task> Walk(Deswik.Scheduler.Engine.Task cur) {
    var w = new List<Deswik.Scheduler.Engine.Task>(); var seen = new HashSet<Deswik.Scheduler.Engine.Task>();
    while (cur != null && seen.Add(cur)) { w.Add(cur); cur = Lk(cur, "Successors", "ToNumeric").Select(l => l.x).Where(x => IsDec(x, side)).OrderBy(x => x.Start).FirstOrDefault(); }
    return w;
  }
  foreach (var h in head) { var w = Walk(h); Out.AppendLine($"  {side} head {h.Name} [{h.Id}] {h.Start:dd/MM/yy}: {w.Count} rounds to {w.Last().Name} [{w.Last().Id}] {w.Last().Finish:dd/MM/yy}"); }
  var c = head.Select(Walk).OrderByDescending(w => w.Count).First();
  Out.AppendLine($"{side} DEC chain: {c.Count} of {all.Count} rounds, heads={head.Count} ({c.First().Name} {c.First().Start:dd/MM/yy} -> {c.Last().Name} {c.Last().Finish:dd/MM/yy})");
  return c;
}
Deswik.Scheduler.Engine.Task Jstp(Deswik.Scheduler.Engine.Task acc) => Lk(acc, "Predecessors", "FromNumeric").Select(l => l.x).FirstOrDefault(x => FV(x, "ACT_TYPE") == "JSTP_CBL");
var pa = new List<int>(); var pb = new List<int>();
foreach (var side in new[] { 'W', 'H' }) {
  var c = Chain(side); var ix = c.Select((t, i) => (t, i)).ToDictionary(p => p.t, p => p.i);
  for (int k = 0; k < c.Count; k++) foreach (var l in Lk(c[k], "Successors", "ToNumeric").Where(l => Lay(l.o).Contains("Dev-74") && !IsDec(l.x, side))) {
    var acc = l.x; var j = Jstp(acc);
    var hold = Lk(acc, "Successors", "ToNumeric").Where(m => ix.ContainsKey(m.x)).Select(m => ix[m.x] - k).ToList();
    var jl = j == null ? new List<int>() : Lk(j, "Predecessors", "FromNumeric").Where(m => ix.ContainsKey(m.x)).Select(m => ix[m.x] - k).ToList();
    var lagD = (acc.Start - c[k].Finish).TotalDays;
    Out.AppendLine($"  {side} k={k} {c[k].Name} fin {c[k].Finish:dd/MM/yy} -> {acc.Name} start {acc.Start:dd/MM/yy} ({lagD:0} d later) | dec->JSTP at k+[{string.Join(",", jl)}] | access->dec hold at k+[{string.Join(",", hold)}]");
    if (side == 'W') { pa.AddRange(jl); pb.AddRange(hold); continue; }
    if (FV(acc, "ACT_TYPE") != "MLA") continue;
    foreach (var r in live.Where(x => x.Name == acc.Name && FV(x, "Priority") != "1")) { Out.AppendLine($"      PRI {r.Id} {FV(r, "Priority")} -> 1"); if (apply) r.SetFieldValue("Priority", 1.0, System.Globalization.CultureInfo.InvariantCulture); }
    int a = pa.GroupBy(x => x).OrderByDescending(g => g.Count()).First().Key, b = pb.GroupBy(x => x).OrderByDescending(g => g.Count()).First().Key;
    if (hold.Count == 0 && k + b < c.Count) {
      Out.AppendLine($"      ADD hold: {acc.Name} [{acc.Id}] -> {c[k + b].Name} [{c[k + b].Id}] (now starts {c[k + b].Start:dd/MM/yy})");
      if (apply) Schedule.Add(new Deswik.Scheduler.Engine.Dependency(-1, System.Convert.ToInt32(acc.UniqueId), System.Convert.ToInt32(c[k + b].UniqueId)) { LinkType = Deswik.Scheduler.API.Contracts.LinkType.FinishStart }, false, true, false);
    }
    if (j != null && jl.Count == 0 && k + a < c.Count) {
      Out.AppendLine($"      ADD jstp: {c[k + a].Name} [{c[k + a].Id}] -> {j.Name} [{j.Id}]");
      if (apply) Schedule.Add(new Deswik.Scheduler.Engine.Dependency(-1, System.Convert.ToInt32(c[k + a].UniqueId), System.Convert.ToInt32(j.UniqueId)) { LinkType = Deswik.Scheduler.API.Contracts.LinkType.FinishStart }, false, true, false);
    }
  }
  if (side == 'W') Out.AppendLine($"W rule: dec->JSTP offsets {string.Join(",", pa)} | hold offsets {string.Join(",", pb)}");
}
if (apply) Out.AppendLine($"applied; cycles: {Schedule.Dependencies.Count(x => !x.Deleted && x.CausesCycle)}");
return null;
