// Read-only: DSP mouth in detail. Every wall vertex near the DSP (drafted WALLS and Drafter CENTERLINES\WALLS, decline and
// DSP) with decline chainage/offset/floor dz and DSP-frame along/side/height above the DSP grade; plus each decline wall
// string's vertex spacing and offset along its whole length (to spot kinks / bulges / odd spacing).
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
var H = new Dictionary<string, Deswik.Graphics.Primaries.Figure>(StringComparer.OrdinalIgnoreCase); foreach (Deswik.Graphics.Primaries.Figure x in App.ModelSpace.Entities) if (!x.Deleted && x.Layer != null) H[x.Handle] = x;
List<double[]> P3(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
double D2(double[] a, double[] b) => Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2));
double Az(double[] a, double[] b) { var z = Math.Atan2(b[0] - a[0], b[1] - a[1]) * 180 / Math.PI; return z < 0 ? z + 360 : z; }
double Turn(double a1, double a2) { var d = a2 - a1; while (d > 180) d -= 360; while (d < -180) d += 360; return d; }
var M = P3(H["44BD9C0"]); var d = P3(H["4588F43"]); var CL = M.Take(M.Count - 1).Concat(d).ToList(); double sJ = 0; for (int i = 1; i < M.Count; i++) sJ += D2(M[i], M[i - 1]);
(double s, double off, double dz) Fr(double[] q)
{
    double bs = 0, bd = 1e9, bo = 0, bz = 0, c0 = 0;
    for (int i = 1; i < CL.Count; i++)
    {
        double vx = CL[i][0] - CL[i - 1][0], vy = CL[i][1] - CL[i - 1][1], L = Math.Sqrt(vx * vx + vy * vy), t = Math.Max(0, Math.Min(1, ((q[0] - CL[i - 1][0]) * vx + (q[1] - CL[i - 1][1]) * vy) / (L * L)));
        double dd = Math.Sqrt(Math.Pow(CL[i - 1][0] + t * vx - q[0], 2) + Math.Pow(CL[i - 1][1] + t * vy - q[1], 2));
        if (dd < bd - 1e-9) { bd = dd; bs = c0 + t * L; bo = ((q[0] - CL[i - 1][0]) * vy - (q[1] - CL[i - 1][1]) * vx) / L; bz = CL[i - 1][2] + t * (CL[i][2] - CL[i - 1][2]); }
        c0 += L;
    }
    return (bs - sJ, bo, q[2] - bz);
}
var dc = P3(H["4588F65"]); var S = dc[0]; double ux = dc[1][0] - S[0], uy = dc[1][1] - S[1], ul = Math.Sqrt(ux * ux + uy * uy); ux /= ul; uy /= ul;
string DF(double[] q) { double a = (q[0] - S[0]) * ux + (q[1] - S[1]) * uy, s = (q[0] - S[0]) * uy - (q[1] - S[1]) * ux; return $"along {a:F2} side {(s >= 0 ? "R" : "L")}{Math.Abs(s):F2} h {q[2] - (S[2] + Math.Max(0, a) / 50):F3}"; }
Out.AppendLine($"DSP CL start ({S[0]:F3},{S[1]:F3},{S[2]:F3}) ch {Fr(S).s:F2}, az {Az(S, dc[1]):F2}; R = right looking into the DSP");
var walls = H.Values.Where(f => f.IsPolyline && (f.Layer.Name == "0\\DESIGN DRAFT\\WALLS" || f.Layer.Name == "0\\DESIGN DRAFT\\CENTERLINES\\WALLS") && (A(f, "Name") == "W6390 DSP" || A(f, "Name") == "MAIN DEC")).ToList();
foreach (var f in walls)
{
    var p = P3(f); var idx = Enumerable.Range(0, p.Count).Where(i => D2(p[i], S) < 14).ToList(); if (idx.Count == 0) continue;
    Out.AppendLine($"=== {f.Handle} [{f.Layer.Name.Replace("0\\DESIGN DRAFT\\", "")}] {A(f, "Name")} n={p.Count} closed={f.asPolyline.Closed}");
    foreach (var i in idx)
    {
        var o = Fr(p[i]); string turn = (i > 0 && i < p.Count - 1) ? $" turn {Turn(Az(p[i - 1], p[i]), Az(p[i], p[i + 1])):F1}" : "";
        Out.AppendLine($"  v{i,3} ({p[i][0]:F3},{p[i][1]:F3},{p[i][2]:F3}) ch {o.s:F2} off {o.off:F2} dz {o.dz:F3} | {DF(p[i])}" + (i > 0 ? $" | seg {D2(p[i], p[i - 1]):F2}" : "") + turn);
    }
}
// whole-length profile of the two drafted decline walls and the generated decline wall
foreach (var f in walls.Where(f => A(f, "Name") == "MAIN DEC"))
{
    var p = P3(f); var o = p.Select(Fr).ToList(); var seg = Enumerable.Range(1, p.Count - 1).Select(i => D2(p[i], p[i - 1])).ToList();
    var bad = Enumerable.Range(1, p.Count - 2).Where(i => Math.Abs(Turn(Az(p[i - 1], p[i]), Az(p[i], p[i + 1]))) > 20 && Math.Abs(Math.Abs(o[i].off) - 2.75) < 0.3).ToList();
    Out.AppendLine($"--- {f.Handle} [{f.Layer.Name.Replace("0\\DESIGN DRAFT\\", "")}] n={p.Count}: seg {seg.Min():F2}-{seg.Max():F2}, sharp turns on the wall line at v{string.Join(",v", bad)}; off by 5 m bins: " + string.Join(" ", o.GroupBy(x => Math.Floor(x.s / 5) * 5).OrderBy(g => g.Key).Select(g => $"[{g.Key:F0}:{g.Min(x => x.off):F2}/{g.Max(x => x.off):F2} n{g.Count()}]")));
}
return null;
