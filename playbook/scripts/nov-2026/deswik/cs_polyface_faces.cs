// Read-only: the types behind Polyface.VertexList / FaceList and their Add methods, and the raw face list of one strip solid.
var t = typeof(Deswik.Graphics.Figures.Polyface);
foreach (var name in new[] { "VertexList", "FaceList" })
{
    var pt = t.GetProperty(name).PropertyType; Out.AppendLine(name + ": " + pt.FullName + " ctors: " + string.Join(" | ", pt.GetConstructors().Select(c => "(" + string.Join(",", c.GetParameters().Select(p => p.ParameterType.Name)) + ")")));
    foreach (var m in pt.GetMethods().Where(m => m.Name == "Add" || m.Name == "AddRange")) Out.AppendLine("   " + m.Name + "(" + string.Join(",", m.GetParameters().Select(p => p.ParameterType.FullName)) + ")");
}
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || f.Layer.Name != "0\\REFERENCE\\STRIP STANDARDS\\SOLIDS" || !f.IsPolyface) continue;
    var pf = f.asPolyface; Out.AppendLine($"strip solid verts {pf.VertexCount} faces {pf.FaceCount}: " + string.Join(",", pf.FaceList.Cast<object>().Select(o => o.ToString())));
    break;
}
return null;
