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
foreach (var h in new[] { "44BD9C0", "4588F43", "4588F46", "4588F47", "4588F5D", "4588F5F", "4588F61", "4588F63" }) if (!H.ContainsKey(h)) { Out.AppendLine($"{h} missing - nothing done"); return null; }
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
// DSP squared: rotate the CL about its start (on the decline wall) to 90 deg to the decline tangent there, same length and
// 1:50 up. Mouth ends on the decline wall unchanged; fillet points 3.0 m in, 2.75 m each side; end face 5.5 m. RLs on the
// 1:50 grade from the start. Back strip (4 polygons) rebuilt to the standard on the new drive direction. Dry run unless APPLY.
var dc = P3(H["4588F46"]); var dw = P3(H["4588F47"]); var S = dc[0]; double L = D2(dc[0], dc[1]);
double ChOf0(double[] q) { double best = 1e9, bs = 0; for (double s = 20; s <= 60; s += 0.005) { var dd = D2(At(s, -2.75).p, q); if (dd < best) { best = dd; bs = s; } } return bs; }
double sS = ChOf0(S); double tz = At(sS, -2.75).az; double oldAz = Az(dc[0], dc[1]);
double newAz = tz - 90; if (Math.Abs(Turn(newAz, oldAz)) > 45) newAz = tz + 90; newAz = (newAz + 360) % 360;
double ux = Math.Sin(Rad(newAz)), uy = Math.Cos(Rad(newAz));
double[] Pt(double along, double side) => new[] { S[0] + along * ux + side * uy, S[1] + along * uy - side * ux, S[2] + along / 50.0 };   // side + = right of the drive
var E = Pt(L, 0); var ndc = new List<double[]> { S, E };
// wall order: v0 mouth end, v1 fillet same side, v2 end, v3 end, v4 fillet, v5 mouth end
double sideOf(double[] q) => (q[0] - S[0]) * uy - (q[1] - S[1]) * ux;
double s1 = Math.Sign(sideOf(dw[1])); var ndw = new List<double[]> { dw[0], Pt(3.0, 2.75 * s1), Pt(L, 2.75 * s1), Pt(L, -2.75 * s1), Pt(3.0, -2.75 * s1), dw[5] };
Out.AppendLine($"DSP take-off: decline tangent {tz:F2}, drive az {oldAz:F2} -> {newAz:F2} ({Turn(tz, oldAz):F2} -> {Turn(tz, newAz):F2} deg); end moves {D2(dc[1], E):F2} m, RL {dc[1][2]:F3} -> {E[2]:F3}");
Out.AppendLine("DSP wall moves: " + string.Join(" ", dw.Select((q, i) => $"v{i} {D3(q, ndw[i]):F2}")) + $"; end face {D2(ndw[2], ndw[3]):F2}, fillets {D2(ndw[0], ndw[1]):F2}/{D2(ndw[4], ndw[5]):F2}");
// ---- truck loading back strips rebuilt to the standard (Truck Loading Strip 1:7 down, decline: 8.0 m flat, 1.5 m high,
// 3.5 m ramps, drop-down = drive width x 3.5 m deep x 1.5 m drop), centred on each drive's take-off.
// u = decline chainage from the drive start (down-dip +), v = toward the drive from the decline CL, z above the decline floor
const double ZS = 7.146, ZE = 5.646, VF = -2.49, VN = 2.51, VTOP = 3.5;
double[] UV(double s0, double u, double v, double z) { var a = At(s0 + u, -v); return new[] { a.p[0], a.p[1], a.p[2] + z }; }
double FloorAt(double[] q) { double best = 1e9, bz = 0; for (int i = 1; i < CL.Count; i++) { double vx = CL[i][0] - CL[i - 1][0], vy = CL[i][1] - CL[i - 1][1], L2 = vx * vx + vy * vy, t = Math.Max(0, Math.Min(1, ((q[0] - CL[i - 1][0]) * vx + (q[1] - CL[i - 1][1]) * vy) / L2)); double dd = Math.Pow(CL[i - 1][0] + t * vx - q[0], 2) + Math.Pow(CL[i - 1][1] + t * vy - q[1], 2); if (dd < best) { best = dd; bz = CL[i - 1][2] + t * (CL[i][2] - CL[i - 1][2]); } } return bz; }
double ChOf(double[] q) { double best = 1e9, bs = 0; for (double s = -20; s <= 80; s += 0.01) { var dd = D2(At(s, -2.75).p, q); if (dd < best) { best = dd; bs = s; } } return bs; }
List<List<double[]>> Strip(double s0, double[] start, double[] inner, double W)
{
    double ux2 = inner[0] - start[0], uy2 = inner[1] - start[1], l2 = Math.Sqrt(ux2 * ux2 + uy2 * uy2); ux2 /= l2; uy2 /= l2;
    // drop-down corners in the drive frame: top edge 0.75 m in from the wall line (v 3.5), bottom 3.5 m further
    double sideSign = 1; { var t = new[] { start[0] + uy2, start[1] - ux2, 0.0 }; if (ChOf(t) < s0) sideSign = -1; }   // + side = down-dip
    double[] DC(double along, double side, double dz) { var q = new[] { start[0] + along * ux2 + side * sideSign * uy2, start[1] + along * uy2 - side * sideSign * ux2, 0.0 }; return q; }
    var tUp = DC(0.75, -W / 2, 0); var tDn = DC(0.75, W / 2, 0); var bDn = DC(4.25, W / 2, 0); var bUp = DC(4.25, -W / 2, 0);
    foreach (var q in new[] { tUp, tDn }) q[2] = FloorAt(q) + ZS; bDn[2] = tDn[2] - 1.5; bUp[2] = tUp[2] - 1.5;
    double u0 = ChOf(tUp) - s0, u1 = u0 + 8.0;   // flat starts at the up-dip edge of the drop-down, 8 m long
    var A = new List<double[]>(); for (int i = 0; i <= 7; i++) { double u = u0 - 3.5 + i * 0.5; A.Add(UV(s0, u, VF, ZE + (ZS - ZE) * i / 7.0)); } for (int i = 7; i >= 0; i--) { double u = u0 - 3.5 + i * 0.5; A.Add(UV(s0, u, VN, ZE + (ZS - ZE) * i / 7.0)); }
    var C = new List<double[]>(); for (int i = 0; i <= 16; i++) C.Add(UV(s0, u0 + i * 0.5, VF, ZS)); C.Add(UV(s0, u1, VN, ZS)); C.Add(tDn); C.Add(tUp); C.Add(UV(s0, u0, VN, ZS));
    var B = new List<double[]>(); for (int i = 0; i <= 7; i++) { double u = u1 + i * 0.5; B.Add(UV(s0, u, VF, ZS - (ZS - ZE) * i / 7.0)); } for (int i = 7; i >= 0; i--) { double u = u1 + i * 0.5; B.Add(UV(s0, u, VN, ZS - (ZS - ZE) * i / 7.0)); }
    var Dd = new List<double[]> { tUp, tDn, bDn, bUp };
    Out.AppendLine($"  strip at drive start ch {s0:F2}: flat u {u0:F2}..{u1:F2}, ramps 3.5 m, drop-down top {D2(tUp, tDn):F2} m wide, {D2(tDn, bDn):F2} m deep, drop {tUp[2] - bUp[2]:F2}; top edge {FloorAt(tUp) - FloorAt(tDn):F2} m fall across (follows 1:7)");
    return new List<List<double[]>> { A, C, B, Dd };
}

Out.AppendLine("DSP strip (W 5.5):"); var dspStrip = Strip(sS, S, E, 5.5);
var dspH = new[] { "4588F5D", "4588F5F", "4588F61", "4588F63" };
for (int i = 0; i < 4; i++) Out.AppendLine($"  {dspH[i]} moves max {P3(H[dspH[i]]).Zip(dspStrip[i], (a, b) => D3(a, b)).Max():F2} m");
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }
var log = new System.Text.StringBuilder(); foreach (var h in new[] { "4588F46", "4588F47" }.Concat(dspH)) { log.AppendLine("== " + h); foreach (var q in P3(H[h])) log.AppendLine($"{q[0]:R},{q[1]:R},{q[2]:R}"); }
System.IO.File.WriteAllText(@"C:\dswtmp\dsp_square_backup.txt", log.ToString());
Deswik.Graphics.Primaries.Figure Replace(Deswik.Graphics.Primaries.Figure src, List<double[]> p)
{
    var c = (Deswik.Graphics.Primaries.Figure)src.Clone(); var pl = c.asPolyline; bool cl = pl.Closed; while (pl.VertexCount > 0) pl.RemoveVertexAt(0);
    foreach (var q in p) pl.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]); pl.Closed = cl;
    App.ModelSpace.Entities.AddItem(c, null, null, true, false); src.Deleted = true; return c;
}
var res = new List<string> { "DSP CL " + Replace(H["4588F46"], ndc).Handle, "DSP wall " + Replace(H["4588F47"], ndw).Handle };
for (int i = 0; i < 4; i++) res.Add(dspH[i] + "->" + Replace(H[dspH[i]], dspStrip[i]).Handle);
App.Redraw(true); Out.AppendLine("applied: " + string.Join(", ", res) + "; backup C:\\dswtmp\\dsp_square_backup.txt"); return null;