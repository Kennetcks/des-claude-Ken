if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW " + System.IO.Path.GetFileName(App.Filename)); return null; }
// TEST 2: give 44B8F27 (flat-section loop option) the same scheduler attributes as the FINAL loop 44B8DE9 (RAD, level 390).
// Copies every non-empty value of 44B8DE9 onto 44B8F27 and lists what changed. Geometry not touched. File not saved.
Deswik.Graphics.Primaries.Figure src = null, dst = null;
foreach (var e in App.ModelSpace.Entities) { var f = e as Deswik.Graphics.Primaries.Figure; if (f == null || f.Deleted || f.Layer == null) continue;
  var h = $"{f.GetType().GetProperty("Handle")?.GetValue(f)}"; if (h == "44B8DE9") src = f; if (h == "44B8F27") dst = f; }
if (src == null || dst == null) { Out.AppendLine("44B8DE9 or 44B8F27 not found - nothing changed"); return null; }
int n = 0;
foreach (var a in dst.Layer.Attributes) { var name = (string)a.GetType().GetProperty("Name").GetValue(a);
  var v = System.Convert.ToString(src.AttributeValueGet(name, "")); if (v == "") continue;
  var old = System.Convert.ToString(dst.AttributeValueGet(name, "")); if (old == v) continue;
  dst.AttributeValueSet(name, v); n++; Out.AppendLine($"  {name}: '{old}' -> '{System.Convert.ToString(dst.AttributeValueGet(name, ""))}'"); }
App.Redraw(true, false);
Out.AppendLine($"44B8F27: {n} values set (copied from 44B8DE9). File not saved.");
return null;
