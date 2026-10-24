if (!App.Filename.EndsWith("Level Design Drafter.vdcl")) { Out.AppendLine("WRONG WINDOW " + System.IO.Path.GetFileName(App.Filename)); return null; }
// Shorten FAN 44BD9CB to 8.5 m centred in the 15 m flat (flat ch 30.0-45.0 -> FAN ch 33.25-41.75).
// The two flat leftovers (3.25 m each side) go back to the RAD strings: 44BD9CA gets ch 30.0-33.25, 44BD9CC gets ch 41.75-45.0.
// New points A and B lie on the existing flat segments at RL 6418.65, so no grade changes. File not saved.
Deswik.Graphics.Primaries.Figure Get(string h) { foreach (var e in App.ModelSpace.Entities) { var f = e as Deswik.Graphics.Primaries.Figure; if (f != null && !f.Deleted && f.IsPolyline && $"{f.GetType().GetProperty("Handle")?.GetValue(f)}" == h) return f; } return null; }
var radA = Get("44BD9CA"); var fan = Get("44BD9CB"); var radB = Get("44BD9CC");
if (radA == null || fan == null || radB == null) { Out.AppendLine("one of 44BD9CA / 44BD9CB / 44BD9CC not found - nothing changed"); return null; }
Deswik.Graphics.Geometry.Verticies V(Deswik.Graphics.Primaries.Figure f) { var p = f.asPolyline; return (Deswik.Graphics.Geometry.Verticies)p.GetType().GetProperty("VertexList").GetValue(p); }
List<(double x, double y, double z)> P(Deswik.Graphics.Primaries.Figure f) { var l = new List<(double, double, double)>(); foreach (var v in (System.Collections.IEnumerable)V(f)) { var t = v.GetType(); l.Add((System.Convert.ToDouble(t.GetProperty("x").GetValue(v)), System.Convert.ToDouble(t.GetProperty("y").GetValue(v)), System.Convert.ToDouble(t.GetProperty("z").GetValue(v)))); } return l; }
var fp = P(fan);
if (fp.Count != 5 || fp.Any(q => System.Math.Abs(q.z - fp[0].z) > 0.005) || System.Convert.ToString(fan.AttributeValueGet("ACT_TYPE", "")) != "FAN") { Out.AppendLine("44BD9CB is not the 5-vertex flat FAN string any more - nothing changed"); return null; }
// chainage along the flat string (2D)
var ch = new double[fp.Count]; for (int i = 1; i < fp.Count; i++) ch[i] = ch[i - 1] + System.Math.Sqrt(System.Math.Pow(fp[i].x - fp[i - 1].x, 2) + System.Math.Pow(fp[i].y - fp[i - 1].y, 2));
double total = ch[fp.Count - 1], s0 = (total - 8.5) / 2, s1 = s0 + 8.5;
(double x, double y, double z, int seg) At(double s) { for (int i = 1; i < fp.Count; i++) if (s <= ch[i] + 1e-9) { double t = (s - ch[i - 1]) / (ch[i] - ch[i - 1]); return (fp[i - 1].x + t * (fp[i].x - fp[i - 1].x), fp[i - 1].y + t * (fp[i].y - fp[i - 1].y), fp[i - 1].z, i); } return (fp[^1].x, fp[^1].y, fp[^1].z, fp.Count - 1); }
var A = At(s0); var B = At(s1);
Deswik.Graphics.Geometry.Point Pt((double x, double y, double z, int seg) q) => new Deswik.Graphics.Geometry.Point(q.x, q.y, q.z);
Deswik.Graphics.Geometry.Point Pt3((double x, double y, double z) q) => new Deswik.Graphics.Geometry.Point(q.x, q.y, q.z);
// RAD before: append the flat vertices up to A
var va = V(radA); for (int i = 1; i < A.seg; i++) va.InsertAt(va.Count, Pt3(fp[i])); va.InsertAt(va.Count, Pt(A));
// RAD after: prepend B and the flat vertices after it
var vb = V(radB); var after = new List<Deswik.Graphics.Geometry.Point> { Pt(B) }; for (int i = B.seg; i < fp.Count - 1; i++) after.Add(Pt3(fp[i]));
for (int i = after.Count - 1; i >= 0; i--) vb.InsertAt(0, after[i]);
// FAN: A, the flat vertices between A and B, B
var vf = V(fan); var mid = new List<Deswik.Graphics.Geometry.Point> { Pt(A) }; for (int i = A.seg; i < B.seg; i++) mid.Add(Pt3(fp[i])); mid.Add(Pt(B));
vf.RemoveAll(); foreach (var q in mid) vf.InsertAt(vf.Count, q);
foreach (var f in new[] { radA, fan, radB }) try { f.asPolyline.GetType().GetMethod("Regen", System.Type.EmptyTypes)?.Invoke(f.asPolyline, null); } catch { }
App.Redraw(true, false);
Out.AppendLine($"flat {total:F2} m: FAN now ch {s0:F2}-{s1:F2} of the flat");
foreach (var f in new[] { radA, fan, radB }) { var q = P(f); Out.AppendLine($"{f.GetType().GetProperty("Handle")?.GetValue(f)} {f.AttributeValueGet("ACT_TYPE", "")}: {q.Count} vertices, length {f.asPolyline.Length:F2} m, RL {q[0].z:F2} -> {q[^1].z:F2}"); }
Out.AppendLine("Undo: Deswik undo. File not saved.");
return null;
