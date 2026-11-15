// MPA 07 W6390 RAD drop-down: put its top edge on the high section (same height above the decline as the high
// section's up corner, 1:7 along the edge), bottom 1.5 m below; the high section's two edge vertices follow. Dry run unless APPLY.
const bool APPLY = false;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
List<double[]> P3(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
double D2(double[] a, double[] b) => Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2));
Deswik.Graphics.Primaries.Figure dec = null, box = null, hs = null; var radStart = new[] { 8086.18, 63114.33 };
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || A(f, "CLAUDE_MPA07") != "1" || !f.IsPolyline) continue;
    if (f.Layer.Name == "0\\DESIGN DRAFT\\CENTERLINES" && A(f, "ACT_TYPE") == "DEC") dec = f;
    if (f.Layer.Name == "0\\DESIGN DRAFT\\STRIPPING") { var p = P3(f); if (p.Count == 4 && p.Max(q => q[2]) - p.Min(q => q[2]) > 1.2 && p.Min(q => D2(q, radStart)) < 6) box = f; }
}
var B = P3(box);
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
    if (!f.Deleted && f.Layer != null && f.Layer.Name == "0\\DESIGN DRAFT\\STRIPPING" && f.IsPolyline && A(f, "CLAUDE_MPA07") == "1" && f != box && P3(f).Count(q => B.Any(b => D2(b, q) < 0.01)) == 2) hs = f;
var D = P3(dec); var C = new List<double> { 0 }; for (int i = 1; i < D.Count; i++) C.Add(C[i - 1] + D2(D[i], D[i - 1]));
(double s, double z) On(double x, double y)
{
    double bs = 0, bd = 1e9, bz = 0;
    for (int i = 1; i < D.Count; i++) { double vx = D[i][0] - D[i - 1][0], vy = D[i][1] - D[i - 1][1], L = Math.Sqrt(vx * vx + vy * vy); double t = ((x - D[i - 1][0]) * vx + (y - D[i - 1][1]) * vy) / (L * L); double tc = i == 1 ? t : Math.Max(0, Math.Min(1, t)); double tt = Math.Max(0, Math.Min(1, t)); double d = Math.Sqrt(Math.Pow(x - D[i - 1][0] - tt * vx, 2) + Math.Pow(y - D[i - 1][1] - tt * vy, 2)); if (d < bd - 1e-9) { bd = d; bs = C[i - 1] + tc * L; bz = D[i - 1][2] + tc * (D[i][2] - D[i - 1][2]); } }
    return (bs, bz);
}
var H = P3(hs); var edge = Enumerable.Range(0, 4).Where(i => H.Any(q => D2(q, B[i]) < 0.01)).ToList();
int up = edge.OrderBy(i => On(B[i][0], B[i][1]).s).First(), dn = edge.First(i => i != up);
var corner = H.Where(q => B.All(b => D2(b, q) > 0.01)).OrderBy(q => D2(q, B[up])).First();
double h = corner[2] - On(corner[0], corner[1]).z;
var NB = B.Select(q => (double[])q.Clone()).ToList();
NB[up][2] = On(B[up][0], B[up][1]).z + h; NB[dn][2] = NB[up][2] - D2(B[up], B[dn]) / 7.0;
for (int i = 0; i < 4; i++) if (!edge.Contains(i)) NB[i][2] = NB[D2(B[i], B[up]) < D2(B[i], B[dn]) ? up : dn][2] - 1.5;
Out.AppendLine($"high section {h:F3} m above the decline at its up corner; drop-down RLs " + string.Join(", ", Enumerable.Range(0, 4).Select(i => $"{B[i][2]:F3} -> {NB[i][2]:F3}")));
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }
Deswik.Graphics.Primaries.Figure Replace(Deswik.Graphics.Primaries.Figure src, List<double[]> p)
{
    var c = (Deswik.Graphics.Primaries.Figure)src.Clone(); var pl = c.asPolyline; bool cl = src.asPolyline.Closed; while (pl.VertexCount > 0) pl.RemoveVertexAt(0);
    foreach (var q in p) pl.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]); pl.Closed = cl; App.ModelSpace.Entities.AddItem(c, null, null, true, false); src.Deleted = true; return c;
}
Replace(hs, H.Select(q => { int k = edge.FirstOrDefault(i => D2(B[i], q) < 0.01, -1); return k >= 0 ? new[] { q[0], q[1], NB[k][2] } : q; }).ToList());
Replace(box, NB);
App.Redraw(true); Out.AppendLine("applied"); return null;
