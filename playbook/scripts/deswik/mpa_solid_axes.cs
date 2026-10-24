// Read-only: for every solid on ...\MPA TEST, find its long axis in plan (principal direction of its vertices),
// the two end points 0.4 m inside each end, and the floor height there (lowest GetMinZ over all MPA TEST solids
// at that XY, so boolean-cut junction solids read the true floor). Writes %TEMP%\dsw\mpa_axes.tsv.
var solids = new List<(string h, Deswik.Graphics.Primaries.Figure f, Deswik.Graphics.Figures.Polyface pf)>();
foreach (var e in App.ModelSpace.Entities) {
    var f = e as Deswik.Graphics.Primaries.Figure;
    if (f == null || f.Layer == null || !f.Layer.Name.EndsWith("MPA TEST") || !f.IsPolyface) continue;
    solids.Add((System.Convert.ToString(f.GetType().GetProperty("Handle")?.GetValue(f)), f, f.asPolyface));
}
double Floor(double x, double y) {
    double best = double.MaxValue;
    foreach (var s in solids) { var bb = s.f.BoundingBox; if (x < bb.Min.x - 0.01 || x > bb.Max.x + 0.01 || y < bb.Min.y - 0.01 || y > bb.Max.y + 0.01) continue;
        double z; try { z = s.pf.GetMinZ(x, y, bb); } catch { continue; } if (z < best) best = z; }
    return best;
}
var sb = new System.Text.StringBuilder("Handle\tAx\tAy\tAz\tBx\tBy\tBz\tLen\tWidth\tVol\tZmin\tZmax\n");
foreach (var s in solids) {
    var pts = new List<(double x, double y, double z)>();
    foreach (var v in (System.Collections.IEnumerable)s.pf.GetType().GetProperty("VertexList").GetValue(s.pf)) {
        var t = v.GetType();
        pts.Add((System.Convert.ToDouble(t.GetProperty("x").GetValue(v)), System.Convert.ToDouble(t.GetProperty("y").GetValue(v)), System.Convert.ToDouble(t.GetProperty("z").GetValue(v))));
    }
    double mx = pts.Average(p => p.x), my = pts.Average(p => p.y);
    double sxx = pts.Sum(p => (p.x - mx) * (p.x - mx)), syy = pts.Sum(p => (p.y - my) * (p.y - my)), sxy = pts.Sum(p => (p.x - mx) * (p.y - my));
    double ang = 0.5 * System.Math.Atan2(2 * sxy, sxx - syy); double ux = System.Math.Cos(ang), uy = System.Math.Sin(ang);
    double pmin = pts.Min(p => (p.x - mx) * ux + (p.y - my) * uy), pmax = pts.Max(p => (p.x - mx) * ux + (p.y - my) * uy);
    double wmin = pts.Min(p => -(p.x - mx) * uy + (p.y - my) * ux), wmax = pts.Max(p => -(p.x - mx) * uy + (p.y - my) * ux);
    double wc = (wmin + wmax) / 2; // centre across the drive
    double ax = mx + (pmin + 0.4) * ux - wc * uy, ay = my + (pmin + 0.4) * uy + wc * ux;
    double bx = mx + (pmax - 0.4) * ux - wc * uy, by = my + (pmax - 0.4) * uy + wc * ux;
    double az = Floor(ax, ay), bz = Floor(bx, by);
    double vol = -1; try { vol = s.pf.Volume; } catch { }
    var bb = s.f.BoundingBox;
    sb.Append(s.h).Append('\t').Append($"{ax:F3}\t{ay:F3}\t{az:F3}\t{bx:F3}\t{by:F3}\t{bz:F3}\t{pmax - pmin:F2}\t{wmax - wmin:F2}\t{vol:F1}\t{bb.Min.z:F2}\t{bb.Max.z:F2}").Append('\n');
}
System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "dsw", "mpa_axes.tsv"), sb.ToString());
Out.AppendLine("solids: " + solids.Count);
return null;
