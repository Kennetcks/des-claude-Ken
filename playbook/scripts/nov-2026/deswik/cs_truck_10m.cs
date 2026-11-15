// Rebuild the 10.0 m truck loading strip (1:50 up) with fixed dimensions: 5.0 m wide drive part, 5.0 m stockpile
// opening centred on the high section, 3.0 m truck fillets, +1.5 m above backs, 5 m ramps at both ends and the front.
// Replaces only the existing 10 m strip (panels in its footprint and its label). Style cloned from the existing panels.
const string dst = "0\\REFERENCE\\STRIP STANDARDS\\TRUCK LOADING TRANSITIONS";
double rise = 1.5, trans = 5.0, L = 10.0, W = 5.0, Wi = 5.0, fil = 2.5, g = 1.0 / 50;
double a0 = 0.0, bw = 49.0;                                    // strip start along, far wall across (same spot as before)
double[] P1 = { 8041.17, 62914.04, 6427.80 }, u = { -0.4679, 0.8838 }, v = { 0.8850, 0.4656 };
double[] Pt(double a, double c, double up) { double aa = a0 + a, bb = bw - c; return new[] { P1[0] + aa * u[0] + bb * v[0], P1[1] + aa * u[1] + bb * v[1], P1[2] + aa * g + up }; }

Deswik.Graphics.Primaries.Figure tPoly = null, tText = null; var old = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || f.Layer.Name != dst) continue;
    double x, y;
    if (f.IsPolyline) { var q = f.asPolyline.GetVertex(0); x = q.x; y = q.y; }
    else if (f.IsMText) { var ip = f.asMText.InsertionPoint; x = ip.x; y = ip.y; }
    else continue;
    double a = (x - P1[0]) * u[0] + (y - P1[1]) * u[1], b = (x - P1[0]) * v[0] + (y - P1[1]) * v[1];
    if (f.IsMText && f.asMText.TextString.StartsWith("Truck Loading Strip (1:50 up)") && f.asMText.TextString.Contains("10.0 mL")) { tText = f; old.Add(f); }
    else if (f.IsPolyline && b > 30 && b < 50 && a > -1 && a < 27) { tPoly ??= f; old.Add(f); }
}
if (tPoly == null || tText == null) { Out.AppendLine("Existing 10 m strip not found - nothing changed."); return null; }

double h0 = trans, h1 = trans + L, am = (h0 + h1) / 2, oL = am - Wi / 2, oR = am + Wi / 2, fL = oL - fil, fR = oR + fil;
double H(double a) => a <= 0 || a >= h1 + trans ? 0 : a < h0 ? rise * a / trans : a > h1 ? rise * (h1 + trans - a) / trans : rise;  // height above backs along the drive
var panels = new List<double[][]> {
    new[] { Pt(0, 0, 0), Pt(h0, 0, rise), Pt(h0, W, rise), Pt(0, W, 0) },                              // start ramp
    new[] { Pt(h0, 0, rise), Pt(h1, 0, rise), Pt(h1, W, rise), Pt(h0, W, rise) },                       // high section, drive part
    new[] { Pt(h1, 0, rise), Pt(h1 + trans, 0, 0), Pt(h1 + trans, W, 0), Pt(h1, W, rise) },            // end ramp
    new[] { Pt(oL, W, rise), Pt(oR, W, rise), Pt(oR, W + fil, rise), Pt(oL, W + fil, rise) },           // stockpile opening, high
    new[] { Pt(fL, W, H(fL)), Pt(oL, W, rise), Pt(oL, W + fil, rise) },                                 // fillet, start side
    new[] { Pt(oR, W, rise), Pt(fR, W, H(fR)), Pt(oR, W + fil, rise) },                                 // fillet, end side
    new[] { Pt(oL, W + fil, rise), Pt(oR, W + fil, rise), Pt(oR, W + fil + trans, 0), Pt(oL, W + fil + trans, 0) } };  // front ramp
var layer = tPoly.Layer;
var ip0 = tText.asMText.InsertionPoint;
foreach (var pts in panels)
{
    var c = (Deswik.Graphics.Primaries.Figure)tPoly.Clone(); var p = c.asPolyline;
    while (p.VertexCount > 0) p.RemoveVertexAt(0);
    foreach (var q in pts) p.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]);
    p.Closed = true; c.Layer = layer;
    App.ModelSpace.Entities.AddItem(c, null, null, true, false);
}
var t = (Deswik.Graphics.Primaries.Figure)tText.Clone();
t.asMText.TextString = $"Truck Loading Strip (1:50 up)\r\n{L:F1} mL x {W:F1}mW x {rise:F1}mH, {fil:F1}m fillets\r\n+ {trans:F1}m transition each end and front (0 to {rise:F1}mH)\r\nTotal {L + 2 * trans:F1}m";
t.Layer = layer; App.ModelSpace.Entities.AddItem(t, null, null, true, false);
foreach (var f in old) f.Deleted = true;
App.Redraw(true);
Out.AppendLine($"Replaced {old.Count} old figures with {panels.Count} panels + 1 label");
Out.AppendLine($"fillets start {h0 - fL:F2} m into the start ramp side / {fR - h1:F2} m past the end of the high section (0 = inside)");
return null;
