// Put tasks in the old schedule's order: SortId ranked by the mapped old task's SortId (ties by cut number).
// Saves current SortIds to C:\dswtmp\SortId_Before_*.tsv first so it can be put back.
const bool APPLY = false;
var tl = System.IO.File.ReadAllLines(@"C:\dswtmp\Schedule_Tasks_20261113_090226.tsv"); var th = tl[0].Split('\t').ToList(); int iS = th.IndexOf("SortId");
var oldSort = new Dictionary<string, int>(); foreach (var l in tl.Skip(1)) { var c = l.Split('\t'); int v; if (int.TryParse(c[iS], out v)) oldSort[c[0]] = v; }
var n2o = System.IO.File.ReadAllLines(@"C:\dswtmp\Map2.tsv").Skip(1).Select(l => l.Split('\t')).ToDictionary(c => c[0], c => c[1]);
var ts = Schedule.Tasks.Where(t => !t.Deleted).ToList();
int ActNo(Deswik.Scheduler.Engine.Task t) { int v; return int.TryParse(t.GetFieldValueAndCacheIt("DIS Activity Number"), out v) ? v : 0; }
double Key(Deswik.Scheduler.Engine.Task t) => n2o.TryGetValue(t.Id, out var o) && oldSort.TryGetValue(o, out var s) ? s : 1e9 + t.SortId;
var ordered = ts.OrderBy(Key).ThenBy(ActNo).ToList();
int moved = 0; for (int i = 0; i < ordered.Count; i++) if (ordered[i].SortId != i + 1) moved++;
Out.AppendLine((APPLY ? "APPLIED" : "DRY RUN") + "  tasks " + ts.Count + "  SortId changes " + moved + "  unmapped at end " + ts.Count(t => Key(t) >= 1e9));
if (!APPLY) return null;
var sb = new StringBuilder("TaskID\tSortId\n"); foreach (var t in ts) sb.Append(t.Id + "\t" + t.SortId + "\n");
System.IO.File.WriteAllText(@"C:\dswtmp\SortId_Before_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".tsv", sb.ToString(), new UTF8Encoding(true));
for (int i = 0; i < ordered.Count; i++) ordered[i].SortId = i + 1;
Schedule.ScheduleCalculator.CalculateSchedule(true, false); Schedule.ScheduleCalculator.WaitForCalculationsToComplete(false, 300000);
Out.AppendLine("done; first 3: " + string.Join(", ", ordered.Take(3).Select(t => t.SortId + " " + t.Name)));
return null;
