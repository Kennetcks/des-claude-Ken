if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// H ore drives (ACT_TYPE OD, name starting H) released by an MLA, or by an OD that is itself MLA-fed: set every round to Priority 1.
// Old priorities written to C:\dswtmp\od_priority_backup.tsv. Set C:\dswtmp\od_priority.txt to "apply" to change; otherwise report only.
// Also prints, per H OD, the release predecessor's finish and the OD start.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
string FV(Deswik.Scheduler.Engine.Task t, string f) { try { return System.Convert.ToString(t.GetNullaleFieldFullValue(f)); } catch { return ""; } }
var apply = System.IO.File.Exists(@"C:\dswtmp\od_priority.txt") && System.IO.File.ReadAllText(@"C:\dswtmp\od_priority.txt").Trim() == "apply";
var live = Schedule.Tasks.Where(t => !t.Deleted && FV(t, "SCHEDULED") == "1").ToList();
var byUid = live.ToDictionary(t => System.Convert.ToInt64(t.UniqueId));
long V(object o, string p) => System.Convert.ToInt64(o.GetType().GetProperty(p, BF).GetValue(o));
List<Deswik.Scheduler.Engine.Task> Preds(Deswik.Scheduler.Engine.Task t) => ((System.Collections.IEnumerable)t.Predecessors).Cast<object>().Where(o => byUid.ContainsKey(V(o, "FromNumeric"))).Select(o => byUid[V(o, "FromNumeric")]).ToList();
var ods = live.Where(t => FV(t, "ACT_TYPE") == "OD" && t.Name.StartsWith("H")).GroupBy(t => t.Name).ToDictionary(g => g.Key, g => g.OrderBy(t => t.Start).ToList());
// The release of an OD heading = predecessors of its rounds from another heading (not JSTP_CBL).
Dictionary<string, List<Deswik.Scheduler.Engine.Task>> rel = ods.ToDictionary(k => k.Key, k => k.Value.SelectMany(t => Preds(t).Where(p => p.Name != t.Name && FV(p, "ACT_TYPE") != "JSTP_CBL")).ToList());
var fed = new HashSet<string>(ods.Keys.Where(k => rel[k].Any(p => FV(p, "ACT_TYPE") == "MLA")));
for (bool grew = true; grew;) { grew = false; foreach (var k in ods.Keys) if (!fed.Contains(k) && rel[k].Any(p => FV(p, "ACT_TYPE") == "OD" && fed.Contains(p.Name))) { fed.Add(k); grew = true; } }
var bak = new System.Text.StringBuilder(); int n = 0;
foreach (var k in ods.Keys.OrderBy(k => ods[k][0].Start)) {
  var rs = ods[k]; var r0 = rel[k].OrderBy(p => p.Finish).FirstOrDefault();
  var change = fed.Contains(k) ? rs.Where(t => FV(t, "Priority") != "1").ToList() : new List<Deswik.Scheduler.Engine.Task>();
  Out.AppendLine($"{(fed.Contains(k) ? "MLA-fed" : "other  ")} {k}: release {r0?.Name} fin {r0?.Finish:dd/MM/yy} -> OD start {rs[0].Start:dd/MM/yy} ({(r0 == null ? 0 : (rs[0].Start - r0.Finish).TotalDays):0} d) pri={string.Join(",", rs.Select(t => FV(t, "Priority")).Distinct())} change {change.Count}");
  foreach (var t in change) { bak.Append($"{t.Id}\t{FV(t, "Priority")}\r\n"); n++; if (apply) t.SetFieldValue("Priority", 1.0, System.Globalization.CultureInfo.InvariantCulture); }
}
if (apply) System.IO.File.WriteAllText(@"C:\dswtmp\od_priority_backup.tsv", bak.ToString());
Out.AppendLine($"{fed.Count} MLA-fed OD headings, {n} rounds {(apply ? "set" : "would be set")} to priority 1");
return null;
