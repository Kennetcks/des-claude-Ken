// Read-only (Ventilation Plan Master): attribute names/values on the vent station polyline (via the document's attribute
// field list), the Figure/App methods that look like attribute or annotation APIs, and the station layer's annotation setup.
Deswik.Graphics.Primaries.Figure st = null;
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (!f.Deleted && f.Layer != null && f.Layer.Name == "0\\PRIMARY STATIONS" && f.IsPolyline) st = f;
foreach (var m in st.GetType().GetMethods().Where(m => m.Name.Contains("Attribute")).Select(m => m.Name + "(" + string.Join(",", m.GetParameters().Select(p => p.ParameterType.Name)) + ")").Distinct()) Out.AppendLine("Figure." + m);
foreach (var m in App.GetType().GetMembers().Where(m => System.Text.RegularExpressions.Regex.IsMatch(m.Name, "Attribute|Annotat")).Select(m => m.MemberType + " " + m.Name).Distinct()) Out.AppendLine("App." + m);
foreach (var m in st.Layer.GetType().GetMembers().Where(m => System.Text.RegularExpressions.Regex.IsMatch(m.Name, "Attribute|Annotat|Label|Text")).Select(m => m.MemberType + " " + m.Name).Distinct()) Out.AppendLine("Layer." + m);
return null;
