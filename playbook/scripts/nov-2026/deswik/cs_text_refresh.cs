// Refresh the display of the three MPA 07 length labels after their text was changed: call the figure's own
// Update/Invalidate methods where they exist, then redraw. Text content is not changed.
var hs = new HashSet<string> { "4587951", "4587952", "4587953" }; var done = new List<string>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || !hs.Contains(Convert.ToString(f.GetType().GetProperty("Handle")?.GetValue(f)))) continue;
    foreach (var name in new[] { "Update", "Invalidate", "ResetCache", "Regen" })
    {
        var m = f.GetType().GetMethods().FirstOrDefault(x => x.Name == name && x.GetParameters().Length == 0);
        if (m != null) { try { m.Invoke(f, null); done.Add(name); } catch (Exception e) { done.Add(name + " failed: " + e.InnerException?.Message); } }
    }
}
foreach (var name in new[] { "Regen", "RegenAll", "Refresh" }) { var m = App.GetType().GetMethods().FirstOrDefault(x => x.Name == name && x.GetParameters().Length == 0); if (m != null) { m.Invoke(App, null); done.Add("App." + name); } }
App.Redraw(true);
Out.AppendLine("called: " + string.Join(", ", done.Distinct()));
return null;
