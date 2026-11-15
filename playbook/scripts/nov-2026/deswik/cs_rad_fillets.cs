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
foreach (var h in new[] { "44BD9C0", "4588F43", "4588F6B", "4588F6C", "4588F5B", "44BDA03" }) if (!H.ContainsKey(h)) { Out.AppendLine($"{h} missing - nothing done"); return null; }
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
// W6390 RAD fillets made exactly 3 m x 3 m: corner = drive wall line (2.5 m off the RAD CL) meets the projected decline
// wall (2.75 m off the decline CL); fillet point 3.0 m into the drive from the corner (RAD grade RL); mouth end 3.0 m along
// the decline wall from the corner (decline floor RL). Down-dip end: decline wall 4588F5B start moved to it. Up-dip end:
// Design Master decline wall 44BDA03 end moved to it (along its own wall line). Dry run unless APPLY.
double ChOf(double[] q) { double best = 1e9, bs = 0; for (double s = -10; s <= 20; s += 0.005) { var dd = D2(At(s, -2.75).p, q); if (dd < best) { best = dd; bs = s; } } return bs; }
double OffAt(double[] q) { double bd = 1e9, bo = 0; for (int i = 1; i < CL.Count; i++) { double vx = CL[i][0] - CL[i - 1][0], vy = CL[i][1] - CL[i - 1][1], L = Math.Sqrt(vx * vx + vy * vy), t = Math.Max(0, Math.Min(1, ((q[0] - CL[i - 1][0]) * vx + (q[1] - CL[i - 1][1]) * vy) / (L * L))); double dd = Math.Sqrt(Math.Pow(CL[i - 1][0] + t * vx - q[0], 2) + Math.Pow(CL[i - 1][1] + t * vy - q[1], 2)); if (dd < bd) { bd = dd; bo = ((q[0] - CL[i - 1][0]) * vy - (q[1] - CL[i - 1][1]) * vx) / L; } } return bo; }
var c = P3(H["4588F6B"]); var w = P3(H["4588F6C"]); var S = c[0];
double ux = c[1][0] - c[0][0], uy = c[1][1] - c[0][1], ul = Math.Sqrt(ux * ux + uy * uy); ux /= ul; uy /= ul; double gr = (c[1][2] - c[0][2]) / ul;
double sS = ChOf(S);
(double[] E, double[] F, double sE) Fillet(double side)
{
    var o = new[] { S[0] + side * uy, S[1] - side * ux }; double lo = -6, hi = 6; double g(double a) => OffAt(new[] { o[0] + a * ux, o[1] + a * uy }) + 2.75;
    for (int it = 0; it < 80; it++) { double m = (lo + hi) / 2; if (Math.Sign(g(m)) == Math.Sign(g(lo))) lo = m; else hi = m; }
    var K = new[] { o[0] + lo * ux, o[1] + lo * uy }; var F = new[] { K[0] + 3.0 * ux, K[1] + 3.0 * uy, S[2] + (lo + 3.0) * gr };
    double sK = ChOf(new[] { K[0], K[1], 0 }); double dir = ChOf(new[] { o[0], o[1], 0 }) < sS ? -1 : 1;
    double a = sK, b = sK + dir * 5; for (int it = 0; it < 80; it++) { double m = (a + b) / 2; if (D2(At(m, -2.75).p, K) < 3.0) a = m; else b = m; }
    var E = At((a + b) / 2, -2.75).p; return (E, F, (a + b) / 2);
}
var f1 = Fillet(2.5); var f2 = Fillet(-2.5); var up = f1.sE < f2.sE ? f1 : f2; var dn = f1.sE < f2.sE ? f2 : f1;
var nw = w.Select(q => q).ToList(); nw[0] = up.E; nw[1] = up.F; nw[6] = dn.F; nw[7] = dn.E;
if (D2(nw[1], w[1]) > 1.5 || D2(nw[6], w[6]) > 1.5) { Out.AppendLine("fillet side mismatch - nothing done"); return null; }
Out.AppendLine($"RAD wall moves: v0 {D3(w[0], nw[0]):F2} v1 {D3(w[1], nw[1]):F2} v6 {D3(w[6], nw[6]):F2} v7 {D3(w[7], nw[7]):F2}; mouth ch {up.sE:F2}..{dn.sE:F2}, {D2(up.E, dn.E):F2} m across; chords {D2(up.E, up.F):F2}/{D2(dn.E, dn.F):F2} (3x3 = 4.24)");
// decline wall 4588F5B from the new down-dip end
var w5 = P3(H["4588F5B"]); var keep = w5.Where(q => ChOf(q) > dn.sE + 0.05).ToList(); var n5 = new List<double[]> { dn.E };
for (double s = dn.sE + 0.5; s < ChOf(keep[0]) - 0.25; s += 0.5) n5.Add(At(s, -2.75).p); n5.AddRange(keep);
Out.AppendLine($"4588F5B start ch {ChOf(w5[0]):F2} -> {dn.sE:F2} ({w5.Count} -> {n5.Count} pts)");
// Design Master wall 44BDA03: its last vertex is the RAD up-dip mouth end
var dm = P3(H["44BDA03"]); bool endIsLast = D3(dm[^1], w[0]) < 0.01; if (!endIsLast && D3(dm[0], w[0]) > 0.01) { Out.AppendLine("44BDA03 does not end on the RAD wall - nothing done"); return null; }
if (!endIsLast) dm.Reverse();
var ndm = dm.Where(q => ChOf(q) < up.sE - 0.05).ToList(); for (double s = ChOf(ndm[^1]) + 0.5; s < up.sE - 0.25; s += 0.5) ndm.Add(At(s, -2.75).p); ndm.Add(up.E);
if (!endIsLast) ndm.Reverse();
Out.AppendLine($"44BDA03 (DESIGN MASTER\\WALLS) end ch -1.20 -> {up.sE:F2} ({(up.sE > -1.2 ? "extended" : "trimmed")} {Math.Abs(up.sE + 1.2):F2} m), {dm.Count} -> {ndm.Count} pts; end RL {w[0][2]:F3} -> {up.E[2]:F3}");
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }
var log = new System.Text.StringBuilder(); foreach (var h in new[] { "4588F6C", "4588F5B", "44BDA03" }) { log.AppendLine("== " + h); foreach (var q in P3(H[h])) log.AppendLine($"{q[0]:R},{q[1]:R},{q[2]:R}"); }
System.IO.File.WriteAllText(@"C:\dswtmp\rad_fillets_backup.txt", log.ToString());
Deswik.Graphics.Primaries.Figure Replace(Deswik.Graphics.Primaries.Figure src, List<double[]> p) { var k = (Deswik.Graphics.Primaries.Figure)src.Clone(); var pl = k.asPolyline; bool cl = pl.Closed; while (pl.VertexCount > 0) pl.RemoveVertexAt(0); foreach (var q in p) pl.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]); pl.Closed = cl; App.ModelSpace.Entities.AddItem(k, null, null, true, false); src.Deleted = true; return k; }
var r1 = Replace(H["4588F6C"], nw); var r2 = Replace(H["4588F5B"], n5); var r3 = Replace(H["44BDA03"], ndm); App.Redraw(true);
Out.AppendLine($"applied: RAD wall {r1.Handle}, decline wall {r2.Handle}, DM wall {r3.Handle}; backup C:\\dswtmp\\rad_fillets_backup.txt"); return null;