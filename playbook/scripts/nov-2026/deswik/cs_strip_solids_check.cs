// Strip solids layer: the faces were added with 0-based vertex numbers but Deswik's face list is 1-based, so shift every
// face by one (only when a face still references vertex 0), then validate and read back Deswik's volume.
const string target = "0\\REFERENCE\\STRIP STANDARDS\\SOLIDS";
int n = 0, ok = 0, shifted = 0; double tot = 0; var msgs = new List<string>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || f.Layer.Name != target || !f.IsPolyface) continue; n++;
    var pf = f.asPolyface; var fl = pf.FaceList.Cast<object>().Select(o => Convert.ToInt32(o)).ToList();
    bool zeroBased = Enumerable.Range(0, pf.FaceCount).Any(i => Enumerable.Range(0, 4).Any(k => fl[i * 5 + k] == 0));
    if (zeroBased) { for (int i = 0; i < pf.FaceCount; i++) pf.SetFaceIndexes(i, fl[i * 5] + 1, fl[i * 5 + 1] + 1, fl[i * 5 + 2] + 1, fl[i * 5 + 3] + 1); shifted++; }
    bool valid = pf.Validate(true, true, true, true, false, true, false, false);
    pf.ResetVolumeAndAreaCache(); double v = pf.Volume;
    if (valid && v > 0) ok++; else if (msgs.Count < 5) msgs.Add($"({f.BoundingBox.Midpoint.x:F1},{f.BoundingBox.Midpoint.y:F1}) valid={valid} vol={v:F2} {pf.ValidateMessage()}");
    tot += Math.Max(v, 0);
}
App.Redraw(true);
Out.AppendLine($"{n} solids, {shifted} face lists shifted, {ok} valid with positive volume, total {tot:F1} m3"); foreach (var m in msgs) Out.AppendLine(m);
return null;
