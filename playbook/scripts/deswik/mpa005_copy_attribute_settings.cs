// Copy the attribute field settings (Group, Description, Type, ValuesList, LimitToList, Required, DisplayInProperties, ...)
// from 0\DESIGN MASTER\CENTRELINES to 0\SKETCH LAYER\MPA TEST\CENTRELINES. Adds 6400 to the LEVEL list on the new layer only.
// Then re-applies each line's existing values so typed fields (SCHEDULED, SLOT_FLG = Double) hold numbers. Design Master unchanged.
var tm = App.Layers.FindName("0\\DESIGN MASTER\\CENTRELINES", false);
var nw = App.Layers.FindName("0\\SKETCH LAYER\\MPA TEST\\CENTRELINES", false);
if (tm == null || nw == null) { Out.AppendLine("layer not found"); return null; }
var copy = new[] { "Group", "Description", "Type", "ValuesList", "LimitToList", "LookupList", "Required", "Prompt", "Format", "DisplayMode", "WeightField", "DisplayInProperties", "Locked" };
var tAttrs = new Dictionary<string, object>(); foreach (var a in tm.Attributes) tAttrs[(string)a.GetType().GetProperty("Name").GetValue(a)] = a;
// keep the current values of the 5 lines before touching field types
var lines = new List<(Deswik.Graphics.Primaries.Figure f, Dictionary<string, string> v)>();
foreach (var e in App.ModelSpace.Entities) { var f = e as Deswik.Graphics.Primaries.Figure; if (f == null || f.Layer == null || f.Layer.Name != nw.Name) continue;
  var v = new Dictionary<string, string>(); foreach (var n in tAttrs.Keys) v[n] = System.Convert.ToString(f.AttributeValueGet(n, "")); lines.Add((f, v)); }
int changed = 0;
foreach (var a in nw.Attributes) {
  var name = (string)a.GetType().GetProperty("Name").GetValue(a); if (!tAttrs.TryGetValue(name, out var src)) continue;
  foreach (var pn in copy) { var p = a.GetType().GetProperty(pn); if (p == null || !p.CanWrite) continue; var val = p.GetValue(src); if (!Equals(p.GetValue(a), val)) { p.SetValue(a, val); changed++; } }
  if (name == "LEVEL") { var p = a.GetType().GetProperty("ValuesList"); var vl = (string)p.GetValue(a); if (!("|" + vl + "|").Contains("|6400|")) p.SetValue(a, vl + "|6400"); }
}
foreach (var l in lines) foreach (var kv in l.v) if (kv.Value != "") l.f.AttributeValueSet(kv.Key, kv.Value);
App.Redraw(true, false);
Out.AppendLine($"Copied {changed} field settings onto {nw.Name}; re-applied values on {lines.Count} lines. File not saved.");
foreach (var a in nw.Attributes) { var t = a.GetType(); Out.AppendLine($"  {t.GetProperty("Name").GetValue(a),-14} group '{t.GetProperty("Group").GetValue(a)}' type {t.GetProperty("TypeAsString").GetValue(a)} list '{t.GetProperty("ValuesList").GetValue(a)}' required {t.GetProperty("Required").GetValue(a)} show {t.GetProperty("DisplayInProperties").GetValue(a)}"); }
return null;
