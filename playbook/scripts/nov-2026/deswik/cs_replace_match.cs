// Read-only: match new tunnel tasks to old ones (same source string + ACT_TYPE, nearest solid centre), diff task fields, list missing dependencies.
// Writes C:\dswtmp\Replace_Map.tsv, Replace_FieldDiff.tsv, Replace_MissingDeps.tsv
var ci = System.Globalization.CultureInfo.InvariantCulture;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
double N(string s) { double v; return double.TryParse(s, System.Globalization.NumberStyles.Float, ci, out v) ? v : double.NaN; }
string C(object v) => v == null ? "" : (v is IFormattable fm ? fm.ToString(null, ci) : v.ToString()).Replace("\t", " ").Replace("\r", " ").Replace("\n", " ");
string[] Rd(string p, out List<string> h) { var l = System.IO.File.ReadAllLines(p); h = l[0].Split('\t').ToList(); return l.Skip(1).ToArray(); }
// old solids: key -> centre, type
var sl = Rd(@"C:\dswtmp\TaskSolids_Snapshot_20261113_084151.tsv", out var sh);
int sP = sh.IndexOf("DIS Parent ID"), sN = sh.IndexOf("DIS Activity Number"), sT = sh.IndexOf("DIS Activity Type"), sA = sh.IndexOf("ACT_TYPE");
var oldBySrc = new Dictionary<string, List<(string key, double x, double y, double z, string at)>>();
foreach (var l in sl) { var c = l.Split('\t'); if (c[sT] != "Tunnel" || c[3] == "") continue; var k = c[sP] + "|" + c[sN]; var e = (k, (N(c[3]) + N(c[6])) / 2, (N(c[4]) + N(c[7])) / 2, (N(c[5]) + N(c[8])) / 2, c[sA]); if (!oldBySrc.ContainsKey(c[sP])) oldBySrc[c[sP]] = new List<(string, double, double, double, string)>(); oldBySrc[c[sP]].Add(e); }
// old tasks: key -> row
var tl = Rd(@"C:\dswtmp\Schedule_Tasks_20261113_090226.tsv", out var th);
int tP = th.IndexOf("DIS Parent ID"), tN = th.IndexOf("DIS Activity Number"), tT = th.IndexOf("DIS Activity Type");
var oldTask = new Dictionary<string, string[]>(); var oldIdToKey = new Dictionary<string, string>(); var oldRows = new Dictionary<string, string[]>();
foreach (var l in tl) { var c = l.Split('\t'); oldRows[c[0]] = c; if (c[tT] == "Tunnel") { oldTask[c[tP] + "|" + c[tN]] = c; oldIdToKey[c[0]] = c[tP] + "|" + c[tN]; } }
// new solids from COMBINED strings
var srcOf = new Dictionary<string, string>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (!f.Deleted && f.Layer != null && f.Layer.Name == "0\\06 SITE\\03 ENGINEERING\\01 DEV STRINGS\\COMBINED 12.11") srcOf[f.Handle] = A(f, "SOURCE_STRING");
var newKeyToOldKey = new Dictionary<string, (string ok, double d)>(); var usedOld = new Dictionary<string, (string nk, double d)>(); int noSrc = 0, far = 0;
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || f.Layer.Name != "SCHEDULER\\TASK SOLIDS" || !f.IsPolyface) continue;
    var p = A(f, "DIS Parent ID"); if (!srcOf.TryGetValue(p, out var src)) continue;
    if (!oldBySrc.TryGetValue(src, out var cand)) { noSrc++; continue; }
    var vl = f.asPolyface.GetType().GetProperty("VertexList")?.GetValue(f.asPolyface) as System.Collections.IEnumerable; double x0 = 1e18, y0 = 1e18, z0 = 1e18, x1 = -1e18, y1 = -1e18, z1 = -1e18;
    foreach (var q in vl) { var t = q.GetType(); double x = Convert.ToDouble(t.GetProperty("x").GetValue(q)), y = Convert.ToDouble(t.GetProperty("y").GetValue(q)), z = Convert.ToDouble(t.GetProperty("z").GetValue(q)); x0 = Math.Min(x0, x); y0 = Math.Min(y0, y); z0 = Math.Min(z0, z); x1 = Math.Max(x1, x); y1 = Math.Max(y1, y); z1 = Math.Max(z1, z); }
    double cx = (x0 + x1) / 2, cy = (y0 + y1) / 2, cz = (z0 + z1) / 2; var at = A(f, "ACT_TYPE");
    var best = cand.OrderBy(o => Math.Sqrt((o.x - cx) * (o.x - cx) + (o.y - cy) * (o.y - cy) + (o.z - cz) * (o.z - cz))).First();
    double d = Math.Sqrt((best.x - cx) * (best.x - cx) + (best.y - cy) * (best.y - cy) + (best.z - cz) * (best.z - cz));
    var nk = p + "|" + A(f, "DIS Activity Number");
    if (d > 2.5) { far++; continue; }
    newKeyToOldKey[nk] = (best.key, d);
}
// one-to-one: if two new map to same old, keep closest for field copy (deps still use all)
foreach (var kv in newKeyToOldKey) { if (!usedOld.TryGetValue(kv.Value.ok, out var u) || kv.Value.d < u.d) usedOld[kv.Value.ok] = (kv.Key, kv.Value.d); }
// current tasks
var cur = Schedule.Tasks.Where(t => !t.Deleted).ToList();
var fields = th.Skip(1).ToList();
var rows = Schedule.ExportTaskData(fields).Select(r => ((IEnumerable<object>)r).Select(C).ToArray()).ToList();
var curById = rows.ToDictionary(r => r[0]); var curKeyToId = new Dictionary<string, string>();
foreach (var r in rows) if (r[tT] == "Tunnel") curKeyToId[r[tP] + "|" + r[tN]] = r[0];
// map old task id -> current task id (new tunnel via match, or same id if still present)
var oldToCur = new Dictionary<string, string>();
foreach (var kv in oldRows) if (curById.ContainsKey(kv.Key)) oldToCur[kv.Key] = kv.Key;
var map = new StringBuilder("NewTaskID\tNewKey\tOldTaskID\tOldKey\tDist\n");
foreach (var kv in usedOld) { if (!oldTask.TryGetValue(kv.Key, out var orow) || !curKeyToId.TryGetValue(kv.Value.nk, out var nid)) continue; oldToCur[orow[0]] = nid; map.Append(nid + "\t" + kv.Value.nk + "\t" + orow[0] + "\t" + kv.Key + "\t" + kv.Value.d.ToString("F2", ci) + "\n"); }
// field diffs (old vs new) for matched pairs
var diffCount = new Dictionary<string, int>(); var fd = new StringBuilder("NewTaskID\tOldTaskID\tField\tOld\tNew\n");
foreach (var kv in usedOld)
{
    if (!oldTask.TryGetValue(kv.Key, out var o) || !curKeyToId.TryGetValue(kv.Value.nk, out var nid)) continue; var nr = curById[nid];
    for (int i = 1; i < th.Count; i++) { var a = i < o.Length ? o[i] : ""; var b = i < nr.Length ? nr[i] : ""; if (a == b || a == "") continue; double x = N(a), y = N(b); if (!double.IsNaN(x) && !double.IsNaN(y) && Math.Abs(x - y) < 1e-6) continue; diffCount[th[i]] = diffCount.TryGetValue(th[i], out var n) ? n + 1 : 1; fd.Append(nid + "\t" + o[0] + "\t" + th[i] + "\t" + a + "\t" + b + "\n"); }
}
// dependencies
var byUid = cur.ToDictionary(t => t.UniqueId, t => t.Id);
var have = new HashSet<string>(Schedule.Dependencies.Where(d => !d.Deleted && byUid.ContainsKey(d.From) && byUid.ContainsKey(d.To)).Select(d => byUid[d.From] + ">" + byUid[d.To]));
var dl = Rd(@"C:\dswtmp\Schedule_Dependencies_20261113_090226.tsv", out var dh);
int miss = 0, ok = 0, unm = 0, self = 0; var md = new StringBuilder("FromNew\tToNew\tFromOld\tToOld\tFromName\tToName\tLinkType\tLag\tLayerId\n"); var mdT = new Dictionary<string, int>();
foreach (var l in dl) { var c = l.Split('\t'); if (!oldToCur.TryGetValue(c[1], out var a) || !oldToCur.TryGetValue(c[6], out var b)) { unm++; continue; } if (a == b) { self++; continue; } if (have.Contains(a + ">" + b)) { ok++; continue; } miss++; md.Append(a + "\t" + b + "\t" + c[1] + "\t" + c[6] + "\t" + c[2] + "\t" + c[7] + "\t" + c[10] + "\t" + c[11] + "\t" + c[12] + "\n"); var tk = (oldRows[c[1]][tT]) + "->" + (oldRows[c[6]][tT]); mdT[tk] = mdT.TryGetValue(tk, out var z) ? z + 1 : 1; }
System.IO.File.WriteAllText(@"C:\dswtmp\Replace_Map.tsv", map.ToString(), new UTF8Encoding(true));
System.IO.File.WriteAllText(@"C:\dswtmp\Replace_FieldDiff.tsv", fd.ToString(), new UTF8Encoding(true));
System.IO.File.WriteAllText(@"C:\dswtmp\Replace_MissingDeps.tsv", md.ToString(), new UTF8Encoding(true));
Out.AppendLine("NEW_SOLIDS_MATCHED " + newKeyToOldKey.Count + "  far>2.5m " + far + "  no_source " + noSrc + "  one-to-one pairs " + usedOld.Count);
Out.AppendLine("OLD_TUNNEL_TASKS " + oldTask.Count + "  mapped " + oldToCur.Count(k => oldIdToKey.ContainsKey(k.Key)));
Out.AppendLine("DEPS old " + dl.Length + "  present " + ok + "  missing " + miss + "  unmapped-end " + unm + "  collapsed " + self);
foreach (var kv in mdT.OrderByDescending(k => k.Value)) Out.AppendLine("  MISS " + kv.Key + "\t" + kv.Value);
foreach (var kv in diffCount.OrderByDescending(k => k.Value).Take(40)) Out.AppendLine("  DIFF " + kv.Key + "\t" + kv.Value);
return null;
