// Truck loading strips with a stockpile opening, fixed dimensions: 5.0 m wide drive part, 5.0 m opening centred on the
// high section, 2.5 m truck fillets, +1.5 m above backs, 5 m ramps at both ends and the front. Where a fillet starts
// outside the high section it sits on the ramp at the ramp height. Style cloned from the existing Truck Loading Strip.
const string src = "0\\REFERENCE\\STRIP STANDARDS";
const string dst = "0\\REFERENCE\\STRIP STANDARDS\\TRUCK LOADING TRANSITIONS";
double rise = 1.5, trans = 5.0, W = 5.0, Wi = 5.0, fil = 2.5;
double[] P1 = { 8041.17, 62914.04, 6427.80 }, u = { -0.4679, 0.8838 }, v = { 0.8850, 0.4656 };
var strips = new (double L, double a0, double bw, double g, string gt)[] {
    (10.0,   0.0, 49.0,  1.0 / 50, "1:50 up"),
    (10.0, -24.0, 49.0,  1.0 / 50, "1:50 up"),
    (10.0, -24.0, 72.0, -1.0 / 7,  "1:7 down"),
    (10.0,   0.0, 72.0,  1.0 / 7,  "1:7 up") };

Deswik.Graphics.Primaries.Figure tPoly = null, tText = null; int have = 0;
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null) continue;
    if (f.Layer.Name.Equals(dst, StringComparison.OrdinalIgnoreCase) && f.IsMText && f.asMText.TextString.StartsWith("Truck Loading Strip (")) have++;
    if (f.Layer.Name != src) continue;
    if (f.IsPolyline) { var q = f.asPolyline.GetVertex(0); if (Math.Abs(q.x - 8057.00) < 0.05 && Math.Abs(q.y - 62896.95) < 0.05) tPoly = f; }
    else if (f.IsMText && f.asMText.TextString.StartsWith("Truck Loading Strip")) tText = f;
}
if (have > 0) { int d = 0; double[] Q = { 8041.17, 62914.04 }; foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) { if (f.Deleted || f.Layer == null || f.Layer.Name != dst) continue; if (f.IsMText) { if (f.asMText.TextString.StartsWith("Truck Loading Strip (")) { f.Deleted = true; d++; } continue; } if (!f.IsPolyline) continue; var q = f.asPolyline.GetVertex(0); double b = (q.x - Q[0]) * v[0] + (q.y - Q[1]) * v[1]; if (b > 30 && b < 80) { f.Deleted = true; d++; } } Out.AppendLine($"Removed {d} figures of the old stockpile-opening set"); }
if (tPoly == null || tText == null) { Out.AppendLine("Template not found - nothing added."); return null; }
var layer = App.Layers.FirstOrDefault(l => l.Name.Equals(dst, StringComparison.OrdinalIgnoreCase)) ?? App.Layers.Add(dst, false, 1);

int n = 0;
foreach (var s in strips)
{
    double[] Pt(double a, double c, double up) { double aa = s.a0 + a, bb = s.bw - c; return new[] { P1[0] + aa * u[0] + bb * v[0], P1[1] + aa * u[1] + bb * v[1], P1[2] + aa * s.g + up }; }
    double h0 = trans, h1 = trans + s.L, am = (h0 + h1) / 2, oL = am - Wi / 2, oR = am + Wi / 2, fL = oL - fil, fR = oR + fil;
    double H(double a) => a <= 0 || a >= h1 + trans ? 0 : a < h0 ? rise * a / trans : a > h1 ? rise * (h1 + trans - a) / trans : rise;
    var panels = new List<double[][]> {
        new[] { Pt(0, 0, 0), Pt(h0, 0, rise), Pt(h0, W, rise), Pt(0, W, 0) },
        new[] { Pt(h0, 0, rise), Pt(h1, 0, rise), Pt(h1, W, rise), Pt(h0, W, rise) },
        new[] { Pt(h1, 0, rise), Pt(h1 + trans, 0, 0), Pt(h1 + trans, W, 0), Pt(h1, W, rise) },
        new[] { Pt(oL, W, rise), Pt(oR, W, rise), Pt(oR, W + fil, rise), Pt(oL, W + fil, rise) },
        new[] { Pt(fL, W, H(fL)), Pt(oL, W, rise), Pt(oL, W + fil, rise) },
        new[] { Pt(oR, W, rise), Pt(fR, W, H(fR)), Pt(oR, W + fil, rise) },
        new[] { Pt(oL, W + fil, rise), Pt(oR, W + fil, rise), Pt(oR, W + fil + trans, 0), Pt(oL, W + fil + trans, 0) } };
    foreach (var pts in panels)
    {
        var c = (Deswik.Graphics.Primaries.Figure)tPoly.Clone(); var p = c.asPolyline;
        while (p.VertexCount > 0) p.RemoveVertexAt(0);
        foreach (var q in pts) p.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]);
        p.Closed = true; c.Layer = layer;
        App.ModelSpace.Entities.AddItem(c, null, null, true, false); n++;
    }
    var t = (Deswik.Graphics.Primaries.Figure)tText.Clone();
    t.asMText.TextString = $"Truck Loading Strip ({s.gt})\r\n{s.L:F1} mL x {W:F1}mW x {rise:F1}mH, {fil:F1}m fillets\r\n+ {trans:F1}m transition each end and front (0 to {rise:F1}mH)\r\nTotal {s.L + 2 * trans:F1}m";
    var ip = Pt(h1 + trans + 1.8, -5.0, rise);
    t.asMText.InsertionPoint = new Deswik.Graphics.Geometry.Point(ip[0], ip[1], ip[2]);
    t.Layer = layer; App.ModelSpace.Entities.AddItem(t, null, null, true, false); n++;
}
App.Redraw(true);
Out.AppendLine($"Added {n} figures (4 strips x 7 panels + 4 labels)");
return null;
