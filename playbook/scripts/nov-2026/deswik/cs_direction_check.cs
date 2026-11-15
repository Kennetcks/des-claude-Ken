// Read-only: for each COMBINED string, compare the mining direction of the new cuts with the old cuts of its source string.
var ci = System.Globalization.CultureInfo.InvariantCulture;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
double N(string s) { double v; return double.TryParse(s, System.Globalization.NumberStyles.Float, ci, out v) ? v : double.NaN; }
var sl = System.IO.File.ReadAllLines(@"C:\dswtmp\TaskSolids_Snapshot_20261113_084151.tsv"); var sh = sl[0].Split('\t').ToList();
int sP = sh.IndexOf("DIS Parent ID"), sN = sh.IndexOf("DIS Activity Number"), sT = sh.IndexOf("DIS Activity Type"), sS = sh.IndexOf("SCHEDULED");
var oldC = new Dictionary<string, List<(int no, double x, double y, double z)>>();
foreach (var l in sl.Skip(1)) { var c = l.Split('\t'); if (c[sT] != "Tunnel" || c[3] == "" || c[sS] != "1") continue; if (!oldC.ContainsKey(c[sP])) oldC[c[sP]] = new List<(int, double, double, double)>(); oldC[c[sP]].Add((int.Parse(c[sN]), (N(c[3]) + N(c[6])) / 2, (N(c[4]) + N(c[7])) / 2, (N(c[5]) + N(c[8])) / 2)); }
var strs = new Dictionary<string, Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (!f.Deleted && f.Layer != null && f.Layer.Name == "0\\06 SITE\\03 ENGINEERING\\01 DEV STRINGS\\COMBINED 12.11") strs[f.Handle] = f;
var newC = new Dictionary<string, List<(int no, double x, double y, double z)>>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || f.Layer.Name != "SCHEDULER\\TASK SOLIDS" || !f.IsPolyface) continue; var p = A(f, "DIS Parent ID"); if (!strs.ContainsKey(p)) continue;
    var vl = f.asPolyface.GetType().GetProperty("VertexList")?.GetValue(f.asPolyface) as System.Collections.IEnumerable; double x0 = 1e18, y0 = 1e18, z0 = 1e18, x1 = -1e18, y1 = -1e18, z1 = -1e18;
    foreach (var q in vl) { var ty = q.GetType(); double x = Convert.ToDouble(ty.GetProperty("x").GetValue(q)), y = Convert.ToDouble(ty.GetProperty("y").GetValue(q)), z = Convert.ToDouble(ty.GetProperty("z").GetValue(q)); x0 = Math.Min(x0, x); y0 = Math.Min(y0, y); z0 = Math.Min(z0, z); x1 = Math.Max(x1, x); y1 = Math.Max(y1, y); z1 = Math.Max(z1, z); }
    if (!newC.ContainsKey(p)) newC[p] = new List<(int, double, double, double)>(); newC[p].Add((int.Parse(A(f, "DIS Activity Number")), (x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2));
}
int same = 0, rev = 0, single = 0; var revList = new List<string>(); var revByType = new Dictionary<string, int>();
foreach (var kv in strs)
{
    var src = A(kv.Value, "SOURCE_STRING"); if (!newC.TryGetValue(kv.Key, out var nc) || !oldC.TryGetValue(src, out var oc)) continue;
    if (nc.Count < 2 || oc.Count < 2) { single++; continue; }
    var n1 = nc.OrderBy(c => c.no).First();
    // map each new cut to nearest old cut number; count rising vs falling steps
    var seq = nc.OrderBy(c => c.no).Select(c => oc.OrderBy(q => Math.Pow(q.x - c.x, 2) + Math.Pow(q.y - c.y, 2) + Math.Pow(q.z - c.z, 2)).First().no).ToList();
    int up = 0, dn = 0; for (int i = 1; i < seq.Count; i++) { if (seq[i] > seq[i - 1]) up++; else if (seq[i] < seq[i - 1]) dn++; }
    if (up >= dn) same++; else { rev++; var at = A(kv.Value, "ACT_TYPE"); revByType[at] = revByType.TryGetValue(at, out var z) ? z + 1 : 1; revList.Add(A(kv.Value, "Name") + " (" + kv.Key + ", " + nc.Count + " cuts, " + (n1.z < nc.Max(c => c.z) - 0.5 ? "now bottom-up" : n1.z > nc.Min(c => c.z) + 0.5 ? "now top-down" : "flat") + ")"); }
}
Out.AppendLine("strings same direction " + same + "  REVERSED " + rev + "  single-cut " + single);
foreach (var kv in revByType.OrderByDescending(k => k.Value)) Out.AppendLine("  " + kv.Key + "\t" + kv.Value);
foreach (var s in revList.OrderBy(s => s)) Out.AppendLine("  REV " + s);
return null;
