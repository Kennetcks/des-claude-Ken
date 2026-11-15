// Level Design Drafter: solids of the strip standards on a NEW layer 0\REFERENCE\STRIP STANDARDS\SOLIDS, same place as
// the drawn strips. Nothing existing is changed. Fillets are skipped.
//  - back strips: high section = the drawn panel down to 1.5 m below it (prism); ramp = wedge from the panel down to the
//    original back (1.5 m at the edge it shares with the high section, 0 at the toe).
//  - wall boxes (distribution, J/Box, fan starter, pump, laser): the drawn outline at floor level up by the height in
//    the nearest label (open outlines are closed along the wall).
// One solid per panel; the dry run lists them grouped by strip with volumes.
const bool APPLY = false;
const string root = "0\\REFERENCE\\STRIP STANDARDS", target = root + "\\SOLIDS";
const double RISE = 1.5;
var inv = System.Globalization.CultureInfo.InvariantCulture;
// REBUILD: the SOLIDS layer was made by this script; its solids are replaced (nothing else on any layer is touched)
const bool REBUILD = true;
if (App.Layers.Exists(target) && !REBUILD) { Out.AppendLine(target + " already exists - stopping, nothing changed."); return null; }

var panels = new List<(string layer, List<double[]> p, bool closed, int hatch)>(); var labels = new List<(string layer, double x, double y, string text)>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || !f.Layer.Name.StartsWith(root)) continue;
    if (f.IsMText) { var m = f.BoundingBox.Midpoint; labels.Add((f.Layer.Name, m.x, m.y, f.asMText.TextString.Replace("\r\n", " / ").Replace("\n", " / ").Trim())); continue; }
    if (!f.IsPolyline || f.asPolyline.VertexCount < 3) continue;
    var pl = f.asPolyline; var pts = Enumerable.Range(0, pl.VertexCount).Select(i => { var q = pl.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
    if (Math.Abs(pts[0][0] - pts[^1][0]) < 1e-6 && Math.Abs(pts[0][1] - pts[^1][1]) < 1e-6) pts.RemoveAt(pts.Count - 1);
    panels.Add((f.Layer.Name, pts, pl.Closed, pl.HatchProperties == null ? -1 : pl.HatchProperties.FillBackColor.ColorIndex));
}
bool Same(double[] a, double[] b) => Math.Abs(a[0] - b[0]) < 0.02 && Math.Abs(a[1] - b[1]) < 0.02 && Math.Abs(a[2] - b[2]) < 0.05;
double SArea(List<double[]> p) { double a = 0; for (int i = 0; i < p.Count; i++) { var s = p[i]; var t = p[(i + 1) % p.Count]; a += s[0] * t[1] - t[0] * s[1]; } return a / 2; }

// ear clipping in plan, ring made CCW first; returns index triples into the ring
List<int[]> Ears(List<double[]> p)
{
    var idx = Enumerable.Range(0, p.Count).ToList(); var tris = new List<int[]>();
    double Cr(double[] a, double[] b, double[] c) => (b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0]);
    int guard = 0;
    while (idx.Count > 3 && guard++ < 10000)
    {
        bool cut = false;
        for (int k = 0; k < idx.Count; k++)
        {
            int ia = idx[(k + idx.Count - 1) % idx.Count], ib = idx[k], ic = idx[(k + 1) % idx.Count];
            if (Cr(p[ia], p[ib], p[ic]) <= 1e-9) continue;
            bool inside = false;
            foreach (var j in idx) { if (j == ia || j == ib || j == ic) continue; if (Cr(p[ia], p[ib], p[j]) >= 0 && Cr(p[ib], p[ic], p[j]) >= 0 && Cr(p[ic], p[ia], p[j]) >= 0) { inside = true; break; } }
            if (inside) continue;
            tris.Add(new[] { ia, ib, ic }); idx.RemoveAt(k); cut = true; break;
        }
        if (!cut) break;
    }
    if (idx.Count == 3) tris.Add(idx.ToArray());
    return tris;
}

// a solid = vertex list + triangles; volume by the divergence sum, flipped to positive
var solids = new List<(string layer, string kind, double cx, double cy, List<double[]> v, List<int[]> t, double vol)>();
double Vol(List<double[]> v, List<int[]> t) => t.Sum(f => { var a = v[f[0]]; var b = v[f[1]]; var c = v[f[2]]; return (a[0] * (b[1] * c[2] - b[2] * c[1]) - a[1] * (b[0] * c[2] - b[2] * c[0]) + a[2] * (b[0] * c[1] - b[1] * c[0])) / 6; });
void Add(string layer, string kind, List<double[]> ring, List<double[]> v, List<int[]> t)
{
    var vol = Vol(v, t); if (vol < 0) { foreach (var f in t) (f[1], f[2]) = (f[2], f[1]); vol = -vol; }
    solids.Add((layer, kind, ring.Average(q => q[0]), ring.Average(q => q[1]), v, t, vol));
}
void Prism(string layer, string kind, List<double[]> lower, List<double[]> upper)
{
    int n = lower.Count; var v = new List<double[]>(lower); v.AddRange(upper); var t = new List<int[]>();
    foreach (var e in Ears(lower)) { t.Add(new[] { n + e[0], n + e[1], n + e[2] }); t.Add(new[] { e[2], e[1], e[0] }); }
    for (int i = 0; i < n; i++) { int j = (i + 1) % n; t.Add(new[] { i, j, n + j }); t.Add(new[] { i, n + j, n + i }); }
    Add(layer, kind, upper, v, t);
}
// ramp: depth 1.5 on the edge shared with the high section, 0 on the toe (the longest edge of the rest of the ring),
// and in between along the two sides by arc length - so ramps that follow a curved decline keep their full wedge
void Wedge(string layer, List<double[]> ring, bool[] high)
{
    int n = ring.Count; int e = Enumerable.Range(0, n).First(i => high[i] && !high[(i + 1) % n]);   // last high vertex
    var R = new List<int>(); for (int k = 1; k < n; k++) { int i = (e + k) % n; if (high[i]) break; R.Add(i); }
    double D2(int i, int j) => Math.Sqrt(Math.Pow(ring[i][0] - ring[j][0], 2) + Math.Pow(ring[i][1] - ring[j][1], 2));
    int toe = Enumerable.Range(0, R.Count - 1).OrderByDescending(k => D2(R[k], R[k + 1])).FirstOrDefault();   // toe edge R[toe]-R[toe+1]
    var depth = new double[n]; for (int i = 0; i < n; i++) depth[i] = high[i] ? RISE : 0;
    void Side(List<int> chain)   // chain[0] on the high edge, last on the toe
    {
        var s = new double[chain.Count]; for (int k = 1; k < chain.Count; k++) s[k] = s[k - 1] + D2(chain[k - 1], chain[k]);
        for (int k = 1; k < chain.Count; k++) depth[chain[k]] = RISE * (1 - s[k] / s[^1]);
    }
    var side1 = new List<int> { e }; side1.AddRange(R.Take(toe + 1)); Side(side1);
    var side2 = new List<int> { (R[^1] + 1) % n }; side2.AddRange(Enumerable.Reverse(R.Skip(toe + 1))); Side(side2);
    // prism with per-vertex depth; toe vertices (depth 0) are shared by top and bottom
    var v = new List<double[]>(ring); var bot = new int[n];
    for (int i = 0; i < n; i++) { if (depth[i] < 1e-6) bot[i] = i; else { bot[i] = v.Count; v.Add(new[] { ring[i][0], ring[i][1], ring[i][2] - depth[i] }); } }
    var t = new List<int[]>();
    foreach (var f in Ears(ring)) { t.Add(new[] { f[0], f[1], f[2] }); t.Add(new[] { bot[f[2]], bot[f[1]], bot[f[0]] }); }
    for (int i = 0; i < n; i++)
    {
        int j = (i + 1) % n; if (bot[i] == i && bot[j] == j) continue;
        if (bot[j] != j) t.Add(new[] { bot[i], bot[j], j });
        if (bot[i] != i) t.Add(new[] { bot[i], j, i });
    }
    Add(layer, "ramp", ring, v, t);
}

int skipped = 0;
var ramps = panels.Where(p => p.closed && p.hatch == 253).ToList();
foreach (var pn in panels)
{
    var ring = SArea(pn.p) < 0 ? Enumerable.Reverse(pn.p).ToList() : pn.p.ToList();
    if (pn.closed && pn.hatch == 253)
    {
        var high = ring.Select(q => panels.Any(o => o.closed && o.hatch != 253 && o.layer == pn.layer && o.p.Any(r => Same(r, q)))).ToArray();
        if (!high.Any(x => x) || high.All(x => x)) { Out.AppendLine($"ramp at ({ring[0][0]:F1},{ring[0][1]:F1}) has no high edge - skipped"); skipped++; continue; }
        Wedge(pn.layer, ring, high); continue;
    }
    bool isHigh = pn.closed && ramps.Any(r => r.layer == pn.layer && r.p.Any(q => pn.p.Any(w => Same(q, w))));
    if (isHigh) { Prism(pn.layer, "high", ring.Select(q => new[] { q[0], q[1], q[2] - RISE }).ToList(), ring); continue; }
    if (ring.Count == 3) { skipped++; continue; }   // fillet
    double cx = ring.Average(q => q[0]), cy = ring.Average(q => q[1]);
    var lab = labels.Select(l => (l, d: Math.Sqrt(Math.Pow(l.x - cx, 2) + Math.Pow(l.y - cy, 2)), m: System.Text.RegularExpressions.Regex.Match(l.text, @"x\s*([\d.]+)\s*mH")))
        .Where(z => z.m.Success && double.Parse(z.m.Groups[1].Value, inv) >= 2.0).OrderBy(z => z.d).FirstOrDefault();
    if (lab.l.text == null || lab.d > 20) { Out.AppendLine($"outline at ({cx:F1},{cy:F1}) has no box label nearby - skipped"); skipped++; continue; }
    double H = double.Parse(lab.m.Groups[1].Value, inv);
    Prism(pn.layer, "box " + lab.l.text.Split(new[] { " / " }, StringSplitOptions.None)[0].Trim(), ring, ring.Select(q => new[] { q[0], q[1], q[2] + H }).ToList());
}

// report: group by strip = nearest strip label on the same layer (back strips) or the box label
foreach (var g in solids.GroupBy(s => s.kind.StartsWith("box") ? s.kind.Substring(4) + $" @({s.cx:F0},{s.cy:F0})" :
             labels.Where(l => l.layer == s.layer && l.text.Contains("1.5mH")).OrderBy(l => Math.Pow(l.x - s.cx, 2) + Math.Pow(l.y - s.cy, 2)).Select(l => l.text + $" [{l.x:F0},{l.y:F0}]").FirstOrDefault() ?? "?")
             .OrderBy(g => g.Key))
    Out.AppendLine($"{g.Key}: " + string.Join(" + ", g.Select(s => $"{(s.kind.StartsWith("box") ? "box" : s.kind)} {s.vol:F1}")) + $" = {g.Sum(s => s.vol):F1} m3");
Out.AppendLine($"{solids.Count} solids, {skipped} outlines skipped (fillets etc.)");
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }

int removed = 0;
if (App.Layers.Exists(target))
    foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
        if (!f.Deleted && f.Layer != null && f.Layer.Name == target && f.IsPolyface) { f.Deleted = true; removed++; }
var layer = App.Layers.Exists(target) ? App.Layers.FindName(target, false) : App.Layers.Add(target, false, false, 0, false);
var setLayer = typeof(Deswik.Graphics.Primaries.Figure).GetProperty("Layer");
int bad = 0; double tot = 0;
foreach (var s in solids)
{
    // Deswik face list: 1-based vertex numbers, 5 per face (p1, p2, p3, p4 = p1 for a triangle, -1)
    var pts = new Deswik.Graphics.Geometry.Points(); foreach (var q in s.v) pts.Add(q[0], q[1], q[2]);
    var fl = new List<int>(); foreach (var f in s.t) if (f[0] != f[1] && f[1] != f[2] && f[0] != f[2]) fl.AddRange(new[] { f[0] + 1, f[1] + 1, f[2] + 1, f[0] + 1, -1 });
    var pf = new Deswik.Graphics.Figures.Polyface(pts, new Deswik.Graphics.Geometry.Int32Array(fl.ToArray()));
    setLayer.SetValue(pf, layer);
    App.ModelSpace.Entities.AddItem(pf, null, null, true, false);
    bool valid = pf.Validate(true, true, true, true, false, true, false, false);
    pf.ResetVolumeAndAreaCache(); var dv = pf.Volume; tot += dv; if (!valid || Math.Abs(dv - s.vol) > 0.05) { bad++; if (bad <= 5) Out.AppendLine($"check ({s.cx:F1},{s.cy:F1}) {s.kind}: valid {valid} Deswik {dv:F2} vs {s.vol:F2} {pf.ValidateMessage()}"); }
}
Out.AppendLine($"removed {removed} earlier solids from {target}");
App.Redraw(true);
Out.AppendLine($"applied: {solids.Count} solids on {target}, Deswik volume total {tot:F1} m3, {bad} solids where Deswik's volume or closure disagrees");
return null;
