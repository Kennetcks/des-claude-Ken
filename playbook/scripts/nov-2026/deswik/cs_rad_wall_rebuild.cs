// MPA 07 W6390 RAD drafted wall (DESIGN DRAFT\WALLS): rebuild on the current centreline (1:8.8 to the COG, flat after).
// Keeps the two fillet ends on the decline walls as drafted and the two fillet tangent points
// (snapped to +/- half width at their chainage); in between, one wall vertex per centreline vertex at +/- half width,
// all RLs = centreline RL at that chainage. Drops the old 6.99 / 13.49 m vertices. Dry run unless APPLY.
const bool APPLY = false;
const double HALF = 2.5;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
List<double[]> P3(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
double D2(double[] a, double[] b) => Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2));
Deswik.Graphics.Primaries.Figure cl = null, wall = null; var dw = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || !f.IsPolyline || A(f, "CLAUDE_MPA07") != "1") continue;
    if (f.Layer.Name == "0\\DESIGN DRAFT\\CENTERLINES" && A(f, "Name") == "W6390 RAD") cl = f;
    if (f.Layer.Name == "0\\DESIGN DRAFT\\WALLS" && A(f, "Name") == "W6390 RAD") wall = f;
    if (f.Layer.Name == "0\\DESIGN DRAFT\\WALLS" && A(f, "ACT_TYPE") == "DEC") dw.Add(f);
}
var P = P3(cl); var C = new List<double> { 0 }; for (int i = 1; i < P.Count; i++) C.Add(C[i - 1] + D2(P[i], P[i - 1]));
(double s, double off) On(double[] q)
{
    double bs = 0, bd = 1e9, bo = 0;
    for (int i = 1; i < P.Count; i++)
    {
        double vx = P[i][0] - P[i - 1][0], vy = P[i][1] - P[i - 1][1], L = Math.Sqrt(vx * vx + vy * vy), t = ((q[0] - P[i - 1][0]) * vx + (q[1] - P[i - 1][1]) * vy) / (L * L);
        double tc = Math.Max(i == 1 ? -1 : 0, Math.Min(i == P.Count - 1 ? 2 : 1, t)), d = Math.Sqrt(Math.Pow(q[0] - P[i - 1][0] - tc * vx, 2) + Math.Pow(q[1] - P[i - 1][1] - tc * vy, 2));
        if (d < bd - 1e-9) { bd = d; bs = C[i - 1] + tc * L; bo = ((q[0] - P[i - 1][0]) * vy - (q[1] - P[i - 1][1]) * vx) / L; }
    }
    return (bs, bo);
}
// point at chainage s, offset off (+ = right of the direction of travel); at a vertex the offset uses the incoming segment
// direction for the end vertex and the mitred direction at interior vertices
double[] At(double s, double off)
{
    int i = 1; while (i < P.Count - 1 && C[i] < s - 1e-6) i++;
    double vx = P[i][0] - P[i - 1][0], vy = P[i][1] - P[i - 1][1], L = Math.Sqrt(vx * vx + vy * vy), t = (s - C[i - 1]) / L;
    double nx = vy / L, ny = -vx / L, k = 1;
    if (Math.Abs(t - 1) < 1e-6 && i < P.Count - 1)
    {
        double wx = P[i + 1][0] - P[i][0], wy = P[i + 1][1] - P[i][1], M = Math.Sqrt(wx * wx + wy * wy), mx = nx + wy / M, my = ny - wx / M, ml = Math.Sqrt(mx * mx + my * my);
        mx /= ml; my /= ml; k = 1 / (mx * nx + my * ny); nx = mx; ny = my;
    }
    return new[] { P[i - 1][0] + t * vx + nx * off * k, P[i - 1][1] + t * vy + ny * off * k, P[i - 1][2] + t * (P[i][2] - P[i - 1][2]) };
}
double DecZ(double[] q)
{
    double bd = 1e9, bz = q[2];
    foreach (var w in dw) { var p = P3(w); for (int i = 1; i < p.Count; i++) { double vx = p[i][0] - p[i - 1][0], vy = p[i][1] - p[i - 1][1], L2 = vx * vx + vy * vy; if (L2 < 1e-12) continue; double t = Math.Max(0, Math.Min(1, ((q[0] - p[i - 1][0]) * vx + (q[1] - p[i - 1][1]) * vy) / L2)), d = Math.Sqrt(Math.Pow(q[0] - p[i - 1][0] - t * vx, 2) + Math.Pow(q[1] - p[i - 1][1] - t * vy, 2)); if (d < bd) { bd = d; bz = p[i - 1][2] + t * (p[i][2] - p[i - 1][2]); } } }
    Out.AppendLine($"    decline wall {bd:F3} m from ({q[0]:F2},{q[1]:F2}) at RL {bz:F3} (was {q[2]:F3})");
    return bz;
}
var W = P3(wall); if (W.Count != 10) { Out.AppendLine($"RAD wall has {W.Count} vertices, expected 10 - nothing changed"); return null; }
// layout: 0 fillet end, 1 tangent, 2..4 left side, 5..7 right side (end -> start), 8 tangent, 9 fillet end
var oL = On(W[1]); var oR = On(W[8]); double sgnL = Math.Sign(oL.off), sgnR = Math.Sign(oR.off);
var N = new List<double[]>();
N.Add(W[0]);   // fillet ends stay as drafted (left on the Design Master decline wall, right on the MPA 07 one)
N.Add(At(oL.s, sgnL * HALF));
for (int i = 1; i < P.Count; i++) N.Add(At(C[i], sgnL * HALF));
for (int i = P.Count - 1; i >= 1; i--) N.Add(At(C[i], sgnR * HALF));
N.Add(At(oR.s, sgnR * HALF));
N.Add(W[9]);
Out.AppendLine("old: " + string.Join(" ", W.Select(q => $"({q[0]:F2},{q[1]:F2},{q[2]:F3})")));
Out.AppendLine("new: " + string.Join(" ", N.Select(q => $"({q[0]:F2},{q[1]:F2},{q[2]:F3})")));
foreach (var q in N.Skip(1).Take(N.Count - 2)) { var o = On(q); Out.AppendLine($"  ch {o.s:F2} off {o.off:F2}"); }
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }
var c = (Deswik.Graphics.Primaries.Figure)wall.Clone(); var pl = c.asPolyline; bool closed = wall.asPolyline.Closed; while (pl.VertexCount > 0) pl.RemoveVertexAt(0);
foreach (var q in N) pl.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]); pl.Closed = closed; App.ModelSpace.Entities.AddItem(c, null, null, true, false); wall.Deleted = true;
App.Redraw(true); Out.AppendLine("applied"); return null;
