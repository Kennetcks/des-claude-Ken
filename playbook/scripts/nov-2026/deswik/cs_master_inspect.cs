// Read-only: describe the current selection, and every strip on STRIPPING (tagged rebuilt ones grouped by shared corners)
// with the labels within 15 m of it.
const string SL = "0\\DESIGN MASTER\\STRIPPING";
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
Out.AppendLine("File: " + App.Filename);
foreach (var f in App.Selections.MainSelection.Cast<Deswik.Graphics.Primaries.Figure>())
{
    var m = f.BoundingBox.Midpoint;
    Out.AppendLine($"SEL layer={f.Layer?.Name} poly={f.IsPolyline} text={f.IsMText} name='{A(f, "Name")}' act='{A(f, "ACT_TYPE")}' tag='{A(f, "CLAUDE_DECLINE_STRIP")}' n={(f.IsPolyline ? f.asPolyline.VertexCount : 0)} mid=({m.x:F1},{m.y:F1},{m.z:F1})" + (f.IsMText ? " txt='" + f.asMText.TextString.Replace("\r\n", " / ").Replace("\n", " / ") + "'" : ""));
}
var tagged = new List<Deswik.Graphics.Primaries.Figure>(); var texts = new List<Deswik.Graphics.Primaries.Figure>(); var others = new List<Deswik.Graphics.Primaries.Figure>();
var seen = new HashSet<string>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null) continue;
    bool onSL = f.Layer.Name == SL; if (!onSL && !f.IsMText) continue;   // only strips and text: the drawing is large
    var m = f.BoundingBox.Midpoint; string sig = $"{f.Layer.Name}|{m.x:F2},{m.y:F2},{m.z:F2}|{f.IsMText}"; if (!seen.Add(sig)) continue;
    if (f.IsMText && f.asMText.TextString.ToLower().Contains("strip")) texts.Add(f);
    else if (f.Layer.Name == SL && f.IsPolyline) { if (A(f, "CLAUDE_DECLINE_STRIP") == "1") tagged.Add(f); else others.Add(f); }
}
// group tagged panels by shared corners
var F = tagged; int[] par = Enumerable.Range(0, F.Count).ToArray();
int Find(int i) { while (par[i] != i) i = par[i] = par[par[i]]; return i; }
var byKey = new Dictionary<string, int>();
for (int i = 0; i < F.Count; i++) for (int k = 0; k < F[i].asPolyline.VertexCount; k++) { var q = F[i].asPolyline.GetVertex(k); string key = $"{q.x:F2},{q.y:F2}"; if (byKey.TryGetValue(key, out var j)) par[Find(i)] = Find(j); else byKey[key] = i; }
foreach (var g in Enumerable.Range(0, F.Count).GroupBy(Find))
{
    var figs = g.Select(i => F[i]).ToList(); var pts = figs.SelectMany(f => Enumerable.Range(0, f.asPolyline.VertexCount).Select(k => f.asPolyline.GetVertex(k))).ToList();
    double cx = pts.Average(p => p.x), cy = pts.Average(p => p.y), cz = pts.Average(p => p.z);
    var red = figs.FirstOrDefault(f => f.asPolyline.HatchProperties?.FillBackColor.ColorIndex != 253);
    // red outline extent: max distance between its points across / along
    string ext = "";
    if (red != null) { var rp = Enumerable.Range(0, red.asPolyline.VertexCount).Select(k => red.asPolyline.GetVertex(k)).ToList(); double L = 0; for (int k = 1; k < rp.Count; k++) L += Math.Sqrt(Math.Pow(rp[k].x - rp[k - 1].x, 2) + Math.Pow(rp[k].y - rp[k - 1].y, 2)); ext = $" redN={rp.Count} redPerim={L:F1} redArea={Math.Abs(Enumerable.Range(0, rp.Count).Sum(k => rp[k].x * rp[(k + 1) % rp.Count].y - rp[(k + 1) % rp.Count].x * rp[k].y)) / 2:F1}"; }
    Out.AppendLine($"STRIP name='{A(figs[0], "Name")}' panels={figs.Count} centre=({cx:F1},{cy:F1},{cz:F1}){ext}");
    foreach (var t in texts.OrderBy(t => Math.Pow(t.BoundingBox.Midpoint.x - cx, 2) + Math.Pow(t.BoundingBox.Midpoint.y - cy, 2)).Take(2))
    { var m = t.BoundingBox.Midpoint; double d = Math.Sqrt(Math.Pow(m.x - cx, 2) + Math.Pow(m.y - cy, 2)); if (d < 25) Out.AppendLine($"   label {d:F1} m layer={t.Layer.Name} z={m.z:F1}: '{t.asMText.TextString.Replace("\r\n", " / ").Replace("\n", " / ")}'"); }
}
Out.AppendLine($"Untagged strips on STRIPPING: {others.Count}");
return null;
