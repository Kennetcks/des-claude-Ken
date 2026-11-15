if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Read-only: figures with handle between the two hex handles in C:\dswtmp\find_text.txt ("lo,hi"), grouped by MINE/LEVEL/ACT_TYPE,
// all attributes of the two end figures, and the scheduled tasks whose Start matches each figure's Start attribute.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
string FV(Deswik.Scheduler.Engine.Task t, string f) { try { return System.Convert.ToString(t.GetNullaleFieldFullValue(f)); } catch { return ""; } }
var hs = System.IO.File.ReadAllText(@"C:\dswtmp\find_text.txt").Trim().Split(',');
long lo = System.Convert.ToInt64(hs[0], 16), hi = System.Convert.ToInt64(hs[1], 16);
var figs = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) {
  long v; if (!long.TryParse(System.Convert.ToString(f.GetType().GetProperty("Handle", BF)?.GetValue(f)), System.Globalization.NumberStyles.HexNumber, null, out v)) continue;
  if (v >= lo && v <= hi) figs.Add(f);
}
Out.AppendLine($"figures in handle range: {figs.Count}");
foreach (var g in figs.GroupBy(f => $"{f.AttributeValueGet("MINE", "")} {f.AttributeValueGet("LEVEL", "")} {f.AttributeValueGet("ACT_TYPE", "")} SCH={f.AttributeValueGet("SCHEDULED", "")}"))
  Out.AppendLine($"  {g.Key}: {g.Count()}");
foreach (var h in hs) {
  var fig = figs.FirstOrDefault(f => string.Equals(System.Convert.ToString(f.GetType().GetProperty("Handle", BF)?.GetValue(f)), h, System.StringComparison.OrdinalIgnoreCase));
  if (fig == null) { Out.AppendLine($"{h}: not found"); continue; }
  Out.AppendLine($"== {h} attributes");
  try { foreach (var a in (System.Collections.IEnumerable)fig.GetType().GetMethod("AttributeNames", BF, null, System.Type.EmptyTypes, null)?.Invoke(fig, null) ?? new object[0]) Out.AppendLine($"  {a} = {fig.AttributeValueGet(System.Convert.ToString(a), "")}"); } catch (System.Exception e) { Out.AppendLine("  (attr list failed: " + e.Message + ")"); }
  System.DateTime st; if (!System.DateTime.TryParse(System.Convert.ToString(fig.AttributeValueGet("Start", "")), out st)) continue;
  foreach (var t in Schedule.Tasks.Where(t => !t.Deleted && System.Math.Abs((t.Start - st).TotalMinutes) < 2))
    Out.AppendLine($"  TASK [{t.Id}] {t.Name} {FV(t, "MINE")}/{FV(t, "LEVEL")}/{FV(t, "ACT_TYPE")} {t.Start:dd/MM/yy HH:mm}-{t.Finish:dd/MM/yy HH:mm} res={FV(t, "Resource")}");
}
return null;
