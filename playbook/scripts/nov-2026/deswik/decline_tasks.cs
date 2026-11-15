if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Read-only: scheduled decline tasks (any MINE, ACT_TYPE DEC) starting 25/12/2026 - 15/02/2027, in start order.
string FV(Deswik.Scheduler.Engine.Task t, string f) { try { return System.Convert.ToString(t.GetNullaleFieldFullValue(f)); } catch { return ""; } }
foreach (var g in Schedule.Tasks.Where(t => !t.Deleted && FV(t, "SCHEDULED") == "1" && FV(t, "ACT_TYPE") == "DEC" && t.Start >= new System.DateTime(2026, 12, 25) && t.Start < new System.DateTime(2027, 2, 15))
  .GroupBy(t => t.Name + " (" + FV(t, "MINE") + ")").OrderBy(g => g.Min(t => t.Start)))
  Out.AppendLine($"{g.Key}: {g.Min(t => t.Start):dd/MM/yy HH:mm} - {g.Max(t => t.Finish):dd/MM/yy HH:mm}");
return null;
