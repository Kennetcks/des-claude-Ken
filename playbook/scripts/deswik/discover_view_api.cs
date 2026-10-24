// Read-only discovery: names and signatures of view / clipping / plane-definition members.
// Calls no property getters and changes nothing.
var BF = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static;
var rx = new System.Text.RegularExpressions.Regex("View|Clip|Plane|Camera|Viewport|Redraw|Regen|Zoom|Orient", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
System.Func<System.Reflection.MemberInfo, string> sig = m =>
    m is System.Reflection.MethodInfo mi ? mi.ReturnType.Name + " " + mi.Name + "(" + string.Join(", ", mi.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name)) + ")"
  : m is System.Reflection.PropertyInfo pi ? pi.PropertyType.FullName + " " + pi.Name + (pi.CanWrite ? " {get;set;}" : " {get;}")
  : m is System.Reflection.FieldInfo fi ? fi.FieldType.FullName + " " + fi.Name : m.Name;
Out.AppendLine("App type: " + App.GetType().FullName);
foreach (var m in App.GetType().GetMembers(BF).Where(m => !(m is System.Reflection.MethodInfo mm && mm.IsSpecialName) && rx.IsMatch(m.Name)).OrderBy(m => m.Name))
    Out.AppendLine("  App " + m.MemberType + ": " + sig(m));
var trx = new System.Text.RegularExpressions.Regex("ClippingBox|ClipBox|PlaneDefinition|ViewDirection|ViewManager|ViewState|SavedView|NamedView|Camera", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
int n = 0;
foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetName().Name.StartsWith("Deswik")))
{
    System.Type[] ts; try { ts = asm.GetTypes(); } catch (System.Reflection.ReflectionTypeLoadException e) { ts = e.Types.Where(t => t != null).ToArray(); }
    foreach (var t in ts.Where(t => t.IsPublic && trx.IsMatch(t.Name)).OrderBy(t => t.FullName))
    { if (++n > 80) break; Out.AppendLine("TYPE " + t.FullName + "  [" + asm.GetName().Name + "]"); }
}
return null;
