if (!App.Filename.EndsWith("Level Design Drafter.vdcl")) { Out.AppendLine("WRONG WINDOW " + System.IO.Path.GetFileName(App.Filename)); return null; }
// Split RAD centreline 44BD9CA at its existing vertices v7 (ch 30.0) and v11 (ch 45.0) so the 15 m flat section
// (RL 6418.65) is its own string with fan chamber attributes (fan chamber GA drawing: 10.5 m W x 6.5 m H + 0.3 m concrete base).
// No vertex is moved or added, so every grade stays as it is. Same layer. File not saved.
Deswik.Graphics.Primaries.Figure src = null;
foreach (var e in App.ModelSpace.Entities) { var f = e as Deswik.Graphics.Primaries.Figure; if (f == null || f.Deleted || f.Layer == null) continue;
  if ($"{f.GetType().GetProperty("Handle")?.GetValue(f)}" == "44BD9CA") src = f; }
if (src == null || !src.IsPolyline) { Out.AppendLine("44BD9CA not found or not a polyline - nothing changed"); return null; }
var pl = src.asPolyline; var vl = pl.GetType().GetProperty("VertexList").GetValue(pl);
var pts = new List<(double x, double y, double z)>();
foreach (var v in (System.Collections.IEnumerable)vl) { var t = v.GetType(); pts.Add((System.Convert.ToDouble(t.GetProperty("x").GetValue(v)), System.Convert.ToDouble(t.GetProperty("y").GetValue(v)), System.Convert.ToDouble(t.GetProperty("z").GetValue(v)))); }
// safety checks: 73 vertices, v7..v11 flat at 6418.65
if (pts.Count != 73 || System.Math.Abs(pts[7].z - 6418.65) > 0.02 || System.Math.Abs(pts[11].z - 6418.65) > 0.02) { Out.AppendLine($"44BD9CA has changed ({pts.Count} vertices, v7 z {pts[7].z:F2}) - nothing changed"); return null; }
var verts = (Deswik.Graphics.Geometry.Verticies)vl;
// pieces are clones of 44BD9CA (same layer, colour, line type and attributes), trimmed to their vertex range
Deswik.Graphics.Primaries.Figure Piece(int a, int b) { var n = (Deswik.Graphics.Primaries.Figure)src.Clone(); var np = n.asPolyline; var nv = (Deswik.Graphics.Geometry.Verticies)np.GetType().GetProperty("VertexList").GetValue(np);
  if (b < pts.Count - 1) nv.RemoveAfter(b); if (a > 0) nv.RemoveBefore(a); n.Layer = src.Layer; n.AttributesCopyFrom(src); return n; }
var fan = Piece(7, 11); var rest = Piece(11, 72);
int Cnt(object o) { var p = o is Deswik.Graphics.Primaries.Figure fg && fg.IsPolyline ? (object)fg.asPolyline : o; return ((System.Collections.IEnumerable)p.GetType().GetProperty("VertexList").GetValue(p)).Cast<object>().Count(); }
double Z0(Deswik.Graphics.Primaries.Figure fg) { var p = fg.asPolyline; var v = ((System.Collections.IEnumerable)p.GetType().GetProperty("VertexList").GetValue(p)).Cast<object>().First(); return System.Convert.ToDouble(v.GetType().GetProperty("z").GetValue(v)); }
if (Cnt(fan) != 5 || Cnt(rest) != 62 || System.Math.Abs(Z0(fan) - pts[7].z) > 0.001 || System.Math.Abs(Z0(rest) - pts[11].z) > 0.001) { Out.AppendLine($"piece check failed (fan {Cnt(fan)} verts, rest {Cnt(rest)} verts) - nothing changed"); return null; }
App.ModelSpace.Entities.AddItem(fan, null, null, false, false); App.ModelSpace.Entities.AddItem(rest, null, null, false, false);
var fanVals = new Dictionary<string, string> { ["ACT_TYPE"] = "FAN", ["Description"] = "Fan Chamber (from the GA drawing)", ["PROFILE"] = "10.5mW x 6.8mH SA" };
foreach (var kv in fanVals) fan.AttributeValueSet(kv.Key, kv.Value);
verts.RemoveAfter(7);   // original keeps v0..v7 (ch 0-30 m) and all its attributes
var list = new int[Cnt(pl)];
try { pl.GetType().GetMethod("Regen", System.Type.EmptyTypes)?.Invoke(pl, null); fan.asPolyline.GetType().GetMethod("Regen", System.Type.EmptyTypes)?.Invoke(fan.asPolyline, null); rest.asPolyline.GetType().GetMethod("Regen", System.Type.EmptyTypes)?.Invoke(rest.asPolyline, null); } catch { }App.Redraw(true, false);
string H(object o) => $"{o.GetType().GetProperty("Handle")?.GetValue(o)}";
Out.AppendLine($"44BD9CA (RAD, ch 0-30.0 m): {list.Length} vertices, length {pl.Length:F2} m, RL {pts[0].z:F2} -> {pts[7].z:F2}");
Out.AppendLine($"{H(fan)} (FAN, ch 30.0-45.0 m): {Cnt(fan)} vertices, length {fan.asPolyline.Length:F2} m, RL {pts[7].z:F2} -> {pts[11].z:F2}, PROFILE {fan.AttributeValueGet("PROFILE", "")}");
Out.AppendLine($"{H(rest)} (RAD, ch 45.0-240.6 m): {Cnt(rest)} vertices, length {rest.asPolyline.Length:F2} m, RL {pts[11].z:F2} -> {pts[72].z:F2}");
Out.AppendLine("Undo: Deswik undo, or delete the two new strings and re-join. File not saved.");
return null;
