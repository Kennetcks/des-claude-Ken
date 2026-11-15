// MPA 07 draft MAIN DEC (4587A08) rebuilt smooth (like 44BD9C0) and fitted to the MPA 07 sketch solids (decline cuts):
// joint = 44BD9C0's end (unchanged); arc 1 tangent to 44BD9C0, straight on the solids, end arc fitted to the end cuts,
// vertices evenly spaced, RLs exactly 1:7 down from the joint (plan chord / 7). Dependants on DESIGN DRAFT
// keep their chainage / offset / height to the decline: decline walls 4588CAC/CAD, RAD-side back strip CD8/CD9/CDC/CDD,
// DSP back strip CA8/CA9/CAA/CAB, open strip CED. W6390 RAD CL keeps its two inner points, start moved onto the new
// decline wall; RAD wall v7 follows the decline wall, v1/v6 rebuilt 2.92 m into the RAD. DSP CL/wall: start/ends follow.
// Dry run unless APPLY. Replaced figures are clones (same attributes); old vertices saved to C:\dswtmp first.
const bool APPLY = false;
const bool OPTIONS_ONLY = false;
const double JOINT_DEFL = 3.5;   // option A: joint vertex turn ~8.3 deg, same as the turns on 44BD9C0's curve
var inv = System.Globalization.CultureInfo.InvariantCulture;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
List<double[]> P3(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
double D2(double[] a, double[] b) => Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2));
double Rad(double d) => d * Math.PI / 180;
double Az(double[] a, double[] b) { var z = Math.Atan2(b[0] - a[0], b[1] - a[1]) * 180 / Math.PI; return z < 0 ? z + 360 : z; }
double Turn(double a1, double a2) { var d = a2 - a1; while (d > 180) d -= 360; while (d < -180) d += 360; return d; }
var byH = new Dictionary<string, Deswik.Graphics.Primaries.Figure>(StringComparer.OrdinalIgnoreCase); var sol = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) { if (f.Deleted || f.Layer == null) continue; byH[f.Handle] = f; if (f.Layer.Name == "0\\SKETCH LAYER\\MPA 07" && f.IsPolyface && A(f, "_CPX") != "") sol.Add(f); }
var deps = new[] { "4588CAC", "4588CAD", "4588CD8", "4588CD9", "4588CDC", "4588CDD", "4588CA8", "4588CA9", "4588CAA", "4588CAB", "4588CED" };
foreach (var h in new[] { "44BD9C0", "4587A08", "4588D8B", "4588D8C", "4588D89", "4588D8A" }.Concat(deps))
    if (!byH.ContainsKey(h)) { Out.AppendLine($"{h} missing (changed since the check) - nothing done"); return null; }
var M = P3(byH["44BD9C0"]); var D = P3(byH["4587A08"]); var E = M[^1];
if (D2(E, D[0]) > 0.001) { Out.AppendLine("draft decline no longer starts on 44BD9C0's end - nothing done"); return null; }
var OldCL = M.Take(M.Count - 1).Concat(D).ToList(); double sJ = 0; for (int i = 1; i < M.Count; i++) sJ += D2(M[i], M[i - 1]);
(double s, double off, double dz) Frame(List<double[]> cl, double[] q)
{
    double bs = 0, bd = 1e9, bo = 0, bz = 0, c0 = 0;
    for (int i = 1; i < cl.Count; i++)
    {
        double vx = cl[i][0] - cl[i - 1][0], vy = cl[i][1] - cl[i - 1][1], L = Math.Sqrt(vx * vx + vy * vy), t = Math.Max(0, Math.Min(1, ((q[0] - cl[i - 1][0]) * vx + (q[1] - cl[i - 1][1]) * vy) / (L * L)));
        double px = cl[i - 1][0] + t * vx, py = cl[i - 1][1] + t * vy, d = Math.Sqrt(Math.Pow(q[0] - px, 2) + Math.Pow(q[1] - py, 2));
        if (d < bd - 1e-9) { bd = d; bs = c0 + t * L; bo = ((q[0] - cl[i - 1][0]) * vy - (q[1] - cl[i - 1][1]) * vx) / L; bz = cl[i - 1][2] + t * (cl[i][2] - cl[i - 1][2]); }
        c0 += L;
    }
    return (bs - sJ, bo, q[2] - bz);
}
// decline cuts past the joint (near the current decline line), in chainage order
var cuts = sol.Select(f => (f, x: double.Parse(A(f, "_CPX"), inv), y: double.Parse(A(f, "_CPY"), inv), z: double.Parse(A(f, "_CPZ"), inv), az: double.Parse(A(f, "_Azimuth"), inv), h: 0.0))
    .Select(c => (c.f, c.x, c.y, c.z, c.az, fr: Frame(OldCL, new[] { c.x, c.y, c.z }))).Where(c => Math.Abs(c.fr.off) < 1.0 && c.fr.s > 0).OrderBy(c => c.fr.s).ToList();
var stc0 = cuts.First(c => Math.Abs(c.az - 142.968) < 0.01);
// arc 1 options: a deflection at the joint (the joint vertex turn stays within 44BD9C0's own curve turns)
{
    double a0 = Az(M[^2], M[^1]);
    for (double dfl = 0; dfl <= 6.01; dfl += 0.5)
    {
        double az = a0 + dfl, mx = Math.Cos(Rad(az)), my = -Math.Sin(Rad(az)), sx = stc0.az, lx = Math.Cos(Rad(sx)), ly = -Math.Sin(Rad(sx));
        double R = ((E[0] - stc0.x) * lx + (E[1] - stc0.y) * ly) / (1 - (mx * lx + my * ly)), cx = E[0] + R * mx, cy = E[1] + R * my;
        double ct = 2 * Math.Asin(3.47 / 2 / R) * 180 / Math.PI;
        var offs = cuts.Where(c => c.fr.s < stc0.fr.s).Select(c => { double r = Math.Sqrt(Math.Pow(c.x - cx, 2) + Math.Pow(c.y - cy, 2)); double ang = Math.Atan2(c.x - cx, c.y - cy); return Math.Abs(r - R); }).ToList();
        Out.AppendLine($"OPTION joint deflection {dfl:F1}: R1 {R:F2} m, chord turn {ct:F2}, joint vertex turn ~{dfl + ct / 2:F2}, cut centres off arc 1 max {offs.Max():F3} avg {offs.Average():F3}");
    }
}if (OPTIONS_ONLY) return null;
double HL = 3.465 / 2;
// smooth, decline-tool style: arc 1 tangent to 44BD9C0's last segment, into the straight the solids sit on; constant-radius
// end arc fitted to the end cuts; vertices every ~3.465 m plan along the line
double az0 = Az(M[^2], M[^1]) + JOINT_DEFL;
var stc = cuts.Where(c => Math.Abs(c.az - cuts.GroupBy(k => Math.Round(k.az, 3)).OrderByDescending(g => g.Count()).First().Key) < 0.01).ToList();
double azS = stc[0].az, ux = Math.Sin(Rad(azS)), uy = Math.Cos(Rad(azS)), SAx = stc[0].x, SAy = stc[0].y;
double n0x = Math.Cos(Rad(az0)), n0y = -Math.Sin(Rad(az0)), nLx = Math.Cos(Rad(azS)), nLy = -Math.Sin(Rad(azS));
double R1 = ((E[0] - SAx) * nLx + (E[1] - SAy) * nLy) / (1 - (n0x * nLx + n0y * nLy)), th1 = azS - az0; while (th1 < 0) th1 += 360; double L1 = R1 * Rad(th1);
double C1x = E[0] + R1 * n0x, C1y = E[1] + R1 * n0y, T1x = C1x - R1 * nLx, T1y = C1y - R1 * nLy;
(double x, double y, double az) Pos(double s, double Ls, double R2, double th2)
{
    if (s <= L1) { double a = az0 + s / R1 * 180 / Math.PI; return (C1x - R1 * Math.Cos(Rad(a)), C1y + R1 * Math.Sin(Rad(a)), a); }
    double t = s - L1; if (t <= Ls) return (T1x + t * ux, T1y + t * uy, azS);
    double Bx = T1x + Ls * ux, By = T1y + Ls * uy, C2x = Bx + R2 * nLx, C2y = By + R2 * nLy, L2 = R2 * Rad(th2); t -= Ls;
    if (t <= L2) { double a = azS + t / R2 * 180 / Math.PI; return (C2x - R2 * Math.Cos(Rad(a)), C2y + R2 * Math.Sin(Rad(a)), a); }
    double ae = azS + th2, Ex = C2x - R2 * Math.Cos(Rad(ae)), Ey = C2y + R2 * Math.Sin(Rad(ae)); t -= L2;
    return (Ex + t * Math.Sin(Rad(ae)), Ey + t * Math.Cos(Rad(ae)), ae);
}
var lastStraight = stc.OrderBy(c => c.fr.s).Last(); var endCuts = cuts.Where(c => c.fr.s > lastStraight.fr.s - 0.1).ToList();
var fin = cuts[^1]; var FE = new[] { fin.x + HL * Math.Sin(Rad(fin.az)), fin.y + HL * Math.Cos(Rad(fin.az)) };
double LsT = (lastStraight.x - T1x) * ux + (lastStraight.y - T1y) * uy;
double best = 1e9, bLs = 0, bR2 = 0, bth = 0, bL = 0;
for (double R2 = 15; R2 <= 30.001; R2 += 0.5)
    for (double th2 = 35; th2 <= 60.001; th2 += 0.5)
        for (double Ls = LsT - 2; Ls <= LsT + 6; Ls += 0.1)
        {
            double sA = L1 + Ls, sB = sA + R2 * Rad(th2) + 8, m = 0, sEnd = 0, dEnd = 1e9;
            var pts = new List<(double s, double x, double y)>(); for (double s = sA - 2; s <= sB; s += 0.2) { var p = Pos(s, Ls, R2, th2); pts.Add((s, p.x, p.y)); }
            foreach (var c in endCuts) { m = Math.Max(m, pts.Min(p => Math.Sqrt(Math.Pow(p.x - c.x, 2) + Math.Pow(p.y - c.y, 2)))); if (m >= best) break; }
            if (m >= best) continue;
            foreach (var p in pts) { var d = Math.Sqrt(Math.Pow(p.x - FE[0], 2) + Math.Pow(p.y - FE[1], 2)); if (d < dEnd) { dEnd = d; sEnd = p.s; } }
            m = Math.Max(m, dEnd); if (m < best) { best = m; bLs = Ls; bR2 = R2; bth = th2; bL = sEnd; }
        }
Out.AppendLine($"arc 1: R {R1:F2} m right, {th1:F2} deg, {L1:F2} m, tangent to 44BD9C0 (az {az0:F2}) and to the straight (az {azS:F3})");
Out.AppendLine($"straight {bLs:F2} m; arc 2: R {bR2:F2} m right, {bth:F2} deg, {bR2 * Rad(bth):F2} m; end straight {bL - L1 - bLs - bR2 * Rad(bth):F2} m; max offset from end cut centres / last cut face {best:F3} m");
int nSeg = (int)Math.Round(bL / 3.465); double stp = bL / nSeg;
var NV = new List<double[]> { new[] { E[0], E[1], E[2] } };
for (int k = 1; k <= nSeg; k++) { var p = Pos(k * stp, bLs, bR2, bth); NV.Add(new[] { p.x, p.y, 0.0 }); }
foreach (var c in cuts.Where(c => c.fr.s < lastStraight.fr.s)) { double bd = 1e9; for (double s = 0; s <= bL; s += 0.05) { var p = Pos(s, bLs, bR2, bth); bd = Math.Min(bd, Math.Sqrt(Math.Pow(p.x - c.x, 2) + Math.Pow(p.y - c.y, 2))); } if (c.fr.s < L1 + 3) Out.AppendLine($"  arc 1 vs cut {c.f.Handle}: centre {bd:F3} m off the new line"); }
double ch = 0; for (int i = 1; i < NV.Count; i++) { ch += D2(NV[i], NV[i - 1]); NV[i][2] = E[2] - ch / 7; }var NewCL = M.Take(M.Count - 1).Concat(NV).ToList();
foreach (var c in cuts) { var fr = Frame(NewCL, new[] { c.x, c.y, c.z }); Out.AppendLine($"  cut {c.f.Handle} centre off {fr.off:F3}, cut floor - new CL {fr.dz:F3}"); }
Out.AppendLine($"{cuts.Count} decline cuts past the joint ({string.Join(",", cuts.Select(c => c.f.Handle))})");
Out.AppendLine($"new MAIN DEC: {NV.Count} vertices, plan {ch:F2} m (was {D.Skip(1).Select((q, i) => D2(q, D[i])).Sum():F2}), end RL {D[^1][2]:F3} -> {NV[^1][2]:F3}, end moves {D2(D[^1], NV[^1]):F2} m");
for (int i = 1; i < NV.Count; i++)
{
    string s = $"  v{i,2} ({NV[i][0]:F3},{NV[i][1]:F3},{NV[i][2]:F3}) seg {D2(NV[i], NV[i - 1]):F3} az {Az(NV[i - 1], NV[i]):F2}";
    if (i < NV.Count - 1) { var t = Turn(Az(NV[i - 1], NV[i]), Az(NV[i], NV[i + 1])); s += $" turn {t:F2}" + (Math.Abs(t) > 0.05 ? $" R~{D2(NV[i], NV[i + 1]) / Rad(Math.Abs(t)):F1}" : ""); }
    Out.AppendLine(s);
}
Out.AppendLine($"joint: 44BD9C0 last az {Az(M[^2], M[^1]):F2}, new first az {Az(NV[0], NV[1]):F2} (turn {Turn(Az(M[^2], M[^1]), Az(NV[0], NV[1])):F2}); old first az {Az(D[0], D[1]):F2} (turn {Turn(Az(M[^2], M[^1]), Az(D[0], D[1])):F2})");
Out.AppendLine("old draft vertices -> new line: " + string.Join(" ", D.Select((q, i) => $"v{i} {Math.Abs(Frame(NewCL, q).off):F2}")));
// vertex normals (bisector) on the new line, blended along each segment
var nrm = new List<double[]>();
for (int i = 0; i < NewCL.Count; i++)
{
    double nx = 0, ny = 0;
    if (i > 0) { double dx = NewCL[i][0] - NewCL[i - 1][0], dy = NewCL[i][1] - NewCL[i - 1][1], l = Math.Sqrt(dx * dx + dy * dy); nx += dy / l; ny += -dx / l; }
    if (i < NewCL.Count - 1) { double dx = NewCL[i + 1][0] - NewCL[i][0], dy = NewCL[i + 1][1] - NewCL[i][1], l = Math.Sqrt(dx * dx + dy * dy); nx += dy / l; ny += -dx / l; }
    double nl = Math.Sqrt(nx * nx + ny * ny); nrm.Add(new[] { nx / nl, ny / nl });
}
double[] OldFoot(double s0)
{
    double s = s0 + sJ, c0 = 0;
    for (int i = 1; i < OldCL.Count; i++) { double L = D2(OldCL[i], OldCL[i - 1]); if (s <= c0 + L || i == OldCL.Count - 1) { double t = Math.Min(1, (s - c0) / L); return new[] { OldCL[i - 1][0] + t * (OldCL[i][0] - OldCL[i - 1][0]), OldCL[i - 1][1] + t * (OldCL[i][1] - OldCL[i - 1][1]), 0.0 }; } c0 += L; }
    return OldCL[^1];
}
double[] Place((double s, double off, double dz) f, double[] q)
{
    if (f.s <= 0.001) return q;
    // same place along the drive: the old centreline foot point, projected onto the new line (not the same chainage)
    var foot = OldFoot(f.s); double s = Frame(NewCL, foot).s + sJ, c0 = 0;
    for (int i = 1; i < NewCL.Count; i++)
    {
        double L = D2(NewCL[i], NewCL[i - 1]);
        if (s <= c0 + L + 1e-9 || i == NewCL.Count - 1)
        {
            double t = Math.Min(1, (s - c0) / L), px = NewCL[i - 1][0] + t * (NewCL[i][0] - NewCL[i - 1][0]), py = NewCL[i - 1][1] + t * (NewCL[i][1] - NewCL[i - 1][1]), pz = NewCL[i - 1][2] + t * (NewCL[i][2] - NewCL[i - 1][2]);
            double nx = nrm[i - 1][0] + t * (nrm[i][0] - nrm[i - 1][0]), ny = nrm[i - 1][1] + t * (nrm[i][1] - nrm[i - 1][1]), nl = Math.Sqrt(nx * nx + ny * ny);
            return new[] { px + f.off * nx / nl, py + f.off * ny / nl, pz + f.dz };
        }
        c0 += L;
    }
    return q;
}
var plan = new Dictionary<string, List<double[]>>();
foreach (var h in deps)
{
    var p = P3(byH[h]); var np = p.Select(q => Place(Frame(OldCL, q), q)).ToList(); plan[h] = np;
    var mv = p.Select((q, i) => Math.Sqrt(Math.Pow(np[i][0] - q[0], 2) + Math.Pow(np[i][1] - q[1], 2) + Math.Pow(np[i][2] - q[2], 2))).ToList();
    Out.AppendLine($"{h} {A(byH[h], "Name")} [{byH[h].Layer.Name.Replace("0\\DESIGN DRAFT\\", "")}] {p.Count} pts: move max {mv.Max():F3} m, avg {mv.Average():F3} m");
}
// a drive centreline: keep its inner points, slide its start along its own line onto the same offset from the new decline
List<double[]> Drive(List<double[]> rc)
{
    var dir = new[] { rc[0][0] - rc[1][0], rc[0][1] - rc[1][1] }; double rl = Math.Sqrt(dir[0] * dir[0] + dir[1] * dir[1]); dir[0] /= rl; dir[1] /= rl;
    double W = Frame(OldCL, rc[0]).off, dzs = Frame(OldCL, rc[0]).dz, lo = rl - 3, hi = rl + 3, sg = Math.Sign(Frame(NewCL, new[] { rc[1][0], rc[1][1], 0 }).off - W);
    for (int it = 0; it < 60; it++) { double mid = (lo + hi) / 2; var g = Frame(NewCL, new[] { rc[1][0] + mid * dir[0], rc[1][1] + mid * dir[1], 0 }); if ((g.off - W) * sg > 0) lo = mid; else hi = mid; }
    var r0 = new[] { rc[1][0] + lo * dir[0], rc[1][1] + lo * dir[1], 0.0 }; var g0 = Frame(NewCL, new[] { r0[0], r0[1], 0.0 }); r0[2] = r0[2] = Place((g0.s, g0.off, dzs), r0)[2];
    // RL: decline CL RL at that point + the old start's height above the decline
    var z = NewCL; r0[2] = 0; var gg = Frame(NewCL, r0); r0[2] = -gg.dz + dzs;
    var r = new List<double[]> { r0 }; r.AddRange(rc.Skip(1)); return r;
}
var rc = P3(byH["4588D8B"]); var nrc = Drive(rc);
Out.AppendLine($"W6390 RAD CL start moves {D2(rc[0], nrc[0]):F3} m, dz {nrc[0][2] - rc[0][2]:F3}; first leg 1:{D2(rc[0], rc[1]) / (rc[1][2] - rc[0][2]):F2} -> 1:{D2(nrc[0], nrc[1]) / (nrc[1][2] - nrc[0][2]):F2}; length {D2(rc[0], rc[1]) + D2(rc[1], rc[2]):F2} -> {D2(nrc[0], nrc[1]) + D2(nrc[1], nrc[2]):F2} m");
var dc = P3(byH["4588D89"]); var ndc = Drive(dc);
double dzD = ndc[0][2] - dc[0][2]; for (int i = 1; i < ndc.Count; i++) ndc[i] = new[] { ndc[i][0], ndc[i][1], ndc[i][2] + dzD };   // whole DSP follows its mouth, stays 1:50
Out.AppendLine($"W6390 DSP CL start moves {D2(dc[0], ndc[0]):F3} m, dz {ndc[0][2] - dc[0][2]:F3}; grade 1:{D2(dc[0], dc[1]) / (dc[1][2] - dc[0][2]):F1} -> 1:{D2(ndc[0], ndc[1]) / (ndc[1][2] - ndc[0][2]):F1}; length {D2(dc[0], dc[1]):F2} -> {D2(ndc[0], ndc[1]):F2} m");
var rw = P3(byH["4588D8C"]); var nrw = rw.Select(q => new[] { q[0], q[1], q[2] }).ToList(); nrw[7] = plan["4588CAC"][0];
{
    double dx = nrc[1][0] - nrc[0][0], dy = nrc[1][1] - nrc[0][1], L = Math.Sqrt(dx * dx + dy * dy); dx /= L; dy /= L; double a = 2.924;
    double cx = nrc[0][0] + a * dx, cy = nrc[0][1] + a * dy, cz = nrc[0][2] + a / L * (nrc[1][2] - nrc[0][2]);
    var p1 = new[] { cx - 2.5 * dy, cy + 2.5 * dx, cz }; var p6 = new[] { cx + 2.5 * dy, cy - 2.5 * dx, cz };
    if (D2(p1, rw[1]) > D2(p6, rw[1])) { var t = p1; p1 = p6; p6 = t; } nrw[1] = p1; nrw[6] = p6;
}
Out.AppendLine("W6390 RAD wall moves: " + string.Join(" ", rw.Select((q, i) => $"v{i} {D2(q, nrw[i]):F3}/{nrw[i][2] - q[2]:F3}")));
var dw = P3(byH["4588D8A"]); var ndw = dw.Select(q => new[] { q[0], q[1], q[2] }).ToList(); ndw[0] = plan["4588CAC"][^1]; ndw[^1] = plan["4588CAD"][0]; for (int i = 1; i < ndw.Count - 1; i++) ndw[i][2] += dzD;
Out.AppendLine($"W6390 DSP wall ends move {D2(dw[0], ndw[0]):F3} / {D2(dw[^1], ndw[^1]):F3} m");
double Sh(List<double[]> a, List<double[]> b) => a.Min(p => b.Min(q => Math.Sqrt(Math.Pow(p[0] - q[0], 2) + Math.Pow(p[1] - q[1], 2) + Math.Pow(p[2] - q[2], 2))));
Out.AppendLine($"shared points after: CD8-CDC {Sh(plan["4588CD8"], plan["4588CDC"]) * 1000:F0} mm, CDC-CD9 {Sh(plan["4588CDC"], plan["4588CD9"]) * 1000:F0}, CDC-CDD {Sh(plan["4588CDC"], plan["4588CDD"]) * 1000:F0}, CA8-CAA {Sh(plan["4588CA8"], plan["4588CAA"]) * 1000:F0}, CAA-CA9 {Sh(plan["4588CAA"], plan["4588CA9"]) * 1000:F0}, CAA-CAB {Sh(plan["4588CAA"], plan["4588CAB"]) * 1000:F0}");
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }
var log = new System.Text.StringBuilder();
foreach (var h in new[] { "4587A08", "4588D8B", "4588D8C", "4588D89", "4588D8A" }.Concat(deps)) { log.AppendLine("== " + h); foreach (var q in P3(byH[h])) log.AppendLine($"{q[0]:R},{q[1]:R},{q[2]:R}"); }
System.IO.Directory.CreateDirectory(@"C:\dswtmp"); System.IO.File.WriteAllText(@"C:\dswtmp\mpa07_dec_on_solids_backup.txt", log.ToString());
Deswik.Graphics.Primaries.Figure Replace(Deswik.Graphics.Primaries.Figure src, List<double[]> p)
{
    var c = (Deswik.Graphics.Primaries.Figure)src.Clone(); var pl = c.asPolyline; bool cl = pl.Closed; while (pl.VertexCount > 0) pl.RemoveVertexAt(0);
    foreach (var q in p) pl.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]); pl.Closed = cl;
    App.ModelSpace.Entities.AddItem(c, null, null, true, false); src.Deleted = true; return c;
}
var res = new List<string> { "DEC " + Replace(byH["4587A08"], NV).Handle, "RAD CL " + Replace(byH["4588D8B"], nrc).Handle, "RAD wall " + Replace(byH["4588D8C"], nrw).Handle, "DSP CL " + Replace(byH["4588D89"], ndc).Handle, "DSP wall " + Replace(byH["4588D8A"], ndw).Handle };
foreach (var kv in plan) res.Add($"{kv.Key}->{Replace(byH[kv.Key], kv.Value).Handle}");
App.Redraw(true); Out.AppendLine("applied: " + string.Join(", ", res) + "; old vertices in C:\\dswtmp\\mpa07_dec_on_solids_backup.txt"); return null;
