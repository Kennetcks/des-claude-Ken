// W390 RAD strings 458905E / 458905F / 4589060: RL only (XY untouched) dropped by DZ so they sit on the W390 RAD W solid
// floor (they were drawn 6.50 m high). Then W412 RAD (new, 0\DESIGN DRAFT\CENTERLINES): from the lowered 458905E v0 out
// along the W412 RAD W solids' bearing and gradient (az 117.53, 5.624 deg down) to where it meets the pit floor (4589044).
const bool APPLY = false;
const double DZ = -6.504;
var inv = System.Globalization.CultureInfo.InvariantCulture;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
var H = new Dictionary<string, Deswik.Graphics.Primaries.Figure>(StringComparer.OrdinalIgnoreCase); foreach (Deswik.Graphics.Primaries.Figure x in App.ModelSpace.Entities) if (!x.Deleted && x.Layer != null) H[x.Handle] = x;
var hs = new[] { "458905E", "458905F", "4589060" }; foreach (var h in hs.Concat(new[] { "4589044" })) if (!H.ContainsKey(h)) { Out.AppendLine(h + " missing - nothing done"); return null; }
List<double[]> P3(string h) => Enumerable.Range(0, H[h].asPolyline.VertexCount).Select(i => { var q = H[h].asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
var pit = H["4589044"]; var pf = pit.asPolyface; var bb = pit.BoundingBox; double Pz(double x, double y) => pf.GetMaxZ(x, y, bb);
// solid floor reference (W390 RAD W CP chain) for the read-out
var rad = H.Values.Where(f => f.IsPolyface && f.Layer.Name == "0\\3MS\\TASK SOLIDS" && A(f, "Name") == "W390 RAD W").Select(f => new[] { double.Parse(A(f, "_CPX"), inv), double.Parse(A(f, "_CPY"), inv), double.Parse(A(f, "_CPZ"), inv) }).OrderByDescending(c => c[2]).ToList();
double Sol(double x, double y) { double bd = 1e9, bz = 0; for (int i = 1; i < rad.Count; i++) { double vx = rad[i][0] - rad[i - 1][0], vy = rad[i][1] - rad[i - 1][1], L2 = vx * vx + vy * vy, t = Math.Max(0, Math.Min(1, ((x - rad[i - 1][0]) * vx + (y - rad[i - 1][1]) * vy) / L2)); double dd = Math.Pow(rad[i - 1][0] + t * vx - x, 2) + Math.Pow(rad[i - 1][1] + t * vy - y, 2); if (dd < bd) { bd = dd; bz = rad[i - 1][2] + t * (rad[i][2] - rad[i - 1][2]); } } return bz; }
var nw = new Dictionary<string, List<double[]>>();
foreach (var h in hs)
{
    var p = P3(h).Select(q => new[] { q[0], q[1], q[2] + DZ }).ToList(); nw[h] = p; var r = p.Select(q => q[2] - Sol(q[0], q[1])).ToList();
    Out.AppendLine($"{h} {A(H[h], "ACT_TYPE")}: {p.Count} pts, RL {P3(h)[0][2]:F3}->{p[0][2]:F3} ... {P3(h)[^1][2]:F3}->{p[^1][2]:F3}; vs solid floor {r.Min():+0.000;-0.000} to {r.Max():+0.000;-0.000}");
}
// anything else in the draft that shares a vertex with the three strings (not changed, just listed)
foreach (var f in H.Values.Where(f => f.IsPolyline && !hs.Contains(f.Handle) && f.Layer.Name.StartsWith("0\\DESIGN DRAFT")))
{
    var q = P3(f.Handle); var hit = q.Where(a => hs.Any(h => P3(h).Any(b => Math.Abs(a[0] - b[0]) < 0.05 && Math.Abs(a[1] - b[1]) < 0.05 && Math.Abs(a[2] - b[2]) < 0.05))).Count();
    if (hit > 0) Out.AppendLine($"  shares {hit} vertex(es) with them: {f.Handle} [{f.Layer.Name}] {A(f, "ACT_TYPE")}");
}
// W412 RAD
double az = 117.53 * Math.PI / 180, g = Math.Tan(5.624 * Math.PI / 180); var S = nw["458905E"][0]; double sx = Math.Sin(az), sy = Math.Cos(az);
double[] At(double d) => new[] { S[0] + d * sx, S[1] + d * sy, S[2] - d * g };
double Gap(double d) { var q = At(d); return Pz(q[0], q[1]) - q[2]; } // > 0: still under the pit surface
Out.AppendLine($"W412 start = 458905E v0 lowered ({S[0]:F3},{S[1]:F3},{S[2]:F3}), pit above it {Pz(S[0], S[1]):F3}");
double lo = 0, hi = 0; for (double d = 0.05; d < 30; d += 0.05) if (Gap(d) <= 0) { hi = d; lo = d - 0.05; break; }
if (hi == 0) { Out.AppendLine("string never reaches the pit floor within 30 m - nothing done"); return null; }
for (int it = 0; it < 40; it++) { double m = (lo + hi) / 2; if (Gap(m) > 0) lo = m; else hi = m; }
var E = At(hi); var pts = new List<double[]> { S }; double step = 3.5 * Math.Cos(Math.Atan(g)); for (double d = step; d < hi - 1.0; d += step) pts.Add(At(d)); pts.Add(E);
Out.AppendLine($"W412 end ({E[0]:F3},{E[1]:F3},{E[2]:F3}) pit {Pz(E[0], E[1]):F3}; plan {hi:F2} m, slope {hi / Math.Cos(Math.Atan(g)):F2} m, 1:{1 / g:F2}; pts " + string.Join(" ", pts.Select(q => $"({q[0]:F3},{q[1]:F3},{q[2]:F3})")));
for (double d = hi - 1; d <= hi + 3; d += 0.5) { var q = At(d); Out.AppendLine($"  d {d:F2} string {q[2]:F3} pit {Pz(q[0], q[1]):F3}"); }
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }
var log = new System.Text.StringBuilder(); foreach (var h in hs) { log.AppendLine("== " + h); foreach (var q in P3(h)) log.AppendLine($"{q[0]:R},{q[1]:R},{q[2]:R}"); }
System.IO.File.WriteAllText(@"C:\dswtmp\w390rad_rl_backup.txt", log.ToString());
Deswik.Graphics.Primaries.Figure Replace(Deswik.Graphics.Primaries.Figure src, List<double[]> p, bool keep) { var k = (Deswik.Graphics.Primaries.Figure)src.Clone(); var pl = k.asPolyline; bool cl = pl.Closed; while (pl.VertexCount > 0) pl.RemoveVertexAt(0); foreach (var q in p) pl.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]); pl.Closed = cl; App.ModelSpace.Entities.AddItem(k, null, null, true, false); if (!keep) src.Deleted = true; return k; }
var outH = hs.Select(h => Replace(H[h], nw[h], false).Handle).ToList();
var w412 = Replace(H["458905E"], pts, true); w412.AttributeValueSet("LEVEL", "6412"); App.Redraw(true);
Out.AppendLine($"applied: {string.Join(", ", hs.Select((h, i) => h + " -> " + outH[i]))}; W412 RAD {w412.Handle} [{w412.Layer.Name}]; backup C:\\dswtmp\\w390rad_rl_backup.txt");
return null;
