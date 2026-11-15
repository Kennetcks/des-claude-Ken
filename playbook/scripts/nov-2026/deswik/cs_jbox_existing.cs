// Read-only: existing Design Master J/Box strips - the strip string next to each label, its attributes, and where it sits
// against the nearest centreline (offset, height above it) and the nearest wall string.
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
List<double[]> P3(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
double D2(double[] a, double[] b) => Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2));
var all = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (!f.Deleted && f.Layer != null) all.Add(f);
(double d, double off, double z, Deswik.Graphics.Primaries.Figure f) Near(double[] q, string layer)
{
    var best = (1e9, 0.0, 0.0, (Deswik.Graphics.Primaries.Figure)null);
    foreach (var f in all.Where(f => f.IsPolyline && f.Layer.Name == layer))
    {
        var p = P3(f); for (int i = 1; i < p.Count; i++)
        {
            double vx = p[i][0] - p[i - 1][0], vy = p[i][1] - p[i - 1][1], L2 = vx * vx + vy * vy; if (L2 < 1e-9) continue;
            double t = Math.Max(0, Math.Min(1, ((q[0] - p[i - 1][0]) * vx + (q[1] - p[i - 1][1]) * vy) / L2)), px = p[i - 1][0] + t * vx, py = p[i - 1][1] + t * vy, d = Math.Sqrt(Math.Pow(q[0] - px, 2) + Math.Pow(q[1] - py, 2));
            if (d < best.Item1 && Math.Abs(q[2] - (p[i - 1][2] + t * (p[i][2] - p[i - 1][2]))) < 6) best = (d, ((q[0] - p[i - 1][0]) * vy - (q[1] - p[i - 1][1]) * vx) / Math.Sqrt(L2), p[i - 1][2] + t * (p[i][2] - p[i - 1][2]), f);
        }
    }
    return best;
}
foreach (var t in all.Where(f => f.IsMText && f.Layer.Name == "0\\DESIGN MASTER\\STRIPPING" && f.asMText.TextString.StartsWith("J/Box Strip")))
{
    var m = t.BoundingBox.Midpoint; var c = new[] { m.x, m.y, m.z };
    Out.AppendLine($"=== '{t.asMText.TextString.Replace("\r\n", " | ")}' at ({m.x:F2},{m.y:F2},{m.z:F2})");
    foreach (var f in all.Where(f => f.IsPolyline && f.Layer.Name == "0\\DESIGN MASTER\\STRIPPING" && P3(f).Min(q => Math.Sqrt(Math.Pow(D2(q, c), 2) + Math.Pow(q[2] - c[2], 2))) < 7))
    {
        var p = P3(f);
        Out.AppendLine($"  strip n={p.Count} closed={f.asPolyline.Closed} Name={A(f, "Name")} ACT={A(f, "ACT_TYPE")} PROF={A(f, "PROFILE")} Desc={A(f, "Description")} GSS={A(f, "GSS")}");
        foreach (var q in p)
        {
            var cl = Near(q, "0\\DESIGN MASTER\\CENTERLINES"); var w = Near(q, "0\\DESIGN MASTER\\WALLS");
            Out.AppendLine($"    ({q[0]:F2},{q[1]:F2},{q[2]:F2})  CL {(cl.f != null ? A(cl.f, "Name") : "-")} off {cl.off:F2} dz {q[2] - cl.z:F2} | wall {w.d:F2} m");
        }
    }
    var lead = all.Where(f => f.IsLeader && f.Layer.Name == "0\\DESIGN MASTER\\STRIPPING").OrderBy(f => D2(new[] { f.BoundingBox.Midpoint.x, f.BoundingBox.Midpoint.y }, c)).FirstOrDefault();
    if (lead != null) Out.AppendLine($"  nearest leader {D2(new[] { lead.BoundingBox.Midpoint.x, lead.BoundingBox.Midpoint.y }, c):F1} m");
}
return null;
