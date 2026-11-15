// Read-only: every W6390 RAD figure on CENTERLINES sublayers (GRADE etc.) and every W6390 RAD centreline anywhere,
// with handle, text and position - to find where the duplicate grade label comes from.
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || A(f, "Name") != "W6390 RAD") continue;
    var m = f.BoundingBox.Midpoint; string kind = f.IsText ? "text '" + f.asText.TextString + "'" : f.IsMText ? "mtext '" + f.asMText.TextString + "'" : f.IsLeader ? "leader" : f.IsPolyline ? "polyline n=" + f.asPolyline.VertexCount : "other";
    if (f.IsPolyline && f.Layer.Name.EndsWith("SOLIDS")) continue;
    string attrs = f.IsPolyline ? $" MPA07={A(f, "CLAUDE_MPA07")} ACT={A(f, "ACT_TYPE")}" : "";
    Out.AppendLine($"{f.Handle} {f.Layer.Name} {kind} ({m.x:F2},{m.y:F2},{m.z:F2}){attrs}");
    if (f.IsPolyline && f.Layer.Name.EndsWith("CENTERLINES")) { var pl = f.asPolyline; Out.AppendLine("    " + string.Join(" ", Enumerable.Range(0, pl.VertexCount).Select(i => { var q = pl.GetVertex(i); return $"({q.x:F2},{q.y:F2},{q.z:F3})"; }))); }
}
return null;
