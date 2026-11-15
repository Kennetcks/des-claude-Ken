// MPA 07: move the truck loading strip that sits on the W6390 DSP mouth down the decline with the W6390 DSP (MOVE m).
// Strip panels and their label/leader (by handle), cables and CB labels on 0\DESIGN DRAFT\CABLEBOLTS inside the strip
// area. Each vertex keeps its decline station + MOVE, its offset across the decline and its height above the decline
// centreline, so it follows the decline exactly. Points/text/leaders move by that step at their own position. Dry run unless APPLY.
const bool APPLY = false;
const double MOVE = 5.0;
var H = new HashSet<string> { "458729D", "458730A" };   // strip label and leader; panels by distance from the DSP start (they are re-created on every edit)
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
string Hd(Deswik.Graphics.Primaries.Figure f) => Convert.ToString(f.GetType().GetProperty("Handle")?.GetValue(f));
Deswik.Graphics.Primaries.Figure dec = null; var mv = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
    if (!f.Deleted && f.Layer != null && f.Layer.Name == "0\\DESIGN DRAFT\\CENTERLINES" && f.IsPolyline && A(f, "CLAUDE_MPA07") == "1" && A(f, "ACT_TYPE") == "DEC") dec = f;
var D = Enumerable.Range(0, dec.asPolyline.VertexCount).Select(i => { var q = dec.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
var C = new List<double> { 0 }; for (int i = 1; i < D.Count; i++) C.Add(C[i - 1] + Math.Sqrt(Math.Pow(D[i][0] - D[i - 1][0], 2) + Math.Pow(D[i][1] - D[i - 1][1], 2)));
// station, signed offset (+ right of travel) and height above the centreline
(double s, double off, double h) On(double x, double y, double z)
{
    double bs = 0, bd = 1e9, bo = 0, bz = 0;
    for (int i = 1; i < D.Count; i++)
    {
        double vx = D[i][0] - D[i - 1][0], vy = D[i][1] - D[i - 1][1], L = Math.Sqrt(vx * vx + vy * vy); double t = ((x - D[i - 1][0]) * vx + (y - D[i - 1][1]) * vy) / (L * L);
        double tc = Math.Max(0, Math.Min(1, t)), px = D[i - 1][0] + tc * vx, py = D[i - 1][1] + tc * vy, d = Math.Sqrt(Math.Pow(x - px, 2) + Math.Pow(y - py, 2));
        if (d < bd - 1e-9) { bd = d; bs = C[i - 1] + t * L; bo = ((x - D[i - 1][0]) * vy - (y - D[i - 1][1]) * vx) / L; bz = D[i - 1][2] + tc * (D[i][2] - D[i - 1][2]); }
    }
    return (bs, bo, z - bz);
}
double[] Back(double s, double off, double h)
{
    int i = 1; while (i < D.Count - 1 && C[i] < s) i++;
    double vx = D[i][0] - D[i - 1][0], vy = D[i][1] - D[i - 1][1], L = Math.Sqrt(vx * vx + vy * vy), t = (s - C[i - 1]) / L;
    double px = D[i - 1][0] + t * vx, py = D[i - 1][1] + t * vy, pz = D[i - 1][2] + t * (D[i][2] - D[i - 1][2]);
    return new[] { px + vy / L * off, py - vx / L * off, pz + h };
}
double[] Map(double x, double y, double z) { var o = On(x, y, z); return Back(o.s + MOVE, o.off, o.h); }
// strip area (plan box of the four panels) for the cables
double x0 = 1e9, y0 = 1e9, x1 = -1e9, y1 = -1e9;
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null) continue;
    bool panel = f.Layer.Name == "0\\DESIGN DRAFT\\STRIPPING" && f.IsPolyline && A(f, "CLAUDE_MPA07") == "1" && Enumerable.Range(0, f.asPolyline.VertexCount).Any(i => { var q = f.asPolyline.GetVertex(i); return Math.Sqrt(Math.Pow(q.x - 8110.890, 2) + Math.Pow(q.y - 63093.593, 2)) < 7; });
    if (H.Contains(Hd(f)) || panel) { mv.Add(f); if (f.IsPolyline) { var b = f.BoundingBox; x0 = Math.Min(x0, b.Min.x); y0 = Math.Min(y0, b.Min.y); x1 = Math.Max(x1, b.Max.x); y1 = Math.Max(y1, b.Max.y); } }
}
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || f.Layer.Name != "0\\DESIGN DRAFT\\CABLEBOLTS") continue; var m = f.BoundingBox.Midpoint;
    bool inside = m.x > x0 && m.x < x1 && m.y > y0 && m.y < y1;
    if ((f.IsPoint && inside && m.z > 6397) || ((f.IsMText || f.IsText) && Math.Sqrt(Math.Pow(m.x - (x0 + x1) / 2, 2) + Math.Pow(m.y - (y0 + y1) / 2, 2)) < 10)) mv.Add(f);
}
Out.AppendLine($"strip area ({x0:F1},{y0:F1})-({x1:F1},{y1:F1}); to move: {mv.Count}");
foreach (var f in mv)
{
    var m = f.BoundingBox.Midpoint; var o = On(m.x, m.y, m.z); var n = Map(m.x, m.y, m.z);
    string what = f.IsPolyline ? $"strip panel {f.asPolyline.VertexCount} pts" : f.IsMText ? "label '" + f.asMText.TextString.Replace("\r\n", " ").Trim() + "'" : f.IsText ? "text '" + f.asText.TextString + "'" : f.IsPoint ? "cable" : "leader/other";
    Out.AppendLine($"  {Hd(f)} {f.Layer.Name.Replace("0\\DESIGN DRAFT\\", "")} {what}: station {o.s:F2} -> {o.s + MOVE:F2}, moves ({n[0] - m.x:F2},{n[1] - m.y:F2},{n[2] - m.z:F2})");
}
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }
foreach (var f in mv)
{
    if (f.IsPolyline)
    {
        var c = (Deswik.Graphics.Primaries.Figure)f.Clone(); var pl = c.asPolyline; bool cl = f.asPolyline.Closed;
        var np = Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return Map(q.x, q.y, q.z); }).ToList();
        while (pl.VertexCount > 0) pl.RemoveVertexAt(0); foreach (var q in np) pl.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]); pl.Closed = cl;
        App.ModelSpace.Entities.AddItem(c, null, null, true, false); f.Deleted = true;
    }
    else { var m = f.BoundingBox.Midpoint; var n = Map(m.x, m.y, m.z); f.Translate(n[0] - m.x, n[1] - m.y, n[2] - m.z); }
}
App.Redraw(true); Out.AppendLine("applied"); return null;
