// Read-only (Ventilation Plan Master): stations on 0\PRIMARY STATIONS (id, vertices, closed) and the LABELS layer's attribute fields.
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || f.Layer.Name != "0\\PRIMARY STATIONS") continue;
    Out.AppendLine($"{f.Handle} poly={f.IsPolyline} n={(f.IsPolyline ? f.asPolyline.VertexCount : 0)} closed={(f.IsPolyline && f.asPolyline.Closed)} id='{f.AttributeValueGet("1. STATION ID", "")}' area='{f.AttributeValueGet("4. AREA", "")}' q='{f.AttributeValueGet("8. Q (m3)", "")}'");
}
var lay = App.Layers.FirstOrDefault(l => l.Name == "0\\PRIMARY STATIONS\\LABELS");
Out.AppendLine("LABELS attrs: " + (lay == null ? "no layer" : lay.Attributes.Count + " - " + string.Join(", ", lay.Attributes.Cast<Deswik.Common.Core.Objects.Attribute>().Select(a => a.Name))));
return null;
