// Make COMBINED 12.11 hold exactly the task-solid parent strings (tunnel tasks):
//  - parent strings on COMBINED 12.11\NEW or on OLD layers -> moved onto COMBINED 12.11 (same figure, task link kept)
//  - COMBINED 12.11 strings with no task solids -> deleted
//  - Name / LEVEL / MINE / ACT_TYPE on each kept string set from its task solid
// Writes a backup of every string touched (handle, layer, attributes, vertices) first.
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString().Trim();
const string MAIN = "0\\06 SITE\\03 ENGINEERING\\01 DEV STRINGS\\COMBINED 12.11";
var LT = App.Layers.FirstOrDefault(l => l.Name == MAIN); if (LT == null) { Out.AppendLine("layer not found - nothing done"); return null; }
var BACKUP = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "01 Planning & Scheduling", "3MS Backups", "COMBINED 12.11 strings before task match " + DateTime.Now.ToString("yyyyMMdd-HHmm") + ".tsv");
var fields = new[] { "Name", "LEVEL", "MINE", "ACT_TYPE" };
var task = new Dictionary<string, Deswik.Graphics.Primaries.Figure>();
var byH = new Dictionary<string, Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null) continue;
    if (f.IsPolyline) byH[f.Handle] = f;
    if (f.Layer.Name == "SCHEDULER\\TASK SOLIDS" && f.IsPolyface && A(f, "DIS Activity Type") == "Tunnel") { var p = A(f, "DIS Parent ID"); if (!task.ContainsKey(p)) task[p] = f; }
}
var onComb = byH.Values.Where(f => f.Layer.Name == MAIN || f.Layer.Name.StartsWith(MAIN + "\\")).ToList();
var toDelete = onComb.Where(f => !task.ContainsKey(f.Handle)).ToList();
var toKeep = task.Keys.Where(h => byH.ContainsKey(h)).Select(h => byH[h]).ToList();
var missing = task.Keys.Where(h => !byH.ContainsKey(h)).ToList();
// backup
var sb = new System.Text.StringBuilder("Handle\tAction\tLayer\tName\tLEVEL\tMINE\tACT_TYPE\tVertices\n");
foreach (var f in toDelete.Concat(toKeep).Distinct())
{
    var pl = f.asPolyline; var v = string.Join(";", Enumerable.Range(0, pl.VertexCount).Select(i => { var q = pl.GetVertex(i); return q.x.ToString("F3") + "," + q.y.ToString("F3") + "," + q.z.ToString("F3"); }));
    sb.Append(f.Handle + "\t" + (task.ContainsKey(f.Handle) ? "keep" : "delete") + "\t" + f.Layer.Name + "\t" + string.Join("\t", fields.Select(k => A(f, k))) + "\t" + v + "\n");
}
System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(BACKUP)); System.IO.File.WriteAllText(BACKUP, sb.ToString());
Out.AppendLine("backup: " + BACKUP);
// apply
int moved = 0, attr = 0; var movedFrom = new Dictionary<string, int>();
foreach (var f in toKeep)
{
    if (f.Layer != LT) { movedFrom[f.Layer.Name] = movedFrom.TryGetValue(f.Layer.Name, out var c) ? c + 1 : 1; f.Layer = LT; moved++; }
    var t = task[f.Handle]; bool ch = false;
    foreach (var k in fields) { var tv = A(t, k); if (tv != "" && tv != A(f, k)) { f.AttributeValueSet(k, tv); ch = true; } }
    if (ch) attr++;
}
foreach (var f in toDelete) { Out.AppendLine("deleted " + f.Handle + " '" + A(f, "Name") + "' from " + f.Layer.Name); f.Deleted = true; }
Out.AppendLine("moved onto COMBINED 12.11: " + moved); foreach (var kv in movedFrom) Out.AppendLine("  from " + kv.Key + ": " + kv.Value);
Out.AppendLine("strings with attributes updated: " + attr);
Out.AppendLine("deleted (no task): " + toDelete.Count);
Out.AppendLine("tunnel tasks with no parent string found: " + missing.Count + (missing.Count > 0 ? " -> " + string.Join(", ", missing) : ""));
return null;
