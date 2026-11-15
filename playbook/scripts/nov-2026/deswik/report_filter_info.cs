if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Read-only: which task filter is active, and which filter the report views use.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
object G(object o, string n) { if (o == null) return null; var p = o.GetType().GetProperty(n, BF); if (p != null && p.GetIndexParameters().Length == 0) return p.GetValue(o); return o.GetType().GetField(n, BF)?.GetValue(o); }
var fl = ((System.Collections.IEnumerable)G(Schedule, "<TaskFilters>k__BackingField")).Cast<object>().ToList();
int act = (int)G(Schedule, "m_activeTaskFilter");
Out.AppendLine($"active task filter [{act}] {G(fl[act], "Name")}  where: {G(fl[act], "where")}");
foreach (var i in new[] { 190, 191, 228 }) if (i < fl.Count) Out.AppendLine($"  [{i}] {G(fl[i], "Name")}");
var view = ((System.Collections.IEnumerable)G(Schedule, "CrossTabViews")).Cast<object>().First();
foreach (var p in view.GetType().GetProperties(BF)) { if (p.GetIndexParameters().Length > 0) continue; if (System.Text.RegularExpressions.Regex.IsMatch(p.Name, "(?i)filter|display|resource")) Out.AppendLine($"view0.{p.Name} = {p.GetValue(view)}"); }
var disp = G(view, "Display");
if (disp != null) foreach (var p in disp.GetType().GetProperties(BF)) { if (p.GetIndexParameters().Length > 0) continue; object v = null; try { v = p.GetValue(disp); } catch { } if (System.Text.RegularExpressions.Regex.IsMatch(p.Name, "(?i)filter")) Out.AppendLine($"view0.Display.{p.Name} = {v}"); }
int tot = Schedule.Tasks.Count(t => !t.Deleted), inF = 0; var tif = Schedule.GetType().GetMethods(BF).First(m => m.Name == "TaskIsInFilter" && m.GetParameters().Length == 2);
foreach (var t in Schedule.Tasks.Where(t => !t.Deleted)) if ((bool)tif.Invoke(Schedule, new object[] { t, System.Convert.ToString(G(fl[act], "Name")) })) inF++;
Out.AppendLine($"tasks {tot}, in active filter {inF}");
return null;
