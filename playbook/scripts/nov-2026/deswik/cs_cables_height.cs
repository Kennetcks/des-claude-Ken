// Cable bolt collars (points on CABLEBOLTS) to the right height: on the strip surface where the point lies inside a strip
// panel (plane fitted to the panel's vertices within 3 m), otherwise on the backs of the nearest drive (floor + profile H).
// Dry run unless APPLY; only points inside strips are moved unless MOVE_OUTSIDE.
const bool APPLY = false, MOVE_OUTSIDE = false; const double TOL = 0.05;
const string CB = "0\\DESIGN MASTER\\CABLEBOLTS", CL = "0\\DESIGN MASTER\\CENTRELINES", SL = "0\\DESIGN MASTER\\STRIPPING";
var inv = System.Globalization.CultureInfo.InvariantCulture;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
List<Deswik.Graphics.Geometry.Point> Pts(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => f.asPolyline.GetVertex(i)).ToList();
(double d, double z) Near(List<Deswik.Graphics.Geometry.Point> D, double x, double y)
{
    double bd = 1e9, bz = 0;
    for (int i = 1; i < D.Count; i++) { double ax = D[i - 1].x, ay = D[i - 1].y, vx = D[i].x - ax, vy = D[i].y - ay, L2 = vx * vx + vy * vy; double t = L2 > 0 ? Math.Max(0, Math.Min(1, ((x - ax) * vx + (y - ay) * vy) / L2)) : 0; double d = Math.Sqrt(Math.Pow(ax + t * vx - x, 2) + Math.Pow(ay + t * vy - y, 2)); if (d < bd) { bd = d; bz = D[i - 1].z + t * (D[i].z - D[i - 1].z); } }
    return (bd, bz);
}
bool Inside(List<Deswik.Graphics.Geometry.Point> P, double x, double y)
{
    bool c = false; for (int i = 0, j = P.Count - 1; i < P.Count; j = i++) if ((P[i].y > y) != (P[j].y > y) && x < (P[j].x - P[i].x) * (y - P[i].y) / (P[j].y - P[i].y) + P[i].x) c = !c; return c;
}
double PlaneZ(List<Deswik.Graphics.Geometry.Point> P, double x, double y)
{   // least-squares z = a + b dx + c dy over the panel's vertices near the point (all of them if fewer than 3 are near)
    var Q = P.OrderBy(q => Math.Pow(q.x - x, 2) + Math.Pow(q.y - y, 2)).Take(8).ToList();
    double Idw() { var N = Q.Take(4).ToList(); double ws = 0, zs = 0; foreach (var q in N) { double w = 1 / (1e-6 + Math.Pow(q.x - x, 2) + Math.Pow(q.y - y, 2)); ws += w; zs += w * q.z; } return zs / ws; }
    double[,] M = new double[3, 3]; double[] r = new double[3];
    foreach (var q in Q) { double[] v = { 1, q.x - x, q.y - y }; for (int i = 0; i < 3; i++) { r[i] += v[i] * q.z; for (int j = 0; j < 3; j++) M[i, j] += v[i] * v[j]; } }
    double Det(double[,] m) => m[0, 0] * (m[1, 1] * m[2, 2] - m[1, 2] * m[2, 1]) - m[0, 1] * (m[1, 0] * m[2, 2] - m[1, 2] * m[2, 0]) + m[0, 2] * (m[1, 0] * m[2, 1] - m[1, 1] * m[2, 0]);
    double d = Det(M); if (Math.Abs(d) < 1e-9) return Idw();
    var M0 = (double[,])M.Clone(); for (int i = 0; i < 3; i++) M0[i, 0] = r[i];
    double zp = Det(M0) / d;   // value at (x, y)
    return zp >= P.Min(q => q.z) - 0.05 && zp <= P.Max(q => q.z) + 0.05 ? zp : Idw();   // a poor fit falls back to the nearest corners
}
var pts = new List<Deswik.Graphics.Primaries.Figure>(); var cls = new List<Deswik.Graphics.Primaries.Figure>(); var strips = new List<(Deswik.Graphics.Primaries.Figure f, List<Deswik.Graphics.Geometry.Point> p)>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null) continue;
    if (f.Layer.Name == CB && f.IsPoint) pts.Add(f);
    else if (f.Layer.Name == CL && f.IsPolyline && f.asPolyline.VertexCount >= 2) cls.Add(f);
    else if (f.Layer.Name == SL && f.IsPolyline && f.asPolyline.VertexCount >= 3) strips.Add((f, Pts(f)));
}
int nIn = 0, nOut = 0, moved = 0, ok = 0;
foreach (var f in pts)
{
    var ip0 = f.asPoint.InsertionPoint; double x = ip0.x, y = ip0.y, z = ip0.z;
    // only strips within 3 m of the cable's height: drives on other levels overlap in plan
    var hitsAll = strips.Where(s => Inside(s.p, x, y)).Select(s => (s, z: PlaneZ(s.p, x, y))).Where(t => t.z >= t.s.p.Min(q => q.z) - 0.1 && t.z <= t.s.p.Max(q => q.z) + 0.1).ToList();
    var hits = hitsAll.Where(t => Math.Abs(t.z - z) < 3).ToList();
    if (hits.Count == 0 && hitsAll.Count > 0)
    {   // over a strip but more than 3 m off: still that strip's cable when no drive runs at the cable's own level
        bool driveHere = cls.Any(c => { var n = Near(Pts(c), x, y); return n.d < 5 && n.z <= z + 1 && n.z + 7.5 >= z; });
        if (!driveHere) { var nz = hitsAll.OrderBy(t => Math.Abs(t.z - z)).First(); hits = hitsAll.Where(t => t.s.f == nz.s.f || Math.Abs(t.z - nz.z) < 2).ToList(); }
    }
    double target; string where;
    if (hits.Count > 0)
    {   // overlapping panels share edges: take the highest surface (the strip top wins over a ramp at a shared edge)
        var best = hits.OrderByDescending(t => t.z).First();
        target = best.z; where = $"strip '{A(best.s.f, "Name")}' {(best.s.f.asPolyline.HatchProperties?.FillBackColor.ColorIndex == 253 ? "ramp" : "high")}"; nIn++;
    }
    else
    {
        double Hof(Deswik.Graphics.Primaries.Figure cf) { var pm = System.Text.RegularExpressions.Regex.Match(A(cf, "PROFILE"), @"([\d.]+)\s*mH"); return pm.Success ? double.Parse(pm.Groups[1].Value, inv) : 5.5; }
        var c = cls.Select(c => (c, n: Near(Pts(c), x, y))).Where(t => t.n.d < 5 && t.n.z <= z + 1 && t.n.z + Hof(t.c) + 3 >= z).OrderBy(t => t.n.d).FirstOrDefault();
        if (c.c == null) { Out.AppendLine($"({x:F1},{y:F1}) z {z:F2}: no drive within 5 m at this level [not moved]"); nOut++; continue; }
        double H = Hof(c.c);
        target = c.n.z + H; where = $"backs of '{A(c.c, "Name")}' ({c.n.d:F1} m off)"; nOut++;
    }
    double dz = target - z;
    if (Math.Abs(dz) <= TOL) { ok++; continue; }
    bool move = hits.Count > 0 || MOVE_OUTSIDE;
    Out.AppendLine($"({x:F1},{y:F1}) z {z:F2} -> {target:F2} ({dz:+0.00;-0.00}) on {where}{(move ? "" : " [not moved: outside strips]")}");
    if (APPLY && move) { var pt = f.asPoint; var ip = pt.InsertionPoint; pt.InsertionPoint = new Deswik.Graphics.Geometry.Point(ip.x, ip.y, target); pt.Update(); moved++; }
}
if (APPLY) App.Redraw(true);
Out.AppendLine($"{pts.Count} cable points: {nIn} inside strips, {nOut} outside; {ok} already within {TOL} m; {(APPLY ? $"moved {moved}" : "dry run - nothing changed")}");
return null;
