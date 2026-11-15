// MPA 07: put the MAIN DEC centreline (DESIGN DRAFT, CLAUDE_MPA07) on the centre of the cut solids on
// 0\SKETCH LAYER\MPA 07, and move what hangs off it to match:
// - decline: each vertex moved sideways by the solids' centre offset at its station (0 at the joint with the existing
//   decline, interpolated between solids), RLs recomputed so the grade stays exactly 1:7 from the joint
// - W6390 RAD / W6390 EWD: start point moved along the drive onto the new decline wall (grade kept)
// - walls: rebuilt from the centrelines, Design Master style, 3 m fillets (as cs_mpa07_rebuild.cs)
// - truck strips, strip labels, cables and CB labels: moved sideways with the decline at their station
// COG lines on the RAD are not affected. Dry run unless APPLY.
const bool APPLY = false;
const double FIL = 3.0, EXT = 4.0;
const string SK = "0\\SKETCH LAYER\\MPA 07", CL = "0\\DESIGN DRAFT\\CENTERLINES", WL = "0\\DESIGN DRAFT\\WALLS", DS = "0\\DESIGN DRAFT\\STRIPPING", DB = "0\\DESIGN DRAFT\\CABLEBOLTS";
var inv = System.Globalization.CultureInfo.InvariantCulture;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
List<double[]> P3(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();

Deswik.Graphics.Primaries.Figure dec = null, rad = null, ewd = null;
var walls = new List<Deswik.Graphics.Primaries.Figure>(); var strip = new List<Deswik.Graphics.Primaries.Figure>(); var stripLab = new List<Deswik.Graphics.Primaries.Figure>();
var cab = new List<Deswik.Graphics.Primaries.Figure>(); var cabLab = new List<Deswik.Graphics.Primaries.Figure>(); var sol = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null) continue; var ln = f.Layer.Name; bool tag = A(f, "CLAUDE_MPA07") == "1";
    if (ln == CL && f.IsPolyline && tag) { var n = A(f, "Name"); if (A(f, "ACT_TYPE") == "DEC") dec = f; else if (n == "W6390 RAD") rad = f; else if (n == "W6390 EWD") ewd = f; }
    else if (ln == WL && f.IsPolyline && tag) walls.Add(f);
    else if (ln == DS && tag && f.IsPolyline) strip.Add(f);
    else if (ln == DS && tag && f.IsMText) stripLab.Add(f);
    else if (ln == DB && tag && f.IsPoint) cab.Add(f);
    else if (ln == DB && tag && (f.IsMText || f.IsText)) cabLab.Add(f);
    else if (ln == SK && f.IsPolyface && A(f, "_CPX") != "") sol.Add(f);
}
object layW = null; foreach (var l in (System.Collections.IEnumerable)App.Layers) if ((string)l.GetType().GetProperty("Name").GetValue(l) == WL) layW = l;
Out.AppendLine($"decline {dec != null}, RAD {rad != null}, EWD {ewd != null}, walls {walls.Count}, strip panels {strip.Count}, strip labels {stripLab.Count}, cables {cab.Count}, CB labels {cabLab.Count}, solids {sol.Count}");
if (dec == null || rad == null || ewd == null || walls.Count == 0 || layW == null) { Out.AppendLine("missing input - nothing changed"); return null; }

// ---- helpers (as cs_mpa07_rebuild.cs)
List<double> Cum(List<double[]> p) { var c = new List<double> { 0 }; for (int i = 1; i < p.Count; i++) c.Add(c[i - 1] + Math.Sqrt(Math.Pow(p[i][0] - p[i - 1][0], 2) + Math.Pow(p[i][1] - p[i - 1][1], 2))); return c; }
double[] At(List<double[]> p, List<double> c, double s) { s = Math.Max(0, Math.Min(c[^1], s)); int i = 1; while (i < p.Count - 1 && c[i] < s) i++; double t = c[i] > c[i - 1] ? (s - c[i - 1]) / (c[i] - c[i - 1]) : 0; return new[] { p[i - 1][0] + t * (p[i][0] - p[i - 1][0]), p[i - 1][1] + t * (p[i][1] - p[i - 1][1]), p[i - 1][2] + t * (p[i][2] - p[i - 1][2]) }; }
List<double[]> Sub(List<double[]> p, List<double> c, double s0, double s1)
{
    var r = new List<double[]> { At(p, c, s0) };
    if (s1 >= s0) { for (int i = 0; i < p.Count; i++) if (c[i] > s0 + 1e-6 && c[i] < s1 - 1e-6) r.Add(p[i]); } else { for (int i = p.Count - 1; i >= 0; i--) if (c[i] < s0 - 1e-6 && c[i] > s1 + 1e-6) r.Add(p[i]); }
    r.Add(At(p, c, s1)); return r;
}
List<(double sa, double sb, double x, double y)> Cross(List<double[]> a, List<double> ca, List<double[]> b, List<double> cb)
{
    var r = new List<(double, double, double, double)>();
    for (int i = 1; i < a.Count; i++) for (int j = 1; j < b.Count; j++)
    {
        double ax = a[i - 1][0], ay = a[i - 1][1], ux = a[i][0] - ax, uy = a[i][1] - ay, bx = b[j - 1][0], by = b[j - 1][1], vx = b[j][0] - bx, vy = b[j][1] - by;
        double den = ux * vy - uy * vx; if (Math.Abs(den) < 1e-12) continue;
        double t = ((bx - ax) * vy - (by - ay) * vx) / den, w = ((bx - ax) * uy - (by - ay) * ux) / den;
        if (t < 0 || t > 1 || w < 0 || w > 1) continue;
        r.Add((ca[i - 1] + t * (ca[i] - ca[i - 1]), cb[j - 1] + w * (cb[j] - cb[j - 1]), ax + t * ux, ay + t * uy));
    }
    return r;
}
List<double[]> Outline(List<double[]> c, double w)
{
    var L = new List<double[]>(); var R = new List<double[]>();
    for (int i = 0; i < c.Count; i++)
    {
        double nx = 0, ny = 0;
        if (i > 0) { double dx = c[i][0] - c[i - 1][0], dy = c[i][1] - c[i - 1][1], l = Math.Sqrt(dx * dx + dy * dy); nx += -dy / l; ny += dx / l; }
        if (i < c.Count - 1) { double dx = c[i + 1][0] - c[i][0], dy = c[i + 1][1] - c[i][1], l = Math.Sqrt(dx * dx + dy * dy); nx += -dy / l; ny += dx / l; }
        double nl = Math.Sqrt(nx * nx + ny * ny); nx /= nl; ny /= nl;
        double k = 1.0; if (i > 0 && i < c.Count - 1) { double dx = c[i + 1][0] - c[i][0], dy = c[i + 1][1] - c[i][1], l = Math.Sqrt(dx * dx + dy * dy); k = 1 / Math.Max(0.5, (-dy / l) * nx + (dx / l) * ny); }
        L.Add(new[] { c[i][0] + nx * w / 2 * k, c[i][1] + ny * w / 2 * k, c[i][2] }); R.Add(new[] { c[i][0] - nx * w / 2 * k, c[i][1] - ny * w / 2 * k, c[i][2] });
    }
    R.Reverse(); L.AddRange(R); return L;
}
double Wof(Deswik.Graphics.Primaries.Figure f, string u, double def) { var m = System.Text.RegularExpressions.Regex.Match(A(f, "PROFILE"), @"([\d.]+)\s*m" + u); return m.Success ? double.Parse(m.Groups[1].Value, inv) : def; }

// ---- station, offset and right-hand normal on the OLD decline
var D0 = P3(dec); var C0 = Cum(D0);
(double s, double d, double nx, double ny) OnDec(double x, double y)
{
    (double, double, double, double) best = (0, 1e9, 0, 0);
    for (int i = 1; i < D0.Count; i++) { var a = D0[i - 1]; var b = D0[i]; double vx = b[0] - a[0], vy = b[1] - a[1], L = Math.Sqrt(vx * vx + vy * vy); if (L < 1e-9) continue; double t = Math.Max(0, Math.Min(1, ((x - a[0]) * vx + (y - a[1]) * vy) / (L * L))); double px = a[0] + t * vx, py = a[1] + t * vy, dd = Math.Sqrt(Math.Pow(x - px, 2) + Math.Pow(y - py, 2)); if (dd < best.Item2) best = (C0[i - 1] + t * L, dd, vy / L, -vx / L); }
    return best;
}
// solids' centre offsets (+ = right of travel) by station
var offs = new List<(double s, double off)> { (0, 0) };
foreach (var s in sol)
{
    double cx = double.Parse(A(s, "_CPX"), inv), cy = double.Parse(A(s, "_CPY"), inv); var o = OnDec(cx, cy);
    if (o.d > 2.0 || o.s < 0.5) continue;   // branch solids and the existing decline
    var acr = new List<double>(); foreach (var v in (System.Collections.IEnumerable)s.asPolyface.VertexList) { var t = v.GetType(); acr.Add((Convert.ToDouble(t.GetProperty("x").GetValue(v)) - cx) * o.nx + (Convert.ToDouble(t.GetProperty("y").GetValue(v)) - cy) * o.ny); }
    var side = Math.Sign((cx - At(D0, C0, o.s)[0]) * o.nx + (cy - At(D0, C0, o.s)[1]) * o.ny);
    offs.Add((o.s, side * o.d + (acr.Min() + acr.Max()) / 2));
}
offs = offs.OrderBy(q => q.s).ToList();
double OffAt(double s) { if (s >= offs[^1].s) return offs[^1].off; for (int i = 1; i < offs.Count; i++) if (s <= offs[i].s) { double t = (s - offs[i - 1].s) / (offs[i].s - offs[i - 1].s); return offs[i - 1].off + t * (offs[i].off - offs[i - 1].off); } return 0; }
Out.AppendLine($"solid centres right of the decline by {offs.Skip(1).Min(q => q.off):F2} to {offs.Skip(1).Max(q => q.off):F2} m ({offs.Count - 1} solids)");

// ---- new decline
var D1 = new List<double[]>();
for (int i = 0; i < D0.Count; i++)
{
    var a = D0[Math.Max(0, i - 1)]; var b = D0[Math.Min(D0.Count - 1, i + 1)]; double vx = b[0] - a[0], vy = b[1] - a[1], L = Math.Sqrt(vx * vx + vy * vy);
    double o = OffAt(C0[i]); D1.Add(new[] { D0[i][0] + vy / L * o, D0[i][1] - vx / L * o, 0 });
}
var C1 = Cum(D1); for (int i = 0; i < D1.Count; i++) D1[i][2] = D0[0][2] - C1[i] / 7.0;
Out.AppendLine($"decline: {C0[^1]:F2} -> {C1[^1]:F2} m, end RL {D0[^1][2]:F2} -> {D1[^1][2]:F2}, grade 1:{C1[^1] / (D1[0][2] - D1[^1][2]):F3}");
(double x, double y) Shift(double x, double y) { var o = OnDec(x, y); if (o.d > 15) return (x, y); double off = OffAt(o.s); return (x + o.nx * off, y + o.ny * off); }

// ---- branches: start on the new decline wall, along the drive, grade kept
double Wd = Wof(dec, "W", 5.5), Wb = 5.0;
var dwNew = Outline(D1, Wd); var dwc = Cum(dwNew);
List<double[]> Restart(Deswik.Graphics.Primaries.Figure br)
{
    var p = P3(br); double ux = p[1][0] - p[0][0], uy = p[1][1] - p[0][1], L = Math.Sqrt(ux * ux + uy * uy); ux /= L; uy /= L; double g = (p[1][2] - p[0][2]) / L;
    var ray = new List<double[]> { p[1], new[] { p[1][0] - ux * (L + 4), p[1][1] - uy * (L + 4), 0 } }; var rc = Cum(ray);
    var hit = Cross(ray, rc, dwNew, dwc).OrderBy(q => q.sa).First(); double t = hit.sa;
    var q0 = new[] { p[1][0] - ux * t, p[1][1] - uy * t, p[1][2] - g * t };
    Out.AppendLine($"{A(br, "Name")}: start moves {Math.Sqrt(Math.Pow(q0[0] - p[0][0], 2) + Math.Pow(q0[1] - p[0][1], 2)):F2} m along the drive ({(t > L ? "longer" : "shorter")}), start RL {p[0][2]:F2} -> {q0[2]:F2}");
    var r = new List<double[]>(p); r[0] = q0; return r;
}
var R1 = Restart(rad); var E1 = Restart(ewd);

// ---- walls (as cs_mpa07_rebuild.cs, from the new centrelines)
var cuts = new List<(double s0, double s1)>(); var newWalls = new List<(List<double[]> p, Deswik.Graphics.Primaries.Figure src)>();
foreach (var (br, c) in new[] { (rad, R1), (ewd, E1) })
{
    double dx = c[1][0] - c[0][0], dy = c[1][1] - c[0][1], l = Math.Sqrt(dx * dx + dy * dy);
    var ce = new List<double[]> { new[] { c[0][0] - dx / l * EXT, c[0][1] - dy / l * EXT, c[0][2] } }; ce.AddRange(c);
    var bw = Outline(ce, Wb); var bwc = Cum(bw);
    var xs = Cross(dwNew, dwc, bw, bwc).OrderBy(q => q.sb).ToList();
    if (xs.Count < 2) { Out.AppendLine($"{A(br, "Name")}: wall does not meet the decline wall - nothing changed"); return null; }
    var x1 = xs.First(); var x2 = xs.Last(); int away1 = x1.sa < x2.sa ? -1 : 1;
    double fa1 = x1.sa + away1 * FIL, fa2 = x2.sa - away1 * FIL; string note = "";
    if (fa1 < 0 || fa2 < 0) { note = $" (fillet on the decline side {Math.Min(x1.sa, x2.sa):F1} m: the existing decline wall starts there)"; fa1 = Math.Max(0, fa1); fa2 = Math.Max(0, fa2); }
    var bp = new List<double[]> { At(dwNew, dwc, fa1) }; bp.AddRange(Sub(bw, bwc, x1.sb + FIL, x2.sb - FIL)); bp.Add(At(dwNew, dwc, fa2));
    newWalls.Add((bp, br)); cuts.Add((Math.Min(fa1, fa2), Math.Max(fa1, fa2)));
    Out.AppendLine($"{A(br, "Name")} wall: mouth {Math.Min(x1.sa, x2.sa):F1}-{Math.Max(x1.sa, x2.sa):F1} m on the decline wall, 3 m fillets{note}");
}
cuts = cuts.OrderBy(c => c.s0).ToList(); double from = 0;
foreach (var c in cuts) { if (c.s0 - from > 0.05) newWalls.Add((Sub(dwNew, dwc, from, c.s0), dec)); from = c.s1; }
newWalls.Add((Sub(dwNew, dwc, from, dwc[^1]), dec));

// ---- check: solid centres against the new decline
double worst = 0;
{
    var C = C1; foreach (var s in sol)
    {
        double cx = double.Parse(A(s, "_CPX"), inv), cy = double.Parse(A(s, "_CPY"), inv); var o0 = OnDec(cx, cy); if (o0.d > 2.0 || o0.s < 0.5) continue;
        double bd = 1e9, bnx = 0, bny = 0, bpx = 0, bpy = 0;
        for (int i = 1; i < D1.Count; i++) { var a = D1[i - 1]; var b = D1[i]; double vx = b[0] - a[0], vy = b[1] - a[1], L = Math.Sqrt(vx * vx + vy * vy); double t = Math.Max(0, Math.Min(1, ((cx - a[0]) * vx + (cy - a[1]) * vy) / (L * L))); double px = a[0] + t * vx, py = a[1] + t * vy, dd = Math.Sqrt(Math.Pow(cx - px, 2) + Math.Pow(cy - py, 2)); if (dd < bd) { bd = dd; bnx = vy / L; bny = -vx / L; bpx = px; bpy = py; } }
        var acr = new List<double>(); foreach (var v in (System.Collections.IEnumerable)s.asPolyface.VertexList) { var t = v.GetType(); acr.Add((Convert.ToDouble(t.GetProperty("x").GetValue(v)) - cx) * bnx + (Convert.ToDouble(t.GetProperty("y").GetValue(v)) - cy) * bny); }
        double off = (cx - bpx) * bnx + (cy - bpy) * bny + (acr.Min() + acr.Max()) / 2; worst = Math.Max(worst, Math.Abs(off));
    }
}
Out.AppendLine($"after the move: decline within {worst:F2} m of every solid centre; walls {walls.Count} -> {newWalls.Count} strings; {strip.Count} strip panels, {stripLab.Count} strip labels, {cab.Count} cables, {cabLab.Count} CB labels move with the decline");
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }

// ---- apply
Deswik.Graphics.Primaries.Figure Replace(Deswik.Graphics.Primaries.Figure src, List<double[]> p, bool closed)
{
    var c = (Deswik.Graphics.Primaries.Figure)src.Clone(); var pl = c.asPolyline; while (pl.VertexCount > 0) pl.RemoveVertexAt(0);
    foreach (var q in p) pl.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]); pl.Closed = closed;
    App.ModelSpace.Entities.AddItem(c, null, null, true, false); src.Deleted = true; return c;
}
var wallSrc = (Deswik.Graphics.Primaries.Figure)walls[0].Clone();   // copied while it is still live
foreach (var f in walls) f.Deleted = true;
foreach (var (p, src) in newWalls)
{
    var c = (Deswik.Graphics.Primaries.Figure)wallSrc.Clone(); var pl = c.asPolyline; while (pl.VertexCount > 0) pl.RemoveVertexAt(0);
    foreach (var q in p) pl.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]); pl.Closed = false;
    foreach (var k in new[] { "Name", "ACT_TYPE", "PROFILE", "GSS", "Description", "DIRECTION" }) c.AttributeValueSet(k, A(src, k));
    App.ModelSpace.Entities.AddItem(c, null, null, true, false);
}
foreach (var f in strip) { var p = P3(f).Select(q => { var s = Shift(q[0], q[1]); return new[] { s.x, s.y, q[2] }; }).ToList(); Replace(f, p, f.asPolyline.Closed); }
foreach (var f in cab) { var ip = f.asPoint.InsertionPoint; var s = Shift(ip.x, ip.y); f.asPoint.InsertionPoint = new Deswik.Graphics.Geometry.Point(s.x, s.y, ip.z); }
foreach (var f in stripLab) { var ip = f.asMText.InsertionPoint; var s = Shift(ip.x, ip.y); f.asMText.InsertionPoint = new Deswik.Graphics.Geometry.Point(s.x, s.y, ip.z); }
foreach (var f in cabLab) { if (f.IsMText) { var ip = f.asMText.InsertionPoint; var s = Shift(ip.x, ip.y); f.asMText.InsertionPoint = new Deswik.Graphics.Geometry.Point(s.x, s.y, ip.z); } else { var ip = f.asText.InsertionPoint; var s = Shift(ip.x, ip.y); f.asText.InsertionPoint = new Deswik.Graphics.Geometry.Point(s.x, s.y, ip.z); } }
Replace(dec, D1, false); Replace(rad, R1, false); Replace(ewd, E1, false);
App.Redraw(true); Out.AppendLine("applied"); return null;
