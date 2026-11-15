// Read-only: W6390 RAD centreline(s) - vertices, plan/3D length, segment grades, and the grade needed before a flat last 8 m.
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || f.Layer.Name != "0\\DESIGN DRAFT\\CENTERLINES" || !f.IsPolyline || !A(f, "Name").Contains("RAD")) continue;
    var pl = f.asPolyline; var p = Enumerable.Range(0, pl.VertexCount).Select(i => { var q = pl.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
    Out.AppendLine($"=== {A(f, "Name")} MPA07={A(f, "CLAUDE_MPA07")} ACT={A(f, "ACT_TYPE")} PROF={A(f, "PROFILE")} LEN={A(f, "LEN")} GRADE={A(f, "GRADE")} n={p.Count}");
    double plan = 0, d3 = 0;
    for (int i = 0; i < p.Count; i++)
    {
        double h = i == 0 ? 0 : Math.Sqrt(Math.Pow(p[i][0] - p[i - 1][0], 2) + Math.Pow(p[i][1] - p[i - 1][1], 2)); plan += h; if (i > 0) d3 += Math.Sqrt(h * h + Math.Pow(p[i][2] - p[i - 1][2], 2));
        Out.AppendLine($"  ({p[i][0]:F3},{p[i][1]:F3},{p[i][2]:F3}) plan {plan:F2}" + (i > 0 && h > 0 ? $" grade 1:{h / (p[i][2] - p[i - 1][2]):F1}" : ""));
    }
    double dz = p.Last()[2] - p[0][2];
    Out.AppendLine($"  plan {plan:F2} m, 3D {d3:F2} m, rise {dz:F3} m, overall 1:{plan / dz:F1}");
    if (plan > 8) Out.AppendLine($"  last 8 m flat: first {plan - 8:F2} m at 1:{(plan - 8) / dz:F1} ({dz / (plan - 8) * 100:F2} %)");
}
return null;
