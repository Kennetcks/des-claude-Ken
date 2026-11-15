// Read-only: how DSPs are attributed in the Design Master (Description, GSS, PROFILE), and what the EWD grade arrow is
// (type, geometry members, move/transform methods).
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
var seen = new Dictionary<string, int>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || A(f, "ACT_TYPE") != "DSP" || !f.Layer.Name.StartsWith("0\\DESIGN MASTER")) continue;
    var k = $"{f.Layer.Name} | Desc={A(f, "Description")} | GSS={A(f, "GSS")} | PROF={A(f, "PROFILE")}"; seen[k] = seen.TryGetValue(k, out var n) ? n + 1 : 1;
}
foreach (var kv in seen.OrderByDescending(q => q.Value).Take(15)) Out.AppendLine($"{kv.Value} x {kv.Key}");
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || f.Layer.Name != "0\\DESIGN DRAFT\\CENTERLINES\\GRADE" || A(f, "Name") != "W6390 EWD" || f.IsText) continue;
    Out.AppendLine($"grade arrow: {f.GetType().FullName}; IsPolyline={f.IsPolyline} IsBlockInsert={f.GetType().GetProperty("IsBlockInsert")?.GetValue(f)}");
    foreach (var p in f.GetType().GetProperties().Where(p => p.Name.Contains("Insert") || p.Name.Contains("Point") || p.Name.Contains("Rotation") || p.Name.StartsWith("as"))) { object v = null; try { v = p.GetValue(f); } catch { } if (v != null && p.Name.StartsWith("as")) Out.AppendLine($"  {p.Name}: {v.GetType().Name}"); else if (!p.Name.StartsWith("as")) Out.AppendLine($"  {p.Name} = {v}"); }
    foreach (var m in f.GetType().GetMethods().Where(m => m.Name.Contains("Move") || m.Name.Contains("Transl") || m.Name.Contains("Transform") || m.Name.Contains("Offset")).Select(m => m.Name + "(" + string.Join(",", m.GetParameters().Select(q => q.ParameterType.Name)) + ")").Distinct()) Out.AppendLine("  method " + m);
}
return null;
