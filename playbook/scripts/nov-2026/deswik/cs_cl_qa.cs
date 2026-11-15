// 
// Body of: object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out)
// Usings: 

if (!(App.Filename ?? "").Contains("06 Dec\\MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG_DOC " + App.Filename); return null; }
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
double Num(string s) { double v; return double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v) ? v : 0; }
var L = "0\\06 SITE\\03 ENGINEERING\\01 DEV STRINGS\\COMBINED 12.11";
var cls = new List<(Deswik.Graphics.Primaries.Figure f, List<double[]> p)>(); var sol = new Dictionary<string, List<Deswik.Graphics.Primaries.Figure>>(); var H = new HashSet<string>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null) continue; H.Add(f.Handle);
    if (f.Layer.Name == L && f.IsPolyline) cls.Add((f, Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList()));
    else if (f.Layer.Name == "SCHEDULER\\TASK SOLIDS" && f.IsPolyface && A(f, "SCHEDULED") == "1" && A(f, "DIS Activity Type") == "Tunnel") { var k = A(f, "DIS Parent ID"); if (!sol.ContainsKey(k)) sol[k] = new List<Deswik.Graphics.Primaries.Figure>(); sol[k].Add(f); }
}
double SegD3(double[] p, List<double[]> pl, out double[] foot) { double b = 1e9; foot = pl[0]; for (int i = 1; i < pl.Count; i++) { double vx = pl[i][0] - pl[i - 1][0], vy = pl[i][1] - pl[i - 1][1], vz = pl[i][2] - pl[i - 1][2], L2 = vx * vx + vy * vy + vz * vz, t = L2 == 0 ? 0 : Math.Max(0, Math.Min(1, ((p[0] - pl[i - 1][0]) * vx + (p[1] - pl[i - 1][1]) * vy + (p[2] - pl[i - 1][2]) * vz) / L2)); var f = new[] { pl[i - 1][0] + t * vx, pl[i - 1][1] + t * vy, pl[i - 1][2] + t * vz }; double d = Math.Sqrt(Math.Pow(f[0] - p[0], 2) + Math.Pow(f[1] - p[1], 2) + Math.Pow(f[2] - p[2], 2)); if (d < b) { b = d; foot = f; } } return b; }
bool InBox(Deswik.Graphics.Primaries.Figure s, double[] p, double m) { var b = s.BoundingBox; return p[0] >= b.Min.x - m && p[0] <= b.Max.x + m && p[1] >= b.Min.y - m && p[1] <= b.Max.y + m && p[2] >= b.Min.z - m && p[2] <= b.Max.z + m; }
var keys = cls.Select(c => A(c.f, "SOURCE_STRING")).ToList();
var srcFig = new Dictionary<string, Deswik.Graphics.Primaries.Figure>(); foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (!f.Deleted && f.Layer != null && f.IsPolyline && f.Layer.Name != L && keys.Contains(f.Handle)) srcFig[f.Handle] = f;
var tag = new Dictionary<string, string>(); foreach (var cl in cls) { var k = A(cl.f, "SOURCE_STRING"); bool cp = false; if (srcFig.TryGetValue(k, out var o)) { var pl = o.asPolyline; var a = pl.GetVertex(0); var b = pl.GetVertex(pl.VertexCount - 1); cp = Math.Abs(a.x - cl.p[0][0]) + Math.Abs(a.y - cl.p[0][1]) < 0.01 || Math.Abs(b.x - cl.p[^1][0]) + Math.Abs(b.y - cl.p[^1][1]) < 0.01 || Math.Abs(a.x - cl.p[1][0]) + Math.Abs(a.y - cl.p[1][1]) < 0.01; } tag[cl.f.Handle] = cp ? "O" : "R"; }
Out.AppendLine($"centrelines {cls.Count}; source strings with SCH=1 solids {sol.Count}; source strings with no centreline: {sol.Keys.Count(k => !keys.Contains(k))}; centrelines with no solids: {keys.Count(k => !sol.ContainsKey(k))}");
var iss = new Dictionary<string, List<string>>(); void Add(string c, string s) { var h = System.Text.RegularExpressions.Regex.Match(s, @"458[0-9A-F]{4}").Value; var t = tag.ContainsKey(h) ? tag[h] : "?"; c = c + " [" + (t == "O" ? "original" : "rebuilt") + "]"; if (!iss.ContainsKey(c)) iss[c] = new List<string>(); iss[c].Add(s); }
var allLat = cls.Where(c => A(c.f, "LAT_VERT") != "VERT").Select(c => c.p).ToList();
foreach (var g in cls.GroupBy(c => A(c.f, "SOURCE_STRING")))
{
    var key = g.Key; if (!sol.ContainsKey(key)) continue; var S = sol[key]; var nm = A(g.First().f, "Name"); bool vert = A(S[0], "LAT_VERT") == "VERT"; var hs = string.Join("/", g.Select(c => c.f.Handle));
    // 1 coverage: each solid's centre near a centreline of its string
    int miss = 0; foreach (var s in S) { var bb = s.BoundingBox; var c = new[] { bb.Midpoint.x, bb.Midpoint.y, bb.Midpoint.z }; double d = g.Min(cl => SegD3(c, cl.p, out _)); double half = Math.Max(bb.Max.z - bb.Min.z, 3) / 2 + 1.0; if (d > half) miss++; }
    if (miss > 0) Add("solids with no centreline through them", $"{nm} {hs}: {miss} of {S.Count} cuts");
    // 2 containment: sample every 0.5 m, must be inside some own-solid bbox (+0.3 m) - extensions of up to 3 m at the ends allowed
    double outside = 0, worstOut = 0; foreach (var cl in g) { var p = cl.p; double run = 0, tot = 0; for (int i = 1; i < p.Count; i++) tot += Math.Sqrt(Math.Pow(p[i][0] - p[i - 1][0], 2) + Math.Pow(p[i][1] - p[i - 1][1], 2) + Math.Pow(p[i][2] - p[i - 1][2], 2));
        double seg = 0; for (int i = 1; i < p.Count; i++) { double l = Math.Sqrt(Math.Pow(p[i][0] - p[i - 1][0], 2) + Math.Pow(p[i][1] - p[i - 1][1], 2) + Math.Pow(p[i][2] - p[i - 1][2], 2)); for (double t = 0; t < l; t += 0.5) { var q = new[] { p[i - 1][0] + (p[i][0] - p[i - 1][0]) * t / l, p[i - 1][1] + (p[i][1] - p[i - 1][1]) * t / l, p[i - 1][2] + (p[i][2] - p[i - 1][2]) * t / l }; double ch = seg + t; bool endZone = ch < 3.5 || tot - ch < 3.5; if (!endZone && !S.Any(s => InBox(s, q, 0.3))) { outside += 0.5; } } seg += l; } }
    if (outside > 1.0) Add("centreline outside its own solids (away from the ends)", $"{nm} {hs}: {outside:F1} m outside");
    // 3 length vs scheduled metres
    if (!vert) { double met = S.Sum(s => Num(A(s, "Metres"))); double len = g.Sum(cl => { double t = 0; for (int i = 1; i < cl.p.Count; i++) t += Math.Sqrt(Math.Pow(cl.p[i][0] - cl.p[i - 1][0], 2) + Math.Pow(cl.p[i][1] - cl.p[i - 1][1], 2) + Math.Pow(cl.p[i][2] - cl.p[i - 1][2], 2)); return t; }); if (len < met - 2 || len > met + 8) Add("length vs scheduled metres off", $"{nm} {hs}: CL {len:F1} m vs solids {met:F1} m"); }
    // 4 geometry: interior turns > 45 deg, RL steps, long segments
    if (!vert) foreach (var cl in g) { var p = cl.p; for (int i = 1; i < p.Count; i++) { double l = Math.Sqrt(Math.Pow(p[i][0] - p[i - 1][0], 2) + Math.Pow(p[i][1] - p[i - 1][1], 2)); double dz = p[i][2] - p[i - 1][2]; if (l > 0.3 && Math.Abs(dz / l) > 0.25) Add("steep segment (>25% grade)", $"{nm} {cl.f.Handle} seg {i}: {dz:+0.00;-0.00} m over {l:F1} m"); if (l < 0.3 && Math.Abs(dz) > 0.3) Add("RL step on a near-zero segment", $"{nm} {cl.f.Handle} seg {i}: {dz:+0.00;-0.00} m"); if (l > 15 && !(A(cl.f, "ACT_TYPE") == "DEC")) Add("long straight segment > 15 m", $"{nm} {cl.f.Handle} seg {i}: {l:F1} m"); }
        for (int i = 1; i < p.Count - 1; i++) { double a1 = Math.Atan2(p[i][0] - p[i - 1][0], p[i][1] - p[i - 1][1]), a2 = Math.Atan2(p[i + 1][0] - p[i][0], p[i + 1][1] - p[i][1]); double t = Math.Abs(((a2 - a1) * 180 / Math.PI + 540) % 360 - 180); if (t > 45) Add("sharp turn > 45 deg", $"{nm} {cl.f.Handle} vertex {i}: {t:F0} deg"); } }
}
// 5 loose ends: a lateral end with another drive (lateral) ahead of it within 3 m in plan and 2 m in RL, not touching
foreach (var c in cls.Where(c => A(c.f, "LAT_VERT") != "VERT" && c.p.Count >= 2)) foreach (var (e, e2) in new[] { (c.p[0], c.p[1]), (c.p[^1], c.p[^2]) })
{
    double dx = e[0] - e2[0], dy = e[1] - e2[1], dl = Math.Sqrt(dx * dx + dy * dy); if (dl < 1e-6) continue; dx /= dl; dy /= dl;
    double best = 1e9; string who = ""; double bz = 0;
    foreach (var o in cls) { if (ReferenceEquals(o.p, c.p) || A(o.f, "LAT_VERT") == "VERT") continue; for (int i = 1; i < o.p.Count; i++) { var q = o.p; double vx = q[i][0] - q[i - 1][0], vy = q[i][1] - q[i - 1][1], L2 = vx * vx + vy * vy; double u = L2 == 0 ? 0 : Math.Max(0, Math.Min(1, ((e[0] - q[i - 1][0]) * vx + (e[1] - q[i - 1][1]) * vy) / L2)); double fx = q[i - 1][0] + u * vx, fy = q[i - 1][1] + u * vy, fz = q[i - 1][2] + u * (q[i][2] - q[i - 1][2]); double d = Math.Sqrt(Math.Pow(fx - e[0], 2) + Math.Pow(fy - e[1], 2)); if (Math.Abs(fz - e[2]) > 2) continue; if (d > 0.1 && ((fx - e[0]) * dx + (fy - e[1]) * dy) / d < 0.5) continue; if (d < best) { best = d; who = A(o.f, "Name"); bz = fz - e[2]; } } }
    if (best > 0.1 && best <= 3) Add("lateral end with a drive ahead but not touching", $"{A(c.f, "Name")} {c.f.Handle} at {e[0]:F1},{e[1]:F1},{e[2]:F1}: {best:F1} m to {who} (RL {bz:+0.0;-0.0})");
}// 6 raises: both ends on a lateral string
foreach (var c in cls.Where(c => A(c.f, "LAT_VERT") == "VERT")) foreach (var (e, w) in new[] { (c.p[0], "bottom"), (c.p[^1], "top") })
{ double best = allLat.Min(o => SegD3(e, o, out _)); if (best > 0.1) Add("raise end not on a drive centreline", $"{A(c.f, "Name")} {c.f.Handle} {w}: {best:F1} m from the nearest drive CL"); }
// 7 duplicates: two centrelines with near-identical start and end
for (int i = 0; i < cls.Count; i++) for (int j = i + 1; j < cls.Count; j++) { var a = cls[i].p; var b = cls[j].p; double d1 = Math.Abs(a[0][0] - b[0][0]) + Math.Abs(a[0][1] - b[0][1]) + Math.Abs(a[^1][0] - b[^1][0]) + Math.Abs(a[^1][1] - b[^1][1]); if (d1 < 0.5) Add("possible duplicate", $"{A(cls[i].f, "Name")} {cls[i].f.Handle} = {A(cls[j].f, "Name")} {cls[j].f.Handle}"); }
foreach (var kv in iss.OrderBy(k => k.Key)) { Out.AppendLine($"## {kv.Key}: {kv.Value.Count}"); if (kv.Key.Contains("[rebuilt]") && !kv.Key.StartsWith("long straight")) foreach (var x in kv.Value.Take(60)) Out.AppendLine("  " + x); }
return null;