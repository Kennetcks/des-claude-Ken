// Shorten the end ramps (ramp up / ramp down along the drive) from 5 m to 2.5 m on the strips in the new STRIP STANDARDS
// band (across < -24.2 m). A ramp panel's low corners (not shared with any other panel) move halfway towards the high
// corners, onto the backs line (high corner minus 1.5 m). The front ramp into the stockpile is left as is.
const string std = "0\\REFERENCE\\STRIP STANDARDS";
double[] P1 = { 8041.17, 62914.04 }, u = { -0.4679, 0.8838 }, v = { 0.8850, 0.4656 };
double rise = 1.5, keep = 0.5;                                               // keep half of the 5 m ramp
(double a, double b) AB(double x, double y) => ((x - P1[0]) * u[0] + (y - P1[1]) * u[1], (x - P1[0]) * v[0] + (y - P1[1]) * v[1]);
var figs = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || f.Layer.Name != std || !f.IsPolyline || f.asPolyline.VertexCount < 3) continue;
    var p = f.asPolyline; bool inBand = true;
    for (int i = 0; i < p.VertexCount; i++) { var q = p.GetVertex(i); if (AB(q.x, q.y).b > -24.2) inBand = false; }
    if (inBand) figs.Add(f);
}
string K(Deswik.Graphics.Geometry.Point q) => $"{q.x:F2},{q.y:F2}";
var uses = new Dictionary<string, int>();
foreach (var f in figs) foreach (var k in Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => K(f.asPolyline.GetVertex(i))).Distinct()) uses[k] = uses.TryGetValue(k, out var c) ? c + 1 : 1;
int n = 0;
foreach (var f in figs)
{
    var p = f.asPolyline; if (p.VertexCount != 4) continue;
    var P = Enumerable.Range(0, 4).Select(i => p.GetVertex(i)).ToList();
    var low = Enumerable.Range(0, 4).Where(i => uses[K(P[i])] == 1).ToList(); var high = Enumerable.Range(0, 4).Where(i => uses[K(P[i])] > 1).ToList();
    if (low.Count != 2 || high.Count != 2) continue;
    var abL = AB(P[low[0]].x, P[low[0]].y);
    // pair each low corner with the high corner on the same side (closest across)
    int Pair(int li) { var l = AB(P[li].x, P[li].y); return high.OrderBy(h => Math.Abs(AB(P[h].x, P[h].y).b - l.b)).First(); }
    var hp = AB(P[Pair(low[0])].x, P[Pair(low[0])].y);
    if (Math.Abs(hp.a - abL.a) < Math.Abs(hp.b - abL.b)) continue;            // front ramp (runs across) - leave
    var pts = P.Select(q => new[] { q.x, q.y, q.z }).ToList();
    foreach (int li in low)
    {
        var h = P[Pair(li)]; var l = P[li];
        double nz = (l.z + keep * ((h.z - rise) - l.z)) + 0;                  // backs line, moving towards the high corner
        pts[li] = new[] { l.x + keep * (h.x - l.x), l.y + keep * (h.y - l.y), nz };
    }
    bool closed = p.Closed; while (p.VertexCount > 0) p.RemoveVertexAt(0);
    foreach (var q in pts) p.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]);
    p.Closed = closed; p.Update(); n++;
}
App.Redraw(true);
Out.AppendLine($"Shortened {n} end ramps to 2.5 m (expected 8: 2 per strip x 4 strips)");
return null;
