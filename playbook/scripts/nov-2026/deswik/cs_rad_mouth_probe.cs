// Read-only: wall strings within 12 m of the MPA 07 joint (existing MAIN DEC end), with every vertex near the joint.
double jx = 8082.206, jy = 63112.193, r = 12;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || !f.IsPolyline || !(f.Layer.Name.EndsWith("\\WALLS"))) continue;
    var pl = f.asPolyline; var P = Enumerable.Range(0, pl.VertexCount).Select(i => pl.GetVertex(i)).ToList();
    if (!P.Any(p => Math.Sqrt(Math.Pow(p.x - jx, 2) + Math.Pow(p.y - jy, 2)) < r)) continue;
    Out.AppendLine($"{f.Layer.Name} h={f.GetType().GetProperty("Handle")?.GetValue(f)} {A(f, "Name")} n={P.Count} closed={pl.Closed} tag={A(f, "CLAUDE_MPA07")}");
    for (int i = 0; i < P.Count; i++) { double d = Math.Sqrt(Math.Pow(P[i].x - jx, 2) + Math.Pow(P[i].y - jy, 2)); if (d < r || i == 0 || i == P.Count - 1) Out.AppendLine($"   {i}: ({P[i].x:F3},{P[i].y:F3},{P[i].z:F3}) d={d:F2}"); }
}
return null;
