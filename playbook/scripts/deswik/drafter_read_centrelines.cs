if (!App.Filename.EndsWith("Level Design Drafter.vdcl")) { Out.AppendLine("WRONG WINDOW " + System.IO.Path.GetFileName(App.Filename)); return null; }
// READ ONLY: list the strings on the active layer (0\DESIGN DRAFT\CENTERLINES) with segments, grades and attributes.
var lay = App.ActiveLayer; Out.AppendLine("Active layer: " + lay.Name);
var fields = new List<string>(); foreach (var a in lay.Attributes) { var an = a.GetType().GetProperty("Name")?.GetValue(a)?.ToString(); if (an != null) fields.Add(an); }
Out.AppendLine("Fields: " + string.Join(", ", fields));
foreach (var e in App.ModelSpace.Entities) {
  var f = e as Deswik.Graphics.Primaries.Figure; if (f == null || f.Deleted || f.Layer == null || f.Layer.Name != lay.Name) continue;
  var h = $"{f.GetType().GetProperty("Handle")?.GetValue(f)}";
  Out.AppendLine($"--- {h} {f.GetType().Name} selected={f.GetType().GetProperty("Selected")?.GetValue(f)}");
  object geo = f.IsPolyline ? (object)f.asPolyline : f; var vl = geo.GetType().GetProperty("VertexList")?.GetValue(geo) as System.Collections.IEnumerable; if (vl == null) Out.AppendLine("  (no VertexList; IsPolyline=" + f.IsPolyline + ")");
  if (vl != null) { var pts = new List<(double x, double y, double z)>();
    foreach (var v in vl) { var t = v.GetType(); pts.Add((System.Convert.ToDouble(t.GetProperty("x").GetValue(v)), System.Convert.ToDouble(t.GetProperty("y").GetValue(v)), System.Convert.ToDouble(t.GetProperty("z").GetValue(v)))); }
    double cum = 0;
    for (int i = 0; i < pts.Count; i++) { string seg = "";
      if (i > 0) { double dx = pts[i].x - pts[i - 1].x, dy = pts[i].y - pts[i - 1].y, dz = pts[i].z - pts[i - 1].z, hl = System.Math.Sqrt(dx * dx + dy * dy); cum += hl;
        seg = $" seg {hl:F2} m, dz {dz:F2}, grade {(hl > 0 ? (dz / hl * 100) : 0):F2}% (1:{(dz != 0 ? hl / System.Math.Abs(dz) : 0):F1}), chain {cum:F2}"; }
      Out.AppendLine($"  v{i} {pts[i].x:F2} {pts[i].y:F2} {pts[i].z:F2}{seg}"); } }
  Out.AppendLine("  " + string.Join(" | ", new[] { "ACT_TYPE", "Name", "PROFILE", "GSS", "Description", "Design Status" }.Select(fn => fn + "=" + System.Convert.ToString(f.AttributeValueGet(fn, "")))));
}
return null;
