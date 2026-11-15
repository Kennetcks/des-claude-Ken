// Set SCHEDULED back to the old value where a current task maps to an old task that had SCHEDULED 0.
// Mapping: Map2.tsv (first replace) + COMBINED/NEW string SOURCE_STRING whole-string check. Sets the task field, the task solid attribute
// and, when every cut of a string is 0, the string attribute too.
const bool APPLY = false;
var ci = System.Globalization.CultureInfo.InvariantCulture;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
var tl = System.IO.File.ReadAllLines(@"C:\dswtmp\Schedule_Tasks_20261113_090226.tsv"); var th = tl[0].Split('\t').ToList();
var old = tl.Skip(1).Select(l => l.Split('\t')).ToDictionary(c => c[0]); int iS = th.IndexOf("SCHEDULED"), iN = th.IndexOf("Name");
var map = System.IO.File.ReadAllLines(@"C:\dswtmp\Map2.tsv").Skip(1).Select(l => l.Split('\t')).ToDictionary(c => c[0], c => c[1]);
var cur = Schedule.Tasks.Where(t => !t.Deleted).ToList();
string F(Deswik.Scheduler.Engine.Task t, string f) => t.GetFieldValueAndCacheIt(f) ?? "";
var fix = new List<Deswik.Scheduler.Engine.Task>();
foreach (var t in cur) { if (!map.TryGetValue(t.Id, out var o) || !old.TryGetValue(o, out var r)) continue; if (r[iS] == "0" && F(t, "SCHEDULED") != "0") fix.Add(t); }
var byParent = fix.GroupBy(t => F(t, "DIS Parent ID")).ToList();
foreach (var g in byParent) { var all = cur.Count(t => F(t, "DIS Parent ID") == g.Key); Out.AppendLine("parent " + g.Key + "  " + g.First().Name + "  tasks to set 0: " + g.Count() + " of " + all + " on the string"); }
Out.AppendLine((APPLY ? "APPLIED " : "DRY RUN ") + fix.Count + " tasks");
if (!APPLY) return null;
var parents = byParent.Where(g => g.Count() == cur.Count(t => F(t, "DIS Parent ID") == g.Key)).Select(g => g.Key).ToHashSet();
var keys = new HashSet<string>(fix.Select(t => F(t, "DIS Parent ID") + "|" + F(t, "DIS Activity Number") + "|" + F(t, "DIS Activity Type")));
foreach (var t in fix) t.SetFieldValue("SCHEDULED", "0", ci);
int nf = 0; foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null) continue;
    if (f.Layer.Name == "SCHEDULER\\TASK SOLIDS" && keys.Contains(A(f, "DIS Parent ID") + "|" + A(f, "DIS Activity Number") + "|" + A(f, "DIS Activity Type"))) { f.AttributeValueSet("SCHEDULED", "0"); nf++; }
    else if (parents.Contains(f.Handle) && f.IsPolyline) { f.AttributeValueSet("SCHEDULED", "0"); Out.AppendLine("string " + f.Handle + " " + A(f, "Name") + " SCHEDULED -> 0"); }
}
Schedule.ScheduleCalculator.CalculateSchedule(true, false); Schedule.ScheduleCalculator.WaitForCalculationsToComplete(false, 300000);
Out.AppendLine("solids updated " + nf);
foreach (var t in fix.Take(12)) Out.AppendLine("  " + t.Name + " #" + F(t, "DIS Activity Number") + " SCH " + F(t, "SCHEDULED") + " Sch_metres " + F(t, "Sch_metres") + " start " + t.Start.ToString("d/MM/yy"));
return null;
