if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Read-only: list task filters with their items and where clauses, and the active one.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
var filters = Schedule.GetType().GetField("<TaskFilters>k__BackingField", BF).GetValue(Schedule);
var fl = ((System.Collections.IEnumerable)filters).Cast<object>().ToList();
object G(object o, string n) { var p = o.GetType().GetProperty(n, BF); if (p != null) return p.GetValue(o); return o.GetType().GetField(n, BF)?.GetValue(o); }
Out.AppendLine($"active filter: [{Schedule.GetType().GetField("m_activeTaskFilter", BF).GetValue(Schedule)}]");
for (int i = 0; i < fl.Count; i++) {
  var f = fl[i];
  Out.AppendLine($"[{i}] {G(f, "Name")}  where: {G(f, "where")}");
  var items = (System.Collections.IEnumerable)f.GetType().GetField("mFilter", BF).GetValue(f);
  foreach (var it in items) {
    var parts = it.GetType().GetFields(BF).Select(x => $"{x.Name}={x.GetValue(it)}");
    Out.AppendLine("     item: " + string.Join("; ", parts));
  }
}
return null;
