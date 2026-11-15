// Read-only: every polyline whose Name starts with W6390, by layer, with vertex count, attributes and plan extent.
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || !f.IsPolyline || !A(f, "Name").StartsWith("W6390") || f.Layer.Name.EndsWith("SOLIDS")) continue;
    var b = f.BoundingBox;
    Out.AppendLine($"{f.Handle} {f.Layer.Name} '{A(f, "Name")}' ACT={A(f, "ACT_TYPE")} PROF={A(f, "PROFILE")} MPA07={A(f, "CLAUDE_MPA07")} n={f.asPolyline.VertexCount} closed={f.asPolyline.Closed} ({b.Min.x:F1},{b.Min.y:F1},{b.Min.z:F1})-({b.Max.x:F1},{b.Max.y:F1},{b.Max.z:F1})");
}
return null;
