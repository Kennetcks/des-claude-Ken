// MPA 07 MAIN DEC: check every segment is exactly 1:7 down (plan length / drop); if FIX, reset each vertex RL to
// start RL - plan distance / 7 (plan position unchanged). Also checks the joint with the existing decline.
const bool FIX = false;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
Deswik.Graphics.Primaries.Figure dec = null, ex = null;
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || !f.IsPolyline) continue;
    if (f.Layer.Name == "0\\DESIGN DRAFT\\CENTERLINES" && A(f, "CLAUDE_MPA07") == "1" && A(f, "ACT_TYPE") == "DEC") dec = f;
    else if (f.Layer.Name == "0\\DESIGN MASTER\\CENTRELINES" && A(f, "Name") == "MAIN DEC") { var e = f.asPolyline.GetVertex(f.asPolyline.VertexCount - 1); if (Math.Abs(e.x - 8082.206) < 0.05 && Math.Abs(e.y - 63112.193) < 0.05) ex = f; }
}
var pl = dec.asPolyline; var P = Enumerable.Range(0, pl.VertexCount).Select(i => pl.GetVertex(i)).ToList();
double minR = 1e9, maxR = -1e9, maxErr = 0, s = 0;
for (int i = 1; i < P.Count; i++)
{
    double L = Math.Sqrt(Math.Pow(P[i].x - P[i - 1].x, 2) + Math.Pow(P[i].y - P[i - 1].y, 2)); s += L; double drop = P[i - 1].z - P[i].z; double r = L / drop;
    minR = Math.Min(minR, r); maxR = Math.Max(maxR, r); maxErr = Math.Max(maxErr, Math.Abs(P[i].z - (P[0].z - s / 7.0)));
}
Out.AppendLine($"{P.Count} vertices, {s:F3} m plan; segment grades 1:{minR:F4} to 1:{maxR:F4}; overall 1:{s / (P[0].z - P[^1].z):F4}; largest RL error from exact 1:7 = {maxErr * 1000:F2} mm");
if (ex != null) { var e = ex.asPolyline.GetVertex(ex.asPolyline.VertexCount - 1); Out.AppendLine($"joint with existing MAIN DEC: gap {Math.Sqrt(Math.Pow(e.x - P[0].x, 2) + Math.Pow(e.y - P[0].y, 2) + Math.Pow(e.z - P[0].z, 2)) * 1000:F1} mm; existing decline last segment 1:{Math.Sqrt(Math.Pow(e.x - ex.asPolyline.GetVertex(ex.asPolyline.VertexCount - 2).x, 2) + Math.Pow(e.y - ex.asPolyline.GetVertex(ex.asPolyline.VertexCount - 2).y, 2)) / (ex.asPolyline.GetVertex(ex.asPolyline.VertexCount - 2).z - e.z):F3}"); }
if (!FIX) return null;
var np = new List<double[]> { new[] { P[0].x, P[0].y, P[0].z } }; s = 0;
for (int i = 1; i < P.Count; i++) { s += Math.Sqrt(Math.Pow(P[i].x - P[i - 1].x, 2) + Math.Pow(P[i].y - P[i - 1].y, 2)); np.Add(new[] { P[i].x, P[i].y, P[0].z - s / 7.0 }); }
var c = (Deswik.Graphics.Primaries.Figure)dec.Clone(); var cp = c.asPolyline; while (cp.VertexCount > 0) cp.RemoveVertexAt(0);
foreach (var q in np) cp.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]); cp.Closed = false; App.ModelSpace.Entities.AddItem(c, null, null, true, false); dec.Deleted = true;
App.Redraw(true); Out.AppendLine("RLs reset to exact 1:7"); return null;
