// MPA 07 W6390 DSP: GSS R 3.0 -> R 2.0 (Design Master DSPs at 5.5 m use R 2.0) on every DSP figure, and the GSS label text.
// Dry run unless APPLY.
const bool APPLY = false;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
var figs = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
    if (!f.Deleted && f.Layer != null && A(f, "CLAUDE_MPA07") == "1" && A(f, "Name") == "W6390 DSP") figs.Add(f);
var lab = figs.Where(f => f.IsText && f.asText.TextString.StartsWith("GSS:")).ToList();
Out.AppendLine($"{figs.Count} DSP figures (GSS now: {string.Join(", ", figs.Select(f => A(f, "GSS")).Distinct())}); GSS labels {lab.Count}: {string.Join(", ", lab.Select(f => f.asText.TextString))}");
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }
foreach (var f in figs) f.AttributeValueSet("GSS", "R 2.0");
foreach (var f in lab)
{
    f.asText.TextString = "GSS:R 2.0";
    foreach (var name in new[] { "Update", "Invalidate" }) { var m = f.GetType().GetMethods().FirstOrDefault(x => x.Name == name && x.GetParameters().Length == 0); if (m != null) try { m.Invoke(f, null); } catch { } }
}
var rf = App.GetType().GetMethods().FirstOrDefault(x => x.Name == "Refresh" && x.GetParameters().Length == 0); rf?.Invoke(App, null);
App.Redraw(true); Out.AppendLine("applied: GSS R 2.0, label 'GSS:R 2.0'"); return null;
