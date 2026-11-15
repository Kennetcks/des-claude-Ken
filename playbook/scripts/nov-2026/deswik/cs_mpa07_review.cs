// Read-only review of the MPA 07 draft (Level Design Drafter): every figure on 0\DESIGN DRAFT near the W6390 decline /
// RAD / DSP, with checks on geometry, ties, attributes, leftovers, Drafter outputs, strips vs drives, hazards and
// Design Master figures it meets. Prints findings only (one line each); nothing is changed.
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
List<double[]> P3(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
double D2(double[] a, double[] b) => Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2));
double D3(double[] a, double[] b) => Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2) + Math.Pow(a[2] - b[2], 2));
double Az(double[] a, double[] b) { var z = Math.Atan2(b[0] - a[0], b[1] - a[1]) * 180 / Math.PI; return z < 0 ? z + 360 : z; }
double Turn(double a1, double a2) { var d = a2 - a1; while (d > 180) d -= 360; while (d < -180) d += 360; return d; }
var all = new List<Deswik.Graphics.Primaries.Figure>(); foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (!f.Deleted && f.Layer != null) all.Add(f);
var H = all.ToDictionary(f => f.Handle, StringComparer.OrdinalIgnoreCase);
double[] C0 = { 8105, 63092 }; bool Near(Deswik.Graphics.Primaries.Figure f, double r) { var m = f.BoundingBox.Midpoint; return Math.Sqrt(Math.Pow(m.x - C0[0], 2) + Math.Pow(m.y - C0[1], 2)) < r; }
var draft = all.Where(f => f.Layer.Name.StartsWith("0\\DESIGN DRAFT") && !f.Layer.Name.Contains("GEOLOGY")).ToList();
Out.AppendLine($"file {App.Filename}; DESIGN DRAFT (excl. geology): {draft.Count} figures");
foreach (var g in draft.GroupBy(f => f.Layer.Name).OrderBy(g => g.Key)) Out.AppendLine($"  {g.Key}: " + string.Join(", ", g.GroupBy(f => A(f, "Name") + (f.IsPolyline ? (f.asPolyline.Closed ? " [closed]" : " [open]") : f.IsText || f.IsMText ? " [text]" : " [" + f.GetType().Name + "]")).Select(x => $"{x.Key} x{x.Count()}")));
// current design figures
string[] want = { "4588F43", "4588F6B", "4588F65", "4588F6C", "4588F66", "4588F5B", "4588F49" };
foreach (var h in want) if (!H.ContainsKey(h)) Out.AppendLine($"!! {h} is gone (edited since?)");
var M = P3(H["44BD9C0"]); var d = P3(H["4588F43"]); var CL = M.Take(M.Count - 1).Concat(d).ToList(); double sJ = 0; for (int i = 1; i < M.Count; i++) sJ += D2(M[i], M[i - 1]);
(double s, double off, double dz) Fr(double[] q)
{
    double bs = 0, bd = 1e9, bo = 0, bz = 0, c0 = 0;
    for (int i = 1; i < CL.Count; i++)
    {
        double vx = CL[i][0] - CL[i - 1][0], vy = CL[i][1] - CL[i - 1][1], L = Math.Sqrt(vx * vx + vy * vy), t = Math.Max(0, Math.Min(1, ((q[0] - CL[i - 1][0]) * vx + (q[1] - CL[i - 1][1]) * vy) / (L * L)));
        double dd = Math.Sqrt(Math.Pow(CL[i - 1][0] + t * vx - q[0], 2) + Math.Pow(CL[i - 1][1] + t * vy - q[1], 2));
        if (dd < bd - 1e-9) { bd = dd; bs = c0 + t * L; bo = ((q[0] - CL[i - 1][0]) * vy - (q[1] - CL[i - 1][1]) * vx) / L; bz = CL[i - 1][2] + t * (CL[i][2] - CL[i - 1][2]); }
        c0 += L;
    }
    return (bs - sJ, bo, q[2] - bz);
}
// 1 attributes on the three centrelines
Out.AppendLine("== centreline attributes");
foreach (var h in new[] { "4588F43", "4588F6B", "4588F65" }) { var f = H[h]; Out.AppendLine($"  {h} Name={A(f, "Name")} ACT={A(f, "ACT_TYPE")} MINE={A(f, "MINE")} LEVEL={A(f, "LEVEL")} PROF={A(f, "PROFILE")} GSS={A(f, "GSS")} Desc={A(f, "Description")} LAT_VERT={A(f, "LAT_VERT")} STO_DEV={A(f, "STO_DEV")} ORE={A(f, "ORE_FLAG")}"); }
// 2 any duplicate current-design figures on the same layer + name (old copies left behind)
Out.AppendLine("== duplicates (same layer + Name, design layers)");
foreach (var g in draft.Where(f => f.IsPolyline && !f.Layer.Name.Contains("NOTES") && !f.Layer.Name.Contains("CABLEBOLTS")).GroupBy(f => f.Layer.Name + " | " + A(f, "Name")).Where(g => g.Count() > 1 && !g.Key.Contains("STRIPPING")))
    Out.AppendLine($"  {g.Key}: {g.Count()} ({string.Join(",", g.Select(f => f.Handle))})");
// 3 polyline hygiene: duplicate consecutive vertices, tiny segments, open strips
Out.AppendLine("== polyline hygiene");
foreach (var f in draft.Where(f => f.IsPolyline && !f.Layer.Name.Contains("HAZARDS")))
{
    var p = P3(f); if (p.Count < 2) { Out.AppendLine($"  {f.Handle} [{f.Layer.Name}] {A(f, "Name")}: {p.Count} vertex"); continue; }
    int dup = 0; double mn = 1e9; for (int i = 1; i < p.Count; i++) { var dd = D3(p[i], p[i - 1]); if (dd < 0.01) dup++; mn = Math.Min(mn, dd); }
    if (f.asPolyline.Closed && D3(p[0], p[^1]) < 0.01) dup++;
    if (dup > 0 || mn < 0.05) Out.AppendLine($"  {f.Handle} [{f.Layer.Name}] {A(f, "Name")}: {dup} duplicate vertices, shortest segment {mn:F3} m");
    if (f.Layer.Name.EndsWith("STRIPPING") && !f.asPolyline.Closed) Out.AppendLine($"  {f.Handle} [{f.Layer.Name}] {A(f, "Name")}: OPEN strip string, {p.Count} pts, from ({p[0][0]:F2},{p[0][1]:F2}) ch {Fr(p[0]).s:F1}");
}
// 4 decline walls: offsets, gaps, end face, tie at the joint side to Design Master walls
Out.AppendLine("== decline walls");
var dmw = all.Where(f => f.Layer.Name == "0\\DESIGN MASTER\\WALLS" && f.IsPolyline && Near(f, 80)).ToList();
foreach (var h in new[] { "4588F5B", "4588F49" })
{
    var p = P3(H[h]); var o = p.Select(Fr).ToList(); var side = o.Where(x => Math.Abs(Math.Abs(x.off) - 2.75) < 0.4).ToList();
    double maxSeg = Enumerable.Range(1, p.Count - 1).Max(i => D2(p[i], p[i - 1]));
    Out.AppendLine($"  {h}: {p.Count} pts ch {o.Min(x => x.s):F1}..{o.Max(x => x.s):F1}, |off| {side.Min(x => Math.Abs(x.off)):F2}-{side.Max(x => Math.Abs(x.off)):F2}, floor dz {side.Min(x => x.dz):F3}..{side.Max(x => x.dz):F3}, longest segment {maxSeg:F2}");
    foreach (var e in new[] { p[0], p[^1] })
    {
        var ties = all.Where(f => f.IsPolyline && f.Handle != h && f.Layer.Name.Contains("WALL") && !f.Layer.Name.Contains("CENTERLINES\\")).Select(f => (f, dd: P3(f).Min(q => D3(q, e)))).OrderBy(x => x.dd).First();
        Out.AppendLine($"    end ({e[0]:F2},{e[1]:F2},{e[2]:F2}) ch {Fr(e).s:F2} off {Fr(e).off:F2}: nearest other wall vertex {ties.dd * 1000:F0} mm on {ties.f.Handle} [{ties.f.Layer.Name}] {A(ties.f, "Name")}");
    }
}
var dend = d[^1]; var cad = P3(H["4588F49"]); Out.AppendLine($"  decline end face: CL end ch {Fr(dend).s:F2}; wall pts beyond CL end {cad.Count(q => Fr(q).s > Fr(dend).s + 0.01)}; face width ~{cad.Where(q => Math.Abs(Fr(q).s - Fr(dend).s) < 0.3).Select(q => Fr(q).off).DefaultIfEmpty(0).Max() - cad.Where(q => Math.Abs(Fr(q).s - Fr(dend).s) < 0.3).Select(q => Fr(q).off).DefaultIfEmpty(0).Min():F2}");
// Design Master walls of 44BD9C0 around the joint
foreach (var f in dmw.Where(f => P3(f).Any(q => D2(q, M[^1]) < 8))) { var p = P3(f); Out.AppendLine($"  DM wall {f.Handle} {A(f, "Name")} n={p.Count}: ends ({p[0][0]:F2},{p[0][1]:F2}) ch {Fr(p[0]).s:F2} off {Fr(p[0]).off:F2} / ({p[^1][0]:F2},{p[^1][1]:F2}) ch {Fr(p[^1]).s:F2} off {Fr(p[^1]).off:F2}"); }
// 5 RAD / DSP: angle, mouth, ends vs solids, wall closure
Out.AppendLine("== drives");
var sol = all.Where(f => f.Layer.Name == "0\\SKETCH LAYER\\MPA 07" && f.IsPolyface && A(f, "_CPX") != "").Select(f => new[] { double.Parse(A(f, "_CPX"), System.Globalization.CultureInfo.InvariantCulture), double.Parse(A(f, "_CPY"), System.Globalization.CultureInfo.InvariantCulture), double.Parse(A(f, "_CPZ"), System.Globalization.CultureInfo.InvariantCulture), double.Parse(A(f, "_Azimuth"), System.Globalization.CultureInfo.InvariantCulture) }).ToList();
foreach (var (nm, hc, hw, W, H5) in new[] { ("RAD", "4588F6B", "4588F6C", 5.0, 5.0), ("DSP", "4588F65", "4588F66", 5.5, 5.5) })
{
    var c = P3(H[hc]); var w = P3(H[hw]); double daz = Az(c[0], c[1]);
    var mySol = sol.Where(s => c.Skip(1).Any(q => D2(q, s) < 25) && Math.Abs(Fr(s).off) > 3).ToList();
    double solOff = mySol.Count == 0 ? -1 : mySol.Max(s => { double best = 1e9; for (int i = 1; i < c.Count; i++) { double vx = c[i][0] - c[i - 1][0], vy = c[i][1] - c[i - 1][1], L = Math.Sqrt(vx * vx + vy * vy), t = Math.Max(0, Math.Min(1, ((s[0] - c[i - 1][0]) * vx + (s[1] - c[i - 1][1]) * vy) / (L * L))); best = Math.Min(best, Math.Sqrt(Math.Pow(c[i - 1][0] + t * vx - s[0], 2) + Math.Pow(c[i - 1][1] + t * vy - s[1], 2))); } return best; });
    var far = mySol.OrderByDescending(s => D2(s, c[0])).FirstOrDefault();
    Out.AppendLine($"  {nm}: {c.Count} pts, length {Enumerable.Range(1, c.Count - 1).Sum(i => D2(c[i], c[i - 1])):F2}, legs " + string.Join(" / ", Enumerable.Range(1, c.Count - 1).Select(i => $"{D2(c[i], c[i - 1]):F2} az {Az(c[i - 1], c[i]):F2} " + (Math.Abs(c[i][2] - c[i - 1][2]) < 0.001 ? "flat" : "1:" + (D2(c[i], c[i - 1]) / (c[i][2] - c[i - 1][2])).ToString("F1")))));
    Out.AppendLine($"    MPA 07 {nm} solids: {mySol.Count}, max centre offset from CL {solOff:F2} m; far solid centre to CL end {(far == null ? -1 : D2(far, c[^1])):F2} m, floor {(far == null ? 0 : far[2] - c[^1][2]):F2}");
    Out.AppendLine($"    wall {w.Count} pts open={!H[hw].asPolyline.Closed}; widths across CL: " + string.Join(" ", Enumerable.Range(0, w.Count / 2).Select(i => D2(w[i], w[w.Count - 1 - i]).ToString("F2"))));
    // drop-down vs drive back: the drop-down bottom should land on the drive's back
    var strip = all.Where(f => f.Layer.Name == "0\\DESIGN DRAFT\\STRIPPING" && f.IsPolyline && f.asPolyline.VertexCount == 4 && f.asPolyline.Closed && P3(f).Max(q => q[2]) - P3(f).Min(q => q[2]) > 1.2 && P3(f).Any(q => D2(q, c[0]) < 6)).ToList();
    foreach (var sb in strip)
    {
        var B = P3(sb); var low = B.OrderBy(q => q[2]).Take(2).ToList(); var top = B.OrderByDescending(q => q[2]).Take(2).ToList();
        double along(double[] q) => ((q[0] - c[0][0]) * (c[1][0] - c[0][0]) + (q[1] - c[0][1]) * (c[1][1] - c[0][1])) / D2(c[0], c[1]);
        double floorAt(double a) => c[0][2] + a * (c[1][2] - c[0][2]) / D2(c[0], c[1]);
        Out.AppendLine($"    drop-down {sb.Handle}: top {D2(top[0], top[1]):F2} wide (drive {W} -> expect {W - 0.5}), {along(top[0]):F2}..{along(low[0]):F2} m into the drive; bottom edge {low.Average(q => q[2]) - floorAt(low.Average(along)):F2} m above the drive floor vs drive height {H5} -> lip {low.Average(q => q[2]) - floorAt(low.Average(along)) - H5:+0.00;-0.00} m; top edge {top.Average(q => q[2]) - floorAt(top.Average(along)):F2} above floor");
    }
}
// 6 back strips: shared points, heights above decline floor
Out.AppendLine("== back strips");
var strips = draft.Where(f => f.Layer.Name == "0\\DESIGN DRAFT\\STRIPPING" && f.IsPolyline).ToList();
foreach (var f in strips) { var p = P3(f); var o = p.Select(Fr).ToList(); Out.AppendLine($"  {f.Handle} {A(f, "Name")} ACT={A(f, "ACT_TYPE")} PROF={A(f, "PROFILE")} n={p.Count} closed={f.asPolyline.Closed}: ch {o.Min(x => x.s):F1}..{o.Max(x => x.s):F1}, off {o.Min(x => x.off):F2}..{o.Max(x => x.off):F2}, z above decline floor {o.Min(x => x.dz):F2}..{o.Max(x => x.dz):F2}"); }
// 7 Drafter outputs vs current lines
Out.AppendLine("== Drafter generated layers");
foreach (var f in draft.Where(f => f.Layer.Name.StartsWith("0\\DESIGN DRAFT\\CENTERLINES\\") && (f.Layer.Name.EndsWith("WALLS") || f.Layer.Name.EndsWith("LENGTH") || f.Layer.Name.EndsWith("GRADE") && (f.IsText || f.IsMText))))
{
    string nm = A(f, "Name"); string h = nm == "MAIN DEC" ? "4588F43" : nm == "W6390 RAD" ? "4588F6B" : nm == "W6390 DSP" ? "4588F65" : null; if (h == null) continue; var c = P3(H[h]);
    double len = Enumerable.Range(1, c.Count - 1).Sum(i => D2(c[i], c[i - 1]));
    if (f.IsPolyline) { var p = P3(f); double half = nm == "W6390 RAD" ? 2.5 : 2.75; Out.AppendLine($"  {f.Handle} WALLS {nm}: CL end to nearest wall pt {p.Min(q => D2(q, c[^1])):F2} (expect {half:F2}), CL start {p.Min(q => D2(q, c[0])):F2} -> {(Math.Abs(p.Min(q => D2(q, c[^1])) - half) > 0.05 ? "STALE" : "ok")}"); }
    else Out.AppendLine($"  {f.Handle} {f.Layer.Name.Split('\\').Last()} {nm}: '{(f.IsText ? f.asText.TextString : f.asMText.TextString)}' (actual length {len:F1} m)");
}
// 8 hazards
Out.AppendLine("== hazards (NOTES\\HAZARDS)");
foreach (var f in all.Where(f => f.Layer.Name.StartsWith("0\\DESIGN DRAFT\\NOTES\\HAZARDS")))
{
    if (f.IsPolyline) { var p = P3(f); var o = p.Select(Fr).ToList(); var cl = o.OrderBy(x => Math.Abs(x.off)).First(); Out.AppendLine($"  {f.Handle} ACT={A(f, "ACT_TYPE")} PROF={A(f, "PROFILE")} RL {p.Average(q => q[2]):F2}: closest to decline ch {cl.s:F1} off {cl.off:F2}; to RAD CL {P3(H["4588F6B"]).Min(q => p.Min(r => D2(q, r))):F1} m, to DSP CL {P3(H["4588F65"]).Min(q => p.Min(r => D2(q, r))):F1} m"); }
    else if (f.IsText || f.IsMText) Out.AppendLine($"  label '{(f.IsText ? f.asText.TextString : f.asMText.TextString)}'");
}
// 9 other odd bits on DESIGN DRAFT
Out.AppendLine("== other");
foreach (var f in draft.Where(f => f.Layer.Name.Contains("CABLEBOLTS") || f.Layer.Name == "0\\DESIGN DRAFT\\NOTES").Take(3)) { var m = f.BoundingBox.Midpoint; Out.AppendLine($"  {f.Handle} [{f.Layer.Name}] {A(f, "Name")} {f.GetType().Name} mid ({m.x:F1},{m.y:F1},{m.z:F1}) MPA07={A(f, "CLAUDE_MPA07")}"); }
Out.AppendLine($"  cablebolts: {draft.Count(f => f.Layer.Name.Contains("CABLEBOLTS"))} on {string.Join(",", draft.Where(f => f.Layer.Name.Contains("CABLEBOLTS")).Select(f => A(f, "Name")).Distinct())}, RL {draft.Where(f => f.Layer.Name.Contains("CABLEBOLTS")).Select(f => f.BoundingBox.Midpoint.z).DefaultIfEmpty(0).Min():F1}-{draft.Where(f => f.Layer.Name.Contains("CABLEBOLTS")).Select(f => f.BoundingBox.Midpoint.z).DefaultIfEmpty(0).Max():F1}");
// pillar between RAD and DSP / decline end
var rc = P3(H["4588F6B"]); var dc = P3(H["4588F65"]);
Out.AppendLine($"  RAD end to decline CL: {Math.Abs(Fr(rc[^1]).off):F1} m; DSP end to decline CL {Math.Abs(Fr(dc[^1]).off):F1} m; RAD-DSP closest CL {rc.Min(q => dc.Min(r => D2(q, r))):F1} m");
// Design Master strips near the joint (possible double strip on 44BD9C0)
foreach (var f in all.Where(f => f.Layer.Name.StartsWith("0\\DESIGN MASTER\\STRIPPING") && f.IsPolyline && P3(f).Any(q => D2(q, M[^1]) < 12))) { var p = P3(f); Out.AppendLine($"  DM strip {f.Handle} {A(f, "Name")} n={p.Count} RL {p.Min(q => q[2]):F1}-{p.Max(q => q[2]):F1}, ch {p.Min(q => Fr(q).s):F1}..{p.Max(q => Fr(q).s):F1}"); }
return null;
