if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Move the schedule target start from 16/10/2026 06:00 to 27/10/2026 06:00 (11 days). Nothing else changes. File not saved.
// To undo: set Schedule.TargetStart back to 1791871200.
const long OLD = 1791871200, NEW = OLD + 11L * 86400;
if (Schedule.TargetStart != OLD) { Out.AppendLine($"TargetStart is {Schedule.TargetStart}, not the expected {OLD} - nothing changed"); return null; }
Schedule.TargetStart = NEW;
Out.AppendLine($"TargetStart {OLD} -> {Schedule.TargetStart}");
return null;
