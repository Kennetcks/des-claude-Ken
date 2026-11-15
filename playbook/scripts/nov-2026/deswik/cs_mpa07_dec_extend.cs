// MPA 07: extend the MAIN DEC centreline (DESIGN DRAFT, CLAUDE_MPA07) through the solids added on 0\SKETCH LAYER\MPA 07
// past its current end, to the end of the last solid. Tangent at the joint, azimuth interpolated along the cut centres
// (_CPX/_CPY/_Azimuth), exactly 1:7 down, a vertex every ~0.5 m like the rest of the decline. The decline wall that runs
// from the W6390 EWD mouth round the end face is redrawn to the new end; the other walls are unchanged.
const bool APPLY = false;
const string SK = "0\\SKETCH LAYER\\MPA 07", CL = "0\\DESIGN DRAFT\\CENTERLINES", WL = "0\\DESIGN DRAFT\\WALLS";
const double STEP = 0.5, HALFCUT = 1.73;
var inv = System.Globalization.CultureInfo.InvariantCulture;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
List<double[]> P3(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();

Deswik.Graphics.Primaries.Figure dec = null, endWall = null; var cuts = new List<(double x, double y, double z, double az)>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null) continue; var ln = f.Layer.Name;
    if (ln == CL && f.IsPolyline && A(f, "CLAUDE_MPA07") == "1" && A(f, "ACT_TYPE") == "DEC") dec = f;
    else if (ln == WL && f.IsPolyline && A(f, "CLAUDE_MPA07") == "1" && A(f, "Name") == "MAIN DEC" && f.asPolyline.VertexCount > 25) endWall = f;
    else if (ln == SK && f.IsPolyface && A(f, "_CPX") != "") cuts.Add((double.Parse(A(f, "_CPX"), inv), double.Parse(A(f, "_CPY"), inv), double.Parse(A(f, "_CPZ"), inv), double.Parse(A(f, "_Azimuth"), inv)));
}
if (dec == null || endWall == null) { Out.AppendLine($"decline {dec != null}, end wall {endWall != null} - nothing changed"); return null; }
var d = P3(dec); var E = d[^1]; var Ep = d[^2];
double az0 = Math.Atan2(E[0] - Ep[0], E[1] - Ep[1]) * 180 / Math.PI, tx = Math.Sin(az0 * Math.PI / 180), ty = Math.Cos(az0 * Math.PI / 180);
// cuts past the end of the decline (centre ahead of the end, close to the decline line), in order along it
var ahead = cuts.Where(c => (c.x - E[0]) * tx + (c.y - E[1]) * ty > 0.5).ToList();
var seq = new List<(double x, double y, double z, double az)>(); double px = E[0], py = E[1];
while (ahead.Count > 0) { var n = ahead.OrderBy(c => Math.Pow(c.x - px, 2) + Math.Pow(c.y - py, 2)).First(); if (Math.Sqrt(Math.Pow(n.x - px, 2) + Math.Pow(n.y - py, 2)) > 6) break; seq.Add(n); ahead.Remove(n); px = n.x; py = n.y; }
Out.AppendLine($"decline end ({E[0]:F2},{E[1]:F2},{E[2]:F3}) az {az0:F1}; {cuts.Count} solids with cut centres, {seq.Count} ahead of the end, {ahead.Count} left over");
if (seq.Count == 0) { Out.AppendLine("no solids past the decline end - nothing changed"); return null; }

// azimuth stations, unwrapped so interpolation never jumps across 0/360
var st = new List<(double s, double az)> { (0, az0) }; double sAcc = 0; px = E[0]; py = E[1];
foreach (var c in seq) { sAcc += Math.Sqrt(Math.Pow(c.x - px, 2) + Math.Pow(c.y - py, 2)); px = c.x; py = c.y; double a = c.az; while (a - st[^1].az > 180) a -= 360; while (a - st[^1].az < -180) a += 360; st.Add((sAcc, a)); }
double sEnd = sAcc + HALFCUT; st.Add((sEnd, st[^1].az));
double AzAt(double s) { for (int i = 1; i < st.Count; i++) if (s <= st[i].s) { double t = (s - st[i - 1].s) / (st[i].s - st[i - 1].s); return st[i - 1].az + t * (st[i].az - st[i - 1].az); } return st[^1].az; }
var ext = new List<double[]>(); double x = E[0], y = E[1], s = 0; var dense = new List<(double x, double y)>();
int nStep = (int)Math.Ceiling(sEnd / STEP); double h = sEnd / nStep;
for (int i = 0; i < nStep; i++)
{
    for (int k = 0; k < 10; k++) { double a = AzAt(s + h / 20) * Math.PI / 180; x += h / 10 * Math.Sin(a); y += h / 10 * Math.Cos(a); s += h / 10; dense.Add((x, y)); }
    ext.Add(new[] { x, y, E[2] - s / 7.0 });
}
double dev = seq.Max(c => dense.Min(p => Math.Sqrt(Math.Pow(p.x - c.x, 2) + Math.Pow(p.y - c.y, 2))));
double zdev = seq.Max(c => Math.Abs(c.z - (E[2] - dense.Select((p, i) => (p, i)).OrderBy(q => Math.Pow(q.p.x - c.x, 2) + Math.Pow(q.p.y - c.y, 2)).First().i * h / 10 / 7.0 - h / 10 / 7.0)));
var all = new List<double[]>(d); all.AddRange(ext);
double len = 0; for (int i = 1; i < all.Count; i++) len += Math.Sqrt(Math.Pow(all[i][0] - all[i - 1][0], 2) + Math.Pow(all[i][1] - all[i - 1][1], 2));
Out.AppendLine($"extension {sEnd:F1} m at 1:7, RL {E[2]:F2} -> {ext[^1][2]:F2}, end ({ext[^1][0]:F2},{ext[^1][1]:F2}), {ext.Count} new vertices; max offset from cut centres {dev:F2} m, max RL difference from cut centres {zdev:F2} m");
Out.AppendLine($"MAIN DEC (MPA 07) becomes {len:F1} m, {all.Count} vertices, overall grade 1:{len / (all[0][2] - all[^1][2]):F2}");

// wall: Design Master style open outline of the whole decline, kept from the EWD mouth point round the new end face
var Wd = 5.5; { var m = System.Text.RegularExpressions.Regex.Match(A(dec, "PROFILE"), @"([\d.]+)\s*mW"); if (m.Success) Wd = double.Parse(m.Groups[1].Value, inv); }
List<double[]> Outline(List<double[]> c, double w)
{
    var L = new List<double[]>(); var R = new List<double[]>();
    for (int i = 0; i < c.Count; i++)
    {
        double nx = 0, ny = 0;
        if (i > 0) { double dx = c[i][0] - c[i - 1][0], dy = c[i][1] - c[i - 1][1], l = Math.Sqrt(dx * dx + dy * dy); nx += -dy / l; ny += dx / l; }
        if (i < c.Count - 1) { double dx = c[i + 1][0] - c[i][0], dy = c[i + 1][1] - c[i][1], l = Math.Sqrt(dx * dx + dy * dy); nx += -dy / l; ny += dx / l; }
        double nl = Math.Sqrt(nx * nx + ny * ny); nx /= nl; ny /= nl;
        double k = 1.0; if (i > 0 && i < c.Count - 1) { double dx = c[i + 1][0] - c[i][0], dy = c[i + 1][1] - c[i][1], l = Math.Sqrt(dx * dx + dy * dy); k = 1 / Math.Max(0.5, (-dy / l) * nx + (dx / l) * ny); }
        L.Add(new[] { c[i][0] + nx * w / 2 * k, c[i][1] + ny * w / 2 * k, c[i][2] }); R.Add(new[] { c[i][0] - nx * w / 2 * k, c[i][1] - ny * w / 2 * k, c[i][2] });
    }
    R.Reverse(); L.AddRange(R); return L;
}
var ow = P3(endWall); var dw = Outline(all, Wd);
// the old wall starts at the EWD mouth: find that point on the new outline (left side, before the end face)
int half = all.Count; int iStart = 0; double bd = 1e9;
for (int i = 0; i < half - 1; i++)
{
    double ax = dw[i][0], ay = dw[i][1], vx = dw[i + 1][0] - ax, vy = dw[i + 1][1] - ay, L2 = vx * vx + vy * vy; double t = L2 > 0 ? Math.Max(0, Math.Min(1, ((ow[0][0] - ax) * vx + (ow[0][1] - ay) * vy) / L2)) : 0;
    double dd = Math.Sqrt(Math.Pow(ax + t * vx - ow[0][0], 2) + Math.Pow(ay + t * vy - ow[0][1], 2)); if (dd < bd) { bd = dd; iStart = i; }
}
var nw = new List<double[]> { ow[0] }; for (int i = iStart + 1; i < dw.Count; i++) nw.Add(dw[i]);
double endGap = Math.Sqrt(Math.Pow(nw[^1][0] - ow[^1][0], 2) + Math.Pow(nw[^1][1] - ow[^1][1], 2));
Out.AppendLine($"end wall: starts at the EWD mouth point (outline {bd:F2} m from it), {ow.Count} -> {nw.Count} vertices, finishes {endGap:F2} m from the old finish at the decline start");
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }

Deswik.Graphics.Primaries.Figure Replace(Deswik.Graphics.Primaries.Figure src, List<double[]> p, bool closed)
{
    var c = (Deswik.Graphics.Primaries.Figure)src.Clone(); var pl = c.asPolyline; while (pl.VertexCount > 0) pl.RemoveVertexAt(0);
    foreach (var q in p) pl.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]); pl.Closed = closed;
    App.ModelSpace.Entities.AddItem(c, null, null, true, false); src.Deleted = true; return c;
}
Replace(dec, all, false); Replace(endWall, nw, false);
App.Redraw(true); Out.AppendLine("applied: decline centreline extended and end wall redrawn"); return null;
