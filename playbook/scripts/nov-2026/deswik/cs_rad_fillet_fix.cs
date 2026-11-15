// MPA 07, W6390 RAD mouth at the joint with the existing decline:
// - the RAD west wall meets the decline west wall 1.2 m past the joint, so its 3 m fillet runs back onto the EXISTING
//   decline wall (Design Master copy, 0\DESIGN MASTER\WALLS, MAIN DEC): fillet = 3 m back along the decline wall to
//   3 m up the RAD wall
// - that existing wall string is cut at the fillet point and its end face across the joint is dropped (west wall up to
//   the fillet point, east wall from the joint on, as two strings with the same attributes)
// - the new decline east wall ends exactly on the existing east wall end (closes a 0.6 m overlap)
// Dry run unless APPLY.
const bool APPLY = false;
const double FIL = 3.0;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
List<double[]> P3(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
Deswik.Graphics.Primaries.Figure mw = null, radW = null, decE = null, dec = null;
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || !f.IsPolyline) continue; var h = Convert.ToString(f.GetType().GetProperty("Handle")?.GetValue(f));
    if (f.Layer.Name == "0\\DESIGN MASTER\\WALLS" && A(f, "Name") == "MAIN DEC" && f.asPolyline.VertexCount == 30) { var p = P3(f); if (Math.Abs(p[7][0] - 8081.914) < 0.01 && Math.Abs(p[8][0] - 8082.498) < 0.01) mw = f; }
    else if (f.Layer.Name == "0\\DESIGN DRAFT\\WALLS" && A(f, "CLAUDE_MPA07") == "1" && A(f, "Name") == "W6390 RAD") radW = f;
    else if (f.Layer.Name == "0\\DESIGN DRAFT\\WALLS" && A(f, "CLAUDE_MPA07") == "1" && A(f, "Name") == "MAIN DEC" && f.asPolyline.VertexCount > 100) decE = f;
    else if (f.Layer.Name == "0\\DESIGN DRAFT\\CENTERLINES" && A(f, "CLAUDE_MPA07") == "1" && A(f, "ACT_TYPE") == "DEC") dec = f;
}
Out.AppendLine($"existing decline wall {mw != null}, RAD wall {radW != null}, new decline wall to the end {decE != null}");
if (mw == null || radW == null || decE == null || dec == null) { Out.AppendLine("missing input - nothing changed"); return null; }
var M = P3(mw); var R = P3(radW); var E = P3(decE);
// west decline wall as one line: existing west wall (v0..v7) then the new decline west wall offset from the new centreline
var D = P3(dec); var west = M.Take(8).ToList();
for (int i = 0; i < Math.Min(D.Count - 1, 30); i++)
{
    var a = D[i]; var b = D[i + 1]; double vx = b[0] - a[0], vy = b[1] - a[1], l = Math.Sqrt(vx * vx + vy * vy);
    var w = new[] { a[0] - vy / l * 2.75, a[1] + vx / l * 2.75, a[2] }; if (i > 0) west.Add(w);
}
List<double> Cum(List<double[]> p) { var c = new List<double> { 0 }; for (int i = 1; i < p.Count; i++) c.Add(c[i - 1] + Math.Sqrt(Math.Pow(p[i][0] - p[i - 1][0], 2) + Math.Pow(p[i][1] - p[i - 1][1], 2))); return c; }
double[] At(List<double[]> p, List<double> c, double s) { int i = 1; while (i < p.Count - 1 && c[i] < s) i++; double t = c[i] > c[i - 1] ? (s - c[i - 1]) / (c[i] - c[i - 1]) : 0; return new[] { p[i - 1][0] + t * (p[i][0] - p[i - 1][0]), p[i - 1][1] + t * (p[i][1] - p[i - 1][1]), p[i - 1][2] + t * (p[i][2] - p[i - 1][2]) }; }
var wc = Cum(west);
// RAD west wall: R[2] is its first vertex up the drive; line R[2] -> R[3] direction, extended back to the decline wall
double ux = R[3][0] - R[2][0], uy = R[3][1] - R[2][1], ul = Math.Sqrt(ux * ux + uy * uy); ux /= ul; uy /= ul;
double sx = -1, tx = 0;
for (int i = 1; i < west.Count; i++)
{
    // wall A + t v  =  R2 - w u   (back down the RAD wall)
    double ax = west[i - 1][0], ay = west[i - 1][1], vx = west[i][0] - ax, vy = west[i][1] - ay, den = vx * uy - vy * ux; if (Math.Abs(den) < 1e-12) continue;
    double bx = R[2][0] - ax, by = R[2][1] - ay, t = (bx * uy - by * ux) / den, w = (vx * by - vy * bx) / den;
    if (t >= 0 && t <= 1 && w >= 0) { sx = wc[i - 1] + t * (wc[i] - wc[i - 1]); tx = w; break; }
}
if (sx < 0) { Out.AppendLine("RAD west wall does not reach the decline wall - nothing changed"); return null; }
double sJoint = wc[7];
var X = At(west, wc, sx); var Pf = At(west, wc, sx - FIL);
double gz = (R[3][2] - R[2][2]) / ul;   // RAD grade along its wall
var F = new[] { X[0] + ux * FIL, X[1] + uy * FIL, X[2] + gz * FIL };
Out.AppendLine($"RAD west wall meets the decline west wall {sx - sJoint:F2} m past the joint; fillet {FIL} m back along the decline wall = {sJoint - (sx - FIL):F2} m onto the existing wall, at ({Pf[0]:F3},{Pf[1]:F3},{Pf[2]:F3}); RAD side ({F[0]:F3},{F[1]:F3},{F[2]:F3})");
var newRad = new List<double[]> { Pf, F }; newRad.AddRange(R.Skip(2));
// existing wall cut: west part up to the fillet point, east part from the joint (v8) on
int k = 0; while (k < 7 && wc[k + 1] < sx - FIL) k++;
var mWest = M.Take(k + 1).ToList(); mWest.Add(Pf);
var mEast = M.Skip(8).ToList();
// new decline east wall: last vertex onto the existing east wall end
var newE = E.Take(E.Count - 1).ToList(); newE.Add(M[8]);
Out.AppendLine($"existing MAIN DEC wall (30 vertices) -> west part {mWest.Count} vertices (to the fillet point) + east part {mEast.Count} vertices; end face across the joint removed");
Out.AppendLine($"new decline east wall: last vertex moved {Math.Sqrt(Math.Pow(E[^1][0] - M[8][0], 2) + Math.Pow(E[^1][1] - M[8][1], 2)):F2} m onto the existing east wall end");
Out.AppendLine($"RAD wall: {R.Count} -> {newRad.Count} vertices (old start {R[0][0]:F2},{R[0][1]:F2} and fillet point dropped)");
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }
Deswik.Graphics.Primaries.Figure Make(Deswik.Graphics.Primaries.Figure src, List<double[]> p)
{
    var c = (Deswik.Graphics.Primaries.Figure)src.Clone(); var pl = c.asPolyline; while (pl.VertexCount > 0) pl.RemoveVertexAt(0);
    foreach (var q in p) pl.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]); pl.Closed = false; App.ModelSpace.Entities.AddItem(c, null, null, true, false); return c;
}
Make(mw, mWest); Make(mw, mEast); mw.Deleted = true;
Make(radW, newRad); radW.Deleted = true;
Make(decE, newE); decE.Deleted = true;
App.Redraw(true); Out.AppendLine("applied"); return null;
