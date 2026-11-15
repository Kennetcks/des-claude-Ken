// MPA 07 W6390 DSP truck loading strip -> "Truck Loading Strip (1:7 down) 8.0 mL x 6.0mW x 1.5mH" (5.0 m drop-down):
// - high section and both ramps shift up the decline so the 8.0 m high section starts flush with the drop-down edge
//   (they were set out for the 5.0 m leg standard, 0.24 m inside); RL follows 1:7, cables move with them
// - drop-down top edge on the decline grade (+/-2.5/7 m about its middle) instead of level, bottom edge 1.5 m below it
//   (was a level 2.05 m drop); the high section's two vertices on that edge follow it
// Plan positions of the drop-down are kept. Dry run unless APPLY.
const bool APPLY = false;
const double DROP = 1.5;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
List<double[]> P3(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
double D2(double[] a, double[] b) => Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2));
Deswik.Graphics.Primaries.Figure dec = null, box = null; var strip = new List<Deswik.Graphics.Primaries.Figure>(); var cab = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
    if (!f.Deleted && f.Layer != null && f.Layer.Name == "0\\DESIGN DRAFT\\CENTERLINES" && f.IsPolyline && A(f, "CLAUDE_MPA07") == "1" && A(f, "ACT_TYPE") == "DEC") dec = f;
var dsp = new[] { 8110.89, 63093.59 };
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || f.Layer.Name != "0\\DESIGN DRAFT\\STRIPPING" || !f.IsPolyline || A(f, "CLAUDE_MPA07") != "1") continue;
    var p = P3(f); if (p.Count == 4 && p.Max(q => q[2]) - p.Min(q => q[2]) > 1.9 && p.Min(q => D2(q, dsp)) < 6) box = f;
}
var B = P3(box); var bc = new[] { B.Average(q => q[0]), B.Average(q => q[1]) };
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || f == box) continue;
    if (f.Layer.Name == "0\\DESIGN DRAFT\\STRIPPING" && f.IsPolyline && A(f, "CLAUDE_MPA07") == "1" && f.asPolyline.VertexCount >= 16 && P3(f).Min(q => D2(q, bc)) < 8) strip.Add(f);
}
// down-decline direction at the drop-down
var D = P3(dec); int si = 1; double bd = 1e9;
for (int i = 1; i < D.Count; i++) { double vx = D[i][0] - D[i - 1][0], vy = D[i][1] - D[i - 1][1], L2 = vx * vx + vy * vy; double t = Math.Max(0, Math.Min(1, ((bc[0] - D[i - 1][0]) * vx + (bc[1] - D[i - 1][1]) * vy) / L2)); double d = Math.Sqrt(Math.Pow(D[i - 1][0] + t * vx - bc[0], 2) + Math.Pow(D[i - 1][1] + t * vy - bc[1], 2)); if (d < bd) { bd = d; si = i; } }
double dx = D[si][0] - D[si - 1][0], dy = D[si][1] - D[si - 1][1], dl = Math.Sqrt(dx * dx + dy * dy); double upx = -dx / dl, upy = -dy / dl;
// drop-down top edge = the two vertices it shares with the high section
var hs = strip.FirstOrDefault(f => P3(f).Count(q => B.Any(b => D2(b, q) < 0.01)) == 2);
if (hs == null || strip.Count != 3) { Out.AppendLine($"strip panels {strip.Count}, high section {hs != null} - nothing changed"); return null; }
var H = P3(hs); var edge = Enumerable.Range(0, 4).Where(i => H.Any(q => D2(q, B[i]) < 0.01)).ToList();
var O = new[] { (B[edge[0]][0] + B[edge[1]][0]) / 2, (B[edge[0]][1] + B[edge[1]][1]) / 2, (B[edge[0]][2] + B[edge[1]][2]) / 2 };
double U(double[] q) => (q[0] - O[0]) * upx + (q[1] - O[1]) * upy;
// high-section corner on the drop-down side line, up end: how far it sits inside the drop-down edge
var upEdge = B[edge.OrderByDescending(i => U(B[i])).First()];
var cornerUp = H.Where(q => B.All(b => D2(b, q) > 0.01)).OrderBy(q => D2(q, upEdge)).First();
double shift = U(upEdge) - U(cornerUp), sz = shift / 7.0;
Out.AppendLine($"drop-down edge {D2(B[edge[0]], B[edge[1]]):F2} m, top RL {O[2]:F3}; high section up corner {shift:F2} m inside the drop-down edge -> high section, ramps and cables move {shift:F2} m up the decline (+{sz:F3} m RL)");
var NB = new List<double[]>(); foreach (var q in B) { double u = U(q); bool top = edge.Contains(B.IndexOf(q)); double z = O[2] + u / 7.0 - (top ? 0 : DROP); NB.Add(new[] { q[0], q[1], z }); }
// bottom vertices take the RL of the top vertex on their side
for (int i = 0; i < 4; i++) if (!edge.Contains(i)) { var t = edge.OrderBy(j => Math.Abs(U(B[j]) - U(B[i]))).First(); NB[i][2] = O[2] + U(B[t]) / 7.0 - DROP; }
Out.AppendLine("drop-down RLs: " + string.Join(", ", Enumerable.Range(0, 4).Select(i => $"{B[i][2]:F3} -> {NB[i][2]:F3}")));
var cs = new List<Deswik.Graphics.Primaries.Figure>();
var mc = hs.BoundingBox;
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || f.Layer.Name != "0\\DESIGN DRAFT\\CABLEBOLTS" || !f.IsPoint) continue; var m = f.BoundingBox.Midpoint;
    if (m.x > mc.Min.x - 0.5 && m.x < mc.Max.x + 0.5 && m.y > mc.Min.y - 0.5 && m.y < mc.Max.y + 0.5 && Math.Abs(m.z - mc.Max.z) < 3) cs.Add(f);
}
Out.AppendLine($"panels: {string.Join(", ", strip.Select(f => f.asPolyline.VertexCount + " pts"))}; cables moving {cs.Count}");
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }
Deswik.Graphics.Primaries.Figure Replace(Deswik.Graphics.Primaries.Figure src, List<double[]> p)
{
    var c = (Deswik.Graphics.Primaries.Figure)src.Clone(); var pl = c.asPolyline; bool cl = src.asPolyline.Closed; while (pl.VertexCount > 0) pl.RemoveVertexAt(0);
    foreach (var q in p) pl.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]); pl.Closed = cl; App.ModelSpace.Entities.AddItem(c, null, null, true, false); src.Deleted = true; return c;
}
foreach (var f in strip)
{
    var p = P3(f).Select(q =>
    {
        int k = edge.FirstOrDefault(i => D2(B[i], q) < 0.01, -1);
        return k >= 0 ? new[] { q[0], q[1], NB[k][2] } : new[] { q[0] + upx * shift, q[1] + upy * shift, q[2] + sz };
    }).ToList();
    Replace(f, p);
}
Replace(box, NB);
foreach (var f in cs) f.Translate(upx * shift, upy * shift, sz);
App.Redraw(true); Out.AppendLine("applied"); return null;
