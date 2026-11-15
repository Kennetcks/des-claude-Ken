// Fill out the truck loading set: replace the duplicate 10 m 1:50 up strip (along -24, far wall 49) with an 8 m strip in
// the new design, and add 10 m strips at 1:7 down / 1:7 up in a clear row (far wall 215).
// 10 m design: 5.0 m opening centred, 2.5 m fillets both sides. 8 m design: opening flush with the start of the high
// section, one fillet to its end (3.0 x 2.5 m). Both 5.0 m wide, +1.5 m above backs, 5 m ramps at both ends and the front.
const string src = "0\\REFERENCE\\STRIP STANDARDS";
const string dst = "0\\REFERENCE\\STRIP STANDARDS\\TRUCK LOADING TRANSITIONS";
double rise = 1.5, trans = 5.0, W = 5.0, Wi = 5.0, fil = 2.5;
double[] P1 = { 8041.17, 62914.04, 6427.80 }, u = { -0.4679, 0.8838 }, v = { 0.8850, 0.4656 };
var strips = new (double L, double a0, double bw, double g, string gt)[] {
    ( 8.0, -24.0,  49.0,  1.0 / 50, "1:50 up"),
    (10.0, -24.0, 215.0, -1.0 / 7,  "1:7 down"),
    (10.0,   0.0, 215.0,  1.0 / 7,  "1:7 up") };
(double a, double b) AB(double x, double y) => ((x - P1[0]) * u[0] + (y - P1[1]) * u[1], (x - P1[0]) * v[0] + (y - P1[1]) * v[1]);

Deswik.Graphics.Primaries.Figure tPoly = null, tText = null; var old = new List<Deswik.Graphics.Primaries.Figure>(); int clash = 0;
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null) continue;
    double x = 0, y = 0; bool has = false;
    if (f.IsPolyline) { var q = f.asPolyline.GetVertex(0); x = q.x; y = q.y; has = true; } else if (f.IsMText) { var ip = f.asMText.InsertionPoint; x = ip.x; y = ip.y; has = true; }
    if (has && f.Layer.Name.StartsWith(src, StringComparison.OrdinalIgnoreCase))
    {
        var (a, b) = AB(x, y);
        if (f.Layer.Name == dst && b > 34 && b < 55 && a > -26 && a < -1)          // the duplicate 10 m 1:50 up strip and its label
        { if (f.IsPolyline || f.asMText.TextString.StartsWith("Truck Loading Strip (1:50 up)")) old.Add(f); }
        else if (b > 200 && b < 222 && a > -30 && a < 30) clash++;
    }
    if (f.Layer.Name != src) continue;
    if (f.IsPolyline) { var q = f.asPolyline.GetVertex(0); if (Math.Abs(q.x - 8057.00) < 0.05 && Math.Abs(q.y - 62896.95) < 0.05) tPoly = f; }
    else if (f.IsMText && f.asMText.TextString.StartsWith("Truck Loading Strip")) tText = f;
}
if (clash > 0) { Out.AppendLine($"{clash} existing points where the new 10 m 1:7 row goes - nothing changed."); return null; }
if (tPoly == null || tText == null) { Out.AppendLine("Template not found - nothing changed."); return null; }
if (old.Count != 8) { Out.AppendLine($"Expected 8 figures for the duplicate 10 m strip, found {old.Count} - nothing changed."); return null; }
foreach (var f in old) f.Deleted = true;
var layer = App.Layers.FirstOrDefault(l => l.Name.Equals(dst, StringComparison.OrdinalIgnoreCase)) ?? App.Layers.Add(dst, false, 1);

int n = 0;
foreach (var s in strips)
{
    double[] Pt(double a, double c, double up) { double aa = s.a0 + a, bb = s.bw - c; return new[] { P1[0] + aa * u[0] + bb * v[0], P1[1] + aa * u[1] + bb * v[1], P1[2] + aa * s.g + up }; }
    double h0 = trans, h1 = trans + s.L;
    bool flush = s.L < Wi + 2 * fil;                                           // 8 m: opening flush with the start
    double oL = flush ? h0 : (h0 + h1) / 2 - Wi / 2, oR = oL + Wi;
    var panels = new List<double[][]> {
        new[] { Pt(0, 0, 0), Pt(h0, 0, rise), Pt(h0, W, rise), Pt(0, W, 0) },
        new[] { Pt(h0, 0, rise), Pt(h1, 0, rise), Pt(h1, W, rise), Pt(h0, W, rise) },
        new[] { Pt(h1, 0, rise), Pt(h1 + trans, 0, 0), Pt(h1 + trans, W, 0), Pt(h1, W, rise) },
        new[] { Pt(oL, W, rise), Pt(oR, W, rise), Pt(oR, W + fil, rise), Pt(oL, W + fil, rise) },
        new[] { Pt(oL, W + fil, rise), Pt(oR, W + fil, rise), Pt(oR, W + fil + trans, 0), Pt(oL, W + fil + trans, 0) } };
    if (!flush) panels.Add(new[] { Pt(oL - fil, W, rise), Pt(oL, W, rise), Pt(oL, W + fil, rise) });
    panels.Add(new[] { Pt(oR, W, rise), Pt(flush ? h1 : oR + fil, W, rise), Pt(oR, W + fil, rise) });
    foreach (var pts in panels)
    {
        var c = (Deswik.Graphics.Primaries.Figure)tPoly.Clone(); var p = c.asPolyline;
        while (p.VertexCount > 0) p.RemoveVertexAt(0);
        foreach (var q in pts) p.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]);
        p.Closed = true; c.Layer = layer;
        App.ModelSpace.Entities.AddItem(c, null, null, true, false); n++;
    }
    string fillet = flush ? $"{h1 - oR:F1} x {fil:F1}m fillet one side" : $"{fil:F1}m fillets";
    var t = (Deswik.Graphics.Primaries.Figure)tText.Clone();
    t.asMText.TextString = $"Truck Loading Strip ({s.gt})\r\n{s.L:F1} mL x {W:F1}mW x {rise:F1}mH, {fillet}\r\n+ {trans:F1}m transition each end and front (0 to {rise:F1}mH)\r\nTotal {s.L + 2 * trans:F1}m";
    var ip = Pt(h1 + trans + 1.8, -5.0, rise);
    t.asMText.InsertionPoint = new Deswik.Graphics.Geometry.Point(ip[0], ip[1], ip[2]);
    t.Layer = layer; App.ModelSpace.Entities.AddItem(t, null, null, true, false); n++;
}
App.Redraw(true);
Out.AppendLine($"Removed {old.Count} (duplicate 10 m 1:50 up), added {n}");
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
    if (!f.Deleted && f.Layer != null && f.Layer.Name == dst && f.IsMText && f.asMText.TextString.StartsWith("Truck Loading Strip ("))
        Out.AppendLine("  " + f.asMText.TextString.Split('\r')[0] + " | " + f.asMText.TextString.Split('\n')[1].Trim());
return null;
