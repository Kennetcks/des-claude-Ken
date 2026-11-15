if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Read-only: write the task names in each "3MS S1x" filter to C:\dswtmp\slide_filter_names.tsv (filter, name).
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
object G(object o, string n) { var p = o.GetType().GetProperty(n, BF); if (p != null) return p.GetValue(o); return o.GetType().GetField(n, BF)?.GetValue(o); }
var fl = ((System.Collections.IEnumerable)G(Schedule, "<TaskFilters>k__BackingField")).Cast<object>().ToList();
var sb = new StringBuilder();
foreach (var f in fl.Where(x => System.Convert.ToString(G(x, "Name")).StartsWith("3MS S1")))
  foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(System.Convert.ToString(G(f, "where")), "Name=\"([^\"]+)\""))
    sb.AppendLine(G(f, "Name") + "\t" + m.Groups[1].Value);
System.IO.File.WriteAllText(@"C:\dswtmp\slide_filter_names.tsv", sb.ToString());
Out.AppendLine("ok");
return null;
