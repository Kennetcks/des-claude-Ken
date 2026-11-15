// Read-only: the J/Box Strip standard (polylines near its label, with the nearest decline-like reference string), the
// attributes on an MPA 07 strip panel, and any existing J/Box strips in the drawing.
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
List<double[]> P3(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
double D2(double[] a, double[] b) => Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2));
var all = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (!f.Deleted && f.Layer != null) all.Add(f);
foreach (var t in all.Where(f => f.IsMText && f.Layer.Name.StartsWith("0\\REFERENCE\\STRIP STANDARDS") && f.asMText.TextString.Contains("J/Box")))
{
    var m = t.BoundingBox.Midpoint; var c = new[] { m.x, m.y };
    Out.AppendLine($"=== '{t.asMText.TextString.Replace("\r\n", " ").Replace("\n", " ")}' at ({m.x:F2},{m.y:F2},{m.z:F2})");
    foreach (var f in all.Where(f => f.IsPolyline && f.Layer.Name.StartsWith("0\\REFERENCE") && P3(f).Min(q => D2(q, c)) < 9).OrderBy(f => P3(f).Min(q => D2(q, c))))
    {
        var p = P3(f);
        Out.AppendLine($"  {f.Layer.Name} n={p.Count} closed={f.asPolyline.Closed} " + (p.Count <= 24 ? string.Join(" ", p.Select(q => $"({q[0]:F2},{q[1]:F2},{q[2]:F2})")) : $"bbox ({f.BoundingBox.Min.x:F1},{f.BoundingBox.Min.y:F1})-({f.BoundingBox.Max.x:F1},{f.BoundingBox.Max.y:F1})"));
    }
    foreach (var f in all.Where(f => f.IsMText && f != t && f.Layer.Name.StartsWith("0\\REFERENCE") && D2(new[] { f.BoundingBox.Midpoint.x, f.BoundingBox.Midpoint.y }, c) < 9))
        Out.AppendLine($"  text {f.Layer.Name}: {f.asMText.TextString.Replace("\r\n", " | ")}");
}
var panel = all.FirstOrDefault(f => f.IsPolyline && f.Layer.Name == "0\\DESIGN DRAFT\\STRIPPING" && A(f, "CLAUDE_MPA07") == "1");
if (panel != null)
{
    Out.AppendLine($"MPA07 strip panel attrs:");
    foreach (var k in new[] { "Name", "ACT_TYPE", "MINE", "LEVEL", "PROFILE", "Description", "GSS", "CLAUDE_MPA07", "STO_DEV", "LAT_VERT" }) Out.AppendLine($"  {k}={A(panel, k)}");
}
var lab = all.FirstOrDefault(f => f.IsMText && f.Layer.Name.StartsWith("0\\DESIGN DRAFT") && A(f, "CLAUDE_MPA07") == "1" && f.asMText.TextString.Contains("Truck"));
if (lab != null) Out.AppendLine($"MPA07 strip label layer {lab.Layer.Name}: {lab.asMText.TextString.Replace("\r\n", " | ")}");
foreach (var f in all.Where(f => f.IsMText && !f.Layer.Name.StartsWith("0\\REFERENCE") && f.asMText.TextString.Contains("J/Box")).Take(10))
    Out.AppendLine($"existing J/Box text {f.Layer.Name} ({f.BoundingBox.Midpoint.x:F1},{f.BoundingBox.Midpoint.y:F1},{f.BoundingBox.Midpoint.z:F1}): {f.asMText.TextString.Replace("\r\n", " | ")}");
return null;
