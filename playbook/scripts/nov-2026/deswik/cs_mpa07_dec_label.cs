// MPA 07: update the MAIN DEC label length (and any length attribute on the tagged centreline) to the extended string.
const bool APPLY = false;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
Deswik.Graphics.Primaries.Figure dec = null; var labs = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || !f.Layer.Name.StartsWith("0\\DESIGN DRAFT")) continue;
    if (f.IsPolyline && f.Layer.Name == "0\\DESIGN DRAFT\\CENTERLINES" && A(f, "CLAUDE_MPA07") == "1" && A(f, "ACT_TYPE") == "DEC") dec = f;
    else if (f.IsMText && f.asMText.TextString.Contains("Length")) labs.Add(f);
}
var pl = dec.asPolyline; double len = 0; for (int i = 1; i < pl.VertexCount; i++) { var a = pl.GetVertex(i - 1); var b = pl.GetVertex(i); len += Math.Sqrt(Math.Pow(b.x - a.x, 2) + Math.Pow(b.y - a.y, 2)); }
Out.AppendLine($"decline length {len:F1} m");
foreach (var k in new[] { "LENGTH", "Length", "LENGHT", "PLAN_LENGTH" }) { var v = A(dec, k); if (v != "") Out.AppendLine($"attribute {k} = {v}"); }
// labels within 40 m of the decline start, naming MAIN DEC
var p0 = pl.GetVertex(0); var cand = new List<Deswik.Graphics.Primaries.Figure>();
foreach (var f in labs)
{
    var m = f.BoundingBox.Midpoint; double d = Math.Sqrt(Math.Pow(m.x - p0.x, 2) + Math.Pow(m.y - p0.y, 2));
    var t = f.asMText.TextString; Out.AppendLine($"label on {f.Layer.Name} {d:F0} m from start: \"{t.Replace("\r\n", " / ").Replace("\\P", " / ")}\"");
    if (d < 60 && t.Contains("MAIN DEC")) cand.Add(f);
}
if (cand.Count != 1) { Out.AppendLine($"{cand.Count} MAIN DEC labels found - nothing changed"); return null; }
var lab = cand[0]; var txt = lab.asMText.TextString;
var nt = System.Text.RegularExpressions.Regex.Replace(txt, @"Length:\s*[\d.]+\s*m", $"Length: {len:F1}m");
Out.AppendLine($"label -> \"{nt.Replace("\r\n", " / ").Replace("\\P", " / ")}\"");
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }
lab.asMText.TextString = nt;
foreach (var k in new[] { "LENGTH", "Length", "LENGHT" }) if (A(dec, k) != "") dec.AttributeValueSet(k, len.ToString("F1", System.Globalization.CultureInfo.InvariantCulture));
App.Redraw(true); Out.AppendLine("applied"); return null;
