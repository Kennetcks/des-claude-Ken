// Set every ramp (end ramps and truck front ramps) to TARGET m on the fan and truck transition layers and on the strip
// copies in the STRIP STANDARDS sheet's new band. A ramp's low corners (used by no other panel) slide along the ramp line
// to TARGET m from their high corners, staying on the backs line (high corner minus 1.5 m, same grade). Each changed
// ramp is replaced by a fresh copy so Deswik rebuilds its hatch.
const double TARGET = 3.5;
const string std = "0\\REFERENCE\\STRIP STANDARDS";
var sets = new (string lay, bool bandOnly)[] { (std + "\\FAN STRIP TRANSITIONS", false), (std + "\\TRUCK LOADING TRANSITIONS", false), (std, true) };
double[] P1 = { 8041.17, 62914.04 }, u = { -0.4679, 0.8838 }, v = { 0.8850, 0.4656 };
double rise = 1.5;
(double a, double b) AB(double x, double y) => ((x - P1[0]) * u[0] + (y - P1[1]) * u[1], (x - P1[0]) * v[0] + (y - P1[1]) * v[1]);
string K(Deswik.Graphics.Geometry.Point q) => $"{q.x:F2},{q.y:F2}";
foreach (var s in sets)
{
    var figs = new List<Deswik.Graphics.Primaries.Figure>();
    foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
    {
        if (f.Deleted || f.Layer == null || f.Layer.Name != s.lay || !f.IsPolyline || f.asPolyline.VertexCount < 3) continue;
        var p = f.asPolyline; bool ok = true;
        if (s.bandOnly) for (int i = 0; i < p.VertexCount; i++) { var q = p.GetVertex(i); if (AB(q.x, q.y).b > -24.2) ok = false; }
        if (ok) figs.Add(f);
    }
    var uses = new Dictionary<string, int>();
    foreach (var f in figs) foreach (var k in Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => K(f.asPolyline.GetVertex(i))).Distinct()) uses[k] = uses.TryGetValue(k, out var c) ? c + 1 : 1;
    int ends = 0, fronts = 0, same = 0;
    foreach (var f in figs)
    {
        var p = f.asPolyline; if (p.VertexCount != 4) continue;
        var P = Enumerable.Range(0, 4).Select(i => p.GetVertex(i)).ToList();
        var low = Enumerable.Range(0, 4).Where(i => uses[K(P[i])] == 1).ToList(); var high = Enumerable.Range(0, 4).Where(i => uses[K(P[i])] > 1).ToList();
        if (low.Count != 2 || high.Count != 2) continue;
        var l0 = AB(P[low[0]].x, P[low[0]].y); var hA = high.Select(h => AB(P[h].x, P[h].y)).ToList();
        bool along = hA.Min(h => Math.Abs(h.b - l0.b)) < hA.Min(h => Math.Abs(h.a - l0.a));
        int Pair(int li) { var l = AB(P[li].x, P[li].y); return along ? high.OrderBy(h => Math.Abs(AB(P[h].x, P[h].y).b - l.b)).First() : high.OrderBy(h => Math.Abs(AB(P[h].x, P[h].y).a - l.a)).First(); }
        double len = Math.Sqrt(Math.Pow(P[Pair(low[0])].x - P[low[0]].x, 2) + Math.Pow(P[Pair(low[0])].y - P[low[0]].y, 2));
        if (Math.Abs(len - TARGET) < 0.01) { same++; continue; }
        double k = TARGET / len;
        var pts = P.Select(q => new[] { q.x, q.y, q.z }).ToList();
        foreach (int li in low) { var h = P[Pair(li)]; var l = P[li]; double mc = h.z - rise; pts[li] = new[] { h.x + k * (l.x - h.x), h.y + k * (l.y - h.y), mc + k * (l.z - mc) }; }
        var c2 = (Deswik.Graphics.Primaries.Figure)f.Clone(); var p2 = c2.asPolyline;
        while (p2.VertexCount > 0) p2.RemoveVertexAt(0);
        foreach (var q in pts) p2.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]);
        p2.Closed = true; c2.Layer = f.Layer;
        App.ModelSpace.Entities.AddItem(c2, null, null, true, false); f.Deleted = true;
        if (along) ends++; else fronts++;
    }
    Out.AppendLine($"{s.lay.Substring(s.lay.LastIndexOf('\\') + 1)}{(s.bandOnly ? " (sheet copies)" : "")}: {ends} end ramps, {fronts} front ramps set to {TARGET} m; {same} already {TARGET} m");
}
App.Redraw(true);
return null;
