// Read-only (Ventilation Plan Master): the PRIMARY STATIONS figures - kind (Is* flags), vertices/position, all attributes,
// text and text properties - and the nearest centreline/walls to each.
var all = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (!f.Deleted && f.Layer != null) all.Add(f);
foreach (var f in all.Where(f => f.Layer.Name.StartsWith("0\\PRIMARY STATIONS")))
{
    var t = f.GetType(); var flags = t.GetProperties().Where(p => p.Name.StartsWith("Is") && p.PropertyType == typeof(bool)).Where(p => { try { return (bool)p.GetValue(f); } catch { return false; } }).Select(p => p.Name);
    var b = f.BoundingBox;
    Out.AppendLine($"=== {f.Handle} {f.Layer.Name} [{string.Join(",", flags)}] bbox ({b.Min.x:F2},{b.Min.y:F2},{b.Min.z:F2})-({b.Max.x:F2},{b.Max.y:F2},{b.Max.z:F2})");
    if (f.IsPolyline) { var pl = f.asPolyline; Out.AppendLine("  pts: " + string.Join(" ", Enumerable.Range(0, pl.VertexCount).Select(i => { var q = pl.GetVertex(i); return $"({q.x:F2},{q.y:F2},{q.z:F2})"; })) + $" closed={pl.Closed}"); }
    foreach (var p in t.GetProperties().Where(p => p.Name.StartsWith("as")))
    {
        object o = null; try { o = p.GetValue(f); } catch { } if (o == null) continue;
        foreach (var q in o.GetType().GetProperties().Where(q => q.PropertyType.IsPrimitive || q.PropertyType == typeof(string) || q.PropertyType.IsEnum || q.PropertyType.Name.Contains("Point") || q.PropertyType.Name.Contains("Vector")))
        { try { Out.AppendLine($"  {p.Name}.{q.Name} = {q.GetValue(o)}"); } catch { } }
    }
    // attributes: try the attribute collection by reflection
    foreach (var m in t.GetMethods().Where(m => m.Name.StartsWith("Attribute") && m.GetParameters().Length == 0)) { try { var r = m.Invoke(f, null); if (r is System.Collections.IEnumerable e && !(r is string)) foreach (var x in e) Out.AppendLine($"  {m.Name}: {x}"); else Out.AppendLine($"  {m.Name}: {r}"); } catch { } }
}
Out.AppendLine("layer names under PRIMARY STATIONS / LEGEND sample:");
foreach (var ln in all.Select(f => f.Layer.Name).Distinct().Where(n => n.Contains("STATION"))) Out.AppendLine("  " + ln);
return null;
