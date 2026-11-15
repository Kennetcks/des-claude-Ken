// Put each fan / truck strip label above its strip: centred along the strip, 1 m beyond the strip's top edge (the
// stockpile side, -v), text running along the strip (+u) so it reads upright, bottom-centre justified.
// Each label currently sits on its strip's high panel (centroid), which identifies the strip.
string[] layers = { "0\\REFERENCE\\STRIP STANDARDS\\FAN STRIP TRANSITIONS", "0\\REFERENCE\\STRIP STANDARDS\\TRUCK LOADING TRANSITIONS" };
double[] P1 = { 8041.17, 62914.04 }, u = { -0.4679, 0.8838 }, v = { 0.8850, 0.4656 };
double gap = 1.0, rot = Math.Atan2(u[1], u[0]);
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
    int[] par = Enumerable.Range(0, polys.Count).ToArray();
    int Find(int i) { while (par[i] != i) i = par[i] = par[par[i]]; return i; }
    var byKey = new Dictionary<string, int>();
    for (int i = 0; i < polys.Count; i++)
        foreach (var q in polys[i]) { string k = $"{q[0]:F2},{q[1]:F2}"; if (byKey.TryGetValue(k, out var j)) par[Find(i)] = Find(j); else byKey[k] = i; }
    foreach (var t in labels)
    {
        var ip = t.asMText.InsertionPoint;
        int hp = Enumerable.Range(0, polys.Count).Where(i => polys[i].Count == 4).OrderBy(i => Math.Abs(polys[i].Average(q => q[0]) - ip.x) + Math.Abs(polys[i].Average(q => q[1]) - ip.y)).First();
        var strip = Enumerable.Range(0, polys.Count).Where(i => Find(i) == Find(hp)).SelectMany(i => polys[i]).ToList();
        var ab = strip.Select(q => AB(q[0], q[1])).ToList();
        double am = (ab.Min(z => z.a) + ab.Max(z => z.a)) / 2, bt = ab.Min(z => z.b) - gap;
        double x = P1[0] + am * u[0] + bt * v[0], y = P1[1] + am * u[1] + bt * v[1];
        double z = polys[hp].Average(q => q[2]);
        var m = t.asMText;
        m.InsertionPoint = new Deswik.Graphics.Geometry.Point(x, y, z);
        m.GetType().GetProperty("Rotation")?.SetValue(m, rot);
        foreach (var (prop, word) in new[] { ("HorJustify", "Center"), ("VerJustify", "Bottom") })
        {
            var pi = m.GetType().GetProperty(prop); if (pi == null || !pi.PropertyType.IsEnum) continue;
            var name = Enum.GetNames(pi.PropertyType).FirstOrDefault(nm => nm.Contains(word));
            if (name != null) pi.SetValue(m, Enum.Parse(pi.PropertyType, name));
        }
        moved++;
        Out.AppendLine($"'{m.TextString.Split('\r')[0]}' -> centred above strip ({ab.Max(zz => zz.a) - ab.Min(zz => zz.a):F1} m long)");
    }
}
App.Redraw(true);
Out.AppendLine($"Moved {moved} labels; rotation {rot * 180 / Math.PI:F1} deg");
return null;
