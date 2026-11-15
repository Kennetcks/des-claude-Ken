// CB labels on CABLEBOLTS to the highest RL of their cable group: cables within 10 m in plan and 6 m in height of the
// label (its level), label z set to the highest of them. Dry run unless APPLY.
const bool APPLY = false;
const string CB = "0\\DESIGN MASTER\\CABLEBOLTS";
var pts = new List<Deswik.Graphics.Geometry.Point>(); var labs = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || f.Layer.Name != CB) continue;
    if (f.IsPoint) pts.Add(f.asPoint.InsertionPoint); else if (f.IsMText || f.IsText) labs.Add(f);
}
// cable groups: cables chained within 2.5 m in plan and 3 m in height
int[] par = Enumerable.Range(0, pts.Count).ToArray();
int Find(int i) { while (par[i] != i) i = par[i] = par[par[i]]; return i; }
for (int i = 0; i < pts.Count; i++) for (int j = i + 1; j < pts.Count; j++)
    if (Math.Sqrt(Math.Pow(pts[i].x - pts[j].x, 2) + Math.Pow(pts[i].y - pts[j].y, 2)) < 2.5 && Math.Abs(pts[i].z - pts[j].z) < 3) par[Find(i)] = Find(j);
var groups = Enumerable.Range(0, pts.Count).GroupBy(Find).Select(g => g.Select(i => pts[i]).ToList()).ToList();
foreach (var f in labs)
{
    object t = f.IsMText ? (object)f.asMText : f.asText; var prop = t.GetType().GetProperty("InsertionPoint");
    var ip = (Deswik.Graphics.Geometry.Point)prop.GetValue(t);
    string txt = f.IsMText ? f.asMText.TextString : (t.GetType().GetProperty("TextString")?.GetValue(t) ?? "").ToString();
    double Dist(List<Deswik.Graphics.Geometry.Point> g) => g.Where(p => Math.Abs(p.z - ip.z) < 7).Select(p => Math.Sqrt(Math.Pow(p.x - ip.x, 2) + Math.Pow(p.y - ip.y, 2))).DefaultIfEmpty(1e9).Min();
    var grp = groups.OrderBy(Dist).First(); if (Dist(grp) > 8) grp = new List<Deswik.Graphics.Geometry.Point>();
    if (grp.Count == 0) { Out.AppendLine($"'{txt.Split('\r', '\n')[0]}' at ({ip.x:F1},{ip.y:F1},{ip.z:F2}): no cables nearby - left"); continue; }
    double top = grp.Max(p => p.z);
    Out.AppendLine($"'{txt.Split('\r', '\n')[0]}' at ({ip.x:F1},{ip.y:F1}) z {ip.z:F2} -> {top:F2} (highest of {grp.Count} cables)");
    if (APPLY && Math.Abs(top - ip.z) > 0.005) { prop.SetValue(t, new Deswik.Graphics.Geometry.Point(ip.x, ip.y, top)); t.GetType().GetMethod("Update", Type.EmptyTypes)?.Invoke(t, null); }
}
if (APPLY) App.Redraw(true);
return null;
