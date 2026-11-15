// MPA 07 draft MAIN DEC (4587A08) rebuilt like the Deswik decline tool, as a continuation of 44BD9C0 (DESIGN MASTER):
// tangent to 44BD9C0's last segment at the joint, right-hand arc R1 into the existing straight (kept, so the DSP mouth and
// DSP back strip stay put), the straight, then an end arc R2 (12-20 m, fitted to the current end) and a short straight to the
// same plan length. Vertices evenly spaced along the line (~3.5 m slope like 44BD9C0), RLs exactly 1:7 down from the joint.
// Dependants on DESIGN DRAFT keep their chainage / offset / height to the decline: decline walls 4588CAC/CAD, RAD-side back
// strip CD8/CD9/CDC/CDD and wall points on the decline. RAD centreline keeps its two inner points; its start moves onto the
// new decline wall. Dry run unless APPLY. Every replaced figure is a clone (same attributes); the old one is deleted.
const bool APPLY = false;
const double STEP3D = 3.5;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
List<double[]> P3(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
double D2(double[] a, double[] b) => Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2));
double Rad(double d) => d * Math.PI / 180;
double Az(double[] a, double[] b) { var z = Math.Atan2(b[0] - a[0], b[1] - a[1]) * 180 / Math.PI; return z < 0 ? z + 360 : z; }
var byH = new Dictionary<string, Deswik.Graphics.Primaries.Figure>(StringComparer.OrdinalIgnoreCase);
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (!f.Deleted) byH[f.Handle] = f;
foreach (var h in new[] { "44BD9C0", "4587A08", "4588D8B", "4588CAC", "4588CAD", "4588D8C", "4588CD8", "4588CD9", "4588CDC", "4588CDD" })
    if (!byH.ContainsKey(h)) { Out.AppendLine($"{h} missing (changed since the check) - nothing done"); return null; }
var M = P3(byH["44BD9C0"]); var D = P3(byH["4587A08"]);
var E = M[^1]; if (D2(E, D[0]) > 0.001 || Math.Abs(E[2] - D[0][2]) > 0.001) { Out.AppendLine("draft decline no longer starts on 44BD9C0's end - nothing done"); return null; }
double az0 = Az(M[^2], M[^1]);
// existing straight = draft segment v8 -> v9 (the long one)
int si = Enumerable.Range(1, D.Count - 1).OrderByDescending(i => D2(D[i], D[i - 1])).First();
var SA = D[si - 1]; var SB = D[si]; double azS = Az(SA, SB);
double ux = Math.Sin(Rad(azS)), uy = Math.Cos(Rad(azS));
// right-hand arc from E (tangent az0) tangent to the straight line: centre C = E + R n0, (C - SA).nL = R
double n0x = Math.Cos(Rad(az0)), n0y = -Math.Sin(Rad(az0)), nLx = Math.Cos(Rad(azS)), nLy = -Math.Sin(Rad(azS));
double h0 = (E[0] - SA[0]) * nLx + (E[1] - SA[1]) * nLy, R1 = h0 / (1 - (n0x * nLx + n0y * nLy));
double th1 = azS - az0; while (th1 < 0) th1 += 360; double L1 = R1 * Rad(th1);
double C1x = E[0] + R1 * n0x, C1y = E[1] + R1 * n0y;
double T1x = C1x - R1 * nLx, T1y = C1y - R1 * nLy;            // tangent point on the straight
double sT1 = L1, oldEnd = 0; for (int i = 1; i < D.Count; i++) oldEnd += D2(D[i], D[i - 1]);
// alignment: arc1 [0,L1], straight [L1, L1+Ls], arc2 R2 th2 [.., +L2], final straight to oldEnd
(double x, double y, double az) Pos(double s, double Ls, double R2, double th2)
{
    if (s <= L1) { double a = az0 + s / R1 * 180 / Math.PI; return (C1x - R1 * Math.Cos(Rad(a)), C1y + R1 * Math.Sin(Rad(a)), a); }
    double t = s - L1; if (t <= Ls) return (T1x + t * ux, T1y + t * uy, azS);
    double Bx = T1x + Ls * ux, By = T1y + Ls * uy, C2x = Bx + R2 * nLx, C2y = By + R2 * nLy, L2 = R2 * Rad(th2); t -= Ls;
    if (t <= L2) { double a = azS + t / R2 * 180 / Math.PI; return (C2x - R2 * Math.Cos(Rad(a)), C2y + R2 * Math.Sin(Rad(a)), a); }
    double ae = azS + th2, Ex = C2x - R2 * Math.Cos(Rad(ae)), Ey = C2y + R2 * Math.Sin(Rad(ae)); t -= L2;
    return (Ex + t * Math.Sin(Rad(ae)), Ey + t * Math.Cos(Rad(ae)), ae);
}
List<double[]> Dense(double Ls, double R2, double th2, double L) { var r = new List<double[]>(); for (double s = 0; s <= L + 1e-9; s += 0.05) { var p = Pos(s, Ls, R2, th2); r.Add(new[] { p.x, p.y, s }); } return r; }
double DistTo(List<double[]> d, double[] q) => d.Min(p => Math.Sqrt(Math.Pow(p[0] - q[0], 2) + Math.Pow(p[1] - q[1], 2)));
// fit arc 2 to the draft vertices after the straight (and its end point), same total plan length
var tail = D.Skip(si).ToList(); double best = 1e9, bLs = 0, bR2 = 0, bth = 0;
double LsMax = D2(T1x, T1y, SB) ;
for (double R2 = 12; R2 <= 20.001; R2 += 0.5)
    for (double th2 = 30; th2 <= 70; th2 += 0.5)
        for (double Ls = LsMax - 12; Ls <= LsMax + 6; Ls += 0.1)
        {
            if (L1 + Ls + R2 * Rad(th2) > oldEnd) continue;
            double m = 0; foreach (var q in tail) { double dd = 1e9; for (double s = L1 + Ls - 2; s <= oldEnd; s += 0.25) { var p = Pos(s, Ls, R2, th2); dd = Math.Min(dd, Math.Sqrt(Math.Pow(p.x - q[0], 2) + Math.Pow(p.y - q[1], 2))); } m = Math.Max(m, dd); if (m >= best) break; }
            var pe = Pos(oldEnd, Ls, R2, th2); m = Math.Max(m, Math.Sqrt(Math.Pow(pe.x - D[^1][0], 2) + Math.Pow(pe.y - D[^1][1], 2)));
            if (m < best) { best = m; bLs = Ls; bR2 = R2; bth = th2; }
        }
double D2(double x, double y, double[] b) => Math.Sqrt(Math.Pow(x - b[0], 2) + Math.Pow(y - b[1], 2));
Out.AppendLine($"joint: 44BD9C0 ends az {az0:F2}; draft starts az {Az(D[0], D[1]):F2} -> kink {Az(D[0], D[1]) - az0:F2} deg (removed)");
Out.AppendLine($"arc 1: R {R1:F2} m right, {th1:F2} deg, {L1:F2} m, meets the existing straight (az {azS:F2}) at ({T1x:F2},{T1y:F2})");
Out.AppendLine($"straight: {bLs:F2} m; arc 2: R {bR2:F1} m right, {bth:F1} deg, {bR2 * Rad(bth):F2} m; end straight {oldEnd - L1 - bLs - bR2 * Rad(bth):F2} m; plan length {oldEnd:F2} m (unchanged); max offset from the current end section {best:F2} m");
// even vertices
double Lt = oldEnd, stepPlan = STEP3D * 7 / Math.Sqrt(50); int n = (int)Math.Round(Lt / stepPlan); double st = Lt / n;
var NV = new List<double[]>(); double chord = 0; double[] prev = null;
for (int k = 0; k <= n; k++) { var p = Pos(k * st, bLs, bR2, bth); var q = new[] { p.x, p.y, 0.0 }; if (prev != null) chord += D2(prev, q); q[2] = E[2] - chord / 7; NV.Add(q); prev = q; }
NV[0] = new[] { E[0], E[1], E[2] };
var segs = Enumerable.Range(1, NV.Count - 1).Select(i => D2(NV[i], NV[i - 1])).ToList();
Out.AppendLine($"new MAIN DEC: {NV.Count} vertices, plan segments {segs.Min():F3}-{segs.Max():F3} m (3D {segs.Min() * Math.Sqrt(50) / 7:F2}-{segs.Max() * Math.Sqrt(50) / 7:F2}), end RL {D[^1][2]:F3} -> {NV[^1][2]:F3}, end moves {D2(D[^1], NV[^1]):F2} m");
var dense = Dense(bLs, bR2, bth, Lt);
Out.AppendLine("current draft vertices -> new line: " + string.Join(" ", D.Select((q, i) => $"v{i} {DistTo(dense, q):F2}")));

// curvilinear frames. old = 44BD9C0 + draft (chainage 0 at the joint); new = 44BD9C0 + new vertices
List<double[]> OldCL = M.Take(M.Count - 1).Concat(D).ToList(), NewCL = M.Take(M.Count - 1).Concat(NV).ToList();
double sJ = 0; for (int i = 1; i < M.Count; i++) sJ += D2(M[i], M[i - 1]);
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
// new point at old (s, off, dz): s >= 0 on the analytic new line (chainage rescaled to chord length), s < 0 unchanged
double[] Place((double s, double off, double dz) f, double[] q)
{
    if (f.s <= 0.001) return q;
    var p = Pos(Math.Min(f.s, Lt), bLs, bR2, bth); double nx = Math.Cos(Rad(p.az)), ny = -Math.Sin(Rad(p.az));
    double x = p.x + f.off * nx, y = p.y + f.off * ny;
    var fn = Frame(NewCL, new[] { p.x, p.y, 0 }); double zc = 0; { var g = Frame(NewCL, new[] { p.x, p.y, 0.0 }); }
    // RL of the new centreline at that plan point
    double z = NewCLz(p.x, p.y); return new[] { x, y, z + f.dz };
}
double NewCLz(double x, double y)
{
    double bd = 1e9, bz = 0;
    for (int i = 1; i < NewCL.Count; i++)
    {
        double vx = NewCL[i][0] - NewCL[i - 1][0], vy = NewCL[i][1] - NewCL[i - 1][1], L2 = vx * vx + vy * vy, t = Math.Max(0, Math.Min(1, ((x - NewCL[i - 1][0]) * vx + (y - NewCL[i - 1][1]) * vy) / L2));
        double d = Math.Pow(NewCL[i - 1][0] + t * vx - x, 2) + Math.Pow(NewCL[i - 1][1] + t * vy - y, 2); if (d < bd) { bd = d; bz = NewCL[i - 1][2] + t * (NewCL[i][2] - NewCL[i - 1][2]); }
    }
    return bz;
}
var plan = new Dictionary<string, List<double[]>>();
foreach (var h in new[] { "4588CAC", "4588CAD", "4588CD8", "4588CD9", "4588CDC", "4588CDD" })
{
    var p = P3(byH[h]); var np = p.Select(q => Place(Frame(OldCL, q), q)).ToList(); plan[h] = np;
    var mv = p.Select((q, i) => Math.Sqrt(Math.Pow(np[i][0] - q[0], 2) + Math.Pow(np[i][1] - q[1], 2) + Math.Pow(np[i][2] - q[2], 2))).ToList();
    Out.AppendLine($"{h} {A(byH[h], "Name")} [{byH[h].Layer.Name.Replace("0\\DESIGN DRAFT\\", "")}] {p.Count} pts move max {mv.Max():F2} m, avg {mv.Average():F2} m");
}
// RAD centreline: keep v1, v2; v0 = where the line v1->v0 meets the new left decline wall (off -2.75), RL = new decline RL there
var rc = P3(byH["4588D8B"]); var rdir = new[] { rc[0][0] - rc[1][0], rc[0][1] - rc[1][1] }; double rl = Math.Sqrt(rdir[0] * rdir[0] + rdir[1] * rdir[1]); rdir[0] /= rl; rdir[1] /= rl;
double Woff = Frame(OldCL, rc[0]).off; double lo = rl - 3, hi = rl + 3;
for (int it = 0; it < 60; it++) { double mid = (lo + hi) / 2; var q = new[] { rc[1][0] + mid * rdir[0], rc[1][1] + mid * rdir[1], 0 }; var g = Frame(NewCL, q); if (g.off < Woff) hi = mid; else lo = mid; }
var r0 = new[] { rc[1][0] + lo * rdir[0], rc[1][1] + lo * rdir[1], 0.0 }; r0[2] = NewCLz(r0[0], r0[1]);
var nrc = new List<double[]> { r0, rc[1], rc[2] };
Out.AppendLine($"W6390 RAD CL start {D2(rc[0], r0):F2} m along its own line (off {Woff:F2}); first leg {rl:F2} -> {D2(r0, rc[1]):F2} m, grade 1:{rl / (rc[1][2] - rc[0][2]):F2} -> 1:{D2(r0, rc[1]) / (rc[1][2] - r0[2]):F2}; RAD length {rl + D2(rc[1], rc[2]):F2} -> {D2(r0, rc[1]) + D2(rc[1], rc[2]):F2} m");
// RAD walls: v0 on 44BD9C0's wall (unchanged), v7 on the decline wall (remapped), v1/v6 = 2.92 m into the RAD, 2.5 m each side of the new CL, RL on the RAD grade; v2-v5 unchanged
var rw = P3(byH["4588D8C"]); var nrw = rw.Select(q => new[] { q[0], q[1], q[2] }).ToList();
nrw[7] = Place(Frame(OldCL, rw[7]), rw[7]);
{
    double dx = rc[1][0] - r0[0], dy = rc[1][1] - r0[1], L = Math.Sqrt(dx * dx + dy * dy); dx /= L; dy /= L; double a = 2.924;
    double cx = r0[0] + a * dx, cy = r0[1] + a * dy, cz = r0[2] + a / L * (rc[1][2] - r0[2]);
    nrw[1] = new[] { cx - 2.5 * dy, cy + 2.5 * dx, cz }; nrw[6] = new[] { cx + 2.5 * dy, cy - 2.5 * dx, cz };
    if (D2(nrw[1], rw[1]) > D2(nrw[1], rw[6])) { var t = nrw[1]; nrw[1] = nrw[6]; nrw[6] = t; }
}
Out.AppendLine("W6390 RAD wall moves: " + string.Join(" ", rw.Select((q, i) => $"v{i} {D2(q, nrw[i]):F2}/{nrw[i][2] - q[2]:F3}")));
// decline wall ends must still meet the DSP / RAD walls
Out.AppendLine($"ties: CAC start-RAD wall end {D2(plan["4588CAC"][0], nrw[7]) * 1000:F0} mm, CAC end-DSP wall start {D2(plan["4588CAC"][^1], P3(byH["4588D8A"])[0]) * 1000:F0} mm, CAD start-DSP wall end {D2(plan["4588CAD"][0], P3(byH["4588D8A"])[^1]) * 1000:F0} mm");
// back strip sanity: shared vertices CDC/CDD and CD8/CDC/CD9 still coincide
double Sh(List<double[]> a, List<double[]> b) => a.Min(p => b.Min(q => Math.Sqrt(Math.Pow(p[0] - q[0], 2) + Math.Pow(p[1] - q[1], 2) + Math.Pow(p[2] - q[2], 2))));
Out.AppendLine($"back strip shared points: CD8-CDC {Sh(plan["4588CD8"], plan["4588CDC"]) * 1000:F0} mm, CDC-CD9 {Sh(plan["4588CDC"], plan["4588CD9"]) * 1000:F0} mm, CDC-CDD {Sh(plan["4588CDC"], plan["4588CDD"]) * 1000:F0} mm");
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }
Deswik.Graphics.Primaries.Figure Replace(Deswik.Graphics.Primaries.Figure src, List<double[]> p)
{
    var c = (Deswik.Graphics.Primaries.Figure)src.Clone(); var pl = c.asPolyline; bool cl = pl.Closed; while (pl.VertexCount > 0) pl.RemoveVertexAt(0);
    foreach (var q in p) pl.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]); pl.Closed = cl;
    App.ModelSpace.Entities.AddItem(c, null, null, true, false); src.Deleted = true; return c;
}
var log = new System.Text.StringBuilder();
void Keep(string h) { log.AppendLine("== " + h); foreach (var q in P3(byH[h])) log.AppendLine($"{q[0]:R},{q[1]:R},{q[2]:R}"); }
foreach (var h in new[] { "4587A08", "4588D8B", "4588D8C" }.Concat(plan.Keys)) Keep(h);
System.IO.File.WriteAllText(@"C:\dswtmp\mpa07_dec_rebuild_backup.txt", log.ToString());
var nd = Replace(byH["4587A08"], NV); var nr = Replace(byH["4588D8B"], nrc); var nw = Replace(byH["4588D8C"], nrw);
var res = new List<string> { $"DEC {nd.Handle}", $"RAD CL {nr.Handle}", $"RAD wall {nw.Handle}" };
foreach (var kv in plan) res.Add($"{kv.Key}->{Replace(byH[kv.Key], kv.Value).Handle}");
App.Redraw(true); Out.AppendLine("applied: " + string.Join(", ", res) + "; old vertices in C:\\dswtmp\\mpa07_dec_rebuild_backup.txt"); return null;
