// Read-only: MPA 07 sketch solids (0\SKETCH LAYER\MPA 07) - cut centre, azimuth, floor RL (lowest vertex near the centre)
// and where each sits against the draft MAIN DEC (4587A08) and 44BD9C0: chainage from the joint, plan offset, floor - CL RL.
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
var inv = System.Globalization.CultureInfo.InvariantCulture;
List<double[]> P3(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
double D2(double[] a, double[] b) => Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2));
Deswik.Graphics.Primaries.Figure M = null, Dd = null; var sol = new List<Deswik.Graphics.Primaries.Figure>(); var lays = new Dictionary<string, int>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null) continue;
    if (f.Handle == "44BD9C0") M = f; if (f.Handle == "4587A08") Dd = f;
    if (f.Layer.Name.ToUpper().Contains("MPA 07") || f.Layer.Name.ToUpper().Contains("MPA07")) { lays[f.Layer.Name + " " + f.GetType().Name + (f.IsPolyface ? " polyface" : "")] = lays.TryGetValue(f.Layer.Name + " " + f.GetType().Name + (f.IsPolyface ? " polyface" : ""), out var c) ? c + 1 : 1; if (f.IsPolyface) sol.Add(f); }
}
foreach (var kv in lays) Out.AppendLine($"layer {kv.Key}: {kv.Value}");
var CL = P3(M).Take(M.asPolyline.VertexCount - 1).Concat(P3(Dd)).ToList(); double sJ = 0; { var m = P3(M); for (int i = 1; i < m.Count; i++) sJ += D2(m[i], m[i - 1]); }
(double s, double off, double z) On(double x, double y)
{
    double bs = 0, bd = 1e9, bo = 0, bz = 0, c0 = 0;
    for (int i = 1; i < CL.Count; i++)
    {
        double vx = CL[i][0] - CL[i - 1][0], vy = CL[i][1] - CL[i - 1][1], L = Math.Sqrt(vx * vx + vy * vy), t = Math.Max(0, Math.Min(1, ((x - CL[i - 1][0]) * vx + (y - CL[i - 1][1]) * vy) / (L * L)));
        double d = Math.Sqrt(Math.Pow(CL[i - 1][0] + t * vx - x, 2) + Math.Pow(CL[i - 1][1] + t * vy - y, 2));
        if (d < bd - 1e-9) { bd = d; bs = c0 + t * L; bo = ((x - CL[i - 1][0]) * vy - (y - CL[i - 1][1]) * vx) / L; bz = CL[i - 1][2] + t * (CL[i][2] - CL[i - 1][2]); }
        c0 += L;
    }
    return (bs - sJ, bo, bz);
}
var rows = new List<(double s, string t)>();
foreach (var f in sol)
{
    var bb = f.BoundingBox; string cp = A(f, "_CPX");
    double x = cp != "" ? double.Parse(cp, inv) : bb.Midpoint.x, y = cp != "" ? double.Parse(A(f, "_CPY"), inv) : bb.Midpoint.y, z = cp != "" ? double.Parse(A(f, "_CPZ"), inv) : bb.Min.z;
    var o = On(x, y);
    rows.Add((o.s, $"{f.Handle} [{f.Layer.Name}] Name='{A(f, "Name")}' cp ({x:F2},{y:F2},{z:F3}) az {A(f, "_Azimuth")} bb z {bb.Min.z:F2}-{bb.Max.z:F2} size {bb.Max.x - bb.Min.x:F1}x{bb.Max.y - bb.Min.y:F1} | decCh {o.s:F2} off {o.off:F2} cpZ-CL {z - o.z:F3} minZ-CL {bb.Min.z - o.z:F3}"));
}
foreach (var r in rows.OrderBy(r => r.s)) Out.AppendLine(r.t);
var f0 = sol.FirstOrDefault(); if (f0 != null) { Out.AppendLine("attributes of first solid:"); foreach (var k in new[] { "_CPX", "_CPY", "_CPZ", "_Azimuth", "_Gradient", "_Dip", "_Length", "_Width", "_Height", "Name", "ACT_TYPE", "PROFILE" }) Out.AppendLine($"  {k}={A(f0, k)}"); }
return null;
