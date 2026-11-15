// Read-only: CENTERLINES\GRADE texts per drive Name (text, position) and each drive's centreline vertex count / attributes
// that might drive labelling (GRADE fields), to see whether the double grade label is RAD-only.
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
var lab = new Dictionary<string, List<string>>(); var cls = new Dictionary<string, Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null) continue;
    if (f.Layer.Name == "0\\DESIGN DRAFT\\CENTERLINES\\GRADE" && f.IsText) { var n = A(f, "Name"); if (!lab.ContainsKey(n)) lab[n] = new List<string>(); var m = f.BoundingBox.Midpoint; lab[n].Add($"'{f.asText.TextString}'@({m.x:F1},{m.y:F1})"); }
    if (f.Layer.Name == "0\\DESIGN DRAFT\\CENTERLINES" && f.IsPolyline) cls[A(f, "Name")] = f;
}
foreach (var kv in lab) Out.AppendLine($"{kv.Key}: {kv.Value.Count} -> {string.Join(" ", kv.Value)}" + (cls.ContainsKey(kv.Key) ? $" | CL n={cls[kv.Key].asPolyline.VertexCount}" : ""));
// attribute names on the RAD centreline (any containing GRAD / DIR / LABEL)
var rad = cls["W6390 RAD"]; var names = new List<string>();
foreach (var mi in rad.GetType().GetMethods().Where(m => m.Name.StartsWith("Attribute") && m.GetParameters().Length == 0)) Out.AppendLine("method " + mi.Name);
return null;
