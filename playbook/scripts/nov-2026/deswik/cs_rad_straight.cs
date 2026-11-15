// W6390 RAD straightened: keep start v0 and grade break v1 (both on the square take-off line), v2 = v1 + same flat length on
// the same bearing, flat. Walls: v2/v5 at the grade break and v3/v4 at the end rebuilt 2.5 m each side, square to the drive.
const bool APPLY = APPLY_FLAG;
var H = new Dictionary<string, Deswik.Graphics.Primaries.Figure>(StringComparer.OrdinalIgnoreCase); foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (!f.Deleted) H[f.Handle] = f;
foreach (var h in new[] { "4588F64", "4588F5A" }) if (!H.ContainsKey(h)) { Out.AppendLine(h + " missing - nothing done"); return null; }
List<double[]> P3(string h) => Enumerable.Range(0, H[h].asPolyline.VertexCount).Select(i => { var q = H[h].asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
double D2(double[] a, double[] b) => Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2));
var c = P3("4588F64"); var w = P3("4588F5A");
double ux = c[1][0] - c[0][0], uy = c[1][1] - c[0][1], l = Math.Sqrt(ux * ux + uy * uy); ux /= l; uy /= l; double Lf = D2(c[1], c[2]);
var v2 = new[] { c[1][0] + Lf * ux, c[1][1] + Lf * uy, c[1][2] };
double side(double[] q) => (q[0] - c[0][0]) * uy - (q[1] - c[0][1]) * ux;
double[] P(double[] o, double s, double z) => new[] { o[0] + s * uy, o[1] - s * ux, z };
var nw = w.Select(q => q).ToList(); double s2 = Math.Sign(side(w[2]));
nw[2] = P(c[1], 2.5 * s2, c[1][2]); nw[3] = P(v2, 2.5 * s2, v2[2]); nw[4] = P(v2, -2.5 * s2, v2[2]); nw[5] = P(c[1], -2.5 * s2, c[1][2]);
Out.AppendLine($"RAD: bend {Math.Atan2(c[2][0] - c[1][0], c[2][1] - c[1][1]) * 180 / Math.PI - Math.Atan2(ux, uy) * 180 / Math.PI:F2} deg removed; end moves {D2(c[2], v2):F2} m; flat {Lf:F2} m; total {l + Lf:F2} m");
Out.AppendLine("wall moves: " + string.Join(" ", w.Select((q, i) => $"v{i} {D2(q, nw[i]):F2}")) + $"; widths break {D2(nw[2], nw[5]):F2} end {D2(nw[3], nw[4]):F2}; fillet pts to break {D2(nw[1], nw[2]):F2}/{D2(nw[6], nw[5]):F2}");
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }
var log = new System.Text.StringBuilder(); foreach (var h in new[] { "4588F64", "4588F5A" }) { log.AppendLine("== " + h); foreach (var q in P3(h)) log.AppendLine($"{q[0]:R},{q[1]:R},{q[2]:R}"); }
System.IO.File.WriteAllText(@"C:\dswtmp\rad_straight_backup.txt", log.ToString());
Deswik.Graphics.Primaries.Figure Replace(Deswik.Graphics.Primaries.Figure src, List<double[]> p) { var k = (Deswik.Graphics.Primaries.Figure)src.Clone(); var pl = k.asPolyline; bool cl = pl.Closed; while (pl.VertexCount > 0) pl.RemoveVertexAt(0); foreach (var q in p) pl.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]); pl.Closed = cl; App.ModelSpace.Entities.AddItem(k, null, null, true, false); src.Deleted = true; return k; }
var a = Replace(H["4588F64"], new List<double[]> { c[0], c[1], v2 }); var b = Replace(H["4588F5A"], nw); App.Redraw(true);
Out.AppendLine($"applied: RAD CL {a.Handle}, RAD wall {b.Handle}; backup C:\\dswtmp\\rad_straight_backup.txt"); return null;