if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Read-only: write every report view's row names to C:\dswtmp\view_names.tsv (view index, view name, row name).
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
object G(object o, string n) { if (o == null) return null; var p = o.GetType().GetProperty(n, BF); if (p != null && p.GetIndexParameters().Length == 0) return p.GetValue(o); return o.GetType().GetField(n, BF)?.GetValue(o); }
var views = ((System.Collections.IEnumerable)G(Schedule, "CrossTabViews")).Cast<object>().ToList();
var sb = new StringBuilder();
for (int i = 0; i < views.Count; i++) foreach (var tf in (System.Collections.IEnumerable)G(views[i], "TotaledFields")) sb.AppendLine($"{i}\t{G(views[i], "Name")}\t{G(tf, "Name")}");
System.IO.File.WriteAllText(@"C:\dswtmp\view_names.tsv", sb.ToString());
Out.AppendLine("ok");
return null;
