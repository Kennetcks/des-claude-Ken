// MPA 07 (Level Design Drafter): cut the walls at the take-offs with 3 m fillets, Design Master style - open wall
// strings, the decline wall broken across each mouth, the fillet chamfer part of the branch wall - on
// 0\DESIGN DRAFT\WALLS (the closed walls the process made stay as they are). Fillet strips redrawn on the new corners.
// Truck loading strip (6.0 m wide x 8 m, +1.5 m, 3.5 m ramps, flat front ramp) on the decline at the G6390 RAD mouth;
// CB 02 cables inside it moved onto the strip surface. Dry run unless APPLY.
const bool APPLY = false;
const double FIL = 3.0, RISE = 1.5, RAMP = 3.5, L8 = 8.0, STEP = 0.5, LOBE = 1.0;
const string CL = "0\\DESIGN DRAFT\\CENTERLINES", CW = "0\\DESIGN DRAFT\\CENTERLINES\\WALLS", WL = "0\\DESIGN DRAFT\\WALLS", DS = "0\\DESIGN DRAFT\\STRIPPING", DB = "0\\DESIGN DRAFT\\CABLEBOLTS";
const string TRK = "0\\REFERENCE\\STRIP STANDARDS\\TRUCK LOADING TRANSITIONS", DMS = "0\\DESIGN MASTER\\STRIPPING";
var inv = System.Globalization.CultureInfo.InvariantCulture;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
List<double[]> P3(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
double Prof(Deswik.Graphics.Primaries.Figure f, string u, double def) { var m = System.Text.RegularExpressions.Regex.Match(A(f, "PROFILE"), @"([\d.]+)\s*m" + u); return m.Success ? double.Parse(m.Groups[1].Value, inv) : def; }
var layProp = typeof(Deswik.Graphics.Primaries.Figure).GetProperty("Layer");

// ---- inputs
var cls = new Dictionary<string, Deswik.Graphics.Primaries.Figure>(); var walls = new Dictionary<string, Deswik.Graphics.Primaries.Figure>();
var oldFil = new List<Deswik.Graphics.Primaries.Figure>(); var cablePts = new List<Deswik.Graphics.Primaries.Figure>();
Deswik.Graphics.Primaries.Figure red = null, grey = null, labSrc = null; object layW = null, layS = null;
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null) continue; string ln = f.Layer.Name; bool tag = A(f, "CLAUDE_MPA07") == "1";
    if (ln == CL && f.IsPolyline && tag) cls[A(f, "Name")] = f;
    else if (ln == CW && f.IsPolyline && f.asPolyline.Closed && tag) walls[A(f, "Name")] = f;
    else if (ln == DS && f.IsPolyline && f.asPolyline.VertexCount == 3 && tag) oldFil.Add(f);
    else if (ln == DB && f.IsPoint && tag) cablePts.Add(f);
    else if (ln == TRK && f.IsPolyline && f.asPolyline.HatchProperties != null) { if (f.asPolyline.HatchProperties.FillBackColor.ColorIndex == 253) grey ??= f; else red ??= f; }
    else if (ln == DMS && f.IsMText && labSrc == null && f.asMText.TextString.StartsWith("Stripping Truck Loading")) labSrc = f;
}
foreach (var l in (System.Collections.IEnumerable)App.Layers) { string n = (string)l.GetType().GetProperty("Name").GetValue(l); if (n == WL) layW = l; else if (n == DS) layS = l; }
Out.AppendLine($"CLs {string.Join(",", cls.Keys)}; walls {string.Join(",", walls.Keys)}; old fillets {oldFil.Count}; cables {cablePts.Count}; styles red {red != null} grey {grey != null} label {labSrc != null}; layers {layW != null}/{layS != null}");
foreach (var n in new[] { "MAIN DEC", "G6390 EWD", "G6390 RAD" }) if (!cls.ContainsKey(n) || !walls.ContainsKey(n)) { Out.AppendLine("missing " + n + " - nothing changed"); return null; }
if (red == null || grey == null || labSrc == null || layW == null || layS == null) { Out.AppendLine("missing style/layer - nothing changed"); return null; }

// ---- polyline helpers (2D length, z carried)
List<double> Cum(List<double[]> p) { var c = new List<double> { 0 }; for (int i = 1; i < p.Count; i++) c.Add(c[i - 1] + Math.Sqrt(Math.Pow(p[i][0] - p[i - 1][0], 2) + Math.Pow(p[i][1] - p[i - 1][1], 2))); return c; }
double[] At(List<double[]> p, List<double> c, double s)
{
    s = Math.Max(0, Math.Min(c[^1], s)); int i = 1; while (i < p.Count - 1 && c[i] < s) i++;
    double t = c[i] > c[i - 1] ? (s - c[i - 1]) / (c[i] - c[i - 1]) : 0; return new[] { p[i - 1][0] + t * (p[i][0] - p[i - 1][0]), p[i - 1][1] + t * (p[i][1] - p[i - 1][1]), p[i - 1][2] + t * (p[i][2] - p[i - 1][2]) };
}
List<double[]> Sub(List<double[]> p, List<double> c, double s0, double s1)
{
    var r = new List<double[]> { At(p, c, s0) };
    if (s1 >= s0) { for (int i = 0; i < p.Count; i++) if (c[i] > s0 + 1e-6 && c[i] < s1 - 1e-6) r.Add(p[i]); }
    else { for (int i = p.Count - 1; i >= 0; i--) if (c[i] < s0 - 1e-6 && c[i] > s1 + 1e-6) r.Add(p[i]); }
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
// closed wall -> open string with the face nearest 'start' removed (v0 and the last vertex are that face's ends)
List<double[]> OpenAt(Deswik.Graphics.Primaries.Figure w, double[] start)
{
    var p = P3(w); int n = p.Count, bi = 0; double bd = 1e9;
    for (int i = 0; i < n; i++) { var a = p[i]; var b = p[(i + 1) % n]; double d = Math.Pow((a[0] + b[0]) / 2 - start[0], 2) + Math.Pow((a[1] + b[1]) / 2 - start[1], 2); if (d < bd) { bd = d; bi = i; } }
    // the start face can be 2 edges (a mid point on the centreline): drop a vertex sitting on the centreline start too
    var r = Enumerable.Range(0, n).Select(k => p[(bi + 1 + k) % n]).ToList();
    if (Math.Sqrt(Math.Pow(r[0][0] - start[0], 2) + Math.Pow(r[0][1] - start[1], 2)) < 0.3) r.RemoveAt(0);
    if (Math.Sqrt(Math.Pow(r[^1][0] - start[0], 2) + Math.Pow(r[^1][1] - start[1], 2)) < 0.3) r.RemoveAt(r.Count - 1);
    return r;
}

// ---- walls
var decCL = P3(cls["MAIN DEC"]);
var dw = OpenAt(walls["MAIN DEC"], decCL[0]); var dwc = Cum(dw);
var cuts = new List<(double s0, double s1)>(); var newWalls = new List<(List<double[]> p, string name)>(); var fillets = new List<(List<double[]> p, string name)>();
foreach (var bn in new[] { "G6390 EWD", "G6390 RAD" })
{
    var bcl = P3(cls[bn]); var bw = OpenAt(walls[bn], bcl[0]); var bwc = Cum(bw);
    var xs = Cross(dw, dwc, bw, bwc).OrderBy(q => q.sb).ToList();
    if (xs.Count < 2) { Out.AppendLine($"{bn}: wall does not cross the decline wall ({xs.Count}) - nothing changed"); return null; }
    var x1 = xs.First(); var x2 = xs.Last();                                   // x1 on the branch's first side, x2 on its second
    int away1 = x1.sa < x2.sa ? -1 : 1;                                         // along the decline wall, away from the mouth
    double fa1 = x1.sa + away1 * FIL, fa2 = x2.sa - away1 * FIL, fb1 = x1.sb + FIL, fb2 = x2.sb - FIL;
    string note = "";
    if (fa1 < 0 || fa2 < 0) { note = $" (fillet on the decline side cut short to {Math.Min(x1.sa, x2.sa):F1} m: the take-off is that close to the existing decline end)"; fa1 = Math.Max(0, fa1); fa2 = Math.Max(0, fa2); }
    var bp = new List<double[]> { At(dw, dwc, fa1) }; bp.AddRange(Sub(bw, bwc, fb1, fb2)); bp.Add(At(dw, dwc, fa2));
    newWalls.Add((bp, bn));
    double[] P1 = { x1.x, x1.y, At(dw, dwc, x1.sa)[2] }, P2 = { x2.x, x2.y, At(dw, dwc, x2.sa)[2] };
    fillets.Add((new List<double[]> { P1, At(dw, dwc, fa1), At(bw, bwc, fb1) }, bn));
    fillets.Add((new List<double[]> { P2, At(dw, dwc, fa2), At(bw, bwc, fb2) }, bn));
    cuts.Add((Math.Min(fa1, fa2), Math.Max(fa1, fa2)));
    Out.AppendLine($"{bn}: mouth on the decline wall at {Math.Min(x1.sa, x2.sa):F1}-{Math.Max(x1.sa, x2.sa):F1} m, wall {Cum(bp)[^1]:F1} m with 3 m fillets{note}");
}
cuts = cuts.OrderBy(c => c.s0).ToList(); double from = 0;
foreach (var c in cuts) { if (c.s0 - from > 0.05) newWalls.Add((Sub(dw, dwc, from, c.s0), "MAIN DEC")); from = c.s1; }
newWalls.Add((Sub(dw, dwc, from, dwc[^1]), "MAIN DEC"));
Out.AppendLine($"decline wall split into {newWalls.Count(w => w.name == "MAIN DEC")} strings (open at the joint with the existing decline)");

// ---- truck loading strip at the RAD mouth
var D = decCL; var S = Cum(D); var rcl = P3(cls["G6390 RAD"]);
double[] Pos(double s) => At(D, S, s);
(double tx, double ty) Tan(double s) { var a = Pos(s - 1.0); var b = Pos(s + 1.0); double l = Math.Sqrt(Math.Pow(b[0] - a[0], 2) + Math.Pow(b[1] - a[1], 2)); return ((b[0] - a[0]) / l, (b[1] - a[1]) / l); }
(double s, double d) Proj(double x, double y)
{
    double bs = 0, bd = 1e9;
    for (int i = 1; i < D.Count; i++) { double ax = D[i - 1][0], ay = D[i - 1][1], vx = D[i][0] - ax, vy = D[i][1] - ay, L2 = vx * vx + vy * vy; double t = L2 > 0 ? Math.Max(0, Math.Min(1, ((x - ax) * vx + (y - ay) * vy) / L2)) : 0; double d = Math.Sqrt(Math.Pow(ax + t * vx - x, 2) + Math.Pow(ay + t * vy - y, 2)); if (d < bd) { bd = d; bs = S[i - 1] + t * Math.Sqrt(L2); } }
    return (bs, bd);
}
double Wd = Prof(cls["MAIN DEC"], "W", 5.5), Hd = Prof(cls["MAIN DEC"], "H", 5.7), Ws = Prof(cls["G6390 RAD"], "W", 5.0), Hs = Prof(cls["G6390 RAD"], "H", 5.0);
double hw = Math.Min(Wd / 2 - 0.25, 2.5), Wi = Ws - 0.5;
var E0 = rcl[0]; var E1 = rcl[1]; var pe = Proj(E0[0], E0[1]); double s0 = pe.s;
double dl = Math.Sqrt(Math.Pow(E1[0] - E0[0], 2) + Math.Pow(E1[1] - E0[1], 2)), Dx = (E1[0] - E0[0]) / dl, Dy = (E1[1] - E0[1]) / dl, sg = (E1[2] - E0[2]) / dl;
var t0 = Tan(s0); int side = Math.Sign(Dx * -t0.ty + Dy * t0.tx); if (side == 0) side = 1;
double o0 = s0 - Wi / 2, o1 = s0 + Wi / 2, hi0 = o0, hi1 = o0 + L8;
if (hi1 + RAMP > S[^1]) { hi1 = o1; hi0 = o1 - L8; }
double[] Pt(double s, double off, double up) { var c = Pos(s); var t = Tan(s); return new[] { c[0] - t.ty * side * off, c[1] + t.tx * side * off, c[2] + Hd + up }; }
IEnumerable<double> St(double a, double b) { int n = Math.Max(1, (int)Math.Ceiling((b - a) / STEP)); return Enumerable.Range(0, n + 1).Select(i => a + (b - a) * i / n); }
var panels = new List<(List<double[]> p, bool ramp)>();
List<double[]> Band(double a, double b, Func<double, double> up) { var q = St(a, b).Select(s => Pt(s, -hw, up(s))).ToList(); q.AddRange(St(a, b).Reverse().Select(s => Pt(s, hw, up(s)))); return q; }
panels.Add((Band(hi0 - RAMP, hi0, s => RISE * (s - (hi0 - RAMP)) / RAMP), true));
panels.Add((Band(hi1, hi1 + RAMP, s => RISE * ((hi1 + RAMP) - s) / RAMP), true));
double[] Out_(double s, double d) { var w = Pt(s, hw, RISE); return new[] { w[0] + Dx * d, w[1] + Dy * d, w[2] }; }
var redP = St(hi0, hi1).Select(s => Pt(s, -hw, RISE)).ToList(); redP.Add(Pt(hi1, hw, RISE)); redP.Add(Out_(o1, LOBE)); redP.Add(Out_(o0, LOBE)); redP.Add(Pt(hi0, hw, RISE));
panels.Add((redP, false));
var w0 = Out_(o0, LOBE); var w1 = Out_(o1, LOBE); double low = E0[2] + sg * (LOBE + RAMP + pe.d) + Hs, drop = (w0[2] + w1[2]) / 2 - low;
panels.Add((new List<double[]> { w0, w1, new[] { w1[0] + Dx * RAMP, w1[1] + Dy * RAMP, w1[2] - drop }, new[] { w0[0] + Dx * RAMP, w0[1] + Dy * RAMP, w0[2] - drop } }, true));
Out.AppendLine($"truck loading strip at the RAD mouth: high section {hi0:F1}-{hi1:F1} m along the decline, {2 * hw + LOBE:F1} m wide, front ramp drops {drop:F2} m into the RAD");

// cables inside the strip -> strip surface
bool Inside(List<double[]> P, double x, double y) { bool c = false; for (int i = 0, j = P.Count - 1; i < P.Count; j = i++) if ((P[i][1] > y) != (P[j][1] > y) && x < (P[j][0] - P[i][0]) * (y - P[i][1]) / (P[j][1] - P[i][1]) + P[i][0]) c = !c; return c; }
double Idw(List<double[]> P, double x, double y) { var N = P.OrderBy(q => Math.Pow(q[0] - x, 2) + Math.Pow(q[1] - y, 2)).Take(4).ToList(); double ws = 0, zs = 0; foreach (var q in N) { double w = 1 / (1e-6 + Math.Pow(q[0] - x, 2) + Math.Pow(q[1] - y, 2)); ws += w; zs += w * q[2]; } return zs / ws; }
var cabMoves = new List<(Deswik.Graphics.Primaries.Figure f, double z)>();
foreach (var f in cablePts)
{
    var ip = f.asPoint.InsertionPoint; var hit = panels.Where(p => Inside(p.p, ip.x, ip.y)).Select(p => Idw(p.p, ip.x, ip.y)).OrderByDescending(z => z).ToList();
    if (hit.Count > 0 && Math.Abs(hit[0] - ip.z) > 0.02) cabMoves.Add((f, hit[0]));
}
Out.AppendLine($"cables moved onto the strip: {cabMoves.Count}");
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }

// ---- draw
Deswik.Graphics.Primaries.Figure Poly(Deswik.Graphics.Primaries.Figure src, List<double[]> p, object lay, bool closed)
{
    var c = (Deswik.Graphics.Primaries.Figure)src.Clone(); var pl = c.asPolyline;
    while (pl.VertexCount > 0) pl.RemoveVertexAt(0);
    foreach (var q in p) pl.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]);
    pl.Closed = closed; layProp.SetValue(c, lay); c.AttributeValueSet("CLAUDE_MPA07", "1"); App.ModelSpace.Entities.AddItem(c, null, null, true, false); return c;
}
foreach (var (p, name) in newWalls) Poly(walls[name], p, layW, false);
var filSrc = oldFil.Count > 0 ? oldFil[0] : red;
foreach (var (p, name) in fillets) { var f = Poly(filSrc, p, layS, true); f.AttributeValueSet("Name", name); }
foreach (var f in oldFil) f.Deleted = true;
Deswik.Graphics.Objects.Color Grey() { var g = new Deswik.Graphics.Objects.Color(); g.ColorIndex = 253; g.Transparency = 128; return g; }
foreach (var (p, isRamp) in panels)
{
    var f = Poly(isRamp ? grey : red, p, layS, true); f.AttributeValueSet("Name", "MAIN DEC"); f.AttributeValueSet("CLAUDE_DECLINE_STRIP", "1");
    if (isRamp) { var hp = f.asPolyline.HatchProperties; hp.FillBackColor = Grey(); hp.Solid2DTransparency = 128; f.asPolyline.HatchProperties = hp; }
}
var lab = (Deswik.Graphics.Primaries.Figure)labSrc.Clone(); var mt = lab.asMText;
mt.TextString = $"Stripping Truck Loading\r\n {Hd + RISE:0.0}H x {2 * hw + LOBE:0.0}W x {L8:0.0}L";
var lp = Pt((hi0 + hi1) / 2, -hw - 4.0, RISE); mt.InsertionPoint = new Deswik.Graphics.Geometry.Point(lp[0], lp[1], lp[2]);
layProp.SetValue(lab, layS); lab.AttributeValueSet("CLAUDE_MPA07", "1"); App.ModelSpace.Entities.AddItem(lab, null, null, true, false);
foreach (var (f, z) in cabMoves) { var ip = f.asPoint.InsertionPoint; f.asPoint.InsertionPoint = new Deswik.Graphics.Geometry.Point(ip.x, ip.y, z); }
App.Redraw(true);
Out.AppendLine($"drawn: {newWalls.Count} wall strings on DESIGN DRAFT\\WALLS, {fillets.Count} fillets (old {oldFil.Count} removed), {panels.Count} truck strip panels + label, {cabMoves.Count} cables lifted");
return null;
