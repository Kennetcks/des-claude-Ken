// Read-only: for each LH_Stope task solid, find overlapping development (Tunnel) task solids and the overlap volume.
// Writes C:\dswtmp\Stope_Overlaps.tsv (StopeHandle, StopeName, DevHandle, DevName, OverlapM3).
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
var stopes = new List<(Deswik.Graphics.Primaries.Figure f, double[] lo, double[] hi)>(); var devs = new List<(Deswik.Graphics.Primaries.Figure f, double[] lo, double[] hi)>();
(double[], double[]) Box(Deswik.Graphics.Primaries.Figure f) { var vl = f.asPolyface.GetType().GetProperty("VertexList")?.GetValue(f.asPolyface) as System.Collections.IEnumerable; double x0 = 1e18, y0 = 1e18, z0 = 1e18, x1 = -1e18, y1 = -1e18, z1 = -1e18; foreach (var q in vl) { var ty = q.GetType(); double x = Convert.ToDouble(ty.GetProperty("x").GetValue(q)), y = Convert.ToDouble(ty.GetProperty("y").GetValue(q)), z = Convert.ToDouble(ty.GetProperty("z").GetValue(q)); x0 = Math.Min(x0, x); y0 = Math.Min(y0, y); z0 = Math.Min(z0, z); x1 = Math.Max(x1, x); y1 = Math.Max(y1, y); z1 = Math.Max(z1, z); } return (new[] { x0, y0, z0 }, new[] { x1, y1, z1 }); }
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || f.Layer.Name != "SCHEDULER\\TASK SOLIDS" || !f.IsPolyface) continue;
    var ty = A(f, "DIS Activity Type"); if (ty == "Solid" && A(f, "ACT_TYPE") == "LH_Stope") { var b = Box(f); stopes.Add((f, b.Item1, b.Item2)); } else if (ty == "Tunnel" && A(f, "SCHEDULED") == "1") { var b = Box(f); devs.Add((f, b.Item1, b.Item2)); }
}
bool Ov(double[] alo, double[] ahi, double[] blo, double[] bhi) => alo[0] < bhi[0] && blo[0] < ahi[0] && alo[1] < bhi[1] && blo[1] < ahi[1] && alo[2] < bhi[2] && blo[2] < ahi[2];
var sb = new StringBuilder("StopeHandle\tStope\tDevHandle\tDev\tOverlapM3\n"); int pairs = 0, fails = 0, withOv = 0; double tot = 0; var perType = new Dictionary<string, double>(); var top = new List<(string s, double v)>();
foreach (var s in stopes)
{
    double sv = 0;
    foreach (var d in devs)
    {
        if (!Ov(s.lo, s.hi, d.lo, d.hi)) continue; double v = 0; bool ok;
        try { ok = s.f.asPolyface.BooleanIntersectionVolume(d.f.asPolyface, out v, false); } catch { ok = false; }
        if (!ok) { fails++; sb.Append(s.f.Handle + "\t" + A(s.f, "Name") + "\t" + d.f.Handle + "\t" + A(d.f, "Name") + "\tFAILED\n"); continue; }
        if (v < 0.05) continue; pairs++; sv += v; var at = A(d.f, "ACT_TYPE"); perType[at] = perType.TryGetValue(at, out var x) ? x + v : v;
        sb.Append(s.f.Handle + "\t" + A(s.f, "Name") + "\t" + d.f.Handle + "\t" + A(d.f, "Name") + "\t" + v.ToString("F2") + "\n");
    }
    if (sv > 0) { withOv++; tot += sv; top.Add((A(s.f, "Name") + " [" + s.f.Handle + "]", sv)); }
}
System.IO.File.WriteAllText(@"C:\dswtmp\Stope_Overlaps.tsv", sb.ToString(), new UTF8Encoding(true));
Out.AppendLine("stope solids " + stopes.Count + "  dev solids " + devs.Count + "  stopes overlapping dev " + withOv + "  overlapping pairs " + pairs + "  failed checks " + fails + "  total overlap " + tot.ToString("F0") + " m3");
Out.AppendLine("overlap by dev type: " + string.Join(", ", perType.OrderByDescending(k => k.Value).Select(k => k.Key + " " + k.Value.ToString("F0") + " m3")));
foreach (var t in top.OrderByDescending(t => t.v).Take(10)) Out.AppendLine("  " + t.s + "  " + t.v.ToString("F1") + " m3");
return null;
