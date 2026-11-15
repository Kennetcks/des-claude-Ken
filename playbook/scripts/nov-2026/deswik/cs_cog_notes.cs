// Read-only: NOTES polylines within 4 m of the RAD's old grade breaks, and the two COG texts' position/rotation.
var breaks = new[] { new[] { 8087.409, 63121.211 }, new[] { 8088.566, 63127.607 } };
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || !f.Layer.Name.StartsWith("0\\DESIGN DRAFT\\NOTES")) continue;
    if (f.IsPolyline)
    {
        var pl = f.asPolyline; var p = Enumerable.Range(0, pl.VertexCount).Select(i => pl.GetVertex(i)).ToList();
        if (p.Any(q => breaks.Any(b => Math.Sqrt(Math.Pow(q.x - b[0], 2) + Math.Pow(q.y - b[1], 2)) < 4)) && p.Count < 20)
            Out.AppendLine($"{f.Handle} {f.Layer.Name} polyline n={p.Count}: " + string.Join(" ", p.Select(q => $"({q.x:F2},{q.y:F2},{q.z:F2})")));
    }
    else if (f.IsText && f.asText.TextString == "COG")
    {
        var t = f.asText; var props = t.GetType().GetProperties().Where(q => new[] { "Rotation", "Height", "Position", "InsertionPoint", "Alignment", "Justification" }.Contains(q.Name)).Select(q => { try { return q.Name + "=" + q.GetValue(t); } catch { return q.Name + "=?"; } });
        Out.AppendLine($"{f.Handle} COG text bbox ({f.BoundingBox.Min.x:F2},{f.BoundingBox.Min.y:F2})-({f.BoundingBox.Max.x:F2},{f.BoundingBox.Max.y:F2}) z {f.BoundingBox.Midpoint.z:F2} {string.Join(" ", props)}");
    }
}
return null;
