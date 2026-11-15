// Read-only: strings on COMBINED 15.11\UPDATED vs the scheduler. For each: task count, whether its centreline still sits in its own task
// solids (raise solids start ~5 m up at the drive back, so the bottom 5.5 m of a raise is ignored), solids that stray off the string;
// strings with no tasks (NEW), and tasks whose parent string no longer exists (orphans, any layer). Writes C:\dswtmp\updated_status.tsv.
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
string F(Deswik.Scheduler.Engine.Task t, string f) => t.GetFieldValueAndCacheIt(f) ?? "";
const string U = "0\\06 SITE\\03 ENGINEERING\\01 DEV STRINGS\\COMBINED 12.11\\COMBINED 15.11\\UPDATED";
var strs = new Dictionary<string, (Deswik.Graphics.Primaries.Figure f, List<double[]> p)>(); var live = new HashSet<string>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null) continue; live.Add(f.Handle);
    if (f.Layer.Name == U && f.IsPolyline) { var pl = f.asPolyline; strs[f.Handle] = (f, Enumerable.Range(0, pl.VertexCount).Select(i => { var q = pl.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList()); }
}
var boxes = new Dictionary<string, List<(double[] lo, double[] hi)>>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || f.Layer.Name != "SCHEDULER\\TASK SOLIDS" || !f.IsPolyface) continue; var pid = A(f, "DIS Parent ID"); if (!strs.ContainsKey(pid)) continue;
    var vl = f.asPolyface.GetType().GetProperty("VertexList")?.GetValue(f.asPolyface) as System.Collections.IEnumerable; var lo = new[] { 1e18, 1e18, 1e18 }; var hi = new[] { -1e18, -1e18, -1e18 };
    foreach (var q in vl) { var ty = q.GetType(); var v = new[] { Convert.ToDouble(ty.GetProperty("x").GetValue(q)), Convert.ToDouble(ty.GetProperty("y").GetValue(q)), Convert.ToDouble(ty.GetProperty("z").GetValue(q)) }; for (int i = 0; i < 3; i++) { lo[i] = Math.Min(lo[i], v[i]); hi[i] = Math.Max(hi[i], v[i]); } }
    if (!boxes.ContainsKey(pid)) boxes[pid] = new List<(double[], double[])>(); boxes[pid].Add((lo, hi));
}
var tasks = Schedule.Tasks.Where(t => !t.Deleted).GroupBy(t => F(t, "DIS Parent ID")).ToDictionary(g => g.Key, g => g.ToList());
double Gap(double[] q, (double[] lo, double[] hi) b) { double s = 0; for (int i = 0; i < 3; i++) { double d = Math.Max(0, Math.Max(b.lo[i] - q[i], q[i] - b.hi[i])); s += d * d; } return Math.Sqrt(s); }
var sb = new StringBuilder("Handle\tStatus\tMINE\tLEVEL\tACT_TYPE\tTasks\n"); int nC = 0, nN = 0;
foreach (var kv in strs.OrderBy(k => k.Key))
{
    var (f, p) = kv.Value; bool raise = A(f, "LAT_VERT") == "VERT" || new[] { "EWR", "RAR", "SRH", "RBH" }.Contains(A(f, "ACT_TYPE"));
    int nt = tasks.TryGetValue(kv.Key, out var tl) ? tl.Count : 0; string status;
    string info = $"{kv.Key} {A(f, "MINE")} {A(f, "LEVEL")} {A(f, "ACT_TYPE")} {A(f, "DIRECTION")}: tasks {nt}";
    if (!boxes.TryGetValue(kv.Key, out var bx)) { status = nt == 0 ? "NEW" : "NO_SOLIDS"; Out.AppendLine($"{info}, no solids   <-- {status}{(A(f, "STOPE TYPE") != "-" ? $" (STOPE TYPE '{A(f, "STOPE TYPE")}')" : "")}"); }
    else
    {
        double worst = 0, zmin = p.Min(q => q[2]);
        for (int i = 1; i < p.Count; i++) { double l = Math.Sqrt(Math.Pow(p[i][0] - p[i - 1][0], 2) + Math.Pow(p[i][1] - p[i - 1][1], 2) + Math.Pow(p[i][2] - p[i - 1][2], 2)); int n = Math.Max(1, (int)(l / 0.5)); for (int j = 0; j <= n; j++) { var q = new[] { p[i - 1][0] + (p[i][0] - p[i - 1][0]) * j / n, p[i - 1][1] + (p[i][1] - p[i - 1][1]) * j / n, p[i - 1][2] + (p[i][2] - p[i - 1][2]) * j / n }; if (raise && q[2] < zmin + 5.5) continue; worst = Math.Max(worst, bx.Min(b => Gap(q, b))); } }
        // plan distance from each solid box centre to the string, measured at the same RL along the string (raises) or in plan (drives)
        double over = 0; foreach (var b in bx) { var c = new[] { (b.lo[0] + b.hi[0]) / 2, (b.lo[1] + b.hi[1]) / 2, (b.lo[2] + b.hi[2]) / 2 }; double bd = 1e9; for (int i = 1; i < p.Count; i++) { double vx = p[i][0] - p[i - 1][0], vy = p[i][1] - p[i - 1][1], vz = p[i][2] - p[i - 1][2]; double t; if (raise && Math.Abs(vz) > 1e-6) t = Math.Max(0, Math.Min(1, (c[2] - p[i - 1][2]) / vz)); else { double L2 = vx * vx + vy * vy; t = L2 < 1e-12 ? 0 : Math.Max(0, Math.Min(1, ((c[0] - p[i - 1][0]) * vx + (c[1] - p[i - 1][1]) * vy) / L2)); } bd = Math.Min(bd, Math.Sqrt(Math.Pow(p[i - 1][0] + t * vx - c[0], 2) + Math.Pow(p[i - 1][1] + t * vy - c[1], 2))); } over = Math.Max(over, bd); }
        status = worst > 0.3 || (raise && over > 0.5) ? "CHANGED" : "OK";   // box-centre test only for raises: drive cuts that wrap a corner sit off the CL
        Out.AppendLine($"{info}, {bx.Count} solids, CL outside {worst:F2} m, solid centre off CL (plan) {over:F2} m" + (status == "CHANGED" ? "   <-- CHANGED" : ""));
    }
    if (status != "OK") { if (status == "NEW") nN++; else nC++; }
    sb.Append($"{kv.Key}\t{status}\t{A(f, "MINE")}\t{A(f, "LEVEL")}\t{A(f, "ACT_TYPE")}\t{nt}\n");
}
// orphans: tunnel tasks only (stope tasks S100/N100... have never had a CAD parent string)
var orph = tasks.Where(kv => kv.Key != "" && kv.Key != "/" && !live.Contains(kv.Key) && kv.Value.Any(t => F(t, "DIS Activity Type") == "Tunnel")).ToList();
foreach (var kv in orph) { Out.AppendLine($"ORPHAN parent {kv.Key}: {kv.Value.Count} tasks ({string.Join(", ", kv.Value.Select(t => t.Name).Distinct().Take(4))})"); sb.Append($"{kv.Key}\tORPHAN\t\t\t\t{kv.Value.Count}\n"); }
System.IO.File.WriteAllText(@"C:\dswtmp\updated_status.tsv", sb.ToString());
Out.AppendLine($"UPDATED strings {strs.Count}: changed {nC}, new {nN}; orphan parents {orph.Count}");
return null;
