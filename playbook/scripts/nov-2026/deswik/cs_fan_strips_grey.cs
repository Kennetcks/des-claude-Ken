// Recolour the 5 m transition panels on FAN STRIP TRANSITIONS light grey (pen and hatch). High sections stay as they are.
const string dst = "0\\REFERENCE\\STRIP STANDARDS\\FAN STRIP TRANSITIONS";
var grey = System.Drawing.Color.FromArgb(192, 192, 192);
int n = 0;
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Layer == null || f.Layer.Name != dst || !f.IsPolyline) continue;
    var p = f.asPolyline; var a = p.GetVertex(0); var b = p.GetVertex(1);
    double run = Math.Sqrt((b.x - a.x) * (b.x - a.x) + (b.y - a.y) * (b.y - a.y));
    if (Math.Abs(run - 5.0) > 0.1) continue;               // transition panels are 5 m long in plan
    f.PenColor = new Deswik.Graphics.Objects.Color(grey);
    var hp = p.HatchProperties;
    if (hp != null)
        foreach (var pr in hp.GetType().GetProperties().Where(x => x.CanWrite && x.PropertyType == typeof(Deswik.Graphics.Objects.Color)))
        {
            var old = pr.GetValue(hp) as Deswik.Graphics.Objects.Color;
            var c = new Deswik.Graphics.Objects.Color(grey);
            var alpha = old?.GetType().GetProperty("AlphaBlending");
            if (alpha != null && alpha.CanWrite) alpha.SetValue(c, alpha.GetValue(old));
            pr.SetValue(hp, c);
        }
    p.Update(); n++;
}
App.Redraw(true);
Out.AppendLine($"Recoloured {n} transition panels light grey");
return null;
