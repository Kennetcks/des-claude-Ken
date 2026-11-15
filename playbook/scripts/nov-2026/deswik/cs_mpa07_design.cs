// MPA 07 decline extension in the Level Design Drafter, from the cut solids on 0\SKETCH LAYER\MPA 07.
// - MAIN DEC continues from the end of the existing decline (tangent, no gap) at 1:7 down, following the cut azimuths
//   (azimuth interpolated along the solids' stations, so the curve is smooth and passes the cut centres)
// - G6390 EWD (north, to the EWR) and G6390 RAD (north-east) leave from the decline centreline through their cut centres
// - 2.5 m loader fillets (strip triangles) at both corners of each take-off, CB 02 cables at each intersection on the
//   backs, with a CB 02 label
// Everything goes on 0\DESIGN DRAFT\..., is tagged CLAUDE_MPA07=1 and copies the attributes/style of existing strings.
const bool APPLY = false;
const string SK = "0\\SKETCH LAYER\\MPA 07", DMC = "0\\DESIGN MASTER\\CENTRELINES", DMS = "0\\DESIGN MASTER\\STRIPPING", DMB = "0\\DESIGN MASTER\\CABLEBOLTS";
const string DC = "0\\DESIGN DRAFT\\CENTERLINES", DS = "0\\DESIGN DRAFT\\STRIPPING", DB = "0\\DESIGN DRAFT\\CABLEBOLTS";
const double FIL = 3.0, HD = 5.7;
const bool ONLY_FILLETS = false;   // true: replace just the tagged fillet strips (e.g. after a fillet size change)
var inv = System.Globalization.CultureInfo.InvariantCulture;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
double D(string s) => double.Parse(s, inv);
List<Deswik.Graphics.Geometry.Point> Pts(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => f.asPolyline.GetVertex(i)).ToList();

// ---- inputs
var cuts = new SortedDictionary<int, (double x, double y, double z, double az, double gr)>(); int k = 0;
Deswik.Graphics.Primaries.Figure decSrc = null, subSrc = null, redSrc = null, cbPt = null, cbLab = null;
var cbSrc = new List<Deswik.Graphics.Geometry.Point>();
object layC = null, layS = null, layB = null; var layProp = typeof(Deswik.Graphics.Primaries.Figure).GetProperty("Layer");
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null) continue; string ln = f.Layer.Name;
    if (ln == SK) { k++; cuts[k] = (D(A(f, "_CPX")), D(A(f, "_CPY")), D(A(f, "_CPZ")), D(A(f, "_Azimuth")), D(A(f, "_Gradient"))); }
    else if (ln == DMC && f.IsPolyline)
    {
        var p = Pts(f); var e = p[^1];
        if (A(f, "Name") == "MAIN DEC" && Math.Abs(e.x - 8082.206) < 0.05 && Math.Abs(e.y - 63112.193) < 0.05) decSrc = f;
        if (A(f, "Name") == "G6390 SUB") subSrc = f;
    }
    else if (ln == DMS && f.IsPolyline && redSrc == null && f.asPolyline.HatchProperties != null && f.asPolyline.HatchProperties.FillBackColor.ColorIndex != 253) redSrc = f;
    else if (ln == DMB && f.IsPoint) { var ip = f.asPoint.InsertionPoint; if (ip.x > 8114 && ip.x < 8126 && ip.y > 62950 && ip.y < 62964) { cbSrc.Add(ip); cbPt = f; } }
    else if (ln == DMB && f.IsMText && cbLab == null) cbLab = f;
}
foreach (var l in (System.Collections.IEnumerable)App.Layers)
{
    string lnm = (string)l.GetType().GetProperty("Name").GetValue(l);
    if (lnm == DC) layC = l; else if (lnm == DS) layS = l; else if (lnm == DB) layB = l;
}
cbSrc = cbSrc.GroupBy(q => $"{q.x:F2},{q.y:F2}").Select(g => g.First()).ToList();
Out.AppendLine($"cuts {cuts.Count}, decline src {decSrc != null}, SUB src {subSrc != null}, red strip src {redSrc != null}, CB 02 points {cbSrc.Count}, CB label {cbLab != null}, layers {layC != null}/{layS != null}/{layB != null}");
if (cuts.Count != 29 || decSrc == null || subSrc == null || redSrc == null || cbSrc.Count != 9 || cbLab == null || layC == null || layS == null || layB == null) { Out.AppendLine("missing input - nothing changed"); return null; }

// ---- decline: integrate azimuth along the cut stations from the existing end, tangent to its last segment
var dp = Pts(decSrc); var E = dp[^1]; var Ep = dp[^2];
double az0 = Math.Atan2(E.x - Ep.x, E.y - Ep.y) * 180 / Math.PI;
var decCuts = new[] { 6, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 28, 29 };
var st = new List<(double s, double az)> { (0, az0) }; double sAcc = 0; double px = E.x, py = E.y;
foreach (var c in decCuts) { var q = cuts[c]; sAcc += Math.Sqrt(Math.Pow(q.x - px, 2) + Math.Pow(q.y - py, 2)); px = q.x; py = q.y; st.Add((sAcc, q.az)); }
double sEnd = sAcc + 1.73;   // to the end of the last cut
st.Add((sEnd, cuts[29].az));
double AzAt(double s) { for (int i = 1; i < st.Count; i++) if (s <= st[i].s) { double t = (s - st[i - 1].s) / (st[i].s - st[i - 1].s); double a0 = st[i - 1].az, a1 = st[i].az; return a0 + t * (a1 - a0); } return st[^1].az; }
var dec = new List<double[]> { new[] { E.x, E.y, E.z } }; double x = E.x, y = E.y, s = 0, lastAz = az0, h = 0.25;
var dense = new List<(double s, double x, double y)> { (0, x, y) };
while (s < sEnd - 1e-6)
{
    double ds = Math.Min(h, sEnd - s); double a = AzAt(s + ds / 2) * Math.PI / 180;
    x += ds * Math.Sin(a); y += ds * Math.Cos(a); s += ds; dense.Add((s, x, y));
    double azNow = AzAt(s);
    if (Math.Abs(azNow - lastAz) >= 2.0 || s >= sEnd - 1e-6 || s - (dec.Count > 0 ? 0 : 0) >= 0 && Math.Sqrt(Math.Pow(x - dec[^1][0], 2) + Math.Pow(y - dec[^1][1], 2)) >= 6) { dec.Add(new[] { x, y, E.z - s / 7.0 }); lastAz = azNow; }
}
double devMax = decCuts.Max(c => dense.Min(p => Math.Sqrt(Math.Pow(p.x - cuts[c].x, 2) + Math.Pow(p.y - cuts[c].y, 2))));
double zEnd = E.z - sEnd / 7.0;
Out.AppendLine($"MAIN DEC extension: {sEnd:F1} m at 1:7 from RL {E.z:F2} to {zEnd:F2}, {dec.Count} vertices, start tangent {az0:F1} deg, max offset from cut centres {devMax:F2} m");

// decline helpers (floor z, tangent) on the new string
(double s, double x, double y, double d) OnDec(double qx, double qy)
{
    (double s, double x, double y, double d) best = (0, 0, 0, 1e9);
    foreach (var p in dense) { double dd = Math.Sqrt(Math.Pow(p.x - qx, 2) + Math.Pow(p.y - qy, 2)); if (dd < best.d) best = (p.s, p.x, p.y, dd); }
    return best;
}
(double tx, double ty) DecTan(double ss) { double a = AzAt(ss) * Math.PI / 180; return (Math.Sin(a), Math.Cos(a)); }

// ---- branch: from the decline centreline back along the first cut's azimuth, through the cut centres
(List<double[]> pts, double sJ, double jx, double jy) Branch(int[] ids, double endExt)
{
    var c0 = cuts[ids[0]]; double a = (c0.az + 180) * Math.PI / 180; double bx = Math.Sin(a), by = Math.Cos(a);
    double best = 1e9, jx = 0, jy = 0, sJ = 0;
    for (double t = 0; t < 12; t += 0.02) { double qx = c0.x + t * bx, qy = c0.y + t * by; var o = OnDec(qx, qy); if (o.d < best) { best = o.d; jx = qx; jy = qy; sJ = o.s; } }
    var r = new List<double[]> { new[] { jx, jy, E.z - sJ / 7.0 } };
    foreach (var id in ids) r.Add(new[] { cuts[id].x, cuts[id].y, cuts[id].z });
    var cl = cuts[ids[^1]]; double ae = cl.az * Math.PI / 180;
    r.Add(new[] { cl.x + endExt * Math.Sin(ae), cl.y + endExt * Math.Cos(ae), cl.z - endExt * Math.Tan(cl.gr * Math.PI / 180) });
    return (r, sJ, jx, jy);
}
var ewd = Branch(new[] { 7, 8, 9, 10, 11 }, 2.4);
var rad = Branch(new[] { 22, 23, 24, 25, 26, 27 }, 2.15);
double Len(List<double[]> p) => Enumerable.Range(1, p.Count - 1).Sum(i => Math.Sqrt(Math.Pow(p[i][0] - p[i - 1][0], 2) + Math.Pow(p[i][1] - p[i - 1][1], 2)));
Out.AppendLine($"G6390 EWD: {Len(ewd.pts):F1} m from decline station {ewd.sJ:F1} m (RL {ewd.pts[0][2]:F2} to {ewd.pts[^1][2]:F2})");
Out.AppendLine($"G6390 RAD: {Len(rad.pts):F1} m from decline station {rad.sJ:F1} m (RL {rad.pts[0][2]:F2} to {rad.pts[^1][2]:F2})");

// ---- fillets: triangles at the two corners where the branch walls meet the decline wall
var strips = new List<(List<double[]> p, string name)>();
void Fillets((List<double[]> pts, double sJ, double jx, double jy) br, double wb, string name)
{
    var (tx, ty) = DecTan(br.sJ); double bx = br.pts[1][0] - br.pts[0][0], by = br.pts[1][1] - br.pts[0][1], bl = Math.Sqrt(bx * bx + by * by); bx /= bl; by /= bl;
    double side = Math.Sign(tx * by - ty * bx);                       // +1 branch on the left of the decline
    double nx = -ty * side, ny = tx * side;                             // decline wall normal towards the branch
    double wx = br.jx + nx * HD * 0 + nx * 5.5 / 2, wy = br.jy + ny * 5.5 / 2; // point on the decline wall (5.5 m drive)
    double zb = E.z - br.sJ / 7.0 + HD;
    foreach (var sg in new[] { -1.0, 1.0 })
    {
        double ox = br.jx - by * sg * wb / 2, oy = br.jy + bx * sg * wb / 2;    // branch wall line: o + t*b
        // intersection of the branch wall with the decline wall (w + u*t_dec)
        double det = bx * (-ty) - by * (-tx); double t = ((wx - ox) * (-ty) - (wy - oy) * (-tx)) / det;
        double cx = ox + t * bx, cy = oy + t * by;
        double dirAlong = Math.Sign((cx - br.jx) * tx + (cy - br.jy) * ty); if (dirAlong == 0) dirAlong = sg;
        strips.Add((new List<double[]> { new[] { cx, cy, zb }, new[] { cx + dirAlong * FIL * tx, cy + dirAlong * FIL * ty, zb }, new[] { cx + FIL * bx, cy + FIL * by, zb } }, name));
    }
}
Fillets(ewd, 4.5, "G6390 EWD"); Fillets(rad, 5.0, "G6390 RAD");

// ---- CB 02 cables: copy the G6410 MLA / ISP pattern into each intersection frame, on the backs
double[] sJ0 = { 8121.386, 62956.137 }; double[] sm = { -0.76362, -0.64586 }, sb = { 0.64622, -0.76320 };
var cables = new List<double[]>(); var cbLabels = new List<double[]>();
void Cables((List<double[]> pts, double sJ, double jx, double jy) br, double mc)
{
    var (tx, ty) = DecTan(br.sJ); double bx = br.pts[1][0] - br.pts[0][0], by = br.pts[1][1] - br.pts[0][1], bl = Math.Sqrt(bx * bx + by * by); bx /= bl; by /= bl;
    if (tx * by - ty * bx < 0) { tx = -tx; ty = -ty; }                 // keep the source's handedness (branch on the left)
    foreach (var q in cbSrc)
    {
        double a = (q.x - sJ0[0]) * sm[0] + (q.y - sJ0[1]) * sm[1], c = (q.x - sJ0[0]) * sb[0] + (q.y - sJ0[1]) * sb[1];
        double qx = br.jx + a * tx + c * bx, qy = br.jy + a * ty + c * by;
        var o = OnDec(qx, qy); double zDec = E.z - o.s / 7.0 + HD;
        double zBr = br.pts[0][2] + mc + Math.Max(0, c) * 0;           // branch backs near the take-off
        cables.Add(new[] { qx, qy, c > 3.5 ? zBr : zDec });
    }
    cbLabels.Add(new[] { br.jx - bx * 4.5, br.jy - by * 4.5, E.z - br.sJ / 7.0 + HD });
}
Cables(ewd, 4.5); Cables(rad, 5.0);
Out.AppendLine($"fillets {strips.Count} x {FIL} m, CB 02 cables {cables.Count}, labels {cbLabels.Count}");
foreach (var st2 in strips) Out.AppendLine($"  fillet {st2.name}: " + string.Join(" | ", st2.p.Select(q => $"{q[0]:F1},{q[1]:F1}")));
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }

// ---- draw
Deswik.Graphics.Primaries.Figure Poly(Deswik.Graphics.Primaries.Figure src, List<double[]> p, object lay, bool closed)
{
    var c = (Deswik.Graphics.Primaries.Figure)src.Clone(); var pl = c.asPolyline;
    while (pl.VertexCount > 0) pl.RemoveVertexAt(0);
    foreach (var q in p) pl.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]);
    pl.Closed = closed; layProp.SetValue(c, lay); c.AttributeValueSet("CLAUDE_MPA07", "1"); App.ModelSpace.Entities.AddItem(c, null, null, true, false); return c;
}
if (ONLY_FILLETS)
{
    int gone = 0;
    foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
        if (!f.Deleted && f.Layer != null && f.Layer.Name == DS && f.IsPolyline && f.asPolyline.VertexCount == 3 && A(f, "CLAUDE_MPA07") == "1") { f.Deleted = true; gone++; }
    foreach (var (p, name) in strips) { var f = Poly(redSrc, p, layS, true); f.AttributeValueSet("Name", name); }
    App.Redraw(true); Out.AppendLine($"replaced {gone} fillets with {strips.Count} x {FIL} m"); return null;
}
var fd = Poly(decSrc, dec, layC, false); fd.AttributeValueSet("MPA", "007"); fd.AttributeValueSet("Design Status", "Draft");
var fe = Poly(subSrc, ewd.pts, layC, false);
foreach (var kv in new Dictionary<string, string> { { "Name", "G6390 EWD" }, { "ACT_TYPE", "EWD" }, { "Description", "Escapeway Drive" }, { "DIRECTION", "N" }, { "PROFILE", "4.5mW x 4.5mH SA" }, { "GSS", "R 4.0" }, { "MPA", "007" }, { "Design Status", "Draft" } }) fe.AttributeValueSet(kv.Key, kv.Value);
var fr = Poly(subSrc, rad.pts, layC, false);
foreach (var kv in new Dictionary<string, string> { { "Name", "G6390 RAD" }, { "ACT_TYPE", "RAD" }, { "Description", "Return Air Drive" }, { "DIRECTION", "NE" }, { "PROFILE", "5.0mW x 5.0mH SA" }, { "GSS", "R 3.0" }, { "MPA", "007" }, { "Design Status", "Draft" } }) fr.AttributeValueSet(kv.Key, kv.Value);
foreach (var (p, name) in strips) { var f = Poly(redSrc, p, layS, true); f.AttributeValueSet("Name", name); }
foreach (var q in cables)
{
    var c = (Deswik.Graphics.Primaries.Figure)cbPt.Clone(); c.asPoint.InsertionPoint = new Deswik.Graphics.Geometry.Point(q[0], q[1], q[2]);
    layProp.SetValue(c, layB); c.AttributeValueSet("CLAUDE_MPA07", "1"); App.ModelSpace.Entities.AddItem(c, null, null, true, false);
}
foreach (var q in cbLabels)
{
    var c = (Deswik.Graphics.Primaries.Figure)cbLab.Clone(); var mt = c.asMText; mt.TextString = "CB 02"; mt.InsertionPoint = new Deswik.Graphics.Geometry.Point(q[0], q[1], q[2]);
    layProp.SetValue(c, layB); c.AttributeValueSet("CLAUDE_MPA07", "1"); App.ModelSpace.Entities.AddItem(c, null, null, true, false);
}
App.Redraw(true);
Out.AppendLine("drawn: 3 centrelines, " + strips.Count + " fillets, " + cables.Count + " cables, " + cbLabels.Count + " labels on DESIGN DRAFT");
return null;
