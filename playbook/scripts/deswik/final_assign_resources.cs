if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW " + System.IO.Path.GetFileName(App.Filename)); return null; }
// Resources for the new FINAL-layer tasks, copied from an existing W task of the same type (same routine as the Assign Resources dialog):
//   RAD/EWD -> as W_368_RAD_N_450cf056 (Jumbo #1 E 0.24 @ 370/mo)   RAR  -> as W_326_RAR_-_932a8198 (STOPING R 1.00 @ 750/d)
//   SLOT    -> as W_368_SLOT_S_74fe32a6 (RHINO R 1.00 @ 15/d)        LH_RAR -> first task with Prod Drill R 1.00 @ 250/d
// Task lists from %TEMP%\dsw\final_res.json. Skips tasks that already have a resource. DEE (W346 RAD W) is not included. File not saved.
var js = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "dsw", "final_res.json")));
string[] L(string k) => js[k].Select(x => (string)x).ToArray();
var lhTemplate = Schedule.GetTasks().FirstOrDefault(x => !x.Deleted && (x.ResourceAssignmentDetails ?? "").StartsWith("Prod Drill:R:1.00@250/d"))?.Id;
if (lhTemplate == null) { Out.AppendLine("no Prod Drill 250/d template found - nothing changed"); return null; }
var targets = new Dictionary<string, string[]> { ["W_368_RAD_N_450cf056"] = L("lat"), ["W_326_RAR_-_932a8198"] = L("rar"), ["W_368_SLOT_S_74fe32a6"] = L("slot"), [lhTemplate] = L("lh") };
var types = System.Enum.GetNames(typeof(Deswik.Scheduler.Engine.ResourceAssignmentType));
var resources = Schedule.GetResources().NonDeletedResources;
int ok = 0, skipped = 0;
foreach (var kv in targets) {
  var ex = Schedule.GetTask(kv.Key);
  var ea = ((System.Collections.Generic.IEnumerable<Deswik.Scheduler.Engine.ResourceAssignment>)ex.ResourceAssignments).First();
  var letter = ex.ResourceAssignmentDetails.Split(':')[1];
  var tname = types.Where(n => n.StartsWith(letter)).ToList();
  if (tname.Count != 1) { Out.AppendLine($"{kv.Key}: assignment type '{letter}' is ambiguous - skipped"); skipped += kv.Value.Length; continue; }
  var atype = (Deswik.Scheduler.Engine.ResourceAssignmentType)System.Enum.Parse(typeof(Deswik.Scheduler.Engine.ResourceAssignmentType), tname[0]);
  var res = resources.First(r => r.Name == ea.ResourceName);
  Out.AppendLine($"template {kv.Key}: {ex.ResourceAssignmentDetails} -> {kv.Value.Length} tasks");
  foreach (var id in kv.Value) {
    var t = Schedule.GetTask(id);
    if (t == null || t.Deleted) { Out.AppendLine(id + ": not found - skipped"); skipped++; continue; }
    if (!string.IsNullOrEmpty(t.PhysicalResource) || ((System.Collections.Generic.IEnumerable<Deswik.Scheduler.Engine.ResourceAssignment>)t.ResourceAssignments).Any()) { Out.AppendLine(id + ": already has " + t.ResourceAssignmentDetails + " - skipped"); skipped++; continue; }
    int assigned = 0, aborted = 0; string msg = ""; var colour = System.Drawing.Color.Empty;
    Schedule.AssignResourcesToTasks(new List<Deswik.Scheduler.Engine.Task> { t }, new List<Deswik.Scheduler.Engine.Resource> { res },
        atype, ea.Rate.ToString(), ea.Units, ea.UnitsMax, "<None>", "<None>", ref assigned, ref aborted, ref msg, ref colour, false, true);
    if (assigned < 1) { Out.AppendLine(id + ": assign failed (" + msg + ")"); skipped++; continue; }
    ok++;
  }
}
Schedule.ScheduleCalculator.CalculateSchedule(true, false);
Schedule.ScheduleCalculator.WaitForCalculationsToComplete(false, 120000);
Out.AppendLine($"Assigned {ok}, skipped {skipped}. File not saved.");
return null;
