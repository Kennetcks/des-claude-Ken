// Screenshots of the EWY solids in the current Deswik legend. Display-only: everything outside a box around the EWY is hidden for the capture.
// For each view it also writes where each EWY string's mid-point lands on the image (for numbered labels). View + visibility restored at the end.
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
var seq = new[] { "45933C4", "45931DD", "45933B9", "45931DF", "458EDDC", "45931E0", "45931E1", "45931E2" };
var ewy = new HashSet<string>(seq);
var VIEWS = new[] { ("3 3D from SW", 225.0, 60.0) };
const int IW = 1800, IH = 1200;
double bx0 = 1e18, by0 = 1e18, bz0 = 1e18, bx1 = -1e18, by1 = -1e18, bz1 = -1e18;
var mids = new Dictionary<string, Deswik.Graphics.Geometry.Point>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || !f.IsPolyline || !ewy.Contains(f.Handle)) continue;
    var n = f.asPolyline.VertexCount; double sx = 0, sy = 0, sz = 0;
    for (int i = 0; i < n; i++) { var q = f.asPolyline.GetVertex(i); bx0 = Math.Min(bx0, q.x); by0 = Math.Min(by0, q.y); bz0 = Math.Min(bz0, q.z); bx1 = Math.Max(bx1, q.x); by1 = Math.Max(by1, q.y); bz1 = Math.Max(bz1, q.z); sx += q.x; sy += q.y; sz += q.z; }
    mids[f.Handle] = new Deswik.Graphics.Geometry.Point(sx / n, sy / n, sz / n);
}
double ewyCx = (bx0 + bx1) / 2, ewyCy = (by0 + by1) / 2, ewyZ0 = bz0, ewyZ1 = bz1;
double mg = 10; bx0 -= mg; by0 -= mg; bz0 -= 6; bx1 += mg; by1 += mg; bz1 += 6;
bool Inside(Deswik.Graphics.Primaries.Figure f)
{
    System.Collections.IEnumerable pts = null; if (f.IsPolyface) pts = (System.Collections.IEnumerable)f.asPolyface.VertexList; else if (f.IsPolyline) { var l = new List<double[]>(); for (int i = 0; i < f.asPolyline.VertexCount; i++) { var q = f.asPolyline.GetVertex(i); l.Add(new[] { q.x, q.y, q.z }); } return l.Any(p => p[0] > bx0 && p[0] < bx1 && p[1] > by0 && p[1] < by1 && p[2] > bz0 && p[2] < bz1); }
    if (pts == null) return false;
    foreach (var q in pts) { var ty = q.GetType(); double x = Convert.ToDouble(ty.GetProperty("x").GetValue(q)), y = Convert.ToDouble(ty.GetProperty("y").GetValue(q)), z = Convert.ToDouble(ty.GetProperty("z").GetValue(q)); if (x > bx0 && x < bx1 && y > by0 && y < by1 && z > bz0 && z < bz1) return true; }
    return false;
}
// keep: EWY solids, decline solids, and solids of drives that touch an EWY solid
(double[] lo, double[] hi) BB(Deswik.Graphics.Primaries.Figure f) { double x0 = 1e18, y0 = 1e18, z0 = 1e18, x1 = -1e18, y1 = -1e18, z1 = -1e18; foreach (var q in (System.Collections.IEnumerable)f.asPolyface.VertexList) { var ty = q.GetType(); double x = Convert.ToDouble(ty.GetProperty("x").GetValue(q)), y = Convert.ToDouble(ty.GetProperty("y").GetValue(q)), z = Convert.ToDouble(ty.GetProperty("z").GetValue(q)); x0 = Math.Min(x0, x); y0 = Math.Min(y0, y); z0 = Math.Min(z0, z); x1 = Math.Max(x1, x); y1 = Math.Max(y1, y); z1 = Math.Max(z1, z); } return (new[] { x0, y0, z0 }, new[] { x1, y1, z1 }); }
bool Touch((double[] lo, double[] hi) a, (double[] lo, double[] hi) b, double tol) { for (int i = 0; i < 3; i++) if (a.lo[i] > b.hi[i] + tol || b.lo[i] > a.hi[i] + tol) return false; return true; }
var cand = new List<(Deswik.Graphics.Primaries.Figure f, (double[] lo, double[] hi) bb)>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (!f.Deleted && f.Visible && f.Layer != null && f.Layer.Name == "SCHEDULER\\TASK SOLIDS" && f.IsPolyface && A(f, "DIS Activity Type") == "Tunnel" && Inside(f)) cand.Add((f, BB(f)));
var ewyBB = cand.Where(x => ewy.Contains(A(x.f, "DIS Parent ID"))).Select(x => x.bb).ToList();
var touching = new HashSet<string>(cand.Where(x => !ewy.Contains(A(x.f, "DIS Parent ID")) && A(x.f, "ACT_TYPE") != "DEC" && ewyBB.Any(e => Touch(x.bb, e, 0.5))).Select(x => A(x.f, "DIS Parent ID")));
var keepRoutes = new HashSet<string> { "from W6368 ACC", "from W6328 ACC", "from G6370 MLA", "from G6350 MLA" };
var pathPts = System.IO.File.ReadAllLines(@"C:\dswtmp\EWY_paths.tsv").Skip(1).Select(l => l.Split('\t')).Where(r => keepRoutes.Contains(r[2])).ToList();
var routeStr = new HashSet<string>(pathPts.Select(r => r[7]).Where(h => !ewy.Contains(h)));
var keepSet = new HashSet<string>(cand.Where(x => ewy.Contains(A(x.f, "DIS Parent ID")) || A(x.f, "ACT_TYPE") == "DEC" || touching.Contains(A(x.f, "DIS Parent ID"))).Select(x => x.f.Handle));
// full solids of the route drives (not the decline), outside the box too; widen the zoom box to fit them
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || !f.Visible || f.Layer == null || f.Layer.Name != "SCHEDULER\\TASK SOLIDS" || !f.IsPolyface || A(f, "DIS Activity Type") != "Tunnel") continue;
    if (A(f, "ACT_TYPE") == "DEC")
    {   // full decline loop around the EWY (same RL range, within 50 m in plan of the EWY centre)
        var db = BB(f); double dcx = (db.lo[0] + db.hi[0]) / 2, dcy = (db.lo[1] + db.hi[1]) / 2, dcz = (db.lo[2] + db.hi[2]) / 2;
        if (Math.Abs(dcx - ewyCx) < 80 && Math.Abs(dcy - ewyCy) < 80 && dcz > ewyZ0 - 8 && dcz < ewyZ1 + 8) { keepSet.Add(f.Handle); bx0 = Math.Min(bx0, db.lo[0]); by0 = Math.Min(by0, db.lo[1]); bx1 = Math.Max(bx1, db.hi[0]); by1 = Math.Max(by1, db.hi[1]); }
        continue;
    }
    var pid = A(f, "DIS Parent ID"); bool extra = pid == "458EDB2" || pid == "458EE7E" || pid == "458EF8E";   // + W6346 RAD N, W6348 ACC (6346 level access)   // user asked for the 6330 MLA: GPS one is named G6320 MLA (RL 6332)
    if (!extra && (!routeStr.Contains(A(f, "DIS Parent ID")) || A(f, "ACT_TYPE") == "DDD")) continue;
    keepSet.Add(f.Handle); var b = BB(f); bx0 = Math.Min(bx0, b.lo[0]); by0 = Math.Min(by0, b.lo[1]); bx1 = Math.Max(bx1, b.hi[0]); by1 = Math.Max(by1, b.hi[1]);
}
// user: leave the W6306 EWD / EWR out of the picture
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) { if (f.Deleted || !keepSet.Contains(f.Handle)) continue; var nm = A(f, "Name").Trim(); if (nm.StartsWith("W6306 EWD") || nm.StartsWith("W6306 EWR")) keepSet.Remove(f.Handle); }
Out.AppendLine("route drives: " + string.Join(", ", routeStr.Select(h => cand.FirstOrDefault(x => A(x.f, "DIS Parent ID") == h).f?.AttributeValueGet("Name", "") ?? h)));
Out.AppendLine("touching drives: " + string.Join(", ", cand.Where(x => touching.Contains(A(x.f, "DIS Parent ID"))).Select(x => A(x.f, "Name")).Distinct()));
// label anchors: one per kept (non-decline) drive string, at its middle cut
var anchors = new List<(string name, string at, bool isEwy, Deswik.Graphics.Geometry.Point p)>();
var byPar = new Dictionary<string, List<(string name, string at, (double[] lo, double[] hi) bb)>>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || !keepSet.Contains(f.Handle)) continue; var at = A(f, "ACT_TYPE"); if (at == "DEC") continue;
    var par = A(f, "DIS Parent ID"); if (!byPar.ContainsKey(par)) byPar[par] = new List<(string, string, (double[], double[]))>(); byPar[par].Add((A(f, "Name"), at, BB(f)));
}
foreach (var kv in byPar) { var cs = kv.Value.OrderBy(x => x.bb.lo[0] + x.bb.lo[1] + x.bb.lo[2]).ToList(); var m = cs[cs.Count / 2]; anchors.Add((m.name, m.at, ewy.Contains(kv.Key), new Deswik.Graphics.Geometry.Point((m.bb.lo[0] + m.bb.hi[0]) / 2, (m.bb.lo[1] + m.bb.hi[1]) / 2, (m.bb.lo[2] + m.bb.hi[2]) / 2))); }
// one decline label at the lowest-RL kept decline cut on the near side is enough: pick the kept decline cut furthest from the EWY centre
Deswik.Graphics.Geometry.Point decP = null; double decBest = -1;
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) { if (f.Deleted || !keepSet.Contains(f.Handle) || A(f, "ACT_TYPE") != "DEC") continue; var b = BB(f); double cx = (b.lo[0] + b.hi[0]) / 2, cy = (b.lo[1] + b.hi[1]) / 2, cz = (b.lo[2] + b.hi[2]) / 2; double d = Math.Sqrt((cx - ewyCx) * (cx - ewyCx) + (cy - ewyCy) * (cy - ewyCy)) - Math.Abs(cz - (ewyZ0 + ewyZ1) / 2) * 0.3; if (d > decBest) { decBest = d; decP = new Deswik.Graphics.Geometry.Point(cx, cy, cz); } }
if (decP != null) anchors.Add(("Decline", "DEC", false, decP));
var hide = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) { if (f.Deleted || f.Layer == null || !f.Visible) continue; if (!keepSet.Contains(f.Handle)) hide.Add(f); }
Out.AppendLine("kept " + keepSet.Count + " solids");
var oldTarget = App.TargetPointView; var oldM = App.WorldToViewMatrix; double oldSize = App.ViewSize; var oldSel = App.Selections.MainSelection.Cast<Deswik.Graphics.Primaries.Figure>().ToList();
var lab = new StringBuilder("View\tHandle\tPX\tPY\n");
const int DEC_ALPHA = 55; var alphaOld = new Dictionary<string, object>(); var tmOld = new Dictionary<string, object>(); var tmProp = typeof(Deswik.Graphics.Primaries.Figure).GetProperty("TransparencyMethod");
try
{
    App.Selections.MainSelection.Clear(); foreach (var f in hide) f.Visible = false;
    // see-through decline: set alpha on the VectorDraw pen colour of the kept decline solids (display only, restored below)
    foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
    {
        if (f.Deleted || !keepSet.Contains(f.Handle) || A(f, "ACT_TYPE") != "DEC") continue;
        var vf = f.GetType().GetField("m_vdFigure", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public).GetValue(f);
        var pc = vf.GetType().GetProperty("PenColor").GetValue(vf); var ap = pc.GetType().GetProperty("AlphaBlending");
        if (ap == null) { Out.AppendLine("no AlphaBlending on vdColor: " + string.Join(",", pc.GetType().GetProperties().Select(p => p.Name))); break; }
        alphaOld[f.Handle] = ap.GetValue(pc); ap.SetValue(pc, Convert.ChangeType(DEC_ALPHA, ap.PropertyType)); tmOld[f.Handle] = tmProp.GetValue(f); tmProp.SetValue(f, Enum.Parse(tmProp.PropertyType, "KeepOwn")); f.Update();
    }
    Out.AppendLine("decline solids made see-through: " + alphaOld.Count);
    var c = new Deswik.Graphics.Geometry.Point((bx0 + bx1) / 2, (by0 + by1) / 2, (bz0 + bz1) / 2);
    var box = new Deswik.Graphics.Geometry.Box(new Deswik.Graphics.Geometry.Point(bx0, by0, bz0), new Deswik.Graphics.Geometry.Point(bx1, by1, bz1));
    var dir = @"C:\dswtmp\EWY_shots"; System.IO.Directory.CreateDirectory(dir);
    foreach (var v in VIEWS)
    {
        App.SetView(c, v.Item2, v.Item3, 0, true, false, false, false); App.ZoomToBox(box, 1.45, false); App.Redraw(true, true);
        var bmp = App.ViewToBitmap(System.Drawing.Color.White, IW, IH, 1f, false); bmp.Save(System.IO.Path.Combine(dir, v.Item1 + ".png"), System.Drawing.Imaging.ImageFormat.Png); bmp.Dispose();
        var M = App.WorldToViewMatrix; var tv = M.Transform(App.TargetPointView); double s = IH / App.ViewSize;
        foreach (var an in anchors) { var p = M.Transform(an.p); lab.Append(v.Item1 + "\tNAME|" + an.name + "|" + an.at + "|" + (an.isEwy ? "1" : "0") + "\t" + (IW / 2.0 + (p.x - tv.x) * s).ToString("F1") + "\t" + (IH / 2.0 - (p.y - tv.y) * s).ToString("F1") + "\n"); }
        foreach (var kv in mids) { var p = M.Transform(kv.Value); lab.Append(v.Item1 + "\t" + kv.Key + "\t" + (IW / 2.0 + (p.x - tv.x) * s).ToString("F1") + "\t" + (IH / 2.0 - (p.y - tv.y) * s).ToString("F1") + "\n"); }
        foreach (var r in pathPts) { var ic = System.Globalization.CultureInfo.InvariantCulture; var p = M.Transform(new Deswik.Graphics.Geometry.Point(double.Parse(r[4], ic), double.Parse(r[5], ic), double.Parse(r[6], ic))); lab.Append(v.Item1 + "\tPATH|" + r[0] + "|" + r[1] + "|" + r[2] + "|" + r[3] + "\t" + (IW / 2.0 + (p.x - tv.x) * s).ToString("F1") + "\t" + (IH / 2.0 - (p.y - tv.y) * s).ToString("F1") + "\n"); }
    }
    System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "labels.tsv"), lab.ToString());
    Out.AppendLine("hidden " + hide.Count + " figures; captured " + VIEWS.Length + " views; box " + (bx1 - bx0).ToString("F0") + "x" + (by1 - by0).ToString("F0") + "x" + (bz1 - bz0).ToString("F0"));
}
finally
{
    foreach (var f in hide) f.Visible = true;
    foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
    {
        if (f.Deleted || !alphaOld.ContainsKey(f.Handle)) continue;
        var vf = f.GetType().GetField("m_vdFigure", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public).GetValue(f);
        var pc = vf.GetType().GetProperty("PenColor").GetValue(vf); pc.GetType().GetProperty("AlphaBlending").SetValue(pc, alphaOld[f.Handle]); tmProp.SetValue(f, tmOld[f.Handle]); f.Update();
    }
    App.Selections.MainSelection.Clear(); App.Selections.MainSelection.AddItems(oldSel);
    App.SetView(oldTarget, oldM, true, false);
    var h = oldSize / 2; var rb = new Deswik.Graphics.Geometry.Box(new Deswik.Graphics.Geometry.Point(oldTarget.x - h, oldTarget.y - h, oldTarget.z - h), new Deswik.Graphics.Geometry.Point(oldTarget.x + h, oldTarget.y + h, oldTarget.z + h));
    App.ZoomToBox(rb, 1.0, false); App.SetView(oldTarget, oldM, true, false); double k = App.ViewSize / oldSize; if (k > 0) { App.ZoomToBox(rb, k, false); App.SetView(oldTarget, oldM, true, false); }
    App.Redraw(true, true);
    Out.AppendLine("view restored: size " + App.ViewSize.ToString("F0") + " (was " + oldSize.ToString("F0") + ")");
}
return null;
