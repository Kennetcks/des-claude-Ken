// Read-only: fresh map from current tasks to old tasks.
// Tunnel: nearest old cut (centre) on the same source string, no distance cap. Derived: via parent tunnel + ACT_TYPE. Others: same TaskID.
// Writes C:\dswtmp\Map2.tsv (NewTaskID, OldTaskID, Kind, Dist, NewParent, NewActNo).
var ci = System.Globalization.CultureInfo.InvariantCulture;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
string C(object v) => v == null ? "" : (v is IFormattable fm ? fm.ToString(null, ci) : v.ToString());
double N(string s) { double v; return double.TryParse(s, System.Globalization.NumberStyles.Float, ci, out v) ? v : double.NaN; }
var sl = System.IO.File.ReadAllLines(@"C:\dswtmp\TaskSolids_Snapshot_20261113_084151.tsv"); var sh = sl[0].Split('\t').ToList();
int sP = sh.IndexOf("DIS Parent ID"), sN = sh.IndexOf("DIS Activity Number"), sT = sh.IndexOf("DIS Activity Type");
var oldBySrc = new Dictionary<string, List<(string key, double x, double y, double z)>>();
foreach (var l in sl.Skip(1)) { var c = l.Split('\t'); if (c[sT] != "Tunnel" || c[3] == "") continue; if (!oldBySrc.ContainsKey(c[sP])) oldBySrc[c[sP]] = new List<(string, double, double, double)>(); oldBySrc[c[sP]].Add((c[sP] + "|" + c[sN], (N(c[3]) + N(c[6])) / 2, (N(c[4]) + N(c[7])) / 2, (N(c[5]) + N(c[8])) / 2)); }
var tl = System.IO.File.ReadAllLines(@"C:\dswtmp\Schedule_Tasks_20261113_090226.tsv"); var th = tl[0].Split('\t').ToList();
int tP = th.IndexOf("DIS Parent ID"), tN = th.IndexOf("DIS Activity Number"), tT = th.IndexOf("DIS Activity Type"), tA = th.IndexOf("ACT_TYPE"), tD = th.IndexOf("DerivedTasksDetails");
var old = new Dictionary<string, string[]>(); var oldKey = new Dictionary<string, string>();
foreach (var l in tl.Skip(1)) { var c = l.Split('\t'); old[c[0]] = c; if (c[tT] == "Tunnel") oldKey[c[tP] + "|" + c[tN]] = c[0]; }
var srcOf = new Dictionary<string, string>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (!f.Deleted && f.Layer != null && f.Layer.Name == "0\\06 SITE\\03 ENGINEERING\\01 DEV STRINGS\\COMBINED 12.11") srcOf[f.Handle] = A(f, "SOURCE_STRING");
var cur = Schedule.Tasks.Where(t => !t.Deleted).ToList(); var curById = cur.ToDictionary(t => t.Id);
var curKey = new Dictionary<string, string>(); foreach (var t in cur) if (t.GetFieldValueAndCacheIt("DIS Activity Type") == "Tunnel") curKey[t.GetFieldValueAndCacheIt("DIS Parent ID") + "|" + t.GetFieldValueAndCacheIt("DIS Activity Number")] = t.Id;
var map = new Dictionary<string, (string old, string kind, double d)>();
foreach (var t in cur) if (old.ContainsKey(t.Id)) map[t.Id] = (t.Id, "same", 0);
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || f.Layer.Name != "SCHEDULER\\TASK SOLIDS" || !f.IsPolyface) continue;
    var p = A(f, "DIS Parent ID"); if (!srcOf.TryGetValue(p, out var src) || !oldBySrc.TryGetValue(src, out var cand)) continue;
    if (!curKey.TryGetValue(p + "|" + A(f, "DIS Activity Number"), out var nid)) continue;
    var vl = f.asPolyface.GetType().GetProperty("VertexList")?.GetValue(f.asPolyface) as System.Collections.IEnumerable; double x0 = 1e18, y0 = 1e18, z0 = 1e18, x1 = -1e18, y1 = -1e18, z1 = -1e18;
    foreach (var q in vl) { var ty = q.GetType(); double x = Convert.ToDouble(ty.GetProperty("x").GetValue(q)), y = Convert.ToDouble(ty.GetProperty("y").GetValue(q)), z = Convert.ToDouble(ty.GetProperty("z").GetValue(q)); x0 = Math.Min(x0, x); y0 = Math.Min(y0, y); z0 = Math.Min(z0, z); x1 = Math.Max(x1, x); y1 = Math.Max(y1, y); z1 = Math.Max(z1, z); }
    double cx = (x0 + x1) / 2, cy = (y0 + y1) / 2, cz = (z0 + z1) / 2;
    var best = cand.Where(o => oldKey.ContainsKey(o.key)).OrderBy(o => (o.x - cx) * (o.x - cx) + (o.y - cy) * (o.y - cy) + (o.z - cz) * (o.z - cz)).FirstOrDefault(); if (best.key == null) continue;
    map[nid] = (oldKey[best.key], "tunnel", Math.Sqrt((best.x - cx) * (best.x - cx) + (best.y - cy) * (best.y - cy) + (best.z - cz) * (best.z - cz)));
}
char[] sep = { ',', ';', '|', ' ' }; int dm = 0;
foreach (var t in cur)
{
    if (!map.TryGetValue(t.Id, out var mo) || mo.kind != "tunnel") continue; var nds = C(t.GetFieldValueAndCacheIt("DerivedTasksDetails")).Split(sep, StringSplitOptions.RemoveEmptyEntries).Where(curById.ContainsKey).ToList(); if (nds.Count == 0) continue;
    var ods = old[mo.old][tD].Split(sep, StringSplitOptions.RemoveEmptyEntries).Where(old.ContainsKey).ToList();
    foreach (var nd in nds) { if (map.ContainsKey(nd)) continue; var at = curById[nd].GetFieldValueAndCacheIt("ACT_TYPE"); var od = ods.FirstOrDefault(x => old[x][tA] == at); if (od != null) { map[nd] = (od, "derived", 0); dm++; } }
}
var sb = new StringBuilder("NewTaskID\tOldTaskID\tKind\tDist\n"); foreach (var kv in map) sb.Append(kv.Key + "\t" + kv.Value.old + "\t" + kv.Value.kind + "\t" + kv.Value.d.ToString("F2", ci) + "\n");
System.IO.File.WriteAllText(@"C:\dswtmp\Map2.tsv", sb.ToString(), new UTF8Encoding(true));
var unT = cur.Where(t => !map.ContainsKey(t.Id)).GroupBy(t => t.GetFieldValueAndCacheIt("DIS Activity Type")).Select(g => g.Key + "=" + g.Count());
Out.AppendLine("current " + cur.Count + "  mapped " + map.Count + " (same " + map.Values.Count(v => v.kind == "same") + ", tunnel " + map.Values.Count(v => v.kind == "tunnel") + ", derived " + dm + ")  unmapped: " + string.Join(", ", unT));
var ds = map.Values.Where(v => v.kind == "tunnel").Select(v => v.d).OrderBy(x => x).ToList(); Out.AppendLine("tunnel match dist median " + ds[ds.Count / 2].ToString("F2") + "  >2.5 m " + ds.Count(x => x > 2.5) + "  max " + ds.Last().ToString("F1"));
Out.AppendLine("old tunnel tasks used " + map.Values.Where(v => v.kind == "tunnel").Select(v => v.old).Distinct().Count() + " of " + oldKey.Count);
return null;
