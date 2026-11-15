// Read-only (Ventilation Plan Master): how to put attribute fields on a layer and set values on a figure.
Deswik.Graphics.Primaries.Figure st = null, lab = null;
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null) continue;
    if (f.Layer.Name == "0\\PRIMARY STATIONS" && f.IsPolyline) st = f;
    if (f.Layer.Name == "0\\PRIMARY STATIONS\\LABELS") lab = f;
}
var la = st.Layer.Attributes; Out.AppendLine("Layer.Attributes type: " + la.GetType().FullName);
foreach (var m in la.GetType().GetMethods().Select(m => m.Name + "(" + string.Join(",", m.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name)) + ")").Distinct()) Out.AppendLine("  " + m);
foreach (Deswik.Common.Core.Objects.Attribute a in la) Out.AppendLine($"  attr '{a.Name}' type={a.GetType().FullName}");
var a0 = la.Cast<Deswik.Common.Core.Objects.Attribute>().First();
foreach (var p in a0.GetType().GetProperties()) { try { Out.AppendLine($"  Attribute.{p.Name} = {p.GetValue(a0)}"); } catch { } }
Out.AppendLine("LABELS layer attrs: " + string.Join(", ", lab.Layer.Attributes.Cast<Deswik.Common.Core.Objects.Attribute>().Select(a => a.Name)));
foreach (var m in st.GetType().GetMethods().Where(m => m.Name.Contains("Attribute")).Select(m => m.Name + "(" + string.Join(",", m.GetParameters().Select(p => p.ParameterType.Name)) + ")").Distinct()) Out.AppendLine("Figure." + m);
foreach (var m in App.GetType().GetMembers().Where(m => m.Name.Contains("Attribute")).Select(m => m.MemberType + " " + m.Name).Distinct()) Out.AppendLine("App." + m);
return null;
