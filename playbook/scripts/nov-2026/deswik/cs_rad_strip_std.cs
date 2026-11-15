// MPA 07 W6390 RAD truck loading strip -> truck loading standard (1:7 down), as done for the DSP (cs_dsp_strip_std.cs):
// - high section and both ramps slide along the decline (following its curve) so the high section starts flush with the
//   drop-down edge; RL keeps its height above the decline; cables slide with them
// - drop-down top edge on the decline grade (+/- half width / 7 about its middle) instead of level, bottom edge 1.5 m below
//   it (was a level 1.47 m drop); the high section's two vertices on that edge follow it
// Drop-down plan position and width (4.46 m, 5.0 m RAD) kept. Dry run unless APPLY.
const bool APPLY = false;
const double DROP = 1.5;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
List<double[]> P3(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
double D2(double[] a, double[] b) => Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2));
Deswik.Graphics.Primaries.Figure dec = null, box = null; var strip = new List<Deswik.Graphics.Primaries.Figure>();
var radStart = new[] { 8086.18, 63114.33 };
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || A(f, "CLAUDE_MPA07") != "1" || !f.IsPolyline) continue;
    if (f.Layer.Name == "0\\DESIGN DRAFT\\CENTERLINES" && A(f, "ACT_TYPE") == "DEC") dec = f;
    if (f.Layer.Name == "0\\DESIGN DRAFT\\STRIPPING") { var p = P3(f); if (p.Count == 4 && p.Max(q => q[2]) - p.Min(q => q[2]) > 1.2 && p.Min(q => D2(q, radStart)) < 6) box = f; }
}
if (box == null) { Out.AppendLine("RAD drop-down not found - nothing changed"); return null; }
var B = P3(box); var bc = new[] { B.Average(q => q[0]), B.Average(q => q[1]) };
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
    if (!f.Deleted && f.Layer != null && f != box && f.Layer.Name == "0\\DESIGN DRAFT\\STRIPPING" && f.IsPolyline && A(f, "CLAUDE_MPA07") == "1" && f.asPolyline.VertexCount >= 16 && P3(f).Min(q => D2(q, bc)) < 8) strip.Add(f);
var hs = strip.FirstOrDefault(f => P3(f).Count(q => B.Any(b => D2(b, q) < 0.01)) == 2);
if (hs == null || strip.Count != 3) { Out.AppendLine($"strip panels {strip.Count}, high section {hs != null} - nothing changed"); return null; }

// decline: station / offset / height, and back (as cs_dsp_strip_move.cs)
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
// drop-down top edge = the two vertices shared with the high section; "up" = lower station
var H = P3(hs); var edge = Enumerable.Range(0, 4).Where(i => H.Any(q => D2(q, B[i]) < 0.01)).ToList();
var O = new[] { (B[edge[0]][0] + B[edge[1]][0]) / 2, (B[edge[0]][1] + B[edge[1]][1]) / 2, (B[edge[0]][2] + B[edge[1]][2]) / 2 };
double sO = On(O[0], O[1], O[2]).s;
int up = edge.OrderBy(i => On(B[i][0], B[i][1], B[i][2]).s).First(), dn = edge.First(i => i != up);
var cornerUp = H.Where(q => B.All(b => D2(b, q) > 0.01)).OrderBy(q => D2(q, B[up])).First();
double ds = On(B[up][0], B[up][1], B[up][2]).s - On(cornerUp[0], cornerUp[1], cornerUp[2]).s;   // + = slide down the decline
double half = D2(B[up], B[dn]) / 2;
Out.AppendLine($"drop-down {2 * half:F2} m wide, top RL {B[up][2]:F3}/{B[dn][2]:F3}, bottom {B.Where((q, i) => !edge.Contains(i)).Select(q => q[2].ToString("F3")).Aggregate((a, b) => a + "/" + b)}");
Out.AppendLine($"high section up corner {Math.Abs(ds):F2} m {(ds > 0 ? "up the decline from" : "inside")} the drop-down edge -> high section, ramps and cables slide {Math.Abs(ds):F2} m {(ds > 0 ? "down" : "up")} the decline");
var NB = B.Select(q => (double[])q.Clone()).ToList();
NB[up][2] = O[2] + half / 7.0; NB[dn][2] = O[2] - half / 7.0;
for (int i = 0; i < 4; i++) if (!edge.Contains(i)) { int t = D2(B[i], B[up]) < D2(B[i], B[dn]) ? up : dn; NB[i][2] = NB[t][2] - DROP; }
Out.AppendLine("drop-down RLs: " + string.Join(", ", Enumerable.Range(0, 4).Select(i => $"{B[i][2]:F3} -> {NB[i][2]:F3}")));
double[] Map(double[] q) { var o = On(q[0], q[1], q[2]); return Back(o.s + ds, o.off, o.h); }
var cs = new List<Deswik.Graphics.Primaries.Figure>(); var mc = hs.BoundingBox;
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || f.Layer.Name != "0\\DESIGN DRAFT\\CABLEBOLTS" || !f.IsPoint) continue; var m = f.BoundingBox.Midpoint;
    if (m.x > mc.Min.x - 0.5 && m.x < mc.Max.x + 0.5 && m.y > mc.Min.y - 0.5 && m.y < mc.Max.y + 0.5 && Math.Abs(m.z - mc.Max.z) < 3) cs.Add(f);
}
// check: a vertex mapped there and back
var t0 = H[0]; var t1 = Map(t0); Out.AppendLine($"e.g. high section vertex moves ({t1[0] - t0[0]:F3},{t1[1] - t0[1]:F3},{t1[2] - t0[2]:F3})");
Out.AppendLine($"panels: {string.Join(", ", strip.Select(f => f.asPolyline.VertexCount + " pts"))}; cables moving {cs.Count}");
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }
Deswik.Graphics.Primaries.Figure Replace(Deswik.Graphics.Primaries.Figure src, List<double[]> p)
{
    var c = (Deswik.Graphics.Primaries.Figure)src.Clone(); var pl = c.asPolyline; bool cl = src.asPolyline.Closed; while (pl.VertexCount > 0) pl.RemoveVertexAt(0);
    foreach (var q in p) pl.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]); pl.Closed = cl; App.ModelSpace.Entities.AddItem(c, null, null, true, false); src.Deleted = true; return c;
}
foreach (var f in strip)
    Replace(f, P3(f).Select(q => { int k = edge.FirstOrDefault(i => D2(B[i], q) < 0.01, -1); return k >= 0 ? new[] { q[0], q[1], NB[k][2] } : Map(q); }).ToList());
Replace(box, NB);
foreach (var f in cs) { var m = f.BoundingBox.Midpoint; var n = Map(new[] { m.x, m.y, m.z }); f.Translate(n[0] - m.x, n[1] - m.y, n[2] - m.z); }
App.Redraw(true); Out.AppendLine("applied"); return null;
