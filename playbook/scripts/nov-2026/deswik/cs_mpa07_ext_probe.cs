// Read-only: MPA 07 decline centreline end, its walls, and the solids on 0\SKETCH LAYER\MPA 07 relative to the decline end.
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
List<double[]> P3(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
Deswik.Graphics.Primaries.Figure dec = null; var walls = new List<Deswik.Graphics.Primaries.Figure>(); var sol = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null) continue; var ln = f.Layer.Name;
    if (ln == "0\\DESIGN DRAFT\\CENTERLINES" && f.IsPolyline && A(f, "CLAUDE_MPA07") == "1" && A(f, "ACT_TYPE") == "DEC") dec = f;
    else if (ln == "0\\DESIGN DRAFT\\WALLS" && f.IsPolyline && A(f, "CLAUDE_MPA07") == "1") walls.Add(f);
    else if (ln == "0\\SKETCH LAYER\\MPA 07" && f.IsPolyface) sol.Add(f);
}
var d = P3(dec); var e = d[^1]; var p = d[^2];
double tx = e[0] - p[0], ty = e[1] - p[1], tl = Math.Sqrt(tx * tx + ty * ty); tx /= tl; ty /= tl;
double len = 0; for (int i = 1; i < d.Count; i++) len += Math.Sqrt(Math.Pow(d[i][0] - d[i - 1][0], 2) + Math.Pow(d[i][1] - d[i - 1][1], 2));
Out.AppendLine($"decline {A(dec, "Name")} {A(dec, "PROFILE")}: {d.Count} pts, {len:F1} m, start ({d[0][0]:F2},{d[0][1]:F2},{d[0][2]:F2}) end ({e[0]:F2},{e[1]:F2},{e[2]:F3}), last-segment az {Math.Atan2(tx, ty) * 180 / Math.PI:F1}, grade over the string {(d[0][2] - e[2]) / len:F4}");
for (int i = Math.Max(0, d.Count - 6); i < d.Count; i++) Out.AppendLine($"  pt{i} ({d[i][0]:F2},{d[i][1]:F2},{d[i][2]:F3})");
foreach (var w in walls) { var q = P3(w); Out.AppendLine($"wall {A(w, "Name")} n={q.Count} first ({q[0][0]:F2},{q[0][1]:F2}) last ({q[^1][0]:F2},{q[^1][1]:F2})"); }
foreach (var s in sol.OrderBy(s => s.BoundingBox.Midpoint.x * tx + s.BoundingBox.Midpoint.y * ty))
{
    var b = s.BoundingBox; var m = b.Midpoint; double along = (m.x - e[0]) * tx + (m.y - e[1]) * ty, across = -(m.x - e[0]) * ty + (m.y - e[1]) * tx;
    // furthest vertex along the decline direction
    double far = double.MinValue; foreach (var o in (System.Collections.IEnumerable)s.asPolyface.VertexList) { var t = o.GetType(); double x = Convert.ToDouble(t.GetProperty("x").GetValue(o)), y = Convert.ToDouble(t.GetProperty("y").GetValue(o)); far = Math.Max(far, (x - e[0]) * tx + (y - e[1]) * ty); }
    Out.AppendLine($"solid {s.GetType().GetProperty("Handle")?.GetValue(s)}: mid along {along:F1} across {across:F1} z {b.Min.z:F2}-{b.Max.z:F2} far {far:F1} vol {s.asPolyface.Volume:F1}");
}
return null;
