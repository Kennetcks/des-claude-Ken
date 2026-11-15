// G6390 SUB take-off (Level Design Drafter): add CB 02 cable bolts on 0\DESIGN DRAFT\CABLEBOLTS, laid out as the MPA 07
// take-offs (cs_mpa07_cables.cs): 3 rows x 3 at 2 m, centred on the SUB axis, rows 2.25 / 4.25 / 6.25 m from the decline's
// far wall; collars on the backs (decline floor + 5.7 m inside the decline, SUB floor + 5.0 m inside the SUB, or on a strip
// where one covers the point); a "CB 02" label 1.5 m outside the far wall, text along the decline. Points and label are
// copies of Design Master templates (458941C point, 458942D label) with the SUB's attributes. Dry run unless APPLY.
const bool APPLY = false; const double ROW0 = 2.25, SP = 2.0, WD = 5.5, HD = 5.7, MC = 5.0;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
List<double[]> P3(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
var H = new Dictionary<string, Deswik.Graphics.Primaries.Figure>(StringComparer.OrdinalIgnoreCase); var strips = new List<List<double[]>>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null) continue; H[f.Handle] = f;
    if ((f.Layer.Name == "0\\DESIGN DRAFT\\STRIPPING" || f.Layer.Name == "0\\DESIGN MASTER\\STRIPPING") && f.IsPolyline && f.asPolyline.VertexCount >= 3) strips.Add(P3(f));
}
foreach (var h in new[] { "44BD9C0", "45894AC", "458941C", "458942D" }) if (!H.ContainsKey(h)) { Out.AppendLine(h + " missing - nothing done"); return null; }
Deswik.Graphics.Primaries.Layer LB = null; foreach (Deswik.Graphics.Primaries.Layer l in App.Layers) if (l.Name == "0\\DESIGN DRAFT\\CABLEBOLTS") LB = l;
var D = P3(H["44BD9C0"]); var sub = H["45894AC"]; var bc = P3(sub);
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
// SUB axis (square to the decline): n = into the SUB; axis crosses the decline CL at c0
double nx = bc[1][0] - bc[0][0], ny = bc[1][1] - bc[0][1], nl = Math.Sqrt(nx * nx + ny * ny); nx /= nl; ny /= nl;
var pj = Proj(D, bc[0][0], bc[0][1]); double back = pj.d;   // start is on the decline wall, pj.d = 2.75 m off the CL
var c0 = Proj(D, bc[0][0] - nx * back, bc[0][1] - ny * back);
double fx = c0.x - nx * WD / 2, fy = c0.y - ny * WD / 2;   // decline far wall, on the SUB axis
var place = new List<double[]>(); int onStrip = 0; var where = new List<string>();
for (int i = 0; i < 3; i++) for (int j = -1; j <= 1; j++)
{
    double x = fx + nx * (ROW0 + SP * i) + c0.tx * SP * j, y = fy + ny * (ROW0 + SP * i) + c0.ty * SP * j;
    var hit = strips.Where(p => Inside(p, x, y)).Select(p => Idw(p, x, y)).OrderByDescending(z => z).ToList();
    double z; string w;
    if (hit.Count > 0) { z = hit[0]; onStrip++; w = "strip"; }
    else { var a = Proj(D, x, y); var b = Proj(bc, x, y); if (a.d - WD / 2 <= b.d) { z = a.z + HD; w = "decline backs"; } else { z = b.z + MC; w = "SUB backs"; } }
    place.Add(new[] { x, y, z }); where.Add($"row {i + 1} col {j + 2}: ({x:F2},{y:F2},{z:F2}) {w}");
}
var lab = new[] { fx - nx * 1.5, fy - ny * 1.5, c0.z + HD };
double rot = Math.Atan2(c0.ty, c0.tx) * 180 / Math.PI;   // text along the decline (math angle, deg)
Out.AppendLine($"draft CABLEBOLTS layer found: {LB != null}; strips near: {strips.Count(p => p.Any(q => Math.Abs(q[0] - fx) < 15 && Math.Abs(q[1] - fy) < 15))}");
Out.AppendLine($"SUB axis crosses the decline CL at ({c0.x:F2},{c0.y:F2}) floor {c0.z:F2}; far wall ({fx:F2},{fy:F2}); decline {WD} x {HD}, SUB {A(sub, "PROFILE")}");
foreach (var s in where) Out.AppendLine("  " + s);
Out.AppendLine($"cables 9 ({onStrip} on a strip); label 'CB 02' at ({lab[0]:F2},{lab[1]:F2},{lab[2]:F2}) rotation {rot:F1}");
// anything already on the draft cable layer near the mouth?
int existing = H.Values.Count(f => f.Layer.Name == "0\\DESIGN DRAFT\\CABLEBOLTS" && Math.Sqrt(Math.Pow(f.BoundingBox.Midpoint.x - c0.x, 2) + Math.Pow(f.BoundingBox.Midpoint.y - c0.y, 2)) < 10);
Out.AppendLine($"existing draft cable figures within 10 m: {existing}");
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }
if (LB == null || existing > 0) { Out.AppendLine("draft cable layer missing or cables already there - nothing done"); return null; }
void Attrs(Deswik.Graphics.Primaries.Figure k) { foreach (var a in new[] { "Name", "ACT_TYPE", "GSS", "PROFILE", "LEVEL", "MINE" }) { var v = A(sub, a); if (v != "") k.AttributeValueSet(a, v); } }
var added = new List<string>();
foreach (var p in place)
{
    var k = (Deswik.Graphics.Primaries.Figure)H["458941C"].Clone(); k.Deleted = false; k.Layer = LB; Attrs(k);
    k.asPoint.InsertionPoint = new Deswik.Graphics.Geometry.Point(p[0], p[1], p[2]); App.ModelSpace.Entities.AddItem(k, null, null, true, false); added.Add(k.Handle);
}
var t = (Deswik.Graphics.Primaries.Figure)H["458942D"].Clone(); t.Deleted = false; t.Layer = LB; Attrs(t);
t.asMText.TextString = "CB 02"; t.asMText.InsertionPoint = new Deswik.Graphics.Geometry.Point(lab[0], lab[1], lab[2]); t.asMText.Rotation = rot;
App.ModelSpace.Entities.AddItem(t, null, null, true, false); added.Add(t.Handle);
App.Redraw(true);
Out.AppendLine("added on 0\\DESIGN DRAFT\\CABLEBOLTS: " + string.Join(", ", added));
return null;
