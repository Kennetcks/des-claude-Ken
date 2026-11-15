// Copy the user's adjusted label (Double Fan Strip (1:7 up)) to the other strip labels: same rotation and justification,
// and the same placement relative to its strip (text-box centre offset from the strip's along-middle and far edge).
string[] layers = { "0\\REFERENCE\\STRIP STANDARDS\\FAN STRIP TRANSITIONS", "0\\REFERENCE\\STRIP STANDARDS\\TRUCK LOADING TRANSITIONS" };
double[] P1 = { 8041.17, 62914.04 }, u = { -0.4679, 0.8838 }, v = { 0.8850, 0.4656 };
(double a, double b) AB(double x, double y) => ((x - P1[0]) * u[0] + (y - P1[1]) * u[1], (x - P1[0]) * v[0] + (y - P1[1]) * v[1]);
(double x, double y) XY(double a, double b) => (P1[0] + a * u[0] + b * v[0], P1[1] + a * u[1] + b * v[1]);

var items = new List<(Deswik.Graphics.Primaries.Figure t, double am, double bMax, double hz)>();
foreach (var lay in layers)
{
    var polys = new List<List<double[]>>(); var labels = new List<Deswik.Graphics.Primaries.Figure>();
    foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
    {
        if (f.Deleted || f.Layer == null || f.Layer.Name != lay) continue;
        if (f.IsMText) labels.Add(f);
        else if (f.IsPolyline) { var p = f.asPolyline; polys.Add(Enumerable.Range(0, p.VertexCount).Select(i => { var q = p.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList()); }
    }
    int[] par = Enumerable.Range(0, polys.Count).ToArray();
    int Find(int i) { while (par[i] != i) i = par[i] = par[par[i]]; return i; }
    var byKey = new Dictionary<string, int>();
    for (int i = 0; i < polys.Count; i++) foreach (var q in polys[i]) { string k = $"{q[0]:F2},{q[1]:F2}"; if (byKey.TryGetValue(k, out var j)) par[Find(i)] = Find(j); else byKey[k] = i; }
    var groups = Enumerable.Range(0, polys.Count).GroupBy(Find).Select(g => g.ToList()).ToList();
    foreach (var t in labels)
    {
        var mid = t.BoundingBox.Midpoint; var (la, lb) = AB(mid.x, mid.y);
        double Dist(List<int> g) { var ab = g.SelectMany(i => polys[i]).Select(q => AB(q[0], q[1])).ToList(); double da = Math.Max(0, Math.Max(ab.Min(z => z.a) - la, la - ab.Max(z => z.a))), db = Math.Max(0, Math.Max(ab.Min(z => z.b) - lb, lb - ab.Max(z => z.b))); return Math.Sqrt(da * da + db * db); }
        var grp = groups.OrderBy(Dist).First();
        var abg = grp.SelectMany(i => polys[i]).Select(q => AB(q[0], q[1])).ToList();
        var high = grp.Select(i => polys[i]).Where(p => p.Count == 4).OrderByDescending(p => p.Max(q => q[2]) - p.Min(q => q[2]) < 1e9 ? p.Average(q => q[2]) : 0).First();
        items.Add((t, (abg.Min(z => z.a) + abg.Max(z => z.a)) / 2, abg.Max(z => z.b), grp.SelectMany(i => polys[i]).Max(q => q[2])));
    }
}
var refItem = items.FirstOrDefault(i => i.t.asMText.TextString.StartsWith("Double Fan Strip (1:7 up)"));
if (refItem.t == null) { Out.AppendLine("Adjusted label not found - nothing changed."); return null; }
var rm = refItem.t.asMText; var rmid = refItem.t.BoundingBox.Midpoint; var (ra, rb) = AB(rmid.x, rmid.y);
double dA = ra - refItem.am, dB = rb - refItem.bMax, dZ = rm.InsertionPoint.z - refItem.hz;
Out.AppendLine($"reference: rotation {rm.RotationRad * 180 / Math.PI:F1} deg, {rm.HorizontalJustification}/{rm.VerticalJustification}, centre {dA:F2} m along from middle, {dB:F2} m off the far edge");
int n = 0;
foreach (var it in items)
{
    if (it.t == refItem.t) continue;
    var m = it.t.asMText;
    m.RotationRad = rm.RotationRad; m.HorizontalJustification = rm.HorizontalJustification; m.VerticalJustification = rm.VerticalJustification;
    m.Height = rm.Height;
    it.t.GetType().GetMethod("Update", Type.EmptyTypes)?.Invoke(it.t, null);
    // shift so the text-box centre lands at the same spot relative to this strip
    var cm = it.t.BoundingBox.Midpoint; var (ca, cb) = AB(cm.x, cm.y);
    double ta = it.am + dA, tb = it.bMax + dB; var (sx, sy) = XY(ta - ca, tb - cb);
    var ip = m.InsertionPoint; m.InsertionPoint = new Deswik.Graphics.Geometry.Point(ip.x + (sx - P1[0]), ip.y + (sy - P1[1]), it.hz + dZ);
    it.t.GetType().GetMethod("Update", Type.EmptyTypes)?.Invoke(it.t, null);
    var nm = it.t.BoundingBox.Midpoint; var (na, nb) = AB(nm.x, nm.y);
    Out.AppendLine($"{m.TextString.Split('\r')[0],-32} centre now {na - it.am:F2} along / {nb - it.bMax:F2} off edge");
    n++;
}
App.Redraw(true);
Out.AppendLine($"Updated {n} labels");
return null;
