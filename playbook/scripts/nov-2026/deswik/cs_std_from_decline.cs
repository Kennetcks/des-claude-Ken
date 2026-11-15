// Replace the 8 m 1:7 down / 1:7 up truck loading standards with the decline-following 8 m strip built by the decline
// tool (panels tagged CLAUDE_DECLINE_STRIP on 0\DESIGN DRAFT\STRIPPING). The down slot gets a straight copy; the up slot
// gets the copy mirrored along its length (climbs instead of drops, stockpile on the same side). Labels are kept.
const string trk = "0\\REFERENCE\\STRIP STANDARDS\\TRUCK LOADING TRANSITIONS";
double[] P1 = { 8041.17, 62914.04 }, u = { -0.4679, 0.8838 }, v = { 0.8850, 0.4656 };
(double a, double b) AB(double x, double y) => ((x - P1[0]) * u[0] + (y - P1[1]) * u[1], (x - P1[0]) * v[0] + (y - P1[1]) * v[1]);

// --- source strip and its frame (origin = high-section centroid, t = downhill along the decline, n = towards the stockpile)
var src = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
    if (!f.Deleted && f.Layer != null && f.Layer.Name == "0\\DESIGN DRAFT\\STRIPPING" && f.IsPolyline && (f.AttributeValueGet("CLAUDE_DECLINE_STRIP", "") ?? "").ToString() == "1") src.Add(f);
if (src.Count != 6) { Out.AppendLine($"Expected the 6 panels of the 8 m decline strip, found {src.Count} - nothing changed."); return null; }
List<Deswik.Graphics.Geometry.Point> V(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => f.asPolyline.GetVertex(i)).ToList();
var high = src.OrderByDescending(f => f.asPolyline.VertexCount).First();         // the high-section band has the most points
var H = V(high); int half = H.Count / 2;
double ox = H.Average(q => q.x), oy = H.Average(q => q.y), oz = H.Average(q => q.z);
double tx = H[half - 1].x - H[0].x, ty = H[half - 1].y - H[0].y, tl = Math.Sqrt(tx * tx + ty * ty); tx /= tl; ty /= tl;
if (H[half - 1].z > H[0].z) { tx = -tx; ty = -ty; }                              // t points downhill
var open = src.Where(f => f != high).OrderBy(f => { var q = V(f); return Math.Abs(q.Max(p => p.z) - H.Max(p => p.z)); }).ThenBy(f => V(f).Count).ToList();
var others = src.Where(f => f != high).SelectMany(V).ToList();
double sx = others.Average(q => q.x) - ox, sy = others.Average(q => q.y) - oy;
double nx = -ty, ny = tx; if (sx * nx + sy * ny < 0) { nx = -nx; ny = -ny; }       // n towards the stockpile side

// --- target slots: the current 8 m 1:7 strips (panels + label) on the truck standards layer
var polys = new List<Deswik.Graphics.Primaries.Figure>(); var labels = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || f.Layer.Name != trk) continue;
    if (f.IsPolyline) polys.Add(f); else if (f.IsMText) labels.Add(f);
}
var P = polys.Select(V).ToList();
int[] par = Enumerable.Range(0, P.Count).ToArray();
int Find(int i) { while (par[i] != i) i = par[i] = par[par[i]]; return i; }
var byKey = new Dictionary<string, int>();
for (int i = 0; i < P.Count; i++) foreach (var q in P[i]) { string k = $"{q.x:F2},{q.y:F2}"; if (byKey.TryGetValue(k, out var j)) par[Find(i)] = Find(j); else byKey[k] = i; }
var groups = Enumerable.Range(0, P.Count).GroupBy(Find).Select(g => g.ToList()).ToList();
int made = 0, removed = 0;
foreach (var (grade, mirror) in new[] { ("1:7 down", false), ("1:7 up", true) })
{
    var lab = labels.FirstOrDefault(t => t.asMText.TextString.StartsWith($"Truck Loading Strip ({grade})") && t.asMText.TextString.Contains("8.0 mL"));
    if (lab == null) { Out.AppendLine($"Label for 8 m {grade} not found - skipped."); continue; }
    var lm = lab.BoundingBox.Midpoint; var (la, lb) = AB(lm.x, lm.y);
    var grp = groups.OrderBy(g => { var ab = g.SelectMany(i => P[i]).Select(q => AB(q.x, q.y)).ToList(); return Math.Abs((ab.Min(z => z.a) + ab.Max(z => z.a)) / 2 - la) + Math.Abs(ab.Max(z => z.b) + 3.0 - lb); }).First();
    var mb = grp.SelectMany(i => P[i]).ToList(); var gab = mb.Select(q => AB(q.x, q.y)).ToList();
    // target frame: centre of the old strip's drive part, T = downhill along the strip, N = towards the stockpile (-v)
    double am = (gab.Min(z => z.a) + gab.Max(z => z.a)) / 2, bm = gab.Max(z => z.b) - 2.5;
    double Tx = mirror ? -u[0] : u[0], Ty = mirror ? -u[1] : u[1], Nx = -v[0], Ny = -v[1];
    double cx = P1[0] + am * u[0] + bm * v[0], cy = P1[1] + am * u[1] + bm * v[1], cz = mb.Average(q => q.z) + 1.0;
    foreach (int i in grp) { polys[i].Deleted = true; removed++; }
    foreach (var f in src)
    {
        var c = (Deswik.Graphics.Primaries.Figure)f.Clone(); var p = c.asPolyline;
        var pts = V(f).Select(q => { double dt = (q.x - ox) * tx + (q.y - oy) * ty, dn = (q.x - ox) * nx + (q.y - oy) * ny; return new[] { cx + dt * Tx + dn * Nx, cy + dt * Ty + dn * Ny, cz + (q.z - oz) }; }).ToList();
        while (p.VertexCount > 0) p.RemoveVertexAt(0);
        foreach (var q in pts) p.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]);
        p.Closed = true; c.Layer = App.Layers.First(l => l.Name == trk);
        c.AttributeValueSet("CLAUDE_DECLINE_STRIP", "");                            // a standard, not a design strip
        App.ModelSpace.Entities.AddItem(c, null, null, true, false); made++;
    }
    Out.AppendLine($"8 m {grade}: replaced {grp.Count} panels with the decline design{(mirror ? " (mirrored)" : "")}");
}
App.Redraw(true);
Out.AppendLine($"Removed {removed}, added {made}");
return null;
