// Read-only: truck loading strip with a drop-down - the reference standards vs the MPA 07 DSP one, in a local frame:
// origin = middle of the drop-down top edge, u = along that edge (down the decline), v = into the leg, z = above the top
// edge. Each group = polylines (not SOLIDS) within R of its drop-down and nearer to it than to any other drop-down.
const double R = 14;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
List<double[]> P3(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
double D2(double[] a, double[] b) => Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2));
var std = new List<Deswik.Graphics.Primaries.Figure>(); var labs = new List<Deswik.Graphics.Primaries.Figure>(); var mine = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null) continue; var ln = f.Layer.Name;
    if (ln.StartsWith("0\\REFERENCE\\STRIP STANDARDS") && !ln.EndsWith("SOLIDS")) { if (f.IsMText) labs.Add(f); else if (f.IsPolyline) std.Add(f); }
    else if (A(f, "CLAUDE_MPA07") == "1" && f.IsPolyline && (ln == "0\\DESIGN DRAFT\\STRIPPING" || ln == "0\\DESIGN DRAFT\\WALLS" || ln == "0\\DESIGN DRAFT\\CENTERLINES")) mine.Add(f);
}
bool IsBox(List<double[]> p) => p.Count == 4 && p.Max(q => q[2]) - p.Min(q => q[2]) > 1.9 && D2(p[0], p[1]) > 3 && D2(p[1], p[2]) > 3;
void Show(string title, List<double[]> B, List<Deswik.Graphics.Primaries.Figure> grp)
{
    var top = Enumerable.Range(0, 4).OrderByDescending(i => B[i][2]).Take(2).ToList(); var a = B[top[0]]; var b = B[top[1]];
    double ox = (a[0] + b[0]) / 2, oy = (a[1] + b[1]) / 2, oz = (a[2] + b[2]) / 2, ux = b[0] - a[0], uy = b[1] - a[1], l = Math.Sqrt(ux * ux + uy * uy); ux /= l; uy /= l;
    var low = B.Where((q, i) => !top.Contains(i)).ToList(); double vx = -uy, vy = ux; if ((low[0][0] - ox) * vx + (low[0][1] - oy) * vy < 0) { vx = -vx; vy = -vy; }
    var all = grp.SelectMany(P3).ToList(); double zp = all.Where(q => (q[0] - ox) * ux + (q[1] - oy) * uy > 2).DefaultIfEmpty(new[] { 0.0, 0, oz }).Average(q => q[2]), zn = all.Where(q => (q[0] - ox) * ux + (q[1] - oy) * uy < -2).DefaultIfEmpty(new[] { 0.0, 0, oz }).Average(q => q[2]);
    if (zp > zn) { ux = -ux; uy = -uy; vx = -vx; vy = -vy; vx = -uy; vy = ux; if ((low[0][0] - ox) * vx + (low[0][1] - oy) * vy < 0) { vx = -vx; vy = -vy; } }
    Out.AppendLine($"=== {title}");
    foreach (var f in grp.OrderBy(f => f.Layer.Name))
    {
        var p = P3(f).Select(q => $"({(q[0] - ox) * ux + (q[1] - oy) * uy:F2},{(q[0] - ox) * vx + (q[1] - oy) * vy:F2},{q[2] - oz:F2})");
        Out.AppendLine($"  {f.Layer.Name.Replace("0\\REFERENCE\\STRIP STANDARDS", "STD").Replace("0\\DESIGN DRAFT\\", "")} {A(f, "Name")} n={f.asPolyline.VertexCount} closed={f.asPolyline.Closed}: " + string.Join(" ", p));
    }
}
var boxes = std.Where(f => IsBox(P3(f))).ToList();
foreach (var bx in boxes)
{
    var B = P3(bx); var c = new[] { B.Average(q => q[0]), B.Average(q => q[1]) };
    var lab = labs.OrderBy(t => Math.Pow(t.BoundingBox.Midpoint.x - c[0], 2) + Math.Pow(t.BoundingBox.Midpoint.y - c[1], 2)).First().asMText.TextString.Replace("\r\n", " ").Trim();
    if (!lab.Contains("Truck Loading") || !lab.Contains("1:7 down") || !lab.Contains("8.0")) continue;
    var grp = std.Where(f => { var m = f.BoundingBox.Midpoint; var mm = new[] { m.x, m.y }; double d = D2(mm, c); return d < R && boxes.All(o => { var Q = P3(o); return D2(mm, new[] { Q.Average(q => q[0]), Q.Average(q => q[1]) }) >= d - 1e-6; }); }).ToList();
    Show("STANDARD " + lab, B, grp);
}
var mb = mine.First(f => f.Layer.Name.EndsWith("STRIPPING") && IsBox(P3(f)) && P3(f).Min(q => D2(q, new[] { 8110.89, 63093.59 })) < 6);
var MB = P3(mb); var mc = new[] { MB.Average(q => q[0]), MB.Average(q => q[1]) };
Show("MPA 07 W6390 DSP", MB, mine.Where(f => P3(f).Any(q => D2(q, mc) < R) && (f.Layer.Name.EndsWith("STRIPPING") || P3(f).Count < 60)).ToList());
return null;
