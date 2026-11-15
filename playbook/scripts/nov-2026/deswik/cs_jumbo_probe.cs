// Read-only: the strip standard labels, the jumbo box standard(s) with their nearby polylines, and where the W6390 DSP
// sits on the MPA 07 decline (station, side), plus decline points 15-35 m below it.
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
List<double[]> P3(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
double D2(double[] a, double[] b) => Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2));
var labs = new List<Deswik.Graphics.Primaries.Figure>(); var std = new List<Deswik.Graphics.Primaries.Figure>();
Deswik.Graphics.Primaries.Figure dec = null, dsp = null;
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null) continue; var ln = f.Layer.Name;
    if (ln.StartsWith("0\\REFERENCE\\STRIP STANDARDS")) { if (f.IsMText) labs.Add(f); else if (f.IsPolyline && !ln.EndsWith("SOLIDS")) std.Add(f); }
    if (ln == "0\\DESIGN DRAFT\\CENTERLINES" && f.IsPolyline && A(f, "CLAUDE_MPA07") == "1") { if (A(f, "ACT_TYPE") == "DEC") dec = f; if (A(f, "Name") == "W6390 DSP") dsp = f; }
}
Out.AppendLine("labels:");
foreach (var t in labs) { var m = t.BoundingBox.Midpoint; Out.AppendLine($"  ({m.x:F1},{m.y:F1},{m.z:F1}) [{t.Layer.Name}] {t.asMText.TextString.Replace("\r\n", " | ").Replace("\n", " | ")}"); }
foreach (var t in labs.Where(t => t.asMText.TextString.ToUpper().Contains("JUMBO")))
{
    var m = t.BoundingBox.Midpoint; var c = new[] { m.x, m.y };
    Out.AppendLine($"=== near '{t.asMText.TextString.Replace("\r\n", " ").Replace("\n", " ")}'");
    foreach (var f in std.Where(f => P3(f).Min(q => D2(q, c)) < 16).OrderBy(f => P3(f).Min(q => D2(q, c))))
        Out.AppendLine($"  {f.Layer.Name.Replace("0\\REFERENCE\\STRIP STANDARDS", "STD")} n={f.asPolyline.VertexCount} closed={f.asPolyline.Closed}: " + string.Join(" ", P3(f).Select(q => $"({q[0]:F2},{q[1]:F2},{q[2]:F2})")));
}
var D = P3(dec); var C = new List<double> { 0 }; for (int i = 1; i < D.Count; i++) C.Add(C[i - 1] + D2(D[i], D[i - 1]));
Out.AppendLine($"decline {D.Count} pts, {C.Last():F1} m; PROFILE {A(dec, "PROFILE")}; first {D[0][0]:F2},{D[0][1]:F2},{D[0][2]:F2} last {D.Last()[0]:F2},{D.Last()[1]:F2},{D.Last()[2]:F2}");
var S = P3(dsp)[0];
for (int i = 1; i < D.Count; i++)
{
    double vx = D[i][0] - D[i - 1][0], vy = D[i][1] - D[i - 1][1], L = Math.Sqrt(vx * vx + vy * vy), t = ((S[0] - D[i - 1][0]) * vx + (S[1] - D[i - 1][1]) * vy) / (L * L);
    if (t >= 0 && t <= 1) { double off = ((S[0] - D[i - 1][0]) * vy - (S[1] - D[i - 1][1]) * vx) / L; Out.AppendLine($"DSP start at station {C[i - 1] + t * L:F2}, offset {off:F2} (+ = right looking down the decline, if stations increase downward)"); }
}
for (int i = 0; i < D.Count; i++) if (C[i] > 40 && C[i] < 90) Out.AppendLine($"  st {C[i]:F2}: ({D[i][0]:F2},{D[i][1]:F2},{D[i][2]:F2})");
// MPA 07 figures near 55-65 m below the DSP start
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || !f.IsPolyline || A(f, "CLAUDE_MPA07") != "1" || f == dec) continue;
    var p = P3(f); if (p.Any(q => D.Where((d, i) => C[i] > 50 && C[i] < 75).Any(d => D2(d, q) < 8)))
        Out.AppendLine($"  near: {f.Layer.Name} {A(f, "Name")} {A(f, "ACT_TYPE")} n={p.Count}");
}
return null;
