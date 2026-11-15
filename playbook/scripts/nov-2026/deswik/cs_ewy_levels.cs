// Display-only screenshots: long section of the top four EWY pieces, and plan views per level showing all development solids
// (incl. ACC / MLA) in an RL band. Writes label anchor points (string name + image px) for plain labels. View + visibility restored.
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
var ewy4 = new HashSet<string> { "45933C4", "45931DD", "45933B9", "45931DF" };
const int IW = 2000, IH = 1300;
// all scheduled tunnel task solids with bbox + centre
var sol = new List<(string h, string par, string name, string at, double[] lo, double[] hi)>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || f.Layer.Name != "SCHEDULER\\TASK SOLIDS" || !f.IsPolyface || A(f, "DIS Activity Type") != "Tunnel" || !f.Visible) continue;
    double x0 = 1e18, y0 = 1e18, z0 = 1e18, x1 = -1e18, y1 = -1e18, z1 = -1e18;
    foreach (var q in (System.Collections.IEnumerable)f.asPolyface.VertexList) { var ty = q.GetType(); double x = Convert.ToDouble(ty.GetProperty("x").GetValue(q)), y = Convert.ToDouble(ty.GetProperty("y").GetValue(q)), z = Convert.ToDouble(ty.GetProperty("z").GetValue(q)); x0 = Math.Min(x0, x); y0 = Math.Min(y0, y); z0 = Math.Min(z0, z); x1 = Math.Max(x1, x); y1 = Math.Max(y1, y); z1 = Math.Max(z1, z); }
    sol.Add((f.Handle, A(f, "DIS Parent ID"), A(f, "Name"), A(f, "ACT_TYPE"), new[] { x0, y0, z0 }, new[] { x1, y1, z1 }));
}
var e = sol.Where(s => ewy4.Contains(s.par)).ToList();
double ex0 = e.Min(s => s.lo[0]), ey0 = e.Min(s => s.lo[1]), ex1 = e.Max(s => s.hi[0]), ey1 = e.Max(s => s.hi[1]);
double cxE = (ex0 + ex1) / 2, cyE = (ey0 + ey1) / 2;
// window that reaches the ACCs (east) and G MLAs (south-west)
double wx0 = cxE - 85, wx1 = cxE + 175, wy0 = cyE - 95, wy1 = cyE + 75;
bool InWin((string h, string par, string name, string at, double[] lo, double[] hi) s) => s.hi[0] > wx0 && s.lo[0] < wx1 && s.hi[1] > wy0 && s.lo[1] < wy1;
var views = new List<(string file, double az, double dip, double z0, double z1, Func<(string h, string par, string name, string at, double[] lo, double[] hi), bool> keep)>();
// long section: EWY4 + decline + drives touching them + ACC/MLA on 6346-6400
views.Add(("LS", 270.0, 90.0, 6338, 6410, s => true));
views.Add(("L6390", 0.0, 0.0, 6386, 6410, s => true));
views.Add(("L6368", 0.0, 0.0, 6360, 6378, s => true));
views.Add(("L6350", 0.0, 0.0, 6338, 6358, s => true));
var oldTarget = App.TargetPointView; var oldM = App.WorldToViewMatrix; double oldSize = App.ViewSize; var oldSel = App.Selections.MainSelection.Cast<Deswik.Graphics.Primaries.Figure>().ToList();
var lab = new StringBuilder("View\tName\tAT\tEWY\tPX\tPY\n"); var dir = @"C:\dswtmp\EWY_levels"; System.IO.Directory.CreateDirectory(dir);
var allFigs = new List<Deswik.Graphics.Primaries.Figure>(); foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (!f.Deleted && f.Layer != null && f.Visible) allFigs.Add(f);
var hiddenNow = new HashSet<string>();
try
{
    App.Selections.MainSelection.Clear();
    foreach (var v in views)
    {
        // which solids: in window, overlapping the RL band (for LS: the band is the EWY4 RL range and a 40 m N-S slab around the EWY)
        var keep = sol.Where(s => InWin(s) && s.hi[2] > v.z0 && s.lo[2] < v.z1 && (v.file != "LS" || (s.hi[0] > cxE - 22 && s.lo[0] < cxE + 22 && s.hi[1] > ey0 - 25 && s.lo[1] < ey1 + 25))).ToList();
        if (v.file == "LS") keep = keep.Where(s => s.at != "SMP" && s.at != "ISP" && s.at != "OD").ToList();
        var keepH = new HashSet<string>(keep.Select(s => s.h));
        foreach (var f in allFigs) { bool want = keepH.Contains(f.Handle); if (!want && f.Visible) { f.Visible = false; hiddenNow.Add(f.Handle); } else if (want && !f.Visible) { f.Visible = true; hiddenNow.Remove(f.Handle); } }
        double bx0 = keep.Min(s => s.lo[0]), by0 = keep.Min(s => s.lo[1]), bz0 = keep.Min(s => s.lo[2]), bx1 = keep.Max(s => s.hi[0]), by1 = keep.Max(s => s.hi[1]), bz1 = keep.Max(s => s.hi[2]);
        var c = new Deswik.Graphics.Geometry.Point((bx0 + bx1) / 2, (by0 + by1) / 2, (bz0 + bz1) / 2);
        App.SetView(c, v.az, v.dip, 0, true, false, false, false);
        App.ZoomToBox(new Deswik.Graphics.Geometry.Box(new Deswik.Graphics.Geometry.Point(bx0, by0, bz0), new Deswik.Graphics.Geometry.Point(bx1, by1, bz1)), 1.08, false); App.Redraw(true, true);
        var bmp = App.ViewToBitmap(System.Drawing.Color.White, IW, IH, 1f, false); bmp.Save(System.IO.Path.Combine(dir, v.file + ".png"), System.Drawing.Imaging.ImageFormat.Png); bmp.Dispose();
        var M = App.WorldToViewMatrix; var tv = M.Transform(App.TargetPointView); double sc = IH / App.ViewSize;
        // one label per string: at the kept cut nearest the string's middle
        foreach (var g in keep.GroupBy(s => s.par))
        {
            var cs = g.OrderBy(s => s.lo[0] + s.lo[1]).ToList(); var m = cs[cs.Count / 2];
            var p = M.Transform(new Deswik.Graphics.Geometry.Point((m.lo[0] + m.hi[0]) / 2, (m.lo[1] + m.hi[1]) / 2, (m.lo[2] + m.hi[2]) / 2));
            lab.Append(v.file + "\t" + m.name + "\t" + m.at + "\t" + (ewy4.Contains(g.Key) ? "1" : "0") + "\t" + (IW / 2.0 + (p.x - tv.x) * sc).ToString("F1") + "\t" + (IH / 2.0 - (p.y - tv.y) * sc).ToString("F1") + "\n");
        }
        Out.AppendLine(v.file + ": " + keep.Count + " solids, " + keep.Select(s => s.par).Distinct().Count() + " strings");
    }
    System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "labels.tsv"), lab.ToString());
}
finally
{
    foreach (var f in allFigs) if (!f.Visible) f.Visible = true;
    App.Selections.MainSelection.Clear(); App.Selections.MainSelection.AddItems(oldSel);
    App.SetView(oldTarget, oldM, true, false); var hh = oldSize / 2;
    App.ZoomToBox(new Deswik.Graphics.Geometry.Box(new Deswik.Graphics.Geometry.Point(oldTarget.x - hh, oldTarget.y - hh, oldTarget.z - hh), new Deswik.Graphics.Geometry.Point(oldTarget.x + hh, oldTarget.y + hh, oldTarget.z + hh)), 1.0, false); App.Redraw(true, true);
}
return null;
