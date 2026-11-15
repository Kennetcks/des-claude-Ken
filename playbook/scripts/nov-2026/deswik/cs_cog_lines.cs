// Level Design Drafter: a red COG line across the drive (6 m, Design Master style) with "COG" text along the drive, at
// every change of grade inside one drive of the MPA 07 centrelines (DESIGN DRAFT, CLAUDE_MPA07). Line and text are
// copies of a Design Master COG pair, moved onto the DESIGN DRAFT notes layer. Dry run unless APPLY.
const bool APPLY = false;
const double HALF = 3.0, TXT_OFF = 0.7;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
// notes layer: the active layer if it is a NOTES layer, else 0\DESIGN DRAFT\NOTES
string active = null;
foreach (var src in new object[] { App, App.Layers }) foreach (var pn in new[] { "ActiveLayer", "CurrentLayer", "Current" })
    try { var v = src.GetType().GetProperty(pn)?.GetValue(src); if (v != null && active == null) active = (string)v.GetType().GetProperty("Name")?.GetValue(v); } catch { }
var notesLayers = new List<string>(); object layN = null;
foreach (var l in (System.Collections.IEnumerable)App.Layers) { var n = (string)l.GetType().GetProperty("Name").GetValue(l); if (n.StartsWith("0\\DESIGN DRAFT") && n.Contains("NOTES")) notesLayers.Add(n); }
string target = active != null && active.Contains("NOTES") ? active : "0\\DESIGN DRAFT\\NOTES";
foreach (var l in (System.Collections.IEnumerable)App.Layers) if ((string)l.GetType().GetProperty("Name").GetValue(l) == target) layN = l;
Out.AppendLine($"active layer: {active ?? "(not readable)"}; DESIGN DRAFT notes layers: {string.Join(", ", notesLayers)}; target {target} {(layN != null ? "found" : "MISSING")}");

Deswik.Graphics.Primaries.Figure tLine = null, tText = null; var cls = new List<Deswik.Graphics.Primaries.Figure>(); var existing = new List<(double x, double y)>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null) continue;
    if (f.Layer.Name == "0\\DESIGN MASTER\\NOTES" && f.IsText && f.asText.TextString == "COG" && tText == null)
    {
        var m = f.BoundingBox.Midpoint;
        foreach (Deswik.Graphics.Primaries.Figure g in App.ModelSpace.Entities)
            if (!g.Deleted && g.Layer != null && g.Layer.Name == f.Layer.Name && g.IsPolyline && g.asPolyline.VertexCount == 2 && Math.Abs(g.BoundingBox.Midpoint.x - m.x) < 3 && Math.Abs(g.BoundingBox.Midpoint.y - m.y) < 3) { tLine = g; break; }
        if (tLine != null) tText = f;
    }
    else if (f.Layer.Name == "0\\DESIGN DRAFT\\CENTERLINES" && f.IsPolyline && A(f, "CLAUDE_MPA07") == "1") cls.Add(f);
    else if (f.Layer.Name == target && f.IsText && f.asText.TextString == "COG") { var m = f.BoundingBox.Midpoint; existing.Add((m.x, m.y)); }
}
if (tLine == null || tText == null || layN == null) { Out.AppendLine("style source or notes layer missing - nothing changed"); return null; }

var cogs = new List<(string drive, double x, double y, double z, double tx, double ty, string from, string to)>();
string G(double g) => Math.Abs(g) < 1e-4 ? "flat" : $"1:{1 / g:F1}";
foreach (var f in cls)
{
    var pl = f.asPolyline; var P = Enumerable.Range(0, pl.VertexCount).Select(i => pl.GetVertex(i)).ToList();
    for (int i = 1; i < P.Count - 1; i++)
    {
        double L0 = Math.Sqrt(Math.Pow(P[i].x - P[i - 1].x, 2) + Math.Pow(P[i].y - P[i - 1].y, 2)), L1 = Math.Sqrt(Math.Pow(P[i + 1].x - P[i].x, 2) + Math.Pow(P[i + 1].y - P[i].y, 2));
        double g0 = (P[i].z - P[i - 1].z) / L0, g1 = (P[i + 1].z - P[i].z) / L1;
        if (Math.Abs(g1 - g0) < 0.002) continue;
        double tx = (P[i + 1].x - P[i - 1].x), ty = (P[i + 1].y - P[i - 1].y), tl = Math.Sqrt(tx * tx + ty * ty);
        cogs.Add((A(f, "Name"), P[i].x, P[i].y, P[i].z, tx / tl, ty / tl, G(g0), G(g1)));
    }
}
foreach (var c in cogs) Out.AppendLine($"COG on {c.drive} at ({c.x:F2},{c.y:F2},{c.z:F2}): {c.from} -> {c.to}{(existing.Any(e => Math.Abs(e.x - c.x) < 3 && Math.Abs(e.y - c.y) < 3) ? " (already has a COG - skipped)" : "")}");
cogs = cogs.Where(c => !existing.Any(e => Math.Abs(e.x - c.x) < 3 && Math.Abs(e.y - c.y) < 3)).ToList();
if (!APPLY) { Out.AppendLine($"{cogs.Count} to draw - dry run, nothing changed"); return null; }

var setLayer = typeof(Deswik.Graphics.Primaries.Figure).GetProperty("Layer");
foreach (var c in cogs)
{
    double nx = -c.ty, ny = c.tx;
    var ln = (Deswik.Graphics.Primaries.Figure)tLine.Clone(); var lp = ln.asPolyline; while (lp.VertexCount > 0) lp.RemoveVertexAt(0);
    lp.AddVertexWithoutAutomaticClose(c.x + nx * HALF, c.y + ny * HALF, c.z); lp.AddVertexWithoutAutomaticClose(c.x - nx * HALF, c.y - ny * HALF, c.z); lp.Closed = false;
    setLayer.SetValue(ln, layN); App.ModelSpace.Entities.AddItem(ln, null, null, true, false);
    // text along the drive, just past the line, centred on the drive
    var tx = (Deswik.Graphics.Primaries.Figure)tText.Clone(); var t = tx.asText;
    t.Rotation = Math.Atan2(c.ty, c.tx) * 180 / Math.PI;
    t.InsertionPoint = new Deswik.Graphics.Geometry.Point(c.x, c.y, c.z);
    setLayer.SetValue(tx, layN); App.ModelSpace.Entities.AddItem(tx, null, null, true, false);
    var bm = tx.BoundingBox.Midpoint; double gx = c.x + c.tx * (TXT_OFF + 0.5) + nx * 0 - bm.x, gy = c.y + c.ty * (TXT_OFF + 0.5) - bm.y;
    var ip = t.InsertionPoint; t.InsertionPoint = new Deswik.Graphics.Geometry.Point(ip.x + gx, ip.y + gy, c.z);
}
App.Redraw(true); Out.AppendLine($"applied: {cogs.Count} COG lines and labels on {target}"); return null;
