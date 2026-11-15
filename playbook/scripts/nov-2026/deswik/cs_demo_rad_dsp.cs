// MPA 07 (Level Design Drafter), 3 Nov 2026:
// - W6390 RAD take-off moved 5.0 m down the decline (along the decline CL), squared to the decline there; new profile
//   from the decline wall: 10 m at +1:50, 20 m flat, 20 m at 1:7 down (50 m plan). Walls at 2.5 m with 3 x 3 m fillets,
//   back strip rebuilt to the standard at the new take-off (drop-down 4.5 m), COG markers at both grade breaks, the 9
//   cable bolts at the RAD mouth moved with the take-off, decline left wall rebuilt across the old and new mouths.
// - W6390 DSP: 6.0 x 6.0 (walls at 3.0 m, 3 x 3 m fillets), 5 m shorter (15.0 m 3D), same start, bearing and 1:50 grade;
//   back strip drop-down 5.5 m (drive width - 0.5 m); decline walls tied to the wider mouth.
// Dry run unless APPLY. Old vertices / positions backed up to C:\dswtmp\demo_rad_dsp_backup.txt.
const bool APPLY = false;
const double MOVE = 5.0;
List<double[]> P3(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
double D2(double[] a, double[] b) => Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2));
double D3(double[] a, double[] b) => Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2) + Math.Pow(a[2] - b[2], 2));
double Az(double[] a, double[] b) { var z = Math.Atan2(b[0] - a[0], b[1] - a[1]) * 180 / Math.PI; return z < 0 ? z + 360 : z; }
double Turn(double a1, double a2) { var d = a2 - a1; while (d > 180) d -= 360; while (d < -180) d += 360; return d; }
var H = new Dictionary<string, Deswik.Graphics.Primaries.Figure>(StringComparer.OrdinalIgnoreCase);
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (!f.Deleted && f.Layer != null) H[f.Handle] = f;
var need = new[] { "44BD9C0", "4588F43", "4588F6B", "4588FB0", "4588F65", "4588F66", "4588FB5", "4588FB6", "4588F6D", "4588F6F", "4588F71", "4588F73", "4588F6E", "4588F70", "4588F72", "4588F74", "45879C0", "45879C1" };
foreach (var h in need) if (!H.ContainsKey(h)) { Out.AppendLine($"{h} missing - nothing done"); return null; }
if (!App.Filename.Contains("Level Design Drafter")) { Out.AppendLine("WRONG WINDOW " + App.Filename); return null; }
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
// point at chainage s (0 = joint), offset off (+ right), decline floor RL; blended right normal
(double[] p, double[] n) At(double s0, double off)
{
    double s = s0 + sJ, c0 = 0;
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
var ch = new List<double> { -sJ }; for (int i = 1; i < CL.Count; i++) ch.Add(ch[i - 1] + D2(CL[i], CL[i - 1]));
double[] Mitre(int i, double off)
{
    double ax = 0, ay = 0, bx = 0, by = 0;
    if (i > 0) { double dx = CL[i][0] - CL[i - 1][0], dy = CL[i][1] - CL[i - 1][1], l = Math.Sqrt(dx * dx + dy * dy); ax = dy / l; ay = -dx / l; }
    if (i < CL.Count - 1) { double dx = CL[i + 1][0] - CL[i][0], dy = CL[i + 1][1] - CL[i][1], l = Math.Sqrt(dx * dx + dy * dy); bx = dy / l; by = -dx / l; }
    if (i == 0) { ax = bx; ay = by; } if (i == CL.Count - 1) { bx = ax; by = ay; }
    double nx = ax + bx, ny = ay + by, nl = Math.Sqrt(nx * nx + ny * ny); nx /= nl; ny /= nl; double k = 1 / (nx * ax + ny * ay);
    return new[] { CL[i][0] + off * k * nx, CL[i][1] + off * k * ny, CL[i][2] };
}
List<double[]> Stations(double s0, double s1, double off) { var r = new List<double[]>(); var idx = Enumerable.Range(0, CL.Count).Where(i => ch[i] > Math.Min(s0, s1) + 0.3 && ch[i] < Math.Max(s0, s1) - 0.3).ToList(); if (s1 < s0) idx.Reverse(); foreach (var i in idx) r.Add(Mitre(i, off)); return r; }
double ChOf(double[] q, double off) { double best = 1e9, bs = 0; for (double s = -12; s <= 85; s += 0.005) { var dd = D2(At(s, off).p, q); if (dd < best) { best = dd; bs = s; } } return bs; }
double OffAt(double[] q) { double bd = 1e9, bo = 0; for (int i = 1; i < CL.Count; i++) { double vx = CL[i][0] - CL[i - 1][0], vy = CL[i][1] - CL[i - 1][1], L = Math.Sqrt(vx * vx + vy * vy), t = Math.Max(0, Math.Min(1, ((q[0] - CL[i - 1][0]) * vx + (q[1] - CL[i - 1][1]) * vy) / (L * L))); double dd = Math.Sqrt(Math.Pow(CL[i - 1][0] + t * vx - q[0], 2) + Math.Pow(CL[i - 1][1] + t * vy - q[1], 2)); if (dd < bd) { bd = dd; bo = ((q[0] - CL[i - 1][0]) * vy - (q[1] - CL[i - 1][1]) * vx) / L; } } return bo; }
double FloorAt(double[] q) { double best = 1e9, bz = 0; for (int i = 1; i < CL.Count; i++) { double vx = CL[i][0] - CL[i - 1][0], vy = CL[i][1] - CL[i - 1][1], L2 = vx * vx + vy * vy, t = Math.Max(0, Math.Min(1, ((q[0] - CL[i - 1][0]) * vx + (q[1] - CL[i - 1][1]) * vy) / L2)); double dd = Math.Pow(CL[i - 1][0] + t * vx - q[0], 2) + Math.Pow(CL[i - 1][1] + t * vy - q[1], 2); if (dd < best) { best = dd; bz = CL[i - 1][2] + t * (CL[i][2] - CL[i - 1][2]); } } return bz; }

// fillet ends for a drive starting at S on the left decline wall, unit direction (ux,uy), first-segment grade gr, half width hw:
// corner K = drive wall line meets the decline wall line (2.75 m off the CL); F = 3 m into the drive; E = 3 m along the decline wall
(double[] E, double[] F, double sE) Fillet(double[] S, double ux, double uy, double gr, double side)
{
    var o = new[] { S[0] + side * uy, S[1] - side * ux }; double lo = -8, hi = 8; double g(double a) => OffAt(new[] { o[0] + a * ux, o[1] + a * uy }) + 2.75;
    for (int it = 0; it < 80; it++) { double m = (lo + hi) / 2; if (Math.Sign(g(m)) == Math.Sign(g(lo))) lo = m; else hi = m; }
    var K = new[] { o[0] + lo * ux, o[1] + lo * uy }; var F = new[] { K[0] + 3.0 * ux, K[1] + 3.0 * uy, S[2] + (lo + 3.0) * gr };
    double sK = ChOf(new[] { K[0], K[1], 0 }, -2.75); double sS = ChOf(S, -2.75); double dir = ChOf(new[] { o[0], o[1], 0 }, -2.75) < sS ? -1 : 1;
    double a = sK, b = sK + dir * 5; for (int it = 0; it < 80; it++) { double m = (a + b) / 2; if (D2(At(m, -2.75).p, K) < 3.0) a = m; else b = m; }
    var E = At((a + b) / 2, -2.75).p; return (E, F, (a + b) / 2);
}
// truck loading back strip to the standard (as cs_strip_narrow.cs): 8 m flat 7.146 m above the floor, 3.5 m ramps to 5.646,
// drop-down W wide x 3.5 m deep x 1.5 m drop, top edge 0.75 m into the drive, flat starting at its up-dip edge
const double ZS = 7.146, ZE = 5.646, VF = -2.49, VN = 2.51;
double[] UV(double s0, double u, double v, double z) { var a = At(s0 + u, -v); return new[] { a.p[0], a.p[1], a.p[2] + z }; }
var stripLog = new List<string>();
List<List<double[]>> Strip(double s0, double[] start, double[] inner, double W)
{
    double ux2 = inner[0] - start[0], uy2 = inner[1] - start[1], l2 = Math.Sqrt(ux2 * ux2 + uy2 * uy2); ux2 /= l2; uy2 /= l2;
    double sideSign = 1; { var t = new[] { start[0] + uy2, start[1] - ux2, 0.0 }; if (ChOf(t, -2.75) < s0) sideSign = -1; }
    double[] DC(double along, double side) => new[] { start[0] + along * ux2 + side * sideSign * uy2, start[1] + along * uy2 - side * sideSign * ux2, 0.0 };
    var tUp = DC(0.75, -W / 2); var tDn = DC(0.75, W / 2); var bDn = DC(4.25, W / 2); var bUp = DC(4.25, -W / 2);
    foreach (var q in new[] { tUp, tDn }) q[2] = FloorAt(q) + ZS; bDn[2] = tDn[2] - 1.5; bUp[2] = tUp[2] - 1.5;
    double u0 = ChOf(tUp, -2.75) - s0, u1 = u0 + 8.0;
    var Ar = new List<double[]>(); for (int i = 0; i <= 7; i++) { double u = u0 - 3.5 + i * 0.5; Ar.Add(UV(s0, u, VF, ZE + (ZS - ZE) * i / 7.0)); } for (int i = 7; i >= 0; i--) { double u = u0 - 3.5 + i * 0.5; Ar.Add(UV(s0, u, VN, ZE + (ZS - ZE) * i / 7.0)); }
    var C = new List<double[]>(); for (int i = 0; i <= 16; i++) C.Add(UV(s0, u0 + i * 0.5, VF, ZS)); C.Add(UV(s0, u1, VN, ZS)); C.Add(tDn); C.Add(tUp); C.Add(UV(s0, u0, VN, ZS));
    var B = new List<double[]>(); for (int i = 0; i <= 7; i++) { double u = u1 + i * 0.5; B.Add(UV(s0, u, VF, ZS - (ZS - ZE) * i / 7.0)); } for (int i = 7; i >= 0; i--) { double u = u1 + i * 0.5; B.Add(UV(s0, u, VN, ZS - (ZS - ZE) * i / 7.0)); }
    stripLog.Add($"flat ch {s0 + u0:F2}..{s0 + u1:F2}, drop-down {D2(tUp, tDn):F2} wide x {D2(tDn, bDn):F2} deep, drop {tUp[2] - bUp[2]:F2}");
    return new List<List<double[]>> { Ar, C, B, new List<double[]> { tUp, tDn, bDn, bUp } };
}

// ================= RAD =================
var rc = P3(H["4588F6B"]); var rw = P3(H["4588FB0"]);
double sOld = ChOf(rc[0], -2.75), sNew = sOld + MOVE;
var at = At(sNew, -2.75); var S = at.p;            // on the left decline wall, decline floor RL
double ux = -at.n[0], uy = -at.n[1];               // square to the decline, to the left (into the RAD)
double azOld = Az(rc[0], rc[1]), azNew = Az(S, new[] { S[0] + ux, S[1] + uy }), dAz = Turn(azOld, azNew);
var r1 = new[] { S[0] + 10 * ux, S[1] + 10 * uy, S[2] + 10.0 / 50 };
var r2 = new[] { r1[0] + 20 * ux, r1[1] + 20 * uy, r1[2] };
var r3 = new[] { r2[0] + 20 * ux, r2[1] + 20 * uy, r2[2] - 20.0 / 7 };
var radCL = new List<double[]> { S, r1, r2, r3 };
double len3 = D3(S, r1) + D3(r1, r2) + D3(r2, r3);
var fa = Fillet(S, ux, uy, 1.0 / 50, 2.5); var fb = Fillet(S, ux, uy, 1.0 / 50, -2.5); var up = fa.sE < fb.sE ? fa : fb; var dn = fa.sE < fb.sE ? fb : fa;
double sideUp = fa.sE < fb.sE ? 2.5 : -2.5;
double[] Side(double[] c, double side) => new[] { c[0] + side * uy, c[1] - side * ux, c[2] };
var radW = new List<double[]> { up.E, up.F, Side(r1, sideUp), Side(r2, sideUp), Side(r3, sideUp), Side(r3, -sideUp), Side(r2, -sideUp), Side(r1, -sideUp), dn.F, dn.E };
Out.AppendLine($"RAD take-off: decline ch {sOld:F2} -> {sNew:F2} ({D2(rc[0], S):F2} m plan, start RL {rc[0][2]:F3} -> {S[2]:F3}); bearing {azOld:F2} -> {azNew:F2} (square to the decline, change {dAz:+0.00;-0.00})");
Out.AppendLine($"RAD CL: 10.00 m +1:50 to RL {r1[2]:F3}, 20.00 m flat, 20.00 m 1:7 down to RL {r3[2]:F3}; plan 50.00 m, 3D {len3:F2} m; end ({r3[0]:F2},{r3[1]:F2})");
Out.AppendLine($"RAD mouth ch {up.sE:F2}..{dn.sE:F2} ({D2(up.E, dn.E):F2} m across); fillet chords {D2(up.E, up.F):F2}/{D2(dn.E, dn.F):F2} (3x3 = 4.24); old mouth ch {ChOf(rw[0], -2.75):F2}..{ChOf(rw[^1], -2.75):F2}");
// left decline wall: new piece from the old up-dip end (Design Master wall end) to the new up-dip fillet end
var oldUpE = rw[0]; double sOldUp = ChOf(oldUpE, -2.75);
var gap = new List<double[]> { oldUpE }; gap.AddRange(Stations(sOldUp, up.sE, -2.75)); gap.Add(up.E);
// ================= DSP =================
var dc = P3(H["4588F65"]); var dw = P3(H["4588F66"]); var DS = dc[0];
double dux = dc[1][0] - DS[0], duy = dc[1][1] - DS[1], dl = Math.Sqrt(dux * dux + duy * duy); dux /= dl; duy /= dl; double dgr = (dc[1][2] - DS[2]) / dl;
double dPlan = 15.0 / Math.Sqrt(1 + dgr * dgr); var dE = new[] { DS[0] + dPlan * dux, DS[1] + dPlan * duy, DS[2] + dPlan * dgr };
var ga = Fillet(DS, dux, duy, dgr, 3.0); var md = Fillet(DS, dux, duy, dgr, -3.0); var dup = ga.sE < md.sE ? ga : md; var ddn = ga.sE < md.sE ? md : ga; double dSideUp = ga.sE < md.sE ? 3.0 : -3.0;
double[] DSide(double[] c, double side) => new[] { c[0] + side * duy, c[1] - side * dux, c[2] };
var dspW = new List<double[]> { dup.E, dup.F, DSide(dE, dSideUp), DSide(dE, -dSideUp), ddn.F, ddn.E };
Out.AppendLine($"DSP: start kept ({DS[0]:F2},{DS[1]:F2},{DS[2]:F3}), bearing {Az(dc[0], dc[1]):F2}, 1:{1 / dgr:F1}; 3D length {D3(dc[0], dc[1]):F2} -> {D3(DS, dE):F2} m; walls 2.75 -> 3.00 m off the CL; end face {D2(dspW[2], dspW[3]):F2} m");
Out.AppendLine($"DSP mouth ch {dup.sE:F2}..{ddn.sE:F2} ({D2(dup.E, ddn.E):F2} m across, was {D2(dw[0], dw[^1]):F2}); fillet chords {D2(dup.E, dup.F):F2}/{D2(ddn.E, ddn.F):F2}");
// decline left wall between the two mouths, and the start of the DSP -> face -> right wall string
var fb5 = new List<double[]> { dn.E }; fb5.AddRange(Stations(dn.sE, dup.sE, -2.75)); fb5.Add(dup.E);
var w6 = P3(H["4588FB6"]); int k6 = 0; while (k6 < w6.Count && Math.Abs(OffAt(w6[k6]) + 2.75) < 0.05 && ChOf(w6[k6], -2.75) < ddn.sE + 0.3) k6++;
var fb6 = new List<double[]> { ddn.E }; fb6.AddRange(w6.Skip(k6));
Out.AppendLine($"decline left wall: new piece ch {sOldUp:F2}..{up.sE:F2} ({gap.Count} pts); RAD->DSP {fb5.Count} pts (was {P3(H["4588FB5"]).Count}); DSP->face->right: {k6} leading pt(s) replaced by the new fillet end");
// ================= strips =================
var radS = Strip(sNew, S, r1, 4.5); var dspS = Strip(ChOf(DS, -2.75), DS, dE, 5.5);
Out.AppendLine("RAD strip: " + stripLog[0]); Out.AppendLine("DSP strip: " + stripLog[1]);
var radSH = new[] { "4588F6D", "4588F6F", "4588F71", "4588F73" }; var dspSH = new[] { "4588F6E", "4588F70", "4588F72", "4588F74" };
for (int i = 0; i < 4; i++) Out.AppendLine($"  {radSH[i]} moves up to {P3(H[radSH[i]]).Zip(radS[i], (a, b) => D3(a, b)).Max():F2} m; {dspSH[i]} up to {P3(H[dspSH[i]]).Zip(dspS[i], (a, b) => D3(a, b)).Max():F2} m");
// ================= COG markers (6 m red line across + "COG" text) at both grade breaks =================
var cogL = P3(H["45879C0"]); var cogT = H["45879C1"].asText; var oldBk = rc[1];
var tOff = new[] { cogT.InsertionPoint.x - oldBk[0], cogT.InsertionPoint.y - oldBk[1], cogT.InsertionPoint.z - oldBk[2] };
double ra = -dAz * Math.PI / 180;  // plan rotation (bearing +dAz = clockwise)
double[] Rot(double[] v) => new[] { v[0] * Math.Cos(ra) - v[1] * Math.Sin(ra), v[0] * Math.Sin(ra) + v[1] * Math.Cos(ra), v[2] };
var cogs = new[] { r1, r2 }.Select(b => (line: new List<double[]> { Side(b, 3.0), Side(b, -3.0) }, txt: new[] { b[0] + Rot(tOff)[0], b[1] + Rot(tOff)[1], b[2] + tOff[2] }, rot: cogT.Rotation - dAz)).ToList();
Out.AppendLine($"COG markers at plan 10.0 m (RL {r1[2]:F3}) and 30.0 m (RL {r2[2]:F3}); text rotation {cogT.Rotation:F2} -> {cogs[0].rot:F2}");
// ================= cable bolts at the RAD mouth: rigid move with the take-off =================
var cabs = H.Values.Where(f => f.IsPoint && f.Layer.Name == "0\\DESIGN DRAFT\\CABLEBOLTS").Where(f => { var q = f.asPoint.InsertionPoint; return D2(new[] { q.x, q.y }, rc[0]) < 6.5; }).ToList();
double[] MoveCab(double[] q) { var v = Rot(new[] { q[0] - rc[0][0], q[1] - rc[0][1], 0 }); return new[] { S[0] + v[0], S[1] + v[1], q[2] + (S[2] - rc[0][2]) }; }
Out.AppendLine($"cable bolts within 6.5 m of the old RAD start: {cabs.Count} (moved with the take-off)");
// ================= checks =================
var bh = H.Values.Where(f => f.IsText && f.Layer.Name.StartsWith("0\\DESIGN DRAFT\\NOTES\\HAZARDS")).ToList();
double SegDist(double[] p, double[] a, double[] b) { double vx = b[0] - a[0], vy = b[1] - a[1], t = Math.Max(0, Math.Min(1, ((p[0] - a[0]) * vx + (p[1] - a[1]) * vy) / (vx * vx + vy * vy))); return Math.Sqrt(Math.Pow(a[0] + t * vx - p[0], 2) + Math.Pow(a[1] + t * vy - p[1], 2)); }
foreach (var t in bh) { var q = t.asText.InsertionPoint; var p = new[] { q.x, q.y }; double dr = Enumerable.Range(1, 3).Min(i => SegDist(p, radCL[i - 1], radCL[i])), dd = SegDist(p, DS, dE); Out.AppendLine($"  hole {t.asText.TextString}: {dr:F1} m from the RAD CL, {dd:F1} m from the DSP CL"); }
foreach (var f in H.Values.Where(f => f.IsPolyline && f.Layer.Name == "0\\DESIGN DRAFT\\CENTERLINES" && f.Handle != "4588F6B" && f.Handle != "4588F43"))
{
    var p = P3(f); double best = 1e9; double[] bp = null; double bz = 0;
    for (int i = 1; i < p.Count; i++) for (double t = 0; t <= 1; t += 0.02) { var q = new[] { p[i - 1][0] + t * (p[i][0] - p[i - 1][0]), p[i - 1][1] + t * (p[i][1] - p[i - 1][1]), p[i - 1][2] + t * (p[i][2] - p[i - 1][2]) }; double dd = Enumerable.Range(1, 3).Min(j => SegDist(q, radCL[j - 1], radCL[j])); if (dd < best) { best = dd; bp = q; } }
    if (best < 6) { double along = (bp[0] - S[0]) * ux + (bp[1] - S[1]) * uy; double zr = along <= 10 ? S[2] + along / 50 : along <= 30 ? r1[2] : r1[2] - (along - 30) / 7; Out.AppendLine($"  CROSSING: {f.Handle} {A(f, "ACT_TYPE")} L{A(f, "LEVEL")} within {best:F1} m (plan) of the new RAD CL at {along:F1} m along, its RL {bp[2]:F2} vs RAD floor {zr:F2}"); }
}
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }
// ================= apply =================
var log = new System.Text.StringBuilder();
foreach (var h in new[] { "4588F6B", "4588FB0", "4588F65", "4588F66", "4588FB5", "4588FB6", "45879C0" }.Concat(radSH).Concat(dspSH)) { log.AppendLine("== " + h); foreach (var q in P3(H[h])) log.AppendLine($"{q[0]:R},{q[1]:R},{q[2]:R}"); }
log.AppendLine($"== 45879C1 text {cogT.InsertionPoint.x:R},{cogT.InsertionPoint.y:R},{cogT.InsertionPoint.z:R} rot {cogT.Rotation:R}");
foreach (var c in cabs) { var q = c.asPoint.InsertionPoint; log.AppendLine($"== cable {c.Handle} {q.x:R},{q.y:R},{q.z:R}"); }
System.IO.File.WriteAllText(@"C:\dswtmp\demo_rad_dsp_backup.txt", log.ToString());
Deswik.Graphics.Primaries.Figure NewPl(Deswik.Graphics.Primaries.Figure src, List<double[]> p) { var k = (Deswik.Graphics.Primaries.Figure)src.Clone(); k.Deleted = false; var pl = k.asPolyline; bool cl = pl.Closed; while (pl.VertexCount > 0) pl.RemoveVertexAt(0); foreach (var q in p) pl.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]); pl.Closed = cl; App.ModelSpace.Entities.AddItem(k, null, null, true, false); return k; }
var res = new List<string>();
void Rep(string h, List<double[]> p) { var k = NewPl(H[h], p); H[h].Deleted = true; res.Add(h + "->" + k.Handle); }
var gapFig = NewPl(H["4588FB5"], gap); res.Add("new left wall " + gapFig.Handle);
var cog2 = NewPl(H["45879C0"], cogs[1].line); res.Add("new COG line " + cog2.Handle);
var cogT2 = (Deswik.Graphics.Primaries.Figure)H["45879C1"].Clone(); cogT2.Deleted = false; App.ModelSpace.Entities.AddItem(cogT2, null, null, true, false);
Rep("4588F6B", radCL); Rep("4588FB0", radW); Rep("4588FB5", fb5); Rep("4588FB6", fb6);
Rep("4588F65", new List<double[]> { DS, dE }); Rep("4588F66", dspW); Rep("45879C0", cogs[0].line);
for (int i = 0; i < 4; i++) { Rep(radSH[i], radS[i]); Rep(dspSH[i], dspS[i]); }
// DSP profile 6 x 6 on its CL and wall (the Drafter annotations pick it up)
foreach (var f in App.ModelSpace.Entities.Cast<Deswik.Graphics.Primaries.Figure>().Where(f => !f.Deleted && f.Layer != null && f.IsPolyline && A(f, "Name") == "W6390 DSP" && (f.Layer.Name == "0\\DESIGN DRAFT\\CENTERLINES" || f.Layer.Name == "0\\DESIGN DRAFT\\WALLS"))) f.AttributeValueSet("PROFILE", "6.0mW x 6.0mH SA");
// COG texts and cables
void MoveText(Deswik.Graphics.Primaries.Figure t, double[] to, double rot) { var q = t.asText.InsertionPoint; t.Translate(to[0] - q.x, to[1] - q.y, to[2] - q.z); t.asText.Rotation = rot; }
MoveText(H["45879C1"], cogs[0].txt, cogs[0].rot); MoveText(cogT2, cogs[1].txt, cogs[1].rot); res.Add("new COG text " + cogT2.Handle);
foreach (var c in cabs) { var q = c.asPoint.InsertionPoint; var to = MoveCab(new[] { q.x, q.y, q.z }); c.Translate(to[0] - q.x, to[1] - q.y, to[2] - q.z); }
App.Redraw(true);
Out.AppendLine("applied: " + string.Join(", ", res) + $"; {cabs.Count} cables moved; DSP PROFILE 6.0mW x 6.0mH SA; backup C:\\dswtmp\\demo_rad_dsp_backup.txt");
return null;
