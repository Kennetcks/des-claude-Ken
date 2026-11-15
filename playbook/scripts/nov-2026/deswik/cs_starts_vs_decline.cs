// Read-only: W6390 DSP and RAD centreline starts against the decline - the decline centreline RL projected square to the
// start (and its station/offset), and the nearest decline wall strings (MPA 07 drafted, Drafter outline, Design Master)
// with their RL there. Also the Design Master decline centreline, which the RAD may tie to instead.
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
List<double[]> P3(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
double D2(double[] a, double[] b) => Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2));
var cls = new List<Deswik.Graphics.Primaries.Figure>(); var decs = new List<Deswik.Graphics.Primaries.Figure>(); var walls = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || !f.IsPolyline) continue; var ln = f.Layer.Name;
    if (ln == "0\\DESIGN DRAFT\\CENTERLINES" && (A(f, "Name") == "W6390 DSP" || A(f, "Name") == "W6390 RAD")) cls.Add(f);
    if (A(f, "ACT_TYPE") == "DEC" && (ln == "0\\DESIGN DRAFT\\CENTERLINES" || ln == "0\\DESIGN MASTER\\CENTERLINES")) decs.Add(f);
    if (A(f, "ACT_TYPE") == "DEC" && (ln.EndsWith("\\WALLS"))) walls.Add(f);
}
(double d, double s, double off, double z) Near(List<double[]> p, double[] q)
{
    double bd = 1e9, bs = 0, bo = 0, bz = 0, c = 0;
    for (int i = 1; i < p.Count; i++)
    {
        double vx = p[i][0] - p[i - 1][0], vy = p[i][1] - p[i - 1][1], L = Math.Sqrt(vx * vx + vy * vy); if (L < 1e-9) continue;
        double t = Math.Max(0, Math.Min(1, ((q[0] - p[i - 1][0]) * vx + (q[1] - p[i - 1][1]) * vy) / (L * L))), d = Math.Sqrt(Math.Pow(q[0] - p[i - 1][0] - t * vx, 2) + Math.Pow(q[1] - p[i - 1][1] - t * vy, 2));
        if (d < bd) { bd = d; bs = c + t * L; bo = ((q[0] - p[i - 1][0]) * vy - (q[1] - p[i - 1][1]) * vx) / L; bz = p[i - 1][2] + t * (p[i][2] - p[i - 1][2]); }
        c += L;
    }
    return (bd, bs, bo, bz);
}
foreach (var f in cls)
{
    var p = P3(f); var q = p[0];
    Out.AppendLine($"=== {A(f, "Name")} start ({q[0]:F3},{q[1]:F3},{q[2]:F3}), first segment grade 1:{D2(p[1], p[0]) / (p[1][2] - p[0][2]):F1}");
    foreach (var d in decs) { var n = Near(P3(d), q); if (n.d < 15) Out.AppendLine($"  decline CL {d.Layer.Name} {d.Handle}: ch {n.s:F2} off {n.off:F2}, RL there {n.z:F3} -> start is {q[2] - n.z:+0.000;-0.000} m vs it"); }
    foreach (var w in walls) { var n = Near(P3(w), q); if (n.d < 3) Out.AppendLine($"  decline wall {w.Layer.Name} {w.Handle} n={w.asPolyline.VertexCount}: {n.d:F3} m away, RL {n.z:F3} -> start is {q[2] - n.z:+0.000;-0.000} m vs it"); }
}
return null;
