// MPA 07 W6390 RAD COG markers (NOTES: a 6 m line across the drive + "COG" text) for the new grade - one break, 1:8.8 to
// flat at plan 10.89 m: the first marker (45879C0/45879C1, at the old 6.99 m break) moves to the new break; the second
// (45879C2/45879C3, at the old 13.49 m break, no longer a change of grade) is deleted. Dry run unless APPLY.
const bool APPLY = false;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
Deswik.Graphics.Primaries.Figure cl = null; var byH = new Dictionary<string, Deswik.Graphics.Primaries.Figure>();
var hs = new[] { "45879C0", "45879C1", "45879C2", "45879C3" };
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null) continue;
    if (f.Layer.Name == "0\\DESIGN DRAFT\\CENTERLINES" && f.IsPolyline && A(f, "Name") == "W6390 RAD" && A(f, "CLAUDE_MPA07") == "1") cl = f;
    if (hs.Contains(f.Handle.ToString())) byH[f.Handle.ToString()] = f;
}
if (byH.Count != 4) { Out.AppendLine($"found {byH.Count} of 4 COG figures - nothing changed"); return null; }
// the new grade break = the vertex where the centreline turns flat
var pl = cl.asPolyline; var p = Enumerable.Range(0, pl.VertexCount).Select(i => pl.GetVertex(i)).ToList();
int k = Enumerable.Range(1, p.Count - 2).First(i => Math.Abs(p[i].z - p[i - 1].z) > 0.01 && Math.Abs(p[i + 1].z - p[i].z) < 0.001);
var nb = p[k]; var line = byH["45879C0"]; var ob = line.BoundingBox.Midpoint;
double dx = nb.x - ob.x, dy = nb.y - ob.y, dz = nb.z - ob.z;
Out.AppendLine($"COG 1 line centre ({ob.x:F2},{ob.y:F2},{ob.z:F2}) -> new break ({nb.x:F2},{nb.y:F2},{nb.z:F2}): move ({dx:F2},{dy:F2},{dz:F2}); COG 2 (old 13.49 m break) deleted");
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }
byH["45879C0"].Translate(dx, dy, dz); byH["45879C1"].Translate(dx, dy, dz);
byH["45879C2"].Deleted = true; byH["45879C3"].Deleted = true;
App.Redraw(true); Out.AppendLine("applied"); return null;
