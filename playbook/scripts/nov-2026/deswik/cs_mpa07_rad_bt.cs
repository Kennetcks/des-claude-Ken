// MPA 07: W6390 RAD breaks through floor to floor into W390 RAD W (the scheduled drive, from its task solids on
// 0\3MS\TASK SOLIDS). The RAD centreline ends on the W390 RAD W wall (half its width off its centreline, Design Master
// style) at that drive's floor; the first +1:10 and the flat section stay, the last section takes whatever grade
// reaches the B/T floor. The RAD wall string loses its end face (open into the B/T) and is trimmed there. Dry run
// unless APPLY.
const bool APPLY = false; const double HALF_BT = 2.5, FLAT = 6.0;   // flat section length
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
var inv = System.Globalization.CultureInfo.InvariantCulture;
List<double[]> P3(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
void Set(Deswik.Graphics.Primaries.Figure f, List<double[]> p) { var pl = f.asPolyline; bool c = pl.Closed; while (pl.VertexCount > 0) pl.RemoveVertexAt(0); foreach (var q in p) pl.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]); pl.Closed = c; }
var bt = new List<double[]>(); Deswik.Graphics.Primaries.Figure rad = null, radWall = null; var seen = new HashSet<string>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null) continue;
    if (f.Layer.Name == "0\\3MS\\TASK SOLIDS" && A(f, "Name") == "W390 RAD W" && A(f, "_CPX") != "") { string k = A(f, "_CPX"); if (seen.Add(k)) bt.Add(new[] { double.Parse(A(f, "_CPX"), inv), double.Parse(A(f, "_CPY"), inv), double.Parse(A(f, "_CPZ"), inv) }); }
    else if (A(f, "CLAUDE_MPA07") == "1" && f.IsPolyline && A(f, "Name") == "W6390 RAD") { if (f.Layer.Name == "0\\DESIGN DRAFT\\CENTERLINES") rad = f; else if (f.Layer.Name == "0\\DESIGN DRAFT\\WALLS") radWall = f; }
}
Out.AppendLine($"W390 RAD W cuts {bt.Count}, RAD centreline {rad != null}, RAD wall {radWall != null}");
if (bt.Count < 4 || rad == null || radWall == null) { Out.AppendLine("missing input - nothing changed"); return null; }
// W390 RAD W centreline through its cut centres, ordered along its run (it runs roughly east-west)
bt = bt.OrderBy(q => q[0]).ToList();
(double d, double z, double s, double tx, double ty) Near(List<double[]> P, double x, double y)
{
    (double d, double z, double s, double tx, double ty) b = (1e9, 0, 0, 0, 0); double run = 0;
    for (int i = 1; i < P.Count; i++) { double ax = P[i - 1][0], ay = P[i - 1][1], vx = P[i][0] - ax, vy = P[i][1] - ay, L = Math.Sqrt(vx * vx + vy * vy); double t = Math.Max(0, Math.Min(1, ((x - ax) * vx + (y - ay) * vy) / (L * L))); double d = Math.Sqrt(Math.Pow(ax + t * vx - x, 2) + Math.Pow(ay + t * vy - y, 2)); if (d < b.d) b = (d, P[i - 1][2] + t * (P[i][2] - P[i - 1][2]), run + t * L, vx / L, vy / L); run += L; }
    return b;
}
var r = P3(rad);
// the flat section's end: last vertex of the run of flat segments
int fs = -1; for (int i = 1; i < r.Count && fs < 0; i++) if (Math.Abs(r[i][2] - r[i - 1][2]) < 1e-4) fs = i - 1;   // flat section starts here
if (fs < 0) { Out.AppendLine("no flat section found - nothing changed"); return null; }
// flat section FLAT m long from its start: insert a vertex there, everything past it is re-graded to the B/T
int flatEndIdx = -1;
{ double acc = 0; int ins = -1; double[] ip = null;
  for (int i = fs + 1; i < r.Count && ins < 0; i++) { double L = Math.Sqrt(Math.Pow(r[i][0] - r[i - 1][0], 2) + Math.Pow(r[i][1] - r[i - 1][1], 2)); if (acc + L >= FLAT) { double t = (FLAT - acc) / L; ip = new[] { r[i - 1][0] + t * (r[i][0] - r[i - 1][0]), r[i - 1][1] + t * (r[i][1] - r[i - 1][1]), r[fs][2] }; ins = i; } acc += L; }
  if (ins < 0) { Out.AppendLine("flat section would run past the end - nothing changed"); return null; }
  for (int i = fs + 1; i < ins; i++) r[i][2] = r[fs][2];
  if (Math.Sqrt(Math.Pow(ip[0] - r[ins][0], 2) + Math.Pow(ip[1] - r[ins][1], 2)) < 0.05) { r[ins][2] = r[fs][2]; ip = null; } else r.Insert(ins, ip); flatEndIdx = ins; }
int flatEnd = flatEndIdx;
Out.AppendLine($"flat section {FLAT} m at RL {r[fs][2]:F2}");
if (flatEnd < 0) { Out.AppendLine("no flat section found - nothing changed"); return null; }
// RAD axis beyond the flat section: from the flat end along the last segment's direction, until it is HALF_BT off W390 RAD W
var a = r[flatEnd]; var lastDir = r[^1]; double dx = lastDir[0] - r[^2][0], dy = lastDir[1] - r[^2][1], dl = Math.Sqrt(dx * dx + dy * dy); dx /= dl; dy /= dl;
double[] end = null; double reach = 0;
// walk the existing plan line past the flat end, then straight on, until HALF_BT off the B/T centreline
var path = r.Skip(flatEnd).Select(q => new[] { q[0], q[1] }).ToList(); for (int k = 1; k <= 40; k++) path.Add(new[] { r[^1][0] + dx * k * 0.5, r[^1][1] + dy * k * 0.5 });
for (int i = 1; i < path.Count && end == null; i++)
{
    double L = Math.Sqrt(Math.Pow(path[i][0] - path[i - 1][0], 2) + Math.Pow(path[i][1] - path[i - 1][1], 2));
    for (double t = 0; t <= L; t += 0.005) { double x = path[i - 1][0] + (path[i][0] - path[i - 1][0]) * t / L, y = path[i - 1][1] + (path[i][1] - path[i - 1][1]) * t / L; var n = Near(bt, x, y); if (n.d <= HALF_BT) { end = new[] { x, y, n.z }; reach += t; break; } }
    if (end == null) reach += L;
}
if (end == null) { Out.AppendLine("the RAD does not reach W390 RAD W - nothing changed"); return null; }
// new plan: up to the flat end, then the plan line on to the end point
var np = r.Take(flatEnd + 1).Select(q => new[] { q[0], q[1], q[2] }).ToList();
for (int i = flatEnd + 1; i < r.Count; i++) { var n = Near(bt, r[i][0], r[i][1]); if (n.d > HALF_BT + 1e-3) np.Add(new[] { r[i][0], r[i][1], 0 }); }
np.Add(end);
double run2 = 0; var cum = new List<double> { 0 }; for (int i = flatEnd + 1; i < np.Count; i++) { run2 += Math.Sqrt(Math.Pow(np[i][0] - np[i - 1][0], 2) + Math.Pow(np[i][1] - np[i - 1][1], 2)); cum.Add(run2); }
double z0 = np[flatEnd][2], rise = end[2] - z0, g = run2 / rise;
for (int i = flatEnd + 1; i < np.Count; i++) np[i][2] = z0 + rise * cum[i - flatEnd] / run2;
Out.AppendLine($"B/T: RAD ends on the W390 RAD W wall at ({end[0]:F2},{end[1]:F2}), floor RL {end[2]:F2} (W390 RAD W floor there); last section {run2:F1} m at {(rise >= 0 ? "+" : "-")}1:{Math.Abs(g):F1} from the flat section (RL {z0:F2}); was ending at RL {r[^1][2]:F2}");
// wall: keep the points before the B/T face, cut both sides at the B/T face, drop the end face (open into the B/T)
var w = P3(radWall); var keep = new List<(int i, bool k)>();
double sEnd = Near(np, end[0], end[1]).s; var cutSegs = new List<int>();
var parts = new List<List<double[]>>(); var cur = new List<double[]>();
for (int i = 0; i < w.Count; i++)
{
    var n = Near(np, w[i][0], w[i][1]); bool inside = n.s < sEnd - 1e-3 || n.d > 3.5;   // points of the RAD past the end station go
    double zf = n.d <= 3.5 ? Near(np, w[i][0], w[i][1]).z : w[i][2];
    var pnt = new[] { w[i][0], w[i][1], n.d <= 3.5 ? zf : w[i][2] };
    if (inside) { if (cur.Count == 0 && i > 0 && parts.Count > 0) { } cur.Add(pnt); }
    else if (cur.Count > 0) { parts.Add(cur); cur = new List<double[]>(); }
}
if (cur.Count > 0) parts.Add(cur);
// close each side onto the B/T face: extend the last kept point of a side to the end station along the wall direction
List<double[]> ToFace(List<double[]> side, bool atEnd)
{
    var s = new List<double[]>(side); var p1 = atEnd ? s[^1] : s[0]; var p0 = atEnd ? (s.Count > 1 ? s[^2] : p1) : (s.Count > 1 ? s[1] : p1);
    double ux = p1[0] - p0[0], uy = p1[1] - p0[1], ul = Math.Sqrt(ux * ux + uy * uy); if (ul < 1e-6) return s; ux /= ul; uy /= ul;
    double[] best = p1; for (double t = 0; t < 12; t += 0.005) { double x = p1[0] + ux * t, y = p1[1] + uy * t; if (Near(np, x, y).s >= sEnd) { best = new[] { x, y, end[2] }; break; } }
    if (atEnd) s.Add(best); else s.Insert(0, best); return s;
}
if (parts.Count == 2) { parts[0] = ToFace(parts[0], true); parts[1] = ToFace(parts[1], false); }
Out.AppendLine($"RAD wall: end face removed, {parts.Count} strings ({string.Join(", ", parts.Select(p => p.Count + " pts"))})");
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }
Set(rad, np);
var src = (Deswik.Graphics.Primaries.Figure)radWall.Clone();
radWall.Deleted = true;
foreach (var p in parts) { var c = (Deswik.Graphics.Primaries.Figure)src.Clone(); Set(c, p); c.asPolyline.Closed = false; App.ModelSpace.Entities.AddItem(c, null, null, true, false); }
App.Redraw(true); Out.AppendLine("applied"); return null;
