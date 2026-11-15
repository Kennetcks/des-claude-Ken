// Read-only: the MPA 07 J/Box strip(s) and label(s) on STRIPPING.
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
var c = new[] { 8126.3, 63073.9 };
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || f.Layer.Name != "0\\DESIGN DRAFT\\STRIPPING" || A(f, "CLAUDE_MPA07") != "1") continue;
    var m = f.BoundingBox.Midpoint; if (Math.Sqrt(Math.Pow(m.x - c[0], 2) + Math.Pow(m.y - c[1], 2)) > 6) continue;
    if (f.IsMText) Out.AppendLine($"label ({m.x:F2},{m.y:F2},{m.z:F2}): {f.asMText.TextString.Replace("\r\n", " | ")}");
    else if (f.IsPolyline) { var pl = f.asPolyline; Out.AppendLine($"strip Name={A(f, "Name")} n={pl.VertexCount} closed={pl.Closed}: " + string.Join(" ", Enumerable.Range(0, pl.VertexCount).Select(i => { var q = pl.GetVertex(i); return $"({q.x:F2},{q.y:F2},{q.z:F2})"; }))); }
}
return null;
