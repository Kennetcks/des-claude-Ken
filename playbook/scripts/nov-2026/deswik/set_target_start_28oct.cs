if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Move the schedule target start from 27/10/2026 06:00 to 28/10/2026 06:00 (1 day). Nothing else changes. File not saved.
// Original value before this session's changes: 1791871200 (16/10/2026 06:00).
const long OLD = 1792821600, NEW = OLD + 86400;
if (Schedule.TargetStart != OLD) { Out.AppendLine($"TargetStart is {Schedule.TargetStart}, not the expected {OLD} - nothing changed"); return null; }
Schedule.TargetStart = NEW;
Out.AppendLine($"TargetStart {OLD} -> {Schedule.TargetStart}");
return null;
