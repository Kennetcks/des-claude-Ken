// Strip standards (Level Design Drafter): truck loading strip into an angled 4.5 m leg, 1:7 down decline. 8 m high
// section +1.5 m, 5.0 m drive part + 1.0 m lobe (6.0mW), opening flush with the start of the high section; the lobe and
// front ramp squared to the leg (leg width - 0.5 = 4.0 m across it, 3.5 m ramp along it). Placed in the free grid cell
// (third row, right column) on TRUCK LOADING TRANSITIONS, styled from the existing panels and label.
const bool APPLY = false;
const string trk = "0\\REFERENCE\\STRIP STANDARDS\\TRUCK LOADING TRANSITIONS";
const double LEG = 5.0, THETA = 90.0, W = 5.0, LOBE = 1.0, RISE = 1.5, RAMP = 3.5, L8 = 8.0, G = 1.0 / 7;
const double ACEN = 30.0, BW = 97.4, Z0 = 6429.80;   // grid cell centre along, far wall, start level
double[] P1 = { 8041.17, 62914.04 }, u = { -0.4679, 0.8838 }, v = { 0.8850, 0.4656 };
(double a, double b) AB(double x, double y) => ((x - P1[0]) * u[0] + (y - P1[1]) * u[1], (x - P1[0]) * v[0] + (y - P1[1]) * v[1]);
Deswik.Graphics.Primaries.Figure red = null, grey = null, lab = null; var clash = 0;
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || f.Layer.Name != trk) continue;
    var m = f.BoundingBox.Midpoint; var ab = AB(m.x, m.y);
    if (Math.Abs(ab.a - ACEN) < 10 && ab.b > BW - 12 && ab.b < BW + 6) clash++;
    if (f.IsMText && f.asMText.TextString.StartsWith("Truck Loading Strip (1:7 down)") && f.asMText.TextString.Contains("8.0 mL")) lab = f;
    else if (f.IsPolyline && f.asPolyline.HatchProperties != null) { if (f.asPolyline.HatchProperties.FillBackColor.ColorIndex == 253) grey ??= f; else red ??= f; }
}
Out.AppendLine($"styles red {red != null} grey {grey != null} label {lab != null}; objects already in the cell: {clash}");
if (red == null || grey == null || lab == null || clash > 0) { Out.AppendLine("cell not free or style missing - nothing changed"); return null; }
double aS = ACEN - (RAMP + L8 + RAMP) / 2, h0 = aS + RAMP, h1 = h0 + L8;
double[] Pt(double a, double c, double up) { double bb = BW - c; return new[] { P1[0] + a * u[0] + bb * v[0], P1[1] + a * u[1] + bb * v[1], Z0 - (a - aS) * G + up }; }
double th = THETA * Math.PI / 180, da = Math.Cos(th), dc = Math.Sin(th), na = -Math.Sin(th), nc = Math.Cos(th);   // leg direction / normal in (a, c)
double Wi = LEG - 0.5, o0 = h0, o1 = o0 + Wi / dc, aJ = (o0 + o1) / 2;
double ca = aJ + da * (LOBE / dc), cc = W + LOBE;                                                                // lobe centre on the leg centreline
var A_ = (a: ca + na * Wi / 2, c: cc + nc * Wi / 2); var B_ = (a: ca - na * Wi / 2, c: cc - nc * Wi / 2);
var panels = new List<(List<double[]> p, bool ramp)> {
    (new List<double[]> { Pt(aS, 0, 0), Pt(h0, 0, RISE), Pt(h0, W, RISE), Pt(aS, W, 0) }, true),
    (new List<double[]> { Pt(h1, 0, RISE), Pt(h1 + RAMP, 0, 0), Pt(h1 + RAMP, W, 0), Pt(h1, W, RISE) }, true),
    (new List<double[]> { Pt(h0, 0, RISE), Pt(h1, 0, RISE), Pt(h1, W, RISE), Pt(B_.a, B_.c, RISE), Pt(A_.a, A_.c, RISE), Pt(h0, W, RISE) }, false),
    (new List<double[]> { Pt(A_.a, A_.c, RISE), Pt(B_.a, B_.c, RISE), Pt(B_.a + da * RAMP, B_.c + dc * RAMP, 0), Pt(A_.a + da * RAMP, A_.c + dc * RAMP, 0) }, true) };
Out.AppendLine($"angled leg {LEG} m at {THETA} deg: opening {o0:F2}-{o1:F2} along the decline, lobe {Wi} m across the leg (corners c={A_.c:F2} and {B_.c:F2}), front ramp {RAMP} m along the leg");
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }
foreach (var (p, isRamp) in panels)
{
    var c = (Deswik.Graphics.Primaries.Figure)(isRamp ? grey : red).Clone(); var pl = c.asPolyline; while (pl.VertexCount > 0) pl.RemoveVertexAt(0);
    foreach (var q in p) pl.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]); pl.Closed = true; App.ModelSpace.Entities.AddItem(c, null, null, true, false);
}
// label: same offset from the far wall and the same style as the other standards' labels
var lm = lab.BoundingBox.Midpoint; var lab0 = AB(lm.x, lm.y); var l = (Deswik.Graphics.Primaries.Figure)lab.Clone(); var mt = l.asMText;
mt.TextString = $"Truck Loading Strip (1:7 down, {LEG:0.0}m leg)\r\n8.0 mL x 6.0mW x 1.5mH";
double da2 = ACEN - lab0.a, db2 = (BW + 3.03) - lab0.b; var ip = mt.InsertionPoint;
mt.InsertionPoint = new Deswik.Graphics.Geometry.Point(ip.x + da2 * u[0] + db2 * v[0], ip.y + da2 * u[1] + db2 * v[1], Z0 - (ACEN - aS) * G + RISE + 0.5);
App.ModelSpace.Entities.AddItem(l, null, null, true, false);
App.Redraw(true); Out.AppendLine("applied: 4 panels + label"); return null;
