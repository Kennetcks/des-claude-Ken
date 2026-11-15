// Second truck loading standard ("Stripping Truck Loading 7.2H x 5.5W x 8.0L", 5-point outline with a chamfered corner):
// its outline becomes the high section (+1.5 m above backs) with 5 m ramps at both ends and on the chamfered (front) side.
// Adds a new row on TRUCK LOADING TRANSITIONS; does not touch what is already there.
const string src = "0\\REFERENCE\\STRIP STANDARDS";
const string dst = "0\\REFERENCE\\STRIP STANDARDS\\TRUCK LOADING TRANSITIONS";
double rise = 1.5, trans = 5.0;
double[] P1 = { 8041.17, 62914.04, 6427.80 }, u = { -0.4679, 0.8838 }, v = { 0.8850, 0.4656 };
var strips = new (double a0, double bw, double g, string gt)[] {
    (-24.0, 185.0, -1.0 / 7,  "1:7 down"),
    (  0.0, 185.0,  1.0 / 7,  "1:7 up"),
    ( 24.0, 185.0,  1.0 / 50, "1:50 up") };

Deswik.Graphics.Primaries.Figure tPoly = null, tText = null; int clash = 0, have = 0;
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null) continue;
    if (f.Layer.Name.Equals(dst, StringComparison.OrdinalIgnoreCase) && f.IsMText && f.asMText.TextString.Contains("chamfer other end")) have++;
    if (f.Layer.Name.StartsWith(src, StringComparison.OrdinalIgnoreCase))
    {
        var pts = new List<(double x, double y)>();
        if (f.IsPolyline) { var p = f.asPolyline; for (int i = 0; i < p.VertexCount; i++) { var q = p.GetVertex(i); pts.Add((q.x, q.y)); } }
        else if (f.IsMText) { var ip = f.asMText.InsertionPoint; pts.Add((ip.x, ip.y)); }
        foreach (var (x, y) in pts) { double a = (x - P1[0]) * u[0] + (y - P1[1]) * u[1], b = (x - P1[0]) * v[0] + (y - P1[1]) * v[1]; if (b > 168 && b < 200 && a > -32 && a < 45) clash++; }
    }
    if (f.Layer.Name != src) continue;
    if (f.IsPolyline) { var g = f.asPolyline.GetVertex(0); if (Math.Abs(g.x - 8070.51) < 0.05 && Math.Abs(g.y - 62874.37) < 0.05) tPoly = f; }
    else if (f.IsMText && f.asMText.TextString.StartsWith("Stripping Truck Loading")) tText = f;
}
if (have > 0) { Out.AppendLine("These strips are already on the layer - nothing added."); return null; }
if (clash > 0) { Out.AppendLine($"{clash} existing points in the target area - nothing added."); return null; }
if (tPoly == null || tText == null || tPoly.asPolyline.VertexCount != 5) { Out.AppendLine("Template not found - nothing added."); return null; }

// template frame: origin vertex 0, along vertex 0 -> 4 (straight wall), across towards the chamfered wall
var tp = tPoly.asPolyline; var V = Enumerable.Range(0, 5).Select(i => tp.GetVertex(i)).ToArray();
double ex = V[4].x - V[0].x, ey = V[4].y - V[0].y, eL = Math.Sqrt(ex * ex + ey * ey); ex /= eL; ey /= eL;
double nx = -ey, ny = ex; if ((V[2].x - V[0].x) * nx + (V[2].y - V[0].y) * ny < 0) { nx = -nx; ny = -ny; }
var loc = V.Select(q => (al: 7.98 - ((q.x - V[0].x) * ex + (q.y - V[0].y) * ey), ac: (q.x - V[0].x) * nx + (q.y - V[0].y) * ny)).ToArray();
Out.AppendLine("template (along, across): " + string.Join(" | ", loc.Select(p => $"{p.al:F2},{p.ac:F2}")));

var layer = App.Layers.FirstOrDefault(l => l.Name.Equals(dst, StringComparison.OrdinalIgnoreCase)) ?? App.Layers.Add(dst, false, 1);
int n = 0;
foreach (var s in strips)
{
    double[] W(double a, double c, double up) { double aa = s.a0 + a, bb = s.bw - c; return new[] { P1[0] + aa * u[0] + bb * v[0], P1[1] + aa * u[1] + bb * v[1], P1[2] + aa * s.g + up }; }
    var high = loc.Select(p => W(trans + p.al, p.ac, rise)).ToArray();
    double[] Low(int i, double da, double dc) => W(trans + loc[i].al + da, loc[i].ac + dc, 0);
    double RW = 5.0;   // end ramp width from the straight wall
    double D0 = 1.0, D4 = -1.0;   // ramp direction at template vertex 0 and vertex 4 ends
    var panels = new List<double[][]> {
        high,                                                                    // high section (template outline)
        new[] { W(trans + loc[0].al, 0, rise), W(trans + loc[0].al, RW, rise), W(trans + loc[0].al + D0 * trans, RW, 0), W(trans + loc[0].al + D0 * trans, 0, 0) },   // end ramp, RW wide from the straight wall
        new[] { W(trans + loc[4].al, 0, rise), W(trans + loc[4].al, RW, rise), W(trans + loc[4].al + D4 * trans, RW, 0), W(trans + loc[4].al + D4 * trans, 0, 0) },   // end ramp, RW wide from the straight wall
        new[] { W(trans + loc[3].al, loc[3].ac, rise), W(trans + loc[3].al + Math.Sign(loc[2].al - loc[3].al) * 5.0, loc[3].ac, rise), W(trans + loc[3].al + Math.Sign(loc[2].al - loc[3].al) * 5.0, loc[3].ac + trans, 0), W(trans + loc[3].al, loc[3].ac + trans, 0) } };   // front ramp, 5.0 m wide from the square end
    foreach (var pts in panels)
    {
        var c = (Deswik.Graphics.Primaries.Figure)tPoly.Clone(); var p = c.asPolyline;
        while (p.VertexCount > 0) p.RemoveVertexAt(0);
        foreach (var q in pts) p.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]);
        p.Closed = true; c.Layer = layer;
        App.ModelSpace.Entities.AddItem(c, null, null, true, false); n++;
    }
    var t = (Deswik.Graphics.Primaries.Figure)tText.Clone();
    t.asMText.TextString = $"Stripping Truck Loading ({s.gt}, chamfer other end)\r\n8.0L x 6.0W, backs +{rise:F1}m\r\n+ {trans:F1}m transition each end and front (0 to {rise:F1}m)\r\nTotal 18.0m";
    var ip = W(trans + 7.98 + trans + 1.8, -5.0, rise);
    t.asMText.InsertionPoint = new Deswik.Graphics.Geometry.Point(ip[0], ip[1], ip[2]);
    t.Layer = layer; App.ModelSpace.Entities.AddItem(t, null, null, true, false); n++;
}
App.Redraw(true);
Out.AppendLine($"Added {n} figures");
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || f.Layer.Name != dst) continue;
    if (f.IsMText && f.asMText.TextString.Contains("chamfer other end")) Out.AppendLine("label: " + f.asMText.TextString.Split('\r')[0]);
}
return null;
