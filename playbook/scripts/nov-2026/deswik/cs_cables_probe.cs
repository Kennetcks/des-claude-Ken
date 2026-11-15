// Read-only: everything on the CABLEBOLTS layer, with the nearest drive centreline (floor z there, profile height) and the
// strip under it if any, so each cable's height can be compared with the backs (floor + H) and the stripped backs (+1.5 m).
const string CB = "0\\DESIGN MASTER\\CABLEBOLTS", CL = "0\\DESIGN MASTER\\CENTRELINES", SL = "0\\DESIGN MASTER\\STRIPPING";
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
List<Deswik.Graphics.Geometry.Point> Pts(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => f.asPolyline.GetVertex(i)).ToList();
(double d, double z) Near(List<Deswik.Graphics.Geometry.Point> D, double x, double y)
{
    double bd = 1e9, bz = 0;
    for (int i = 1; i < D.Count; i++) { double ax = D[i - 1].x, ay = D[i - 1].y, vx = D[i].x - ax, vy = D[i].y - ay, L2 = vx * vx + vy * vy; double t = L2 > 0 ? Math.Max(0, Math.Min(1, ((x - ax) * vx + (y - ay) * vy) / L2)) : 0; double d = Math.Sqrt(Math.Pow(ax + t * vx - x, 2) + Math.Pow(ay + t * vy - y, 2)); if (d < bd) { bd = d; bz = D[i - 1].z + t * (D[i].z - D[i - 1].z); } }
    return (bd, bz);
}
var cbs = new List<Deswik.Graphics.Primaries.Figure>(); var cls = new List<Deswik.Graphics.Primaries.Figure>(); var strips = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null) continue;
    if (f.Layer.Name == CB) cbs.Add(f);
    else if (f.Layer.Name == CL && f.IsPolyline && f.asPolyline.VertexCount >= 2) cls.Add(f);
    else if (f.Layer.Name == SL && f.IsPolyline) strips.Add(f);
}
Out.AppendLine($"CABLEBOLTS: {cbs.Count} objects");
foreach (var f in cbs)
{
    var m = f.BoundingBox.Midpoint; string what = f.IsMText ? "TEXT '" + f.asMText.TextString.Replace("\r\n", " / ").Trim() + "'" : f.IsPolyline ? $"POLY n={f.asPolyline.VertexCount} closed={f.asPolyline.Closed}" : f.GetType().Name;
    Out.AppendLine($"{what} mid=({m.x:F1},{m.y:F1},{m.z:F2}) zmin={f.BoundingBox.Min.z:F2} zmax={f.BoundingBox.Max.z:F2} Name='{A(f, "Name")}' ACT='{A(f, "ACT_TYPE")}' PROFILE='{A(f, "PROFILE")}'");
    if (f.IsPolyline) Out.AppendLine("   pts: " + string.Join(" | ", Pts(f).Take(8).Select(q => $"{q.x:F1},{q.y:F1},{q.z:F2}")));
    foreach (var c in cls.Select(c => (c, n: Near(Pts(c), m.x, m.y))).Where(t => t.n.d < 8).OrderBy(t => t.n.d).Take(2))
    {
        var pm = System.Text.RegularExpressions.Regex.Match(A(c.c, "PROFILE"), @"([\d.]+)\s*mH"); double H = pm.Success ? double.Parse(pm.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : double.NaN;
        Out.AppendLine($"   CL {c.n.d:F1} m '{A(c.c, "Name")}' {A(c.c, "PROFILE")}: floor {c.n.z:F2}, backs {c.n.z + H:F2}, stripped backs {c.n.z + H + 1.5:F2}");
    }
    var under = strips.Where(s => { var b = s.BoundingBox; return m.x >= b.Min.x && m.x <= b.Max.x && m.y >= b.Min.y && m.y <= b.Max.y; }).ToList();
    foreach (var s in under) Out.AppendLine($"   inside strip bbox '{A(s, "Name")}' tag={A(s, "CLAUDE_DECLINE_STRIP")} z {s.BoundingBox.Min.z:F2}-{s.BoundingBox.Max.z:F2}");
}
return null;
