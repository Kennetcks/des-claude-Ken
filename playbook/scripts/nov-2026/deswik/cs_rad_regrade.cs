// MPA 07 W6390 RAD: regrade the centreline to a constant grade up from its start (decline wall) and a FLAT m flat section to the end, start and end RL
// kept; a vertex is added where the grade breaks. The RAD's own wall strings keep their height above the centreline at
// the same plan chainage. Drafter-generated CENTERLINES\SOLIDS and \WALLS are left for regeneration. Labels mentioning
// a grade are listed, not changed. Dry run unless APPLY.
const bool APPLY = false;
const double FLAT = 7.0;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
List<double[]> P3(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
double D2(double[] a, double[] b) => Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2));
Deswik.Graphics.Primaries.Figure cl = null; var walls = new List<Deswik.Graphics.Primaries.Figure>(); var texts = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || A(f, "CLAUDE_MPA07") != "1" || A(f, "Name") != "W6390 RAD") continue;
    if (f.Layer.Name == "0\\DESIGN DRAFT\\CENTERLINES" && f.IsPolyline) cl = f;
    else if (f.Layer.Name == "0\\DESIGN DRAFT\\WALLS" && f.IsPolyline) walls.Add(f);
    else if (f.IsMText) texts.Add(f);
}
var P = P3(cl); var C = new List<double> { 0 }; for (int i = 1; i < P.Count; i++) C.Add(C[i - 1] + D2(P[i], P[i - 1]));
double z0 = P[0][2], z1 = P.Last()[2], sb = C.Last() - FLAT, G = sb / (z1 - z0);
double Zn(double s) => s >= sb ? z1 : z0 + s / G;
double Zo(double s) { int i = 1; while (i < P.Count - 1 && C[i] < s) i++; double t = Math.Max(0, Math.Min(1, (s - C[i - 1]) / (C[i] - C[i - 1]))); return P[i - 1][2] + t * (P[i][2] - P[i - 1][2]); }
double St(double x, double y) { double bs = 0, bd = 1e9; for (int i = 1; i < P.Count; i++) { double vx = P[i][0] - P[i - 1][0], vy = P[i][1] - P[i - 1][1], L = Math.Sqrt(vx * vx + vy * vy), t = ((x - P[i - 1][0]) * vx + (y - P[i - 1][1]) * vy) / (L * L), tc = Math.Max(0, Math.Min(1, t)), d = Math.Sqrt(Math.Pow(x - P[i - 1][0] - tc * vx, 2) + Math.Pow(y - P[i - 1][1] - tc * vy, 2)); if (d < bd) { bd = d; bs = C[i - 1] + ((i == 1 || i == P.Count - 1) ? Math.Max(i == 1 ? -1e9 : 0, Math.Min(i == P.Count - 1 ? 1e9 : 1, t)) : tc) * L; } } return Math.Max(0, Math.Min(C.Last(), bs)); }
// new centreline: old plan vertices with new RLs, plus the grade break
var N = new List<double[]>();
for (int i = 0; i < P.Count; i++)
{
    if (i > 0 && C[i - 1] < sb - 0.01 && C[i] > sb + 0.01) { double t = (sb - C[i - 1]) / (C[i] - C[i - 1]); N.Add(new[] { P[i - 1][0] + t * (P[i][0] - P[i - 1][0]), P[i - 1][1] + t * (P[i][1] - P[i - 1][1]), z1 }); }
    N.Add(new[] { P[i][0], P[i][1], Zn(C[i]) });
}
Out.AppendLine($"RAD {C.Last():F2} m plan, RL {z0:F3} -> {z1:F3}; 1:{G:F2} for {sb:F2} m, flat {C.Last() - sb:F2} m");
Out.AppendLine("centreline: " + string.Join(" ", N.Select(q => $"({q[0]:F2},{q[1]:F2},{q[2]:F3})")));
var NW = new Dictionary<Deswik.Graphics.Primaries.Figure, List<double[]>>();
foreach (var w in walls)
{
    var p = P3(w); var n = p.Select(q => { double s = St(q[0], q[1]); return new[] { q[0], q[1], q[2] - Zo(s) + Zn(s) }; }).ToList(); NW[w] = n;
    Out.AppendLine($"wall n={p.Count}: RL change {n.Zip(p, (a, b) => a[2] - b[2]).Min():F3} to {n.Zip(p, (a, b) => a[2] - b[2]).Max():F3}");
}
foreach (var t in texts) Out.AppendLine($"text: {t.Layer.Name} '{t.asMText.TextString.Replace("\r\n", " | ")}'");
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }
Deswik.Graphics.Primaries.Figure Replace(Deswik.Graphics.Primaries.Figure src, List<double[]> p)
{
    var c = (Deswik.Graphics.Primaries.Figure)src.Clone(); var pl = c.asPolyline; bool cl2 = src.asPolyline.Closed; while (pl.VertexCount > 0) pl.RemoveVertexAt(0);
    foreach (var q in p) pl.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]); pl.Closed = cl2; App.ModelSpace.Entities.AddItem(c, null, null, true, false); src.Deleted = true; return c;
}
Replace(cl, N); foreach (var w in walls) Replace(w, NW[w]);
App.Redraw(true); Out.AppendLine("applied"); return null;
