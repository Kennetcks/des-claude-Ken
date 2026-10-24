if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW " + System.IO.Path.GetFileName(App.Filename)); return null; }
// Scheduler attributes for the 9 strings on the active FINAL layer (...\ENGINEER STRINGS\EWY\SOLIDS\FINAL).
// Values copied from the matching scheduler-ready strings on STRINGS ADD - COLLEAGUE PROPOSE; types confirmed by the engineer 17/10/2026.
// Geometry is not touched. Every change is listed (old -> new). File not saved.
var al = App.ActiveLayer;
// common to every string (as on the colleague strings)
var common = new Dictionary<string, string> {
  ["STO_DEV"] = "DEV", ["STOPE TYPE"] = "-", ["Status"] = "Approved", ["STAGE"] = "0", ["SCHEDULED"] = "1", ["COST_CENTRE"] = "C",
  ["DILUTION"] = "0", ["RECOVERY"] = "1.0", ["MINE"] = "W", ["LODE"] = "-", ["PROFILE"] = "4.5mW x 4.5mH SA", ["GSS"] = "R 1.0",
  ["Control"] = "Laser", ["Design Status"] = "Conceptual", ["_AREA"] = "0", ["_LENGTH"] = "0", ["EASTING"] = "0", ["NORTHING"] = "0",
  ["Stope Height"] = "0", ["Stope Length"] = "0", ["Stope Thickness"] = "0", ["AU"] = "0", ["Metres"] = "0", ["Insitu Tonnes"] = "0",
  ["Insitu Volume"] = "0", ["RESCAT"] = "0" };
Dictionary<string, string> T(string act, string level, string desc, string dir, string latvert, string zone = "", string slot = "-", string slotflg = "0") =>
  new Dictionary<string, string> { ["ACT_TYPE"] = act, ["LEVEL"] = level, ["Description"] = desc, ["DIRECTION"] = dir, ["LAT_VERT"] = latvert,
    ["ZONE"] = zone, ["SLOT_TYPE"] = slot, ["SLOT_FLG"] = slotflg };
var plan = new Dictionary<string, Dictionary<string, string>> {
  ["44B8DD1"] = T("EWR", "368", "Escapeway Rise", "-", "VERT", "NT"),            // rise 368 -> 390 (as colleague 44B8CEC)
  ["44B8DD5"] = T("RAD", "390", "Return Air Drive", "S", "LAT"),                 // as colleague 44B8CF0 / 44B8D9C
  ["44B8DD7"] = T("EWD", "390", "Escapeway Drive", "W", "LAT"),                  // as colleague 44B8CED / 44B8CEE
  ["44B8DE9"] = T("RAD", "390", "Return Air Drive", "W", "LAT"),                 // R25 loop 390 -> 422 (DIRECTION as colleague 44B8CF4)
  ["44B8DEC"] = T("EWD", "368", "Escapeway Drive", "W", "LAT"),
  ["44B8DED"] = T("EWR", "346", "Escapeway Rise", "-", "VERT", "NT"),            // rise 346 -> 368
  ["44B8DEE"] = T("EWD", "346", "Escapeway Drive", "W", "LAT"),
  ["44B8DEF"] = T("RAR", "368", "Return Air Rise", "-", "VERT", "NT", "RHINO", "1"), // as colleague 44B8CF1
  ["44B8DF9"] = T("EWD", "368", "Escapeway Drive", "W", "LAT") };                // as colleague 44B8D8D
var fields = new HashSet<string>(); foreach (var a in al.Attributes) fields.Add((string)a.GetType().GetProperty("Name").GetValue(a));
var missing = common.Keys.Concat(plan.First().Value.Keys).Where(k => !fields.Contains(k)).ToList();
if (missing.Count > 0) { Out.AppendLine("Missing fields on the layer, nothing changed: " + string.Join(", ", missing)); return null; }
var found = new HashSet<string>(); int n = 0;
foreach (var e in App.ModelSpace.Entities) { var f = e as Deswik.Graphics.Primaries.Figure; if (f == null || f.Layer == null || f.Deleted || f.Layer.Name != al.Name) continue;
  var h = $"{f.GetType().GetProperty("Handle")?.GetValue(f)}";
  if (!plan.TryGetValue(h, out var own)) { Out.AppendLine($"{h}: not in the list - left alone"); continue; }
  found.Add(h); var ch = new List<string>();
  foreach (var kv in common.Concat(own)) { var old = System.Convert.ToString(f.AttributeValueGet(kv.Key, ""));
    if (old == kv.Value) continue; f.AttributeValueSet(kv.Key, kv.Value); n++; ch.Add($"{kv.Key} '{old}'->'{System.Convert.ToString(f.AttributeValueGet(kv.Key, ""))}'"); }
  Out.AppendLine($"{h} {own["ACT_TYPE"]} L{own["LEVEL"]}: {(ch.Count == 0 ? "already correct" : string.Join("; ", ch))}"); }
foreach (var h in plan.Keys.Where(k => !found.Contains(k))) Out.AppendLine($"{h}: NOT FOUND on the layer");
App.Redraw(true, false);
Out.AppendLine($"{n} values set on {found.Count} strings. File not saved.");
return null;
