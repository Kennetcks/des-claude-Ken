// Read-only: every non-polyline figure (text, leader, point, block) within 15 m of the W6390 RAD on DESIGN DRAFT layers,
// plus how the DSP / other drives show their grade (text containing "1:").
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
var rad = new[] { 8087.8, 63123.1 };
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || !f.Layer.Name.StartsWith("0\\DESIGN DRAFT") || f.IsPolyline) continue;
    var m = f.BoundingBox.Midpoint; double d = Math.Sqrt(Math.Pow(m.x - rad[0], 2) + Math.Pow(m.y - rad[1], 2));
    bool grade = f.IsMText && f.asMText.TextString.Contains("1:");
    if (d > 15 && !(grade && d < 60)) continue;
    string kind = f.IsPoint ? "point" : f.IsMText ? "mtext" : f.IsLeader ? "leader" : f.GetType().Name;
    string extra = f.IsMText ? " '" + f.asMText.TextString.Replace("\r\n", " | ") + "'" : "";
    Out.AppendLine($"{f.Layer.Name} {kind} h={f.Handle} ({m.x:F2},{m.y:F2},{m.z:F2}) d={d:F1} Name={A(f, "Name")}{extra}");
}
return null;
