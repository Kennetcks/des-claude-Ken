if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Read-only: every ore drive heading (ACT_TYPE OD): first round, priority, links to other headings on the first round
// (with how long after the predecessor it starts) and any links from OD rounds back into other headings.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
string FV(Deswik.Scheduler.Engine.Task t, string f) { try { return System.Convert.ToString(t.GetNullaleFieldFullValue(f)); } catch { return ""; } }
var live = Schedule.Tasks.Where(t => !t.Deleted && FV(t, "SCHEDULED") == "1").ToList();
var byUid = live.ToDictionary(t => System.Convert.ToInt64(t.UniqueId));
long V(object o, string p) => System.Convert.ToInt64(o.GetType().GetProperty(p, BF).GetValue(o));
object P(object o, string p) => o.GetType().GetProperty(p, BF).GetValue(o);
List<(Deswik.Scheduler.Engine.Task x, object o)> Lk(Deswik.Scheduler.Engine.Task t, string prop, string end) =>
  ((System.Collections.IEnumerable)t.GetType().GetProperty(prop, BF).GetValue(t)).Cast<object>().Where(o => byUid.ContainsKey(V(o, end))).Select(o => (byUid[V(o, end)], o)).ToList();
foreach (var g in live.Where(t => FV(t, "ACT_TYPE") == "OD").GroupBy(t => t.Name).OrderBy(g => g.Key.StartsWith("H")).ThenBy(g => g.Min(t => t.Start))) {
  var rs = g.OrderBy(t => t.Start).ToList(); var f = rs[0];
  Out.AppendLine($"=== {g.Key}: {rs.Count} rds {f.Start:dd/MM/yy}-{rs.Last().Finish:dd/MM/yy} res={string.Join(",", rs.Select(t => t.PhysicalResource).Distinct())} pri={string.Join(",", rs.Select(t => FV(t, "Priority")).Distinct())}");
  foreach (var t in rs) {
    foreach (var l in Lk(t, "Predecessors", "FromNumeric").Where(l => l.x.Name != t.Name && FV(l.x, "ACT_TYPE") != "JSTP_CBL"))
      Out.AppendLine($"   rd {rs.IndexOf(t) + 1} {t.Start:dd/MM/yy} <- {l.x.Name} [{FV(l.x, "ACT_TYPE")} pri {FV(l.x, "Priority")}] {l.x.Finish:dd/MM/yy} ({(t.Start - l.x.Finish).TotalDays:0} d) L{P(l.o, "Layer")}");
    foreach (var l in Lk(t, "Successors", "ToNumeric").Where(l => l.x.Name != t.Name && !"LH_Stope,PDR_LH_STOPE,SLOT,STO_CBL,JSTP_CBL".Split(',').Contains(FV(l.x, "ACT_TYPE"))))
      Out.AppendLine($"   rd {rs.IndexOf(t) + 1} {t.Finish:dd/MM/yy} -> {l.x.Name} [{FV(l.x, "ACT_TYPE")}] {l.x.Start:dd/MM/yy} L{P(l.o, "Layer")}");
  }
}
return null;
