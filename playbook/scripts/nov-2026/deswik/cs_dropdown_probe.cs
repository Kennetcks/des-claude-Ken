// Read-only: the MPA 07 truck-loading drop-down box (4-point strip panel) edge lengths and fall, and the 4-point panels
// on the strip standards layers with the nearest label, for comparison.
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
string Edges(Deswik.Graphics.Primaries.Figure f)
{
    var p = Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => f.asPolyline.GetVertex(i)).ToList();
    return string.Join(" / ", Enumerable.Range(0, p.Count).Select(i => { var a = p[i]; var b = p[(i + 1) % p.Count]; return $"{Math.Sqrt(Math.Pow(b.x - a.x, 2) + Math.Pow(b.y - a.y, 2)):F2}m"; })) + $" | RL {p.Max(q => q.z):F2} to {p.Min(q => q.z):F2} (fall {p.Max(q => q.z) - p.Min(q => q.z):F2})";
}
var labs = new List<Deswik.Graphics.Primaries.Figure>(); var box = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null) continue;
    if (f.Layer.Name == "0\\DESIGN DRAFT\\STRIPPING" && f.IsPolyline && A(f, "CLAUDE_MPA07") == "1" && f.asPolyline.VertexCount == 4) Out.AppendLine("MPA 07 drop-down: " + Edges(f));
    if (f.Layer.Name.StartsWith("0\\REFERENCE\\STRIP STANDARDS") && !f.Layer.Name.EndsWith("SOLIDS")) { if (f.IsMText) labs.Add(f); else if (f.IsPolyline && f.asPolyline.VertexCount == 4) box.Add(f); }
}
foreach (var b in box)
{
    var m = b.BoundingBox.Midpoint; var l = labs.OrderBy(t => Math.Pow(t.BoundingBox.Midpoint.x - m.x, 2) + Math.Pow(t.BoundingBox.Midpoint.y - m.y, 2)).FirstOrDefault();
    Out.AppendLine($"standard '{l?.asMText.TextString.Replace("\r\n", " ").Trim()}': {Edges(b)}");
}
return null;
