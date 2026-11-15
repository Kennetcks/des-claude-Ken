// Read-only: W6390 RAD walls against its centreline - each wall vertex's chainage, offset and height above the centreline,
// for the drafted WALLS string and the Drafter-generated CENTERLINES\WALLS outline; plus the decline wall near the mouth.
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
List<double[]> P3(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
double D2(double[] a, double[] b) => Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2));
Deswik.Graphics.Primaries.Figure cl = null; var ws = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || !f.IsPolyline || A(f, "Name") != "W6390 RAD") continue;
    if (f.Layer.Name == "0\\DESIGN DRAFT\\CENTERLINES") cl = f;
    if (f.Layer.Name == "0\\DESIGN DRAFT\\WALLS" || f.Layer.Name == "0\\DESIGN DRAFT\\CENTERLINES\\WALLS") ws.Add(f);
}
var P = P3(cl); var C = new List<double> { 0 }; for (int i = 1; i < P.Count; i++) C.Add(C[i - 1] + D2(P[i], P[i - 1]));
Out.AppendLine($"centreline {cl.Handle}: " + string.Join(" ", P.Select((q, i) => $"[{C[i]:F2}] ({q[0]:F3},{q[1]:F3},{q[2]:F3})")));
(double s, double off, double h) On(double[] q)
{
    double bs = 0, bd = 1e9, bo = 0, bz = 0;
    for (int i = 1; i < P.Count; i++)
    {
        double vx = P[i][0] - P[i - 1][0], vy = P[i][1] - P[i - 1][1], L = Math.Sqrt(vx * vx + vy * vy), t = ((q[0] - P[i - 1][0]) * vx + (q[1] - P[i - 1][1]) * vy) / (L * L);
        double tc = Math.Max(i == 1 ? -1 : 0, Math.Min(i == P.Count - 1 ? 2 : 1, t)), px = P[i - 1][0] + tc * vx, py = P[i - 1][1] + tc * vy, d = Math.Sqrt(Math.Pow(q[0] - px, 2) + Math.Pow(q[1] - py, 2));
        if (d < bd - 1e-9) { bd = d; bs = C[i - 1] + tc * L; bo = ((q[0] - P[i - 1][0]) * vy - (q[1] - P[i - 1][1]) * vx) / L; bz = P[i - 1][2] + tc * (P[i][2] - P[i - 1][2]); }
    }
    return (bs, bo, q[2] - bz);
}
foreach (var w in ws)
{
    Out.AppendLine($"=== {w.Layer.Name} {w.Handle} n={w.asPolyline.VertexCount} closed={w.asPolyline.Closed}");
    var p = P3(w);
    for (int i = 0; i < p.Count; i++) { var o = On(p[i]); Out.AppendLine($"  ({p[i][0]:F3},{p[i][1]:F3},{p[i][2]:F3}) ch {o.s:F2} off {o.off:F2} dz {o.h:F3}" + (i > 0 ? $" seg {D2(p[i], p[i - 1]):F2}" : "")); }
}
return null;
