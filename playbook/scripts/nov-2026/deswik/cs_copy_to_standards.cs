// Copy one single fan strip, one double fan strip and one of each truck loading strip onto STRIP STANDARDS, in a new
// border band added beside the two existing ones (same size, on the -v side), laid out in one row along the strips.
// Each strip (panels + its label) is copied rigidly: same shape, grade and label placement.
const string std = "0\\REFERENCE\\STRIP STANDARDS";
string fanL = std + "\\FAN STRIP TRANSITIONS", trkL = std + "\\TRUCK LOADING TRANSITIONS";
double[] P1 = { 8041.17, 62914.04 }, u = { -0.4679, 0.8838 }, v = { 0.8850, 0.4656 };
(double a, double b) AB(double x, double y) => ((x - P1[0]) * u[0] + (y - P1[1]) * u[1], (x - P1[0]) * v[0] + (y - P1[1]) * v[1]);
var want = new (string lay, string head, string dims)[] {
    (fanL, "Single Fan Strip (1:7 up)", ""), (fanL, "Double Fan Strip (1:7 up)", ""),
    (trkL, "Truck Loading Strip (1:50 up)", "8.0 mL"), (trkL, "Truck Loading Strip (1:50 up)", "10.0 mL") };

// --- border: two existing rectangles on STRIP STANDARDS; the new one goes on the far side of the outer one
var rects = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (!f.Deleted && f.Layer != null && f.Layer.Name == std && f.IsRectangle) rects.Add(f);
if (rects.Count != 2) { Out.AppendLine($"Expected 2 border rectangles, found {rects.Count} - nothing changed."); return null; }
var r0 = rects.OrderBy(r => AB(r.asRectangle.InsertionPoint.x, r.asRectangle.InsertionPoint.y).b).ToList();   // r0[0] = outer on -v side
var i0 = r0[0].asRectangle.InsertionPoint; var i1 = r0[1].asRectangle.InsertionPoint;
double sx = i0.x - i1.x, sy = i0.y - i1.y;                                    // one band step towards -v
var b0 = r0[0].BoundingBox;
// band of the new rectangle in (a,b): use the outer rectangle's corners shifted one step
var corners = new[] { (b0.Min.x, b0.Min.y), (b0.Max.x, b0.Max.y), (b0.Min.x, b0.Max.y), (b0.Max.x, b0.Min.y) };
double rA0 = 1e9, rA1 = -1e9, rB0 = 1e9, rB1 = -1e9;
{   // exact band from the rectangle geometry: along = width direction, across = one step
    var ab0 = AB(i0.x, i0.y); var abS = AB(i0.x + sx, i0.y + sy);
    rB1 = ab0.b; rB0 = abS.b;                                                  // new band spans [rB0, rB1] across
    var w = r0[0].asRectangle.Width; double rot = r0[0].asRectangle.Rotation * Math.PI / 180;
    var e = AB(i0.x + w * Math.Cos(-rot), i0.y + w * Math.Sin(-rot));   // Rotation is stored clockwise
    rA0 = Math.Min(ab0.a, e.a); rA1 = Math.Max(ab0.a, e.a);
}
Out.AppendLine($"new band: along {rA0:F1}..{rA1:F1}, across {rB0:F1}..{rB1:F1}");

// --- source strips: cluster panels by shared vertices on each layer, match each wanted label to its strip
var picks = new List<(List<Deswik.Graphics.Primaries.Figure> figs, double aMin, double aMax, double bMax, string name)>();
foreach (var w in want)
{
    var polys = new List<Deswik.Graphics.Primaries.Figure>(); Deswik.Graphics.Primaries.Figure lab = null;
    foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
    {
        if (f.Deleted || f.Layer == null || f.Layer.Name != w.lay) continue;
        if (f.IsPolyline) polys.Add(f);
        else if (f.IsMText && lab == null) { var t = f.asMText.TextString; if (t.StartsWith(w.head) && (w.dims == "" || t.Contains(w.dims))) lab = f; }
    }
    if (lab == null) { Out.AppendLine($"Label '{w.head} {w.dims}' not found - nothing changed."); return null; }
    var P = polys.Select(f => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => f.asPolyline.GetVertex(i)).ToList()).ToList();
    int[] par = Enumerable.Range(0, P.Count).ToArray();
    int Find(int i) { while (par[i] != i) i = par[i] = par[par[i]]; return i; }
    var byKey = new Dictionary<string, int>();
    for (int i = 0; i < P.Count; i++) foreach (var q in P[i]) { string k = $"{q.x:F2},{q.y:F2}"; if (byKey.TryGetValue(k, out var j)) par[Find(i)] = Find(j); else byKey[k] = i; }
    var lm = lab.BoundingBox.Midpoint; var (la, lb) = AB(lm.x, lm.y);
    var grp = Enumerable.Range(0, P.Count).GroupBy(Find).Select(g => g.ToList()).OrderBy(g =>
    {
        var ab = g.SelectMany(i => P[i]).Select(q => AB(q.x, q.y)).ToList();
        double am = (ab.Min(z => z.a) + ab.Max(z => z.a)) / 2, bm = ab.Max(z => z.b);
        return Math.Abs(am - la) + Math.Abs(bm + 3.0 - lb);
    }).First();
    var abg = grp.SelectMany(i => P[i]).Select(q => AB(q.x, q.y)).ToList();
    var figs = grp.Select(i => polys[i]).ToList(); figs.Add(lab);
    picks.Add((figs, abg.Min(z => z.a), abg.Max(z => z.a), abg.Max(z => z.b), $"{w.head} {w.dims}".Trim()));
}

// --- layout: one row, reading order along -u (text direction), 3 m margin, 5 m gaps; label side towards +v
double gap = 5.0, margin = 3.0, wall = rB1 - 5.5;                           // far wall 5.5 m inside the band's +v edge
double next = rA1 - margin;                                                  // start at the high-along end
double need = picks.Sum(p => p.aMax - p.aMin) + gap * (picks.Count - 1) + 2 * margin;
if (need > rA1 - rA0) { Out.AppendLine($"Row needs {need:F1} m but the band is {rA1 - rA0:F1} m - nothing changed."); return null; }
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)    // nothing of the standards already in the new band
{
    if (f.Deleted || f.Layer == null || !f.Layer.Name.StartsWith(std) || f.IsRectangle) continue;
    var m = f.BoundingBox.Midpoint; var (a, b) = AB(m.x, m.y);
    if (a > rA0 && a < rA1 && b > rB0 && b < rB1) { Out.AppendLine($"Something on {f.Layer.Name} already sits in the new band - nothing changed."); return null; }
}
var layer = App.Layers.First(l => l.Name == std);
foreach (var p in picks)
{
    double da = (next - p.aMax), db = (wall - p.bMax);
    double dx = da * u[0] + db * v[0], dy = da * u[1] + db * v[1];
    var srcZ = p.figs.Where(f => f.IsPolyline).SelectMany(f => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => f.asPolyline.GetVertex(i).z)).Min();
    double dz = 6429.80 - srcZ;                                              // sit at the sheet's working level
    foreach (var f in p.figs)
    {
        var c = (Deswik.Graphics.Primaries.Figure)f.Clone();
        if (c.IsPolyline)
        {
            var pl = c.asPolyline; var pts = Enumerable.Range(0, pl.VertexCount).Select(i => pl.GetVertex(i)).Select(q => new[] { q.x + dx, q.y + dy, q.z + dz }).ToList();
            bool closed = pl.Closed; while (pl.VertexCount > 0) pl.RemoveVertexAt(0);
            foreach (var q in pts) pl.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]);
            pl.Closed = closed;
        }
        else { var ip = c.asMText.InsertionPoint; c.asMText.InsertionPoint = new Deswik.Graphics.Geometry.Point(ip.x + dx, ip.y + dy, ip.z + dz); }
        c.Layer = layer; App.ModelSpace.Entities.AddItem(c, null, null, true, false);
    }
    Out.AppendLine($"copied {p.name} ({p.figs.Count - 1} panels) to along {next - (p.aMax - p.aMin):F1}..{next:F1}");
    next -= (p.aMax - p.aMin) + gap;
}
// --- border: copy the outer rectangle one band further out
var nr = (Deswik.Graphics.Primaries.Figure)r0[0].Clone();
nr.asRectangle.InsertionPoint = new Deswik.Graphics.Geometry.Point(i0.x + sx, i0.y + sy, i0.z);
nr.Layer = layer; App.ModelSpace.Entities.AddItem(nr, null, null, true, false);
App.Redraw(true);
Out.AppendLine("added border band; row uses " + need.ToString("F1") + " m of " + (rA1 - rA0).ToString("F1") + " m");
return null;
