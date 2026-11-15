// Replace the selected strips on 0\DESIGN MASTER\STRIPPING with the 8 m decline-following strip design.
// For each selected strip: parent drive = centreline on 0\DESIGN MASTER\CENTRELINES with the strip's Name, nearest to it;
// stockpile = another centreline on that layer with an end within 6 m of the parent, nearest to the strip.
// The new strip follows the parent's curve and grade (8 m high section, 5.0 m wide, +1.5 m above backs, opening flush
// with one end, one fillet, 3.5 m ramps at both ends and the front), takes the old strip's layer and attributes, and the
// old strip is removed. APPLY = false only reports what it finds.
const bool APPLY = false; const int FILLET_SIDE = 1;
const string CL = "0\\DESIGN MASTER\\CENTRELINES";
const string STYLE_LAYER = "0\\REFERENCE\\STRIP STANDARDS\\TRUCK LOADING TRANSITIONS";
double rise = 1.5, ramp = 3.5, fil = 2.5, step = 0.5, L8 = 8.0;
var inv = System.Globalization.CultureInfo.InvariantCulture;

List<Deswik.Graphics.Geometry.Point> Pts(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => f.asPolyline.GetVertex(i)).ToList();
(double s, double d, double x, double y) Proj(List<Deswik.Graphics.Geometry.Point> D, double x, double y)
{
    double bs = 0, bd = 1e9, run = 0, bx = 0, by = 0;
    for (int i = 1; i < D.Count; i++)
    {
        double ax = D[i - 1].x, ay = D[i - 1].y, vx = D[i].x - ax, vy = D[i].y - ay, L2 = vx * vx + vy * vy, L = Math.Sqrt(L2);
        double t = L2 > 0 ? Math.Max(0, Math.Min(1, ((x - ax) * vx + (y - ay) * vy) / L2)) : 0;
        double px = ax + t * vx, py = ay + t * vy, d = Math.Sqrt((px - x) * (px - x) + (py - y) * (py - y));
        if (d < bd) { bd = d; bs = run + t * L; bx = px; by = py; }
        run += L;
    }
    return (bs, bd, bx, by);
}
double Prof(Deswik.Graphics.Primaries.Figure f, string unit, double def)
{
    var m = System.Text.RegularExpressions.Regex.Match((f.AttributeValueGet("PROFILE", "") ?? "").ToString(), @"([\d.]+)\s*m" + unit);
    return m.Success ? double.Parse(m.Groups[1].Value, inv) : def;
}

var sel = App.Selections.MainSelection.Cast<Deswik.Graphics.Primaries.Figure>().Where(f => f.IsPolyline && f.Layer != null && f.Layer.Name.EndsWith("\\STRIPPING")).ToList();
if (sel.Count == 0)   // selection lost: use the four strips picked earlier, found by their centre
{
    var picks = new[] { (8136.7, 62973.2), (8103.1, 63019.8), (8060.3, 63099.9), (8068.6, 63025.2) };
    foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
    {
        if (f.Deleted || f.Layer == null || f.Layer.Name != "0\\DESIGN MASTER\\STRIPPING" || !f.IsPolyline) continue;
        var m = f.BoundingBox.Midpoint;
        if (picks.Any(p => Math.Abs(p.Item1 - m.x) < 0.1 && Math.Abs(p.Item2 - m.y) < 0.1)) sel.Add(f);
    }
    Out.AppendLine($"selection was empty - using the {sel.Count} strips picked earlier");
}
if (sel.Count == 0) { Out.AppendLine("No strips found - nothing changed."); return null; }
var cls = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (!f.Deleted && f.Layer != null && f.Layer.Name == CL && f.IsPolyline && f.asPolyline.VertexCount >= 2) cls.Add(f);
// style: each new panel is a copy of the old strip (red hatch, its attributes); ramps get the grey hatch (colour 253)
Deswik.Graphics.Objects.Color Grey(Deswik.Graphics.Objects.Color like) { var g = new Deswik.Graphics.Objects.Color(); g.ColorIndex = 253; if (like != null) g.Transparency = like.Transparency; return g; }
string[] copy = { "ACT_TYPE", "Name", "LEVEL", "MINE", "ZONE", "LODE", "DESIGN", "Design Status", "MPA", "DIRECTION", "GSS", "PROFILE", "Description", "LAT_VERT", "STO_DEV", "COST_CENTRE", "Control", "SUPPORT", "STATUS", "MINING" };

int built = 0;
foreach (var old in sel)
{
    var op = Pts(old); double cx = op.Average(q => q.x), cy = op.Average(q => q.y);
    string name = (old.AttributeValueGet("Name", "") ?? "").ToString();
    var dec = cls.Where(f => (f.AttributeValueGet("Name", "") ?? "").ToString() == name).OrderBy(f => Proj(Pts(f), cx, cy).d).FirstOrDefault();
    if (dec == null || Proj(Pts(dec), cx, cy).d > 8)   // fall back to the nearest decline centreline
        dec = cls.Where(f => (f.AttributeValueGet("ACT_TYPE", "") ?? "").ToString() == (old.AttributeValueGet("ACT_TYPE", "") ?? "").ToString()).OrderBy(f => Proj(Pts(f), cx, cy).d).FirstOrDefault();
    if (dec == null || Proj(Pts(dec), cx, cy).d > 8) { Out.AppendLine($"[{name}] no centreline within 8 m - skipped"); continue; }
    if ((dec.AttributeValueGet("Name", "") ?? "").ToString() != name) Out.AppendLine($"[{name}] using nearest centreline {dec.AttributeValueGet("Name", "?")}");
    var D = Pts(dec);
    // stockpile: another centreline with an end within 6 m of the parent, nearest to the strip
    Deswik.Graphics.Primaries.Figure stk = null; double best = 1e9; bool fromStart = true;
    foreach (var f in cls)
    {
        if (f == dec || (f.AttributeValueGet("Name", "") ?? "").ToString() == name) continue;
        var K = Pts(f);
        foreach (var (e, st) in new[] { (K[0], true), (K[^1], false) })
        {
            var pr = Proj(D, e.x, e.y); if (pr.d > 10) continue;
            double dd = Math.Sqrt((e.x - cx) * (e.x - cx) + (e.y - cy) * (e.y - cy));
            if (dd < best) { best = dd; stk = f; fromStart = st; }
        }
    }
    if (stk == null || best > 20)   // no stockpile: a fan strip - ramp, high section, ramp, centred where the old strip was
    {
        double Wd0 = Prof(dec, "W", 5.5), Hd0 = Prof(dec, "H", 5.7), hw0 = Wd0 / 2 - 0.25;
        var S0 = new List<double> { 0 }; for (int i = 1; i < D.Count; i++) S0.Add(S0[i - 1] + Math.Sqrt(Math.Pow(D[i].x - D[i - 1].x, 2) + Math.Pow(D[i].y - D[i - 1].y, 2)));
        var st = op.Select(q => Proj(D, q.x, q.y).s).ToList(); double sm = (st.Min() + st.Max()) / 2, oldLen = st.Max() - st.Min();
        double Lf = oldLen <= 11.5 ? 8.0 : 15.0, f0 = sm - Lf / 2, f1 = sm + Lf / 2;
        Out.AppendLine($"[{name}] fan strip on {dec.AttributeValueGet("Name", "?")}: old strip {oldLen:F1} m long -> {(Lf == 8 ? "single" : "double")} fan, {Lf} m high section at station {sm:F1} m");
        if (f0 - ramp < 0 || f1 + ramp > S0[^1]) { Out.AppendLine("   would run past the end of the centreline - skipped"); continue; }
        if (!APPLY) continue;
        (double x, double y, double z, double tx, double ty) At0(double s)
        {
            int i = 1; while (i < D.Count - 1 && S0[i] < s) i++;
            double L = S0[i] - S0[i - 1], t = L > 0 ? (s - S0[i - 1]) / L : 0, tx = (D[i].x - D[i - 1].x) / L, ty = (D[i].y - D[i - 1].y) / L;
            return (D[i - 1].x + t * (D[i].x - D[i - 1].x), D[i - 1].y + t * (D[i].y - D[i - 1].y), D[i - 1].z + t * (D[i].z - D[i - 1].z), tx, ty);
        }
        double[] P0(double s, double off, double up) { var c = At0(s); return new[] { c.x - c.ty * off, c.y + c.tx * off, c.z + Hd0 + up }; }
        IEnumerable<double> St0(double a, double b) { int n = Math.Max(1, (int)Math.Ceiling((b - a) / step)); return Enumerable.Range(0, n + 1).Select(i => a + (b - a) * i / n); }
        double Up0(double s) => s < f0 ? rise * (s - (f0 - ramp)) / ramp : s > f1 ? rise * ((f1 + ramp) - s) / ramp : rise;
        List<double[]> Band0(double a, double b) { var L = St0(a, b).Select(s => P0(s, -hw0, Up0(s))).ToList(); L.AddRange(St0(a, b).Reverse().Select(s => P0(s, hw0, Up0(s)))); return L; }
        foreach (var (pts, isRamp) in new[] { (Band0(f0 - ramp, f0), true), (Band0(f0, f1), false), (Band0(f1, f1 + ramp), true) })
        {
            var c = (Deswik.Graphics.Primaries.Figure)old.Clone(); var p = c.asPolyline;
            if (isRamp && p.HatchProperties != null) p.HatchProperties.FillBackColor = Grey(p.HatchProperties.FillBackColor);
            while (p.VertexCount > 0) p.RemoveVertexAt(0);
            foreach (var q in pts) p.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]);
            p.Closed = true; c.Layer = old.Layer; c.AttributeValueSet("CLAUDE_DECLINE_STRIP", "1");
            App.ModelSpace.Entities.AddItem(c, null, null, true, false);
        }
        old.Deleted = true; built++; Out.AppendLine("   replaced with 3 panels");
        continue;
    }
    var Kp = Pts(stk); var E = fromStart ? Kp[0] : Kp[^1]; var E2 = fromStart ? Kp[1] : Kp[^2];
    var pe = Proj(D, E.x, E.y); double s0 = pe.s, ed = pe.d;
    double Wd = Prof(dec, "W", 5.5), Hd = Prof(dec, "H", 5.7), Ws = Prof(stk, "W", 5.5), Hs = Prof(stk, "H", 5.7), hw = Wd / 2 - 0.25, Wi = Ws - 0.5;
    var S = new List<double> { 0 }; for (int i = 1; i < D.Count; i++) S.Add(S[i - 1] + Math.Sqrt(Math.Pow(D[i].x - D[i - 1].x, 2) + Math.Pow(D[i].y - D[i - 1].y, 2)));
    (double x, double y, double z, double tx, double ty) At(double s)
    {
        int i = 1; while (i < D.Count - 1 && S[i] < s) i++;
        double L = S[i] - S[i - 1], t = L > 0 ? (s - S[i - 1]) / L : 0, tx = (D[i].x - D[i - 1].x) / L, ty = (D[i].y - D[i - 1].y) / L;
        return (D[i - 1].x + t * (D[i].x - D[i - 1].x), D[i - 1].y + t * (D[i].y - D[i - 1].y), D[i - 1].z + t * (D[i].z - D[i - 1].z), tx, ty);
    }
    double dlen = Math.Sqrt(Math.Pow(E2.x - E.x, 2) + Math.Pow(E2.y - E.y, 2)), Dx = (E2.x - E.x) / dlen, Dy = (E2.y - E.y) / dlen, sGrade = (E2.z - E.z) / dlen;
    var c0 = At(s0); int side = Math.Sign(Dx * -c0.ty + Dy * c0.tx); if (side == 0) side = 1;
    double o0 = s0 - Wi / 2, o1 = s0 + Wi / 2, h0, h1; var fillets = new List<(double a, double b)>();
    int fs = FILLET_SIDE; if (fs > 0 ? o0 + L8 + ramp > S[^1] : o1 - L8 - ramp < 0) fs = -fs;   // put the fillet on the side that fits
    if (fs > 0) { h0 = o0; h1 = o0 + L8; fillets.Add((o1, h1)); } else { h1 = o1; h0 = o1 - L8; fillets.Add((h0, o0)); }
    double gradeR = (At(Math.Min(h1 + ramp, S[^1])).z - At(Math.Max(h0 - ramp, 0)).z) / (h1 - h0 + 2 * ramp);
    Out.AppendLine($"[{name}] parent {dec.AttributeValueGet("Name", "?")} (fillet {(fs > 0 ? "up" : "down")}-station, {Wd}W x {Hd}H, grade 1:{Math.Abs(1 / gradeR):F0}), stockpile {stk.AttributeValueGet("Name", "?")} ({Ws}W x {Hs}H) at station {s0:F1} m, {(side > 0 ? "left" : "right")} side; old strip {op.Count} pts on {old.Layer.Name}");
    if (h0 - ramp < 0 || h1 + ramp > S[^1]) { Out.AppendLine($"   strip would run past the end of {name} - skipped"); continue; }
    if (!APPLY) continue;

    double[] Pt(double s, double off, double up) { var c = At(s); double nx = -c.ty * side, ny = c.tx * side; return new[] { c.x + nx * off, c.y + ny * off, c.z + Hd + up }; }
    IEnumerable<double> St(double a, double b) { int n = Math.Max(1, (int)Math.Ceiling((b - a) / step)); return Enumerable.Range(0, n + 1).Select(i => a + (b - a) * i / n); }
    double Up(double s) => s < h0 ? rise * (s - (h0 - ramp)) / ramp : s > h1 ? rise * ((h1 + ramp) - s) / ramp : rise;
    List<double[]> Band(double a, double b) { var L = St(a, b).Select(s => Pt(s, -hw, Up(s))).ToList(); L.AddRange(St(a, b).Reverse().Select(s => Pt(s, hw, Up(s)))); return L; }
    double[] Out_(double s, double d, double up) { var w = Pt(s, hw, rise); return new[] { w[0] + Dx * d, w[1] + Dy * d, w[2] - rise + up }; }
    var panels = new List<(List<double[]> pts, bool ramp)> { (Band(h0 - ramp, h0), true), (Band(h0, h1), false), (Band(h1, h1 + ramp), true) };
    var open = St(o0, o1).Select(s => Pt(s, hw, rise)).ToList(); open.Add(Out_(o1, fil, rise)); open.Add(Out_(o0, fil, rise)); panels.Add((open, false));
    foreach (var (a, b) in fillets) { var fp = St(a, b).Select(s => Pt(s, hw, rise)).ToList(); fp.Add(Out_(a == o1 ? o1 : o0, fil, rise)); panels.Add((fp, false)); }
    var w0 = Out_(o0, fil, rise); var w1 = Out_(o1, fil, rise); double dz = E.z + sGrade * (fil + ramp + ed) + Hs;
    panels.Add((new List<double[]> { w0, w1, new[] { w1[0] + Dx * ramp, w1[1] + Dy * ramp, dz }, new[] { w0[0] + Dx * ramp, w0[1] + Dy * ramp, dz } }, true));
    foreach (var (pts, isRamp) in panels)
    {
        var c = (Deswik.Graphics.Primaries.Figure)old.Clone(); var p = c.asPolyline;
        if (isRamp && p.HatchProperties != null) p.HatchProperties.FillBackColor = Grey(p.HatchProperties.FillBackColor);
        while (p.VertexCount > 0) p.RemoveVertexAt(0);
        foreach (var q in pts) p.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]);
        p.Closed = true; c.Layer = old.Layer;
        foreach (var a in copy) { var val = old.AttributeValueGet(a, null); if (val != null && val.ToString() != "") c.AttributeValueSet(a, val); }
        c.AttributeValueSet("CLAUDE_DECLINE_STRIP", "1");
        App.ModelSpace.Entities.AddItem(c, null, null, true, false);
    }
    old.Deleted = true; built++;
    Out.AppendLine($"   replaced with {panels.Count} panels");
}
if (APPLY) { App.Redraw(true); Out.AppendLine($"Replaced {built} of {sel.Count} strips"); }
return null;
