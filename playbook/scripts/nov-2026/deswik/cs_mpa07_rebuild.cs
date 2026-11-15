// MPA 07: north drive -> W6390 RAD, north-east drive -> W6390 EWD, both 5.0mW x 5.0mH SA. Walls rebuilt from the
// centrelines (decline 5.5 m, branches 5.0 m) and cut Design Master style with 3 m fillets in the wall strings.
// Truck loading strips at both mouths (W6390 RAD and W6390 EWD) (its back ramp may run onto the existing decline: the decline used
// for the strip is the existing MAIN DEC string followed by the new one). Cables: on the strip where inside it,
// otherwise on the backs of the nearest drive. Dry run unless APPLY.
const bool APPLY = false;
const double FIL = 3.0, RISE = 1.5, RAMP = 3.5, L8 = 8.0, STEP = 0.5, LOBE = 1.0, EXT = 4.0;
const string CL = "0\\DESIGN DRAFT\\CENTERLINES", WL = "0\\DESIGN DRAFT\\WALLS", DS = "0\\DESIGN DRAFT\\STRIPPING", DB = "0\\DESIGN DRAFT\\CABLEBOLTS", DMC = "0\\DESIGN MASTER\\CENTRELINES";
var inv = System.Globalization.CultureInfo.InvariantCulture;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
List<double[]> P3(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
var layProp = typeof(Deswik.Graphics.Primaries.Figure).GetProperty("Layer");

// ---- inputs
Deswik.Graphics.Primaries.Figure dec = null, north = null, ne = null, exDec = null, labOld = null;
var oldWalls = new List<Deswik.Graphics.Primaries.Figure>(); var oldStrip = new List<Deswik.Graphics.Primaries.Figure>(); var cab = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null) continue; string ln = f.Layer.Name; bool tag = A(f, "CLAUDE_MPA07") == "1";
    if (ln == CL && f.IsPolyline && tag)
    {
        if (A(f, "ACT_TYPE") == "DEC") dec = f;
        else { var p = P3(f); double az = Math.Atan2(p[^1][0] - p[0][0], p[^1][1] - p[0][1]) * 180 / Math.PI; if (az < 30 && az > -30) north = f; else ne = f; }
    }
    else if (ln == WL && f.IsPolyline && tag) oldWalls.Add(f);
    else if (ln == DS && tag && f.IsPolyline) oldStrip.Add(f);
    else if (ln == DS && tag && f.IsMText) labOld = f;
    else if (ln == DB && tag && f.IsPoint) cab.Add(f);
    else if (ln == DMC && f.IsPolyline && A(f, "Name") == "MAIN DEC") { var e = P3(f)[^1]; if (Math.Abs(e[0] - 8082.206) < 0.05 && Math.Abs(e[1] - 63112.193) < 0.05) exDec = f; }
}
object layW = null; foreach (var l in (System.Collections.IEnumerable)App.Layers) if ((string)l.GetType().GetProperty("Name").GetValue(l) == WL) layW = l;
Out.AppendLine($"decline {dec != null}, north {north != null} ({(north != null ? A(north, "Name") : "")}), north-east {ne != null} ({(ne != null ? A(ne, "Name") : "")}), existing decline {exDec != null}, walls {oldWalls.Count}, strip panels {oldStrip.Count}, label {labOld != null}, cables {cab.Count}");
if (dec == null || north == null || ne == null || exDec == null || oldWalls.Count == 0 || oldStrip.Count == 0 || labOld == null || layW == null) { Out.AppendLine("missing input - nothing changed"); return null; }

// ---- helpers
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
// open outline of a drive: left wall from the start, the end face, right wall back to the start (start left open)
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

// ---- new names / profiles (applied at the end); walls use the new widths
var attrs = new Dictionary<Deswik.Graphics.Primaries.Figure, Dictionary<string, string>> {
    { north, new Dictionary<string, string> { { "Name", "W6390 RAD" }, { "ACT_TYPE", "RAD" }, { "Description", "Return Air Drive" }, { "DIRECTION", "N" }, { "PROFILE", "5.0mW x 5.0mH SA" }, { "GSS", "R 3.0" } } },
    { ne, new Dictionary<string, string> { { "Name", "W6390 EWD" }, { "ACT_TYPE", "EWD" }, { "Description", "Escapeway Drive" }, { "DIRECTION", "NE" }, { "PROFILE", "5.0mW x 5.0mH SA" }, { "GSS", "R 3.0" } } } };
double Wd = Wof(dec, "W", 5.5), Hd = Wof(dec, "H", 5.7), Wb = 5.0, Mc = 5.0;

// ---- walls
var dcl = P3(dec); var dw = Outline(dcl, Wd); var dwc = Cum(dw);
var cuts = new List<(double s0, double s1)>(); var newWalls = new List<(List<double[]> p, Deswik.Graphics.Primaries.Figure src)>();
foreach (var br in new[] { north, ne })
{
    var c = P3(br); double dx = c[1][0] - c[0][0], dy = c[1][1] - c[0][1], l = Math.Sqrt(dx * dx + dy * dy);
    var ce = new List<double[]> { new[] { c[0][0] - dx / l * EXT, c[0][1] - dy / l * EXT, c[0][2] } }; ce.AddRange(c);   // reach back into the decline
    var bw = Outline(ce, Wb); var bwc = Cum(bw);
    var xs = Cross(dw, dwc, bw, bwc).OrderBy(q => q.sb).ToList();
    if (xs.Count < 2) { Out.AppendLine($"{attrs[br]["Name"]}: wall does not meet the decline wall - nothing changed"); return null; }
    var x1 = xs.First(); var x2 = xs.Last(); int away1 = x1.sa < x2.sa ? -1 : 1;
    double fa1 = x1.sa + away1 * FIL, fa2 = x2.sa - away1 * FIL; string note = "";
    if (fa1 < 0 || fa2 < 0) { note = $" (fillet on the decline side {Math.Min(x1.sa, x2.sa):F1} m: the existing decline wall starts there)"; fa1 = Math.Max(0, fa1); fa2 = Math.Max(0, fa2); }
    var bp = new List<double[]> { At(dw, dwc, fa1) }; bp.AddRange(Sub(bw, bwc, x1.sb + FIL, x2.sb - FIL)); bp.Add(At(dw, dwc, fa2));
    newWalls.Add((bp, br)); cuts.Add((Math.Min(fa1, fa2), Math.Max(fa1, fa2)));
    Out.AppendLine($"{attrs[br]["Name"]}: {Wb} m wide, mouth at {Math.Min(x1.sa, x2.sa):F1}-{Math.Max(x1.sa, x2.sa):F1} m on the decline wall, 3 m fillets{note}");
}
cuts = cuts.OrderBy(c => c.s0).ToList(); double from = 0;
foreach (var c in cuts) { if (c.s0 - from > 0.05) newWalls.Add((Sub(dw, dwc, from, c.s0), dec)); from = c.s1; }
newWalls.Add((Sub(dw, dwc, from, dwc[^1]), dec));

// ---- truck loading strips at both mouths (W6390 RAD and W6390 EWD), on the existing + new decline
var D = P3(exDec); var nd = dcl.Skip(1).ToList(); D.AddRange(nd); var S = Cum(D);
double[] Pos(double s) => At(D, S, s);
(double tx, double ty) Tan(double s) { var a = Pos(s - 1.0); var b = Pos(s + 1.0); double l = Math.Sqrt(Math.Pow(b[0] - a[0], 2) + Math.Pow(b[1] - a[1], 2)); return ((b[0] - a[0]) / l, (b[1] - a[1]) / l); }
(double s, double d) Proj(double x, double y)
{
    double bs = 0, bd = 1e9;
    for (int i = 1; i < D.Count; i++) { double ax = D[i - 1][0], ay = D[i - 1][1], vx = D[i][0] - ax, vy = D[i][1] - ay, L2 = vx * vx + vy * vy; double t = L2 > 0 ? Math.Max(0, Math.Min(1, ((x - ax) * vx + (y - ay) * vy) / L2)) : 0; double d = Math.Sqrt(Math.Pow(ax + t * vx - x, 2) + Math.Pow(ay + t * vy - y, 2)); if (d < bd) { bd = d; bs = S[i - 1] + t * Math.Sqrt(L2); } }
    return (bs, bd);
}
double sJoint = S[P3(exDec).Count - 1];
double hw = Math.Min(Wd / 2 - 0.25, 2.5), Wi = Wb - 0.5;
IEnumerable<double> St(double a, double b) { int n = Math.Max(1, (int)Math.Ceiling((b - a) / STEP)); return Enumerable.Range(0, n + 1).Select(i => a + (b - a) * i / n); }
var panels = new List<(List<double[]> p, bool ramp)>(); var labPos = new List<double[]>();
foreach (var brF in new[] { north, ne })
{
    var rc = P3(brF); var E0 = rc[0]; var E1 = rc[1]; var pe = Proj(E0[0], E0[1]); double s0 = pe.s;
    double dl = Math.Sqrt(Math.Pow(E1[0] - E0[0], 2) + Math.Pow(E1[1] - E0[1], 2)), Dx = (E1[0] - E0[0]) / dl, Dy = (E1[1] - E0[1]) / dl, sg = (E1[2] - E0[2]) / dl;
    var t0 = Tan(s0); int side = Math.Sign(Dx * -t0.ty + Dy * t0.tx); if (side == 0) side = 1;
    double o0 = s0 - Wi / 2, o1 = s0 + Wi / 2, hi0 = o0, hi1 = o0 + L8;
    if (hi1 + RAMP > S[^1]) { hi1 = o1; hi0 = o1 - L8; }
    double[] Pt(double s, double off, double up) { var c = Pos(s); var t = Tan(s); return new[] { c[0] - t.ty * side * off, c[1] + t.tx * side * off, c[2] + Hd + up }; }
    List<double[]> Band(double a, double b, Func<double, double> up) { var q = St(a, b).Select(s => Pt(s, -hw, up(s))).ToList(); q.AddRange(St(a, b).Reverse().Select(s => Pt(s, hw, up(s)))); return q; }
    panels.Add((Band(hi0 - RAMP, hi0, s => RISE * (s - (hi0 - RAMP)) / RAMP), true));
    panels.Add((Band(hi1, hi1 + RAMP, s => RISE * ((hi1 + RAMP) - s) / RAMP), true));
    double[] Out_(double s, double d) { var w = Pt(s, hw, RISE); return new[] { w[0] + Dx * d, w[1] + Dy * d, w[2] }; }
    var redP = St(hi0, hi1).Select(s => Pt(s, -hw, RISE)).ToList(); redP.Add(Pt(hi1, hw, RISE)); redP.Add(Out_(o1, LOBE)); redP.Add(Out_(o0, LOBE)); redP.Add(Pt(hi0, hw, RISE));
    panels.Add((redP, false));
    var w0 = Out_(o0, LOBE); var w1 = Out_(o1, LOBE); double low = E0[2] + sg * (LOBE + RAMP + pe.d) + Mc, drop = (w0[2] + w1[2]) / 2 - low;
    panels.Add((new List<double[]> { w0, w1, new[] { w1[0] + Dx * RAMP, w1[1] + Dy * RAMP, w1[2] - drop }, new[] { w0[0] + Dx * RAMP, w0[1] + Dy * RAMP, w0[2] - drop } }, true));
    labPos.Add(Pt((hi0 + hi1) / 2, -hw - 5.0, RISE));
    Out.AppendLine($"truck loading strip at {attrs[brF]["Name"]}: high section {hi0 - sJoint:F1} to {hi1 - sJoint:F1} m from the joint, {2 * hw + LOBE:F1} m wide; back ramp {Math.Max(0, sJoint - (hi0 - RAMP)):F1} m onto the existing decline; front ramp drops {drop:F2} m into the drive");
}
// ---- cables: strip surface inside the strip, else backs of the nearest drive
bool Inside(List<double[]> P, double x, double y) { bool c = false; for (int i = 0, j = P.Count - 1; i < P.Count; j = i++) if ((P[i][1] > y) != (P[j][1] > y) && x < (P[j][0] - P[i][0]) * (y - P[i][1]) / (P[j][1] - P[i][1]) + P[i][0]) c = !c; return c; }
double Idw(List<double[]> P, double x, double y) { var N = P.OrderBy(q => Math.Pow(q[0] - x, 2) + Math.Pow(q[1] - y, 2)).Take(4).ToList(); double ws = 0, zs = 0; foreach (var q in N) { double w = 1 / (1e-6 + Math.Pow(q[0] - x, 2) + Math.Pow(q[1] - y, 2)); ws += w; zs += w * q[2]; } return zs / ws; }
(double d, double z) Near(List<double[]> P, double x, double y) { double bd = 1e9, bz = 0; for (int i = 1; i < P.Count; i++) { double ax = P[i - 1][0], ay = P[i - 1][1], vx = P[i][0] - ax, vy = P[i][1] - ay, L2 = vx * vx + vy * vy; double t = L2 > 0 ? Math.Max(0, Math.Min(1, ((x - ax) * vx + (y - ay) * vy) / L2)) : 0; double d = Math.Sqrt(Math.Pow(ax + t * vx - x, 2) + Math.Pow(ay + t * vy - y, 2)); if (d < bd) { bd = d; bz = P[i - 1][2] + t * (P[i][2] - P[i - 1][2]); } } return (bd, bz); }
var cabZ = new List<(Deswik.Graphics.Primaries.Figure f, double z)>(); int onStrip = 0;
foreach (var f in cab)
{
    var ip = f.asPoint.InsertionPoint; var hit = panels.Where(p => Inside(p.p, ip.x, ip.y)).Select(p => Idw(p.p, ip.x, ip.y)).OrderByDescending(z => z).ToList();
    double z; if (hit.Count > 0) { z = hit[0]; onStrip++; }
    else { var a = Near(D, ip.x, ip.y); var b = Near(P3(north), ip.x, ip.y); var c = Near(P3(ne), ip.x, ip.y); z = a.d <= Math.Min(b.d, c.d) ? a.z + Hd : (b.d <= c.d ? b.z : c.z) + Mc; }
    if (Math.Abs(z - ip.z) > 0.005) cabZ.Add((f, z));
}
Out.AppendLine($"walls: {newWalls.Count} strings; cables: {onStrip} on the strip, {cabZ.Count} change height");
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }

// ---- apply
foreach (var kv in attrs) foreach (var a in kv.Value) kv.Key.AttributeValueSet(a.Key, a.Value);
var wallSrc = (Deswik.Graphics.Primaries.Figure)oldWalls[0].Clone();   // copied while it is still live
foreach (var f in oldWalls) f.Deleted = true;
foreach (var (p, src) in newWalls)
{
    var c = (Deswik.Graphics.Primaries.Figure)wallSrc.Clone(); var pl = c.asPolyline; while (pl.VertexCount > 0) pl.RemoveVertexAt(0);
    foreach (var q in p) pl.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]); pl.Closed = false;
    foreach (var k in new[] { "Name", "ACT_TYPE", "PROFILE", "GSS", "Description", "DIRECTION" }) c.AttributeValueSet(k, attrs.ContainsKey(src) && attrs[src].ContainsKey(k) ? attrs[src][k] : A(src, k));
    App.ModelSpace.Entities.AddItem(c, null, null, true, false);
}
var red = oldStrip.First(f => f.IsPolyline && f.asPolyline.HatchProperties?.FillBackColor.ColorIndex != 253); var grey = oldStrip.First(f => f.asPolyline.HatchProperties?.FillBackColor.ColorIndex == 253);
foreach (var (p, isRamp) in panels)
{
    var c = (Deswik.Graphics.Primaries.Figure)(isRamp ? grey : red).Clone(); var pl = c.asPolyline; while (pl.VertexCount > 0) pl.RemoveVertexAt(0);
    foreach (var q in p) pl.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]); pl.Closed = true; App.ModelSpace.Entities.AddItem(c, null, null, true, false);
}
foreach (var f in oldStrip) f.Deleted = true;
foreach (var lp in labPos) { var lc = (Deswik.Graphics.Primaries.Figure)labOld.Clone(); lc.asMText.InsertionPoint = new Deswik.Graphics.Geometry.Point(lp[0], lp[1], lp[2]); App.ModelSpace.Entities.AddItem(lc, null, null, true, false); }
labOld.Deleted = true;
foreach (var (f, z) in cabZ) { var ip = f.asPoint.InsertionPoint; f.asPoint.InsertionPoint = new Deswik.Graphics.Geometry.Point(ip.x, ip.y, z); }
App.Redraw(true); Out.AppendLine("applied"); return null;
