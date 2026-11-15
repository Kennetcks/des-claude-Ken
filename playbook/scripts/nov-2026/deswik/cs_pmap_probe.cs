// Read-only: what the open Deswik window exposes about process maps - App members named *Process*/*Map*/*Document*,
// and the loaded assemblies that look like process-map ones.
var t = App.GetType(); Out.AppendLine("App type " + t.FullName);
foreach (var m in t.GetMembers().Where(m => System.Text.RegularExpressions.Regex.IsMatch(m.Name, "Process|Map|Document|Project|Workflow", System.Text.RegularExpressions.RegexOptions.IgnoreCase)).Select(m => m.MemberType + " " + m.Name).Distinct()) Out.AppendLine("  " + m);
foreach (var a in AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetName().Name).Where(n => System.Text.RegularExpressions.Regex.IsMatch(n, "Process|Flow|Map", System.Text.RegularExpressions.RegexOptions.IgnoreCase)).OrderBy(n => n)) Out.AppendLine("asm " + a);
return null;
