if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Read-only: list every heading whose name contains "DEC", with its round count, dates and resource,
// and every link between a decline round and a round of a different heading (in both directions).
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
string FV(Deswik.Scheduler.Engine.Task t, string f) { try { return System.Convert.ToString(t.GetNullaleFieldFullValue(f)); } catch { return ""; } }
var live = Schedule.Tasks.Where(t => !t.Deleted && FV(t, "SCHEDULED") == "1").ToList();
var byUid = live.ToDictionary(t => System.Convert.ToInt64(t.UniqueId));
long V(object o, string p) => System.Convert.ToInt64(o.GetType().GetProperty(p, BF).GetValue(o));
object P(object o, string p) => o.GetType().GetProperty(p, BF).GetValue(o);
foreach (var g in live.Where(t => t.Name.ToUpper().Contains("DEC")).GroupBy(t => t.Name).OrderBy(g => g.Min(t => t.Start))) {
  var rs = g.OrderBy(t => t.Start).ToList();
  Out.AppendLine($"=== {g.Key}: {rs.Count} tasks {rs[0].Start:dd/MM/yy}-{rs.Last().Finish:dd/MM/yy} res={string.Join(",", rs.Select(t => t.PhysicalResource).Distinct())} acts={string.Join(",", rs.Select(t => FV(t, "ACT_TYPE")).Distinct())}");
  for (int i = 0; i < rs.Count; i++) {
    var t = rs[i];
    foreach (var o in ((System.Collections.IEnumerable)t.Successors).Cast<object>())
      if (byUid.TryGetValue(V(o, "ToNumeric"), out var x) && x.Name != t.Name)
        Out.AppendLine($"  #{i + 1} {t.Id} {t.Start:dd/MM/yy} --{P(o, "LinkType")} lag={P(o, "Lag")} L{P(o, "Layer")}--> {x.Name} [{x.Id}] {FV(x, "ACT_TYPE")} {x.Start:dd/MM/yy}");
    foreach (var o in ((System.Collections.IEnumerable)t.Predecessors).Cast<object>())
      if (byUid.TryGetValue(V(o, "FromNumeric"), out var x) && x.Name != t.Name)
        Out.AppendLine($"  #{i + 1} {t.Id} {t.Start:dd/MM/yy} <--{P(o, "LinkType")} lag={P(o, "Lag")} L{P(o, "Layer")}-- {x.Name} [{x.Id}] {FV(x, "ACT_TYPE")} {x.Finish:dd/MM/yy}");
  }
}
return null;
