// Read-only: the W6390 RAD's CENTERLINES\GRADE / CONTROL / NOTES figures - their kind (Is* flags) and any text, by reflection.
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
var hs = new HashSet<string> { "4588CFF", "4588D01", "4588D02", "4588D04", "4588D05", "4588CFA", "45879C1", "45879C3", "4587948", "4587951" };
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || !hs.Contains(f.Handle.ToString())) continue;
    var t = f.GetType(); var flags = t.GetProperties().Where(p => p.Name.StartsWith("Is") && p.PropertyType == typeof(bool)).Where(p => { try { return (bool)p.GetValue(f); } catch { return false; } }).Select(p => p.Name);
    string txt = "";
    foreach (var p in t.GetProperties().Where(p => p.Name.StartsWith("as")))
    {
        object o = null; try { o = p.GetValue(f); } catch { } if (o == null) continue;
        foreach (var q in o.GetType().GetProperties().Where(q => q.Name == "TextString" || q.Name == "Text" || q.Name == "Value")) { try { txt += $" {p.Name}.{q.Name}='{q.GetValue(o)}'"; } catch { } }
    }
    var m = f.BoundingBox.Midpoint;
    Out.AppendLine($"{f.Handle} {f.Layer.Name} ({m.x:F2},{m.y:F2},{m.z:F2}) [{string.Join(",", flags)}]{txt}");
}
return null;
