// Straight 8 m truck loading strips at 1:7 down / 1:7 up: 5.0 m wide drive part, 5.0 m opening flush with the start of
// the high section, one fillet (3.0 x 2.5 m) to its end, +1.5 m above backs, 3.5 m ramps at both ends and the front.
// Style from this layer: ramps cloned from a grey-hatched panel, the rest from a red one. Replaces the straight 8 m 1:7
// strips and labels in their slots (across 55-80 m); the decline-style strips elsewhere are not touched.
const string dst = "0\\REFERENCE\\STRIP STANDARDS\\TRUCK LOADING TRANSITIONS";
double rise = 1.5, trans = 3.5, W = 5.0, Wi = 5.0, fil = 2.5;
double[] P1 = { 8041.17, 62914.04, 6427.80 }, u = { -0.4679, 0.8838 }, v = { 0.8850, 0.4656 };
var strips = new (double L, double a0, double bw, double g, string gt)[] {
    ( 8.0, -24.0, 72.0, -1.0 / 7,  "1:7 down"),
    ( 8.0,   0.0, 72.0,  1.0 / 7,  "1:7 up") };
(double a, double b) AB(double x, double y) => ((x - P1[0]) * u[0] + (y - P1[1]) * u[1], (x - P1[0]) * v[0] + (y - P1[1]) * v[1]);
bool Straight17(string t) => (t.StartsWith("Truck Loading Strip (1:7 down)") || t.StartsWith("Truck Loading Strip (1:7 up)")) && t.Contains("8.0 mL");

Deswik.Graphics.Primaries.Figure tRamp = null, tHigh = null, tText = null; var old = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || f.Layer.Name != dst) continue;
    if (f.IsMText)
    {
        var t = f.asMText.TextString; var ip = f.asMText.InsertionPoint; var (a, b) = AB(ip.x, ip.y);
        if (Straight17(t) && b > 55 && b < 82 && a > -30 && a < 30) old.Add(f);
        if (t.StartsWith("Truck Loading Strip (1:50 up)")) tText ??= f;
        continue;
    }
    if (!f.IsPolyline) continue;
    var p = f.asPolyline; var q0 = p.GetVertex(0); var (pa, pb) = AB(q0.x, q0.y);
    if (pb > 55 && pb < 80 && pa > -30 && pa < 30) old.Add(f);
    else if (p.VertexCount == 4 && p.HatchProperties != null) { if (p.HatchProperties.FillBackColor.ToString() == "253") tRamp ??= f; else tHigh ??= f; }
}
if (tRamp == null || tHigh == null || tText == null) { Out.AppendLine("Style panels or label not found - nothing changed."); return null; }
foreach (var f in old) f.Deleted = true;
Out.AppendLine($"Removed {old.Count} figures from the straight 8 m 1:7 slots");

int n = 0;
foreach (var s in strips)
{
    double[] Pt(double a, double c, double up) { double aa = s.a0 + a, bb = s.bw - c; return new[] { P1[0] + aa * u[0] + bb * v[0], P1[1] + aa * u[1] + bb * v[1], P1[2] + aa * s.g + up }; }
    double h0 = trans, h1 = trans + s.L, oL = h0, oR = h0 + Wi, fR = h1;
    var panels = new List<(double[][] pts, bool ramp)> {
        (new[] { Pt(0, 0, 0), Pt(h0, 0, rise), Pt(h0, W, rise), Pt(0, W, 0) }, true),
        (new[] { Pt(h0, 0, rise), Pt(h1, 0, rise), Pt(h1, W, rise), Pt(h0, W, rise) }, false),
        (new[] { Pt(h1, 0, rise), Pt(h1 + trans, 0, 0), Pt(h1 + trans, W, 0), Pt(h1, W, rise) }, true),
        (new[] { Pt(oL, W, rise), Pt(oR, W, rise), Pt(oR, W + fil, rise), Pt(oL, W + fil, rise) }, false),
        (new[] { Pt(oR, W, rise), Pt(fR, W, rise), Pt(oR, W + fil, rise) }, false),
        (new[] { Pt(oL, W + fil, rise), Pt(oR, W + fil, rise), Pt(oR, W + fil + trans, 0), Pt(oL, W + fil + trans, 0) }, true) };
    foreach (var (pts, ramp) in panels)
    {
        var c = (Deswik.Graphics.Primaries.Figure)(ramp ? tRamp : tHigh).Clone(); var p = c.asPolyline;
        while (p.VertexCount > 0) p.RemoveVertexAt(0);
        foreach (var q in pts) p.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]);
        p.Closed = true; c.Layer = tRamp.Layer;
        App.ModelSpace.Entities.AddItem(c, null, null, true, false); n++;
    }
    var t = (Deswik.Graphics.Primaries.Figure)tText.Clone();
    t.asMText.TextString = $"Truck Loading Strip ({s.gt})\r\n{s.L:F1} mL x {W:F1}mW x {rise:F1}mH";
    var ip = Pt(h1 + trans + 1.8, -5.0, rise);
    t.asMText.InsertionPoint = new Deswik.Graphics.Geometry.Point(ip[0], ip[1], ip[2]);
    t.Layer = tText.Layer; App.ModelSpace.Entities.AddItem(t, null, null, true, false); n++;
}
App.Redraw(true);
Out.AppendLine($"Added {n} figures (2 strips x 6 panels + 2 labels)");
return null;
