// MPA 07 W6390 DSP truck-loading drop-down: widen the 4-point box from 4.5 m to WIDTH, 0.25 m each side, kept centred on
// the drive; depth (3.5 m) and RLs unchanged. The two high-section panel vertices that share the box's top edge move with
// it. Dry run unless APPLY.
const bool APPLY = false;
const double WIDTH = 5.0;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
List<double[]> P3(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
double D2(double[] a, double[] b) => Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2));
var dsp = new[] { 8110.890, 63093.593 }; Deswik.Graphics.Primaries.Figure box = null; var panels = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || f.Layer.Name != "0\\DESIGN DRAFT\\STRIPPING" || !f.IsPolyline || A(f, "CLAUDE_MPA07") != "1") continue;
    var p = P3(f); if (p.Count == 4 && p.Max(q => q[2]) - p.Min(q => q[2]) > 1.9 && p.Min(q => D2(q, dsp)) < 6) box = f; else panels.Add(f);
}
if (box == null) { Out.AppendLine("drop-down not found - nothing changed"); return null; }
var B = P3(box);
// top edge = the two highest vertices; width direction along it
var top = Enumerable.Range(0, 4).OrderByDescending(i => B[i][2]).Take(2).OrderBy(i => i).ToList(); int a = top[0], b = top[1];
double ux = B[b][0] - B[a][0], uy = B[b][1] - B[a][1], w0 = Math.Sqrt(ux * ux + uy * uy); ux /= w0; uy /= w0; double e = (WIDTH - w0) / 2;
var N = B.Select(q => (double[])q.Clone()).ToList();
for (int i = 0; i < 4; i++) { double t = (B[i][0] - B[a][0]) * ux + (B[i][1] - B[a][1]) * uy; double sgn = t < w0 / 2 ? -1 : 1; N[i][0] += sgn * ux * e; N[i][1] += sgn * uy * e; }
// high-section panel sharing the top edge
Deswik.Graphics.Primaries.Figure hs = null; List<double[]> HS = null;
foreach (var f in panels) { var p = P3(f); if (p.Any(q => D2(q, B[a]) < 0.01) && p.Any(q => D2(q, B[b]) < 0.01)) { hs = f; HS = p; } }
Out.AppendLine($"drop-down {w0:F2} m -> {WIDTH:F2} m wide ({e:F2} m each side), depth {D2(B[(b + 1) % 4], B[b]):F2} m, RL {B.Max(q => q[2]):F2} to {B.Min(q => q[2]):F2} unchanged; high-section panel sharing the edge {(hs != null ? HS.Count + " pts" : "not found")}");
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }
Deswik.Graphics.Primaries.Figure Replace(Deswik.Graphics.Primaries.Figure src, List<double[]> p)
{
    var c = (Deswik.Graphics.Primaries.Figure)src.Clone(); var pl = c.asPolyline; bool cl = src.asPolyline.Closed; while (pl.VertexCount > 0) pl.RemoveVertexAt(0);
    foreach (var q in p) pl.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]); pl.Closed = cl; App.ModelSpace.Entities.AddItem(c, null, null, true, false); src.Deleted = true; return c;
}
if (hs != null) { for (int i = 0; i < HS.Count; i++) { if (D2(HS[i], B[a]) < 0.01) HS[i] = new[] { N[a][0], N[a][1], HS[i][2] }; else if (D2(HS[i], B[b]) < 0.01) HS[i] = new[] { N[b][0], N[b][1], HS[i][2] }; } Replace(hs, HS); }
Replace(box, N);
App.Redraw(true); Out.AppendLine("applied"); return null;
