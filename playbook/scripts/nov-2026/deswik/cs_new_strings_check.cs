// Read-only: check strings on COMBINED 12.11\NEW — attributes, and which existing COMBINED 12.11 strings they overlap (replace) or not (new).
var ci = System.Globalization.CultureInfo.InvariantCulture;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
var LN = "0\\06 SITE\\03 ENGINEERING\\01 DEV STRINGS\\COMBINED 12.11\\NEW"; var LC = "0\\06 SITE\\03 ENGINEERING\\01 DEV STRINGS\\COMBINED 12.11";
List<double[]> P(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
double Len(List<double[]> p) { double s = 0; for (int i = 1; i < p.Count; i++) s += Math.Sqrt(Math.Pow(p[i][0] - p[i - 1][0], 2) + Math.Pow(p[i][1] - p[i - 1][1], 2) + Math.Pow(p[i][2] - p[i - 1][2], 2)); return s; }
List<double[]> Sample(List<double[]> p, double step) { var r = new List<double[]>(); for (int i = 1; i < p.Count; i++) { double L = Math.Sqrt(Math.Pow(p[i][0] - p[i - 1][0], 2) + Math.Pow(p[i][1] - p[i - 1][1], 2) + Math.Pow(p[i][2] - p[i - 1][2], 2)); int n = Math.Max(1, (int)Math.Ceiling(L / step)); for (int k = 0; k < n; k++) { double t = (double)k / n; r.Add(new[] { p[i - 1][0] + t * (p[i][0] - p[i - 1][0]), p[i - 1][1] + t * (p[i][1] - p[i - 1][1]), p[i - 1][2] + t * (p[i][2] - p[i - 1][2]) }); } } r.Add(p[p.Count - 1]); return r; }
double Dist(double[] q, List<double[]> pl) { double b = 1e9; for (int i = 1; i < pl.Count; i++) { double vx = pl[i][0] - pl[i - 1][0], vy = pl[i][1] - pl[i - 1][1], vz = pl[i][2] - pl[i - 1][2], L2 = vx * vx + vy * vy + vz * vz, t = L2 == 0 ? 0 : Math.Max(0, Math.Min(1, ((q[0] - pl[i - 1][0]) * vx + (q[1] - pl[i - 1][1]) * vy + (q[2] - pl[i - 1][2]) * vz) / L2)); double dx = pl[i - 1][0] + t * vx - q[0], dy = pl[i - 1][1] + t * vy - q[1], dz = pl[i - 1][2] + t * vz - q[2]; b = Math.Min(b, Math.Sqrt(dx * dx + dy * dy + dz * dz)); } return b; }
var news = new List<Deswik.Graphics.Primaries.Figure>(); var olds = new List<(Deswik.Graphics.Primaries.Figure f, List<double[]> p, double[] bb)>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null) continue;
    if (f.Layer.Name == LN) news.Add(f);
    else if (f.Layer.Name == LC && f.IsPolyline) { var p = P(f); olds.Add((f, p, new[] { p.Min(q => q[0]) - 3, p.Min(q => q[1]) - 3, p.Min(q => q[2]) - 3, p.Max(q => q[0]) + 3, p.Max(q => q[1]) + 3, p.Max(q => q[2]) + 3 })); }
}
// which old strings have scheduled tasks
var parents = new Dictionary<string, int>(); foreach (var t in Schedule.Tasks) if (!t.Deleted && t.GetFieldValueAndCacheIt("DIS Activity Type") == "Tunnel") { var p = t.GetFieldValueAndCacheIt("DIS Parent ID"); parents[p] = parents.TryGetValue(p, out var c) ? c + 1 : 1; }
var req = new[] { "Name", "ACT_TYPE", "LEVEL", "MINE", "LAT_VERT", "STO_DEV", "SCHEDULED", "PROFILE", "GSS", "DIRECTION", "Description" };
var known = new HashSet<string>(olds.Select(o => A(o.f, "ACT_TYPE")));
// reference values per ACT_TYPE from existing strings (most common)
string Common(string at, string k) => olds.Where(o => A(o.f, "ACT_TYPE") == at).GroupBy(o => A(o.f, k)).OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault() ?? "";
Out.AppendLine("NEW strings: " + news.Count + " (" + news.Count(f => f.IsPolyline) + " polylines)");
int i0 = 0;
foreach (var f in news.OrderBy(f => A(f, "Name")))
{
    i0++;
    if (!f.IsPolyline) { Out.AppendLine("#" + i0 + " " + f.Handle + " NOT A POLYLINE"); continue; }
    var p = P(f); double L = Len(p); double dz = p[p.Count - 1][2] - p[0][2]; double hz = Math.Sqrt(Math.Pow(p[p.Count - 1][0] - p[0][0], 2) + Math.Pow(p[p.Count - 1][1] - p[0][1], 2));
    var issues = new List<string>();
    foreach (var k in req) if (A(f, k) == "") issues.Add(k + " blank");
    var at = A(f, "ACT_TYPE"); if (at != "" && !known.Contains(at)) issues.Add("ACT_TYPE '" + at + "' not used on any existing string");
    double lev; if (double.TryParse(A(f, "LEVEL"), System.Globalization.NumberStyles.Float, ci, out lev)) { var zmin = p.Min(q => q[2]); var zmax = p.Max(q => q[2]); if (A(f, "LAT_VERT") != "VERT" && (6000 + lev < zmin - 6 || 6000 + lev > zmax + 6)) issues.Add("LEVEL " + lev + " vs RL " + zmin.ToString("F0") + "-" + zmax.ToString("F0")); }
    bool steep = hz < 0.1 || Math.Abs(dz) / Math.Max(hz, 0.01) > 1.0; if (A(f, "LAT_VERT") == "LAT" && steep) issues.Add("LAT but string is near vertical"); if (A(f, "LAT_VERT") == "VERT" && !steep) issues.Add("VERT but string is flat-ish");
    var nm = A(f, "Name"); if (nm != "" && A(f, "LEVEL") != "" && !nm.Contains(A(f, "LEVEL"))) issues.Add("Name '" + nm + "' doesn't contain LEVEL " + A(f, "LEVEL"));
    if (nm != "" && at != "" && !nm.Contains(at)) issues.Add("Name doesn't contain ACT_TYPE");
    foreach (var k in new[] { "STO_DEV", "PROFILE", "GSS" }) { var cv = Common(at, k); if (cv != "" && A(f, k) != "" && A(f, k) != cv) issues.Add(k + "='" + A(f, k) + "' (others of " + at + " use '" + cv + "')"); }
    // overlap
    var s = Sample(p, 1.0); var hits = new List<string>();
    foreach (var o in olds)
    {
        if (!s.Any(q => q[0] > o.bb[0] && q[1] > o.bb[1] && q[2] > o.bb[2] && q[0] < o.bb[3] && q[1] < o.bb[4] && q[2] < o.bb[5])) continue;
        double inNew = s.Count(q => Dist(q, o.p) <= 1.5) / (double)s.Count; if (inNew < 0.05) continue;
        var so = Sample(o.p, 1.0); double inOld = so.Count(q => Dist(q, p) <= 1.5) / (double)so.Count;
        hits.Add(A(o.f, "Name") + " [" + o.f.Handle + ", " + Len(o.p).ToString("F0") + " m, tasks " + (parents.TryGetValue(o.f.Handle, out var tc) ? tc : 0) + "] covers " + (inNew * 100).ToString("F0") + "% of new / " + (inOld * 100).ToString("F0") + "% of old");
    }
    Out.AppendLine("#" + i0 + " " + nm + "  [" + f.Handle + "] " + at + " " + A(f, "LAT_VERT") + " L=" + L.ToString("F1") + " m, " + p.Count + " pts, SCH=" + A(f, "SCHEDULED") + " PROFILE=" + A(f, "PROFILE") + " GSS=" + A(f, "GSS"));
    Out.AppendLine("   " + (hits.Count == 0 ? "NEW (no overlap with existing strings)" : "OVERLAPS: " + string.Join(" | ", hits)));
    Out.AppendLine("   " + (issues.Count == 0 ? "attributes OK" : "CHECK: " + string.Join("; ", issues)));
}
return null;
