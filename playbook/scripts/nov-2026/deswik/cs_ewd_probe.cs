// Read-only: every figure tied to the MPA 07 W6390 EWD (by Name/attribute, or text mentioning EWD), plus anything within
// 25 m of the EWD centreline start, with layer, type, key attributes and vertices.
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
Deswik.Graphics.Primaries.Figure cl = null, dec = null;
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || !f.IsPolyline || A(f, "CLAUDE_MPA07") != "1" || f.Layer.Name != "0\\DESIGN DRAFT\\CENTERLINES") continue;
    if (A(f, "Name") == "W6390 EWD") cl = f; else if (A(f, "ACT_TYPE") == "DEC") dec = f;
}
var c0 = cl.asPolyline.GetVertex(0); double jx = c0.x, jy = c0.y;
for (int i = 0; i < cl.asPolyline.VertexCount; i++) { var q = cl.asPolyline.GetVertex(i); Out.AppendLine($"EWD CL {i}: ({q.x:F3},{q.y:F3},{q.z:F3})"); }
Out.AppendLine("EWD CL attrs: " + string.Join("; ", new[] { "Name", "ACT_TYPE", "PROFILE", "GSS", "Description", "DIRECTION", "LEVEL", "MINE", "Rate" }.Select(k => k + "=" + A(cl, k))));
// decline station of the EWD start
var D = Enumerable.Range(0, dec.asPolyline.VertexCount).Select(i => dec.asPolyline.GetVertex(i)).ToList(); double s = 0, bs = 0, bd = 1e9;
for (int i = 1; i < D.Count; i++) { double vx = D[i].x - D[i - 1].x, vy = D[i].y - D[i - 1].y, L = Math.Sqrt(vx * vx + vy * vy); double t = Math.Max(0, Math.Min(1, ((jx - D[i - 1].x) * vx + (jy - D[i - 1].y) * vy) / (L * L))); double d = Math.Sqrt(Math.Pow(D[i - 1].x + t * vx - jx, 2) + Math.Pow(D[i - 1].y + t * vy - jy, 2)); if (d < bd) { bd = d; bs = s + t * L; } s += L; }
Out.AppendLine($"EWD start is {bd:F2} m off the decline centreline at decline station {bs:F2} m of {s:F2} m");
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null) continue;
    var bb = f.BoundingBox; var m = bb.Midpoint; double d = Math.Sqrt(Math.Pow(m.x - jx, 2) + Math.Pow(m.y - jy, 2));
    string txt = f.IsText ? f.asText.TextString : f.IsMText ? f.asMText.TextString : "";
    bool named = A(f, "Name").Contains("EWD") || txt.Contains("EWD") || A(f, "ACT_TYPE") == "EWD";
    if (!named && d > 25) continue;
    if (f.Layer.Name.StartsWith("0\\DESIGN MASTER") || f.Layer.Name.StartsWith("0\\3MS")) continue;
    string kind = f.IsPolyline ? $"pl n={f.asPolyline.VertexCount} closed={f.asPolyline.Closed}" : f.IsPolyface ? "polyface" : f.IsText ? "text" : f.IsMText ? "mtext" : f.IsPoint ? "point" : f.GetType().Name;
    Out.AppendLine($"{f.Layer.Name} | {kind} | d={d:F1} | Name={A(f, "Name")} ACT={A(f, "ACT_TYPE")} PROF={A(f, "PROFILE")} tag={A(f, "CLAUDE_MPA07")} | \"{txt.Replace("\n", " ")}\" | mid ({m.x:F2},{m.y:F2},{m.z:F2})");
}
return null;
