// Single and double fan strip standards with 5 m transitions (0 -> 1.5 m above backs), cloned from the existing
// Double Fan Strip so colour, hatch and attributes match. Adds only; changes nothing existing.
const string src = "0\\REFERENCE\\STRIP STANDARDS";
const string dst = "0\\REFERENCE\\STRIP STANDARDS\\FAN STRIP TRANSITIONS";
double rise = 1.5, trans = 5.0, width = 5.5, v0 = 24.0, grade = 1.0 / 7.0;
double[] P1 = { 8041.17, 62914.04, 6427.80 }, u = { -0.4679, 0.8838 }, v = { 0.8850, 0.4656 };

Deswik.Graphics.Primaries.Figure tPoly = null, tText = null;
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Layer == null) continue;
    
    if (f.Layer.Name != src) continue;
    if (f.IsPolyline) { var g = f.asPolyline.GetVertex(0); if (Math.Abs(g.x - 8041.17) < 0.05 && Math.Abs(g.y - 62914.04) < 0.05) tPoly = f; }
    else if (f.IsMText && f.asMText.TextString.StartsWith("Double Fan Strip")) tText = f;
}
if (tPoly == null || tText == null) { Out.AppendLine("Template strip or label not found - nothing added."); return null; }

var layer = App.Layers.FirstOrDefault(l => l.Name.Equals(dst, StringComparison.OrdinalIgnoreCase)) ?? App.Layers.Add(dst, false, 1);

double[] W(double a, double b, double up) => new[] { P1[0] + a * u[0] + b * v[0], P1[1] + a * u[1] + b * v[1], P1[2] + a * grade + up };
int n = 0;
foreach (var (name, L, a0) in new[] { ("Single", 8.0, -24.0), ("Double", 15.0, 0.0) })
{
    double a1 = a0 + trans, a2 = a1 + L, a3 = a2 + trans, b0 = v0, b1 = v0 + width;
    var panels = new[] {
        new[] { W(a0, b0, 0), W(a1, b0, rise), W(a1, b1, rise), W(a0, b1, 0) },
        new[] { W(a1, b0, rise), W(a2, b0, rise), W(a2, b1, rise), W(a1, b1, rise) },
        new[] { W(a2, b0, rise), W(a3, b0, 0), W(a3, b1, 0), W(a2, b1, rise) } };
    foreach (var pts in panels)
    {
        var c = (Deswik.Graphics.Primaries.Figure)tPoly.Clone();
        var p = c.asPolyline;
        while (p.VertexCount > 0) p.RemoveVertexAt(0);
        foreach (var q in pts) p.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]);
        p.Closed = true;
        c.Layer = layer;
        App.ModelSpace.Entities.AddItem(c, null, null, true, false); n++;
    }
    var t = (Deswik.Graphics.Primaries.Figure)tText.Clone();
    t.asMText.TextString = $"{name} Fan Strip (1:7 up)\r\n{L:F1} mL x {width:F1}mW x {rise:F1}mH\r\n+ {trans:F1}m transition each end (0 to {rise:F1}mH)\r\nTotal {L + 2 * trans:F1}m";
    var ip = W(a3 + 1.8, b1 + 5.0, rise);
    t.asMText.InsertionPoint = new Deswik.Graphics.Geometry.Point(ip[0], ip[1], ip[2]);
    t.Layer = layer;
    App.ModelSpace.Entities.AddItem(t, null, null, true, false); n++;
}
App.Redraw(true);
Out.AppendLine($"Added {n} figures to {dst}");
return null;
