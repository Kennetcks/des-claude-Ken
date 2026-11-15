// Build a truck loading strip on a real decline: select the decline centreline and the stockpile centreline, then run.
// The strip follows the decline's curve and grade: 5.0 m wide (drive width - 0.5) centred on the decline, high section
// +1.5 m above the decline backs (centreline + profile height), 3.5 m ramps at each end measured along the curve, and on
// the stockpile side a 5.0 m opening, 2.5 m deep fillets and a 3.5 m front ramp down to the stockpile backs.
// DESIGN: "10" = 10 m high section, opening centred, 2.5 m fillets both sides.
//         "8"  = 8 m high section, opening flush with one end, one 3.0 x 2.5 m fillet (FILLET_SIDE +1 = up-station side).
const string DESIGN = "8"; const int FILLET_SIDE = 1;
const string OUT_LAYER = "0\\DESIGN DRAFT\\STRIPPING";
const string STYLE_LAYER = "0\\REFERENCE\\STRIP STANDARDS\\TRUCK LOADING TRANSITIONS";
double rise = 1.5, ramp = 3.5, fil = 2.5, step = 0.5;

var sel = App.Selections.MainSelection.Cast<Deswik.Graphics.Primaries.Figure>().Where(f => f.IsPolyline && !(f.Layer?.Name ?? "").Contains("\\WALLS")).ToList();
if (sel.Count != 2) { Out.AppendLine($"Select exactly 2 strings (decline and stockpile) - {sel.Count} selected. Nothing changed."); return null; }
var dec = sel.FirstOrDefault(f => (f.AttributeValueGet("ACT_TYPE", "") ?? "").ToString() == "DEC") ?? sel.OrderByDescending(f => f.asPolyline.Length).First();
var stk = sel.First(f => f != dec);
double Prof(Deswik.Graphics.Primaries.Figure f, string unit, double def)
{
    var m = System.Text.RegularExpressions.Regex.Match((f.AttributeValueGet("PROFILE", "") ?? "").ToString(), @"([\d.]+)\s*m" + unit);
    return m.Success ? double.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : def;
}
double Wd = Prof(dec, "W", 5.5), Hd = Prof(dec, "H", 5.7), Ws = Prof(stk, "W", 5.5), Hs = Prof(stk, "H", 5.7);
double hw = Wd / 2 - 0.25, Wi = Ws - 0.5;

// decline as stations
var D = Enumerable.Range(0, dec.asPolyline.VertexCount).Select(i => dec.asPolyline.GetVertex(i)).ToList();
var S = new List<double> { 0 }; for (int i = 1; i < D.Count; i++) S.Add(S[i - 1] + Math.Sqrt(Math.Pow(D[i].x - D[i - 1].x, 2) + Math.Pow(D[i].y - D[i - 1].y, 2)));
(double x, double y, double z, double tx, double ty) At(double s)
{
    int i = 1; while (i < D.Count - 1 && S[i] < s) i++;
    double L = S[i] - S[i - 1], t = L > 0 ? (s - S[i - 1]) / L : 0, tx = (D[i].x - D[i - 1].x) / L, ty = (D[i].y - D[i - 1].y) / L;
    return (D[i - 1].x + t * (D[i].x - D[i - 1].x), D[i - 1].y + t * (D[i].y - D[i - 1].y), D[i - 1].z + t * (D[i].z - D[i - 1].z), tx, ty);
}
// stockpile: end nearest the decline, its station on the decline, and outward direction
var K = Enumerable.Range(0, stk.asPolyline.VertexCount).Select(i => stk.asPolyline.GetVertex(i)).ToList();
(double s, double d) Proj(double x, double y)
{
    double bs = 0, bd = 1e9;
    for (int i = 1; i < D.Count; i++)
    {
        double ax = D[i - 1].x, ay = D[i - 1].y, bx = D[i].x - ax, by = D[i].y - ay, L2 = bx * bx + by * by;
        double t = L2 > 0 ? Math.Max(0, Math.Min(1, ((x - ax) * bx + (y - ay) * by) / L2)) : 0;
        double d = Math.Sqrt(Math.Pow(ax + t * bx - x, 2) + Math.Pow(ay + t * by - y, 2));
        if (d < bd) { bd = d; bs = S[i - 1] + t * Math.Sqrt(L2); }
    }
    return (bs, bd);
}
var pA = Proj(K[0].x, K[0].y); var pB = Proj(K[^1].x, K[^1].y);
bool fromStart = pA.d <= pB.d; var E = fromStart ? K[0] : K[^1]; var E2 = fromStart ? K[1] : K[^2];
double s0 = fromStart ? pA.s : pB.s, ed = Math.Min(pA.d, pB.d);
double dlen = Math.Sqrt(Math.Pow(E2.x - E.x, 2) + Math.Pow(E2.y - E.y, 2)), Dx = (E2.x - E.x) / dlen, Dy = (E2.y - E.y) / dlen;
double sGrade = (E2.z - E.z) / dlen;                                          // stockpile grade from its first segment
var c0 = At(s0); int side = Math.Sign(Dx * -c0.ty + Dy * c0.tx); if (side == 0) side = 1;

// layout along the decline
double h0, h1, o0 = s0 - Wi / 2, o1 = s0 + Wi / 2; var fillets = new List<(double a, double b)>();
if (DESIGN == "10") { h0 = s0 - 5.0; h1 = s0 + 5.0; fillets.Add((o0 - fil, o0)); fillets.Add((o1, o1 + fil)); }
else if (FILLET_SIDE > 0) { h0 = o0; h1 = o0 + 8.0; fillets.Add((o1, h1)); }
else { h1 = o1; h0 = o1 - 8.0; fillets.Add((h0, o0)); }
if (h0 - ramp < S[0] || h1 + ramp > S[^1]) { Out.AppendLine("The strip runs past the end of the decline string - nothing changed."); return null; }

double[] Pt(double s, double off, double up) { var c = At(s); double nx = -c.ty * side, ny = c.tx * side; return new[] { c.x + nx * off, c.y + ny * off, c.z + Hd + up }; }
IEnumerable<double> Stations(double a, double b) { int n = Math.Max(1, (int)Math.Ceiling((b - a) / step)); return Enumerable.Range(0, n + 1).Select(i => a + (b - a) * i / n); }
double Up(double s) => s < h0 ? rise * (s - (h0 - ramp)) / ramp : s > h1 ? rise * ((h1 + ramp) - s) / ramp : rise;
List<double[]> Band(double a, double b) { var L = Stations(a, b).Select(s => Pt(s, -hw, Up(s))).ToList(); L.AddRange(Stations(a, b).Reverse().Select(s => Pt(s, hw, Up(s)))); return L; }
double[] Out_(double s, double d, double up) { var w = Pt(s, hw, rise); return new[] { w[0] + Dx * d, w[1] + Dy * d, w[2] - rise + up }; }
double StkBacks(double d) => E.z + sGrade * (d + ed) + Hs;                     // stockpile backs at distance d past the strip wall

var panels = new List<(List<double[]> pts, string what)> {
    (Band(h0 - ramp, h0), "ramp"), (Band(h0, h1), "high section"), (Band(h1, h1 + ramp), "ramp") };
var open = Stations(o0, o1).Select(s => Pt(s, hw, rise)).ToList(); open.Add(Out_(o1, fil, rise)); open.Add(Out_(o0, fil, rise));
panels.Add((open, "opening"));
foreach (var (a, b) in fillets)
{
    var fp = Stations(a, b).Select(s => Pt(s, hw, rise)).ToList();
    fp.Add(Out_(a == o1 ? o1 : o0, fil, rise));                                 // corner of the opening on that side
    panels.Add((fp, "fillet"));
}
var w0 = Out_(o0, fil, rise); var w1 = Out_(o1, fil, rise);
double dz = StkBacks(fil + ramp);
panels.Add((new List<double[]> { w0, w1, new[] { w1[0] + Dx * ramp, w1[1] + Dy * ramp, dz }, new[] { w0[0] + Dx * ramp, w0[1] + Dy * ramp, dz } }, "front ramp"));

// style from the truck standards, attributes from the decline
Deswik.Graphics.Primaries.Figure tplRamp = null, tplHigh = null;   // your scheme: ramps hatched grey (253), the rest red
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || f.Layer.Name != STYLE_LAYER || !f.IsPolyline || f.asPolyline.HatchProperties == null) continue;
    if (f.asPolyline.HatchProperties.FillBackColor.ToString() == "253") tplRamp ??= f; else tplHigh ??= f;
}
if (tplRamp == null || tplHigh == null) { Out.AppendLine("Grey and red style panels not both found on the truck standards layer - nothing changed."); return null; }
var layer = App.Layers.FirstOrDefault(l => l.Name == OUT_LAYER) ?? App.Layers.Add(OUT_LAYER, false, 1);
// replace a strip this tool built earlier at this junction (panels tagged CLAUDE_DECLINE_STRIP, wholly within 16 m)
var cj = Pt(s0, 0, 0); int removed = 0;
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || f.Layer.Name != OUT_LAYER || !f.IsPolyline || (f.AttributeValueGet("CLAUDE_DECLINE_STRIP", "") ?? "").ToString() != "1") continue;
    var pp = f.asPolyline; bool near = true;
    for (int i = 0; i < pp.VertexCount; i++) { var q = pp.GetVertex(i); if (Math.Sqrt(Math.Pow(q.x - cj[0], 2) + Math.Pow(q.y - cj[1], 2)) > 16) { near = false; break; } }
    if (near) { f.Deleted = true; removed++; }
}
Out.AppendLine($"Removed {removed} panels of the strip already at this junction");
string[] copy = { "LEVEL", "MINE", "ZONE", "LODE", "DESIGN", "Design Status", "MPA", "DIRECTION", "GSS", "Name" };
foreach (var (pts, what) in panels)
{
    var c = (Deswik.Graphics.Primaries.Figure)(what.Contains("ramp") ? tplRamp : tplHigh).Clone(); var p = c.asPolyline;
    while (p.VertexCount > 0) p.RemoveVertexAt(0);
    foreach (var q in pts) p.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]);
    p.Closed = true; c.Layer = layer;
    foreach (var a in copy) { var val = dec.AttributeValueGet(a, null); if (val != null && val.ToString() != "") c.AttributeValueSet(a, val); }
    c.AttributeValueSet("CLAUDE_DECLINE_STRIP", "1");
    App.ModelSpace.Entities.AddItem(c, null, null, true, false);
}
App.Redraw(true);
var cs = At(s0);
Out.AppendLine($"Built {DESIGN} m strip on {dec.AttributeValueGet("Name", "decline")} at station {s0:F1} m (stockpile {ed:F2} m off the centreline, {(side > 0 ? "left" : "right")} side)");
Out.AppendLine($"decline {Wd}W x {Hd}H, stockpile {Ws}W x {Hs}H; grade here 1:{Math.Abs(1 / ((At(h1 + ramp).z - At(h0 - ramp).z) / (h1 - h0 + 2 * ramp))):F1}; {panels.Count} panels on {OUT_LAYER}");
return null;
