// Read-only: every figure on 0\DESIGN DRAFT\STRIPPING (and sublayers) within 25 m of the old EWD / new DSP mouths, with
// attributes, text and vertices; plus the strip standards layers' names and labels (to see which standard fits a DSP).
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
double ox = 8107.879, oy = 63097.585, nx = 8110.890, ny = 63093.593;
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || !f.Layer.Name.StartsWith("0\\DESIGN DRAFT\\STRIPPING")) continue;
    var m = f.BoundingBox.Midpoint; double d = Math.Min(Math.Sqrt(Math.Pow(m.x - ox, 2) + Math.Pow(m.y - oy, 2)), Math.Sqrt(Math.Pow(m.x - nx, 2) + Math.Pow(m.y - ny, 2)));
    if (d > 25) continue;
    var h = f.GetType().GetProperty("Handle")?.GetValue(f);
    string txt = f.IsText ? f.asText.TextString : f.IsMText ? f.asMText.TextString : "";
    Out.AppendLine($"h={h} {f.Layer.Name} | {f.GetType().Name} pl={f.IsPolyline} mt={f.IsMText} | d={d:F1} | Name={A(f, "Name")} ACT={A(f, "ACT_TYPE")} tag={A(f, "CLAUDE_MPA07")} STD={A(f, "CLAUDE_STD")} | '{txt.Replace("\r\n", " / ")}' | bb ({f.BoundingBox.Min.x:F2},{f.BoundingBox.Min.y:F2},{f.BoundingBox.Min.z:F2})-({f.BoundingBox.Max.x:F2},{f.BoundingBox.Max.y:F2},{f.BoundingBox.Max.z:F2})");
    if (f.IsPolyline) Out.AppendLine("    " + string.Join(" ", Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return $"({q.x:F2},{q.y:F2},{q.z:F2})"; })));
}
return null;
