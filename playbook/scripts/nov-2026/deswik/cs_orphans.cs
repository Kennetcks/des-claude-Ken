// Read-only: tasks that now have no predecessor but whose old equivalent had one. Also the old predecessors' names.
var ci = System.Globalization.CultureInfo.InvariantCulture;
var tl = System.IO.File.ReadAllLines(@"C:\dswtmp\Schedule_Tasks_20261113_090226.tsv"); var th = tl[0].Split('\t').ToList();
var old = new Dictionary<string, string[]>(); foreach (var l in tl.Skip(1)) { var c = l.Split('\t'); old[c[0]] = c; }
int iName = th.IndexOf("Name"), iStart = th.IndexOf("Start"), iTy = th.IndexOf("DIS Activity Type");
var n2o = new Dictionary<string, string>();
foreach (var id in old.Keys) n2o[id] = id;
foreach (var m in System.IO.File.ReadAllLines(@"C:\dswtmp\Replace_Map.tsv").Skip(1).Select(l => l.Split('\t'))) n2o[m[0]] = m[2];
foreach (var m in System.IO.File.ReadAllLines(@"C:\dswtmp\Replace_FillRest_Applied.tsv").Skip(1).Select(l => l.Split('\t'))) if (!n2o.ContainsKey(m[0])) n2o[m[0]] = m[2];
var oldPred = new Dictionary<string, List<string>>();
foreach (var c in System.IO.File.ReadAllLines(@"C:\dswtmp\Schedule_Dependencies_20261113_090226.tsv").Skip(1).Select(l => l.Split('\t'))) { if (!oldPred.ContainsKey(c[6])) oldPred[c[6]] = new List<string>(); oldPred[c[6]].Add(c[1]); }
var cur = Schedule.Tasks.Where(t => !t.Deleted).ToList(); var uid = cur.ToDictionary(t => t.UniqueId);
var hasPred = new HashSet<int>(Schedule.Dependencies.Where(d => !d.Deleted && uid.ContainsKey(d.From)).Select(d => d.To));
var rep = new StringBuilder("NewTask\tName\tType\tStartNow\tOldTask\tOldStart\tOldPredecessors\n"); int k = 0; var lines = new List<(DateTime s, string txt)>();
foreach (var t in cur)
{
    if (hasPred.Contains(t.UniqueId)) continue;
    if (!n2o.TryGetValue(t.Id, out var oid) || !oldPred.TryGetValue(oid, out var ps) || ps.Count == 0) continue;
    var ty = t.GetFieldValueAndCacheIt("DIS Activity Type");
    var pn = string.Join("; ", ps.Select(p => old.TryGetValue(p, out var r) ? r[iName] + " (" + p + ")" : p));
    rep.Append(t.Id + "\t" + t.Name + "\t" + ty + "\t" + t.Start.ToString("d/MM/yyyy") + "\t" + oid + "\t" + old[oid][iStart] + "\t" + pn + "\n"); k++;
    lines.Add((t.Start, t.Start.ToString("d/MM/yyyy") + "\t" + t.Name + " [" + ty + "]  was " + old[oid][iStart].Substring(0, 10) + "  pred was: " + string.Join("; ", ps.Take(3).Select(p => old.TryGetValue(p, out var r) ? r[iName] : p))));
}
System.IO.File.WriteAllText(@"C:\dswtmp\Replace_Orphans.tsv", rep.ToString(), new UTF8Encoding(true));
Out.AppendLine("ORPHANED TASKS " + k + "  -> C:\\dswtmp\\Replace_Orphans.tsv");
foreach (var x in lines.OrderBy(x => x.s).Take(60)) Out.AppendLine("  " + x.txt);
return null;
