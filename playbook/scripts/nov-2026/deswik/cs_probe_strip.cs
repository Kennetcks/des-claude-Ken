var lay = App.Layers.FirstOrDefault(l => l.Name == "0\\REFERENCE\\STRIP STANDARDS\\FAN STRIP TRANSITIONS");
Out.AppendLine(lay == null ? "layer missing" : $"layer ok, visible={lay.Visible}, locked={lay.Locked}");
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Layer == null || f.Layer.Name != "0\\REFERENCE\\STRIP STANDARDS\\FAN STRIP TRANSITIONS") continue;
    if (f.IsPolyline)
    {
        var p = f.asPolyline;
        Out.AppendLine($"panel closed={p.Closed} pen={f.PenColor} STO_DEV={f.AttributeValueGet("STO_DEV", "")} : " + string.Join(" | ", Enumerable.Range(0, p.VertexCount).Select(i => { var q = p.GetVertex(i); return $"{q.x:F1},{q.y:F1},{q.z:F2}"; })));
    }
    else if (f.IsMText) Out.AppendLine("label: " + f.asMText.TextString.Replace("\r\n", " / "));
}
return null;
