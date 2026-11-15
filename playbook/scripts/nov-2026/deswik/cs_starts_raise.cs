// MPA 07: put the W6390 DSP and RAD centreline starts on the decline grade (they sit 0.058 m below it).
// - DSP: whole drive up by the difference, so it keeps 1:50
// - RAD: start up, COG (10.89 m) and end RL held, so the first section regrades (to about 1:9.2) and the 7 m flat stays
// Each drive's drafted wall string follows (height above the centreline kept at the same chainage); its first and last
// vertices (fillet ends on the decline walls) stay. Dry run unless APPLY.
const bool APPLY = false;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
List<double[]> P3(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
double D2(double[] a, double[] b) => Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2));
Deswik.Graphics.Primaries.Figure dec = null; var cl = new Dictionary<string, Deswik.Graphics.Primaries.Figure>(); var wl = new Dictionary<string, Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || !f.IsPolyline || A(f, "CLAUDE_MPA07") != "1") continue; var n = A(f, "Name");
    if (f.Layer.Name == "0\\DESIGN DRAFT\\CENTERLINES") { if (A(f, "ACT_TYPE") == "DEC") dec = f; else if (n == "W6390 DSP" || n == "W6390 RAD") cl[n] = f; }
    if (f.Layer.Name == "0\\DESIGN DRAFT\\WALLS" && (n == "W6390 DSP" || n == "W6390 RAD")) wl[n] = f;
}
double DecZ(double[] q)
{
    var p = P3(dec); double bd = 1e9, bz = 0;
    for (int i = 1; i < p.Count; i++) { double vx = p[i][0] - p[i - 1][0], vy = p[i][1] - p[i - 1][1], L2 = vx * vx + vy * vy, t = Math.Max(0, Math.Min(1, ((q[0] - p[i - 1][0]) * vx + (q[1] - p[i - 1][1]) * vy) / L2)), d = Math.Sqrt(Math.Pow(q[0] - p[i - 1][0] - t * vx, 2) + Math.Pow(q[1] - p[i - 1][1] - t * vy, 2)); if (d < bd) { bd = d; bz = p[i - 1][2] + t * (p[i][2] - p[i - 1][2]); } }
    return bz;
}
// centreline RL at the chainage of a plan point (projected onto the centreline's plan)
double ZAt(List<double[]> P, double[] q)
{
    double bd = 1e9, bz = 0;
    for (int i = 1; i < P.Count; i++)
    {
        double vx = P[i][0] - P[i - 1][0], vy = P[i][1] - P[i - 1][1], L2 = vx * vx + vy * vy, t = ((q[0] - P[i - 1][0]) * vx + (q[1] - P[i - 1][1]) * vy) / L2;
        double tc = Math.Max(i == 1 ? -1 : 0, Math.Min(i == P.Count - 1 ? 2 : 1, t)), d = Math.Sqrt(Math.Pow(q[0] - P[i - 1][0] - tc * vx, 2) + Math.Pow(q[1] - P[i - 1][1] - tc * vy, 2));
        if (d < bd - 1e-9) { bd = d; bz = P[i - 1][2] + tc * (P[i][2] - P[i - 1][2]); }
    }
    return bz;
}
var plan = new List<(Deswik.Graphics.Primaries.Figure f, List<double[]> p)>();
foreach (var name in new[] { "W6390 DSP", "W6390 RAD" })
{
    var P = P3(cl[name]); double dz = DecZ(P[0]) - P[0][2];
    var N = P.Select(q => (double[])q.Clone()).ToList();
    if (name == "W6390 DSP") foreach (var q in N) q[2] += dz;
    else N[0][2] += dz;   // RAD: only the start moves; COG and end held
    Out.AppendLine($"=== {name}: start {P[0][2]:F3} -> {N[0][2]:F3} (+{dz:F3}); " + string.Join(", ", Enumerable.Range(1, N.Count - 1).Select(i => $"seg {i} 1:{D2(N[i], N[i - 1]) / (N[i][2] - N[i - 1][2]):F2} (was 1:{D2(P[i], P[i - 1]) / (P[i][2] - P[i - 1][2]):F2})")));
    plan.Add((cl[name], N));
    var W = P3(wl[name]); var NW = W.Select((q, i) => i == 0 || i == W.Count - 1 ? q : new[] { q[0], q[1], q[2] - ZAt(P, q) + ZAt(N, q) }).ToList();
    Out.AppendLine($"  wall n={W.Count}: " + string.Join(" ", W.Zip(NW, (a, b) => $"{a[2]:F3}->{b[2]:F3}")));
    plan.Add((wl[name], NW));
}
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }
foreach (var (src, p) in plan)
{
    var c = (Deswik.Graphics.Primaries.Figure)src.Clone(); var pl = c.asPolyline; bool closed = src.asPolyline.Closed; while (pl.VertexCount > 0) pl.RemoveVertexAt(0);
    foreach (var q in p) pl.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]); pl.Closed = closed; App.ModelSpace.Entities.AddItem(c, null, null, true, false); src.Deleted = true;
}
App.Redraw(true); Out.AppendLine("applied"); return null;
