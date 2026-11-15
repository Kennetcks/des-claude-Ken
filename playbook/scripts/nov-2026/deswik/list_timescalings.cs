if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Read-only: list report time scalings and how the active one is chosen.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
object G(object o, string n) { if (o == null) return null; var p = o.GetType().GetProperty(n, BF); if (p != null && p.GetIndexParameters().Length == 0) return p.GetValue(o); return o.GetType().GetField(n, BF)?.GetValue(o); }
foreach (var p in Schedule.GetType().GetProperties(BF).Where(p => System.Text.RegularExpressions.Regex.IsMatch(p.Name, "(?i)timescal")))
{ object v = null; try { v = p.GetValue(Schedule); } catch { } Out.AppendLine($"P {p.Name} : {p.PropertyType.Name} set:{p.CanWrite} = {(v is System.Collections.ICollection c ? "[" + c.Count + "]" : System.Convert.ToString(v)?.Split('\n')[0])}");
  if (v is System.Collections.IEnumerable en && !(v is string)) { int k = 0; foreach (var it in en) Out.AppendLine($"   [{k++}] {G(it, "Name")}"); } }
foreach (var f in Schedule.GetType().GetFields(BF).Where(f => System.Text.RegularExpressions.Regex.IsMatch(f.Name, "(?i)timescal"))) Out.AppendLine($"F {f.Name} : {f.FieldType.Name} = {System.Convert.ToString(f.GetValue(Schedule))?.Split('\n')[0]}");
var rd = G(Schedule, "reportData");
var ts = G(rd, "ActiveTimeScaling");
Out.AppendLine("rd.ActiveTimeScaling props: " + string.Join(", ", ts.GetType().GetProperties(BF).Where(p => p.GetIndexParameters().Length == 0).Select(p => { object v = null; try { v = p.GetValue(ts); } catch { } return p.Name + "=" + System.Convert.ToString(v)?.Split('\n')[0]; })));
Out.AppendLine("rd ActiveTimeScaling setter: " + (rd.GetType().GetProperty("ActiveTimeScaling", BF)?.CanWrite));
return null;
