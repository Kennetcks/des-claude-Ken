const bool APPLY = false;
List<double[]> P3(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
double D2(double[] a, double[] b) => Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2));
double D3(double[] a, double[] b) => Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2) + Math.Pow(a[2] - b[2], 2));
double Rad(double d) => d * Math.PI / 180;
double Az(double[] a, double[] b) { var z = Math.Atan2(b[0] - a[0], b[1] - a[1]) * 180 / Math.PI; return z < 0 ? z + 360 : z; }
double Turn(double a1, double a2) { var d = a2 - a1; while (d > 180) d -= 360; while (d < -180) d += 360; return d; }
var H = new Dictionary<string, Deswik.Graphics.Primaries.Figure>(StringComparer.OrdinalIgnoreCase); var polys = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (!f.Deleted && f.Layer != null) { H[f.Handle] = f; if (f.IsPolyline) polys.Add(f); }
foreach (var h in new[] { "44BD9C0", "4588F43", "4588FB1", "4588F49" }) if (!H.ContainsKey(h)) { Out.AppendLine($"{h} missing - nothing done"); return null; }
var M = P3(H["44BD9C0"]); var d = P3(H["4588F43"]);
var CL = M.Take(M.Count - 1).Concat(d).ToList(); double sJ = 0; for (int i = 1; i < M.Count; i++) sJ += D2(M[i], M[i - 1]);
var nrm = new List<double[]>();
for (int i = 0; i < CL.Count; i++)
{
    double nx = 0, ny = 0;
    if (i > 0) { double dx = CL[i][0] - CL[i - 1][0], dy = CL[i][1] - CL[i - 1][1], l = Math.Sqrt(dx * dx + dy * dy); nx += dy / l; ny += -dx / l; }
    if (i < CL.Count - 1) { double dx = CL[i + 1][0] - CL[i][0], dy = CL[i + 1][1] - CL[i][1], l = Math.Sqrt(dx * dx + dy * dy); nx += dy / l; ny += -dx / l; }
    double nl = Math.Sqrt(nx * nx + ny * ny); nrm.Add(new[] { nx / nl, ny / nl });
}
// point at chainage s (0 = joint), offset off (+ right), on the floor; with the blended tangent azimuth
(double[] p, double az) At(double s0, double off)
{
    double s = s0 + sJ, c0 = 0;
    for (int i = 1; i < CL.Count; i++)
    {
        double L = D2(CL[i], CL[i - 1]);
        if (s <= c0 + L + 1e-9 || i == CL.Count - 1)
        {
            double t = (s - c0) / L, px = CL[i - 1][0] + t * (CL[i][0] - CL[i - 1][0]), py = CL[i - 1][1] + t * (CL[i][1] - CL[i - 1][1]), pz = CL[i - 1][2] + t * (CL[i][2] - CL[i - 1][2]);
            double nx = nrm[i - 1][0] + t * (nrm[i][0] - nrm[i - 1][0]), ny = nrm[i - 1][1] + t * (nrm[i][1] - nrm[i - 1][1]), nl = Math.Sqrt(nx * nx + ny * ny); nx /= nl; ny /= nl;
            double az = Math.Atan2(-ny, nx) * 180 / Math.PI; // tangent = normal rotated back
            return (new[] { px + off * nx, py + off * ny, pz }, (az + 360) % 360);
        }
        c0 += L;
    }
    return (CL[^1], 0);
}
// Drafted decline walls redrawn clean from the current decline CL (4588F43): exactly 2.75 m each side (blended vertex
// normals), a vertex every ~0.5 m, floor RL; end points kept exactly (they are the fillet / Design Master ties).
// 4588FB1: left wall, RAD down-dip mouth end -> DSP up-dip mouth end. 4588F49: DSP down-dip mouth end -> left wall to the
// face, across the 5.5 m face, right wall back to its existing end (Design Master wall 4588F40 tie). Dry run unless APPLY.
double ChOf(double[] q, double off) { double best = 1e9, bs = 0; for (double s = -5; s <= 85; s += 0.005) { var dd = D2(At(s, off).p, q); if (dd < best) { best = dd; bs = s; } } return bs; }
List<double[]> Run(double s0, double s1, double off) { var r = new List<double[]>(); int n = Math.Max(1, (int)Math.Round(Math.Abs(s1 - s0) / 0.5)); for (int i = 0; i <= n; i++) r.Add(At(s0 + (s1 - s0) * i / n, off).p); return r; }
var a = P3(H["4588FB1"]); var b = P3(H["4588F49"]); double sEnd = 0; for (int i = 1; i < d.Count; i++) sEnd += D2(d[i], d[i - 1]);
double a0 = ChOf(a[0], -2.75), a1 = ChOf(a[^1], -2.75), b0 = ChOf(b[0], -2.75), b1 = ChOf(b[^1], 2.75);
var na = Run(a0, a1, -2.75); na[0] = a[0]; na[^1] = a[^1];
var nb = Run(b0, sEnd, -2.75); nb.AddRange(Run(sEnd, b1, 2.75)); nb[0] = b[0]; nb[^1] = b[^1];
string Q(List<double[]> p) { var o = p.Select(q => { double bd = 1e9, bo = 0; for (int i = 1; i < CL.Count; i++) { double vx = CL[i][0] - CL[i - 1][0], vy = CL[i][1] - CL[i - 1][1], L = Math.Sqrt(vx * vx + vy * vy), t = Math.Max(0, Math.Min(1, ((q[0] - CL[i - 1][0]) * vx + (q[1] - CL[i - 1][1]) * vy) / (L * L))); double dd = Math.Sqrt(Math.Pow(CL[i - 1][0] + t * vx - q[0], 2) + Math.Pow(CL[i - 1][1] + t * vy - q[1], 2)); if (dd < bd) { bd = dd; bo = dd; } } return bo; }).Where(x => x > 2.5).ToList(); var sg = Enumerable.Range(1, p.Count - 1).Select(i => D2(p[i], p[i - 1])).ToList(); return $"{p.Count} pts, seg {sg.Min():F2}-{sg.Max():F2}, distance to CL {o.Min():F3}-{o.Max():F3}"; }
Out.AppendLine($"4588FB1 ch {a0:F2}..{a1:F2}: now {Q(a)} -> {Q(na)}");
Out.AppendLine($"4588F49 ch {b0:F2}..{sEnd:F2}..{b1:F2}: now {Q(b)} -> {Q(nb)}; end face {D2(nb.First(q => true), nb[0]):F0}");
Out.AppendLine($"ends kept: FB1 {D3(na[0], a[0]):F3}/{D3(na[^1], a[^1]):F3}, F49 {D3(nb[0], b[0]):F3}/{D3(nb[^1], b[^1]):F3}; second points move FB1 {D3(na[1], At(a0 + 0.5, -2.75).p):F3}");
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }
var log = new System.Text.StringBuilder(); foreach (var h in new[] { "4588FB1", "4588F49" }) { log.AppendLine("== " + h); foreach (var q in P3(H[h])) log.AppendLine($"{q[0]:R},{q[1]:R},{q[2]:R}"); }
System.IO.File.WriteAllText(@"C:\dswtmp\decline_walls_clean_backup.txt", log.ToString());
Deswik.Graphics.Primaries.Figure Replace(Deswik.Graphics.Primaries.Figure src, List<double[]> p) { var k = (Deswik.Graphics.Primaries.Figure)src.Clone(); var pl = k.asPolyline; bool cl = pl.Closed; while (pl.VertexCount > 0) pl.RemoveVertexAt(0); foreach (var q in p) pl.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]); pl.Closed = cl; App.ModelSpace.Entities.AddItem(k, null, null, true, false); src.Deleted = true; return k; }
var r1 = Replace(H["4588FB1"], na); var r2 = Replace(H["4588F49"], nb); App.Redraw(true);
Out.AppendLine($"applied: {r1.Handle} (RAD-DSP left wall), {r2.Handle} (DSP-face-right wall); backup C:\\dswtmp\\decline_walls_clean_backup.txt"); return null;