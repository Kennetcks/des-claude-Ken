// Read-only: what the RAD wall's left fillet end (8080.717,63114.799) touches - any polyline (not SOLIDS) passing within
// 1 m of it, with the distance and its RL there.
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
var q = new[] { 8080.717, 63114.799, 6396.581 };
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || !f.IsPolyline || f.Layer.Name.EndsWith("SOLIDS")) continue;
    var b = f.BoundingBox; if (q[0] < b.Min.x - 1 || q[0] > b.Max.x + 1 || q[1] < b.Min.y - 1 || q[1] > b.Max.y + 1 || q[2] < b.Min.z - 6 || q[2] > b.Max.z + 6) continue;
    var pl = f.asPolyline; double bd = 1e9, bz = 0; int bi = -1;
    for (int i = 1; i < pl.VertexCount + (pl.Closed ? 1 : 0); i++)
    {
        var a = pl.GetVertex(i - 1); var c = pl.GetVertex(i % pl.VertexCount); double vx = c.x - a.x, vy = c.y - a.y, L2 = vx * vx + vy * vy; if (L2 < 1e-12) continue;
        double t = Math.Max(0, Math.Min(1, ((q[0] - a.x) * vx + (q[1] - a.y) * vy) / L2)), d = Math.Sqrt(Math.Pow(q[0] - a.x - t * vx, 2) + Math.Pow(q[1] - a.y - t * vy, 2));
        if (d < bd) { bd = d; bz = a.z + t * (c.z - a.z); bi = i; }
    }
    if (bd < 1) Out.AppendLine($"{f.Handle} {f.Layer.Name} '{A(f, "Name")}' ACT={A(f, "ACT_TYPE")} MPA07={A(f, "CLAUDE_MPA07")} n={pl.VertexCount}: {bd:F3} m away at seg {bi}, RL {bz:F3}");
}
return null;
