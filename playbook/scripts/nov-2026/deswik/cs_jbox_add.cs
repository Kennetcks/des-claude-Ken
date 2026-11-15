// MPA 07: add a J/Box (jumbo box) strip on the decline's left wall (looking down), 20 m down the decline from the W6390 DSP,
// to "J/Box Strip 2.0mW x 1.5mD x 4.0mH": open 4-point string from the wall, splayed sides, 2.0 m back wall 1.5 m into
// the rock, 4.15 m wide at the wall (as the reference standard). RL follows the wall string. Attributes cloned from an
// MPA 07 strip panel (MAIN DEC), label cloned from the strip label. Dry run unless APPLY.
const bool APPLY = false;
const double BELOW = 20.0, BACK = 1.0, MOUTH = 2.075, DEPTH = 1.5;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
List<double[]> P3(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
double D2(double[] a, double[] b) => Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2));
Deswik.Graphics.Primaries.Figure dec = null, dsp = null, panel = null, lab = null; var walls = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || A(f, "CLAUDE_MPA07") != "1") continue; var ln = f.Layer.Name;
    if (ln == "0\\DESIGN DRAFT\\CENTERLINES" && f.IsPolyline) { if (A(f, "ACT_TYPE") == "DEC") dec = f; if (A(f, "Name") == "W6390 DSP") dsp = f; }
    if (ln == "0\\DESIGN DRAFT\\WALLS" && f.IsPolyline && A(f, "ACT_TYPE") == "DEC") walls.Add(f);
    if (ln == "0\\DESIGN DRAFT\\STRIPPING" && f.IsPolyline && panel == null) panel = f;
    if (ln == "0\\DESIGN DRAFT\\STRIPPING" && f.IsMText && f.asMText.TextString.Contains("Truck Loading")) lab = f;
}
var D = P3(dec); var C = new List<double> { 0 }; for (int i = 1; i < D.Count; i++) C.Add(C[i - 1] + D2(D[i], D[i - 1]));
(double s, double off, double h) On(double x, double y, double z)
{
    double bs = 0, bd = 1e9, bo = 0, bz = 0;
    for (int i = 1; i < D.Count; i++)
    {
        double vx = D[i][0] - D[i - 1][0], vy = D[i][1] - D[i - 1][1], L = Math.Sqrt(vx * vx + vy * vy); double t = ((x - D[i - 1][0]) * vx + (y - D[i - 1][1]) * vy) / (L * L);
        double tc = Math.Max(0, Math.Min(1, t)), px = D[i - 1][0] + tc * vx, py = D[i - 1][1] + tc * vy, d = Math.Sqrt(Math.Pow(x - px, 2) + Math.Pow(y - py, 2));
        if (d < bd - 1e-9) { bd = d; bs = C[i - 1] + (i == 1 ? t : tc) * L; bo = ((x - D[i - 1][0]) * vy - (y - D[i - 1][1]) * vx) / L; bz = D[i - 1][2] + (i == 1 ? t : tc) * (D[i][2] - D[i - 1][2]); }
    }
    return (bs, bo, z - bz);
}
double[] Back(double s, double off, double h)
{
    int i = 1; while (i < D.Count - 1 && C[i] < s) i++;
    double vx = D[i][0] - D[i - 1][0], vy = D[i][1] - D[i - 1][1], L = Math.Sqrt(vx * vx + vy * vy), t = (s - C[i - 1]) / L;
    return new[] { D[i - 1][0] + t * vx + vy / L * off, D[i - 1][1] + t * vy - vx / L * off, D[i - 1][2] + t * (D[i][2] - D[i - 1][2]) + h };
}
// first crossing of a decline wall string on the ray c0 -> c1 (extended 3x)
double[] Ray(double[] c0, double[] c1)
{
    c1 = new[] { c0[0] + 3 * (c1[0] - c0[0]), c0[1] + 3 * (c1[1] - c0[1]) }; double best = 1e9; double[] hit = null;
    foreach (var w in walls)
    {
        var p = P3(w); for (int i = 1; i < p.Count; i++)
        {
            double ax = c1[0] - c0[0], ay = c1[1] - c0[1], bx = p[i][0] - p[i - 1][0], by = p[i][1] - p[i - 1][1], den = ax * by - ay * bx; if (Math.Abs(den) < 1e-12) continue;
            double t = ((p[i - 1][0] - c0[0]) * by - (p[i - 1][1] - c0[1]) * bx) / den, u = ((p[i - 1][0] - c0[0]) * ay - (p[i - 1][1] - c0[1]) * ax) / den;
            if (t >= 0 && t <= 1 && u >= 0 && u <= 1 && t < best) { best = t; hit = new[] { c0[0] + t * ax, c0[1] + t * ay, p[i - 1][2] + u * (p[i][2] - p[i - 1][2]) }; }
        }
    }
    return hit;
}
double s0 = On(P3(dsp)[0][0], P3(dsp)[0][1], P3(dsp)[0][2]).s, s = s0 + BELOW;
// local straight frame at the wall: u along the wall chord (down the decline), n out into the rock (left)
var wc = Ray(Back(s, 0, 0), Back(s, -6, 0)); var wa = Ray(Back(s - MOUTH, 0, 0), Back(s - MOUTH, -6, 0)); var wb = Ray(Back(s + MOUTH, 0, 0), Back(s + MOUTH, -6, 0));
if (wc == null || wa == null || wb == null) { Out.AppendLine("left wall not found at the strip - nothing changed"); return null; }
double ux = wb[0] - wa[0], uy = wb[1] - wa[1], ul = Math.Sqrt(ux * ux + uy * uy); ux /= ul; uy /= ul; double nx = -uy, ny = ux;
var oc = On(wc[0], wc[1], wc[2]); if (On(wc[0] + nx, wc[1] + ny, 0).off > oc.off) { nx = -nx; ny = -ny; }
double h = oc.h;   // wall string height above the centreline
double[] P(double u, double n) { var q = new[] { wc[0] + u * ux + n * nx, wc[1] + u * uy + n * ny }; return new[] { q[0], q[1], On(q[0], q[1], 0).h * -1 + h }; }
var b1 = P(-BACK, DEPTH); var b2 = P(BACK, DEPTH);
// wall ends: from each back corner out towards the wall at +/- MOUTH (the standard's splay), stopped on the wall string
var w0 = Ray(b1, P(-MOUTH, 0)); var w3 = Ray(b2, P(MOUTH, 0));
if (w0 == null || w3 == null) { Out.AppendLine("splay does not reach the wall - nothing changed"); return null; }
var pts = new List<double[]> { w0, b1, b2, w3 };
Out.AppendLine($"DSP at station {s0:F2}; J/Box strip centred at station {s:F2}, left wall offset {oc.off:F2} m, wall string {h:F2} m above the centreline");
Out.AppendLine("vertices: " + string.Join(" ", pts.Select(q => $"({q[0]:F3},{q[1]:F3},{q[2]:F3})")));
Out.AppendLine($"widths: mouth {D2(w0, w3):F2} m, back {D2(b1, b2):F2} m, sides {D2(w0, b1):F2}/{D2(b2, w3):F2} m, depth {Math.Abs(On(b1[0], b1[1], b1[2]).off - oc.off):F2} m");
// anything else already on the left wall there?
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
    if (!f.Deleted && f.Layer != null && f.IsPolyline && f != dec && !walls.Contains(f) && f.Layer.Name.StartsWith("0\\DESIGN") && !f.Layer.Name.EndsWith("SOLIDS") && P3(f).Any(q => D2(q, wc) < 6 && Math.Abs(q[2] - wc[2]) < 6))
        Out.AppendLine($"  nearby: {f.Layer.Name} {A(f, "Name")} n={f.asPolyline.VertexCount}");
var lm = lab.BoundingBox.Midpoint; var lp = Back(s, oc.off - DEPTH - 3.0, h);
Out.AppendLine($"label at ({lp[0]:F2},{lp[1]:F2},{lp[2]:F2}), cloned from '{lab.asMText.TextString.Replace("\r\n", " | ")}'");
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }
var c = (Deswik.Graphics.Primaries.Figure)panel.Clone(); var pl = c.asPolyline; while (pl.VertexCount > 0) pl.RemoveVertexAt(0);
foreach (var q in pts) pl.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]); pl.Closed = false; App.ModelSpace.Entities.AddItem(c, null, null, true, false);
var t2 = (Deswik.Graphics.Primaries.Figure)lab.Clone(); t2.asMText.TextString = "J/Box Strip\r\n2.0mW x 1.5mD x 4.0mH"; App.ModelSpace.Entities.AddItem(t2, null, null, true, false);
var tm = t2.BoundingBox.Midpoint; t2.Translate(lp[0] - tm.x, lp[1] - tm.y, lp[2] - tm.z);
App.Redraw(true); Out.AppendLine("applied"); return null;
