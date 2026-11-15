// Read-only: writable Task properties that are also snapshot columns; count old-vs-current differences on paired tasks.
var ci = System.Globalization.CultureInfo.InvariantCulture; string C(object v) => v == null ? "" : (v is IFormattable fm ? fm.ToString(null, ci) : v.ToString());
var tl = System.IO.File.ReadAllLines(@"C:\dswtmp\Schedule_Tasks_20261113_090226.tsv"); var th = tl[0].Split('\t').ToList();
var old = new Dictionary<string, string[]>(); foreach (var l in tl.Skip(1)) { var c = l.Split('\t'); old[c[0]] = c; }
var pairs = new Dictionary<string, string>();
foreach (var m in System.IO.File.ReadAllLines(@"C:\dswtmp\Replace_Map.tsv").Skip(1).Select(l => l.Split('\t'))) pairs[m[0]] = m[2];
foreach (var m in System.IO.File.ReadAllLines(@"C:\dswtmp\Replace_FillRest_Applied.tsv").Skip(1).Select(l => l.Split('\t'))) if (!pairs.ContainsKey(m[0])) pairs[m[0]] = m[2];
var props = typeof(Deswik.Scheduler.Engine.Task).GetProperties().Where(p => p.CanWrite && p.GetIndexParameters().Length == 0 && th.Contains(p.Name)).ToList();
Out.AppendLine("writable+in snapshot: " + props.Count + " -> " + string.Join(", ", props.Select(p => p.Name + ":" + p.PropertyType.Name)));
// also: all task properties containing Split/Level for reference
Out.AppendLine("SPLIT/LEVEL props: " + string.Join(", ", typeof(Deswik.Scheduler.Engine.Task).GetProperties().Where(p => p.Name.Contains("Split") || p.Name.Contains("Level")).Select(p => p.Name + (p.CanWrite ? "(w)" : ""))));
var cur = Schedule.Tasks.Where(t => !t.Deleted).ToDictionary(t => t.Id);
var cnt = new Dictionary<string, int>(); var ex = new Dictionary<string, string>();
foreach (var pr in pairs) { if (!cur.TryGetValue(pr.Key, out var t) || !old.TryGetValue(pr.Value, out var o)) continue; foreach (var p in props) { string nv; try { nv = C(p.GetValue(t)); } catch { continue; } var ov = o[th.IndexOf(p.Name)]; if (ov == nv) continue; cnt[p.Name] = cnt.TryGetValue(p.Name, out var k) ? k + 1 : 1; if (!ex.ContainsKey(p.Name)) ex[p.Name] = ov + " -> " + nv; } }
foreach (var kv in cnt.OrderByDescending(k => k.Value)) Out.AppendLine(kv.Value + "\t" + kv.Key + "\t" + (ex[kv.Key].Length > 90 ? ex[kv.Key].Substring(0, 90) : ex[kv.Key]));
return null;
