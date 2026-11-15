// Build Ventsim airways from the COMBINED 12.11 DXF (3D polylines, layer = ACT_TYPE).
// Ends of each string are snapped onto the nearest other string (vertex or segment) within a tolerance, so take-offs,
// raises and decline pieces join; then every string becomes airways between its vertices. C# 5 (PowerShell 5.1 Add-Type).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

public class VStr { public string Act, Name, Level, Handle; public List<double[]> P = new List<double[]>(); }

public static class VsmFromDxf
{
    static CultureInfo IC = CultureInfo.InvariantCulture;
    public static StringBuilder Log = new StringBuilder();

    public static List<VStr> ReadDxf(string dxf, string keyTsv)
    {
        var L = File.ReadAllLines(dxf); var res = new List<VStr>(); VStr cur = null;
        for (int i = 0; i + 1 < L.Length; i += 2)
        {
            int code = int.Parse(L[i].Trim()); string v = L[i + 1].Trim();
            if (code == 0) { if (v == "POLYLINE") { cur = new VStr(); res.Add(cur); } else if (v == "SEQEND") cur = null; continue; }
            if (cur == null) continue;
            if (code == 8 && cur.Act == null) cur.Act = v;
            if (code == 10) { double x = double.Parse(v, IC), y = double.Parse(L[i + 3].Trim(), IC), z = double.Parse(L[i + 5].Trim(), IC);
                // POLYLINE header has a dummy 10/20/30 = 0,0,0: skip it (first group-10 before any VERTEX)
                if (!(x == 0 && y == 0 && z == 0)) cur.P.Add(new[] { x, y, z }); }
        }
        var K = File.ReadAllLines(keyTsv).Skip(1).Select(l => l.Split('\t')).ToList();
        if (K.Count != res.Count) throw new Exception("key rows " + K.Count + " vs DXF polylines " + res.Count);
        for (int i = 0; i < res.Count; i++) { res[i].Handle = K[i][0]; res[i].Name = K[i][1]; res[i].Level = K[i][3]; }
        return res;
    }

    static double D(double[] a, double[] b) { double dx = a[0] - b[0], dy = a[1] - b[1], dz = a[2] - b[2]; return Math.Sqrt(dx * dx + dy * dy + dz * dz); }
    static double[] Foot(double[] p, double[] a, double[] b, out double t)
    {
        double vx = b[0] - a[0], vy = b[1] - a[1], vz = b[2] - a[2]; double ll = vx * vx + vy * vy + vz * vz;
        t = ll < 1e-12 ? 0 : ((p[0] - a[0]) * vx + (p[1] - a[1]) * vy + (p[2] - a[2]) * vz) / ll; t = Math.Max(0, Math.Min(1, t));
        return new[] { a[0] + t * vx, a[1] + t * vy, a[2] + t * vz };
    }

    // snap ends; returns per-end report lines
    public static void Snap(List<VStr> S, double tol, double vertexPref)
    {
        int already = 0, toVertex = 0, toSeg = 0, dead = 0; var hist = new int[8]; double[] edges = { 0.05, 1, 2, 3, 4, 5, 6, 8 };
        for (int si = 0; si < S.Count; si++)
            for (int e = 0; e < 2; e++)
            {
                var s = S[si]; int vi = e == 0 ? 0 : s.P.Count - 1; var p = s.P[vi];
                double best = 1e18; int bj = -1, bk = -1; double bt = 0; double[] bf = null;
                double bestV = 1e18; int vj = -1, vk = -1;
                for (int sj = 0; sj < S.Count; sj++)
                {
                    if (sj == si) continue; var q = S[sj].P;
                    for (int k = 0; k < q.Count; k++) { double dv = D(p, q[k]); if (dv < bestV) { bestV = dv; vj = sj; vk = k; } }
                    for (int k = 0; k + 1 < q.Count; k++) { double t; var f = Foot(p, q[k], q[k + 1], out t); double d = D(p, f); if (d < best) { best = d; bj = sj; bk = k; bt = t; bf = f; } }
                }
                int h = 0; while (h < edges.Length && best > edges[h]) h++; if (h < hist.Length) hist[h]++;
                if (bestV <= 0.05) { already++; s.P[vi] = (double[])S[vj].P[vk].Clone(); continue; }
                if (best > tol) { dead++; continue; }
                if (bestV <= best + vertexPref) { s.P[vi] = (double[])S[vj].P[vk].Clone(); toVertex++; }
                else { S[bj].P.Insert(bk + 1, bf); s.P[vi] = (double[])bf.Clone(); toSeg++; }
            }
        Log.AppendLine("ends already joined: " + already + ", snapped to a vertex: " + toVertex + ", snapped onto a segment (vertex inserted): " + toSeg + ", left open (> " + tol + " m, faces): " + dead);
        var hs = new StringBuilder("nearest-other-string distance of ends: ");
        for (int h = 0; h < hist.Length; h++) hs.Append("<=" + edges[h] + "m:" + hist[h] + "  ");
        Log.AppendLine(hs.ToString());
    }

    // nearest other string to each end (no changes): "name | act | end | dist | other name"
    public static List<string> Nearest(List<VStr> S, IEnumerable<int> which)
    {
        var o = new List<string>();
        foreach (int si in which)
            for (int e = 0; e < 2; e++)
            {
                var s = S[si]; var p = e == 0 ? s.P[0] : s.P[s.P.Count - 1]; double best = 1e18; int bj = -1;
                for (int sj = 0; sj < S.Count; sj++) { if (sj == si) continue; var q = S[sj].P; for (int k = 0; k + 1 < q.Count; k++) { double t; var f = Foot(p, q[k], q[k + 1], out t); double d = D(p, f); if (d < best) { best = d; bj = sj; } } }
                o.Add(s.Name + "\t" + s.Act + "\t" + (e == 0 ? "start" : "end") + "\t" + best.ToString("F1", IC) + "\t" + (bj >= 0 ? S[bj].Name + " [" + S[bj].Act + "]" : "") + "\t" + p[0].ToString("F1", IC) + "," + p[1].ToString("F1", IC) + "," + p[2].ToString("F1", IC));
            }
        return o;
    }

    // drop short interior vertices that belong to this string only (keeps junctions and ends)
    public static void Thin(List<VStr> S, double minSeg)
    {
        var use = new Dictionary<string, int>();
        Func<double[], string> K = p => Math.Round(p[0], 2).ToString(IC) + "|" + Math.Round(p[1], 2).ToString(IC) + "|" + Math.Round(p[2], 2).ToString(IC);
        foreach (var s in S) foreach (var p in s.P) { var k = K(p); use[k] = use.ContainsKey(k) ? use[k] + 1 : 1; }
        int before = S.Sum(s => s.P.Count);
        foreach (var s in S)
        {
            var o = new List<double[]> { s.P[0] };
            for (int i = 1; i < s.P.Count - 1; i++) { if (use[K(s.P[i])] > 1 || D(o[o.Count - 1], s.P[i]) >= minSeg) o.Add(s.P[i]); }
            var last = s.P[s.P.Count - 1];
            if (o.Count > 1 && D(o[o.Count - 1], last) < 0.5 * minSeg && use[K(o[o.Count - 1])] == 1) o.RemoveAt(o.Count - 1);
            o.Add(last);
            // remove zero-length steps
            var o2 = new List<double[]> { o[0] }; for (int i = 1; i < o.Count; i++) if (D(o2[o2.Count - 1], o[i]) > 0.01) o2.Add(o[i]);
            s.P = o2;
        }
        Log.AppendLine("vertices " + before + " -> " + S.Sum(s => s.P.Count) + " after thinning (min segment " + minSeg + " m, junctions kept)");
    }

    // connectivity of the airway network
    public static void Components(List<VStr> S)
    {
        var id = new Dictionary<string, int>(); var par = new List<int>();
        Func<double[], int> N = p => { var k = Math.Round(p[0], 2).ToString(IC) + "|" + Math.Round(p[1], 2).ToString(IC) + "|" + Math.Round(p[2], 2).ToString(IC); int v; if (!id.TryGetValue(k, out v)) { v = par.Count; id[k] = v; par.Add(v); } return v; };
        Func<int, int> F = null; F = x => par[x] == x ? x : (par[x] = F(par[x]));
        foreach (var s in S) for (int i = 1; i < s.P.Count; i++) { int a = F(N(s.P[i - 1])), b = F(N(s.P[i])); if (a != b) par[a] = b; }
        var comp = new Dictionary<int, List<VStr>>();
        foreach (var s in S) { int c = F(N(s.P[0])); if (!comp.ContainsKey(c)) comp[c] = new List<VStr>(); comp[c].Add(s); }
        var cs = comp.Values.OrderByDescending(c => c.Count).ToList();
        Log.AppendLine("nodes " + id.Count + ", connected pieces: " + cs.Count + " (largest has " + cs[0].Count + " of " + S.Count + " strings)");
        foreach (var c in cs.Skip(1)) Log.AppendLine("  separate piece: " + string.Join(", ", c.Select(s => s.Name + " [" + s.Act + "]")));
    }
}
