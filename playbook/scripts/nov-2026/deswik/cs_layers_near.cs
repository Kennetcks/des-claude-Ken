// Read-only: every text within 60 m of MPA 07 whose text mentions Length, with layer, handle and type.
double cx = 8105, cy = 63100, r = 60;
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null) continue; var m = f.BoundingBox.Midpoint;
    if (Math.Abs(m.x - cx) > r || Math.Abs(m.y - cy) > r) continue;
    string t = f.IsMText ? f.asMText.TextString : (f.IsText ? f.asText.TextString : null);
    if (t == null || !t.Contains("Length")) continue;
    Out.AppendLine($"{(f.IsMText ? "MText" : "Text")} {f.Layer.Name} h={f.GetType().GetProperty("Handle")?.GetValue(f)} ({m.x:F1},{m.y:F1},{m.z:F1}) vis {f.Layer.Visible}: \"{t.Replace("\r\n", " / ")}\"");
}
return null;
