// Assign resources to the new W368/W390 tasks by copying the assignment of an existing task of the same type:
//   EWD, RAD -> as W_368_RAD_N_de6190e7 (Jumbo #1, effort 0.24 @ 370/mo)
//   RAR      -> as W_348_RAR_-_30f1fbda (STOPING 1.00 @ 750/d)
//   SLOT     -> as W_368_SLOT_S_74fe32a6 (RHINO 1.00 @ 15/d)
//   LH_RAR   -> as W_348_LH_RAR_-_11612399 (Prod Drill 1.00 @ 250/d)
// Uses Deswik's AssignResourcesToTasks (same routine as the Assign Resources dialog). Skips tasks that already have a resource.
var targets = new Dictionary<string, string[]> {
  ["W_368_RAD_N_de6190e7"] = new[] { __EWDRAD__ },
  ["W_348_RAR_-_30f1fbda"] = new[] { "W_368_RAR_-_853b4a82", "W_390_RAR_-_703dde64" },
  ["W_368_SLOT_S_74fe32a6"] = new[] { "W_368_SLOT_-_82bfcbdf", "W_390_SLOT_-_3b12e817" },
  ["W_348_LH_RAR_-_11612399"] = new[] { "W_368_LH_RAR_-_2731596f", "W_390_LH_RAR_-_f90ebf7d" } };
var types = System.Enum.GetNames(typeof(Deswik.Scheduler.Engine.ResourceAssignmentType));
var resources = Schedule.GetResources().NonDeletedResources;
int ok = 0, skipped = 0;
foreach (var kv in targets) {
  var ex = Schedule.GetTask(kv.Key);
  var ea = ((System.Collections.Generic.IEnumerable<Deswik.Scheduler.Engine.ResourceAssignment>)ex.ResourceAssignments).First();
  var letter = ex.ResourceAssignmentDetails.Split(':')[1];
  var tname = types.Where(n => n.StartsWith(letter)).ToList();
  if (tname.Count != 1) { Out.AppendLine($"{kv.Key}: assignment type '{letter}' is ambiguous ({string.Join("/", types)}) - skipped"); skipped += kv.Value.Length; continue; }
  var atype = (Deswik.Scheduler.Engine.ResourceAssignmentType)System.Enum.Parse(typeof(Deswik.Scheduler.Engine.ResourceAssignmentType), tname[0]);
  var res = resources.First(r => r.Name == ea.ResourceName);
  foreach (var id in kv.Value) {
    var t = Schedule.GetTask(id);
    if (t == null || t.Deleted) { Out.AppendLine(id + ": not found - skipped"); skipped++; continue; }
    if (!string.IsNullOrEmpty(t.PhysicalResource) || ((System.Collections.Generic.IEnumerable<Deswik.Scheduler.Engine.ResourceAssignment>)t.ResourceAssignments).Any()) { Out.AppendLine(id + ": already has " + t.ResourceAssignmentDetails + " - skipped"); skipped++; continue; }
    int assigned = 0, aborted = 0; string msg = ""; var colour = System.Drawing.Color.Empty;
    Schedule.AssignResourcesToTasks(new List<Deswik.Scheduler.Engine.Task> { t }, new List<Deswik.Scheduler.Engine.Resource> { res },
        atype, ea.Rate.ToString(), ea.Units, ea.UnitsMax, "<None>", "<None>", ref assigned, ref aborted, ref msg, ref colour, false, true);
    if (assigned < 1) { Out.AppendLine(id + ": assign failed (" + msg + ")"); skipped++; continue; }
    ok++; Out.AppendLine($"{id} ({t.Name}): {t.ResourceAssignmentDetails}");
  }
}
Schedule.ScheduleCalculator.CalculateSchedule(true, false);
Schedule.ScheduleCalculator.WaitForCalculationsToComplete(false, 120000);
Out.AppendLine($"Assigned {ok}, skipped {skipped}. Undo: clear the resource on these tasks.");
return null;
