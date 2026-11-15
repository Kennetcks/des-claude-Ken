// Read-only: the RAD-side and DSP-side back strips against the strip standards. Each strip set is shown in its drive's
// frame: u = along the decline (down-dip +) from the drive CL start, v = into the drive (+) from the decline CL... measured
// from the decline wall line, z = height above the decline floor at that point. Standards (0\REFERENCE\STRIP STANDARDS*)
// are listed with their text labels, and every standard group with a drop-down box is shown in the same kind of frame.
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
List<double[]> P3(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
double D2(double[] a, double[] b) => Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2));
var H = new Dictionary<string, Deswik.Graphics.Primaries.Figure>(StringComparer.OrdinalIgnoreCase); var std = new List<Deswik.Graphics.Primaries.Figure>(); var stdTxt = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null) continue; H[f.Handle] = f;
    if (f.Layer.Name.StartsWith("0\\REFERENCE\\STRIP STANDARDS") && !f.Layer.Name.EndsWith("SOLIDS")) { if (f.IsPolyline) std.Add(f); else if (f.IsText || f.IsMText) stdTxt.Add(f); }
}
var M = P3(H["44BD9C0"]); var d = P3(H["4588F43"]); var CL = M.Take(M.Count - 1).Concat(d).ToList(); double sJ = 0; for (int i = 1; i < M.Count; i++) sJ += D2(M[i], M[i - 1]);
(double s, double off, double dz) Fr(List<double[]> cl, double[] q)
{
    double bs = 0, bd = 1e9, bo = 0, bz = 0, c0 = 0;
    for (int i = 1; i < cl.Count; i++)
    {
        double vx = cl[i][0] - cl[i - 1][0], vy = cl[i][1] - cl[i - 1][1], L = Math.Sqrt(vx * vx + vy * vy), t = Math.Max(0, Math.Min(1, ((q[0] - cl[i - 1][0]) * vx + (q[1] - cl[i - 1][1]) * vy) / (L * L)));
        double dd = Math.Sqrt(Math.Pow(cl[i - 1][0] + t * vx - q[0], 2) + Math.Pow(cl[i - 1][1] + t * vy - q[1], 2));
        if (dd < bd - 1e-9) { bd = dd; bs = c0 + t * L; bo = ((q[0] - cl[i - 1][0]) * vy - (q[1] - cl[i - 1][1]) * vx) / L; bz = cl[i - 1][2] + t * (cl[i][2] - cl[i - 1][2]); }
        c0 += L;
    }
    return (bs, bo, q[2] - bz);
}
foreach (var (nm, drv, hs) in new[] { ("RAD side", "4588F59", new[] { "4588F5C", "4588F5E", "4588F60", "4588F62" }), ("DSP side", "4588F46", new[] { "4588F5D", "4588F5F", "4588F61", "4588F63" }) })
{
    double s0 = Fr(CL, P3(H[drv])[0]).s;
    Out.AppendLine($"=== {nm} (drive CL start at decline ch {s0 - sJ:F2}; u = ch - that, v = -(offset) i.e. + towards the drive, z above floor)");
    foreach (var h in hs) { var p = P3(H[h]); Out.AppendLine($"  {h} n={p.Count} closed={H[h].asPolyline.Closed} ACT={A(H[h], "ACT_TYPE")}: " + string.Join(" ", p.Select(q => { var f = Fr(CL, q); return $"({f.s - s0:F2},{-f.off:F2},{f.dz:F2})"; }))); }
}
Out.AppendLine($"=== standards: {std.Count} polylines, {stdTxt.Count} labels");
foreach (var t in stdTxt) { var m = t.BoundingBox.Midpoint; Out.AppendLine($"  label [{t.Layer.Name}] '{(t.IsMText ? t.asMText.TextString : t.asText.TextString).Replace("\r\n", " | ").Replace("\n", " | ")}' at ({m.x:F1},{m.y:F1},{m.z:F1})"); }
foreach (var g in std.GroupBy(f => f.Layer.Name)) Out.AppendLine($"  layer {g.Key}: {g.Count()} polylines, mid " + string.Join(" ", g.Take(30).Select(f => { var m = f.BoundingBox.Midpoint; return $"{f.Handle}({m.x:F0},{m.y:F0},{m.z:F0} n{f.asPolyline.VertexCount})"; })));
// standards with a drop-down: frame from their own centreline (the longest 2-pt-ish polyline nearby on a CENTRE layer) is unknown,
// so show each drop-down group raw relative to the drop-down box's top edge
bool IsBox(List<double[]> p) => p.Count == 4 && p.Max(q => q[2]) - p.Min(q => q[2]) > 1.2 && D2(p[0], p[1]) > 2.5 && D2(p[1], p[2]) > 2.5;
foreach (var b in std.Where(f => IsBox(P3(f))))
{
    var B = P3(b); var top = Enumerable.Range(0, 4).OrderByDescending(i => B[i][2]).Take(2).ToList(); var ta = B[top[0]]; var tb = B[top[1]];
    double ox = (ta[0] + tb[0]) / 2, oy = (ta[1] + tb[1]) / 2, oz = (ta[2] + tb[2]) / 2, ux = tb[0] - ta[0], uy = tb[1] - ta[1], l = Math.Sqrt(ux * ux + uy * uy); ux /= l; uy /= l;
    var low = B.Where((q, i) => !top.Contains(i)).ToList(); double vx = -uy, vy = ux; if ((low[0][0] - ox) * vx + (low[0][1] - oy) * vy < 0) { vx = -vx; vy = -vy; }
    var grp = std.Where(f => P3(f).Any(q => Math.Sqrt(Math.Pow(q[0] - ox, 2) + Math.Pow(q[1] - oy, 2)) < 14)).ToList();
    Out.AppendLine($"--- standard drop-down {b.Handle} [{b.Layer.Name}] top edge {l:F2} m, drop {oz - low.Average(q => q[2]):F2} m, depth {(low[0][0] - ox) * vx + (low[0][1] - oy) * vy:F2} m; group of {grp.Count}:");
    foreach (var f in grp) Out.AppendLine($"    {f.Handle} [{f.Layer.Name.Replace("0\\REFERENCE\\STRIP STANDARDS", "STD")}] n={f.asPolyline.VertexCount}: " + string.Join(" ", P3(f).Take(24).Select(q => $"({(q[0] - ox) * ux + (q[1] - oy) * uy:F2},{(q[0] - ox) * vx + (q[1] - oy) * vy:F2},{q[2] - oz:F2})")));
}
return null;
