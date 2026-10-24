if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW " + System.IO.Path.GetFileName(App.Filename)); return null; }
// FINAL layer: make Description / SLOT_FLG match the engineer's ACT_TYPE edits (read 17/10/2026 12:14). ACT_TYPE itself is not changed.
// Each change only happens if the value is still what was read; otherwise skipped and reported. Geometry not touched. File not saved.
var changes = new (string h, string attr, string from, string to)[] {
  ("44B8DD5", "Description", "Return Air Drive", "Escapeway Drive"),  // ACT_TYPE EWD
  ("44B8DEC", "Description", "Escapeway Drive", "Return Air Drive"),  // ACT_TYPE RAD
  ("44B8DED", "Description", "Escapeway Rise", "Return Air Rise"),    // ACT_TYPE RAR
  ("44B8DED", "SLOT_FLG", "0", "1"),                                  // RAR + RHINO, as colleague RARs and 44B8DEF
  ("44B8DEE", "Description", "Escapeway Drive", "Return Air Drive") }; // ACT_TYPE RAD
var al = App.ActiveLayer; var figs = new Dictionary<string, Deswik.Graphics.Primaries.Figure>();
foreach (var e in App.ModelSpace.Entities) { var f = e as Deswik.Graphics.Primaries.Figure; if (f == null || f.Layer == null || f.Deleted || f.Layer.Name != al.Name) continue;
  figs[$"{f.GetType().GetProperty("Handle")?.GetValue(f)}"] = f; }
int done = 0, skipped = 0;
foreach (var c in changes) {
  if (!figs.TryGetValue(c.h, out var f)) { Out.AppendLine($"{c.h}: not found - skipped"); skipped++; continue; }
  var now = System.Convert.ToString(f.AttributeValueGet(c.attr, ""));
  if (now != c.from) { Out.AppendLine($"{c.h} {c.attr}: is '{now}', expected '{c.from}' - skipped"); skipped++; continue; }
  f.AttributeValueSet(c.attr, c.to); done++;
  Out.AppendLine($"{c.h} {f.AttributeValueGet("ACT_TYPE", "")} {c.attr}: '{c.from}' -> '{System.Convert.ToString(f.AttributeValueGet(c.attr, ""))}'"); }
App.Redraw(true, false);
Out.AppendLine($"Changed {done}, skipped {skipped}. File not saved.");
return null;
