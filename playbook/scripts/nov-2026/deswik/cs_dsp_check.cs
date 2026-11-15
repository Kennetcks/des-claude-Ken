// Read-only: after the EWD -> DSP move. Lists the W6390 DSP figures, checks the drive wall ends meet the decline wall
// strings, the drive centreline length/grade, any figure still named W6390 EWD, and the mouth width.
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
List<double[]> P3(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
double D3(double[] a, double[] b) => Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2) + Math.Pow(a[2] - b[2], 2));
var dw = new List<List<double[]>>(); List<double[]> w = null, c = null; int ewd = 0;
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || A(f, "CLAUDE_MPA07") != "1") continue; var n = A(f, "Name"); var ln = f.Layer.Name;
    if (n == "W6390 EWD") { ewd++; Out.AppendLine($"still EWD: {ln}"); }
    if (n == "W6390 DSP") Out.AppendLine($"DSP: {ln} | ACT={A(f, "ACT_TYPE")} PROF={A(f, "PROFILE")} Desc={A(f, "Description")} GSS={A(f, "GSS")}" + (f.IsText ? $" | '{f.asText.TextString}' at ({f.asText.InsertionPoint.x:F2},{f.asText.InsertionPoint.y:F2},{f.asText.InsertionPoint.z:F2})" : ""));
    if (ln == "0\\DESIGN DRAFT\\WALLS" && f.IsPolyline) { if (n == "W6390 DSP") w = P3(f); else if (n == "MAIN DEC") dw.Add(P3(f)); }
    if (ln == "0\\DESIGN DRAFT\\CENTERLINES" && f.IsPolyline && n == "W6390 DSP") c = P3(f);
}
Out.AppendLine($"centreline: {D3(c[0], c[1]):F3} m 3D, grade 1:{Math.Sqrt(Math.Pow(c[1][0] - c[0][0], 2) + Math.Pow(c[1][1] - c[0][1], 2)) / (c[1][2] - c[0][2]):F1}");
foreach (var e in new[] { w[0], w[^1] }) Out.AppendLine($"drive wall end ({e[0]:F3},{e[1]:F3},{e[2]:F3}) -> nearest decline wall string end {dw.SelectMany(p => new[] { p[0], p[^1] }).Min(q => D3(q, e)) * 1000:F1} mm");
Out.AppendLine($"drive wall ends {D3(w[0], w[^1]):F2} m apart (5.5 m mouth + 2 x 3 m fillets = 11.5 m); decline wall strings {dw.Count}: " + string.Join(", ", dw.Select(p => p.Count)));
return null;
