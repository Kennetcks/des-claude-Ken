// MPA 005: build one floor centreline per drive from the solids on 0\SKETCH LAYER\MPA TEST, on a NEW layer
// 0\SKETCH LAYER\MPA TEST\CENTRELINES, with the Design Master centreline attribute fields and values.
// Nothing existing is changed. Stops if the target layer already exists. Nothing is saved.
var target = "0\\SKETCH LAYER\\MPA TEST\\CENTRELINES";
if (App.Layers.Exists(target)) { Out.AppendLine("Layer " + target + " already exists - stopping, nothing changed."); return null; }

// Drives: solids in mining order (handles), from the grouping checked on 16/10/2026.
var drives = new (string act, string name, string desc, string profile, string gss, string[] solids)[] {
  ("DEC", "MAIN DEC", "Decline", "5.5mW x 5.7mH SA", "R 2.0", new[] { "44B8DA9","44B8DA8","44B8D87","44B8D88","44B8D89","44B8D8A","44B8D8B","44B8D8C","44B8D8D","44B8D8E","44B8D8F","44B8D90","44B8D91","44B8D92","44B8D94","44B8D93","44B8D95","44B8D96","44B8D97","44B8D9F","44B8DA0","44B8DA2","44B8DA1","44B8DA3","44B8DA4" }),
  ("DEC", "MAIN DEC", "Decline", "5.5mW x 5.7mH SA", "R 2.0", new[] { "44B8DAA","44B8DAB","44B8DAC","44B8DAD","44B8DAE","44B8DB4","44B8DB5","44B8DB6","44B8DB7","44B8DB8","44B8DB9","44B8DBA","44B8DBB","44B8DBC","44B8DBD","44B8DBE","44B8DBF","44B8DC0","44B8DC1","44B8DC2","44B8DC3","44B8DC4","44B8DC5","44B8DC6","44B8DC7","44B8DC8","44B8DC9" }),
  ("DSP", "G6400 DSP", "Decline Stockpile", "5.5mW x 5.7mH SA", "R 2.0", new[] { "44B8DAF","44B8DB0","44B8DB1","44B8DB2","44B8DB3" }),
  ("ACC", "G6400 ACC", "Access", "5.0mW x 5.0mH SA", "R 3.0", new[] { "44B8D98","44B8D99","44B8D9A","44B8D9B","44B8D9C","44B8D9D","44B8D9E" }),
  ("SMP", "G6400 SMP", "Sump", "4.5mW x 4.5mH SA", "R 4.0", new[] { "44B8DA5","44B8DA6","44B8DA7" }) };
var common = new Dictionary<string, string> { ["MINE"] = "W", ["LEVEL"] = "6400", ["Design Status"] = "Approved", ["MPA"] = "005", ["Control"] = "Laser",
  ["INT_CABLES"] = "0", ["LODE"] = "-", ["LAT_VERT"] = "LAT", ["COST_CENTRE"] = "C", ["RECOVERY"] = "1.0", ["SCHEDULED"] = "1", ["SLOT_FLG"] = "0",
  ["SLOT_TYPE"] = "-", ["STO_DEV"] = "DEV", ["DILUTION"] = "0", ["STOPE TYPE"] = "-" };

// Solids on MPA TEST and their plan centres (centre of the vertex extents in the solid's own principal frame).
var sol = new Dictionary<string, (Deswik.Graphics.Primaries.Figure f, Deswik.Graphics.Figures.Polyface pf, double x, double y)>();
foreach (var e in App.ModelSpace.Entities) {
  var f = e as Deswik.Graphics.Primaries.Figure; if (f == null || f.Layer == null || !f.Layer.Name.EndsWith("MPA TEST") || !f.IsPolyface) continue;
  var pf = f.asPolyface; var pts = new List<(double x, double y)>();
  foreach (var v in (System.Collections.IEnumerable)pf.GetType().GetProperty("VertexList").GetValue(pf)) { var t = v.GetType(); pts.Add((System.Convert.ToDouble(t.GetProperty("x").GetValue(v)), System.Convert.ToDouble(t.GetProperty("y").GetValue(v)))); }
  double mx = pts.Average(p => p.x), my = pts.Average(p => p.y);
  double sxx = pts.Sum(p => (p.x - mx) * (p.x - mx)), syy = pts.Sum(p => (p.y - my) * (p.y - my)), sxy = pts.Sum(p => (p.x - mx) * (p.y - my));
  double a = 0.5 * System.Math.Atan2(2 * sxy, sxx - syy), ux = System.Math.Cos(a), uy = System.Math.Sin(a);
  double p0 = pts.Min(p => (p.x - mx) * ux + (p.y - my) * uy), p1 = pts.Max(p => (p.x - mx) * ux + (p.y - my) * uy);
  double q0 = pts.Min(p => -(p.x - mx) * uy + (p.y - my) * ux), q1 = pts.Max(p => -(p.x - mx) * uy + (p.y - my) * ux);
  double pc = (p0 + p1) / 2, qc = (q0 + q1) / 2;
  sol[System.Convert.ToString(f.GetType().GetProperty("Handle")?.GetValue(f))] = (f, pf, mx + pc * ux - qc * uy, my + pc * uy + qc * ux);
}
// Floor = lowest GetMinZ of all MPA TEST solids at that XY (true floor at boolean-cut junctions).
double Floor(double x, double y) { double best = double.MaxValue;
  foreach (var s in sol.Values) { var bb = s.f.BoundingBox; if (x < bb.Min.x || x > bb.Max.x || y < bb.Min.y || y > bb.Max.y) continue; double z; try { z = s.pf.GetMinZ(x, y, bb); } catch { continue; } if (z < best) best = z; }
  return best; }
foreach (var d in drives) foreach (var h in d.solids) if (!sol.ContainsKey(h)) { Out.AppendLine("Solid " + h + " not found on MPA TEST - stopping, nothing changed."); return null; }

// New layer with the Design Master centreline attribute fields.
var tmpl = App.Layers.FindName("0\\DESIGN MASTER\\CENTRELINES", false);
var layer = App.Layers.Add(target, false, false, 0, false);
var at = layer.Attributes;
var addM = at.GetType().GetMethods().First(m => m.Name == "Add" && m.GetParameters().Length == 3 && m.GetParameters()[0].ParameterType == typeof(string));
object strT = System.Enum.Parse(addM.GetParameters()[1].ParameterType, "String");
var fields = new List<string>(); foreach (var a in tmpl.Attributes) { var an = a.GetType().GetProperty("Name")?.GetValue(a)?.ToString(); if (an != null) fields.Add(an); }
foreach (var fn in fields) addM.Invoke(at, new object[] { fn, strT, "" });

string Card(double dx, double dy) { double b = System.Math.Atan2(dx, dy) * 180 / System.Math.PI; if (b < 0) b += 360; return b < 45 || b >= 315 ? "N" : b < 135 ? "E" : b < 225 ? "S" : "W"; }
int k = 0;
foreach (var d in drives) {
  k++; var c = d.solids.Select(h => sol[h]).ToList(); var pts = new List<(double x, double y, double z)>();
  // start: back from the first centre by half a cut, 0.4 m inside the end face
  double sx = c[1].x - c[0].x, sy = c[1].y - c[0].y, sl = System.Math.Sqrt(sx * sx + sy * sy);
  double fx = c[0].x - (sx / sl) * (sl / 2 - 0.4), fy = c[0].y - (sy / sl) * (sl / 2 - 0.4); pts.Add((fx, fy, Floor(fx, fy)));
  for (int i = 0; i < c.Count - 1; i++) {   // joints between cuts: XY midway, floor = average of the two cut-centre floors
    double jx = (c[i].x + c[i + 1].x) / 2, jy = (c[i].y + c[i + 1].y) / 2; pts.Add((jx, jy, (Floor(c[i].x, c[i].y) + Floor(c[i + 1].x, c[i + 1].y)) / 2)); }
  int n = c.Count; double ex = c[n - 1].x - c[n - 2].x, ey = c[n - 1].y - c[n - 2].y, el = System.Math.Sqrt(ex * ex + ey * ey);
  double lx = c[n - 1].x + (ex / el) * (el / 2 - 0.4), ly = c[n - 1].y + (ey / el) * (el / 2 - 0.4); pts.Add((lx, ly, Floor(lx, ly)));
  if (pts.Any(p => p.z > 1e6)) { Out.AppendLine($"drive {k} {d.name}: a floor point fell outside the solids - skipped"); continue; }
  var pl = new Deswik.Graphics.Figures.Polyline(); foreach (var p in pts) pl.AddVertex(p.x, p.y, p.z);
  pl.Layer = layer; App.ModelSpace.Entities.AddItem(pl, null, null, false, false);
  var vals = new Dictionary<string, string>(common) { ["ACT_TYPE"] = d.act, ["Name"] = d.name, ["Description"] = d.desc, ["PROFILE"] = d.profile, ["GSS"] = d.gss,
    ["DIRECTION"] = Card(pts[pts.Count - 1].x - pts[0].x, pts[pts.Count - 1].y - pts[0].y) };
  foreach (var kv in vals) pl.AttributeValueSet(kv.Key, kv.Value);
  Out.AppendLine($"drive {k}: {d.act} '{d.name}' {d.solids.Length} solids, {pts.Count} points, length {pl.Length:F1} m, RL {pts[0].z:F1} -> {pts[pts.Count - 1].z:F1}, DIRECTION {vals["DIRECTION"]}, handle {pl.GetType().GetProperty("Handle")?.GetValue(pl)}");
}
App.Redraw(true, false);
Out.AppendLine($"Created layer {target} with {fields.Count} attribute fields. Undo: delete the layer {target}. File not saved.");
return null;
