// MPA 07, Design Master style: the EWD and RAD centrelines start on the decline wall (half the decline width off its
// centreline, like G6410 ISP off G6410 MLA), so the drives do not intersect; each keeps one grade (+1:50) from the
// decline floor there. The fillet strips go (the fillet is in the wall design). Wall strings follow any floor change.
const bool APPLY = false; const double G = 50.0;
const string CL = "0\\DESIGN DRAFT\\CENTERLINES", WL = "0\\DESIGN DRAFT\\WALLS", DS = "0\\DESIGN DRAFT\\STRIPPING";
var inv = System.Globalization.CultureInfo.InvariantCulture;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
List<double[]> P3(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
void Set(Deswik.Graphics.Primaries.Figure f, List<double[]> p) { var pl = f.asPolyline; bool c = pl.Closed; while (pl.VertexCount > 0) pl.RemoveVertexAt(0); foreach (var q in p) pl.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]); pl.Closed = c; }
(double d, double z) Near(List<double[]> D, double x, double y)
{
    double bd = 1e9, bz = 0;
    for (int i = 1; i < D.Count; i++) { double ax = D[i - 1][0], ay = D[i - 1][1], vx = D[i][0] - ax, vy = D[i][1] - ay, L2 = vx * vx + vy * vy; double t = L2 > 0 ? Math.Max(0, Math.Min(1, ((x - ax) * vx + (y - ay) * vy) / L2)) : 0; double d = Math.Sqrt(Math.Pow(ax + t * vx - x, 2) + Math.Pow(ay + t * vy - y, 2)); if (d < bd) { bd = d; bz = D[i - 1][2] + t * (D[i][2] - D[i - 1][2]); } }
    return (bd, bz);
}
var cls = new Dictionary<string, Deswik.Graphics.Primaries.Figure>(); var wallsF = new List<Deswik.Graphics.Primaries.Figure>(); var fil = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || A(f, "CLAUDE_MPA07") != "1" || !f.IsPolyline) continue;
    if (f.Layer.Name == CL) cls[A(f, "Name")] = f; else if (f.Layer.Name == WL) wallsF.Add(f); else if (f.Layer.Name == DS && f.asPolyline.VertexCount == 3) fil.Add(f);
}
var dec = P3(cls["MAIN DEC"]);
var m = System.Text.RegularExpressions.Regex.Match(A(cls["MAIN DEC"], "PROFILE"), @"([\d.]+)\s*mW"); double half = (m.Success ? double.Parse(m.Groups[1].Value, inv) : 5.5) / 2;
var oldCl = new Dictionary<string, List<double[]>>(); var newCl = new Dictionary<string, List<double[]>>();
foreach (var n in new[] { "G6390 EWD", "G6390 RAD" })
{
    var p = P3(cls[n]); oldCl[n] = p;
    // walk from the start until the point is 'half' off the decline centreline
    double[] st = null; int k = 1;
    for (; k < p.Count && st == null; k++)
    {
        var a = p[k - 1]; var b = p[k]; double L = Math.Sqrt(Math.Pow(b[0] - a[0], 2) + Math.Pow(b[1] - a[1], 2));
        for (double t = 0; t <= L; t += 0.01) { double x = a[0] + (b[0] - a[0]) * t / L, y = a[1] + (b[1] - a[1]) * t / L; var o = Near(dec, x, y); if (o.d >= half) { st = new[] { x, y, o.z }; break; } }
    }
    var r = new List<double[]> { st }; double s = 0; var prev = st;
    for (int i = k - 1; i < p.Count; i++) { s += Math.Sqrt(Math.Pow(p[i][0] - prev[0], 2) + Math.Pow(p[i][1] - prev[1], 2)); r.Add(new[] { p[i][0], p[i][1], st[2] + s / G }); prev = p[i]; }
    newCl[n] = r;
    Out.AppendLine($"{n}: now starts on the decline wall ({half:F2} m off), {s:F1} m long, RL {st[2]:F2} -> {r[^1][2]:F2} at +1:{G} (trimmed {Math.Sqrt(Math.Pow(st[0] - p[0][0], 2) + Math.Pow(st[1] - p[0][1], 2)):F2} m)");
}
// walls: points nearest a branch keep their height above that branch's floor
double Shift(double x, double y)
{
    double best = Near(dec, x, y).d, sh = 0;
    foreach (var n in newCl.Keys) { var o = Near(oldCl[n], x, y); if (o.d < best) { best = o.d; sh = Near(newCl[n], x, y).z - o.z; } }
    return sh;
}
var wallNew = wallsF.Select(f => (f, p: P3(f).Select(q => new[] { q[0], q[1], q[2] + Shift(q[0], q[1]) }).ToList())).ToList();
double maxW = wallsF.Zip(wallNew, (f, w) => P3(f).Zip(w.p, (a, b) => Math.Abs(a[2] - b[2])).Max()).Max();
Out.AppendLine($"walls: {wallNew.Count} strings follow (largest change {maxW:F2} m); fillet strips to remove: {fil.Count}");
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }
foreach (var n in newCl.Keys) Set(cls[n], newCl[n]);
foreach (var (f, p) in wallNew) Set(f, p);
foreach (var f in fil) f.Deleted = true;
App.Redraw(true); Out.AppendLine("applied"); return null;
