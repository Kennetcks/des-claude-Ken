// Rebuild the straight 8 m truck loading standards (labels "8.0 mL", not "decline") with a 6.0 m wide high section (5.0 m drive + 1.0 m into the stockpile), in place: same start,
// same far wall, same grade and level. 8 m high section +1.5 m above backs, 5.0 m opening flush with the start, one fillet
// (2.5 m deep) from the opening to the end of the high section where the end ramp drops, 3.5 m ramps at both ends and the
// front. Panels are copies of the strip's own grey (ramps) and red panels; label width changed to 6.0mW.
const string trk = "0\\REFERENCE\\STRIP STANDARDS\\TRUCK LOADING TRANSITIONS";
double W = 5.0, L = 8.0, Wi = 5.0, fil = 1.0, ramp = 3.5, rise = 1.5;   // drive 5.0 m + 1.0 m into the stockpile = 6.0 m high width
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
var groups = Enumerable.Range(0, F.Count).GroupBy(Find).Select(g => g.ToList()).ToList();
int done = 0;
foreach (var lab in labels.Where(t => t.asMText.TextString.Contains("8.0 mL") && !t.asMText.TextString.Contains("decline")).ToList())
{
    var lm = lab.BoundingBox.Midpoint; var (la, lb) = AB(lm.x, lm.y);
    var grp = groups.OrderBy(g => { var ab = g.SelectMany(i => V[i]).Select(q => AB(q.x, q.y)).ToList(); return Math.Abs((ab.Min(z => z.a) + ab.Max(z => z.a)) / 2 - la) + Math.Abs(ab.Max(z => z.b) + 3.0 - lb); }).First();
    if (grp.Any(i => V[i].Count > 6)) { Out.AppendLine("matched a curved strip - skipped"); continue; }
    var pts = grp.SelectMany(i => V[i]).ToList(); var ab = pts.Select(q => AB(q.x, q.y)).ToList();
    double a0 = ab.Min(z => z.a), bw = ab.Max(z => z.b);
    string text = lab.asMText.TextString; double g = text.Contains("1:7 down") ? -1.0 / 7 : text.Contains("1:7 up") ? 1.0 / 7 : 1.0 / 50;
    // level: the start ramp's low far-wall corner sits on the backs line
    var start = pts.OrderBy(q => AB(q.x, q.y).a).ThenByDescending(q => AB(q.x, q.y).b).First(); double z0 = start.z - (AB(start.x, start.y).a - a0) * g;
    var greyP = grp.Select(i => F[i]).FirstOrDefault(f => f.asPolyline.HatchProperties?.FillBackColor.ColorIndex == 253);
    var redP = grp.Select(i => F[i]).FirstOrDefault(f => f.asPolyline.HatchProperties != null && f.asPolyline.HatchProperties.FillBackColor.ColorIndex != 253);
    if (greyP == null || redP == null) { Out.AppendLine($"'{text.Split('\r')[0]}': style panels not found - skipped"); continue; }
    double[] Pt(double a, double c, double up) { double aa = a0 + a, bb = bw - c; return new[] { P1[0] + aa * u[0] + bb * v[0], P1[1] + aa * u[1] + bb * v[1], z0 + a * g + up }; }
    double h0 = ramp, h1 = ramp + L, oL = h0, oR = h0 + Wi;
    var panels = new List<(double[][] p, bool r)> {
        (new[] { Pt(0, 0, 0), Pt(h0, 0, rise), Pt(h0, W, rise), Pt(0, W, 0) }, true),
        (new[] { Pt(h0, 0, rise), Pt(h1, 0, rise), Pt(h1, W, rise), Pt(oR, W + fil, rise), Pt(oL, W + fil, rise), Pt(h0, W, rise) }, false),   // red: drive part + lobe + fillet(s) as one outline
        (new[] { Pt(h1, 0, rise), Pt(h1 + ramp, 0, 0), Pt(h1 + ramp, W, 0), Pt(h1, W, rise) }, true),
        (new[] { Pt(oL, W + fil, rise), Pt(oR, W + fil, rise), Pt(oR, W + fil + ramp, 0), Pt(oL, W + fil + ramp, 0) }, true) };
    foreach (var (p, r) in panels)
    {
        var c = (Deswik.Graphics.Primaries.Figure)(r ? greyP : redP).Clone(); var pl = c.asPolyline;
        while (pl.VertexCount > 0) pl.RemoveVertexAt(0);
        foreach (var q in p) pl.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]);
        pl.Closed = true; c.Layer = greyP.Layer; App.ModelSpace.Entities.AddItem(c, null, null, true, false);
    }
    foreach (int i in grp) F[i].Deleted = true;
    lab.asMText.TextString = text.Replace("5.0mW", "6.0mW");
    done++; Out.AppendLine($"'{text.Split('\r')[0]}' rebuilt at {W} m wide ({grp.Count} old panels replaced)");
}
App.Redraw(true);
Out.AppendLine($"Rebuilt {done} strips");
return null;
