// MPA 07 W6390 RAD centreline: drop the leftover vertex at plan 6.99 m (old grade break) so the 1:8.8 is one segment.
// Reports how far it sits off the straight start -> grade-break line (plan and RL) before removing it. Dry run unless APPLY.
const bool APPLY = false;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
Deswik.Graphics.Primaries.Figure cl = null;
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
    if (!f.Deleted && f.Layer != null && f.Layer.Name == "0\\DESIGN DRAFT\\CENTERLINES" && f.IsPolyline && A(f, "Name") == "W6390 RAD" && A(f, "CLAUDE_MPA07") == "1") cl = f;
var pl = cl.asPolyline; var p = Enumerable.Range(0, pl.VertexCount).Select(i => { var q = pl.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
if (p.Count != 5) { Out.AppendLine($"RAD has {p.Count} vertices, expected 5 - nothing changed"); return null; }
double vx = p[2][0] - p[0][0], vy = p[2][1] - p[0][1], L = Math.Sqrt(vx * vx + vy * vy), t = ((p[1][0] - p[0][0]) * vx + (p[1][1] - p[0][1]) * vy) / (L * L);
double off = ((p[1][0] - p[0][0]) * vy - (p[1][1] - p[0][1]) * vx) / L, dz = p[1][2] - (p[0][2] + t * (p[2][2] - p[0][2]));
Out.AppendLine($"vertex ({p[1][0]:F3},{p[1][1]:F3},{p[1][2]:F3}) is {off:F3} m off the line in plan, {dz:F3} m in RL; new segment {L:F2} m at 1:{L / (p[2][2] - p[0][2]):F2}");
if (Math.Abs(off) > 0.05 || Math.Abs(dz) > 0.01) { Out.AppendLine("not on the line - nothing changed"); return null; }
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }
p.RemoveAt(1);
var c = (Deswik.Graphics.Primaries.Figure)cl.Clone(); var np = c.asPolyline; bool closed = pl.Closed; while (np.VertexCount > 0) np.RemoveVertexAt(0);
foreach (var q in p) np.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]); np.Closed = closed; App.ModelSpace.Entities.AddItem(c, null, null, true, false); cl.Deleted = true;
App.Redraw(true); Out.AppendLine("applied"); return null;
