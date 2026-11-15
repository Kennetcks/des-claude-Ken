// MPA 07: finish the W6390 RAD walls at the B/T into W390 RAD W. Each RAD side wall runs to the W390 RAD W wall on the
// RAD side (half its width off its centreline, from the task solids), is cut back 3 m and joined to a point 3 m along
// the W390 RAD W wall (the fillet in the wall, as at the decline). The B/T between the two fillets stays open.
const bool APPLY = false; const double FIL = 3.0, HALF_BT = 2.5;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
var inv = System.Globalization.CultureInfo.InvariantCulture;
List<double[]> P3(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
void Set(Deswik.Graphics.Primaries.Figure f, List<double[]> p) { var pl = f.asPolyline; while (pl.VertexCount > 0) pl.RemoveVertexAt(0); foreach (var q in p) pl.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]); pl.Closed = false; }
var bt = new List<double[]>(); var seen = new HashSet<string>(); Deswik.Graphics.Primaries.Figure rad = null; var walls = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null) continue;
    if (f.Layer.Name == "0\\3MS\\TASK SOLIDS" && A(f, "Name") == "W390 RAD W" && A(f, "_CPX") != "") { if (seen.Add(A(f, "_CPX"))) bt.Add(new[] { double.Parse(A(f, "_CPX"), inv), double.Parse(A(f, "_CPY"), inv), double.Parse(A(f, "_CPZ"), inv) }); }
    else if (A(f, "CLAUDE_MPA07") == "1" && f.IsPolyline && A(f, "Name") == "W6390 RAD") { if (f.Layer.Name == "0\\DESIGN DRAFT\\CENTERLINES") rad = f; else if (f.Layer.Name == "0\\DESIGN DRAFT\\WALLS") walls.Add(f); }
}
var rc = P3(rad); var E = rc[^1];
// W390 RAD W centreline near the B/T: its cut centres within 15 m, in order along the drive
var near = bt.Where(q => Math.Sqrt(Math.Pow(q[0] - E[0], 2) + Math.Pow(q[1] - E[1], 2)) < 15).OrderBy(q => q[0]).ToList();
Out.AppendLine($"RAD walls {walls.Count}, W390 RAD W cut centres near the B/T {near.Count}");
if (walls.Count != 2 || near.Count < 3) { Out.AppendLine("unexpected input - nothing changed"); return null; }
(double d, double z, double s, double tx, double ty, double px, double py) Near(List<double[]> P, double x, double y)
{
    (double d, double z, double s, double tx, double ty, double px, double py) b = (1e9, 0, 0, 0, 0, 0, 0); double run = 0;
    for (int i = 1; i < P.Count; i++) { double ax = P[i - 1][0], ay = P[i - 1][1], vx = P[i][0] - ax, vy = P[i][1] - ay, L = Math.Sqrt(vx * vx + vy * vy); double t = Math.Max(0, Math.Min(1, ((x - ax) * vx + (y - ay) * vy) / (L * L))); double px = ax + t * vx, py = ay + t * vy, d = Math.Sqrt(Math.Pow(px - x, 2) + Math.Pow(py - y, 2)); if (d < b.d) b = (d, P[i - 1][2] + t * (P[i][2] - P[i - 1][2]), run + t * L, vx / L, vy / L, px, py); run += L; }
    return b;
}
// the W390 RAD W wall on the RAD side: its centreline offset HALF_BT towards the RAD
var nE = Near(near, E[0], E[1]); double sx = E[0] - nE.px, sy = E[1] - nE.py, sl = Math.Sqrt(sx * sx + sy * sy); sx /= sl; sy /= sl;
var wallBT = near.Select(q => new[] { q[0] + sx * HALF_BT, q[1] + sy * HALF_BT, q[2] }).ToList();
// pair: each RAD wall string ends at the B/T at the end nearest the W390 centreline
var newW = new List<(Deswik.Graphics.Primaries.Figure f, List<double[]> p)>();
foreach (var w in walls)
{
    var p = P3(w); bool atEnd = Near(near, p[^1][0], p[^1][1]).d < Near(near, p[0][0], p[0][1]).d; if (!atEnd) p.Reverse();
    // extend the last segment to the W390 wall line
    var a = p[^2]; var b = p[^1]; double ux = b[0] - a[0], uy = b[1] - a[1], ul = Math.Sqrt(ux * ux + uy * uy); ux /= ul; uy /= ul;
    double[] P = null; for (double t = -6; t < 12 && P == null; t += 0.002) { double x = b[0] + ux * t, y = b[1] + uy * t; if (Near(wallBT, x, y).d < 0.003) P = new[] { x, y }; }
    if (P == null) { Out.AppendLine("a RAD wall does not reach the W390 RAD W wall - nothing changed"); return null; }
    // cut the RAD wall back FIL from P (walking back along the string), keep the part before
    var pts = new List<double[]>(p.Take(p.Count - 1)); pts.Add(new[] { P[0], P[1], b[2] });
    var cum = new List<double> { 0 }; for (int i = pts.Count - 2; i >= 0; i--) cum.Insert(0, cum[0] + Math.Sqrt(Math.Pow(pts[i + 1][0] - pts[i][0], 2) + Math.Pow(pts[i + 1][1] - pts[i][1], 2)));
    var keep = new List<double[]>(); double[] cut = null;
    for (int i = 0; i < pts.Count; i++) { if (cum[i] > FIL) keep.Add(pts[i]); else { var q0 = pts[i - 1]; var q1 = pts[i]; double t = (cum[i - 1] - FIL) / (cum[i - 1] - cum[i]); cut = new[] { q0[0] + t * (q1[0] - q0[0]), q0[1] + t * (q1[1] - q0[1]), q0[2] + t * (q1[2] - q0[2]) }; break; } }
    keep.Add(cut);
    // fillet point FIL along the W390 wall, away from the RAD
    var nw = Near(wallBT, P[0], P[1]); double dir = Math.Sign((P[0] - E[0]) * nw.tx + (P[1] - E[1]) * nw.ty); if (dir == 0) dir = 1;
    double fx = P[0] + dir * nw.tx * FIL, fy = P[1] + dir * nw.ty * FIL; keep.Add(new[] { fx, fy, Near(near, fx, fy).z });
    if (!atEnd) keep.Reverse();
    newW.Add((w, keep));
    Out.AppendLine($"RAD wall: runs to the W390 RAD W wall, cut back {FIL} m, fillet to a point {FIL} m along that wall (RL {keep[atEnd ? keep.Count - 1 : 0][2]:F2})");
}
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }
foreach (var (f, p) in newW) Set(f, p);
App.Redraw(true); Out.AppendLine("applied"); return null;
