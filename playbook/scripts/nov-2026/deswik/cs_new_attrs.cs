// Fix attributes on COMBINED 12.11\NEW strings and decide replace vs new, using the current task solids.
// Replacement: >=80% of a parent's cut centres lie within 2 m of the new string, and >=70% of the new string lies within 3 m of those centres.
//   LEVEL, MINE, DRIVE NUMBER, COST_CENTRE, STO_DEV, LAT_VERT from the old string's first cut; blank Name/Description/DIRECTION from old.
// New: LEVEL, MINE, COST_CENTRE from the nearest lateral task solid at the take-off end (raises: bottom end), within 8 m. Blank Name built as <prefix><LEVEL> <ACT_TYPE> <DIRECTION or ->.
// User-set ACT_TYPE / PROFILE / GSS / non-blank Name are kept. Writes Replace_Plan.tsv.
const bool APPLY = false;
var ci = System.Globalization.CultureInfo.InvariantCulture;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
var LN = "0\\06 SITE\\03 ENGINEERING\\01 DEV STRINGS\\COMBINED 12.11\\NEW";
List<double[]> P(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
List<double[]> Sample(List<double[]> p, double step) { var r = new List<double[]>(); for (int i = 1; i < p.Count; i++) { double L = Math.Sqrt(Math.Pow(p[i][0] - p[i - 1][0], 2) + Math.Pow(p[i][1] - p[i - 1][1], 2) + Math.Pow(p[i][2] - p[i - 1][2], 2)); int n = Math.Max(1, (int)Math.Ceiling(L / step)); for (int k = 0; k < n; k++) { double t = (double)k / n; r.Add(new[] { p[i - 1][0] + t * (p[i][0] - p[i - 1][0]), p[i - 1][1] + t * (p[i][1] - p[i - 1][1]), p[i - 1][2] + t * (p[i][2] - p[i - 1][2]) }); } } r.Add(p[p.Count - 1]); return r; }
double Dist(double[] q, List<double[]> pl) { double b = 1e9; for (int i = 1; i < pl.Count; i++) { double vx = pl[i][0] - pl[i - 1][0], vy = pl[i][1] - pl[i - 1][1], vz = pl[i][2] - pl[i - 1][2], L2 = vx * vx + vy * vy + vz * vz, t = L2 == 0 ? 0 : Math.Max(0, Math.Min(1, ((q[0] - pl[i - 1][0]) * vx + (q[1] - pl[i - 1][1]) * vy + (q[2] - pl[i - 1][2]) * vz) / L2)); double dx = pl[i - 1][0] + t * vx - q[0], dy = pl[i - 1][1] + t * vy - q[1], dz = pl[i - 1][2] + t * vz - q[2]; b = Math.Min(b, Math.Sqrt(dx * dx + dy * dy + dz * dz)); } return b; }
double D3(double[] a, double[] b) => Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2) + Math.Pow(a[2] - b[2], 2));
var news = new List<(Deswik.Graphics.Primaries.Figure f, List<double[]> p)>(); var cuts = new List<(Deswik.Graphics.Primaries.Figure f, string parent, double[] c)>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null) continue;
    if (f.Layer.Name == LN && f.IsPolyline) news.Add((f, P(f)));
    else if (f.Layer.Name == "SCHEDULER\\TASK SOLIDS" && f.IsPolyface && A(f, "DIS Activity Type") == "Tunnel")
    {
        var vl = f.asPolyface.GetType().GetProperty("VertexList")?.GetValue(f.asPolyface) as System.Collections.IEnumerable; double x0 = 1e18, y0 = 1e18, z0 = 1e18, x1 = -1e18, y1 = -1e18, z1 = -1e18;
        foreach (var q in vl) { var ty = q.GetType(); double x = Convert.ToDouble(ty.GetProperty("x").GetValue(q)), y = Convert.ToDouble(ty.GetProperty("y").GetValue(q)), z = Convert.ToDouble(ty.GetProperty("z").GetValue(q)); x0 = Math.Min(x0, x); y0 = Math.Min(y0, y); z0 = Math.Min(z0, z); x1 = Math.Max(x1, x); y1 = Math.Max(y1, y); z1 = Math.Max(z1, z); }
        // floor-ish centre: use bbox centre in plan, bottom + 1 m for laterals
        cuts.Add((f, A(f, "DIS Parent ID"), new[] { (x0 + x1) / 2, (y0 + y1) / 2, A(f, "LAT_VERT") == "VERT" ? (z0 + z1) / 2 : z0 + 0.5 }));
    }
}
var byParent = cuts.GroupBy(c => c.parent).ToDictionary(g => g.Key, g => g.ToList());
var LC = "0\\06 SITE\\03 ENGINEERING\\01 DEV STRINGS\\COMBINED 12.11"; var oldStr = new Dictionary<string, List<double[]>>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (!f.Deleted && f.Layer != null && f.Layer.Name == LC && f.IsPolyline && byParent.ContainsKey(f.Handle)) oldStr[f.Handle] = P(f);
double Len(List<double[]> p) { double s = 0; for (int i = 1; i < p.Count; i++) s += D3(p[i], p[i - 1]); return s; }
double Num(string s) { double v; return double.TryParse(s, System.Globalization.NumberStyles.Float, ci, out v) ? v : 0; }
var proposed = new Dictionary<string, Dictionary<string, string>>();
string Prefix(string mine) => mine.StartsWith("G") ? "G" : mine == "MID" ? "M" : mine;
var plan = new StringBuilder("NewHandle\tOld\tDonorCut\n"); int changes = 0;
// order: strings whose donor is an existing solid first, then ones off other new strings
var order = news.OrderBy(n => A(n.f, "LAT_VERT") == "VERT" ? 1 : 0).ThenBy(n => A(n.f, "Name")).ToList();
foreach (var (f, p) in order)
{
    var s = Sample(p, 1.0); string repParent = null; double best = 0; double newLen = Len(p);
    foreach (var kv in byParent)
    {
        var cs = kv.Value; if (!cs.Any(c => D3(c.c, p[0]) < 300)) continue;
        double score = 0;
        if (oldStr.TryGetValue(kv.Key, out var op) && Len(op) > 2)
        {   // string geometry match
            double a1 = s.Count(q => Dist(q, op) <= 1.5) / (double)s.Count; var so = Sample(op, 1.0); double b1 = so.Count(q => Dist(q, p) <= 1.5) / (double)so.Count;
            if (a1 >= 0.8 && b1 >= 0.8) score = a1 + b1;
        }
        if (score == 0)
        {   // cut-centre match + length check
            double a = cs.Count(c => Dist(c.c, p) <= 2.5) / (double)cs.Count; double oldLen = cs.Sum(c => Num(A(c.f, "Metres")));
            if (a >= 0.8 && oldLen > 0 && newLen / oldLen > 0.6 && newLen / oldLen < 1.6) score = a + 0.5;
        }
        if (score > best) { best = score; repParent = kv.Key; }
    }
    var forced = new Dictionary<string, string> { { "459329F", "458EE85" } }; // W390 RAD W regrade (old string was split in CAD)
    if (forced.TryGetValue(f.Handle, out var fp) && byParent.ContainsKey(fp)) repParent = fp;
    var set = new Dictionary<string, string>(); string donor = "";
    if (repParent != null)
    {
        var first = byParent[repParent].OrderBy(c => { int v; return int.TryParse(A(c.f, "DIS Activity Number"), out v) ? v : 0; }).First().f;
        foreach (var k in new[] { "LEVEL", "MINE", "DRIVE NUMBER", "COST_CENTRE", "STO_DEV", "LAT_VERT" }) if (A(first, k) != "") set[k] = A(first, k);
        foreach (var k in new[] { "Name", "Description", "DIRECTION" }) if (A(f, k) == "" && A(first, k) != "") set[k] = A(first, k);
    }
    else
    {
        bool vert = A(f, "LAT_VERT") == "VERT"; var ends = vert ? new List<double[]> { p[0][2] <= p[p.Count - 1][2] ? p[0] : p[p.Count - 1] } : new List<double[]> { p[0], p[p.Count - 1] };
        Deswik.Graphics.Primaries.Figure d = null; double dd = 1e9;
        foreach (var e in ends) { foreach (var c in cuts) { if (A(c.f, "LAT_VERT") != "LAT") continue; var x = D3(e, c.c); if (x < dd && x <= 8) { dd = x; d = c.f; } } if (d != null) break; }
        if (d == null)
        {   // off another new string: use its proposed values
            foreach (var e in ends) foreach (var o in news) { if (o.f.Handle == f.Handle || A(o.f, "LAT_VERT") != "LAT" || !proposed.ContainsKey(o.f.Handle)) continue; var x = Dist(e, o.p); if (x < dd && x <= 6) { dd = x; donor = "NEW " + o.f.Handle; var pr = proposed[o.f.Handle]; set["LEVEL"] = pr["LEVEL"]; set["MINE"] = pr["MINE"]; if (pr.ContainsKey("COST_CENTRE")) set["COST_CENTRE"] = pr["COST_CENTRE"]; } }
            if (donor != "") { if (A(f, "STO_DEV") == "") set["STO_DEV"] = "DEV"; if (A(f, "Name") == "") set["Name"] = Prefix(set["MINE"]) + set["LEVEL"] + " " + A(f, "ACT_TYPE") + " " + (A(f, "DIRECTION") == "" ? "-" : A(f, "DIRECTION")); }
        }
        if (d != null)
        {
            donor = d.Handle; set["LEVEL"] = A(d, "LEVEL"); set["MINE"] = A(d, "MINE"); if (A(d, "COST_CENTRE") != "") set["COST_CENTRE"] = A(d, "COST_CENTRE");
            if (A(f, "STO_DEV") == "") set["STO_DEV"] = "DEV";
            if (A(f, "Name") == "") set["Name"] = Prefix(set["MINE"]) + set["LEVEL"] + " " + A(f, "ACT_TYPE") + " " + (A(f, "DIRECTION") == "" ? "-" : A(f, "DIRECTION"));
        }
    }
    var full = new Dictionary<string, string> { { "LEVEL", A(f, "LEVEL") }, { "MINE", A(f, "MINE") }, { "COST_CENTRE", A(f, "COST_CENTRE") } }; foreach (var kv in set) full[kv.Key] = kv.Value; proposed[f.Handle] = full;
    var diff = set.Where(kv => kv.Value != A(f, kv.Key)).ToList(); changes += diff.Count;
    var nm = set.ContainsKey("Name") ? set["Name"] : A(f, "Name"); var lv = set.ContainsKey("LEVEL") ? set["LEVEL"] : A(f, "LEVEL");
    var oldName = repParent != null ? A(byParent[repParent][0].f, "Name") : "";
    Out.AppendLine(f.Handle + "  " + (repParent != null ? "REPLACES parent " + repParent + " (" + byParent[repParent].Count + " cuts, " + oldName + ")" : donor.StartsWith("NEW ") ? "NEW, level from new string " + donor.Substring(4) : donor != "" ? "NEW, level from cut " + A(cuts.First(c => c.f.Handle == donor).f, "Name") + " [" + donor + "]" : "NEW, NO DRIVE WITHIN 8 m") + "  -> " + nm + "  LEVEL " + lv + "  MINE " + (set.ContainsKey("MINE") ? set["MINE"] : A(f, "MINE")) + (nm.Contains(lv) ? "" : "   ** name doesn't carry LEVEL"));
    foreach (var kv in diff) Out.AppendLine("     " + kv.Key + ": '" + A(f, kv.Key) + "' -> '" + kv.Value + "'");
    if (APPLY) foreach (var kv in diff) f.AttributeValueSet(kv.Key, kv.Value);
    plan.Append(f.Handle + "\t" + (repParent ?? "NEW") + "\t" + donor + "\n");
}
System.IO.File.WriteAllText(@"C:\dswtmp\Replace_Plan.tsv", plan.ToString(), new UTF8Encoding(true));
Out.AppendLine((APPLY ? "APPLIED " : "DRY RUN ") + changes + " attribute changes");
return null;
