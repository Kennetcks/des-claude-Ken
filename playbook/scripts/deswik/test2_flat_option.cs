if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW " + System.IO.Path.GetFileName(App.Filename)); return null; }
// TEST 2 layer: option for 44B8F26 with a 15 m flat starting 30 m from the start (top end). Start and end points (x, y, RL) unchanged.
// Geometry: bottom leg and the lower R25 arc unchanged; at the west point of the loop a straight runs north s metres and the upper
// R25 arc + top leg are rebuilt from there (upper curve moves north by s, west edge unchanged). s is solved so the sloped part is 1:7.0.
// Grade: uniform 1:k for ch 0-30, flat ch 30-45, uniform 1:k to the end. CREATE = false only reports; true adds a NEW line (44B8F26 kept).
bool CREATE = false;
var al = App.ActiveLayer;
Deswik.Graphics.Figures.Polyline src = null;
foreach (var e in App.ModelSpace.Entities) { var f = e as Deswik.Graphics.Primaries.Figure; if (f == null || f.Layer == null || f.Deleted || f.Layer.Name != al.Name) continue;
  if ($"{f.GetType().GetProperty("Handle")?.GetValue(f)}" == "44B8F26") { src = (e as Deswik.Graphics.Figures.Polyline) ?? f.asPolyline; break; } }
if (src == null) { Out.AppendLine("44B8F26 not found on " + al.Name); return null; }
double G(object v, string c) { var t = v.GetType(); var p = t.GetProperty(c) ?? t.GetProperty(c.ToLower()); return System.Convert.ToDouble(p.GetValue(v)); }
var P = new List<double[]>(); foreach (var v in src.VertexList) P.Add(new[] { G(v, "X"), G(v, "Y"), G(v, "Z") });
if (P.Count != 66) { Out.AppendLine($"44B8F26 has {P.Count} points, expected 66 - stopping"); return null; }
var T = P[0]; var B = P[65]; double dzT = T[2] - B[2]; const double R = 25.0;
double L2(double[] a, double[] b) => Math.Sqrt((a[0] - b[0]) * (a[0] - b[0]) + (a[1] - b[1]) * (a[1] - b[1]));
// circle through 3 arc points -> lower/whole loop centre
double[] Circ(double[] a, double[] b, double[] c) { double d = 2 * (a[0] * (b[1] - c[1]) + b[0] * (c[1] - a[1]) + c[0] * (a[1] - b[1]));
  double ux = ((a[0] * a[0] + a[1] * a[1]) * (b[1] - c[1]) + (b[0] * b[0] + b[1] * b[1]) * (c[1] - a[1]) + (c[0] * c[0] + c[1] * c[1]) * (a[1] - b[1])) / d;
  double uy = ((a[0] * a[0] + a[1] * a[1]) * (c[0] - b[0]) + (b[0] * b[0] + b[1] * b[1]) * (a[0] - c[0]) + (c[0] * c[0] + c[1] * c[1]) * (b[0] - a[0])) / d; return new[] { ux, uy }; }
var c1 = Circ(P[20], P[36], P[52]);
// bottom leg direction (from B going west) and its tangent point on circle 1
double ax = P[55][0] - B[0], ay = P[55][1] - B[1], la = Math.Sqrt(ax * ax + ay * ay); ax /= la; ay /= la;
double sA = (c1[0] - B[0]) * ax + (c1[1] - B[1]) * ay; var tpA = new[] { B[0] + sA * ax, B[1] + sA * ay };
double phA = Math.Atan2(tpA[1] - c1[1], tpA[0] - c1[0]);
// build path from B: leg A, arc1 clockwise phA -> -pi (west point), north straight s, arc2 clockwise from pi down to tangent to T, leg C to T
List<double[]> Build(double s, out double len) {
  var Q = new List<double[]> { new[] { B[0], B[1] } };
  int nA = Math.Max(1, (int)Math.Ceiling(sA / 5)); for (int i = 1; i <= nA; i++) Q.Add(new[] { B[0] + sA * ax * i / nA, B[1] + sA * ay * i / nA });
  double sw1 = phA - (-Math.PI); while (sw1 < 0) sw1 += 2 * Math.PI;
  int n1 = (int)Math.Ceiling(R * sw1 / 2.4); for (int i = 1; i <= n1; i++) { double a = phA - sw1 * i / n1; Q.Add(new[] { c1[0] + R * Math.Cos(a), c1[1] + R * Math.Sin(a) }); }
  var c2 = new[] { c1[0], c1[1] + s };
  if (s > 0.01) { int nS = Math.Max(1, (int)Math.Ceiling(s / 5)); var w = Q[^1]; for (int i = 1; i <= nS; i++) Q.Add(new[] { w[0], w[1] + s * i / nS }); }
  // tangent point: clockwise heading at angle f is (sin f, -cos f); find f in (0, pi) where T lies on the tangent ahead
  double F(double f) { double px = c2[0] + R * Math.Cos(f), py = c2[1] + R * Math.Sin(f); return Math.Sin(f) * (T[1] - py) - (-Math.Cos(f)) * (T[0] - px); }
  double lo = 0.01, hi = Math.PI - 0.01; for (int k = 0; k < 80; k++) { double mid = (lo + hi) / 2; if (Math.Sign(F(mid)) == Math.Sign(F(hi))) hi = mid; else lo = mid; }
  double fC = (lo + hi) / 2, sw2 = Math.PI - fC;
  int n2 = (int)Math.Ceiling(R * sw2 / 2.4); for (int i = 1; i <= n2; i++) { double a = Math.PI - sw2 * i / n2; Q.Add(new[] { c2[0] + R * Math.Cos(a), c2[1] + R * Math.Sin(a) }); }
  var tc = Q[^1]; double lc = L2(tc, T); int nC = Math.Max(1, (int)Math.Ceiling(lc / 5)); for (int i = 1; i <= nC; i++) Q.Add(new[] { tc[0] + (T[0] - tc[0]) * i / nC, tc[1] + (T[1] - tc[1]) * i / nC });
  len = 0; for (int i = 1; i < Q.Count; i++) len += L2(Q[i - 1], Q[i]); return Q; }
Build(0, out var L0);
Out.AppendLine($"check: rebuilt original loop {L0:F1} m (string is 225.5 m); centre {c1[0]:F2},{c1[1]:F2}");
double target = 15 + 7.0 * dzT, sLo = 0, sHi = 40;
for (int k = 0; k < 60; k++) { double m = (sLo + sHi) / 2; Build(m, out var lm); if (lm < target) sLo = m; else sHi = m; }
double sN = Math.Ceiling(sHi * 10) / 10; var Qp = Build(sN, out var L); Qp.Reverse();   // string runs top (T) -> bottom (B), as 44B8F26
// heights by chainage from T: grade k on the sloped part, flat 30-45 m; insert exact vertices at 30 and 45
var ch = new List<double> { 0 }; for (int i = 1; i < Qp.Count; i++) ch.Add(ch[i - 1] + L2(Qp[i - 1], Qp[i]));
foreach (var cut in new[] { 30.0, 45.0 }) { int i = ch.FindIndex(c => c > cut); if (Math.Abs(ch[i - 1] - cut) < 0.3) { ch[i - 1] = ch[i - 1]; continue; }
  double t = (cut - ch[i - 1]) / (ch[i] - ch[i - 1]); Qp.Insert(i, new[] { Qp[i - 1][0] + t * (Qp[i][0] - Qp[i - 1][0]), Qp[i - 1][1] + t * (Qp[i][1] - Qp[i - 1][1]) }); ch.Insert(i, cut); }
double kk = (L - 15) / dzT;
double Z(double c) => c <= 30 ? T[2] - c / kk : c <= 45 ? T[2] - 30 / kk : T[2] - (c - 15) / kk;
var pts = Qp.Select((q, i) => new[] { q[0], q[1], Z(ch[i]) }).ToList(); pts[0] = new[] { T[0], T[1], T[2] }; pts[^1] = new[] { B[0], B[1], B[2] };
double minR = 1e9; for (int i = 1; i < pts.Count - 1; i++) { double a = L2(pts[i - 1], pts[i]), b = L2(pts[i], pts[i + 1]), d = L2(pts[i - 1], pts[i + 1]);
  double ar = Math.Abs((pts[i][0] - pts[i - 1][0]) * (pts[i + 1][1] - pts[i - 1][1]) - (pts[i][1] - pts[i - 1][1]) * (pts[i + 1][0] - pts[i - 1][0])) / 2; if (ar > 1e-6 && a > 1 && b > 1) minR = Math.Min(minR, a * b * d / (4 * ar)); }
double maxG = 0; for (int i = 1; i < pts.Count; i++) { var h = L2(pts[i - 1], pts[i]); if (h > 0.01) maxG = Math.Max(maxG, Math.Abs(pts[i][2] - pts[i - 1][2]) / h); }
Out.AppendLine($"option: north straight {sN:F1} m on the west side (upper curve moves {sN:F1} m N); length {L:F1} m (was 225.5); grade 1:{kk:F2}, steepest segment 1:{1 / maxG:F2}; min radius {minR:F1} m");
Out.AppendLine($"flat: ch 30.0-45.0 at RL {Z(30):F2}; start {pts[0][0]:F2},{pts[0][1]:F2},{pts[0][2]:F2} end {pts[^1][0]:F2},{pts[^1][1]:F2},{pts[^1][2]:F2}; north edge {pts.Max(p => p[1]):F1} (was {P.Max(p => p[1]):F1}); west edge {pts.Min(p => p[0]):F1} (was {P.Min(p => p[0]):F1})");
if (CREATE) { var pl = new Deswik.Graphics.Figures.Polyline(); foreach (var q in pts) pl.AddVertex(q[0], q[1], q[2]); pl.Layer = al; App.ModelSpace.Entities.AddItem(pl, null, null, false, false);
  App.Regen(true); App.Redraw(true, false); Out.AppendLine($"created {pl.GetType().GetProperty("Handle")?.GetValue(pl)} with {pts.Count} pts on {al.Name}. 44B8F26 unchanged. File not saved."); }
return null;
