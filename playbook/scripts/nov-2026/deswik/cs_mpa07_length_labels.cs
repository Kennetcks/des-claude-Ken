// MPA 07: set the "Length: x.xm" labels (0\DESIGN DRAFT\CENTERLINES\LENGTH) of MAIN DEC, W6390 RAD and W6390 EWD to the
// 3D length of their tagged centrelines. Each label is matched to its drive through the drive-name text stacked next to
// it. Dry run unless APPLY.
const bool APPLY = false;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
string T(Deswik.Graphics.Primaries.Figure f) => f.IsText ? f.asText.TextString : (f.IsMText ? f.asMText.TextString : null);
var len = new Dictionary<string, double>(); var lens = new List<Deswik.Graphics.Primaries.Figure>(); var names = new List<(string n, double x, double y)>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || !f.Layer.Name.StartsWith("0\\DESIGN DRAFT\\CENTERLINES")) continue;
    if (f.IsPolyline && f.Layer.Name == "0\\DESIGN DRAFT\\CENTERLINES" && A(f, "CLAUDE_MPA07") == "1")
    {
        var pl = f.asPolyline; double L = 0; for (int i = 1; i < pl.VertexCount; i++) { var a = pl.GetVertex(i - 1); var b = pl.GetVertex(i); L += Math.Sqrt(Math.Pow(b.x - a.x, 2) + Math.Pow(b.y - a.y, 2) + Math.Pow(b.z - a.z, 2)); }
        len[A(f, "Name")] = L;
    }
    else { var t = T(f); if (t == null) continue; var m = f.BoundingBox.Midpoint; if (t.StartsWith("Length:")) lens.Add(f); else names.Add((t.Trim(), m.x, m.y)); }
}
var todo = new List<(Deswik.Graphics.Primaries.Figure f, string nt)>();
foreach (var f in lens)
{
    var m = f.BoundingBox.Midpoint;
    var nm = names.Where(q => len.ContainsKey(q.n)).Select(q => (q.n, d: Math.Sqrt(Math.Pow(q.x - m.x, 2) + Math.Pow(q.y - m.y, 2)))).OrderBy(q => q.d).FirstOrDefault();
    if (nm.n == null || nm.d > 6) continue;
    string nt = $"Length: {len[nm.n]:F1}m"; Out.AppendLine($"{nm.n}: \"{T(f)}\" -> \"{nt}\" (name label {nm.d:F1} m away)");
    if (T(f) != nt) todo.Add((f, nt));
}
foreach (var kv in len) Out.AppendLine($"  {kv.Key} centreline 3D length {kv.Value:F2} m");
if (!APPLY) { Out.AppendLine($"{todo.Count} to change - dry run, nothing changed"); return null; }
foreach (var (f, nt) in todo) { if (f.IsText) f.asText.TextString = nt; else f.asMText.TextString = nt; }
App.Redraw(true); Out.AppendLine($"applied: {todo.Count} labels"); return null;
