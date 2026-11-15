// Level Design Drafter, read-only: list everything on the strip standards layers - labels and closed panels
// (plan area, z range, vertices) - so the standards can be checked against Table 6 of the design standard.
const string root = "0\\REFERENCE\\STRIP STANDARDS";
var byLayer = new SortedDictionary<string, List<string>>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || !f.Layer.Name.StartsWith(root)) continue;
    if (!byLayer.ContainsKey(f.Layer.Name)) byLayer[f.Layer.Name] = new List<string>();
    var b = f.BoundingBox; string s;
    if (f.IsMText) s = $"TEXT @({b.Midpoint.x:F2},{b.Midpoint.y:F2},{b.Midpoint.z:F2}) \"{f.asMText.TextString.Replace("\r\n", " / ").Replace("\n", " / ")}\"";
    else if (f.IsPolyline)
    {
        var pl = f.asPolyline; var pts = Enumerable.Range(0, pl.VertexCount).Select(i => pl.GetVertex(i)).ToList();
        double a = 0; for (int i = 0; i < pts.Count; i++) { var p = pts[i]; var q = pts[(i + 1) % pts.Count]; a += p.x * q.y - q.x * p.y; }
        var hc = pl.HatchProperties == null ? "-" : pl.HatchProperties.FillBackColor.ColorIndex.ToString();
        s = $"PL n={pl.VertexCount} closed={pl.Closed} hatch={hc} area={Math.Abs(a) / 2:F2} z {b.Min.z:F2}-{b.Max.z:F2} : " + string.Join(" ", pts.Select(p => $"({p.x:F2},{p.y:F2},{p.z:F2})"));
    }
    else s = f.GetType().Name + $" @({b.Midpoint.x:F2},{b.Midpoint.y:F2},{b.Midpoint.z:F2})";
    byLayer[f.Layer.Name].Add(s);
}
foreach (var kv in byLayer) { Out.AppendLine($"== {kv.Key} ({kv.Value.Count})"); foreach (var s in kv.Value) Out.AppendLine(s); }
return null;
