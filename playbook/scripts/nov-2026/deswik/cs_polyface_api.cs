// Read-only: how a Figure wraps a Polyface (constructors / setters), and where solids already are in the drawing.
var ft = typeof(Deswik.Graphics.Primaries.Figure);
Out.AppendLine("Figure base: " + ft.BaseType.FullName + "; Polyface base: " + typeof(Deswik.Graphics.Figures.Polyface).BaseType.FullName);
foreach (var m in ft.GetMethods().Where(m => m.GetParameters().Any(p => p.ParameterType.Name.Contains("Polyface") || p.ParameterType.Name.Contains("Primitive") || p.ParameterType.Name.Contains("Geometry"))))
    Out.AppendLine((m.IsStatic ? "static " : "") + m.ReturnType.Name + " " + m.Name + "(" + string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name)) + ")");
foreach (var p in ft.GetProperties().Where(p => p.CanWrite && (p.PropertyType.Name.Contains("Poly") || p.PropertyType.Name.Contains("Primitive") || p.PropertyType.Name.Contains("Figure"))))
    Out.AppendLine("settable prop " + p.PropertyType.FullName + " " + p.Name);
var layers = new Dictionary<string, int>(); int n = 0;
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) { if (f.Deleted || f.Layer == null || !f.IsPolyface) continue; n++; layers[f.Layer.Name] = layers.TryGetValue(f.Layer.Name, out var c) ? c + 1 : 1; }
Out.AppendLine($"solids: {n}"); foreach (var kv in layers.OrderByDescending(k => k.Value).Take(8)) Out.AppendLine($"  {kv.Value} {kv.Key}");
Out.AppendLine("strip solids layer exists: " + App.Layers.Exists("0\\REFERENCE\\STRIP STANDARDS\\SOLIDS"));
return null;
