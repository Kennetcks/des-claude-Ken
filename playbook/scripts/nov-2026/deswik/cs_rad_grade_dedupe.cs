// W6390 RAD: delete duplicate CENTERLINES\GRADE labels - a text (and the leader generated with it) identical in text and
// position to an earlier one is removed, the first pair kept. Dry run unless APPLY.
const bool APPLY = false;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
var gr = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
    if (!f.Deleted && f.Layer != null && f.Layer.Name == "0\\DESIGN DRAFT\\CENTERLINES\\GRADE" && A(f, "Name") == "W6390 RAD" && (f.IsText || f.IsLeader)) gr.Add(f);
string Key(Deswik.Graphics.Primaries.Figure f) { var m = f.BoundingBox.Midpoint; return (f.IsText ? "T" + f.asText.TextString : "L") + $"@{m.x:F2},{m.y:F2},{m.z:F2}"; }
var seen = new HashSet<string>(); var dup = new List<Deswik.Graphics.Primaries.Figure>();
foreach (var f in gr) if (!seen.Add(Key(f))) dup.Add(f);
foreach (var f in gr) Out.AppendLine($"{f.Handle} {Key(f)}{(dup.Contains(f) ? "  <- duplicate" : "")}");
if (dup.Count == 0) { Out.AppendLine("no duplicates - nothing changed"); return null; }
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }
foreach (var f in dup) f.Deleted = true;
App.Redraw(true); Out.AppendLine($"applied - {dup.Count} deleted"); return null;
