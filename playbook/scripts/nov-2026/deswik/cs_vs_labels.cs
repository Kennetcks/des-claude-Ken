// Ventilation Plan Master: label every vent station (closed section polylines on 0\PRIMARY STATIONS) at the side of the
// drive instead of in the middle. For each station: the two wall ends of the section (farthest apart in plan), a short
// tick out from one wall end along the section line, and the "1. STATION ID" text past it, in plan, reading along the tick.
// Output goes to 0\PRIMARY STATIONS\LABELS; that layer is cleared and rebuilt on every run, so re-run after adding or
// moving stations. SIDE picks the wall: +1 = right of the station's facing direction (its extrusion vector), -1 = left.
// Dry run unless APPLY.
const bool APPLY = false;
const int SIDE = 1;
const double TICK = 1.5, GAP = 0.4, HEIGHT = 1.5;
const string SRC = "0\\PRIMARY STATIONS", DST = "0\\PRIMARY STATIONS\\LABELS", FIELD = "1. STATION ID";
var stations = new List<Deswik.Graphics.Primaries.Figure>(); var old = new List<Deswik.Graphics.Primaries.Figure>(); Deswik.Graphics.Primaries.Figure tmpl = null;
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null) continue;
    if (f.Layer.Name == SRC && f.IsPolyline && f.asPolyline.Closed) stations.Add(f);
    if (f.Layer.Name == DST) old.Add(f);
    if (f.Layer.Name == SRC + "\\POINTANNOTATION" && f.IsText) tmpl = f;
}
if (tmpl == null) { Out.AppendLine("no POINTANNOTATION text to copy the style from - nothing changed"); return null; }
var jobs = new List<(string id, double[] a, double[] b, double ang, bool flip)>();
foreach (var st in stations)
{
    var pl = st.asPolyline; var p = Enumerable.Range(0, pl.VertexCount).Select(i => { var q = pl.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
    // wall ends: the pair of floor-level vertices farthest apart in plan (floor = lowest 25 % of the section height)
    double zmin = p.Min(q => q[2]), zmax = p.Max(q => q[2]); var fl = p.Where(q => q[2] <= zmin + 0.25 * (zmax - zmin)).ToList();
    double[] e1 = null, e2 = null; double best = -1;
    foreach (var u in fl) foreach (var v in fl) { double d = Math.Pow(u[0] - v[0], 2) + Math.Pow(u[1] - v[1], 2); if (d > best) { best = d; e1 = u; e2 = v; } }
    var ev = pl.ExtrusionVector; double fx = ev.x, fy = ev.y, rx = fy, ry = -fx;   // right of the facing direction
    var end = ((e1[0] - e2[0]) * rx + (e1[1] - e2[1]) * ry) * SIDE > 0 ? e1 : e2; var oth = end == e1 ? e2 : e1;
    double ux = end[0] - oth[0], uy = end[1] - oth[1], ul = Math.Sqrt(ux * ux + uy * uy); ux /= ul; uy /= ul;
    var a = new[] { end[0], end[1], zmin }; var b = new[] { end[0] + ux * TICK, end[1] + uy * TICK, zmin };
    double ang = Math.Atan2(uy, ux) * 180 / Math.PI; bool flip = ang > 90 || ang < -90;   // keep the text upright
    var id = (st.AttributeValueGet(FIELD, "") ?? "").ToString();
    jobs.Add((id, a, b, ang, flip));
    Out.AppendLine($"{st.Handle} '{id}': section {Math.Sqrt(best):F2} m wide, tick ({a[0]:F2},{a[1]:F2}) -> ({b[0]:F2},{b[1]:F2}) at RL {zmin:F2}, text bearing {ang:F0} deg{(flip ? " (flipped upright)" : "")}");
}
Out.AppendLine($"{stations.Count} stations; {old.Count} old labels on {DST} to clear");
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }
var layer = App.Layers.FirstOrDefault(l => l.Name.Equals(DST, StringComparison.OrdinalIgnoreCase)) ?? App.Layers.Add(DST, false, 1);
foreach (var f in old) f.Deleted = true;
Deswik.Graphics.Primaries.Figure tickTmpl = stations[0];
foreach (var j in jobs)
{
    var ln = (Deswik.Graphics.Primaries.Figure)tickTmpl.Clone(); var lp = ln.asPolyline; while (lp.VertexCount > 0) lp.RemoveVertexAt(0);
    lp.AddVertexWithoutAutomaticClose(j.a[0], j.a[1], j.a[2]); lp.AddVertexWithoutAutomaticClose(j.b[0], j.b[1], j.b[2]); lp.Closed = false; ln.Layer = layer;
    App.ModelSpace.Entities.AddItem(ln, null, null, true, false);
    var t = (Deswik.Graphics.Primaries.Figure)tmpl.Clone(); var tx = t.asText;
    tx.TextString = j.id; tx.Height = HEIGHT; tx.AlignToView = false;
    var evp = tx.GetType().GetProperty("ExtrusionVector"); var up = Activator.CreateInstance(evp.PropertyType, 0.0, 0.0, 1.0); evp.SetValue(tx, up);
    tx.Rotation = -(j.flip ? j.ang + 180 : j.ang);   // Deswik text rotation runs clockwise
    var hj = tx.GetType().GetProperty("HorizontalJustification"); hj.SetValue(tx, Enum.Parse(hj.PropertyType, j.flip ? "Right" : "Left"));
    var ip = tx.InsertionPoint; double dx = j.b[0] - j.a[0], dy = j.b[1] - j.a[1], dl = Math.Sqrt(dx * dx + dy * dy);
    ip.x = j.b[0] + dx / dl * GAP; ip.y = j.b[1] + dy / dl * GAP; ip.z = j.b[2]; tx.InsertionPoint = ip; tx.AlignmentPoint = ip;
    t.Layer = layer; App.ModelSpace.Entities.AddItem(t, null, null, true, false);
}
App.Redraw(true); Out.AppendLine($"applied - {jobs.Count} labels on {DST}"); return null;
