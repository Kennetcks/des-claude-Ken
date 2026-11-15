// New Ventsim model = existing tunnels of a reference .vsm (no bags, fans, tie links; one stage) + the COMBINED 12.11 DXF drives,
// DXF string ends snapped onto the nearest drive (DXF or existing) so everything joins. C# 5 for PowerShell 5.1 Add-Type.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

public class VAir { public string Act, Name, Mine; public bool Ref; public string[] Row; public List<double[]> P = new List<double[]>(); }

public static class VsmBuild
{
    static CultureInfo IC = CultureInfo.InvariantCulture;
    public static StringBuilder Log = new StringBuilder();
    public static bool DxfOnly = false;
    public static HashSet<string> KeepUnique = new HashSet<string>();
    public static List<string> NoDropPrefix = new List<string>();
    public static Dictionary<string, double[]> SizeOverride = new Dictionary<string, double[]>();
    static double D(double[] a, double[] b) { double dx = a[0] - b[0], dy = a[1] - b[1], dz = a[2] - b[2]; return Math.Sqrt(dx * dx + dy * dy + dz * dz); }
    static double[] Foot(double[] p, double[] a, double[] b, out double t)
    {
        double vx = b[0] - a[0], vy = b[1] - a[1], vz = b[2] - a[2]; double ll = vx * vx + vy * vy + vz * vz;
        t = ll < 1e-12 ? 0 : ((p[0] - a[0]) * vx + (p[1] - a[1]) * vy + (p[2] - a[2]) * vz) / ll; t = Math.Max(0, Math.Min(1, t));
        return new[] { a[0] + t * vx, a[1] + t * vy, a[2] + t * vz };
    }
    static string K(double[] p) { return Math.Round(p[0], 2).ToString(IC) + "|" + Math.Round(p[1], 2).ToString(IC) + "|" + Math.Round(p[2], 2).ToString(IC); }
    static string N(double v) { return v.ToString("R", IC); }

    public static string Run(string refVsm, string dxf, string keyTsv, string outVsm, double tol, bool write)
    {
        Log.Clear();
        // ---- reference model text
        string text; using (var fs = File.OpenRead(refVsm)) using (var gz = new GZipStream(fs, CompressionMode.Decompress)) using (var sr = new StreamReader(gz, Encoding.Default)) text = sr.ReadToEnd();
        string nl = text.Contains("\r\n") ? "\r\n" : "\n";
        var L = text.Split(new[] { nl }, StringSplitOptions.None).ToList();
        int m = L.FindIndex(l => l.StartsWith("MAIN\tUnique No")); int end = m + 1; while (!L[end].StartsWith("END\tMAIN")) end++;
        var h = L[m].Split('\t'); var ix = new Dictionary<string, int>(); for (int i = 0; i < h.Length; i++) if (!ix.ContainsKey(h[i])) ix[h[i]] = i;
        var rows = new List<string[]>(); for (int i = m + 1; i < end; i++) rows.Add(L[i].Split('\t'));
        var byU = rows.ToDictionary(r => r[1]);
        int maxU = rows.Max(r => int.Parse(r[1]));
        // tunnel presets: id|name|?|height|width|...|shape(7)|...|s52(12)|spi(13)
        var pre = new Dictionary<string, string[]>();
        int tp = L.IndexOf("TUNNELPRESETS"); for (int i = tp + 1; !L[i].StartsWith("ENDTUNNEL"); i++) { var c = L[i].Split('\t'); pre[c[1]] = c;
            // keep the file's own preset in step with any size override (col 3 = height, col 4 = width)
            if (SizeOverride.ContainsKey(c[1])) { c[4] = N(SizeOverride[c[1]][0]); c[3] = N(SizeOverride[c[1]][1]); L[i] = string.Join("\t", c); Log.AppendLine("preset " + c[1] + " set to " + c[4] + " W x " + c[3] + " H"); } }

        // ---- existing tunnels (drop ducts/bags 134217728, tie links 67108864)
        var A = new List<VAir>(); int dropped = 0;
        foreach (var r in rows)
        {
            long f = long.Parse(r[ix["Branch Flags"]]);
            if ((f & 134217728) != 0 || (f & 67108864) != 0) { dropped++; continue; }
            var a = new VAir { Ref = true, Row = r, Act = "REF", Name = r[ix["Branch Name"]] };
            a.P.Add(new[] { double.Parse(r[ix["X1"]], IC), double.Parse(r[ix["Y1"]], IC), double.Parse(r[ix["Z1"]], IC) });
            a.P.Add(new[] { double.Parse(r[ix["X2"]], IC), double.Parse(r[ix["Y2"]], IC), double.Parse(r[ix["Z2"]], IC) });
            A.Add(a);
        }
        // portal RL from the reference surface-exit decline airways (needed before the existing tunnels are possibly cleared)
        double portalZ = A.Where(z => (long.Parse(z.Row[ix["Branch Flags"]]) & 4096) != 0 && z.Row[ix["Tunnel Type"]] == "1").Select(z => Math.Max(z.P[0][2], z.P[1][2])).DefaultIfEmpty(6422.9).Min();
        if (DxfOnly && KeepUnique.Count > 0) { A.RemoveAll(z => !KeepUnique.Contains(z.Row[1]));
            // a blocked (fixed-resistance) decline airway would cut the decline: open it
            foreach (var z in A.Where(z => z.Row[ix["Tunnel Type"]] == "1" && z.Row[ix["Resistance Type"]] == "1")) { z.Row[ix["Resistance Type"]] = "0"; Log.AppendLine("opened blocked decline airway u" + z.Row[1] + " (was fixed resistance " + z.Row[ix["Resistance"]] + ")"); } Log.AppendLine("reference: kept only " + A.Count + " existing airways (decline etc.): " + string.Join(", ", A.Select(z => "u" + z.Row[1]))); }
        else if (DxfOnly) { A.Clear(); Log.AppendLine("reference: " + rows.Count + " airways - ALL removed (DXF only); the file's presets, layers, fan database and settings are kept"); }
        int nRef = A.Count;
        if (!DxfOnly) Log.AppendLine("reference: " + rows.Count + " airways; kept " + nRef + " tunnels, dropped " + dropped + " bag/duct/fan/tie airways");

        // ---- DXF strings
        var DL = File.ReadAllLines(dxf); VAir cur = null; var dx = new List<VAir>();
        for (int i = 0; i + 1 < DL.Length; i += 2)
        {
            string v = DL[i + 1].Trim(); int code = int.Parse(DL[i].Trim());
            if (code == 0) { if (v == "POLYLINE") { cur = new VAir(); dx.Add(cur); } else if (v == "SEQEND") cur = null; continue; }
            if (cur == null) continue;
            if (code == 8 && cur.Act == null) cur.Act = v;
            if (code == 10) { double x = double.Parse(v, IC), y = double.Parse(DL[i + 3].Trim(), IC), z = double.Parse(DL[i + 5].Trim(), IC); if (!(x == 0 && y == 0 && z == 0)) cur.P.Add(new[] { x, y, z }); }
        }
        var KR = File.ReadAllLines(keyTsv).Skip(1).Select(l => l.Split('\t')).ToList();
        if (KR.Count != dx.Count) throw new Exception("key rows " + KR.Count + " vs DXF " + dx.Count);
        for (int i = 0; i < dx.Count; i++) { dx[i].Name = KR[i][1]; dx[i].Mine = KR[i][2]; }
        // overlap with existing tunnels: DXF interior vertices lying on an existing airway
        int overl = 0; var ovNames = new List<string>();
        foreach (var s in dx)
        {
            int on = 0; for (int i = 1; i < s.P.Count - 1; i++) { foreach (var a in A) { double t; if (D(s.P[i], Foot(s.P[i], a.P[0], a.P[1], out t)) < 1.5) { on++; break; } } }
            if (s.P.Count > 2 && on > 0) { overl++; ovNames.Add(s.Name + " (" + on + "/" + (s.P.Count - 2) + ")"); }
        }
        Log.AppendLine("DXF strings: " + dx.Count + "; strings with vertices lying on an existing tunnel: " + overl + (overl > 0 ? " -> " + string.Join(", ", ovNames) : ""));
        // drop DXF strings that lie entirely on existing tunnels (already in the model)
        Func<double[], bool> OnRef = p => A.Any(a => { double t; return D(p, Foot(p, a.P[0], a.P[1], out t)) < 1.5; });
        var dup = dx.Where(s => !NoDropPrefix.Any(pf => s.Name.StartsWith(pf))).Where(s => s.P.All(OnRef) || (s.P.Count >= 5 && s.P.Skip(1).Take(s.P.Count - 2).Count(OnRef) >= 0.8 * (s.P.Count - 2))).ToList();
        foreach (var s in dup) dx.Remove(s);
        Log.AppendLine("DXF strings dropped as already in the model: " + (dup.Count == 0 ? "none" : string.Join(", ", dup.Select(s => s.Name))));
        // existing airways whose end sits on another existing airway without a node: split there
        int refFix = 0;
        for (int si = 0; si < nRef; si++) for (int e = 0; e < 2; e++)
        {
            var p = A[si].P[e == 0 ? 0 : A[si].P.Count - 1]; bool onNode = false;
            for (int sj = 0; sj < nRef && !onNode; sj++) if (sj != si) foreach (var q in A[sj].P) if (D(p, q) <= 0.05) { onNode = true; break; }
            if (onNode) continue;
            for (int sj = 0; sj < nRef; sj++) { if (sj == si) continue; var q = A[sj].P; bool done = false; for (int k = 0; k + 1 < q.Count; k++) { double t; var f = Foot(p, q[k], q[k + 1], out t); if (t > 0 && t < 1 && D(p, f) <= 0.1) { q.Insert(k + 1, (double[])p.Clone()); refFix++; done = true; break; } } if (done) break; }
        }
        Log.AppendLine("existing airways joined where an end sat on another airway without a node: " + refFix);
        int firstDx = A.Count; A.AddRange(dx);

        // ---- snap DXF ends onto the nearest other airway (vertex preferred within 1 m extra).
        // Ends of DXF strings not processed yet are ignored as targets, so two drives starting at the same point
        // both go onto the parent drive instead of only onto each other.
        int already = 0, toV = 0, toS = 0, open = 0; var far = new List<string>(); var openEnds = new List<KeyValuePair<VAir, int>>();
        for (int si = firstDx; si < A.Count; si++)
            for (int e = 0; e < 2; e++)
            {
                var s = A[si]; int vi = e == 0 ? 0 : s.P.Count - 1; var p = s.P[vi];
                double best = 1e18, bestV = 1e18; int bj = -1, bk = -1, vj = -1, vk = -1; double[] bf = null;
                for (int sj = 0; sj < A.Count; sj++)
                {
                    if (sj == si) continue; var q = A[sj].P; bool pend = sj > si; int lastK = q.Count - 1;
                    for (int k = 0; k < q.Count; k++) { if (pend && (k == 0 || k == lastK)) continue; double dv = D(p, q[k]); if (dv < bestV) { bestV = dv; vj = sj; vk = k; } }
                    for (int k = 0; k + 1 < q.Count; k++) { double t; var f = Foot(p, q[k], q[k + 1], out t); if (pend && ((k == 0 && t < 1e-6) || (k + 1 == lastK && t > 1 - 1e-6))) continue; double d = D(p, f); if (d < best) { best = d; bj = sj; bk = k; bf = f; } }
                }
                if (bestV <= 0.05) { already++; s.P[vi] = (double[])A[vj].P[vk].Clone(); continue; }
                if (best > tol) { open++; openEnds.Add(new KeyValuePair<VAir, int>(s, e)); continue; }
                if (best > 5) far.Add(s.Name + " " + (e == 0 ? "start" : "end") + " -> " + (A[bj].Ref ? "existing tunnel" : A[bj].Name) + " " + best.ToString("F1", IC) + " m");
                if (bestV <= best + 1.0) { s.P[vi] = (double[])A[vj].P[vk].Clone(); toV++; }
                else { A[bj].P.Insert(bk + 1, bf); s.P[vi] = (double[])bf.Clone(); toS++; }
            }
        Log.AppendLine("DXF ends: already on a node " + already + ", snapped to a node " + toV + ", snapped onto an airway (split) " + toS + ", left open as faces " + open);
        if (far.Count > 0) Log.AppendLine("joins over 5 m (check these): " + string.Join("; ", far));
        // open decline ends at the box-cut floor (RL >= portalZ - 1) are new portals -> surface exit on that airway
        var portals = new HashSet<string>();
        Log.AppendLine("portal RL " + portalZ.ToString("F2", IC) + "; open decline ends: " + string.Join("; ", openEnds.Where(o => o.Key.Act == "DEC").Select(o => o.Key.Name + " " + (o.Value == 0 ? o.Key.P[0][2] : o.Key.P[o.Key.P.Count - 1][2]).ToString("F1", IC))));
        foreach (var oe in openEnds) { var s = oe.Key; var p = oe.Value == 0 ? s.P[0] : s.P[s.P.Count - 1]; if (s.Act == "DEC" && p[2] >= portalZ - 1) { portals.Add(K(p)); Log.AppendLine("new portal (surface exit): " + s.Name + " " + (oe.Value == 0 ? "start" : "end") + " at " + p[0].ToString("F1", IC) + ", " + p[1].ToString("F1", IC) + ", RL " + p[2].ToString("F1", IC)); } }

        // ---- thin DXF vertices (keep junctions/ends, segments >= 5 m)
        var use = new Dictionary<string, int>(); foreach (var a in A) foreach (var p in a.P) { var k = K(p); use[k] = use.ContainsKey(k) ? use[k] + 1 : 1; }
        for (int si = firstDx; si < A.Count; si++)
        {
            var s = A[si]; var o = new List<double[]> { s.P[0] };
            for (int i = 1; i < s.P.Count - 1; i++) if (use[K(s.P[i])] > 1 || D(o[o.Count - 1], s.P[i]) >= 5.0) o.Add(s.P[i]);
            var last = s.P[s.P.Count - 1]; if (o.Count > 1 && D(o[o.Count - 1], last) < 2.5 && use[K(o[o.Count - 1])] == 1) o.RemoveAt(o.Count - 1); o.Add(last);
            var o2 = new List<double[]> { o[0] }; for (int i = 1; i < o.Count; i++) if (D(o2[o2.Count - 1], o[i]) > 0.01) o2.Add(o[i]); s.P = o2;
        }

        // ---- node degree + connectivity (pass 0 closes gaps <= 1.5 m and leaves out loose DXF pieces; pass 1 reports)
        // merge points closer than 0.05 m into one node (snaps can land a few mm apart)
        { var reps = new Dictionary<string, List<double[]>>(); int merged = 0;
          foreach (var a in A) for (int i = 0; i < a.P.Count; i++)
          {
              var p = a.P[i]; long cx = (long)Math.Floor(p[0] / 0.1), cy = (long)Math.Floor(p[1] / 0.1), cz = (long)Math.Floor(p[2] / 0.1); double[] hit = null;
              for (long x = cx - 1; x <= cx + 1 && hit == null; x++) for (long y = cy - 1; y <= cy + 1 && hit == null; y++) for (long z = cz - 1; z <= cz + 1 && hit == null; z++)
              { List<double[]> l; if (reps.TryGetValue(x + "|" + y + "|" + z, out l)) foreach (var q in l) if (D(p, q) < 0.05) { hit = q; break; } }
              if (hit != null) { if (D(p, hit) > 0) merged++; a.P[i] = (double[])hit.Clone(); }
              else { var key = cx + "|" + cy + "|" + cz; if (!reps.ContainsKey(key)) reps[key] = new List<double[]>(); reps[key].Add(p); }
          }
          foreach (var a in A) { var o = new List<double[]> { a.P[0] }; for (int i = 1; i < a.P.Count; i++) if (D(o[o.Count - 1], a.P[i]) > 0.001) o.Add(a.P[i]); a.P = o; }
          A.RemoveAll(a => a.P.Count < 2);
          Log.AppendLine("points merged (closer than 0.05 m): " + merged); }
        Dictionary<string, int> deg = null;
        for (int pass = 0; pass < 2; pass++) {
        deg = new Dictionary<string, int>(); var id = new Dictionary<string, int>(); var par = new List<int>();
        Func<string, int> NI = k => { int v; if (!id.TryGetValue(k, out v)) { v = par.Count; id[k] = v; par.Add(v); } return v; };
        Func<int, int> F = null; F = x => par[x] == x ? x : (par[x] = F(par[x]));
        int nAir = 0;
        foreach (var a in A) for (int i = 1; i < a.P.Count; i++) { string k1 = K(a.P[i - 1]), k2 = K(a.P[i]); if (k1 == k2) continue; nAir++; deg[k1] = deg.ContainsKey(k1) ? deg[k1] + 1 : 1; deg[k2] = deg.ContainsKey(k2) ? deg[k2] + 1 : 1; int x = F(NI(k1)), y = F(NI(k2)); if (x != y) par[x] = y; }
        var comp = new Dictionary<int, List<VAir>>(); foreach (var a in A) { int c = F(NI(K(a.P[0]))); if (!comp.ContainsKey(c)) comp[c] = new List<VAir>(); comp[c].Add(a); }
        // the piece holding the surface exits
        int surf = -1; foreach (var a in A.Where(z => z.Ref)) if ((long.Parse(a.Row[ix["Branch Flags"]]) & 4096) != 0) { surf = F(NI(K(a.P[0]))); break; }
        var surfSet = new HashSet<int> { surf }; foreach (var a in A.Where(z => z.Ref)) if ((long.Parse(a.Row[ix["Branch Flags"]]) & 4096) != 0) surfSet.Add(F(NI(K(a.P[0]))));
        foreach (var pk in portals) surfSet.Add(F(NI(pk)));
        var cs = comp.OrderByDescending(c => c.Value.Count).ToList();
        if (pass == 0)
        {
            foreach (var c in cs) if (!surfSet.Contains(c.Key))
            {
                double md = 1e18; double[] mb = null, gf = null; VAir gt = null; int gk = -1;
                foreach (var a in c.Value) foreach (var p in a.P) foreach (var sk in surfSet) if (comp.ContainsKey(sk)) foreach (var b in comp[sk]) for (int k = 0; k + 1 < b.P.Count; k++) { double t; var f = Foot(p, b.P[k], b.P[k + 1], out t); double d = D(p, f); if (d < md) { md = d; mb = p; gf = f; gt = b; gk = k; } }
                if (md <= 1.5) { mb[0] = gf[0]; mb[1] = gf[1]; mb[2] = gf[2]; if (D(gf, gt.P[gk]) > 0.01 && D(gf, gt.P[gk + 1]) > 0.01) gt.P.Insert(gk + 1, (double[])gf.Clone()); Log.AppendLine("joined loose piece (" + string.Join(", ", c.Value.Select(z => z.Ref ? "existing u" + z.Row[1] : z.Name).Distinct()) + ") across a " + md.ToString("F1", IC) + " m gap"); }
                else if (DxfOnly && md <= 20 && c.Value.All(z => !z.Ref))
                {   // short link drive from the loose piece to the nearest drive, same type/name as the piece
                    VAir src = c.Value.First(z => z.P.Any(q => q == mb));
                    if (D(gf, gt.P[gk]) > 0.01 && D(gf, gt.P[gk + 1]) > 0.01) gt.P.Insert(gk + 1, (double[])gf.Clone());
                    var link = new VAir { Act = src.Act, Name = src.Name, Mine = src.Mine }; link.P.Add((double[])mb.Clone()); link.P.Add((double[])gf.Clone()); A.Add(link);
                    Log.AppendLine("added a " + md.ToString("F1", IC) + " m link drive from " + src.Name + " to " + (gt.Ref ? "existing u" + gt.Row[1] : gt.Name));
                }
                else if (c.Value.All(z => z.Ref) && DxfOnly) { foreach (var z in c.Value) A.Remove(z); Log.AppendLine("left out loose existing airway(s): " + string.Join(", ", c.Value.Select(z => "u" + z.Row[1]))); }
                else if (c.Value.All(z => !z.Ref) && !DxfOnly) { foreach (var z in c.Value) A.Remove(z); Log.AppendLine("LEFT OUT (no drive within " + md.ToString("F1", IC) + " m to join to): " + string.Join(", ", c.Value.Select(z => z.Name + " [" + z.Act + "]").Distinct())); }
            }
            continue;
        }
        Log.AppendLine("airways " + nAir + ", nodes " + id.Count + ", connected pieces " + cs.Count + "; pieces open to surface: " + string.Join(" + ", surfSet.Where(comp.ContainsKey).Select(x => comp[x].Count)) + " of " + A.Count + " strings/airways");
        foreach (var c in cs) if (!surfSet.Contains(c.Key))
        {
            // closest gap from this piece to the surface-connected network
            double md = 1e18; string gw = ""; double[] mb = null;
            foreach (var a in c.Value) foreach (var p in a.P) foreach (var sk in surfSet) if (comp.ContainsKey(sk)) foreach (var b in comp[sk]) for (int k = 0; k + 1 < b.P.Count; k++) { double t; var f = Foot(p, b.P[k], b.P[k + 1], out t); double d = D(p, f); if (d < md) { md = d; gw = (a.Ref ? "existing u" + a.Row[1] : a.Name) + " -> " + (b.Ref ? "existing u" + b.Row[1] : b.Name); mb = p; } }
            var names = c.Value.Select(z => z.Ref ? "existing u" + z.Row[1] : z.Name + " [" + z.Act + "]").Distinct().ToList();
            Log.AppendLine("  not connected to surface (" + names.Count + " strings; nearest gap " + md.ToString("F1", IC) + " m: " + gw + " at " + mb[0].ToString("F0", IC) + "," + mb[1].ToString("F0", IC) + "," + mb[2].ToString("F0", IC) + "): " + string.Join(", ", names.Take(6)) + (names.Count > 6 ? " ..." : ""));
        }
        }

        // ---- rows
        var ratio = new Dictionary<string, double[]> { { "19", new[] { 0.961146, 0.935526 } }, { "24", new[] { 0.969015, 0.942433 } }, { "25", new[] { 0.967889, 0.941386 } }, { "17", new[] { 0.952032, 0.928362 } }, { "23", new[] { 0.952032, 0.928362 } } };
        var secName = new Dictionary<string, int>(); { int s0 = L.IndexOf("SECONDARY"); for (int i = s0 + 1; !L[i].StartsWith("END"); i++) { var c = L[i].Split('\t'); if (c[1] != "" && !secName.ContainsKey(c[1])) secName[c[1]] = int.Parse(c[0]); } }
        // free secondary slots for types with no layer yet
        { int s0 = L.IndexOf("SECONDARY"); foreach (var nm in new[] { "FAR", "RBH" }) if (!secName.ContainsKey(nm)) for (int i = s0 + 1; !L[i].StartsWith("END"); i++) { var c = L[i].Split('\t'); if (c[1] == "") { c[1] = nm; L[i] = string.Join("\t", c); secName[nm] = int.Parse(c[0]); break; } } }
        // primary layers: SITE (W), MB (G), plus H and MID in free slots
        var primName = new Dictionary<string, int>(); { int p0 = L.IndexOf("PRIMARY"); for (int i = p0 + 1; !L[i].StartsWith("END"); i++) { var c = L[i].Split('\t'); if (c[1] != "" && !primName.ContainsKey(c[1])) primName[c[1]] = int.Parse(c[0]); }
            foreach (var nm in new[] { "H", "MID" }) if (!primName.ContainsKey(nm)) for (int i = p0 + 1; !L[i].StartsWith("END"); i++) { var c = L[i].Split('\t'); if (c[1] == "") { c[1] = nm; L[i] = string.Join("\t", c); primName[nm] = int.Parse(c[0]); break; } } }
        Func<VAir, int> Prim = s => { char c0 = s.Name.Length > 0 ? s.Name[0] : 'W'; return c0 == 'G' ? primName["MB"] : c0 == 'H' ? primName["H"] : c0 == 'M' ? primName["MID"] : primName["SITE"]; };

        var guids = new HashSet<string>();
        var outRows = new List<string>(); int u = maxU, rowNo = 0, split = 0; var typeCount = new Dictionary<string, int>();
        Action<string[], double[], double[], bool> Emit = (c, a, b, keepU) =>
        {
            double len = D(a, b);
            c[ix["X1"]] = N(a[0]); c[ix["Y1"]] = N(a[1]); c[ix["Z1"]] = N(a[2]); c[ix["X2"]] = N(b[0]); c[ix["Y2"]] = N(b[1]); c[ix["Z2"]] = N(b[2]); c[ix["Length"]] = N(len);
            if (c[ix["Resistance Type"]] == "0") { double k = double.Parse(c[ix["kFactor"]], IC), ar = double.Parse(c[ix["Area"]], IC), pe = double.Parse(c[ix["Perimeter"]], IC); double rr = k * len * pe / Math.Pow(ar, 3); c[ix["Resistance"]] = N(rr); c[ix["Resistance Total"]] = N(rr); }
            if (!keepU) { u++; c[1] = u.ToString(IC); c[ix["GUID"]] = Guid.NewGuid().ToString(); }
            if (!guids.Add(c[ix["GUID"]])) { c[ix["GUID"]] = Guid.NewGuid().ToString(); guids.Add(c[ix["GUID"]]); }
            c[ix["Stage"]] = "1"; c[ix["newstage"]] = "1"; c[ix["Quantity"]] = "0";
            // dead-end flag 262144 only where an end is a face
            long fl = long.Parse(c[ix["Branch Flags"]]); bool face = deg[K(a)] == 1 || deg[K(b)] == 1;
            if (!keepU && (portals.Contains(K(a)) || portals.Contains(K(b)))) { fl |= 4096; face = false; }
            if ((fl & 4096) == 0) { if (face) fl |= 262144; else fl &= ~262144L; }
            c[ix["Branch Flags"]] = fl.ToString(IC);
            rowNo++; c[0] = rowNo.ToString(IC); outRows.Add(string.Join("\t", c));
        };
        foreach (var a in A.Where(z => z.Ref)) for (int i = 1; i < a.P.Count; i++) { if (K(a.P[i - 1]) == K(a.P[i])) continue; if (i > 1) split++; Emit((string[])a.Row.Clone(), a.P[i - 1], a.P[i], i == 1); }
        var baseRow = byU["2966"];
        foreach (var s in A.Where(z => !z.Ref))
        {
            string[] t; string act = s.Act;
            if (act == "EWR") { t = (string[])byU["1663"].Clone(); t[ix["Branch Flags"]] = "0"; }
            else if (act == "SRH") { t = (string[])byU["1678"].Clone(); t[ix["Branch Flags"]] = "0"; }
            else
            {
                t = (string[])baseRow.Clone(); string[] p; if (!pre.TryGetValue(act, out p)) throw new Exception("no Ventsim tunnel preset for " + act);
                double w = double.Parse(p[4], IC), hh = double.Parse(p[3], IC); string shape = p[7]; double ar, pe;
                if (SizeOverride.ContainsKey(act)) { w = SizeOverride[act][0]; hh = SizeOverride[act][1]; }
                if (ratio.ContainsKey(shape)) { ar = ratio[shape][0] * w * hh; pe = ratio[shape][1] * 2 * (w + hh); }
                else if (shape == "26") { double r = w / 2; ar = 0.5 * 40 * r * r * Math.Sin(2 * Math.PI / 40); pe = 40 * 2 * r * Math.Sin(Math.PI / 40); }
                else { ar = w * hh; pe = 2 * (w + hh); }
                t[ix["Tunnel Type"]] = p[0]; t[ix["Shape Type"]] = shape; t[ix["shapetype52"]] = p[12]; t[ix["shapepresetindex"]] = p[13];
                t[ix["Width"]] = N(w); t[ix["Height"]] = N(hh); t[ix["Area"]] = N(ar); t[ix["Perimeter"]] = N(pe);
                t[ix["kFactor"]] = "0.012"; t[ix["Resistance Type"]] = "0"; t[ix["Branch Flags"]] = "0";
            }
            t[ix["Secondary Layer"]] = (secName.ContainsKey(act) ? secName[act] : 0).ToString(IC); t[ix["Primary Layer"]] = Prim(s).ToString(IC);
            t[ix["Branch Name"]] = s.Name; t[ix["Group"]] = "0"; t[ix["Fan Type"]] = "0"; t[ix["Notes"]] = "";
            for (int i = 1; i < s.P.Count; i++) { if (K(s.P[i - 1]) == K(s.P[i])) continue; Emit((string[])t.Clone(), s.P[i - 1], s.P[i], false); typeCount[act] = typeCount.ContainsKey(act) ? typeCount[act] + 1 : 1; }
        }
        Log.AppendLine("rows written: " + outRows.Count + " (existing " + (outRows.Count - typeCount.Values.Sum()) + " incl. " + split + " extra from splitting at new junctions; new " + typeCount.Values.Sum() + ")");
        Log.AppendLine("new airways by type: " + string.Join(", ", typeCount.OrderBy(k => k.Key).Select(k => k.Key + " " + k.Value)));

        // ---- assemble: MAIN rows, empty rollback, one stage, levels = ALL
        var outL = new List<string>(); outL.AddRange(L.Take(m + 1)); outL.AddRange(outRows); outL.AddRange(L.Skip(end));
        int st = outL.IndexOf("STAGES"); int stE = st + 1; while (!outL[stE].StartsWith("END\tSTAGES")) stE++;
        var keepSt = outL.Skip(st + 1).Take(stE - st - 1).Where(l => l.StartsWith("0\t") || l.StartsWith("1\t")).ToList();
        outL.RemoveRange(st + 1, stE - st - 1); outL.InsertRange(st + 1, keepSt);
        int lv = outL.IndexOf("LEVELS"); int lvE = lv + 1; while (!outL[lvE].StartsWith("END\tLEVELS")) lvE++;
        double zmin = A.SelectMany(z => z.P).Min(p => p[2]), zmax = A.SelectMany(z => z.P).Max(p => p[2]);
        // keep the model's own level list (the view can point at one of them); widen ALL and add one level for the deeper development
        { double lowest = 1e18; int lastId = 0;
          for (int i = lv + 1; i < lvE; i++) { var c = outL[i].Split('\t'); lastId = Math.Max(lastId, int.Parse(c[0])); lowest = Math.Min(lowest, double.Parse(c[2], IC)); if (c[1] == "ALL") { c[2] = N(Math.Floor(zmin) - 1); c[3] = N(Math.Max(double.Parse(c[3], IC), Math.Ceiling(zmax) + 1)); outL[i] = string.Join("\t", c); } }
          if (zmin < lowest) outL.Insert(lvE, (lastId + 1) + "\tFY27 development below " + Math.Round(lowest) + "\t" + N(Math.Floor(zmin) - 1) + "\t" + N(lowest)); }
        Log.AppendLine("stages kept: " + string.Join(" / ", keepSt.Select(l => l.Split('\t')[1])) + "; levels: ALL " + Math.Floor(zmin) + " to " + Math.Ceiling(zmax));
        if (write)
        {
            if (File.Exists(outVsm)) throw new Exception("output exists, not overwriting: " + outVsm);
            using (var fs = File.Create(outVsm)) using (var gz = new GZipStream(fs, CompressionLevel.Optimal)) using (var sw = new StreamWriter(gz, Encoding.Default)) sw.Write(string.Join(nl, outL));
            Log.AppendLine("written " + outVsm);
        }
        return Log.ToString();
    }
}
