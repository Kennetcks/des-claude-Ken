// W6390 RAD take-off squared to the new decline (4588F43): the CL keeps its inner points v1/v2 (on the MPA 07 RAD
// solids); its start slides along the projected left decline wall (2.75 m off the CL) to where the first leg is at 90 deg
// to the decline tangent. Mouth re-centred: ends on the projected wall 5.5 m either side of the start (5.0 m drive +
// 3 m fillets, GSS R 3.0), fillet points 2.924 m up the first leg 2.5 m each side; RLs: mouth/start on the decline floor,
// fillet points on the first-leg grade; v2-v5 unchanged. Decline wall 4588F48 trimmed/extended to the new down-dip mouth
// end. The up-dip mouth end ties to whatever string ends there (reported; moved with it if found). Dry run unless APPLY.
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
foreach (var h in new[] { "44BD9C0", "4588F43", "4588F44", "4588F45", "4588F48" }) if (!H.ContainsKey(h)) { Out.AppendLine($"{h} missing - nothing done"); return null; }
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
var rc = P3(H["4588F44"]); var rw = P3(H["4588F45"]); var v1 = rc[1];
// start: chainage where the line wall-point -> v1 is square to the decline tangent
double lo = -2, hi = 9;
double Err(double s) { var a = At(s, -2.75); return Turn(a.az, Az(a.p, v1)) + 90; }
for (int it = 0; it < 80; it++) { double m = (lo + hi) / 2; if (Math.Sign(Err(m)) == Math.Sign(Err(lo))) lo = m; else hi = m; }
double sS = (lo + hi) / 2; var S = At(sS, -2.75).p;
Out.AppendLine($"tangent check: at old start az {At(3.48, -2.75).az:F2}");
Out.AppendLine($"new RAD start ch {sS:F2} ({S[0]:F3},{S[1]:F3},{S[2]:F3}); moves {D2(rc[0], S):F2} m along the wall; take-off {Turn(At(sS, -2.75).az, Az(S, v1)):F2} deg; leg 1 {D2(S, v1):F2} m 1:{D2(S, v1) / (v1[2] - S[2]):F2}, az {Az(S, v1):F2} then {Az(v1, rc[2]):F2} (bend {Turn(Az(S, v1), Az(v1, rc[2])):F2} deg at {D2(S, v1):F2} m)");
// mouth ends: on the projected wall, 5.5 m (plan) from S each side
double Find(double dir) { double a = sS, b = sS + dir * 8; for (int it = 0; it < 80; it++) { double m = (a + b) / 2; if (D2(At(m, -2.75).p, S) < 5.5) a = m; else b = m; } return (a + b) / 2; }
double sUp = Find(-1), sDn = Find(1); var eUp = At(sUp, -2.75).p; var eDn = At(sDn, -2.75).p;
double ux = v1[0] - S[0], uy = v1[1] - S[1], ul = Math.Sqrt(ux * ux + uy * uy); ux /= ul; uy /= ul; double a2 = 2.924, cz = S[2] + a2 / ul * (v1[2] - S[2]);
var fL = new[] { S[0] + a2 * ux - 2.5 * uy, S[1] + a2 * uy + 2.5 * ux, cz }; var fR = new[] { S[0] + a2 * ux + 2.5 * uy, S[1] + a2 * uy - 2.5 * ux, cz };
// match to old wall order: v0 up-dip mouth end, v1 fillet on its side, v2-v5 unchanged, v6 other fillet, v7 down-dip mouth end
if (D2(fL, rw[1]) > D2(fR, rw[1])) { var t = fL; fL = fR; fR = t; }
eUp = rw[0];   // stays on the end of the Design Master decline wall 44BDA03
var nw = new List<double[]> { eUp, fL, rw[2], rw[3], rw[4], rw[5], fR, eDn };
sUp = -1.20; Out.AppendLine($"mouth ch {sUp:F2} .. {sDn:F2} ({sS - sUp:F2} / {sDn - sS:F2} along the CL), {D2(eUp, eDn):F2} m across; wall moves " + string.Join(" ", nw.Select((q, i) => $"v{i} {D3(q, rw[i]):F2}")));
// what the old up-dip end tied to
var ties = polys.Where(f => f.Handle != "4588F45").Select(f => (f, p: P3(f))).Where(x => x.p.Count > 1 && (D3(x.p[0], rw[0]) < 0.05 || D3(x.p[^1], rw[0]) < 0.05)).ToList();
foreach (var t in ties) Out.AppendLine($"up-dip end tie: {t.f.Handle} [{t.f.Layer.Name}] {A(t.f, "Name")} n={t.p.Count} {(D3(t.p[0], rw[0]) < 0.05 ? "start" : "end")}");
var near = polys.Where(f => f.Layer.Name.Contains("WALL") && f.Handle != "4588F45").Select(f => (f, dd: P3(f).Min(q => D3(q, rw[0])))).OrderBy(x => x.dd).Take(4).ToList();
foreach (var x in near) Out.AppendLine($"  nearest wall vertex to old up-dip end: {x.f.Handle} [{x.f.Layer.Name}] {A(x.f, "Name")} {x.dd:F3} m");
// decline wall 4588F48 from the down-dip mouth end
var w48 = P3(H["4588F48"]); double Ch(double[] q) { double best = 1e9, bs = 0; for (double s = sDn - 6; s <= sDn + 40; s += 0.01) { var dd = D2(At(s, -2.75).p, q); if (dd < best) { best = dd; bs = s; } } return bs; }
var keep = w48.Where(q => Ch(q) > sDn + 0.05).ToList(); var nw48 = new List<double[]> { eDn };
double sFirst = Ch(keep[0]); for (double s = sDn + 0.5; s < sFirst - 0.25; s += 0.5) nw48.Add(At(s, -2.75).p); nw48.AddRange(keep);
Out.AppendLine($"4588F48: {w48.Count} -> {nw48.Count} pts, starts at ch {Ch(w48[0]):F2} -> {sDn:F2}");
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
Out.AppendLine("RAD strip (W 5.0):"); var radStrip = Strip(sS, S, v1, 5.0);
var dsc = P3(H["4588F46"]); double sD = ChOf(dsc[0]);
Out.AppendLine("DSP strip (W 5.5):"); var dspStrip = Strip(sD, dsc[0], dsc[1], 5.5);
var radH = new[] { "4588F4A", "4588F4C", "4588F4B", "4588F4D" }; var dspH = new[] { "4588F4E", "4588F50", "4588F4F", "4588F51" };
for (int i = 0; i < 4; i++) { Out.AppendLine($"  {radH[i]}: {P3(H[radH[i]]).Count} -> {radStrip[i].Count} pts; {dspH[i]}: {P3(H[dspH[i]]).Count} -> {dspStrip[i].Count} pts"); }
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }
var log = new System.Text.StringBuilder(); foreach (var h in new[] { "4588F44", "4588F45", "4588F48" }.Concat(radH).Concat(dspH)) { log.AppendLine("== " + h); foreach (var q in P3(H[h])) log.AppendLine($"{q[0]:R},{q[1]:R},{q[2]:R}"); }
System.IO.File.WriteAllText(@"C:\dswtmp\rad_square_strips_backup.txt", log.ToString());
Deswik.Graphics.Primaries.Figure Replace(Deswik.Graphics.Primaries.Figure src, List<double[]> p)
{
    var c = (Deswik.Graphics.Primaries.Figure)src.Clone(); var pl = c.asPolyline; bool cl = pl.Closed; while (pl.VertexCount > 0) pl.RemoveVertexAt(0);
    foreach (var q in p) pl.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]); pl.Closed = cl;
    App.ModelSpace.Entities.AddItem(c, null, null, true, false); src.Deleted = true; return c;
}
var a = Replace(H["4588F44"], new List<double[]> { S, rc[1], rc[2] }); var b = Replace(H["4588F45"], nw); var c2 = Replace(H["4588F48"], nw48);
var sh = new List<string>(); for (int i = 0; i < 4; i++) { sh.Add(radH[i] + "->" + Replace(H[radH[i]], radStrip[i]).Handle); sh.Add(dspH[i] + "->" + Replace(H[dspH[i]], dspStrip[i]).Handle); }
App.Redraw(true); Out.AppendLine("strips: " + string.Join(", ", sh)); Out.AppendLine($"applied: RAD CL {a.Handle}, RAD wall {b.Handle}, decline wall {c2.Handle}; backup C:\\dswtmp\\rad_square_strips_backup.txt"); return null;
