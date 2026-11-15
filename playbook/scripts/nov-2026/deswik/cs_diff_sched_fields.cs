// Read-only: for matched pairs, count differences in scheduling input fields (current state vs old).
var ci = System.Globalization.CultureInfo.InvariantCulture; string C(object v) => v == null ? "" : (v is IFormattable fm ? fm.ToString(null, ci) : v.ToString());
var tl = System.IO.File.ReadAllLines(@"C:\dswtmp\Schedule_Tasks_20261113_090226.tsv"); var th = tl[0].Split('\t').ToList();
var old = new Dictionary<string, string[]>(); foreach (var l in tl.Skip(1)) { var c = l.Split('\t'); old[c[0]] = c; }
var pairs = new Dictionary<string, string>();
foreach (var m in System.IO.File.ReadAllLines(@"C:\dswtmp\Replace_Map.tsv").Skip(1).Select(l => l.Split('\t'))) pairs[m[0]] = m[2];
foreach (var m in System.IO.File.ReadAllLines(@"C:\dswtmp\Replace_FillRest_Applied.tsv").Skip(1).Select(l => l.Split('\t'))) if (!pairs.ContainsKey(m[0])) pairs[m[0]] = m[2];
var F = new List<string> { "TaskGroupName", "Constraint", "ConstraintDate", "DelayDate", "Delay Date", "LevelFromDate", "Deadline", "DistributionDelay", "Group", "IsLockBreaker", "IsStartingBlock", "LevelTask", "TaskShouldBeLeveled", "LevelingCanSplit", "LevelingMaximumDuration", "PercentageComplete", "% Complete", "ActualStart", "ActualFinish", "ActualDrivingQuantity", "Actual Sch_metres", "ResourceSharing", "ResourcePath", "Priority", "Rate", "TaskRate", "MinimumRate", "Calendar", "MP Bench Order", "MP Direction Sequence", "MP Mining Area Number", "MP Resource Name", "SPLIT_FLG", "SLOT_FLG", "Mining Direction", "SEQUENCE", "STAGE", "ResourceAssignmentDetails", "DurationUnit", "IsMilestone", "Notes", "DrivingProperty" }.Where(f => th.Contains(f)).ToList();
var cur = Schedule.ExportTaskData(F).Select(r => ((IEnumerable<object>)r).Select(C).ToArray()).GroupBy(r => r[0]).ToDictionary(g => g.Key, g => g.First());
var cnt = new Dictionary<string, int>(); var ex = new Dictionary<string, string>(); var oldNonBlank = new Dictionary<string, int>();
foreach (var p in pairs)
{
    if (!cur.TryGetValue(p.Key, out var n) || !old.TryGetValue(p.Value, out var o)) continue;
    for (int j = 0; j < F.Count; j++) { var ov = o[th.IndexOf(F[j])]; var nv = n[j + 1]; if (ov != "") oldNonBlank[F[j]] = oldNonBlank.TryGetValue(F[j], out var z) ? z + 1 : 1; if (ov == nv) continue; cnt[F[j]] = cnt.TryGetValue(F[j], out var k) ? k + 1 : 1; if (!ex.ContainsKey(F[j])) ex[F[j]] = ov + " -> " + nv; }
}
Out.AppendLine("pairs " + pairs.Count);
foreach (var f in F) Out.AppendLine((cnt.TryGetValue(f, out var c) ? c : 0) + "\t" + f + "\t(old non-blank " + (oldNonBlank.TryGetValue(f, out var nb) ? nb : 0) + ")\t" + (ex.TryGetValue(f, out var e) ? e : ""));
return null;
