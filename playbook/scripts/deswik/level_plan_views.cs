// Level plan views: Top view (dip 0 = looking straight down, north up) with a clip box around each
// level's height band and plan extent, rendered to PNG. Changes only the camera and clip box, and
// restores both at the end. No schedule, layer or design data is touched. Images go to %TEMP%\dsw\levels.
// Each view: name, minX, maxX, minY, maxY, minZ, maxZ (Z band and extent measured from the task solids).
var views = new (string name, double x0, double x1, double y0, double y1, double z0, double z1)[] { __VIEWS__ };
double margin = 25;                       // plan margin around the level extent (m)
int width = 2400, height = 1500;
var outDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "dsw", "levels");
System.IO.Directory.CreateDirectory(outDir);

// Remember the current view and clip box so they can be put back.
Deswik.Graphics.Geometry.Point tp; double az, dip, tw, size;
App.GetView(out tp, out az, out dip, out tw, out size, false);
bool cbOn = App.ClipBoxEnabled; var cbC = App.ClipBoxCentre; var cbS = App.ClipBoxSize;
var cb = App.ClipBox; double cbDisp = cb != null ? cb.DisplaySize : 0; var cbFlag = cb != null ? cb.Draw3DFlag : default;

try
{
    foreach (var v in views)
    {
        double cx = (v.x0 + v.x1) / 2, cy = (v.y0 + v.y1) / 2, cz = (v.z0 + v.z1) / 2;
        App.SetView(new Deswik.Graphics.Geometry.Point(cx, cy, cz), 0, 0, 0, true, false, false, false);
        // Clip box keeps the level's height band; it is made large and centred well away from the level so its
        // handles (centre dot, arrows, outline) fall outside the image. The image frame is set by ViewSize below.
        double viewH = System.Math.Max(v.y1 - v.y0, (v.x1 - v.x0) * height / width) + 2 * margin;
        double reach = viewH * width / height + 500, off = 3000;
        App.ClipBoxCentre = new VectorDraw.Geometry.gPoint(cx + off, cy + off, cz);
        App.ClipBoxSize = new VectorDraw.Geometry.gPoint(2 * (off + reach), 2 * (off + reach), v.z1 - v.z0);
        App.ClipBoxEnabled = true;
        if (App.ClipBox != null) { App.ClipBox.ShowRotationManipulators(false); App.ClipBox.Draw3DFlag = EnumOf(cbFlag, "ExcludeFromList"); }   // hide the clip box handles in the image
        App.SetView(new Deswik.Graphics.Geometry.Point(cx, cy, cz), 0, 0, 0, true, false, false, false);   // set after the clip box, which re-centres the view
        App.ViewSize = viewH;
        App.Regen(true); App.Redraw(true, false);
        using (var bmp = App.ViewToBitmap(System.Drawing.Color.White, width, height, 150f, true))
        {
            var file = System.IO.Path.Combine(outDir, v.name + ".png");
            bmp.Save(file, System.Drawing.Imaging.ImageFormat.Png);
            Out.AppendLine($"Saved {file}  (RL {v.z0:F0}-{v.z1:F0})");
        }
    }
}
finally
{
    // Put the view and clip box back as they were.
    if (App.ClipBox != null) { App.ClipBox.DisplaySize = cbDisp; App.ClipBox.Draw3DFlag = cbFlag; App.ClipBox.ShowRotationManipulators(true); }
    if (cbC != null) App.ClipBoxCentre = cbC; if (cbS != null) App.ClipBoxSize = cbS;
    App.ClipBoxEnabled = cbOn;
    App.SetView(tp, az, dip, tw, true, false, false, false);
    App.ViewSize = size;
    App.Regen(true); App.Redraw(true, false);
    Out.AppendLine("View and clip box restored.");
}
return null;
static T EnumOf<T>(T like, string name) => (T)System.Enum.Parse(typeof(T), name);
