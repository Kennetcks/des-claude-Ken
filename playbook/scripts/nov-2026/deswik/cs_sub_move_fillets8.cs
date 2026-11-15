// G6390 SUB (Level Design Drafter): draft centreline 458948B moved MOVE m UP the Design Master decline 44BD9C0 (along the
// decline CL), start on the decline wall line (2.75 m off the CL) at decline floor RL, squared to the decline there; same plan
// length and 1:50 grade. Wall: 2.5 m each side with FIL x FIL fillets (corner = drive wall meets decline wall; FIL along each).
// Report only unless APPLY. TARGET = where the new wall goes: "DRAFT" = new string on 0\DESIGN DRAFT\WALLS (DM untouched
// except the two decline wall ends), "DM" = replace the Design Master SUB wall 4583731.
const bool APPLY = false;
const string TARGET = "DRAFT";
const double TOTAL = 0.0, ANG = 89.0, FIL = 2.5, HW = 2.5;
List<double[]> P3(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
double D2(double[] a, double[] b) => Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2));
double D3(double[] a, double[] b) => Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2) + Math.Pow(a[2] - b[2], 2));
double Az(double[] a, double[] b) { var z = Math.Atan2(b[0] - a[0], b[1] - a[1]) * 180 / Math.PI; return z < 0 ? z + 360 : z; }
double Turn(double a1, double a2) { var d = a2 - a1; while (d > 180) d -= 360; while (d < -180) d += 360; return d; }
var H = new Dictionary<string, Deswik.Graphics.Primaries.Figure>(StringComparer.OrdinalIgnoreCase);
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (!f.Deleted && f.Layer != null) H[f.Handle] = f;
foreach (var h in new[] { "44BD9C0", "4589579", "458957A", "458957B", "458957C" }) if (!H.ContainsKey(h)) { Out.AppendLine(h + " missing - nothing done"); return null; }
var CL = P3(H["44BD9C0"]);
var nrm = new List<double[]>();
for (int i = 0; i < CL.Count; i++)
{
    double nx = 0, ny = 0;
    if (i > 0) { double dx = CL[i][0] - CL[i - 1][0], dy = CL[i][1] - CL[i - 1][1], l = Math.Sqrt(dx * dx + dy * dy); nx += dy / l; ny += -dx / l; }
    if (i < CL.Count - 1) { double dx = CL[i + 1][0] - CL[i][0], dy = CL[i + 1][1] - CL[i][1], l = Math.Sqrt(dx * dx + dy * dy); nx += dy / l; ny += -dx / l; }
    double nl = Math.Sqrt(nx * nx + ny * ny); nrm.Add(new[] { nx / nl, ny / nl });
}
double total = 0; for (int i = 1; i < CL.Count; i++) total += D2(CL[i], CL[i - 1]);
(double[] p, double[] n) At(double s, double off)
{
    double c0 = 0;
    for (int i = 1; i < CL.Count; i++)
    {
        double L = D2(CL[i], CL[i - 1]);
        if (s <= c0 + L + 1e-9 || i == CL.Count - 1)
        {
            double t = (s - c0) / L, px = CL[i - 1][0] + t * (CL[i][0] - CL[i - 1][0]), py = CL[i - 1][1] + t * (CL[i][1] - CL[i - 1][1]), pz = CL[i - 1][2] + t * (CL[i][2] - CL[i - 1][2]);
            double nx = nrm[i - 1][0] + t * (nrm[i][0] - nrm[i - 1][0]), ny = nrm[i - 1][1] + t * (nrm[i][1] - nrm[i - 1][1]), nl = Math.Sqrt(nx * nx + ny * ny); nx /= nl; ny /= nl;
            return (new[] { px + off * nx, py + off * ny, pz }, new[] { nx, ny });
        }
        c0 += L;
    }
    return (CL[^1], nrm[^1]);
}
double OffAt(double[] q) { double bd = 1e9, bo = 0; for (int i = 1; i < CL.Count; i++) { double vx = CL[i][0] - CL[i - 1][0], vy = CL[i][1] - CL[i - 1][1], L = Math.Sqrt(vx * vx + vy * vy), t = Math.Max(0, Math.Min(1, ((q[0] - CL[i - 1][0]) * vx + (q[1] - CL[i - 1][1]) * vy) / (L * L))); double dd = Math.Sqrt(Math.Pow(CL[i - 1][0] + t * vx - q[0], 2) + Math.Pow(CL[i - 1][1] + t * vy - q[1], 2)); if (dd < bd) { bd = dd; bo = ((q[0] - CL[i - 1][0]) * vy - (q[1] - CL[i - 1][1]) * vx) / L; } } return bo; }
var sub = P3(H["4589579"]); var S0 = sub[0];
double W = OffAt(S0) < 0 ? -2.75 : 2.75;   // decline wall line on the SUB side
double ChOf(double[] q) { double best = 1e9, bs = 0; for (double s = 0; s <= total; s += 0.002) { var dd = D2(At(s, W).p, q); if (dd < best) { best = dd; bs = s; } } return bs; }
// one consistent measure: chainage = arc length along the decline CL of the perpendicular foot of a point
double ProjS(double[] q) { double bd = 1e9, bs = 0, run = 0; for (int i = 1; i < CL.Count; i++) { double vx = CL[i][0] - CL[i - 1][0], vy = CL[i][1] - CL[i - 1][1], L = Math.Sqrt(vx * vx + vy * vy), t = Math.Max(0, Math.Min(1, ((q[0] - CL[i - 1][0]) * vx + (q[1] - CL[i - 1][1]) * vy) / (L * L))); double dd = Math.Sqrt(Math.Pow(CL[i - 1][0] + t * vx - q[0], 2) + Math.Pow(CL[i - 1][1] + t * vy - q[1], 2)); if (dd < bd) { bd = dd; bs = run + t * L; } run += L; } return bs; }
var orig = P3(H["4589410"]);   // Design Master G6390 SUB centreline = the original position
double sOrig = ProjS(orig[0]); double s0 = ProjS(S0); var a0 = At(s0, W);
bool downIsUp = At(s0 + 1, 0).p[2] > At(s0, 0).p[2];   // is the decline climbing with chainage?
double sN = sOrig + (downIsUp ? TOTAL : -TOTAL);
// square to the decline segment that holds the new chainage; start 2.75 m off the CL on that square line
double tx = 0, ty = 0; { double run = 0; for (int i = 1; i < CL.Count; i++) { double L = D2(CL[i], CL[i - 1]); if (sN <= run + L + 1e-9 || i == CL.Count - 1) { tx = (CL[i][0] - CL[i - 1][0]) / L; ty = (CL[i][1] - CL[i - 1][1]) / L; break; } run += L; } }
double sgn = Math.Sign(W); double ux = sgn * ty, uy = -sgn * tx;   // into the SUB (away from the decline CL), 90 deg to the decline
var cN = At(sN, 0).p; var S = new[] { cN[0] + Math.Abs(W) * ux, cN[1] + Math.Abs(W) * uy, cN[2] };
// tilt the drive from square to ANG deg to the decline, leaning the same way as the original SUB (Design Master copy 4589410)
{ double odx = orig[1][0] - orig[0][0], ody = orig[1][1] - orig[0][1]; double cz = ux * ody - uy * odx; double th = Math.Sign(cz) * (90 - ANG) * Math.PI / 180;
  double rx = ux * Math.Cos(th) - uy * Math.Sin(th), ry = ux * Math.Sin(th) + uy * Math.Cos(th); ux = rx; uy = ry; }
Out.AppendLine($"original start chainage {sOrig:F3}; current {s0:F3} ({sOrig - s0:F3} m up); target {sN:F3} ({TOTAL:F3} m up from the original); new start reads back at {ProjS(S):F3}");
double planL = D2(sub[0], sub[1]), gr = (sub[1][2] - sub[0][2]) / planL;
var E = new[] { S[0] + planL * ux, S[1] + planL * uy, S[2] + planL * gr };
double tanAz0 = Az(At(s0 - 0.5, 0).p, At(s0 + 0.5, 0).p), tanAzN = Az(At(sN - 0.5, 0).p, At(sN + 0.5, 0).p);
Out.AppendLine($"current: start ch {s0:F2} off {OffAt(S0):F3}, start RL {S0[2]:F3} vs decline floor {a0.p[2]:F3}; SUB az {Az(sub[0], sub[1]):F2}, decline tangent {tanAz0:F2} -> take-off {Math.Abs(Turn(tanAz0, Az(sub[0], sub[1]))):F2} deg; plan {planL:F2} m at 1:{1 / gr:F1}");
Out.AppendLine($"new:     start ch {sN:F2} ({(sN < s0 ? "up" : "down")} the decline {Math.Abs(sN - s0):F3} m from where it is now; start moves {D2(S0, S):F3} m plan, RL {S0[2]:F3} -> {S[2]:F3}); SUB az {Az(S, E):F2}, decline tangent {tanAzN:F2} -> take-off {Math.Abs(Turn(tanAzN, Az(S, E))):F2} deg; end moves {D2(sub[1], E):F3} m");
// fillets: corner K where each drive wall line meets the decline wall line, F = FIL along the drive wall, E = FIL along the decline wall
(double[] Ew, double[] Fd, double sE) Fillet(double side)
{
    var o = new[] { S[0] + side * uy, S[1] - side * ux }; double lo = -8, hi = 8; double g(double a) => OffAt(new[] { o[0] + a * ux, o[1] + a * uy }) - W;
    for (int it = 0; it < 80; it++) { double m = (lo + hi) / 2; if (Math.Sign(g(m)) == Math.Sign(g(lo))) lo = m; else hi = m; }
    var K = new[] { o[0] + lo * ux, o[1] + lo * uy }; var F = new[] { K[0] + FIL * ux, K[1] + FIL * uy, S[2] + (lo + FIL) * gr };
    double sK = ChOf(new[] { K[0], K[1], 0 }); double dir = ChOf(new[] { o[0], o[1], 0 }) < sN ? -1 : 1;
    double a = sK, b = sK + dir * 5; for (int it = 0; it < 80; it++) { double m = (a + b) / 2; if (D2(At(m, W).p, K) < FIL) a = m; else b = m; }
    return (At((a + b) / 2, W).p, F, (a + b) / 2);
}
var f1 = Fillet(HW); var f2 = Fillet(-HW); var up = f1.sE < f2.sE ? f1 : f2; var dn = f1.sE < f2.sE ? f2 : f1; double sideUp = f1.sE < f2.sE ? HW : -HW;
double[] Side(double[] c, double side) => new[] { c[0] + side * uy, c[1] - side * ux, c[2] };
var wall = new List<double[]> { up.Ew, up.Fd, Side(E, sideUp), Side(E, -sideUp), dn.Fd, dn.Ew };
var dmw = P3(H["458957A"]);
Out.AppendLine($"Design Master SUB wall 4583731 now: fillet legs {D2(dmw[0], dmw[1]):F2} chord / {D2(dmw[^1], dmw[^2]):F2} chord; mouth {D2(dmw[0], dmw[^1]):F2} m");
Out.AppendLine($"new wall: mouth ch {up.sE:F2}..{dn.sE:F2} ({D2(up.Ew, dn.Ew):F2} m across); legs {FIL} x {FIL}, chords {D2(up.Ew, up.Fd):F2} / {D2(dn.Ew, dn.Fd):F2}; end face {D2(wall[2], wall[3]):F2} m");
// decline wall strings that end on the old mouth
var w9 = P3(H["458957B"]); var w7 = P3(H["458957C"]);
Out.AppendLine($"44BD9FF end ({w9[^1][0]:F2},{w9[^1][1]:F2}) vs old SUB wall start: {D3(w9[^1], dmw[0]) * 1000:F0} mm; first pt vs it {D3(w9[0], dmw[0]) * 1000:F0} mm");
Out.AppendLine($"4589487 start ({w7[0][0]:F2},{w7[0][1]:F2}) vs old SUB wall end: {D3(w7[0], dmw[^1]) * 1000:F0} mm; last pt vs it {D3(w7[^1], dmw[^1]) * 1000:F0} mm");
Out.AppendLine($"decline wall ends move: up-dip {D3(dmw[0], up.Ew):F2} m (ch {ChOf(dmw[0]):F2} -> {up.sE:F2}), down-dip {D3(dmw[^1], dn.Ew):F2} m (ch {ChOf(dmw[^1]):F2} -> {dn.sE:F2})");
// decline wall ends on their own wall line: how far the new fillet ends sit off it (mitred DM wall vs blended frame)
double SegD(double[] p, List<double[]> pl) { double bd = 1e9; for (int i = 1; i < pl.Count; i++) { double vx = pl[i][0] - pl[i - 1][0], vy = pl[i][1] - pl[i - 1][1], L2 = vx * vx + vy * vy, t = Math.Max(0, Math.Min(1, ((p[0] - pl[i - 1][0]) * vx + (p[1] - pl[i - 1][1]) * vy) / L2)); bd = Math.Min(bd, Math.Sqrt(Math.Pow(pl[i - 1][0] + t * vx - p[0], 2) + Math.Pow(pl[i - 1][1] + t * vy - p[1], 2))); } return bd; }
Out.AppendLine($"new up-dip fillet end is {SegD(up.Ew, w9) * 1000:F0} mm off 44BD9FF's line; new down-dip end {SegD(dn.Ew, w7) * 1000:F0} mm off 4589487's line");
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }
Deswik.Graphics.Primaries.Layer LW = null; foreach (Deswik.Graphics.Primaries.Layer l in App.Layers) if (l.Name == "0\\DESIGN DRAFT\\WALLS") LW = l;
if (LW == null) { Out.AppendLine("layer 0\\DESIGN DRAFT\\WALLS not found - nothing done"); return null; }
var log = new System.Text.StringBuilder(); foreach (var h in new[] { "4589579", "458957A", "458957B", "458957C" }) { log.AppendLine("== " + h); foreach (var q in P3(H[h])) log.AppendLine($"{q[0]:R},{q[1]:R},{q[2]:R}"); }
System.IO.File.WriteAllText(@"C:\dswtmp\sub_move_fillets_backup8.txt", log.ToString());
Deswik.Graphics.Primaries.Figure NewPl(Deswik.Graphics.Primaries.Figure src, List<double[]> p, Deswik.Graphics.Primaries.Layer layer) { var k = (Deswik.Graphics.Primaries.Figure)src.Clone(); k.Deleted = false; if (layer != null) k.Layer = layer; var pl = k.asPolyline; bool cl = pl.Closed; while (pl.VertexCount > 0) pl.RemoveVertexAt(0); foreach (var q in p) pl.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]); pl.Closed = cl; App.ModelSpace.Entities.AddItem(k, null, null, true, false); return k; }
var nCL = NewPl(H["4589579"], new List<double[]> { S, E }, null); H["4589579"].Deleted = true;
var nW = NewPl(H["458957A"], wall, LW); H["458957A"].Deleted = true;
var n9 = w9.Take(w9.Count - 1).ToList(); n9.Add(up.Ew); var r9 = NewPl(H["458957B"], n9, null); H["458957B"].Deleted = true;
var n7 = new List<double[]> { dn.Ew }; n7.AddRange(w7.Skip(1)); var r7 = NewPl(H["458957C"], n7, null); H["458957C"].Deleted = true;
App.Redraw(true);
Out.AppendLine($"applied: draft SUB CL 458948B -> {nCL.Handle}; new draft SUB wall {nW.Handle} [{nW.Layer.Name}]; DM decline walls 44BD9FF -> {r9.Handle}, 4589487 -> {r7.Handle}; backup C:\\dswtmp\\sub_move_fillets_backup8.txt");
return null;
