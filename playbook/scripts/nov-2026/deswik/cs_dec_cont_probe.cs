// Read-only: figure 44BD9C0 (layer, attributes, ends, spacing, grade) and every figure on 0\DESIGN DRAFT (layer, name,
// vertices, plan length, segment spacing, grade, ends), so the draft decline can be checked as a continuation of 44BD9C0.
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
List<double[]> P3(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
double D2(double[] a, double[] b) => Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2));
string Pt(double[] q) => $"({q[0]:F2},{q[1]:F2},{q[2]:F2})";
string Desc(Deswik.Graphics.Primaries.Figure f)
{
    var s = $"{f.Handle} [{f.Layer.Name}] Name='{A(f, "Name")}' ACT={A(f, "ACT_TYPE")} PROF={A(f, "PROFILE")} MPA07={A(f, "CLAUDE_MPA07")}";
    if (!f.IsPolyline) return s + $" ({f.GetType().Name})";
    var p = P3(f); if (p.Count < 2) return s + " n=" + p.Count;
    var seg = Enumerable.Range(1, p.Count - 1).Select(i => D2(p[i], p[i - 1])).ToList(); double L = seg.Sum();
    double dz = p[0][2] - p[^1][2];
    return s + $" n={p.Count} closed={f.asPolyline.Closed} plan {L:F1} m, seg min/avg/max {seg.Min():F2}/{seg.Average():F2}/{seg.Max():F2}, dz {dz:F2} (1:{(Math.Abs(dz) > 0.01 ? L / dz : 0):F2}), ends {Pt(p[0])} -> {Pt(p[^1])}";
}
Out.AppendLine("file: " + App.Filename);
Deswik.Graphics.Primaries.Figure tgt = null;
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (!f.Deleted && string.Equals(f.Handle, "44BD9C0", StringComparison.OrdinalIgnoreCase)) tgt = f;
if (tgt == null) Out.AppendLine("44BD9C0 not found (or deleted)");
else
{
    Out.AppendLine("TARGET " + Desc(tgt));
    if (tgt.IsPolyline) { var p = P3(tgt); Out.AppendLine("  last 6: " + string.Join(" ", p.Skip(Math.Max(0, p.Count - 6)).Select(Pt))); Out.AppendLine("  first 3: " + string.Join(" ", p.Take(3).Select(Pt))); }
}
var draft = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (!f.Deleted && f.Layer != null && f.Layer.Name.StartsWith("0\\DESIGN DRAFT")) draft.Add(f);
Out.AppendLine($"DESIGN DRAFT figures: {draft.Count}");
foreach (var g in draft.GroupBy(f => f.Layer.Name).OrderBy(g => g.Key)) { Out.AppendLine($"== {g.Key}: {g.Count()}"); foreach (var f in g.Where(f => !f.IsText && !f.IsMText).Take(40)) Out.AppendLine("  " + Desc(f)); }
if (tgt != null && tgt.IsPolyline)
{
    var e = P3(tgt); var ends = new[] { e[0], e[^1] };
    foreach (var f in draft.Where(f => f.IsPolyline && f.asPolyline.VertexCount > 1)) { var p = P3(f); foreach (var q in new[] { p[0], p[^1] }) foreach (var t in ends) if (D2(q, t) < 10) Out.AppendLine($"near 44BD9C0 end {Pt(t)}: {f.Handle} {A(f, "Name")} end {Pt(q)} plan gap {D2(q, t):F3} dz {q[2] - t[2]:F3}"); }
}
return null;
