// Apply the agreed escapeway mapping: rename/relevel 4 NEW strings, write the final Replace_Plan, delete the 12 orphan tasks left on replaced parents.
const bool APPLY = false;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
var ren = new Dictionary<string, Dictionary<string, string>> {
  { "45931DF", new Dictionary<string, string> { { "Name", "W346 EWR -" }, { "LEVEL", "346" }, { "MINE", "W" } } },
  { "45931DD", new Dictionary<string, string> { { "Name", "W368 EWR -" }, { "LEVEL", "368" }, { "MINE", "W" } } },
  { "458EDDC", new Dictionary<string, string> { { "Name", "W346 DSP -" }, { "LEVEL", "346" }, { "MINE", "W" } } },
  { "45931DC", new Dictionary<string, string> { { "Name", "W368 EWD W" }, { "LEVEL", "368" }, { "MINE", "W" } } } };
var extraMap = new Dictionary<string, string> { { "45931E0", "458EE36" }, { "45931DF", "458EE37" }, { "45931DD", "458EE81" }, { "458EDDC", "458EE18,458EE19" }, { "45931DC", "458EDE3" }, { "45931DA", "458EE1A" }, { "45931E1", "458EE17,458EE16" } };
// cost centre for the renamed ones from the old string's tasks
var tl = System.IO.File.ReadAllLines(@"C:\dswtmp\Schedule_Tasks_20261113_100751.tsv"); var th = tl[0].Split('\t').ToList(); int iP = th.IndexOf("DIS Parent ID"), iC = th.IndexOf("COST_CENTRE");
var ccOf = tl.Skip(1).Select(l => l.Split('\t')).GroupBy(c => c[iP]).ToDictionary(g => g.Key, g => g.First()[iC]);
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || !ren.ContainsKey(f.Handle)) continue; var set = ren[f.Handle];
    var op = extraMap[f.Handle].Split(',')[0]; if (ccOf.TryGetValue(op, out var cc) && cc != "") set["COST_CENTRE"] = cc;
    foreach (var kv in set) { if (A(f, kv.Key) == kv.Value) continue; Out.AppendLine(f.Handle + " " + kv.Key + ": '" + A(f, kv.Key) + "' -> '" + kv.Value + "'"); if (APPLY) f.AttributeValueSet(kv.Key, kv.Value); }
}
var plan = System.IO.File.ReadAllLines(@"C:\dswtmp\Replace_Plan.tsv").Skip(1).Select(l => l.Split('\t')).ToList();
var sb = new StringBuilder("NewHandle\tOld\tDonorCut\tKind\n");
foreach (var c in plan) { string old = c[1], kind = c[1] == "NEW" ? "new" : "geom"; if (extraMap.ContainsKey(c[0])) { old = extraMap[c[0]]; kind = c[0] == "45931E1" ? "geom" : "function"; } sb.Append(c[0] + "\t" + old + "\t" + c[2] + "\t" + kind + "\n"); Out.AppendLine("PLAN " + c[0] + " -> " + old + " (" + kind + ")"); }
if (APPLY) System.IO.File.WriteAllText(@"C:\dswtmp\Replace_Plan2.tsv", sb.ToString(), new UTF8Encoding(true));
var olds = plan.Where(c => c[1] != "NEW").Select(c => c[1]).ToHashSet();
var orph = Schedule.Tasks.Where(t => !t.Deleted && olds.Contains(t.GetFieldValueAndCacheIt("DIS Parent ID"))).ToList();
Out.AppendLine("orphan tasks to delete: " + orph.Count + " -> " + string.Join(", ", orph.Select(t => t.Name)));
if (APPLY) { foreach (var t in orph) Schedule.Delete(t); Schedule.ScheduleCalculator.CalculateSchedule(true, false); Schedule.ScheduleCalculator.WaitForCalculationsToComplete(false, 300000); Out.AppendLine("deleted; tasks now " + Schedule.Tasks.Count(t => !t.Deleted)); }
return null;
