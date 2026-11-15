// Read-only: current state of the MPA 07 W6390 RAD / DSP in the Level Design Drafter - every polyline on 0\DESIGN DRAFT
// layers within 45 m of the decline joint (name, layer, attributes, vertices), plus non-polylines (text/points) nearby.
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
var all = new List<Deswik.Graphics.Primaries.Figure>(); foreach (Deswik.Graphics.Primaries.Figure x in App.ModelSpace.Entities) if (!x.Deleted && x.Layer != null) all.Add(x);
Out.AppendLine("file " + App.Filename);
double jx = 8082.206, jy = 63112.193;   // 44BD9C0 end = joint
bool Near(Deswik.Graphics.Primaries.Figure f, double r) { var m = f.BoundingBox; double cx = Math.Max(m.Min.x, Math.Min(jx, m.Max.x)), cy = Math.Max(m.Min.y, Math.Min(jy, m.Max.y)); return Math.Sqrt(Math.Pow(cx - jx, 2) + Math.Pow(cy - jy, 2)) < r; }
foreach (var f in all.Where(f => f.Layer.Name.StartsWith("0\\DESIGN DRAFT") && Near(f, 45)).OrderBy(f => f.Layer.Name).ThenBy(f => f.Handle))
{
    string kind = f.IsPolyline ? "pl" : f.IsText ? "text" : f.IsMText ? "mtext" : f.IsPoint ? "pt" : f.IsPolyface ? "solid" : "other";
    string txt = f.IsText ? f.asText.TextString : f.IsMText ? f.asMText.TextString : "";
    var line = $"{f.Handle} [{f.Layer.Name.Replace("0\\DESIGN DRAFT", "DD")}] {kind} Name='{A(f, "Name")}' ACT={A(f, "ACT_TYPE")} PROF={A(f, "PROFILE")} GSS={A(f, "GSS")} {txt.Replace("\n", " ")}";
    if (f.IsPolyline) { var p = f.asPolyline; line += $" n={p.VertexCount} cl={p.Closed}: " + string.Join(" ", Enumerable.Range(0, p.VertexCount).Select(i => { var q = p.GetVertex(i); return $"({q.x:F2},{q.y:F2},{q.z:F2})"; })); }
    else { var m = f.BoundingBox.Midpoint; line += $" at ({m.x:F2},{m.y:F2},{m.z:F2})"; }
    Out.AppendLine(line.Length > 1400 ? line.Substring(0, 1400) + " ..." : line);
}
return null;
