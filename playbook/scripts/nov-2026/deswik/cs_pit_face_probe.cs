// Read-only: what a face of the pit surface 4589044 looks like (type, properties), so RL on the surface can be found.
Deswik.Graphics.Primaries.Figure pit = null; foreach (Deswik.Graphics.Primaries.Figure x in App.ModelSpace.Entities) if (!x.Deleted && x.Handle == "4589044") pit = x;
var pf = pit.asPolyface; var fl = pf.FaceList.Cast<object>().ToList(); var f0 = fl[0]; var t = f0.GetType();
Out.AppendLine($"face type {t.FullName}; count {fl.Count}");
foreach (var p in t.GetProperties()) { object v = null; try { v = p.GetValue(f0); } catch { } Out.AppendLine($"  prop {p.Name} : {p.PropertyType.Name} = {v}"); }
foreach (var fd in t.GetFields()) Out.AppendLine($"  field {fd.Name} : {fd.FieldType.Name} = {fd.GetValue(f0)}");
foreach (var m in typeof(Deswik.Graphics.Figures.Polyface).GetMethods().Where(m => m.Name.Contains("Elev") || m.Name.Contains("Intersect") || m.Name.Contains("Z") || m.Name.Contains("Height") || m.Name.Contains("Drape") || m.Name.Contains("Face"))) Out.AppendLine($"  method {m.Name}({string.Join(",", m.GetParameters().Select(q => q.ParameterType.Name + " " + q.Name))}) : {m.ReturnType.Name}");
return null;
