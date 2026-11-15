// MPA 07: CB 02 cables at each take-off, laid out as the GSS sheet: 3 rows x 3 at 2 m, centred on the side drive's
// axis, first row 2.25 m from the decline's far wall, last row 6.25 m (inside the side drive's mouth). Heights: on the
// truck strip where inside it, else the backs of the nearest drive. CB 02 label just outside the far wall. Dry run
// unless APPLY.
const bool APPLY = false; const double ROW0 = 2.25, SP = 2.0;
const string CL = "0\\DESIGN DRAFT\\CENTERLINES", DS = "0\\DESIGN DRAFT\\STRIPPING", DB = "0\\DESIGN DRAFT\\CABLEBOLTS";
var inv = System.Globalization.CultureInfo.InvariantCulture;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
List<double[]> P3(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
double Prof(Deswik.Graphics.Primaries.Figure f, string u, double def) { var m = System.Text.RegularExpressions.Regex.Match(A(f, "PROFILE"), @"([\d.]+)\s*m" + u); return m.Success ? double.Parse(m.Groups[1].Value, inv) : def; }
Deswik.Graphics.Primaries.Figure dec = null; var brs = new List<Deswik.Graphics.Primaries.Figure>(); var pts = new List<Deswik.Graphics.Primaries.Figure>(); var labs = new List<Deswik.Graphics.Primaries.Figure>(); var strips = new List<List<double[]>>();
var seen = new HashSet<string>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || A(f, "CLAUDE_MPA07") != "1") continue;
    if (f.Layer.Name == CL && f.IsPolyline) { if (A(f, "ACT_TYPE") == "DEC") dec = f; else brs.Add(f); }
    else if (f.Layer.Name == DB && f.IsPoint) { var ip = f.asPoint.InsertionPoint; if (seen.Add($"{ip.x:F3},{ip.y:F3},{ip.z:F3}")) pts.Add(f); }
    else if (f.Layer.Name == DB && f.IsMText) labs.Add(f);
    else if (f.Layer.Name == DS && f.IsPolyline) strips.Add(P3(f));
}
Out.AppendLine($"decline {dec != null}, side drives {brs.Count}, cables {pts.Count}, labels {labs.Count}, strip panels {strips.Count}");
if (dec == null || brs.Count != 2 || pts.Count != 18 || labs.Count != 2) { Out.AppendLine("unexpected input - nothing changed"); return null; }
var D = P3(dec); double Wd = Prof(dec, "W", 5.5), Hd = Prof(dec, "H", 5.7);
(double s, double d, double x, double y, double z, double tx, double ty) Proj(List<double[]> P, double x, double y)
{
    (double s, double d, double x, double y, double z, double tx, double ty) b = (0, 1e9, 0, 0, 0, 0, 0); double run = 0;
    for (int i = 1; i < P.Count; i++)
    {
        double ax = P[i - 1][0], ay = P[i - 1][1], vx = P[i][0] - ax, vy = P[i][1] - ay, L = Math.Sqrt(vx * vx + vy * vy); double t = L > 0 ? Math.Max(0, Math.Min(1, ((x - ax) * vx + (y - ay) * vy) / (L * L))) : 0;
        double px = ax + t * vx, py = ay + t * vy, d = Math.Sqrt(Math.Pow(px - x, 2) + Math.Pow(py - y, 2));
        if (d < b.d) b = (run + t * L, d, px, py, P[i - 1][2] + t * (P[i][2] - P[i - 1][2]), vx / L, vy / L); run += L;
    }
    return b;
}
bool Inside(List<double[]> P, double x, double y) { bool c = false; for (int i = 0, j = P.Count - 1; i < P.Count; j = i++) if ((P[i][1] > y) != (P[j][1] > y) && x < (P[j][0] - P[i][0]) * (y - P[i][1]) / (P[j][1] - P[i][1]) + P[i][0]) c = !c; return c; }
double Idw(List<double[]> P, double x, double y) { var N = P.OrderBy(q => Math.Pow(q[0] - x, 2) + Math.Pow(q[1] - y, 2)).Take(4).ToList(); double ws = 0, zs = 0; foreach (var q in N) { double w = 1 / (1e-6 + Math.Pow(q[0] - x, 2) + Math.Pow(q[1] - y, 2)); ws += w; zs += w * q[2]; } return zs / ws; }
var place = new List<double[]>(); var labPos = new List<double[]>(); int onStrip = 0;
foreach (var br in brs.OrderBy(b => Proj(D, P3(b)[0][0], P3(b)[0][1]).s))
{
    var bc = P3(br); double Mc = Prof(br, "H", 5.0);
    var pj = Proj(D, bc[0][0], bc[0][1]);                                         // where the side drive meets the decline
    double nx = bc[0][0] - pj.x, ny = bc[0][1] - pj.y, nl = Math.Sqrt(nx * nx + ny * ny); nx /= nl; ny /= nl;   // towards the side drive
    double bx = bc[1][0] - bc[0][0], by = bc[1][1] - bc[0][1], bl = Math.Sqrt(bx * bx + by * by); bx /= bl; by /= bl;
    // centre the columns on the side drive's axis where it crosses the decline centreline
    double tt = ((pj.x - bc[0][0]) * -ny + (pj.y - bc[0][1]) * nx);              // along the decline from pj to the axis
    double ax0 = bc[0][0] - bx * (nl / Math.Max(0.3, bx * nx + by * ny)), ay0 = bc[0][1] - by * (nl / Math.Max(0.3, bx * nx + by * ny));
    var c0 = Proj(D, ax0, ay0); double fx = c0.x - nx * Wd / 2, fy = c0.y - ny * Wd / 2;  // far wall, on the axis
    for (int i = 0; i < 3; i++) for (int j = -1; j <= 1; j++)
    {
        double x = fx + nx * (ROW0 + SP * i) + c0.tx * SP * j, y = fy + ny * (ROW0 + SP * i) + c0.ty * SP * j;
        var hit = strips.Where(p => Inside(p, x, y)).Select(p => Idw(p, x, y)).OrderByDescending(z => z).ToList();
        double z; if (hit.Count > 0) { z = hit[0]; onStrip++; } else { var a = Proj(D, x, y); var b = Proj(bc, x, y); z = a.d - Wd / 2 <= b.d ? a.z + Hd : b.z + Mc; }
        place.Add(new[] { x, y, z });
    }
    labPos.Add(new[] { fx - nx * 1.5, fy - ny * 1.5, c0.z + Hd });
    Out.AppendLine($"{A(br, "Name")}: rows at {ROW0}, {ROW0 + SP}, {ROW0 + 2 * SP} m from the far wall (decline {Wd} m wide, so the last row is {ROW0 + 2 * SP - Wd:F2} m into the side drive)");
}
Out.AppendLine($"cables: {place.Count}, {onStrip} on a truck strip");
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }
for (int i = 0; i < pts.Count; i++) pts[i].asPoint.InsertionPoint = new Deswik.Graphics.Geometry.Point(place[i][0], place[i][1], place[i][2]);
var lo = labs.OrderBy(l => Proj(D, l.asMText.InsertionPoint.x, l.asMText.InsertionPoint.y).s).ToList();
for (int i = 0; i < 2; i++) lo[i].asMText.InsertionPoint = new Deswik.Graphics.Geometry.Point(labPos[i][0], labPos[i][1], labPos[i][2]);
App.Redraw(true); Out.AppendLine("applied"); return null;
