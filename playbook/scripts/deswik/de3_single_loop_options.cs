if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW " + System.IO.Path.GetFileName(App.Filename)); return null; }
// Single-loop options for 44B8DE3 (active FINAL layer), one uniform grade start -> end. 44B8DE3 itself is NOT changed.
// Each option: start leg kept on its current line (bearing ~279.7 from the start), one circular loop of radius R further north,
// then a straight at bearing B into the fixed end point. Start and end x, y, RL unchanged. New lines on the active layer. File not saved.
var opts = new[] { (B: 126.0, R: 25.0), (B: 128.0, R: 26.0), (B: 122.0, R: 22.0) };
var al = App.ActiveLayer;
Deswik.Graphics.Figures.Polyline src = null;
foreach (var e in App.ModelSpace.Entities) { var f = e as Deswik.Graphics.Primaries.Figure; if (f == null || f.Layer == null || f.Layer.Name != al.Name) continue;
  if ($"{f.GetType().GetProperty("Handle")?.GetValue(f)}" == "44B8DE3") { src = (e as Deswik.Graphics.Figures.Polyline) ?? f.asPolyline; break; } }
if (src == null) { Out.AppendLine("44B8DE3 not found on " + al.Name); return null; }
double G(object v, string c) { var t = v.GetType(); var p = t.GetProperty(c) ?? t.GetProperty(c.ToLower()); if (p != null) return System.Convert.ToDouble(p.GetValue(v));
  var fi = t.GetField(c) ?? t.GetField(c.ToLower()); return System.Convert.ToDouble(fi.GetValue(v)); }
var P = new List<double[]>(); foreach (var v in src.VertexList) P.Add(new[] { G(v, "X"), G(v, "Y"), G(v, "Z") });
if (P.Count != 82) { Out.AppendLine($"44B8DE3 has {P.Count} points, expected 82 - stopping"); return null; }
double L2(double[] a, double[] b) => Math.Sqrt((a[0] - b[0]) * (a[0] - b[0]) + (a[1] - b[1]) * (a[1] - b[1]));
double oldWest = P.Min(p => p[0]), oldNorth = P.Max(p => p[1]);
var S = P[0]; var E = P[81]; double dzT = E[2] - S[2];
double ax = P[24][0] - S[0], ay = P[24][1] - S[1], la = Math.Sqrt(ax * ax + ay * ay); ax /= la; ay /= la;
foreach (var o in opts) {
  double br = o.B * Math.PI / 180, qx = Math.Sin(br), qy = Math.Cos(br), R = o.R;
  // centre = right of the start leg by R and right of the end leg by R (clockwise loop)
  double p1x = S[0] + R * ay, p1y = S[1] - R * ax, p2x = E[0] + R * qy, p2y = E[1] - R * qx;
  double den = ax * (-qy) - ay * (-qx), sA = ((p2x - p1x) * (-qy) - (p2y - p1y) * (-qx)) / den;
  double cx = p1x + sA * ax, cy = p1y + sA * ay, sC = -((cx - E[0]) * qx + (cy - E[1]) * qy);
  if (sA < 5 || sC < 5) { Out.AppendLine($"B{o.B} R{R}: straights too short - skipped"); continue; }
  double t1x = S[0] + sA * ax, t1y = S[1] + sA * ay, t2x = E[0] - sC * qx, t2y = E[1] - sC * qy;
  double a0 = Math.Atan2(t1y - cy, t1x - cx), a1 = Math.Atan2(t2y - cy, t2x - cx);
  double sweep = a0 - a1; while (sweep <= 0) sweep += 2 * Math.PI;   // clockwise sweep
  var Q = new List<double[]>(); Q.Add(new[] { S[0], S[1], 0.0 });
  int nA = (int)Math.Ceiling(sA / 5); for (int i = 1; i < nA; i++) Q.Add(new[] { S[0] + sA * ax * i / nA, S[1] + sA * ay * i / nA, 0.0 });
  int n = (int)Math.Ceiling(R * sweep / 2.5); for (int i = 0; i <= n; i++) { double a = a0 - sweep * i / n; Q.Add(new[] { cx + R * Math.Cos(a), cy + R * Math.Sin(a), 0.0 }); }
  int nC = (int)Math.Ceiling(sC / 5); for (int i = 1; i < nC; i++) Q.Add(new[] { t2x + sC * qx * i / nC, t2y + sC * qy * i / nC, 0.0 });
  Q.Add(new[] { E[0], E[1], 0.0 });
  double tot = 0; for (int i = 1; i < Q.Count; i++) tot += L2(Q[i - 1], Q[i]);
  double c = 0; Q[0][2] = S[2]; for (int i = 1; i < Q.Count; i++) { c += L2(Q[i - 1], Q[i]); Q[i][2] = S[2] + dzT * c / tot; }
  Q[^1][2] = E[2];
  var pl = new Deswik.Graphics.Figures.Polyline(); foreach (var q in Q) pl.AddVertex(q[0], q[1], q[2]);
  pl.Layer = al; App.ModelSpace.Entities.AddItem(pl, null, null, false, false);
  double minR = 1e9; for (int i = 1; i < Q.Count - 1; i++) { double a = L2(Q[i - 1], Q[i]), b = L2(Q[i], Q[i + 1]), d = L2(Q[i - 1], Q[i + 1]);
    double ar = Math.Abs((Q[i][0] - Q[i - 1][0]) * (Q[i + 1][1] - Q[i - 1][1]) - (Q[i][1] - Q[i - 1][1]) * (Q[i + 1][0] - Q[i - 1][0])) / 2; if (ar > 1e-6) minR = Math.Min(minR, a * b * d / (4 * ar)); }
  double maxG = 0; for (int i = 1; i < Q.Count; i++) { var h = L2(Q[i - 1], Q[i]); if (h > 0.01) maxG = Math.Max(maxG, (Q[i][2] - Q[i - 1][2]) / h); }
  double junc = Q.Min(q => L2(q, new[] { 0.0, 0.0 /* set: junction X, Y */ }));
  Out.AppendLine($"B{o.B} R{R}: handle {pl.GetType().GetProperty("Handle")?.GetValue(pl)}, {Q.Count} pts, {tot:F1} m | min radius {minR:F1} m | grade 1:{1 / maxG:F2} uniform" +
    $" | west x {Q.Min(q => q[0]):F1} (was {oldWest:F1}) north y {Q.Max(q => q[1]):F1} (was {oldNorth:F1}) | DDF/DE2 junction now {junc:F1} m off the line" +
    $" | start {Q[0][0]:F2},{Q[0][1]:F2},{Q[0][2]:F2} end {Q[^1][0]:F2},{Q[^1][1]:F2},{Q[^1][2]:F2}");
}
App.Regen(true); App.Redraw(true, false);
Out.AppendLine("Done. 44B8DE3 unchanged. File not saved.");
return null;
