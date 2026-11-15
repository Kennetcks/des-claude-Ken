// MPA 07: W6390 DSP (was EWD; OLD = current name), moved MOVE m down the decline, 5.5 x 5.5, same azimuth and grade, 20.0 m long.
// - centreline: shifted by the decline's own 3D step from station s to s + MOVE, start put back on the decline wall
//   along the drive, end set 20.0 m (3D) from the start
// - decline wall: the two MAIN DEC wall strings either side of the old mouth are joined (gap filled from the decline
//   outline), then cut for the new mouth with 3 m fillets (as cs_mpa07_recentre.cs)
// - drive wall rebuilt at 5.5 m wide; labels and grade arrow moved with the start; Name/ACT_TYPE/PROFILE/Description set
// GSS left as it is. CENTERLINES\SOLIDS and CENTERLINES\WALLS (Drafter-generated) are not touched. Dry run unless APPLY.
const bool APPLY = false;
const double MOVE = 5.0, LEN = 20.0, W = 5.5, FIL = 3.0, EXT = 4.0;
const string OLD = "W6390 DSP", NEW = "W6390 DSP", PROF = "5.5mW x 5.5mH SA", DESC = "Decline Stockpile";
const string CL = "0\\DESIGN DRAFT\\CENTERLINES", WL = "0\\DESIGN DRAFT\\WALLS";
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
List<double[]> P3(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
double D2(double[] a, double[] b) => Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2));

Deswik.Graphics.Primaries.Figure dec = null, cl = null, ew = null; var dw = new List<Deswik.Graphics.Primaries.Figure>(); var labs = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || A(f, "CLAUDE_MPA07") != "1") continue; var ln = f.Layer.Name; var n = A(f, "Name");
    if (ln == CL && f.IsPolyline) { if (A(f, "ACT_TYPE") == "DEC") dec = f; else if (n == OLD) cl = f; }
    else if (ln == WL && f.IsPolyline) { if (n == OLD) ew = f; else if (n == "MAIN DEC") dw.Add(f); }
    else if (ln.StartsWith(CL + "\\") && n == OLD && !ln.EndsWith("\\SOLIDS") && !ln.EndsWith("\\WALLS")) labs.Add(f);
}
Out.AppendLine($"decline {dec != null}, {OLD} centreline {cl != null}, wall {ew != null}, decline wall strings {dw.Count}, labels {labs.Count}");
if (dec == null || cl == null || ew == null || dw.Count == 0) { Out.AppendLine("missing input - nothing changed"); return null; }

// ---- helpers (as cs_mpa07_recentre.cs)
List<double> Cum(List<double[]> p) { var c = new List<double> { 0 }; for (int i = 1; i < p.Count; i++) c.Add(c[i - 1] + D2(p[i], p[i - 1])); return c; }
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
double Proj(List<double[]> p, List<double> c, double[] q) { double bs = 0, bd = 1e9; for (int i = 1; i < p.Count; i++) { double vx = p[i][0] - p[i - 1][0], vy = p[i][1] - p[i - 1][1], L2 = vx * vx + vy * vy; if (L2 < 1e-12) continue; double t = Math.Max(0, Math.Min(1, ((q[0] - p[i - 1][0]) * vx + (q[1] - p[i - 1][1]) * vy) / L2)); double d = Math.Sqrt(Math.Pow(p[i - 1][0] + t * vx - q[0], 2) + Math.Pow(p[i - 1][1] + t * vy - q[1], 2)); if (d < bd) { bd = d; bs = c[i - 1] + t * (c[i] - c[i - 1]); } } return bs; }

// ---- decline wall: the string ending at one end of the old EWD wall, and the one starting at the other end
var E = P3(ew); Deswik.Graphics.Primaries.Figure wa = null, wb = null;
foreach (var f in dw) { var p = P3(f); if (D2(p[^1], E[0]) < 0.01 || D2(p[^1], E[^1]) < 0.01) wa = f; if (D2(p[0], E[0]) < 0.01 || D2(p[0], E[^1]) < 0.01) wb = f; }
if (wa == null || wb == null || wa == wb) { Out.AppendLine($"decline wall either side of the old mouth not found (before {wa != null}, after {wb != null}) - nothing changed"); return null; }
var Pa = P3(wa); var Pb = P3(wb);
var D = P3(dec); var DC = Cum(D); var loop = Outline(D, 5.5); var lc = Cum(loop);
double ua = Proj(loop, lc, Pa[^1]), ub = Proj(loop, lc, Pb[0]);
var merged = new List<double[]>(Pa); merged.AddRange(Sub(loop, lc, ua, ub).Skip(1).SkipLast(1)); merged.AddRange(Pb); var mc = Cum(merged);
Out.AppendLine($"decline wall: {Pa.Count} + {Pb.Count} vertex strings joined across the old mouth ({D2(Pa[^1], Pb[0]):F2} m gap, {Sub(loop, lc, ua, ub).Count - 2} outline points added)");

// ---- new centreline
var C = P3(cl); double s0 = Proj(D, DC, C[0]);
var d0 = At(D, DC, s0); var d1 = At(D, DC, s0 + MOVE); double vx0 = d1[0] - d0[0], vy0 = d1[1] - d0[1], vz0 = d1[2] - d0[2];
var c0 = new[] { C[0][0] + vx0, C[0][1] + vy0, C[0][2] + vz0 }; var c1 = new[] { C[^1][0] + vx0, C[^1][1] + vy0, C[^1][2] + vz0 };
double ux = c1[0] - c0[0], uy = c1[1] - c0[1], uz = c1[2] - c0[2], Lp = Math.Sqrt(ux * ux + uy * uy); double g = uz / Lp; ux /= Lp; uy /= Lp;
var ray = new List<double[]> { c1, new[] { c1[0] - ux * (Lp + 6), c1[1] - uy * (Lp + 6), 0 } }; var hits = Cross(ray, Cum(ray), merged, mc);
if (hits.Count == 0) { Out.AppendLine("moved drive does not reach the decline wall - nothing changed"); return null; }
double t0 = hits.Min(q => q.sa); var ns = new[] { c1[0] - ux * t0, c1[1] - uy * t0, c1[2] - g * t0 };
double k3 = LEN / Math.Sqrt(1 + g * g); var ne = new[] { ns[0] + ux * k3, ns[1] + uy * k3, ns[2] + g * k3 };
double dx = ns[0] - C[0][0], dy = ns[1] - C[0][1], dz = ns[2] - C[0][2];
Out.AppendLine($"decline station of the drive {s0:F2} -> {s0 + MOVE:F2} m (decline drops {-vz0:F3} m over the {MOVE} m)");
Out.AppendLine($"start ({C[0][0]:F3},{C[0][1]:F3},{C[0][2]:F3}) -> ({ns[0]:F3},{ns[1]:F3},{ns[2]:F3}), moved {Math.Sqrt(dx * dx + dy * dy):F2} m in plan, {dz:F3} m in RL");
Out.AppendLine($"end ({ne[0]:F3},{ne[1]:F3},{ne[2]:F3}); length {LEN:F1} m 3D, grade 1:{1 / g:F0} kept, azimuth kept");

// ---- new drive wall and decline wall cut
var ce = new List<double[]> { new[] { ns[0] - ux * EXT, ns[1] - uy * EXT, ns[2] }, ns, ne };
var bw = Outline(ce, W); var bc = Cum(bw);
var xs = Cross(merged, mc, bw, bc).OrderBy(q => q.sb).ToList();
if (xs.Count < 2) { Out.AppendLine("new wall does not meet the decline wall - nothing changed"); return null; }
var x1 = xs.First(); var x2 = xs.Last(); int away = x1.sa < x2.sa ? -1 : 1;
double fa1 = x1.sa + away * FIL, fa2 = x2.sa - away * FIL;
var bp = new List<double[]> { At(merged, mc, fa1) }; bp.AddRange(Sub(bw, bc, x1.sb + FIL, x2.sb - FIL)); bp.Add(At(merged, mc, fa2));
var wa2 = Sub(merged, mc, 0, Math.Min(fa1, fa2)); var wb2 = Sub(merged, mc, Math.Max(fa1, fa2), mc[^1]);
Out.AppendLine($"new mouth {Math.Abs(x2.sa - x1.sa):F2} m on the decline wall, 3 m fillets; decline wall strings now {wa2.Count} + {wb2.Count} vertices, drive wall {bp.Count} vertices");
Out.AppendLine("labels to move and rename: " + string.Join(", ", labs.Select(f => f.Layer.Name.Substring(CL.Length + 1) + (f.IsText ? " '" + f.asText.TextString + "'" : ""))));
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }

// ---- apply
Deswik.Graphics.Primaries.Figure Make(Deswik.Graphics.Primaries.Figure src, List<double[]> p)
{
    var c = (Deswik.Graphics.Primaries.Figure)src.Clone(); var pl = c.asPolyline; while (pl.VertexCount > 0) pl.RemoveVertexAt(0);
    foreach (var q in p) pl.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]); pl.Closed = false; App.ModelSpace.Entities.AddItem(c, null, null, true, false); return c;
}
void Attrs(Deswik.Graphics.Primaries.Figure f) { f.AttributeValueSet("Name", NEW); f.AttributeValueSet("ACT_TYPE", "DSP"); f.AttributeValueSet("PROFILE", PROF); f.AttributeValueSet("Description", DESC); }
Make(wa, wa2); Make(wb, wb2); wa.Deleted = true; wb.Deleted = true;
var nw = Make(ew, bp); Attrs(nw); ew.Deleted = true;
var nc = Make(cl, new List<double[]> { ns, ne }); Attrs(nc); cl.Deleted = true;
foreach (var f in labs)
{
    f.Translate(dx, dy, dz); Attrs(f);
    if (f.IsText && f.asText.TextString.Trim() == OLD) f.asText.TextString = NEW;
    else if (f.IsText && f.asText.TextString.Trim() == "5.0mW x 5.0mH SA") f.asText.TextString = PROF;
    foreach (var name in new[] { "Update", "Invalidate" }) { var m = f.GetType().GetMethods().FirstOrDefault(x => x.Name == name && x.GetParameters().Length == 0); if (m != null) try { m.Invoke(f, null); } catch { } }
}
var rf = App.GetType().GetMethods().FirstOrDefault(x => x.Name == "Refresh" && x.GetParameters().Length == 0); rf?.Invoke(App, null);
App.Redraw(true); Out.AppendLine("applied"); return null;
