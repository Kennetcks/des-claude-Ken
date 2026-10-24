if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW " + System.IO.Path.GetFileName(App.Filename)); return null; }
// R16-R20 options for the tight bend on 44B8DE3 (active FINAL layer). 44B8DE3 itself is NOT changed.
// Each option: v0..vA kept, true arc tangent to the bottom straight (v0-v24) and the NE straight (v37-v40), then vB..v81 kept.
// Start and end points kept (x, y, RL). Heights: v0 -> v55 regraded uniformly by chainage (RL at v0 and v55 unchanged); v55..v81 unchanged.
// New lines go on the active layer. File not saved.
var al = App.ActiveLayer;
Deswik.Graphics.Figures.Polyline src = null;
// after a layer reload entities come back as generic Figure wrappers - use asPolyline
foreach (var e in App.ModelSpace.Entities) { var f = e as Deswik.Graphics.Primaries.Figure; if (f == null || f.Layer == null || f.Layer.Name != al.Name) continue;
  if ($"{f.GetType().GetProperty("Handle")?.GetValue(f)}" == "44B8DE3") { src = (e as Deswik.Graphics.Figures.Polyline) ?? f.asPolyline; break; } }
if (src == null) { Out.AppendLine("44B8DE3 not found on " + al.Name); return null; }
double G(object v, string c) { var t = v.GetType(); var p = t.GetProperty(c) ?? t.GetProperty(c.ToLower()); if (p != null) return System.Convert.ToDouble(p.GetValue(v));
  var fi = t.GetField(c) ?? t.GetField(c.ToLower()); return System.Convert.ToDouble(fi.GetValue(v)); }
var P = new List<double[]>(); foreach (var v in src.VertexList) P.Add(new[] { G(v, "X"), G(v, "Y"), G(v, "Z") });
if (P.Count != 82) { Out.AppendLine($"44B8DE3 has {P.Count} points, expected 82 - stopping"); return null; }
double L2(double[] a, double[] b) => Math.Sqrt((a[0] - b[0]) * (a[0] - b[0]) + (a[1] - b[1]) * (a[1] - b[1]));
double ux = P[24][0] - P[0][0], uy = P[24][1] - P[0][1], l = Math.Sqrt(ux * ux + uy * uy); ux /= l; uy /= l;
double wx = P[40][0] - P[37][0], wy = P[40][1] - P[37][1]; l = Math.Sqrt(wx * wx + wy * wy); wx /= l; wy /= l;
double den = ux * (-wy) - uy * (-wx), s = ((P[37][0] - P[0][0]) * (-wy) - (P[37][1] - P[0][1]) * (-wx)) / den;
double Ix = P[0][0] + s * ux, Iy = P[0][1] + s * uy, th = Math.Acos(ux * wx + uy * wy);
Out.AppendLine($"bend: deflection {th * 180 / Math.PI:F1} deg, corner {Ix:F2},{Iy:F2}");
foreach (var R in new[] { 16, 17, 18, 19, 20 }) {
  double T = R * Math.Tan(th / 2);
  int kA = 0; for (int k = 0; k <= 24; k++) if ((P[k][0] - P[0][0]) * ux + (P[k][1] - P[0][1]) * uy < s - T - 0.5) kA = k;
  int kB = -1; for (int k = 37; k <= 41 && kB < 0; k++) if ((P[k][0] - Ix) * wx + (P[k][1] - Iy) * wy > T + 0.5) kB = k;
  if (kB < 0) { Out.AppendLine($"R{R}: tangent runs past v41 - skipped"); continue; }
  double cx = Ix - T * ux + R * uy, cy = Iy - T * uy - R * ux;   // centre on the inside (clockwise turn)
  double a0 = Math.Atan2(Iy - T * uy - cy, Ix - T * ux - cx); int n = (int)Math.Ceiling(R * th / 2.5);
  var Q = new List<double[]>(); for (int k = 0; k <= kA; k++) Q.Add((double[])P[k].Clone());
  for (int i = 0; i <= n; i++) { double a = a0 - th * i / n; Q.Add(new[] { cx + R * Math.Cos(a), cy + R * Math.Sin(a), 0.0 }); }
  int i55 = Q.Count + (55 - kB); for (int k = kB; k < P.Count; k++) Q.Add((double[])P[k].Clone());
  // heights: uniform grade from v0 to v55
  double tot = 0; for (int i = 1; i <= i55; i++) tot += L2(Q[i - 1], Q[i]);
  double c = 0, z0 = P[0][2], dz = P[55][2] - P[0][2];
  for (int i = 1; i < i55; i++) { c += L2(Q[i - 1], Q[i]); Q[i][2] = z0 + dz * c / tot; }
  var pl = new Deswik.Graphics.Figures.Polyline(); foreach (var q in Q) pl.AddVertex(q[0], q[1], q[2]);
  pl.Layer = al; App.ModelSpace.Entities.AddItem(pl, null, null, false, false);
  // checks: min 3-point radius over the bend, max grade start->v55, end points
  double minR = 1e9; for (int i = Math.Max(1, kA - 2); i < Math.Min(Q.Count - 1, i55); i++) {
    double a = L2(Q[i - 1], Q[i]), b = L2(Q[i], Q[i + 1]), d = L2(Q[i - 1], Q[i + 1]);
    double ar = Math.Abs((Q[i][0] - Q[i - 1][0]) * (Q[i + 1][1] - Q[i - 1][1]) - (Q[i][1] - Q[i - 1][1]) * (Q[i + 1][0] - Q[i - 1][0])) / 2;
    if (ar > 1e-6) minR = Math.Min(minR, a * b * d / (4 * ar)); }
  double maxG = 0; for (int i = 1; i <= i55; i++) { var h = L2(Q[i - 1], Q[i]); if (h > 0.01) maxG = Math.Max(maxG, Math.Abs(Q[i][2] - Q[i - 1][2]) / h); }
  var m = Q[kA + 1 + n / 2];
  Out.AppendLine($"R{R}: handle {pl.GetType().GetProperty("Handle")?.GetValue(pl)}, {Q.Count} pts | min radius {minR:F1} m | start->v55 1:{1 / maxG:F2} | arc mid {m[0]:F2},{m[1]:F2} (v30 was {P[30][0]:F2},{P[30][1]:F2}) | start {Q[0][0]:F2},{Q[0][1]:F2},{Q[0][2]:F2} end {Q[^1][0]:F2},{Q[^1][1]:F2},{Q[^1][2]:F2}");
}
App.Regen(true); App.Redraw(true, false);
Out.AppendLine("Done. 44B8DE3 unchanged. File not saved.");
return null;
