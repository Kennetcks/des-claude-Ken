if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Read-only: for the CAD figure with handle in C:\dswtmp\find_text.txt, print key attributes and the schedule task(s) it belongs to.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
string FV(Deswik.Scheduler.Engine.Task t, string f) { try { return System.Convert.ToString(t.GetNullaleFieldFullValue(f)); } catch { return ""; } }
var h = System.IO.File.ReadAllText(@"C:\dswtmp\find_text.txt").Trim();
Deswik.Graphics.Primaries.Figure fig = null;
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (string.Equals(System.Convert.ToString(f.GetType().GetProperty("Handle", BF)?.GetValue(f)), h, System.StringComparison.OrdinalIgnoreCase)) { fig = f; break; }
if (fig == null) { Out.AppendLine("figure not found"); return null; }
var keys = new[] { "MINE", "LEVEL", "ACT_TYPE", "Description", "DIS Activity ID", "DIS Activity Number", "DIS Parent ID", "Start", "Finish", "SCHEDULED", "DRIVE NUMBER", "AREA", "ZONE" };
foreach (var k in keys) Out.AppendLine($"  {k} = {fig.AttributeValueGet(k, "")}");
var props = string.Join(", ", fig.GetType().GetProperties(BF).Where(p => p.GetIndexParameters().Length == 0 && System.Text.RegularExpressions.Regex.IsMatch(p.Name, "(?i)task|sched|link")).Select(p => { object v = null; try { v = p.GetValue(fig); } catch { } return p.Name + "=" + v; }));
Out.AppendLine("  link props: " + props);
// Match tasks: by name in Description, by GUID in any task field, or by DIS Activity ID = task Id
var guid = System.Convert.ToString(fig.GetType().GetProperty("GUID", BF)?.GetValue(fig));
var desc = System.Convert.ToString(fig.AttributeValueGet("Description", ""));
var dis = System.Convert.ToString(fig.AttributeValueGet("DIS Activity ID", ""));
foreach (var t in Schedule.Tasks.Where(t => !t.Deleted)) {
  var why = new List<string>();
  if (desc != "" && t.Name == desc) why.Add("Name=Description");
  if (dis != "" && (System.Convert.ToString(t.Id) == dis || System.Convert.ToString(t.UniqueId) == dis)) why.Add("Id=DIS Activity ID");
  foreach (var p in t.GetType().GetProperties(BF)) { if (p.GetIndexParameters().Length > 0) continue; if (!System.Text.RegularExpressions.Regex.IsMatch(p.Name, "(?i)guid|graphic|figure|handle")) continue; object v = null; try { v = p.GetValue(t); } catch { continue; } var s = System.Convert.ToString(v); if (s != null && (s.IndexOf(guid ?? "#", System.StringComparison.OrdinalIgnoreCase) >= 0 || s.IndexOf(h, System.StringComparison.OrdinalIgnoreCase) >= 0)) why.Add(p.Name); }
  if (why.Count > 0) Out.AppendLine($"TASK [{t.Id}] {t.Name} ({FV(t, "MINE")}/{FV(t, "ACT_TYPE")}) {t.Start:dd/MM/yy HH:mm}-{t.Finish:dd/MM/yy HH:mm} SCH={FV(t, "SCHEDULED")} via {string.Join(",", why)}");
}
return null;
