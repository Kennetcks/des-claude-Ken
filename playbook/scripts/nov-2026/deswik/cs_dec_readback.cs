// Read-only after the rebuild: new MAIN DEC (joint, spacing, turns, grade), and the ties between decline walls,
// DSP / RAD walls and centrelines; DSP / RAD grades; back strip shared points; attributes kept.
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
List<double[]> P3(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
double D2(double[] a, double[] b) => Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2));
double D3(double[] a, double[] b) => Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2) + Math.Pow(a[2] - b[2], 2));
double Az(double[] a, double[] b) { var z = Math.Atan2(b[0] - a[0], b[1] - a[1]) * 180 / Math.PI; return z < 0 ? z + 360 : z; }
double Turn(double a1, double a2) { var d = a2 - a1; while (d > 180) d -= 360; while (d < -180) d += 360; return d; }
var H = new Dictionary<string, Deswik.Graphics.Primaries.Figure>(StringComparer.OrdinalIgnoreCase);
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (!f.Deleted) H[f.Handle] = f;
foreach (var h in new[] { "4587A08", "4588D8B", "4588CAC" }) Out.AppendLine($"old {h} deleted: {!H.ContainsKey(h)}");
var M = P3(H["44BD9C0"]); var d = P3(H["4588F43"]);
var segs = Enumerable.Range(1, d.Count - 1).Select(i => D2(d[i], d[i - 1])).ToList();
var turns = new List<double> { Turn(Az(M[^2], M[^1]), Az(d[0], d[1])) }; for (int i = 1; i < d.Count - 1; i++) turns.Add(Turn(Az(d[i - 1], d[i]), Az(d[i], d[i + 1])));
var gr = Enumerable.Range(1, d.Count - 1).Select(i => segs[i - 1] / (d[i - 1][2] - d[i][2])).ToList();
Out.AppendLine($"DEC 4588F43 [{H["4588F43"].Layer.Name}] Name={A(H["4588F43"], "Name")} ACT={A(H["4588F43"], "ACT_TYPE")} PROF={A(H["4588F43"], "PROFILE")}: {d.Count} pts, joint gap {D3(M[^1], d[0]) * 1000:F1} mm, seg {segs.Min():F3}-{segs.Max():F3} m, grade 1:{gr.Min():F3}-1:{gr.Max():F3}, turns " + string.Join(" ", turns.Select(t => t.ToString("F1"))));
Out.AppendLine($"44BD9C0 turns (last 8): " + string.Join(" ", Enumerable.Range(M.Count - 9, 8).Select(i => Turn(Az(M[i - 1], M[i]), Az(M[i], M[i + 1])).ToString("F1"))));
var cac = P3(H["4588F48"]); var cad = P3(H["4588F49"]); var rw = P3(H["4588F45"]); var dw = P3(H["4588F47"]); var rc = P3(H["4588F44"]); var dc = P3(H["4588F46"]);
Out.AppendLine($"ties (mm): RAD wall end-CAC start {D3(rw[^1], cac[0]) * 1000:F0}, CAC end-DSP wall start {D3(cac[^1], dw[0]) * 1000:F0}, DSP wall end-CAD start {D3(dw[^1], cad[0]) * 1000:F0}");
// offsets of wall points to the new CL
var CL = M.Take(M.Count - 1).Concat(d).ToList();
(double off, double dz) Off(double[] q) { double bd = 1e9, bo = 0, bz = 0; for (int i = 1; i < CL.Count; i++) { double vx = CL[i][0] - CL[i - 1][0], vy = CL[i][1] - CL[i - 1][1], L = Math.Sqrt(vx * vx + vy * vy), t = Math.Max(0, Math.Min(1, ((q[0] - CL[i - 1][0]) * vx + (q[1] - CL[i - 1][1]) * vy) / (L * L))); double dd = Math.Sqrt(Math.Pow(CL[i - 1][0] + t * vx - q[0], 2) + Math.Pow(CL[i - 1][1] + t * vy - q[1], 2)); if (dd < bd) { bd = dd; bo = ((q[0] - CL[i - 1][0]) * vy - (q[1] - CL[i - 1][1]) * vx) / L; bz = CL[i - 1][2] + t * (CL[i][2] - CL[i - 1][2]); } } return (bo, q[2] - bz); }
foreach (var (n, p) in new[] { ("CAC", cac), ("CAD", cad) }) { var o = p.Select(Off).ToList(); var sides = o.Where(x => Math.Abs(Math.Abs(x.off) - 2.75) < 0.3).ToList(); Out.AppendLine($"{n}: {p.Count} pts, |offset| {o.Min(x => Math.Abs(x.off)):F2}-{o.Max(x => Math.Abs(x.off)):F2} m, wall dz {sides.Min(x => x.dz):F3}..{sides.Max(x => x.dz):F3}"); }
Out.AppendLine($"RAD CL: start off {Off(rc[0]).off:F2} dz {Off(rc[0]).dz:F3}; legs 1:{D2(rc[0], rc[1]) / (rc[1][2] - rc[0][2]):F2} then {(Math.Abs(rc[2][2] - rc[1][2]) < 0.001 ? "flat" : "?")}, length {D2(rc[0], rc[1]) + D2(rc[1], rc[2]):F2} m; az {Az(rc[0], rc[1]):F2} vs decline there {Az(d[0], d[2]):F2}");
Out.AppendLine($"DSP CL: start off {Off(dc[0]).off:F2} dz {Off(dc[0]).dz:F3}; grade 1:{D2(dc[0], dc[1]) / (dc[1][2] - dc[0][2]):F1}, length {D2(dc[0], dc[1]):F2} m, angle to decline {Turn(142.968, Az(dc[0], dc[1])):F1}");
Out.AppendLine($"RAD wall 3 m-in points width {D2(rw[1], rw[6]):F2}, DSP wall end face {D2(dw[2], dw[3]):F2}, RAD end face {D2(rw[3], rw[4]):F2}");
double Sh(string a, string b) => P3(H[a]).Min(p => P3(H[b]).Min(q => D3(p, q)));
Out.AppendLine($"back strip shared (mm): RAD side {Sh("4588F4A", "4588F4C") * 1000:F0}/{Sh("4588F4C", "4588F4B") * 1000:F0}/{Sh("4588F4C", "4588F4D") * 1000:F0}, DSP side {Sh("4588F4E", "4588F50") * 1000:F0}/{Sh("4588F50", "4588F4F") * 1000:F0}/{Sh("4588F50", "4588F51") * 1000:F0}");
foreach (var h in new[] { "4588F4C", "4588F50" }) { var o = P3(H[h]).Select(Off).ToList(); Out.AppendLine($"{h} flat strip height above floor {o.Min(x => x.dz):F3}-{o.Max(x => x.dz):F3}, offset {o.Min(x => x.off):F2}..{o.Max(x => x.off):F2}"); }
foreach (var h in new[] { "4588F43", "4588F44", "4588F45", "4588F46", "4588F47", "4588F48", "4588F49", "4588F4A", "4588F4C", "4588F4E", "4588F50", "4588F52" }) Out.AppendLine($"  {h} [{H[h].Layer.Name}] Name={A(H[h], "Name")} ACT={A(H[h], "ACT_TYPE")} MPA07={A(H[h], "CLAUDE_MPA07")} closed={H[h].asPolyline.Closed}");
return null;
