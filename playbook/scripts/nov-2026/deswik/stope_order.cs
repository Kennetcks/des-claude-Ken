if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Read-only: for each figure handle in C:\dswtmp\find_text.txt, list the tasks tied to it (same Description/MINE/LEVEL/ACT_TYPE
// and start within 2 min of the figure's Start), plus every task sharing that stope name, with links, resource, priority and constraint.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
string FV(Deswik.Scheduler.Engine.Task t, string f) { try { return System.Convert.ToString(t.GetNullaleFieldFullValue(f)); } catch { return ""; } }
string H(Deswik.Graphics.Primaries.Figure f) => System.Convert.ToString(f.GetType().GetProperty("Handle", BF)?.GetValue(f))?.ToUpper();
var hs = System.IO.File.ReadAllText(@"C:\dswtmp\find_text.txt").Trim().Split(',').Select(s => s.Trim().ToUpper()).ToList();
var byUid = Schedule.Tasks.Where(t => !t.Deleted).ToDictionary(t => System.Convert.ToInt64(t.UniqueId));
string Links(Deswik.Scheduler.Engine.Task t, string prop, string end) => string.Join("; ", ((System.Collections.IEnumerable)t.GetType().GetProperty(prop, BF).GetValue(t)).Cast<object>()
  .Select(o => byUid.TryGetValue(System.Convert.ToInt64(o.GetType().GetProperty(end, BF).GetValue(o)), out var x) ? $"{x.Name}/{FV(x, "ACT_TYPE")} {x.Start:dd/MM/yy HH:mm}-{x.Finish:dd/MM/yy HH:mm} {o.GetType().GetProperty("LinkType", BF).GetValue(o)} lag={o.GetType().GetProperty("Lag", BF).GetValue(o)} ({o.GetType().GetProperty("Layer", BF).GetValue(o)})" : "?"));
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) {
  var h = H(f); if (h == null || !hs.Contains(h)) continue;
  string A(string k) => System.Convert.ToString(f.AttributeValueGet(k, ""));
  Out.AppendLine($"== {h}: {A("MINE")} {A("LEVEL")} {A("ACT_TYPE")} '{A("Description")}' parent={A("DIS Parent ID")} {A("Start")} - {A("Finish")} SCH={A("SCHEDULED")} layer={f.GetType().GetProperty("Layer", BF)?.GetValue(f)}");
  System.DateTime.TryParse(A("Start"), out var st);
  var t = Schedule.Tasks.Where(x => !x.Deleted && FV(x, "MINE") == A("MINE") && FV(x, "LEVEL") == A("LEVEL") && FV(x, "ACT_TYPE") == A("ACT_TYPE") && System.Math.Abs((x.Start - st).TotalMinutes) < 2).FirstOrDefault();
  if (t == null) { Out.AppendLine("  no matching task"); continue; }
  foreach (var x in Schedule.Tasks.Where(x => !x.Deleted && x.Name == t.Name && FV(x, "SCHEDULED") == "1").OrderBy(x => x.Start)) {
    Out.AppendLine($"  {(x == t ? ">>" : "  ")} {x.Name} [{x.Id}] {FV(x, "ACT_TYPE")} {x.Start:dd/MM/yy HH:mm}-{x.Finish:dd/MM/yy HH:mm} res={x.ResourceAssignmentDetails} pri={FV(x, "Priority")} {x.ConstraintType} {(x.ConstraintType.ToString() == "AsSoonAsPossible" ? "" : x.ConstraintDate.ToString("dd/MM/yy"))} t={FV(x, "Sch_tonnes")}");
    if (x == t) { Out.AppendLine($"       preds: {Links(x, "Predecessors", "FromNumeric")}"); Out.AppendLine($"       succs: {Links(x, "Successors", "ToNumeric")}"); }
  }
}
return null;
