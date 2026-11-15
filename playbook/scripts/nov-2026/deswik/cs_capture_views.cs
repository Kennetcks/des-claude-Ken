// Render Design Master views to PNG for the design standard. Each shot: hide the clutter layers, set the view
// (azimuth, dip) on a box around a centre, render 2400 x 1500 on white, save. The user's view and layer visibility are
// put back afterwards. Nothing in the drawing changes.
var OUTDIR = @"__OUTDIR__";
var shots = new (string file, double cx, double cy, double cz, double half, double az, double dip)[] { __SHOTS__ };
var hide = new[] { "0\\DESIGN MASTER\\STOPES", "0\\DESIGN MASTER\\STOPES\\STOPE DESIGN", "0\\SURVEY ASBUILT\\DEVELOPMENT\\FLOOR" };
App.GetView(out Deswik.Graphics.Geometry.Point tp0, out double az0, out double dip0, out double tw0); double size0 = App.ViewSize;
var was = new Dictionary<object, bool>();
foreach (var l in (System.Collections.IEnumerable)App.Layers)
{
    var t = l.GetType(); string name = (string)t.GetProperty("Name").GetValue(l);
    if (hide.Contains(name)) { was[l] = (bool)t.GetProperty("Visible").GetValue(l); t.GetProperty("Visible").SetValue(l, false); }
}
try
{
    foreach (var s in shots)
    {
        var c = new Deswik.Graphics.Geometry.Point(s.cx, s.cy, s.cz);
        App.SetView(c, s.az, s.dip, 0, true, false, true, true);
        App.ZoomToBox(new Deswik.Graphics.Geometry.Box(new Deswik.Graphics.Geometry.Point(s.cx - s.half * 1.6, s.cy - s.half, s.cz - s.half), new Deswik.Graphics.Geometry.Point(s.cx + s.half * 1.6, s.cy + s.half, s.cz + s.half)), 1.0, false);
        App.SetView(c, s.az, s.dip, 0, true, false, true, true);
        App.ViewSize = s.half * 2;
        App.Redraw(true);
        using (var bmp = App.ViewToBitmap(System.Drawing.Color.White, 2400, 1500, 200f, true)) bmp.Save(System.IO.Path.Combine(OUTDIR, s.file), System.Drawing.Imaging.ImageFormat.Png);
        Out.AppendLine("saved " + s.file);
    }
}
finally
{
    foreach (var kv in was) kv.Key.GetType().GetProperty("Visible").SetValue(kv.Key, kv.Value);
    App.SetView(tp0, az0, dip0, tw0, true, false, true, true); App.ViewSize = size0; App.Redraw(true);
}
return null;
