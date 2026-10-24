// Fix attributes on 5 strings in layer "...\01 DEV STRINGS\STRINGS ADD - COLLEAGUE PROPOSE".
// Each change only happens if the attribute still holds the value checked on 16/10/2026 15:47;
// anything that has changed since is skipped and reported. Geometry is not touched. Nothing is saved.
var target = "STRINGS ADD - COLLEAGUE PROPOSE";
// handle, attribute, expected current value, new value
var changes = new (string h, string attr, string from, string to)[] {
    ("44B8CEC", "LEVEL", "346", "368"), ("44B8CEC", "Description", "Ore Drive", "Escapeway Rise"), ("44B8CEC", "STOPE TYPE", "", "-"),
    ("44B8CEF", "LEVEL", "346", "390"), ("44B8CEF", "Description", "Ore Drive", "Escapeway Rise"), ("44B8CEF", "STOPE TYPE", "", "-"),
    ("44B8CED", "LEVEL", "368", "390"), ("44B8CED", "Description", "Ore Drive", "Escapeway Drive"), ("44B8CED", "STOPE TYPE", "", "-"),
    ("44B8CEE", "LEVEL", "368", "390"), ("44B8CEE", "Description", "Ore Drive", "Escapeway Drive"), ("44B8CEE", "STOPE TYPE", "", "-"),
    ("44B8CF1", "Name", "W6346 RAR", "") };
var figs = new Dictionary<string, Deswik.Graphics.Primaries.Figure>();
foreach (var e in App.ModelSpace.Entities) {
    var f = e as Deswik.Graphics.Primaries.Figure; if (f == null || f.Layer == null || !f.Layer.Name.EndsWith(target)) continue;
    figs[System.Convert.ToString(f.GetType().GetProperty("Handle")?.GetValue(f))] = f;
}
int done = 0, skipped = 0;
foreach (var c in changes) {
    if (!figs.TryGetValue(c.h, out var f)) { Out.AppendLine($"{c.h}: not found on {target} - skipped"); skipped++; continue; }
    var now = System.Convert.ToString(f.AttributeValueGet(c.attr, ""));
    if (now != c.from) { Out.AppendLine($"{c.h} {c.attr}: is '{now}', expected '{c.from}' - skipped"); skipped++; continue; }
    f.AttributeValueSet(c.attr, c.to);
    Out.AppendLine($"{c.h} {f.AttributeValueGet("ACT_TYPE", "")} {c.attr}: '{c.from}' -> '{System.Convert.ToString(f.AttributeValueGet(c.attr, ""))}'"); done++;
}
App.Redraw(true, false);
Out.AppendLine($"Changed {done}, skipped {skipped}. To undo, run the same list with from/to swapped. File not saved.");
return null;
