// MPA 07: one grade per drive. G6390 EWD and G6390 RAD run at +1:50 from their take-off on the decline (the decline is
// already -1:7 throughout). The cut wall strings and fillet strips follow the new floor heights (each vertex takes the
// floor of the centreline it is nearest); the hatched fillets are rebuilt, not edited in place. Dry run unless APPLY.
const bool APPLY = false; const double G = 50.0;
const string CL = "0\\DESIGN DRAFT\\CENTERLINES", WL = "0\\DESIGN DRAFT\\WALLS", DS = "0\\DESIGN DRAFT\\STRIPPING";
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
List<double[]> P3(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
void Set(Deswik.Graphics.Primaries.Figure f, List<double[]> p) { var pl = f.asPolyline; bool c = pl.Closed; while (pl.VertexCount > 0) pl.RemoveVertexAt(0); foreach (var q in p) pl.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]); pl.Closed = c; }
var cls = new Dictionary<string, Deswik.Graphics.Primaries.Figure>(); var wallsF = new List<Deswik.Graphics.Primaries.Figure>(); var fil = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || A(f, "CLAUDE_MPA07") != "1" || !f.IsPolyline) continue;
    if (f.Layer.Name == CL) cls[A(f, "Name")] = f;
    else if (f.Layer.Name == WL) wallsF.Add(f);
    else if (f.Layer.Name == DS && f.asPolyline.VertexCount == 3) fil.Add(f);
}
var newCl = new Dictionary<string, List<double[]>>();
foreach (var n in new[] { "G6390 EWD", "G6390 RAD" })
{
    var p = P3(cls[n]); double s = 0; var r = new List<double[]> { p[0] };
    for (int i = 1; i < p.Count; i++) { s += Math.Sqrt(Math.Pow(p[i][0] - p[i - 1][0], 2) + Math.Pow(p[i][1] - p[i - 1][1], 2)); r.Add(new[] { p[i][0], p[i][1], p[0][2] + s / G }); }
    newCl[n] = r;
    Out.AppendLine($"{n}: RL {p[0][2]:F2} -> {r[^1][2]:F2} at +1:{G} over {s:F1} m (was {p[^1][2]:F2}, end moves {r[^1][2] - p[^1][2]:+0.00;-0.00} m)");
}
var dec = P3(cls["MAIN DEC"]);
// floor at (x,y): from the nearest centreline (decline or a branch), interpolated along its nearest segment
(double d, double z) Near(List<double[]> D, double x, double y)
{
    double bd = 1e9, bz = 0;
    for (int i = 1; i < D.Count; i++) { double ax = D[i - 1][0], ay = D[i - 1][1], vx = D[i][0] - ax, vy = D[i][1] - ay, L2 = vx * vx + vy * vy; double t = L2 > 0 ? Math.Max(0, Math.Min(1, ((x - ax) * vx + (y - ay) * vy) / L2)) : 0; double d = Math.Sqrt(Math.Pow(ax + t * vx - x, 2) + Math.Pow(ay + t * vy - y, 2)); if (d < bd) { bd = d; bz = D[i - 1][2] + t * (D[i][2] - D[i - 1][2]); } }
    return (bd, bz);
}
var oldCl = new Dictionary<string, List<double[]>> { { "G6390 EWD", P3(cls["G6390 EWD"]) }, { "G6390 RAD", P3(cls["G6390 RAD"]) } };
// shift = new floor - old floor of the nearest branch centreline (0 for points nearest the decline), so walls and
// fillets keep their height above the floor
double Shift(double x, double y)
{
    var dd = Near(dec, x, y); double best = dd.d, sh = 0;
    foreach (var n in newCl.Keys) { var o = Near(oldCl[n], x, y); if (o.d < best) { best = o.d; sh = Near(newCl[n], x, y).z - o.z; } }
    return sh;
}
double maxW = 0; var wallNew = new List<(Deswik.Graphics.Primaries.Figure f, List<double[]> p)>();
foreach (var f in wallsF) { var p = P3(f).Select(q => new[] { q[0], q[1], q[2] + Shift(q[0], q[1]) }).ToList(); maxW = Math.Max(maxW, P3(f).Zip(p, (a, b) => Math.Abs(a[2] - b[2])).Max()); wallNew.Add((f, p)); }
var filNew = new List<(Deswik.Graphics.Primaries.Figure f, List<double[]> p)>();
foreach (var f in fil) filNew.Add((f, P3(f).Select(q => new[] { q[0], q[1], q[2] + Shift(q[0], q[1]) }).ToList()));
Out.AppendLine($"{wallNew.Count} wall strings follow (largest change {maxW:F2} m), {filNew.Count} fillet strips rebuilt");
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }
foreach (var n in newCl.Keys) Set(cls[n], newCl[n]);
foreach (var (f, p) in wallNew) Set(f, p);
foreach (var (f, p) in filNew) { var c = (Deswik.Graphics.Primaries.Figure)f.Clone(); Set(c, p); App.ModelSpace.Entities.AddItem(c, null, null, true, false); f.Deleted = true; }
App.Redraw(true); Out.AppendLine("applied"); return null;
