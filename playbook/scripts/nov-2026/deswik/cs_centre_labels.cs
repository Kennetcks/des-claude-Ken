// Centre each fan / truck strip label on its strip: panels sharing vertices form one strip; each label belongs to the
// strip it was placed beside (1.8 m past the strip end, 5 m beyond the far wall). The label moves to the centroid of
// the strip's largest panel (the high section) and is set to middle-centre justification.
string[] layers = { "0\\REFERENCE\\STRIP STANDARDS\\FAN STRIP TRANSITIONS", "0\\REFERENCE\\STRIP STANDARDS\\TRUCK LOADING TRANSITIONS" };
double[] P1 = { 8041.17, 62914.04 }, u = { -0.4679, 0.8838 }, v = { 0.8850, 0.4656 };
(double a, double b) AB(double x, double y) => ((x - P1[0]) * u[0] + (y - P1[1]) * u[1], (x - P1[0]) * v[0] + (y - P1[1]) * v[1]);
int moved = 0;
foreach (var lay in layers)
{
    var polys = new List<List<double[]>>(); var labels = new List<Deswik.Graphics.Primaries.Figure>();
    foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
    {
        if (f.Deleted || f.Layer == null || f.Layer.Name != lay) continue;
        if (f.IsMText) labels.Add(f);
        else if (f.IsPolyline) { var p = f.asPolyline; polys.Add(Enumerable.Range(0, p.VertexCount).Select(i => { var q = p.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList()); }
    }
    // union panels that share a vertex
    int[] par = Enumerable.Range(0, polys.Count).ToArray();
    int Find(int i) { while (par[i] != i) i = par[i] = par[par[i]]; return i; }
    var byKey = new Dictionary<string, int>();
    for (int i = 0; i < polys.Count; i++)
        foreach (var q in polys[i]) { string k = $"{q[0]:F2},{q[1]:F2}"; if (byKey.TryGetValue(k, out var j)) par[Find(i)] = Find(j); else byKey[k] = i; }
    var groups = Enumerable.Range(0, polys.Count).GroupBy(Find).Select(g => g.ToList()).ToList();
    double Area(List<double[]> p) { double s = 0; for (int i = 0; i < p.Count; i++) { var a = p[i]; var b = p[(i + 1) % p.Count]; s += a[0] * b[1] - b[0] * a[1]; } return Math.Abs(s) / 2; }
    var used = new HashSet<int>();
    foreach (var t in labels)
    {
        var ip = t.asMText.InsertionPoint; var (la, lb) = AB(ip.x, ip.y);
        int best = -1; double bd = 1e9;
        for (int gi = 0; gi < groups.Count; gi++)
        {
            if (used.Contains(gi)) continue;
            var ab = groups[gi].SelectMany(i => polys[i]).Select(q => AB(q[0], q[1])).ToList();
            double ea = ab.Max(z => z.a) + 1.8, eb = ab.Max(z => z.b) + 5.0, d = Math.Sqrt((la - ea) * (la - ea) + (lb - eb) * (lb - eb));
            if (d < bd) { bd = d; best = gi; }
        }
        if (best < 0 || bd > 8) { Out.AppendLine($"no strip found for label '{t.asMText.TextString.Split('\r')[0]}' (closest {bd:F1} m) - left as is"); continue; }
        used.Add(best);
        var high = groups[best].Select(i => polys[i]).OrderByDescending(Area).First();
        double cx = high.Average(q => q[0]), cy = high.Average(q => q[1]), cz = high.Average(q => q[2]);
        t.asMText.InsertionPoint = new Deswik.Graphics.Geometry.Point(cx, cy, cz);
        foreach (var (prop, word) in new[] { ("HorJustify", "Center"), ("VerJustify", "Cen") })
        {
            var pi = t.asMText.GetType().GetProperty(prop); if (pi == null || !pi.PropertyType.IsEnum) continue;
            var name = Enum.GetNames(pi.PropertyType).FirstOrDefault(nm => nm.Contains(word) || nm.Contains("Middle"));
            if (name != null) pi.SetValue(t.asMText, Enum.Parse(pi.PropertyType, name));
        }
        moved++;
        Out.AppendLine($"centred '{t.asMText.TextString.Split('\r')[0]}' ({bd:F1} m match)");
    }
}
App.Redraw(true);
Out.AppendLine($"Centred {moved} labels");
return null;
