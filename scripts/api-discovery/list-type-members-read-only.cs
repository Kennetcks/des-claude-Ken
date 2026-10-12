// List loaded Deswik assemblies and the public members of one type (read only, .NET reflection)
// Body of: object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out)
// Usings:
// Tested in Deswik CAD 2024.2 (2026-10-12) on Deswik.Graphics.Primaries.Figure.

string TypeName = "Deswik.Graphics.Primaries.Figure"; // type to explore
string Filter = "";                                    // e.g. "Attribute" to show only matching members
var asms = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetName().Name.StartsWith("Deswik")).OrderBy(a => a.GetName().Name).ToList();
Out.AppendLine("Deswik assemblies loaded: " + asms.Count);
foreach (var a in asms.Take(15)) { string loc=""; try { loc = a.Location; } catch {} Out.AppendLine("  " + a.GetName().Name + " " + a.GetName().Version + "  " + loc); }
if (asms.Count > 15) Out.AppendLine("  ...");
Type t = null;
foreach (var a in asms) { try { t = a.GetType(TypeName, false); } catch {} if (t != null) break; }
if (t == null) { Out.AppendLine("Type not found: " + TypeName); return null; }
Out.AppendLine("");
Out.AppendLine(t.FullName + "  (" + t.Assembly.GetName().Name + ")  base: " + (t.BaseType == null ? "-" : t.BaseType.FullName));
var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.DeclaredOnly;
int n = 0;
foreach (var p in t.GetProperties(flags).OrderBy(x => x.Name)) { if (Filter != "" && p.Name.IndexOf(Filter, StringComparison.OrdinalIgnoreCase) < 0) continue; Out.AppendLine("  prop   " + p.PropertyType.Name + " " + p.Name); if (++n > 60) break; }
n = 0;
foreach (var m in t.GetMethods(flags).Where(x => !x.IsSpecialName).OrderBy(x => x.Name)) {
  if (Filter != "" && m.Name.IndexOf(Filter, StringComparison.OrdinalIgnoreCase) < 0) continue;
  Out.AppendLine("  method " + m.ReturnType.Name + " " + m.Name + "(" + string.Join(", ", m.GetParameters().Select(q => q.ParameterType.Name + " " + q.Name).ToArray()) + ")");
  if (++n > 80) { Out.AppendLine("  ... (use Filter)"); break; }
}
return null;
