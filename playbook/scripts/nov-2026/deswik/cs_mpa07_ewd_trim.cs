// MPA 07: trim or extend one drive (DRIVE) at its far end so the centreline is 20.0 m (3D) along its last segment, grade kept; move the two end-face corners of
// its wall back by the same amount and set its length label. Dry run unless APPLY.
const bool APPLY = false;
const double TARGET = 20.0;
const string DRIVE = "W6390 EWD";
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
Deswik.Graphics.Primaries.Figure cl = null, wall = null, lab = null; var nameLab = (x: 0.0, y: 0.0); var lens = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null) continue; var ln = f.Layer.Name;
    if (ln == "0\\DESIGN DRAFT\\CENTERLINES" && f.IsPolyline && A(f, "CLAUDE_MPA07") == "1" && A(f, "Name") == DRIVE) cl = f;
    else if (ln == "0\\DESIGN DRAFT\\WALLS" && f.IsPolyline && A(f, "CLAUDE_MPA07") == "1" && A(f, "Name") == DRIVE) wall = f;
    else if (ln.StartsWith("0\\DESIGN DRAFT\\CENTERLINES\\") && f.IsText) { if (f.asText.TextString.StartsWith("Length:")) lens.Add(f); else if (f.asText.TextString.Trim() == DRIVE) { var m = f.BoundingBox.Midpoint; nameLab = (m.x, m.y); } }
}
lab = lens.OrderBy(f => Math.Pow(f.BoundingBox.Midpoint.x - nameLab.x, 2) + Math.Pow(f.BoundingBox.Midpoint.y - nameLab.y, 2)).FirstOrDefault();
if (cl == null || wall == null || lab == null) { Out.AppendLine($"centreline {cl != null}, wall {wall != null}, label {lab != null} - nothing changed"); return null; }
var P = Enumerable.Range(0, cl.asPolyline.VertexCount).Select(i => cl.asPolyline.GetVertex(i)).ToList(); int nv = P.Count;
var p0 = P[nv - 2]; var p1 = P[nv - 1]; double before = 0; for (int i = 1; i < nv - 1; i++) before += Math.Sqrt(Math.Pow(P[i].x - P[i - 1].x, 2) + Math.Pow(P[i].y - P[i - 1].y, 2) + Math.Pow(P[i].z - P[i - 1].z, 2));
double Ls = Math.Sqrt(Math.Pow(p1.x - p0.x, 2) + Math.Pow(p1.y - p0.y, 2) + Math.Pow(p1.z - p0.z, 2)), L3 = before + Ls, k = (TARGET - before) / Ls;
if (k <= 0) { Out.AppendLine("target shorter than the drive before its last segment - nothing changed"); return null; }
double ex = p0.x + (p1.x - p0.x) * k, ey = p0.y + (p1.y - p0.y) * k, ez = p0.z + (p1.z - p0.z) * k, dx = ex - p1.x, dy = ey - p1.y, dz = ez - p1.z;
Out.AppendLine($"{DRIVE} {L3:F2} m -> {TARGET:F1} m: end ({p1.x:F2},{p1.y:F2},{p1.z:F2}) -> ({ex:F2},{ey:F2},{ez:F2}), moved {Math.Sqrt(dx * dx + dy * dy):F2} m {(k < 1 ? "back" : "forward")}, last-segment grade 1:{Math.Sqrt(Math.Pow(p1.x - p0.x, 2) + Math.Pow(p1.y - p0.y, 2)) / (p1.z - p0.z):F1}");
// wall: the two vertices that are the end-face corners (2.5 m either side of the old end)
var wp = Enumerable.Range(0, wall.asPolyline.VertexCount).Select(i => { var q = wall.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
var corners = Enumerable.Range(0, wp.Count).Where(i => Math.Abs(Math.Sqrt(Math.Pow(wp[i][0] - p1.x, 2) + Math.Pow(wp[i][1] - p1.y, 2)) - 2.5) < 0.05).ToList();
Out.AppendLine($"wall: {wp.Count} vertices, end-face corners found {corners.Count}; label \"{lab.asText.TextString}\" -> \"Length: {TARGET:F1}m\"");
if (corners.Count != 2) { Out.AppendLine("end face not found - nothing changed"); return null; }
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }
Deswik.Graphics.Primaries.Figure Replace(Deswik.Graphics.Primaries.Figure src, List<double[]> p)
{
    var c = (Deswik.Graphics.Primaries.Figure)src.Clone(); var pl = c.asPolyline; while (pl.VertexCount > 0) pl.RemoveVertexAt(0);
    foreach (var q in p) pl.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]); pl.Closed = false;
    App.ModelSpace.Entities.AddItem(c, null, null, true, false); src.Deleted = true; return c;
}
foreach (var i in corners) wp[i] = new[] { wp[i][0] + dx, wp[i][1] + dy, wp[i][2] + dz };
Replace(wall, wp);
var np = P.Take(nv - 1).Select(q => new[] { q.x, q.y, q.z }).ToList(); np.Add(new[] { ex, ey, ez }); Replace(cl, np);
lab.asText.TextString = $"Length: {TARGET:F1}m";
App.Redraw(true); Out.AppendLine("applied"); return null;
