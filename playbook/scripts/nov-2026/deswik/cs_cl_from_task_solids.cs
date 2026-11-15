// MINE_UG_FY27 (06 Dec): centrelines for the SCHEDULED = 1 task solids (SCHEDULER\TASK SOLIDS, DIS Activity Type Tunnel), one
// per source string (DIS Parent ID), onto 0\06 SITE\03 ENGINEERING\01 DEV STRINGS\COMBINED 12.11.
// - source string still in the drawing: copied as it is (exact original geometry and attributes)
// - source string gone: rebuilt from its solids in cut order (DIS Activity Number): one vertex at the floor centre of each face two
//   cuts share, the two outer ends mirrored from the first/last cut (single cuts: long axis by fitting the cut length and the
//   PROFILE width); raises (LAT_VERT VERT) run from the bottom centre to the top centre of each lift
// - ends: a string end that points at another string (within MAXEXT in plan, ahead within 25 deg, RL within 2.5 m) is extended
//   to meet it, so take-offs and break-throughs connect
// Stopes (DIS Activity Type Solid) are left out. Dry run unless APPLY; the dry run also rebuilds the existing originals and
// reports how close the rebuild comes. Nothing is deleted. Does not save.
const bool APPLY = false; const double MAXEXT = 3.0;
const string TGT = "0\\06 SITE\\03 ENGINEERING\\01 DEV STRINGS\\COMBINED 12.11";
var inv = System.Globalization.CultureInfo.InvariantCulture;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
if (!App.Filename.EndsWith("06 Dec\\MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW " + App.Filename); return null; }
var H = new Dictionary<string, Deswik.Graphics.Primaries.Figure>(StringComparer.OrdinalIgnoreCase); var sol = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) { if (f.Deleted || f.Layer == null) continue; H[f.Handle] = f; if (f.Layer.Name == "SCHEDULER\\TASK SOLIDS" && f.IsPolyface && A(f, "SCHEDULED") == "1" && A(f, "DIS Activity Type") == "Tunnel") sol.Add(f); }
Deswik.Graphics.Primaries.Layer LT = null; foreach (Deswik.Graphics.Primaries.Layer l in App.Layers) if (l.Name == TGT) LT = l;
if (LT == null) { Out.AppendLine("target layer not found - nothing done"); return null; }
List<double[]> V(Deswik.Graphics.Primaries.Figure f) { var r = new List<double[]>(); foreach (var o in (System.Collections.IEnumerable)f.asPolyface.VertexList) { var t = o.GetType(); r.Add(new[] { Convert.ToDouble(t.GetProperty("x").GetValue(o)), Convert.ToDouble(t.GetProperty("y").GetValue(o)), Convert.ToDouble(t.GetProperty("z").GetValue(o)) }); } return r; }
double D2(double[] a, double[] b) => Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2));
double Num(string s, double d) { double v; return double.TryParse(s, System.Globalization.NumberStyles.Float, inv, out v) ? v : d; }
double ProfW(string p) { var m = System.Text.RegularExpressions.Regex.Match(p ?? "", @"([\d.]+)\s*mW"); return m.Success ? Num(m.Groups[1].Value, 5.0) : 5.0; }
// floor centre of a set of vertices: plan centroid, lowest RL among them
double[] FloorC(List<double[]> pts) => new[] { pts.Average(p => p[0]), pts.Average(p => p[1]), pts.Min(p => p[2]) };
// one cut on its own: best axis so the along-extent matches the cut length and the across-extent the drive width
(double[] a, double[] b) Single(List<double[]> v, double len, double wid)
{
    double best = 1e9, bt = 0; for (int d = 0; d < 180; d++) { double t = d * Math.PI / 180, ux = Math.Sin(t), uy = Math.Cos(t); var al = v.Select(p => p[0] * ux + p[1] * uy).ToList(); var ac = v.Select(p => p[0] * uy - p[1] * ux).ToList(); double e = Math.Abs(al.Max() - al.Min() - len) + Math.Abs(ac.Max() - ac.Min() - wid); if (e < best) { best = e; bt = t; } }
    double vx = Math.Sin(bt), vy = Math.Cos(bt); var al2 = v.Select(p => p[0] * vx + p[1] * vy).ToList(); double lo = al2.Min(), hi = al2.Max();
    var e0 = v.Where(p => p[0] * vx + p[1] * vy < lo + 0.3).ToList(); var e1 = v.Where(p => p[0] * vx + p[1] * vy > hi - 0.3).ToList();
    return (FloorC(e0), FloorC(e1));
}
// cut order along the string: walk the chain of cuts that share a face, starting from the end with the lowest activity number
List<Deswik.Graphics.Primaries.Figure> Chain(List<Deswik.Graphics.Primaries.Figure> c)
{
    if (c.Count < 3) return c; var vs = c.Select(V).ToList(); int n = c.Count; var adj = new List<int>[n]; for (int i = 0; i < n; i++) adj[i] = new List<int>();
    for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) { int sh = vs[i].Count(p => vs[j].Any(q => Math.Abs(q[0] - p[0]) < 0.02 && Math.Abs(q[1] - p[1]) < 0.02 && Math.Abs(q[2] - p[2]) < 0.02)); if (sh >= 3) { adj[i].Add(j); adj[j].Add(i); } }
    if (adj.Any(a => a.Count > 2) || adj.Count(a => a.Count == 1) != 2) return c;   // not one simple chain: keep activity order
    int cur = Enumerable.Range(0, n).Where(i => adj[i].Count == 1).OrderBy(i => i).First(), prev = -1; var o = new List<Deswik.Graphics.Primaries.Figure>();
    while (cur >= 0) { o.Add(c[cur]); int nx = adj[cur].Where(k => k != prev).DefaultIfEmpty(-1).First(); prev = cur; cur = nx; }
    return o.Count == n ? o : c;
}// lateral run of touching cuts -> centreline; ends carried on at the floor gradient (end faces lean on a grade, so the
// furthest vertex is on the backs, not the floor)
List<double[]> RebuildRun(List<List<double[]>> vs, List<Deswik.Graphics.Primaries.Figure> cuts)
{
    if (vs.Count == 1) { var s = Single(vs[0], Num(A(cuts[0], "Metres"), 3.5), ProfW(A(cuts[0], "PROFILE"))); return new List<double[]> { s.a, s.b }; }
    var faces = new List<double[]>();
    for (int i = 1; i < vs.Count; i++)
    {
        var sh = vs[i].Where(p => vs[i - 1].Any(q => Math.Abs(q[0] - p[0]) < 0.02 && Math.Abs(q[1] - p[1]) < 0.02 && Math.Abs(q[2] - p[2]) < 0.02)).ToList();
        if (sh.Count < 3) { var ca = FloorC(vs[i - 1]); var cb = FloorC(vs[i]); faces.Add(new[] { (ca[0] + cb[0]) / 2, (ca[1] + cb[1]) / 2, (ca[2] + cb[2]) / 2 }); }
        else faces.Add(FloorC(sh));
    }
    // end straight on from face along (dx,dy), as far as the cut reaches in plan; RL from the floor gradient
    double[] EndAt(List<double[]> v, double[] face, double dx, double dy, double slope) { double mx = v.Max(p => (p[0] - face[0]) * dx + (p[1] - face[1]) * dy); return new[] { face[0] + mx * dx, face[1] + mx * dy, face[2] + slope * mx }; }
    // floor gradient of one cut, away from its face: line from the face floor to the cut's lowest vertex
    double CutSlope(List<double[]> v, double[] face, double dx, double dy) { var lo = v.OrderBy(p => p[2]).First(); double al = (lo[0] - face[0]) * dx + (lo[1] - face[1]) * dy; return Math.Abs(al) > 0.5 ? (lo[2] - face[2]) / al : 0; }
    if (faces.Count == 1)
    {   // two cuts: one straight line along the second cut's axis, through the shared face
        var f0 = faces[0]; var cl = FloorC(vs[1]); double dx = cl[0] - f0[0], dy = cl[1] - f0[1], dl = Math.Sqrt(dx * dx + dy * dy); dx /= dl; dy /= dl;
        double s1 = CutSlope(vs[1], f0, dx, dy), s0 = CutSlope(vs[0], f0, -dx, -dy);
        if (Math.Abs(s0) < 1e-9) s0 = -s1; if (Math.Abs(s1) < 1e-9) s1 = -s0;
        return new List<double[]> { EndAt(vs[0], f0, -dx, -dy, s0), f0, EndAt(vs[1], f0, dx, dy, s1) };
    }
    (double dx, double dy, double sl) Dir(double[] f, double[] from) { double dx = f[0] - from[0], dy = f[1] - from[1], dl = Math.Sqrt(dx * dx + dy * dy); return (dx / dl, dy / dl, (f[2] - from[2]) / dl); }
    var d0 = Dir(faces[0], faces[1]); var d1 = Dir(faces[^1], faces[^2]);
    var pts = new List<double[]> { EndAt(vs[0], faces[0], d0.dx, d0.dy, d0.sl) }; pts.AddRange(faces); pts.Add(EndAt(vs[^1], faces[^1], d1.dx, d1.dy, d1.sl)); return pts;
}
double MinGap(List<double[]> a, List<double[]> b) => a.Min(p => b.Min(q => Math.Sqrt(Math.Pow(p[0] - q[0], 2) + Math.Pow(p[1] - q[1], 2) + Math.Pow(p[2] - q[2], 2))));
int splitGaps = 0; var gapInfo = new List<string>();
List<List<double[]>> Rebuild(List<Deswik.Graphics.Primaries.Figure> cuts)
{
    var vs = cuts.Select(V).ToList();
    if (A(cuts[0], "LAT_VERT") == "VERT")
    {
        var pts = new List<double[]>();
        foreach (var v in vs.OrderBy(v => v.Min(p => p[2]))) { double zl = v.Min(p => p[2]), zh = v.Max(p => p[2]); var bot = v.Where(p => p[2] < zl + 0.3).ToList(); var top = v.Where(p => p[2] > zh - 0.3).ToList(); var b = new[] { bot.Average(p => p[0]), bot.Average(p => p[1]), zl }; var t = new[] { top.Average(p => p[0]), top.Average(p => p[1]), zh }; if (pts.Count == 0) pts.Add(b); pts.Add(t); }
        return new List<List<double[]>> { pts };
    }
    var res = new List<List<double[]>>(); int st = 0;
    for (int i = 1; i <= vs.Count; i++)
    {
        if (i < vs.Count) { double g = MinGap(vs[i - 1], vs[i]); if (g <= 1.0) continue; splitGaps++; gapInfo.Add($"{A(cuts[0], "Name")}: act {A(cuts[i - 1], "DIS Activity Number")} -> {A(cuts[i], "DIS Activity Number")} gap {g:F1} m"); }
        res.Add(RebuildRun(vs.GetRange(st, i - st), cuts.GetRange(st, i - st))); st = i;
    }
    return res;
}List<double[]> PL(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
double SegDist(double[] p, List<double[]> pl) { double b = 1e9; for (int i = 1; i < pl.Count; i++) { double vx = pl[i][0] - pl[i - 1][0], vy = pl[i][1] - pl[i - 1][1], L2 = vx * vx + vy * vy, t = L2 == 0 ? 0 : Math.Max(0, Math.Min(1, ((p[0] - pl[i - 1][0]) * vx + (p[1] - pl[i - 1][1]) * vy) / L2)); b = Math.Min(b, Math.Sqrt(Math.Pow(pl[i - 1][0] + t * vx - p[0], 2) + Math.Pow(pl[i - 1][1] + t * vy - p[1], 2))); } return b; }
// ---- build
var groups = sol.GroupBy(f => A(f, "DIS Parent ID")).ToList();
var strings = new List<(string key, List<double[]> pts, bool copied, Deswik.Graphics.Primaries.Figure tpl, Deswik.Graphics.Primaries.Figure first, bool vert)>();
var testDev = new List<double>(); int mixedSch = 0; int despiked = 0; int origReb = 0; var origRebInfo = new List<string>();
var schAll = new Dictionary<string, int>(); foreach (var f in H.Values) if (f.Layer.Name == "SCHEDULER\\TASK SOLIDS" && A(f, "SCHEDULED") != "1") { var k = A(f, "DIS Parent ID"); schAll.TryGetValue(k, out var c); schAll[k] = c + 1; }
foreach (var g in groups)
{
    var cuts = Chain(g.OrderBy(f => Num(A(f, "DIS Activity Number"), 0)).ToList()); if (schAll.ContainsKey(g.Key)) mixedSch++;
    bool vert = A(cuts[0], "LAT_VERT") == "VERT"; var pieces = Rebuild(cuts);
    H.TryGetValue(g.Key, out var par); if (par != null && !par.IsPolyline) par = null;
    bool useOrig = false;
    if (par != null)
    {
        var orig = PL(par); testDev.Add(pieces.SelectMany(x => x).Max(p => SegDist(p, orig)));
        // keep the original only when it follows the scheduled solids (interior points within 0.5 m) and none of its cuts are unscheduled
        var inner = pieces.SelectMany(x => x.Count > 2 ? x.Skip(1).Take(x.Count - 2) : x).ToList();
        double dev = vert ? 0 : inner.Max(p => SegDist(p, orig));
        double over = vert ? 0 : new[] { orig[0], orig[^1] }.Max(e => pieces.Min(pc => SegDist(e, pc)));   // original end past the scheduled solids
        useOrig = dev <= 0.5 && over <= 3.0 && !schAll.ContainsKey(g.Key);
        if (useOrig) strings.Add((g.Key, orig, true, par, cuts[0], vert));
        else { origReb++; origRebInfo.Add($"{A(cuts[0], "Name")} ({g.Key}): " + (schAll.ContainsKey(g.Key) ? "has unscheduled cuts" : dev > 0.5 ? $"off its solids by {dev:F1} m" : $"runs {over:F1} m past its scheduled solids")); }
    }
    if (!useOrig) foreach (var rb in pieces)
    {
        if (!vert) { bool again = true; while (again && rb.Count > 3) { again = false; for (int i = 1; i < rb.Count - 1; i++) { double a1 = Math.Atan2(rb[i][0] - rb[i - 1][0], rb[i][1] - rb[i - 1][1]), a2 = Math.Atan2(rb[i + 1][0] - rb[i][0], rb[i + 1][1] - rb[i][1]); double tdeg = Math.Abs(((a2 - a1) * 180 / Math.PI + 540) % 360 - 180); double off = SegDist(rb[i], new List<double[]> { rb[i - 1], rb[i + 1] }); if (tdeg > 25 && off < 1.0) { rb.RemoveAt(i); despiked++; again = true; break; } } } }
        strings.Add((g.Key, rb, false, par, cuts[0], vert));
    }
}
Out.AppendLine($"spike points removed from rebuilt strings: {despiked}"); Out.AppendLine($"originals rebuilt from their scheduled solids instead of copied: {origReb}"); foreach (var x in origRebInfo) Out.AppendLine("  " + x); Out.AppendLine($"rebuilt strings split where scheduled cuts do not touch: {splitGaps}"); foreach (var x in gapInfo) Out.AppendLine("  " + x);
Out.AppendLine($"SCH=1 tunnel solids {sol.Count} from {groups.Count} source strings: copied originals {strings.Count(s => s.copied)}, rebuilt {strings.Count(s => !s.copied)} (raises {strings.Count(s => s.vert)}); strings that also have SCH<>1 cuts: {mixedSch}");
if (testDev.Count > 0) Out.AppendLine($"rebuild check on the {testDev.Count} known originals: rebuilt vertices sit within {testDev.OrderBy(x => x).ElementAt(testDev.Count / 2):F2} m (median) / {testDev.OrderBy(x => x).ElementAt((int)(testDev.Count * 0.95)):F2} m (95%) / {testDev.Max():F2} m (max) of the original");
// ---- connect: extend string ends that point at another string
int ext = 0; var extLen = new List<double>();
var lat = strings.Where(s => !s.vert).Select(s => s.pts).ToList();
for (int si = 0; si < strings.Count; si++)
{
    if (strings[si].vert) continue; var p = strings[si].pts;
    foreach (var atEnd in new[] { false, true })
    {
        var e = atEnd ? p[^1] : p[0]; var e2 = atEnd ? p[^2] : p[1]; double dx = e[0] - e2[0], dy = e[1] - e2[1], dl = Math.Sqrt(dx * dx + dy * dy); if (dl < 1e-6) continue; dx /= dl; dy /= dl;
        double bestT = 1e9; double[] hit = null; double dl0 = dl;
        foreach (var q in lat)
        {
            if (ReferenceEquals(q, p)) continue;
            for (int i = 1; i < q.Count; i++)
            {
                // ray e + t*(dx,dy) against segment q[i-1]..q[i]
                double sx = q[i][0] - q[i - 1][0], sy = q[i][1] - q[i - 1][1], den = dx * sy - dy * sx; if (Math.Abs(den) < 1e-9) continue;
                double wx = q[i - 1][0] - e[0], wy = q[i - 1][1] - e[1]; double t = (wx * sy - wy * sx) / den, u = (wx * dy - wy * dx) / den;
                if (t < -0.01 || t > MAXEXT || u < 0 || u > 1) continue;
                double zq = q[i - 1][2] + u * (q[i][2] - q[i - 1][2]); double gr = (e[2] - e2[2]) / dl; if (Math.Abs(zq - (e[2] + gr * t)) > Math.Max(0.5, 0.15 * t)) continue;   // the other string must sit on this one's grade
                if (t < bestT) { bestT = t; hit = new[] { e[0] + t * dx, e[1] + t * dy, zq }; }
            }
        }
        if (hit == null)
        {   // no straight-on hit: nearest point on another drive within MAXEXT, inside a 30 deg cone ahead, same RL rule
            double gr = (e[2] - e2[2]) / dl;
            foreach (var q in lat) { if (ReferenceEquals(q, p)) continue; for (int i = 1; i < q.Count; i++) { double vx = q[i][0] - q[i - 1][0], vy = q[i][1] - q[i - 1][1], L2 = vx * vx + vy * vy; double u = L2 == 0 ? 0 : Math.Max(0, Math.Min(1, ((e[0] - q[i - 1][0]) * vx + (e[1] - q[i - 1][1]) * vy) / L2)); double fx = q[i - 1][0] + u * vx, fy = q[i - 1][1] + u * vy, fz = q[i - 1][2] + u * (q[i][2] - q[i - 1][2]); double d = Math.Sqrt(Math.Pow(fx - e[0], 2) + Math.Pow(fy - e[1], 2)); if (d < 0.05 || d > MAXEXT + 0.1) continue; double cosA = ((fx - e[0]) * dx + (fy - e[1]) * dy) / d; if (cosA < Math.Cos(60 * Math.PI / 180)) continue; if (Math.Abs(fz - (e[2] + gr * d)) > Math.Max(0.5, 0.15 * d) && Math.Abs(fz - e[2]) > 1.2) continue; if (d < bestT) { bestT = d; hit = new[] { fx, fy, fz }; } } }
        }
        if (hit != null && bestT > 0.05) { if (atEnd) p.Add(hit); else p.Insert(0, hit); ext++; extLen.Add(bestT); if (bestT > 3.0) Out.AppendLine($"  long extension {bestT:F2} m: {A(strings[si].first, "Name")} ({strings[si].key}, {(strings[si].copied ? "original" : "rebuilt")}) at its {(atEnd ? "end" : "start")} ({hit[0]:F1},{hit[1]:F1},{hit[2]:F1})"); }
    }
}
Out.AppendLine($"ends extended to meet another string: {ext}" + (extLen.Count > 0 ? $" (length {extLen.Min():F2}..{extLen.Max():F2} m, median {extLen.OrderBy(x => x).ElementAt(extLen.Count / 2):F2} m)" : ""));
// ---- raises: bottom / top onto the floor centreline of the drive they open into (within 4 m in plan, 1.0 m in RL)
int rsnap = 0; var rsInfo = new List<string>();
foreach (var s in strings.Where(s => s.vert))
{
    foreach (var atEnd in new[] { false, true })
    {
        var e = atEnd ? s.pts[^1] : s.pts[0]; double bd = 1e9; double[] foot = null;
        foreach (var q in lat) for (int i = 1; i < q.Count; i++)
        {
            double vx = q[i][0] - q[i - 1][0], vy = q[i][1] - q[i - 1][1], L2 = vx * vx + vy * vy; double t = L2 == 0 ? 0 : Math.Max(0, Math.Min(1, ((e[0] - q[i - 1][0]) * vx + (e[1] - q[i - 1][1]) * vy) / L2));
            double fx = q[i - 1][0] + t * vx, fy = q[i - 1][1] + t * vy, fz = q[i - 1][2] + t * (q[i][2] - q[i - 1][2]); double d = Math.Sqrt(Math.Pow(fx - e[0], 2) + Math.Pow(fy - e[1], 2));
            if (d <= 4.0 && Math.Abs(fz - e[2]) <= 6.0 && (atEnd ? fz > e[2] - 1.0 : fz < e[2] + 1.0) && d + Math.Abs(fz - e[2]) < bd) { bd = d + Math.Abs(fz - e[2]); foot = new[] { fx, fy, fz }; }
        }
        if (foot != null && D2(foot, e) + Math.Abs(foot[2] - e[2]) > 0.05) { bool mv = Math.Abs(foot[2] - e[2]) <= 1.0; if (atEnd) { if (mv) s.pts[^1] = foot; else s.pts.Add(foot); } else { if (mv) s.pts[0] = foot; else s.pts.Insert(0, foot); } rsnap++; rsInfo.Add($"{A(s.first, "Name")} {(atEnd ? "top" : "bottom")} moved {bd:F1} m in plan, {foot[2] - e[2]:+0.00;-0.00} m RL"); }
    }
}
Out.AppendLine($"raise ends put on the drive floor centreline: {rsnap}"); foreach (var x in rsInfo.Where(x => x.Contains("W368") || x.Contains("H66 EWR"))) Out.AppendLine("  " + x);double Turn(double[] a, double[] b, double[] c) { double a1 = Math.Atan2(b[0] - a[0], b[1] - a[1]), a2 = Math.Atan2(c[0] - b[0], c[1] - b[1]); double d = (a2 - a1) * 180 / Math.PI; while (d > 180) d -= 360; while (d < -180) d += 360; return Math.Abs(d); }
var kinks = strings.Where(s => !s.vert && s.pts.Count >= 3 && (Turn(s.pts[0], s.pts[1], s.pts[2]) > 10 || Turn(s.pts[^3], s.pts[^2], s.pts[^1]) > 10)).ToList();
Out.AppendLine($"strings with a turn over 10 deg at an end: {kinks.Count} (copied {kinks.Count(k => k.copied)}, rebuilt {kinks.Count(k => !k.copied)})" + (kinks.Count > 0 ? ": " + string.Join(", ", kinks.Take(15).Select(k => A(k.first, "Name") + (k.copied ? " (orig)" : ""))) : ""));
var byType = strings.GroupBy(s => A(s.first, "ACT_TYPE")).OrderByDescending(g => g.Count()).Select(g => g.Key + " " + g.Count());
Out.AppendLine("strings by ACT_TYPE: " + string.Join(", ", byType));
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }
// ---- write: copies of the originals; rebuilt strings use a copied original as a template and take the solids' attributes
int cleared = 0; foreach (var f in H.Values.ToList()) if (f.Layer == LT && f.IsPolyline && A(f, "SOURCE_STRING") != "") { f.Deleted = true; cleared++; } Out.AppendLine($"removed the previous {cleared} centrelines from {TGT}");
var tplAny = strings.FirstOrDefault(s => s.copied).tpl; if (tplAny == null) { Out.AppendLine("no template string - nothing done"); return null; }
var fields = new[] { "Name", "ACT_TYPE", "Description", "PROFILE", "GSS", "LEVEL", "MINE", "LAT_VERT", "STO_DEV", "COST_CENTRE", "ZONE", "LODE", "RECOVERY", "DILUTION", "SCHEDULED", "SLOT_FLG", "SLOT_TYPE", "INT_CABLES", "STOPE TYPE", "STAGE" };
int nNew = 0;
foreach (var s in strings)
{
    var src = s.tpl ?? tplAny; var k = (Deswik.Graphics.Primaries.Figure)src.Clone(); k.Deleted = false; k.Layer = LT; var pl = k.asPolyline; pl.Closed = false;
    while (pl.VertexCount > 0) pl.RemoveVertexAt(0); foreach (var q in s.pts) pl.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]);
    if (!s.copied) foreach (var fld in fields) { var v = A(s.first, fld); if (v != "") k.AttributeValueSet(fld, v); }
    k.AttributeValueSet("Name", A(s.first, "Name")); k.AttributeValueSet("SOURCE_STRING", s.key);
    App.ModelSpace.Entities.AddItem(k, null, null, true, false); nNew++;
}
App.Redraw(true);
Out.AppendLine($"applied: {nNew} centrelines added on {TGT}; NOT saved");
return null;
