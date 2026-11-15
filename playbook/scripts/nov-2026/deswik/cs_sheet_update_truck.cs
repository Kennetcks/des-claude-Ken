// Update the two truck loading strips on the STRIP STANDARDS sheet (the 8 m and 10 m 1:50 up copies in the new band,
// across < -24.2 m) to the current design on TRUCK LOADING TRANSITIONS: copy each source strip rigidly onto the old one's
// spot (same along-centre and far wall, sheet level), remove the old panels and update the label text.
const string std = "0\\REFERENCE\\STRIP STANDARDS", trk = std + "\\TRUCK LOADING TRANSITIONS";
double[] P1 = { 8041.17, 62914.04 }, u = { -0.4679, 0.8838 }, v = { 0.8850, 0.4656 };
(double a, double b) AB(double x, double y) => ((x - P1[0]) * u[0] + (y - P1[1]) * u[1], (x - P1[0]) * v[0] + (y - P1[1]) * v[1]);
string Sig(Deswik.Graphics.Primaries.Figure f) { var p = f.asPolyline; return string.Join(";", Enumerable.Range(0, p.VertexCount).Select(i => { var q = p.GetVertex(i); return $"{q.x:F2},{q.y:F2},{q.z:F2}"; })); }
List<List<Deswik.Graphics.Primaries.Figure>> Groups(IEnumerable<Deswik.Graphics.Primaries.Figure> figs)
{
    var F = figs.ToList(); var V = F.Select(f => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => f.asPolyline.GetVertex(i)).ToList()).ToList();
    int[] par = Enumerable.Range(0, F.Count).ToArray();
    int Find(int i) { while (par[i] != i) i = par[i] = par[par[i]]; return i; }
    var byKey = new Dictionary<string, int>();
    for (int i = 0; i < F.Count; i++) foreach (var q in V[i]) { string k = $"{q.x:F2},{q.y:F2}"; if (byKey.TryGetValue(k, out var j)) par[Find(i)] = Find(j); else byKey[k] = i; }
    return Enumerable.Range(0, F.Count).GroupBy(Find).Select(g => g.Select(i => F[i]).ToList()).ToList();
}
List<(double a, double b, double z)> P(List<Deswik.Graphics.Primaries.Figure> g) => g.SelectMany(f => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => f.asPolyline.GetVertex(i))).Select(q => { var ab = AB(q.x, q.y); return (ab.a, ab.b, q.z); }).ToList();
(List<Deswik.Graphics.Primaries.Figure> g, Deswik.Graphics.Primaries.Figure lab) Match(List<List<Deswik.Graphics.Primaries.Figure>> groups, List<Deswik.Graphics.Primaries.Figure> labs, string len)
{
    var lab = labs.FirstOrDefault(t => t.asMText.TextString.StartsWith("Truck Loading Strip (1:50 up)") && t.asMText.TextString.Contains("\n" + len + " mL"));
    if (lab == null) return (null, null);
    var lm = lab.BoundingBox.Midpoint; var (la, lb) = AB(lm.x, lm.y);
    var g = groups.OrderBy(gr => { var p = P(gr); return Math.Abs((p.Min(z => z.a) + p.Max(z => z.a)) / 2 - la) + Math.Abs(p.Max(z => z.b) + 3.0 - lb); }).First();
    return (g, lab);
}
var stdPolys = new Dictionary<string, Deswik.Graphics.Primaries.Figure>(); var stdLabs = new List<Deswik.Graphics.Primaries.Figure>();
var trkPolys = new Dictionary<string, Deswik.Graphics.Primaries.Figure>(); var trkLabs = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null) continue;
    var m = f.BoundingBox.Midpoint;
    if (f.Layer.Name == std && AB(m.x, m.y).b < (f.IsMText ? -23.0 : -24.2)) { if (f.IsPolyline && !f.IsRectangle) stdPolys[Sig(f)] = f; else if (f.IsMText) stdLabs.Add(f); }   // labels sit near the band edge
    else if (f.Layer.Name == trk) { if (f.IsPolyline) trkPolys[Sig(f)] = f; else if (f.IsMText) trkLabs.Add(f); }
}
var stdG = Groups(stdPolys.Values); var trkG = Groups(trkPolys.Values);
foreach (var len in new[] { "8.0", "10.0" })
{
    var (sg, sl) = Match(trkG, trkLabs, len); var (tg, tl) = Match(stdG, stdLabs, len);
    if (sg == null || tg == null) { Out.AppendLine($"{len} m 1:50 up: source or sheet strip not found - skipped"); continue; }
    var sp = P(sg); var tp = P(tg);
    double da = (tp.Min(z => z.a) + tp.Max(z => z.a)) / 2 - (sp.Min(z => z.a) + sp.Max(z => z.a)) / 2, db = tp.Max(z => z.b) - sp.Max(z => z.b);
    double dx = da * u[0] + db * v[0], dy = da * u[1] + db * v[1], dz = 6429.80 - sp.Min(z => z.z);
    foreach (var f in sg)
    {
        var c = (Deswik.Graphics.Primaries.Figure)f.Clone(); var pl = c.asPolyline;
        var pts = Enumerable.Range(0, pl.VertexCount).Select(i => pl.GetVertex(i)).Select(q => new[] { q.x + dx, q.y + dy, q.z + dz }).ToList();
        while (pl.VertexCount > 0) pl.RemoveVertexAt(0);
        foreach (var q in pts) pl.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]);
        pl.Closed = true; c.Layer = tg[0].Layer; App.ModelSpace.Entities.AddItem(c, null, null, true, false);
    }
    foreach (var f in tg) f.Deleted = true;
    tl.asMText.TextString = sl.asMText.TextString;
    Out.AppendLine($"{len} m 1:50 up: replaced {tg.Count} sheet panels with {sg.Count} (new design); label now '{sl.asMText.TextString.Replace("\r\n", " / ")}'");
}
App.Redraw(true);
return null;
