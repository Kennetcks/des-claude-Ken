// Read-only: the selected strip's shape, the centrelines near it (with profile and distance), and labels within 25 m.
const string CL = "0\\DESIGN MASTER\\CENTRELINES";
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
var sel = App.Selections.MainSelection.Cast<Deswik.Graphics.Primaries.Figure>().First();
var op = Enumerable.Range(0, sel.asPolyline.VertexCount).Select(i => sel.asPolyline.GetVertex(i)).ToList();
double cx = op.Average(q => q.x), cy = op.Average(q => q.y);
Out.AppendLine("Selected: " + string.Join(" | ", op.Select(q => $"{q.x:F2},{q.y:F2},{q.z:F2}")));
for (int i = 1; i < op.Count; i++) Out.AppendLine($"  edge {i}: {Math.Sqrt(Math.Pow(op[i].x - op[i - 1].x, 2) + Math.Pow(op[i].y - op[i - 1].y, 2)):F2} m");
Out.AppendLine("  attrs: " + string.Join(", ", new[] { "Name", "ACT_TYPE", "LEVEL", "PROFILE", "MINE" }.Select(k => k + "=" + A(sel, k))));
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null) continue;
    if (f.Layer.Name == CL && f.IsPolyline && f.asPolyline.VertexCount >= 2)
    {
        var D = Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => f.asPolyline.GetVertex(i)).ToList(); double bd = 1e9;
        for (int i = 1; i < D.Count; i++) { double ax = D[i - 1].x, ay = D[i - 1].y, vx = D[i].x - ax, vy = D[i].y - ay, L2 = vx * vx + vy * vy; double t = L2 > 0 ? Math.Max(0, Math.Min(1, ((cx - ax) * vx + (cy - ay) * vy) / L2)) : 0; bd = Math.Min(bd, Math.Sqrt(Math.Pow(ax + t * vx - cx, 2) + Math.Pow(ay + t * vy - cy, 2))); }
        if (bd < 20) Out.AppendLine($"CL {bd:F1} m: Name='{A(f, "Name")}' ACT={A(f, "ACT_TYPE")} PROFILE='{A(f, "PROFILE")}' n={D.Count} start=({D[0].x:F1},{D[0].y:F1},{D[0].z:F1}) end=({D[^1].x:F1},{D[^1].y:F1},{D[^1].z:F1})");
    }
    else if (f.IsMText)
    {
        var m = f.BoundingBox.Midpoint; double d = Math.Sqrt(Math.Pow(m.x - cx, 2) + Math.Pow(m.y - cy, 2));
        if (d < 25) Out.AppendLine($"TXT {d:F1} m layer={f.Layer.Name} z={m.z:F1}: '{f.asMText.TextString.Replace("\r\n", " / ").Replace("\n", " / ")}'");
    }
}
return null;
