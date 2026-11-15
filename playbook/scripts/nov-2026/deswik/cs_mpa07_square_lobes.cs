// Level Design Drafter: (1) remove the angled-leg example added by mistake to the strip standards (everything in that
// grid cell, which was empty before); (2) square the lobe and front ramp of the two MPA 07 truck strips to their leg
// (W6390 RAD / W6390 EWD): centred on the leg centreline, (leg width - 0.5) across it, front ramp straight along it.
const bool APPLY = false;
const string trk = "0\\REFERENCE\\STRIP STANDARDS\\TRUCK LOADING TRANSITIONS";
double[] P1 = { 8041.17, 62914.04 }, u = { -0.4679, 0.8838 }, v = { 0.8850, 0.4656 };
(double a, double b) AB(double x, double y) => ((x - P1[0]) * u[0] + (y - P1[1]) * u[1], (x - P1[0]) * v[0] + (y - P1[1]) * v[1]);
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
var inv = System.Globalization.CultureInfo.InvariantCulture;
List<double[]> P3(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
double Area(List<double[]> p) { double a = 0; for (int i = 0; i < p.Count; i++) { var s = p[i]; var t = p[(i + 1) % p.Count]; a += s[0] * t[1] - t[0] * s[1]; } return Math.Abs(a) / 2; }
var example = new List<Deswik.Graphics.Primaries.Figure>(); var legs = new Dictionary<string, Deswik.Graphics.Primaries.Figure>(); var strip = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null) continue;
    if (f.Layer.Name == trk) { var ab = AB(f.BoundingBox.Midpoint.x, f.BoundingBox.Midpoint.y); if (ab.a > 18 && ab.a < 42 && ab.b > 84 && ab.b < 102) example.Add(f); }
    else if (A(f, "CLAUDE_MPA07") == "1" && f.IsPolyline && f.Layer.Name == "0\\DESIGN DRAFT\\CENTERLINES" && A(f, "ACT_TYPE") != "DEC") legs[A(f, "Name")] = f;
    else if (A(f, "CLAUDE_MPA07") == "1" && f.IsPolyline && f.Layer.Name == "0\\DESIGN DRAFT\\STRIPPING") strip.Add(f);
}
Out.AppendLine($"example objects to remove: {example.Count} ({string.Join(", ", example.Select(f => f.IsMText ? "label" : "panel n=" + f.asPolyline.VertexCount))})");
var work = new List<(Deswik.Graphics.Primaries.Figure f, List<double[]> p)>();
var reds = strip.Where(f => f.asPolyline.HatchProperties?.FillBackColor.ColorIndex != 253).ToList(); var fronts = strip.Where(f => f.asPolyline.VertexCount == 4 && f.asPolyline.HatchProperties?.FillBackColor.ColorIndex == 253).ToList();
foreach (var red in reds)
{
    var rp = P3(red); int n = rp.Count; var lo1 = rp[n - 3]; var lo0 = rp[n - 2]; double mx = (lo0[0] + lo1[0]) / 2, my = (lo0[1] + lo1[1]) / 2;
    var front = fronts.OrderBy(f => { var p = P3(f); return Math.Pow((p[0][0] + p[1][0]) / 2 - mx, 2) + Math.Pow((p[0][1] + p[1][1]) / 2 - my, 2); }).First();
    var leg = legs.Values.OrderBy(f => { var p = P3(f); return Math.Pow(p[0][0] - mx, 2) + Math.Pow(p[0][1] - my, 2); }).First();
    var sc = P3(leg); var m = System.Text.RegularExpressions.Regex.Match(A(leg, "PROFILE"), @"([\d.]+)\s*mW"); double Ws = m.Success ? double.Parse(m.Groups[1].Value, inv) : 5.0, hwS = (Ws - 0.5) / 2;
    double dx = sc[1][0] - sc[0][0], dy = sc[1][1] - sc[0][1], dl = Math.Sqrt(dx * dx + dy * dy); dx /= dl; dy /= dl; double nx = -dy, ny = dx;
    var fp = P3(front); double ramp = Math.Sqrt(Math.Pow(fp[2][0] - fp[1][0], 2) + Math.Pow(fp[2][1] - fp[1][1], 2)), drop = fp[1][2] - fp[2][2];
    double t = (mx - sc[0][0]) * dx + (my - sc[0][1]) * dy; double cx = sc[0][0] + dx * t, cy = sc[0][1] + dy * t, z = (lo0[2] + lo1[2]) / 2;
    var a = new[] { cx + nx * hwS, cy + ny * hwS, z }; var b2 = new[] { cx - nx * hwS, cy - ny * hwS, z };
    double[] n1, n0; if (Math.Pow(a[0] - lo1[0], 2) + Math.Pow(a[1] - lo1[1], 2) <= Math.Pow(b2[0] - lo1[0], 2) + Math.Pow(b2[1] - lo1[1], 2)) { n1 = a; n0 = b2; } else { n1 = b2; n0 = a; }
    var rNew = rp.Select(q => new[] { q[0], q[1], q[2] }).ToList(); rNew[n - 3] = n1; rNew[n - 2] = n0;
    var fNew = new List<double[]> { n0, n1, new[] { n1[0] + dx * ramp, n1[1] + dy * ramp, z - drop }, new[] { n0[0] + dx * ramp, n0[1] + dy * ramp, z - drop } };
    work.Add((red, rNew)); work.Add((front, fNew));
    Out.AppendLine($"{A(leg, "Name")} truck strip: lobe {Ws - 0.5:F1} m across the leg (was {Math.Sqrt(Math.Pow(lo1[0] - lo0[0], 2) + Math.Pow(lo1[1] - lo0[1], 2)):F2} m along the decline), front ramp {Area(fp):F1} -> {Area(fNew):F1} m2");
}
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }
foreach (var f in example) f.Deleted = true;
foreach (var (f, p) in work)
{
    var c = (Deswik.Graphics.Primaries.Figure)f.Clone(); var pl = c.asPolyline; while (pl.VertexCount > 0) pl.RemoveVertexAt(0);
    foreach (var q in p) pl.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]); pl.Closed = true; App.ModelSpace.Entities.AddItem(c, null, null, true, false); f.Deleted = true;
}
App.Redraw(true); Out.AppendLine("applied"); return null;
