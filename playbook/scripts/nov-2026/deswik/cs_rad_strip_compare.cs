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
var rb = mine.Where(f => f.Layer.Name.EndsWith("STRIPPING") && f.asPolyline.VertexCount == 4 && P3(f).Max(q => q[2]) - P3(f).Min(q => q[2]) > 1.2 && P3(f).Min(q => D2(q, new[] { 8110.89 + 3.01, 63093.59 - 3.99 })) > 8).ToList();
Out.AppendLine($"RAD-side drop-downs: {rb.Count}");
foreach (var b in rb) { var RB = P3(b); var rc = new[] { RB.Average(q => q[0]), RB.Average(q => q[1]) }; Show("MPA 07 RAD side", RB, mine.Where(f => P3(f).Any(q => D2(q, rc) < R) && (f.Layer.Name.EndsWith("STRIPPING") || P3(f).Count < 60)).ToList()); }
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (!f.Deleted && f.Layer != null && f.Layer.Name.StartsWith("0\\DESIGN DRAFT\\STRIPPING") && (f.IsMText || f.IsText)) { var m = f.BoundingBox.Midpoint; Out.AppendLine($"strip text '{(f.IsMText ? f.asMText.TextString : f.asText.TextString).Replace("\r\n", " ")}' at ({m.x:F1},{m.y:F1},{m.z:F1}) tag={A(f, "CLAUDE_MPA07")}"); }
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (!f.Deleted && f.Layer != null && f.Layer.Name.StartsWith("0\\DESIGN MASTER\\STRIPPING") && f.IsPolyline) { var m = f.BoundingBox.Midpoint; if (Math.Sqrt(Math.Pow(m.x - 8088, 2) + Math.Pow(m.y - 63110, 2)) < 20) Out.AppendLine($"DM strip n={f.asPolyline.VertexCount} mid ({m.x:F1},{m.y:F1},{m.z:F1})"); }
var rad = mine.First(f => f.Layer.Name == "0\\DESIGN DRAFT\\CENTERLINES" && A(f, "Name") == "W6390 RAD"); Out.AppendLine("RAD CL: " + string.Join(" ", P3(rad).Select(q => $"({q[0]:F2},{q[1]:F2},{q[2]:F2})")) + " PROF=" + A(rad, "PROFILE"));
var dc = mine.First(f => f.Layer.Name == "0\\DESIGN DRAFT\\CENTERLINES" && A(f, "ACT_TYPE") == "DEC"); Out.AppendLine("DEC first pts: " + string.Join(" ", P3(dc).Take(4).Select(q => $"({q[0]:F2},{q[1]:F2},{q[2]:F2})")));
return null;