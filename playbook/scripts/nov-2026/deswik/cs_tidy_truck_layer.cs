// Tidy TRUCK LOADING TRANSITIONS into a grid. Strips = panels sharing vertices. Each strip's high-section length, grade
// and style (straight / decline-curved) are read from its geometry; strips move rigidly into their cell and get a fresh
// label from those values (old labels removed). Row 1: 10 m straight, row 2: 8 m straight, row 3: decline-curved;
// columns: 1:7 down, 1:7 up, 1:50 up. Panels are re-added as fresh copies so the hatch follows.
const string trk = "0\\REFERENCE\\STRIP STANDARDS\\TRUCK LOADING TRANSITIONS";
double[] P1 = { 8041.17, 62914.04 }, u = { -0.4679, 0.8838 }, v = { 0.8850, 0.4656 };
double[] colA = { -30.0, 0.0, 30.0 }; double[] rowB = { 49.0, 72.0, 95.0 };      // cell centre along / far-wall across
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

var plan = new List<(List<int> g, int row, int col, double L, string gt, bool curved)>();
foreach (var g in groups)
{
    bool curved = g.Any(i => V[i].Count > 6);
    // high section: the panel whose vertices are all shared with other panels and is longest along the strip
    var pts = g.SelectMany(i => V[i]).ToList(); var ab = pts.Select(q => AB(q.x, q.y)).ToList();
    var hi = g.OrderByDescending(i => { var a = V[i].Select(q => AB(q.x, q.y).a); return a.Max() - a.Min(); }).First();
    var hab = V[hi].Select(q => (AB(q.x, q.y).a, q.z)).ToList();
    double hl = hab.Max(z => z.a) - hab.Min(z => z.a);
    double slope = (hab.OrderBy(z => z.a).Last().z - hab.OrderBy(z => z.a).First().z) / Math.Max(hl, 0.1);
    double L = curved ? 8.0 : Math.Round(hl);
    string gt = Math.Abs(slope) < 0.05 ? "1:50 up" : slope < 0 ? "1:7 down" : "1:7 up";
    if (curved) { var lo = pts.OrderBy(q => AB(q.x, q.y).a).First(); var hiP = pts.OrderBy(q => AB(q.x, q.y).a).Last(); gt = hiP.z < lo.z ? "1:7 down" : "1:7 up"; }
    int row = curved ? 2 : (L >= 9.5 ? 0 : 1); int col = gt == "1:7 down" ? 0 : gt == "1:7 up" ? 1 : 2;
    plan.Add((g, row, col, L, gt, curved));
    Out.AppendLine($"strip: {(curved ? "curved" : "straight")} {L} m {gt}, {g.Count} panels, now along {ab.Min(z => z.a):F1}..{ab.Max(z => z.a):F1}, across {ab.Min(z => z.b):F1}..{ab.Max(z => z.b):F1} -> row {row + 1}, col {col + 1}");
}
var dup = plan.GroupBy(p => (p.row, p.col)).Where(x => x.Count() > 1).ToList();
if (dup.Any()) { Out.AppendLine("Two strips want the same cell - nothing changed: " + string.Join(", ", dup.Select(d => $"row {d.Key.row + 1} col {d.Key.col + 1}"))); return null; }
var tText = labels.FirstOrDefault(); if (tText == null) { Out.AppendLine("No label to copy - nothing changed."); return null; }

foreach (var p in plan)
{
    var ab = p.g.SelectMany(i => V[i]).Select(q => AB(q.x, q.y)).ToList();
    double da = colA[p.col] - (ab.Min(z => z.a) + ab.Max(z => z.a)) / 2, db = rowB[p.row] - ab.Max(z => z.b);
    double dx = da * u[0] + db * v[0], dy = da * u[1] + db * v[1];
    foreach (int i in p.g)
    {
        var c = (Deswik.Graphics.Primaries.Figure)F[i].Clone(); var pl = c.asPolyline;
        var pts = V[i].Select(q => new[] { q.x + dx, q.y + dy, q.z }).ToList();
        while (pl.VertexCount > 0) pl.RemoveVertexAt(0);
        foreach (var q in pts) pl.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]);
        pl.Closed = true; c.Layer = F[i].Layer;
        App.ModelSpace.Entities.AddItem(c, null, null, true, false); F[i].Deleted = true;
    }
    var t = (Deswik.Graphics.Primaries.Figure)tText.Clone();
    t.asMText.TextString = $"Truck Loading Strip ({p.gt}{(p.curved ? ", decline" : "")})\r\n{p.L:F1} mL x 5.0mW x 1.5mH";
    var c0 = (colA[p.col], rowB[p.row] + 3.0);
    t.asMText.InsertionPoint = new Deswik.Graphics.Geometry.Point(P1[0] + c0.Item1 * u[0] + c0.Item2 * v[0], P1[1] + c0.Item1 * u[1] + c0.Item2 * v[1], tText.asMText.InsertionPoint.z);
    t.Layer = tText.Layer; App.ModelSpace.Entities.AddItem(t, null, null, true, false);
}
foreach (var t in labels) t.Deleted = true;
App.Redraw(true);
Out.AppendLine($"Placed {plan.Count} strips in the grid and relabelled them");
return null;
