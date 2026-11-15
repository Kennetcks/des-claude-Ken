// Read-only: new strings against the backup (XY moved? Z change) and the W412 RAD attributes / vertices.
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
var H = new Dictionary<string, Deswik.Graphics.Primaries.Figure>(StringComparer.OrdinalIgnoreCase); foreach (Deswik.Graphics.Primaries.Figure x in App.ModelSpace.Entities) if (!x.Deleted && x.Layer != null) H[x.Handle] = x;
var bk = new Dictionary<string, List<double[]>>(); string cur = null;
foreach (var l in System.IO.File.ReadAllLines(@"C:\dswtmp\w390rad_rl_backup.txt")) { if (l.StartsWith("== ")) { cur = l.Substring(3); bk[cur] = new List<double[]>(); } else if (l.Trim() != "") bk[cur].Add(l.Split(',').Select(s => double.Parse(s, System.Globalization.CultureInfo.InvariantCulture)).ToArray()); }
foreach (var (o, n) in new[] { ("458905E", "4589096"), ("458905F", "4589097"), ("4589060", "4589098") })
{
    var f = H[n]; var p = bk[o]; double mxy = 0, dzmin = 1e9, dzmax = -1e9;
    for (int i = 0; i < f.asPolyline.VertexCount; i++) { var q = f.asPolyline.GetVertex(i); mxy = Math.Max(mxy, Math.Sqrt(Math.Pow(q.x - p[i][0], 2) + Math.Pow(q.y - p[i][1], 2))); dzmin = Math.Min(dzmin, q.z - p[i][2]); dzmax = Math.Max(dzmax, q.z - p[i][2]); }
    Out.AppendLine($"{n} (was {o}, old deleted: {!H.ContainsKey(o)}) [{f.Layer.Name}] {A(f, "ACT_TYPE")} LEVEL {A(f, "LEVEL")}: {f.asPolyline.VertexCount} pts (was {p.Count}), max XY move {mxy * 1000:F1} mm, dz {dzmin:F3}..{dzmax:F3}");
}
var w = H["4589099"]; Out.AppendLine($"4589099 [{w.Layer.Name}] ACT {A(w, "ACT_TYPE")} PROFILE {A(w, "PROFILE")} GSS {A(w, "GSS")} LEVEL {A(w, "LEVEL")} MINE {A(w, "MINE")}");
for (int i = 0; i < w.asPolyline.VertexCount; i++) { var q = w.asPolyline.GetVertex(i); Out.AppendLine($"  v{i} ({q.x:F3},{q.y:F3},{q.z:F3})"); }
return null;
