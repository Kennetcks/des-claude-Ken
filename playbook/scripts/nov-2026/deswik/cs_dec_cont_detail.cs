// Read-only: vertex-by-vertex geometry of 44BD9C0 and the draft MAIN DEC (4587A08): chainage, segment, azimuth, turn,
// implied radius, grade; the DSP / RAD centreline start against the draft decline (chainage, offset, dz); all DESIGN DRAFT
// WALLS / STRIPPING vertices for the decline, DSP and RAD; the decline control/grade/length figures on the Drafter layers.
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
List<double[]> P3(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
double D2(double[] a, double[] b) => Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2));
double Az(double[] a, double[] b) { var z = Math.Atan2(b[0] - a[0], b[1] - a[1]) * 180 / Math.PI; return z < 0 ? z + 360 : z; }
double Turn(double a1, double a2) { var d = a2 - a1; while (d > 180) d -= 360; while (d < -180) d += 360; return d; }
var byH = new Dictionary<string, Deswik.Graphics.Primaries.Figure>(StringComparer.OrdinalIgnoreCase);
var others = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null) continue; byH[f.Handle] = f;
    if (f.Layer.Name.StartsWith("0\\DESIGN DRAFT\\CENTERLINES\\") && (A(f, "Name") == "MAIN DEC" || A(f, "Name").StartsWith("W6390"))) others.Add(f);
}
void Geo(string h)
{
    var f = byH[h]; var p = P3(f); double ch = 0; Out.AppendLine($"=== {h} {A(f, "Name")} [{f.Layer.Name}] n={p.Count}");
    for (int i = 0; i < p.Count; i++)
    {
        string s = $"  {i,2} ({p[i][0]:F3},{p[i][1]:F3},{p[i][2]:F3}) ch {ch:F2}";
        if (i > 0) { var L = D2(p[i], p[i - 1]); s += $" seg {L:F2} az {Az(p[i - 1], p[i]):F2} gr 1:{L / (p[i - 1][2] - p[i][2]):F2}"; }
        if (i > 0 && i < p.Count - 1) { var t = Turn(Az(p[i - 1], p[i]), Az(p[i], p[i + 1])); var L = (D2(p[i], p[i - 1]) + D2(p[i + 1], p[i])) / 2; s += $" turn {t:F2}" + (Math.Abs(t) > 0.05 ? $" R~{L / (Math.Abs(t) * Math.PI / 180):F1}" : ""); }
        Out.AppendLine(s); if (i < p.Count - 1) ch += D2(p[i], p[i + 1]);
    }
}
Geo("44BD9C0"); Geo("4587A08");
var dec = P3(byH["4587A08"]);
(double s, double off, double dz) On(double[] q)
{
    double bs = 0, bd = 1e9, bo = 0, bz = 0, c0 = 0;
    for (int i = 1; i < dec.Count; i++)
    {
        double vx = dec[i][0] - dec[i - 1][0], vy = dec[i][1] - dec[i - 1][1], L = Math.Sqrt(vx * vx + vy * vy), t = Math.Max(0, Math.Min(1, ((q[0] - dec[i - 1][0]) * vx + (q[1] - dec[i - 1][1]) * vy) / (L * L)));
        double px = dec[i - 1][0] + t * vx, py = dec[i - 1][1] + t * vy, d = Math.Sqrt(Math.Pow(q[0] - px, 2) + Math.Pow(q[1] - py, 2));
        if (d < bd) { bd = d; bs = c0 + t * L; bo = ((q[0] - dec[i - 1][0]) * vy - (q[1] - dec[i - 1][1]) * vx) / L; bz = dec[i - 1][2] + t * (dec[i][2] - dec[i - 1][2]); }
        c0 += L;
    }
    return (bs, bo, q[2] - bz);
}
foreach (var h in new[] { "4588D89", "4588D8B", "4588CAC", "4588CAD", "4588D8A", "4588D8C", "4588CA8", "4588CA9", "4588CAA", "4588CAB", "4588CED", "4588CD8", "4588CD9", "4588CDC", "4588CDD" })
{
    var f = byH[h]; var p = P3(f); Out.AppendLine($"=== {h} {A(f, "Name")} [{f.Layer.Name}] n={p.Count} closed={f.asPolyline.Closed} ACT={A(f, "ACT_TYPE")} Desc={A(f, "Description")}");
    var idx = p.Count <= 24 ? Enumerable.Range(0, p.Count) : Enumerable.Range(0, p.Count).Where(i => i % 10 == 0 || i == p.Count - 1);
    foreach (var i in idx) { var o = On(p[i]); Out.AppendLine($"  {i,3} ({p[i][0]:F3},{p[i][1]:F3},{p[i][2]:F3}) decCh {o.s:F2} off {o.off:F2} dz {o.dz:F3}"); }
}
foreach (var f in others.Where(f => !f.Layer.Name.EndsWith("SOLIDS") && !f.Layer.Name.EndsWith("WALLS")))
{
    string s = $"--- {f.Handle} [{f.Layer.Name}] {A(f, "Name")} {f.GetType().Name}";
    if (f.IsText) s += $" '{f.asText.TextString}'"; else if (f.IsMText) s += $" '{f.asMText.TextString}'"; else if (f.IsPolyline) s += " " + string.Join(" ", P3(f).Take(6).Select(q => $"({q[0]:F2},{q[1]:F2},{q[2]:F2})"));
    Out.AppendLine(s);
}
return null;
