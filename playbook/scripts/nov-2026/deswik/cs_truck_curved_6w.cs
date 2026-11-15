// Rebuild the two curved (decline-style) 8 m truck loading standards on an ideal decline: 25 m radius arc at 1:7 (down /
// up along +u), curving away from the stockpile, placed where each current curved strip is. Layout matches the straight
// standards: 5.0 m drive part (5.5 m decline less 0.25 m each side) + 1.0 m into the stockpile = 6.0 m high width, 8 m high
// section at +1.5 m, 5.0 m opening flush with the start, one fillet (1.0 m deep) to where the end ramp drops, 3.5 m ramps
// along the curve and a flat 3.5 m front ramp. Curved parts are ~1 m slices (near-flat, hatch fits); edges use a smoothed
// tangent. Style copied from the strip's own grey (ramps) and red panels; labels changed to 6.0mW.
const string trk = "0\\REFERENCE\\STRIP STANDARDS\\TRUCK LOADING TRANSITIONS";
double R = 25.0, Hd = 5.7, hw = 2.5, Wi = 5.0, fil = 1.0, L8 = 8.0, ramp = 3.5, rise = 1.5, step = 0.5;
double[] P1 = { 8041.17, 62914.04 }, u = { -0.4679, 0.8838 }, v = { 0.8850, 0.4656 };
(double a, double b) AB(double x, double y) => ((x - P1[0]) * u[0] + (y - P1[1]) * u[1], (x - P1[0]) * v[0] + (y - P1[1]) * v[1]);
string Sig(Deswik.Graphics.Primaries.Figure f) { var p = f.asPolyline; return string.Join(";", Enumerable.Range(0, p.VertexCount).Select(i => { var q = p.GetVertex(i); return $"{q.x:F2},{q.y:F2},{q.z:F2}"; })); }
var polys = new Dictionary<string, Deswik.Graphics.Primaries.Figure>(); var labels = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || f.Layer.Name != trk) continue;
    if (f.IsPolyline) polys[Sig(f)] = f; else if (f.IsMText) labels.Add(f);
}
var F = polys.Values.ToList(); var V = F.Select(f => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => f.asPolyline.GetVertex(i)).ToList()).ToList();
int[] par = Enumerable.Range(0, F.Count).ToArray();
int Find(int i) { while (par[i] != i) i = par[i] = par[par[i]]; return i; }
var byKey = new Dictionary<string, int>();
for (int i = 0; i < F.Count; i++) foreach (var q in V[i]) { string k = $"{q.x:F2},{q.y:F2}"; if (byKey.TryGetValue(k, out var j)) par[Find(i)] = Find(j); else byKey[k] = i; }
var curved = Enumerable.Range(0, F.Count).GroupBy(Find).Select(g => g.ToList()).Where(g => g.Any(i => V[i].Count > 6)).ToList();
if (curved.Count != 2) { Out.AppendLine($"Expected 2 curved strips, found {curved.Count} - nothing changed."); return null; }
int done = 0;
foreach (var grp in curved)
{
    var pts = grp.SelectMany(i => V[i]).ToList(); var ab = pts.Select(q => AB(q.x, q.y)).ToList();
    var lo = pts.OrderBy(q => AB(q.x, q.y).a).First(); var hi = pts.OrderBy(q => AB(q.x, q.y).a).Last();
    double g = hi.z < lo.z ? -1.0 / 7 : 1.0 / 7; string gt = g < 0 ? "1:7 down" : "1:7 up";
    var greyP = grp.Select(i => F[i]).FirstOrDefault(f => f.asPolyline.HatchProperties?.FillBackColor.ColorIndex == 253);
    var redP = grp.Select(i => F[i]).FirstOrDefault(f => f.asPolyline.HatchProperties != null && f.asPolyline.HatchProperties.FillBackColor.ColorIndex != 253);
    if (greyP == null || redP == null) { Out.AppendLine($"{gt}: style panels not found - skipped"); continue; }
    var redZ = grp.Where(i => F[i].asPolyline.HatchProperties?.FillBackColor.ColorIndex != 253).SelectMany(i => V[i]).Average(q => q.z);
    // ideal decline: junction J (opening centre) so the strip is centred on the old one; far wall stays where it was
    double am = (ab.Min(z => z.a) + ab.Max(z => z.a)) / 2, bFar = ab.Max(z => z.b);
    double s0 = 0, o0 = s0 - Wi / 2, o1 = s0 + Wi / 2, h0 = o0, h1 = o0 + L8;
    double aJ = am - (h0 + h1) / 2, bJ = bFar - hw - 0.3, zJ = redZ - rise - Hd;
    // centreline point / smoothed tangent at station s (arc curving towards +v, i.e. away from the stockpile on -v)
    double[] Pos(double s) { double th = s / R; double da = R * Math.Sin(th), db = R * (1 - Math.Cos(th)); return new[] { P1[0] + (aJ + da) * u[0] + (bJ + db) * v[0], P1[1] + (aJ + da) * u[1] + (bJ + db) * v[1], zJ + g * s }; }
    (double tx, double ty) Tan(double s) { var a = Pos(s - 1); var b = Pos(s + 1); double l = Math.Sqrt(Math.Pow(b[0] - a[0], 2) + Math.Pow(b[1] - a[1], 2)); return ((b[0] - a[0]) / l, (b[1] - a[1]) / l); }
    double[] Pt(double s, double off, double up) { var c = Pos(s); var t = Tan(s); return new[] { c[0] - t.ty * off, c[1] + t.tx * off, c[2] + Hd + up }; }   // +off = -v side (stockpile)
    IEnumerable<double> St(double a, double b) { int n = Math.Max(1, (int)Math.Ceiling((b - a) / step)); return Enumerable.Range(0, n + 1).Select(i => a + (b - a) * i / n); }
    var panels = new List<(List<double[]> p, bool r)>();
    void Slices(double a, double b, Func<double, double> up, bool isRamp)
    {
        var ss = St(a, b).ToList();
        for (int i = 0; i < ss.Count - 1; i += 2)
        {
            int j = Math.Min(i + 2, ss.Count - 1); var q = new List<double[]>();
            for (int k = i; k <= j; k++) q.Add(Pt(ss[k], -hw, up(ss[k])));
            for (int k = j; k >= i; k--) q.Add(Pt(ss[k], hw, up(ss[k])));
            panels.Add((q, isRamp));
        }
    }
    // one polyline per part: ramp up and ramp down as single bands (grey); the red high area is one outline below
    List<double[]> Band(double a, double b, Func<double, double> up) { var q = St(a, b).Select(s => Pt(s, -hw, up(s))).ToList(); q.AddRange(St(a, b).Reverse().Select(s => Pt(s, hw, up(s)))); return q; }
    panels.Add((Band(h0 - ramp, h0, s => rise * (s - (h0 - ramp)) / ramp), true)); panels.Add((Band(h1, h1 + ramp, s => rise * ((h1 + ramp) - s) / ramp), true));
    double Dx = -v[0], Dy = -v[1];                                                // stockpile direction
    double[] Out_(double s, double d) { var w = Pt(s, hw, rise); return new[] { w[0] + Dx * d, w[1] + Dy * d, w[2] }; }
    // red high area: far edge h0 -> h1 along the curve, fillet from the wall at h1 to the lobe corner, lobe edge, back to the wall at h0
    var red = St(h0, h1).Select(s => Pt(s, -hw, rise)).ToList(); red.Add(Pt(h1, hw, rise)); red.Add(Out_(o1, fil)); red.Add(Out_(o0, fil)); red.Add(Pt(h0, hw, rise));
    panels.Add((red, false));

    var w0 = Out_(o0, fil); var w1 = Out_(o1, fil);
    panels.Add((new List<double[]> { w0, w1, new[] { w1[0] + Dx * ramp, w1[1] + Dy * ramp, w1[2] - rise }, new[] { w0[0] + Dx * ramp, w0[1] + Dy * ramp, w0[2] - rise } }, true));
    foreach (var (p, r) in panels)
    {
        var c = (Deswik.Graphics.Primaries.Figure)(r ? greyP : redP).Clone(); var pl = c.asPolyline;
        while (pl.VertexCount > 0) pl.RemoveVertexAt(0);
        foreach (var q in p) pl.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]);
        pl.Closed = true; c.Layer = greyP.Layer; App.ModelSpace.Entities.AddItem(c, null, null, true, false);
    }
    foreach (int i in grp) F[i].Deleted = true;
    
    done++; Out.AppendLine($"{gt} decline: rebuilt on a {R} m radius arc, {panels.Count} panels (replaced {grp.Count})");
}
App.Redraw(true);
Out.AppendLine($"Rebuilt {done} curved strips");
return null;
