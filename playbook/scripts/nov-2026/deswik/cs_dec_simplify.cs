// MPA 07 MAIN DEC: fewer vertices. Plan simplification (Douglas-Peucker) keeping the line within TOL of the current
// one, then RLs reset to exactly 1:7 down from the joint along the new plan length. Dry run unless APPLY.
const bool APPLY = false;
const double TOL = 0.05;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
Deswik.Graphics.Primaries.Figure dec = null;
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
    if (!f.Deleted && f.Layer != null && f.Layer.Name == "0\\DESIGN DRAFT\\CENTERLINES" && f.IsPolyline && A(f, "CLAUDE_MPA07") == "1" && A(f, "ACT_TYPE") == "DEC") dec = f;
var P = Enumerable.Range(0, dec.asPolyline.VertexCount).Select(i => { var q = dec.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
double Dist(double[] p, double[] a, double[] b) { double vx = b[0] - a[0], vy = b[1] - a[1], L2 = vx * vx + vy * vy; double t = L2 > 0 ? Math.Max(0, Math.Min(1, ((p[0] - a[0]) * vx + (p[1] - a[1]) * vy) / L2)) : 0; return Math.Sqrt(Math.Pow(a[0] + t * vx - p[0], 2) + Math.Pow(a[1] + t * vy - p[1], 2)); }
var keep = new bool[P.Count]; keep[0] = keep[P.Count - 1] = true;
void DP(int i0, int i1) { double md = 0; int mi = -1; for (int i = i0 + 1; i < i1; i++) { double d = Dist(P[i], P[i0], P[i1]); if (d > md) { md = d; mi = i; } } if (mi >= 0 && md > TOL) { keep[mi] = true; DP(i0, mi); DP(mi, i1); } }
DP(0, P.Count - 1);
var S = Enumerable.Range(0, P.Count).Where(i => keep[i]).Select(i => new[] { P[i][0], P[i][1], 0.0 }).ToList();
double s = 0; S[0][2] = P[0][2]; for (int i = 1; i < S.Count; i++) { s += Math.Sqrt(Math.Pow(S[i][0] - S[i - 1][0], 2) + Math.Pow(S[i][1] - S[i - 1][1], 2)); S[i][2] = P[0][2] - s / 7.0; }
double dev = P.Max(p => Enumerable.Range(1, S.Count - 1).Min(i => Dist(p, S[i - 1], S[i])));
double oldLen = 0; for (int i = 1; i < P.Count; i++) oldLen += Math.Sqrt(Math.Pow(P[i][0] - P[i - 1][0], 2) + Math.Pow(P[i][1] - P[i - 1][1], 2));
Out.AppendLine($"MAIN DEC: {P.Count} -> {S.Count} vertices, max plan deviation {dev:F3} m, plan length {oldLen:F2} -> {s:F2} m, end RL {P[^1][2]:F3} -> {S[^1][2]:F3}, grade exactly 1:7");
Out.AppendLine("segment lengths: " + string.Join(", ", Enumerable.Range(1, S.Count - 1).Select(i => Math.Sqrt(Math.Pow(S[i][0] - S[i - 1][0], 2) + Math.Pow(S[i][1] - S[i - 1][1], 2)).ToString("F1"))));
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }
var c = (Deswik.Graphics.Primaries.Figure)dec.Clone(); var pl = c.asPolyline; while (pl.VertexCount > 0) pl.RemoveVertexAt(0);
foreach (var q in S) pl.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]); pl.Closed = false; App.ModelSpace.Entities.AddItem(c, null, null, true, false); dec.Deleted = true;
App.Redraw(true); Out.AppendLine("applied"); return null;
