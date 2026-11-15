if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Read-only: for each handle in C:\dswtmp\find_text.txt (comma separated), print the figure's attributes and its task,
// matched by DIS Parent ID + DIS Activity Number siblings, then the task's predecessors and successors.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
string FV(Deswik.Scheduler.Engine.Task t, string f) { try { return System.Convert.ToString(t.GetNullaleFieldFullValue(f)); } catch { return ""; } }
string H(Deswik.Graphics.Primaries.Figure f) => System.Convert.ToString(f.GetType().GetProperty("Handle", BF)?.GetValue(f));
var hs = System.IO.File.ReadAllText(@"C:\dswtmp\find_text.txt").Trim().Split(',').Select(s => s.Trim().ToUpper()).ToList();
var figs = new Dictionary<string, Deswik.Graphics.Primaries.Figure>(); var all = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) { all.Add(f); var h = H(f)?.ToUpper(); if (h != null && hs.Contains(h)) figs[h] = f; }
var byUid = Schedule.Tasks.Where(t => !t.Deleted).ToDictionary(t => System.Convert.ToInt64(t.UniqueId));
string Links(Deswik.Scheduler.Engine.Task t, string prop, string end) => string.Join("; ", ((System.Collections.IEnumerable)t.GetType().GetProperty(prop, BF).GetValue(t)).Cast<object>()
  .Select(o => byUid.TryGetValue(System.Convert.ToInt64(o.GetType().GetProperty(end, BF).GetValue(o)), out var x) ? $"{x.Name} [{x.Id}] {x.Start:dd/MM/yy HH:mm} ({o.GetType().GetProperty("Layer", BF).GetValue(o)})" : "?"));
foreach (var h in hs) {
  if (!figs.TryGetValue(h, out var fig)) { Out.AppendLine($"== {h}: figure not found"); continue; }
  string A(string k) => System.Convert.ToString(fig.AttributeValueGet(k, ""));
  Out.AppendLine($"== {h}: {A("MINE")} {A("LEVEL")} {A("ACT_TYPE")} parent={A("DIS Parent ID")} act#={A("DIS Activity Number")} {A("Start")} - {A("Finish")} SCH={A("SCHEDULED")} layer={fig.GetType().GetProperty("Layer", BF)?.GetValue(fig)}");
  // The task is the one whose Start/Finish match the figure attributes and whose mine/level/act type match.
  System.DateTime.TryParse(A("Start"), out var st); System.DateTime.TryParse(A("Finish"), out var fi);
  foreach (var t in Schedule.Tasks.Where(t => !t.Deleted && FV(t, "MINE") == A("MINE") && FV(t, "LEVEL") == A("LEVEL") && FV(t, "ACT_TYPE") == A("ACT_TYPE") && System.Math.Abs((t.Start - st).TotalMinutes) < 2 && System.Math.Abs((t.Finish - fi).TotalMinutes) < 2)) {
    Out.AppendLine($"  TASK {t.Name} [{t.Id}] uid={t.UniqueId} {t.Start:dd/MM/yy HH:mm}-{t.Finish:dd/MM/yy HH:mm} res={t.ResourceAssignmentDetails} {t.ConstraintType}");
    Out.AppendLine($"    preds: {Links(t, "Predecessors", "FromNumeric")}");
    Out.AppendLine($"    succs: {Links(t, "Successors", "ToNumeric")}");
  }
}
return null;
