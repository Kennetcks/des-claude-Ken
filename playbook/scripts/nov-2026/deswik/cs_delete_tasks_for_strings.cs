// For the string handles in C:\dswtmp\create_tunnels_handles.txt: write their task solids' boxes to C:\dswtmp\TaskSolids_RebuildN.tsv
// (TaskSolids_Snapshot format, for cs_newstrings_apply.cs), then delete their IS tasks and their SCHEDULER\TASK SOLIDS + TASK POLYGONS.
// Run cs_schedule_snapshot.cs first. Refuses if any task has progress.
var ps = new HashSet<string>(System.IO.File.ReadAllText(@"C:\dswtmp\create_tunnels_handles.txt").Split(new[] { ",", " ", "\r", "\n", "\t" }, StringSplitOptions.RemoveEmptyEntries));
string F(Deswik.Scheduler.Engine.Task t, string f) => t.GetFieldValueAndCacheIt(f) ?? "";
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
var del = Schedule.Tasks.Where(t => !t.Deleted && ps.Contains(F(t, "DIS Parent ID"))).ToList();
if (del.Any(t => t.PercentageComplete > 0)) { Out.AppendLine("tasks with progress - nothing changed"); return null; }
var ci = System.Globalization.CultureInfo.InvariantCulture; var sb = new StringBuilder("KEY\tHandle\tVerts\tMinX\tMinY\tMinZ\tMaxX\tMaxY\tMaxZ\tDIS Parent ID\tDIS Activity Number\tDIS Activity Type\n");
var figs = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || !(f.Layer.Name == "SCHEDULER\\TASK SOLIDS" || f.Layer.Name == "SCHEDULER\\TASK POLYGONS") || !ps.Contains(A(f, "DIS Parent ID"))) continue; figs.Add(f);
    if (f.Layer.Name != "SCHEDULER\\TASK SOLIDS" || !f.IsPolyface) continue;
    var p = A(f, "DIS Parent ID"); var no = A(f, "DIS Activity Number"); var ty2 = A(f, "DIS Activity Type");
    var vl = f.asPolyface.GetType().GetProperty("VertexList")?.GetValue(f.asPolyface) as System.Collections.IEnumerable; var lo = new[] { 1e18, 1e18, 1e18 }; var hi = new[] { -1e18, -1e18, -1e18 }; int nv = 0;
    foreach (var q in vl) { var t = q.GetType(); var v = new[] { Convert.ToDouble(t.GetProperty("x").GetValue(q)), Convert.ToDouble(t.GetProperty("y").GetValue(q)), Convert.ToDouble(t.GetProperty("z").GetValue(q)) }; nv++; for (int i = 0; i < 3; i++) { lo[i] = Math.Min(lo[i], v[i]); hi[i] = Math.Max(hi[i], v[i]); } }
    sb.Append($"{p}|{no}|{ty2}\t{f.Handle}\t{nv}\t{lo[0].ToString("F3", ci)}\t{lo[1].ToString("F3", ci)}\t{lo[2].ToString("F3", ci)}\t{hi[0].ToString("F3", ci)}\t{hi[1].ToString("F3", ci)}\t{hi[2].ToString("F3", ci)}\t{p}\t{no}\t{ty2}\n");
}
System.IO.File.WriteAllText(@"C:\dswtmp\TaskSolids_RebuildN.tsv", sb.ToString(), new UTF8Encoding(true));
foreach (var t in del) Schedule.Delete(t);
foreach (var f in figs) f.Deleted = true;
App.Redraw(true); Out.AppendLine($"deleted {del.Count} tasks and {figs.Count} task solids/polygons for {ps.Count} strings; solids box file written");
return null;
