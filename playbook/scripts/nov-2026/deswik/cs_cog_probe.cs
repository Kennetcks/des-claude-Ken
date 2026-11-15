// Read-only: anything "COG" - layers, attribute fields, text - and what of it sits near the W6390 RAD.
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
var layers = new HashSet<string>(); int n = 0; var rad = new[] { 8087.8, 63123.1 };
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null) continue; var ln = f.Layer.Name;
    if (ln.ToUpper().Contains("COG")) layers.Add(ln);
    var m = f.BoundingBox.Midpoint; double d = Math.Sqrt(Math.Pow(m.x - rad[0], 2) + Math.Pow(m.y - rad[1], 2));
    bool txt = f.IsMText && f.asMText.TextString.ToUpper().Contains("COG");
    if ((ln.ToUpper().Contains("COG") || txt) && (d < 30 || n < 8))
    {
        n++; string kind = f.IsPolyline ? "polyline n=" + f.asPolyline.VertexCount : f.IsPoint ? "point" : f.IsMText ? "mtext" : f.IsLeader ? "leader" : "other";
        Out.AppendLine($"{ln} {kind} ({m.x:F2},{m.y:F2},{m.z:F2}) d={d:F1} Name={A(f, "Name")} ACT={A(f, "ACT_TYPE")} MPA07={A(f, "CLAUDE_MPA07")}" + (f.IsMText ? " '" + f.asMText.TextString.Replace("\r\n", " | ") + "'" : ""));
    }
}
Out.AppendLine("COG layers: " + string.Join("; ", layers));
// attribute fields on the RAD centreline containing COG
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
    if (!f.Deleted && f.Layer != null && f.Layer.Name == "0\\DESIGN DRAFT\\CENTERLINES" && A(f, "Name") == "W6390 RAD")
        foreach (var k in new[] { "COG", "COG_X", "COG_Y", "COG_Z", "COGX", "COGY", "COGZ", "CENTROID", "X", "Y", "Z", "MID_X", "MID_Y", "MID_Z", "LEN", "GRADE", "TOT_LEN" }) { var v = A(f, k); if (v != "") Out.AppendLine($"RAD attr {k}={v}"); }
return null;
