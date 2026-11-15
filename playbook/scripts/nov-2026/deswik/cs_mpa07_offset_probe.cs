// Read-only: for every MPA 07 sketch solid, the signed plan offset of its cut centre (_CPX/_CPY) and of its true
// plan centre (mid-width at mid-length, from the vertices) from the nearest MPA 07 centreline (+ = right of travel).
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
var inv = System.Globalization.CultureInfo.InvariantCulture;
var cls = new List<(string name, List<double[]> p)>(); var sol = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null) continue;
    if (f.Layer.Name == "0\\DESIGN DRAFT\\CENTERLINES" && f.IsPolyline && A(f, "CLAUDE_MPA07") == "1") { var pl = f.asPolyline; cls.Add((A(f, "Name"), Enumerable.Range(0, pl.VertexCount).Select(i => { var q = pl.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList())); }
    else if (f.Layer.Name == "0\\SKETCH LAYER\\MPA 07" && f.IsPolyface) sol.Add(f);
}
(string name, double s, double off, double tx, double ty) Near(double x, double y)
{
    (string, double, double, double, double) best = ("", 0, 1e9, 0, 0); double bd = 1e9;
    foreach (var c in cls) { double s0 = 0; for (int i = 1; i < c.p.Count; i++) { var a = c.p[i - 1]; var b = c.p[i]; double vx = b[0] - a[0], vy = b[1] - a[1], L = Math.Sqrt(vx * vx + vy * vy); if (L < 1e-9) continue; double t = Math.Max(0, Math.Min(1, ((x - a[0]) * vx + (y - a[1]) * vy) / (L * L))); double px = a[0] + t * vx, py = a[1] + t * vy, d = Math.Sqrt(Math.Pow(x - px, 2) + Math.Pow(y - py, 2)); if (d < bd) { bd = d; double side = Math.Sign(vx * (y - py) - vy * (x - px)); best = (c.name, s0 + t * L, -side * d, vx / L, vy / L); } s0 += L; } }
    return best;
}
foreach (var s in sol)
{
    double cx = double.Parse(A(s, "_CPX"), inv), cy = double.Parse(A(s, "_CPY"), inv);
    var n = Near(cx, cy);
    // true centre: vertices projected on the local tangent/normal; mid of across-range among vertices near mid-length
    var pts = new List<(double x, double y)>(); foreach (var o in (System.Collections.IEnumerable)s.asPolyface.VertexList) { var t = o.GetType(); pts.Add((Convert.ToDouble(t.GetProperty("x").GetValue(o)), Convert.ToDouble(t.GetProperty("y").GetValue(o)))); }
    double nx = n.ty, ny = -n.tx;   // right-hand normal
    var acr = pts.Select(p => (p.x - cx) * nx + (p.y - cy) * ny).ToList(); double mid = (acr.Min() + acr.Max()) / 2, wid = acr.Max() - acr.Min();
    Out.AppendLine($"{n.name,-10} s {n.s,6:F1}  cut-centre off {n.off,6:F2}  solid mid-width rel. cut centre {mid,6:F2} (width {wid:F2})  => solid centre off {n.off + mid,6:F2}");
}
return null;
