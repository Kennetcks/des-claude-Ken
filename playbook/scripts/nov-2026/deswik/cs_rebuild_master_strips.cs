// Rebuild the four Design Master strips cleanly. Removes the panels generated earlier (tagged CLAUDE_DECLINE_STRIP),
// brings back the four original strips (by their centre), then for each builds the strip on its drive centreline:
// - edges offset with a smoothed tangent (no jumps where the strip crosses a centreline bend point)
// - curved panels made as ~1 m slices so each is near-flat and the hatch sits on the outline
// - truck loading: 8 m high section, opening flush with one end, one fillet, 3.5 m ramps at both ends and a flat front ramp
// - fan (no stockpile meeting it): 8 m single / 15 m double, centred where the old strip was, 3.5 m ramps
// Ramps grey (colour 253) at 50 % transparency; everything else keeps the old strip's red style and attributes.
const bool APPLY = true; const int FILLET_SIDE = 1;
const string CL = "0\\DESIGN MASTER\\CENTRELINES", SL = "0\\DESIGN MASTER\\STRIPPING";
double rise = 1.5, ramp = 3.5, fil = 2.5, step = 0.5, L8 = 8.0;
var picks = new[] { (8136.7, 62973.2), (8103.1, 63019.8), (8060.3, 63099.9), (8068.6, 63025.2) };
var inv = System.Globalization.CultureInfo.InvariantCulture;
List<Deswik.Graphics.Geometry.Point> Pts(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => f.asPolyline.GetVertex(i)).ToList();
(double s, double d) Proj(List<Deswik.Graphics.Geometry.Point> D, double x, double y)
{
    double bs = 0, bd = 1e9, run = 0;
    for (int i = 1; i < D.Count; i++)
    {
        double ax = D[i - 1].x, ay = D[i - 1].y, vx = D[i].x - ax, vy = D[i].y - ay, L2 = vx * vx + vy * vy, L = Math.Sqrt(L2);
        double t = L2 > 0 ? Math.Max(0, Math.Min(1, ((x - ax) * vx + (y - ay) * vy) / L2)) : 0;
        double d = Math.Sqrt(Math.Pow(ax + t * vx - x, 2) + Math.Pow(ay + t * vy - y, 2));
        if (d < bd) { bd = d; bs = run + t * L; }
        run += L;
    }
    return (bs, bd);
}
double Prof(Deswik.Graphics.Primaries.Figure f, string unit, double def)
{
    var m = System.Text.RegularExpressions.Regex.Match((f.AttributeValueGet("PROFILE", "") ?? "").ToString(), @"([\d.]+)\s*m" + unit);
    return m.Success ? double.Parse(m.Groups[1].Value, inv) : def;
}
Deswik.Graphics.Objects.Color Grey() { var g = new Deswik.Graphics.Objects.Color(); g.ColorIndex = 253; g.Transparency = 128; return g; }

// --- reset: remove generated panels, restore the four originals
var gen = new List<Deswik.Graphics.Primaries.Figure>(); var olds = new Dictionary<int, Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Layer == null || f.Layer.Name != SL || !f.IsPolyline) continue;
    if (!f.Deleted && (f.AttributeValueGet("CLAUDE_DECLINE_STRIP", "") ?? "").ToString() == "1") { gen.Add(f); continue; }
    var m = f.BoundingBox.Midpoint;
    for (int k = 0; k < picks.Length; k++) if (Math.Abs(picks[k].Item1 - m.x) < 0.1 && Math.Abs(picks[k].Item2 - m.y) < 0.1 && (f.AttributeValueGet("CLAUDE_DECLINE_STRIP", "") ?? "").ToString() != "1") olds[k] = f;
}
if (olds.Count != 4) { Out.AppendLine($"Found {olds.Count} of the 4 original strips - nothing changed."); return null; }
var cls = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (!f.Deleted && f.Layer != null && f.Layer.Name == CL && f.IsPolyline && f.asPolyline.VertexCount >= 2) cls.Add(f);
foreach (var f in gen) f.Deleted = true;
Out.AppendLine($"Removed {gen.Count} generated panels; using the 4 original strips");

int built = 0;
foreach (var old in olds.Values)
{
    var op = Pts(old); double cx = op.Average(q => q.x), cy = op.Average(q => q.y);
    string name = (old.AttributeValueGet("Name", "") ?? "").ToString();
    var dec = cls.Where(f => (f.AttributeValueGet("Name", "") ?? "").ToString() == name).OrderBy(f => Proj(Pts(f), cx, cy).d).FirstOrDefault();
    if (dec == null || Proj(Pts(dec), cx, cy).d > 8)
        dec = cls.Where(f => (f.AttributeValueGet("ACT_TYPE", "") ?? "").ToString() == (old.AttributeValueGet("ACT_TYPE", "") ?? "").ToString()).OrderBy(f => Proj(Pts(f), cx, cy).d).FirstOrDefault();
    if (dec == null || Proj(Pts(dec), cx, cy).d > 8) { Out.AppendLine($"[{name}] no centreline within 8 m - old strip kept"); old.Deleted = false; continue; }
    var D = Pts(dec);
    var S = new List<double> { 0 }; for (int i = 1; i < D.Count; i++) S.Add(S[i - 1] + Math.Sqrt(Math.Pow(D[i].x - D[i - 1].x, 2) + Math.Pow(D[i].y - D[i - 1].y, 2)));
    double[] Pos(double s)
    {
        s = Math.Max(0, Math.Min(S[^1], s)); int i = 1; while (i < D.Count - 1 && S[i] < s) i++;
        double L = S[i] - S[i - 1], t = L > 0 ? (s - S[i - 1]) / L : 0;
        return new[] { D[i - 1].x + t * (D[i].x - D[i - 1].x), D[i - 1].y + t * (D[i].y - D[i - 1].y), D[i - 1].z + t * (D[i].z - D[i - 1].z) };
    }
    (double tx, double ty) Tan(double s) { var a = Pos(s - 1.0); var b = Pos(s + 1.0); double l = Math.Sqrt(Math.Pow(b[0] - a[0], 2) + Math.Pow(b[1] - a[1], 2)); return ((b[0] - a[0]) / l, (b[1] - a[1]) / l); }  // smoothed over 2 m
    double Wd = Prof(dec, "W", 5.5), Hd = Prof(dec, "H", 5.7), hw = Wd / 2 - 0.25;
    IEnumerable<double> St(double a, double b) { int n = Math.Max(1, (int)Math.Ceiling((b - a) / step)); return Enumerable.Range(0, n + 1).Select(i => a + (b - a) * i / n); }
    var panels = new List<(List<double[]> pts, bool ramp)>();
    int side = 1; double hi0, hi1;
    double[] Pt(double s, double off, double up) { var c = Pos(s); var t = Tan(s); return new[] { c[0] - t.ty * side * off, c[1] + t.tx * side * off, c[2] + Hd + up }; }
    void Slices(double a, double b, Func<double, double> up, bool isRamp)
    {
        var ss = St(a, b).ToList();
        for (int i = 0; i < ss.Count - 1; i += 2)
        {
            int j = Math.Min(i + 2, ss.Count - 1); var q = new List<double[]>();
            for (int k = i; k <= j; k++) q.Add(Pt(ss[k], -hw, up(ss[k])));
            for (int k = j; k >= i; k--) q.Add(Pt(ss[k], hw, up(ss[k])));
            panels.Add((q, isRamp));
        }
    }
    // stockpile meeting the drive near the strip
    Deswik.Graphics.Primaries.Figure stk = null; double best = 1e9; bool fromStart = true;
    foreach (var f in cls)
    {
        if (f == dec || (f.AttributeValueGet("Name", "") ?? "").ToString() == (dec.AttributeValueGet("Name", "") ?? "").ToString()) continue;
        var K = Pts(f);
        foreach (var (e, st) in new[] { (K[0], true), (K[^1], false) })
        {
            if (Proj(D, e.x, e.y).d > 10) continue;
            double dd = Math.Sqrt((e.x - cx) * (e.x - cx) + (e.y - cy) * (e.y - cy)); if (dd < best) { best = dd; stk = f; fromStart = st; }
        }
    }
    string kind;
    if (stk == null || best > 20)
    {   // fan strip
        var st = op.Select(q => Proj(D, q.x, q.y).s).ToList(); double sm = (st.Min() + st.Max()) / 2, Lf = st.Max() - st.Min() <= 11.5 ? 8.0 : 15.0;
        hi0 = sm - Lf / 2; hi1 = sm + Lf / 2;
        if (hi0 - ramp < 0 || hi1 + ramp > S[^1]) { Out.AppendLine($"[{name}] fan strip would run past the end of the centreline - old strip kept"); old.Deleted = false; continue; }
        Slices(hi0 - ramp, hi0, s => rise * (s - (hi0 - ramp)) / ramp, true); Slices(hi0, hi1, s => rise, false); Slices(hi1, hi1 + ramp, s => rise * ((hi1 + ramp) - s) / ramp, true);
        kind = $"{(Lf == 8 ? "single" : "double")} fan strip, {Lf} m";
    }
    else
    {   // truck loading
        var Kp = Pts(stk); var E = fromStart ? Kp[0] : Kp[^1]; var E2 = fromStart ? Kp[1] : Kp[^2];
        var pe = Proj(D, E.x, E.y); double s0 = pe.s, ed = pe.d;
        double Ws = Prof(stk, "W", 5.5), Hs = Prof(stk, "H", 5.7), Wi = Ws - 0.5;
        double dl = Math.Sqrt(Math.Pow(E2.x - E.x, 2) + Math.Pow(E2.y - E.y, 2)), Dx = (E2.x - E.x) / dl, Dy = (E2.y - E.y) / dl, sg = (E2.z - E.z) / dl;
        var t0 = Tan(s0); side = Math.Sign(Dx * -t0.ty + Dy * t0.tx); if (side == 0) side = 1;
        double o0 = s0 - Wi / 2, o1 = s0 + Wi / 2; int fs = FILLET_SIDE; if (fs > 0 ? o0 + L8 + ramp > S[^1] : o1 - L8 - ramp < 0) fs = -fs;
        var fl = new List<(double a, double b)>();
        if (fs > 0) { hi0 = o0; hi1 = o0 + L8; fl.Add((o1, hi1)); } else { hi1 = o1; hi0 = o1 - L8; fl.Add((hi0, o0)); }
        if (hi0 - ramp < 0 || hi1 + ramp > S[^1]) { Out.AppendLine($"[{name}] truck strip would run past the end of the centreline - old strip kept"); old.Deleted = false; continue; }
        double a0 = hi0, a1 = hi1;
        Slices(a0 - ramp, a0, s => rise * (s - (a0 - ramp)) / ramp, true); Slices(a0, a1, s => rise, false); Slices(a1, a1 + ramp, s => rise * ((a1 + ramp) - s) / ramp, true);
        double[] Out_(double s, double d) { var w = Pt(s, hw, rise); return new[] { w[0] + Dx * d, w[1] + Dy * d, w[2] }; }
        var open = St(o0, o1).Select(s => Pt(s, hw, rise)).ToList(); open.Add(Out_(o1, fil)); open.Add(Out_(o0, fil)); panels.Add((open, false));
        foreach (var (a, b) in fl) { var fp = St(a, b).Select(s => Pt(s, hw, rise)).ToList(); fp.Add(Out_(a == o1 ? o1 : o0, fil)); panels.Add((fp, false)); }
        // flat front ramp: bottom edge parallel to the top edge, dropping to the stockpile backs on average
        var w0 = Out_(o0, fil); var w1 = Out_(o1, fil);
        double low = E.z + sg * (fil + ramp + ed) + Hs, drop = (w0[2] + w1[2]) / 2 - low;
        panels.Add((new List<double[]> { w0, w1, new[] { w1[0] + Dx * ramp, w1[1] + Dy * ramp, w1[2] - drop }, new[] { w0[0] + Dx * ramp, w0[1] + Dy * ramp, w0[2] - drop } }, true));
        kind = $"truck loading strip at {stk.AttributeValueGet("Name", "?")}, fillet {(fs > 0 ? "up" : "down")}-station";
    }
    foreach (var (pts, isRamp) in panels)
    {
        var c = (Deswik.Graphics.Primaries.Figure)old.Clone(); var p = c.asPolyline;
        if (isRamp && p.HatchProperties != null) { var hp = p.HatchProperties; hp.FillBackColor = Grey(); hp.Solid2DTransparency = 128; p.HatchProperties = hp; }
        while (p.VertexCount > 0) p.RemoveVertexAt(0);
        foreach (var q in pts) p.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]);
        p.Closed = true; c.Layer = old.Layer; c.AttributeValueSet("CLAUDE_DECLINE_STRIP", "1");
        App.ModelSpace.Entities.AddItem(c, null, null, true, false);
    }
    old.Deleted = true; built++;
    Out.AppendLine($"[{name}] on {dec.AttributeValueGet("Name", "?")}: {kind}, {panels.Count} panels");
}
App.Redraw(true);
Out.AppendLine($"Rebuilt {built} of 4 strips");
return null;
