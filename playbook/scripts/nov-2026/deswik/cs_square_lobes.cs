// Design Master: square the truck strip lobe and front ramp to the stockpile drive they open into. The lobe edge and the
// front ramp are centred on the stockpile centreline, (stockpile width - 0.5) wide measured across that drive, and run
// straight along it - so an oblique take-off no longer turns them into a skewed parallelogram that crosses its wall.
// The high section's far edge and the end ramps are unchanged. Hatched panels are rebuilt (clone + replace).
const bool APPLY = false;
var targets = new[] { ("G6390 MLA", "G6390 ISP"), ("MAIN DEC", "G6390 SUB") };
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
var inv = System.Globalization.CultureInfo.InvariantCulture;
List<double[]> P3(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
double Area(List<double[]> p) { double a = 0; for (int i = 0; i < p.Count; i++) { var u = p[i]; var v = p[(i + 1) % p.Count]; a += u[0] * v[1] - v[0] * u[1]; } return Math.Abs(a) / 2; }
var cls = new Dictionary<string, Deswik.Graphics.Primaries.Figure>(); var panels = new List<Deswik.Graphics.Primaries.Figure>(); var seen = new HashSet<string>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || !f.IsPolyline) continue; var b = f.BoundingBox;
    if (f.Layer.Name == "0\\DESIGN MASTER\\CENTRELINES" && targets.Any(t => t.Item2 == A(f, "Name"))) cls[A(f, "Name")] = f;
    else if (f.Layer.Name == "0\\DESIGN MASTER\\STRIPPING" && A(f, "CLAUDE_DECLINE_STRIP") == "1" && b.Min.x > 8050 && b.Max.x < 8080 && b.Min.y > 63015 && b.Max.y < 63110 && seen.Add($"{b.Min.x:F3},{b.Min.y:F3},{b.Min.z:F3}")) panels.Add(f);
}
var work = new List<(Deswik.Graphics.Primaries.Figure f, List<double[]> p)>();
foreach (var (strip, stk) in targets)
{
    var mine = panels.Where(f => A(f, "Name") == strip).ToList();
    var red = mine.FirstOrDefault(f => f.asPolyline.HatchProperties?.FillBackColor.ColorIndex != 253);
    var front = mine.FirstOrDefault(f => f.asPolyline.VertexCount == 4);
    if (red == null || front == null || !cls.ContainsKey(stk)) { Out.AppendLine($"{strip}: strip or {stk} not found - skipped"); continue; }
    var sc = P3(cls[stk]); var m = System.Text.RegularExpressions.Regex.Match(A(cls[stk], "PROFILE"), @"([\d.]+)\s*mW"); double Ws = m.Success ? double.Parse(m.Groups[1].Value, inv) : 5.5, hwS = (Ws - 0.5) / 2;
    double dx = sc[1][0] - sc[0][0], dy = sc[1][1] - sc[0][1], dl = Math.Sqrt(dx * dx + dy * dy); dx /= dl; dy /= dl; double nx = -dy, ny = dx;
    var rp = P3(red); int n = rp.Count; var lo1 = rp[n - 3]; var lo0 = rp[n - 2];          // lobe corners (o1, o0)
    var fp = P3(front);                                                                       // w0, w1, w1 + D*ramp, w0 + D*ramp
    double ramp = Math.Sqrt(Math.Pow(fp[2][0] - fp[1][0], 2) + Math.Pow(fp[2][1] - fp[1][1], 2)), drop = fp[1][2] - fp[2][2];
    // lobe line: same depth along the stockpile as now (through the middle of the current lobe edge), across that drive
    double mx = (lo0[0] + lo1[0]) / 2, my = (lo0[1] + lo1[1]) / 2; double t = (mx - sc[0][0]) * dx + (my - sc[0][1]) * dy;
    double cx = sc[0][0] + dx * t, cy = sc[0][1] + dy * t, z = (lo0[2] + lo1[2]) / 2;
    var a = new[] { cx + nx * hwS, cy + ny * hwS, z }; var b2 = new[] { cx - nx * hwS, cy - ny * hwS, z };
    // keep o1 / o0 on the same sides as before
    double[] n1, n0; if (Math.Pow(a[0] - lo1[0], 2) + Math.Pow(a[1] - lo1[1], 2) <= Math.Pow(b2[0] - lo1[0], 2) + Math.Pow(b2[1] - lo1[1], 2)) { n1 = a; n0 = b2; } else { n1 = b2; n0 = a; }
    var rNew = rp.Select(q => new[] { q[0], q[1], q[2] }).ToList(); rNew[n - 3] = n1; rNew[n - 2] = n0;
    var fNew = new List<double[]> { n0, n1, new[] { n1[0] + dx * ramp, n1[1] + dy * ramp, z - drop }, new[] { n0[0] + dx * ramp, n0[1] + dy * ramp, z - drop } };
    work.Add((red, rNew)); work.Add((front, fNew));
    Out.AppendLine($"{strip} into {stk}: lobe {Ws - 0.5:F1} m across the stockpile (was {Math.Sqrt(Math.Pow(lo1[0] - lo0[0], 2) + Math.Pow(lo1[1] - lo0[1], 2)):F2} m along the decline), front ramp {Area(fp):F1} -> {Area(fNew):F1} m2, high {Area(rp):F1} -> {Area(rNew):F1} m2");
}
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }
foreach (var (f, p) in work)
{
    var c = (Deswik.Graphics.Primaries.Figure)f.Clone(); var pl = c.asPolyline; while (pl.VertexCount > 0) pl.RemoveVertexAt(0);
    foreach (var q in p) pl.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]); pl.Closed = true;
    App.ModelSpace.Entities.AddItem(c, null, null, true, false); f.Deleted = true;
}
App.Redraw(true); Out.AppendLine("applied"); return null;
