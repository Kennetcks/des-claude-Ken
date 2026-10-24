// Assign Jumbo #1 to the 6 new W412 RAD W extension cuts (#12-17), copying the assignment of an existing
// W412 RAD W cut (W_412_RAD_W_82662d4f: effort, rate 370/mo, units 0.2432 = 90/370). Uses Deswik's own
// AssignResourcesToTasks (same routine as the Assign Resources dialog). Skips a cut that already has a resource.
var ids = new[] { "W_412_RAD_W_8cdfdebb", "W_412_RAD_W_83629461", "W_412_RAD_W_7787b5c0", "W_412_RAD_W_94a93b36", "W_412_RAD_W_3a3a4bfd", "W_412_RAD_W_c4a55324" };
var wh = Schedule.GetResources().NonDeletedResources.First(r => r.Name == "Jumbo #1");
var sib = ((System.Collections.Generic.IEnumerable<Deswik.Scheduler.Engine.ResourceAssignment>)Schedule.GetTask("W_412_RAD_W_82662d4f").ResourceAssignments).First(a => a.ResourceName == "Jumbo #1");
int ok = 0, skipped = 0;
foreach (var id in ids) {
    var t = Schedule.GetTask(id);
    if (t == null || t.Deleted) { Out.AppendLine(id + ": not found - skipped"); skipped++; continue; }
    if (!string.IsNullOrEmpty(t.PhysicalResource)) { Out.AppendLine(id + ": already on " + t.PhysicalResource + " - skipped"); skipped++; continue; }
    int assigned = 0, aborted = 0; string msg = ""; var colour = System.Drawing.Color.Empty;
    Schedule.AssignResourcesToTasks(new List<Deswik.Scheduler.Engine.Task> { t }, new List<Deswik.Scheduler.Engine.Resource> { wh },
        Deswik.Scheduler.Engine.ResourceAssignmentType.Effort, sib.Rate.ToString(), sib.Units, sib.UnitsMax,
        "<None>", "<None>", ref assigned, ref aborted, ref msg, ref colour, false, true);
    if (assigned < 1) { Out.AppendLine(id + ": assign failed (" + msg + ")"); skipped++; continue; }
    ok++; Out.AppendLine(id + ": now " + t.ResourceAssignmentDetails);
}
Schedule.ScheduleCalculator.CalculateSchedule(true, false);
Schedule.ScheduleCalculator.WaitForCalculationsToComplete(false, 120000);
Out.AppendLine("Assigned " + ok + ", skipped " + skipped + ". Undo: remove Jumbo #1 from these 6 cuts (t.ClearResourceAssignments).");
return null;
