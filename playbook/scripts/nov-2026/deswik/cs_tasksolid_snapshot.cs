// Read-only: export every SCHEDULER\TASK SOLIDS figure's attributes to C:\dswtmp (TSV). Nothing in Deswik changes.
// Body of: object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out)
if (!(App.Filename ?? "").Contains("06 Dec\\MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG_DOC " + App.Filename); return null; }
var names = App.GetAttributesFromEntity().Select(a => a.Name).ToList();
string A(Deswik.Graphics.Primaries.Figure f, string k) { var v = f.AttributeValueGet(k, ""); if (v == null) return ""; var s = v is IFormattable fm ? fm.ToString(null, System.Globalization.CultureInfo.InvariantCulture) : v.ToString(); return s.Replace("\t", " ").Replace("\r", " ").Replace("\n", " "); }
var sb = new StringBuilder();
sb.AppendLine("KEY\tHandle\tVerts\tMinX\tMinY\tMinZ\tMaxX\tMaxY\tMaxZ\t" + string.Join("\t", names));
int n = 0; var keys = new Dictionary<string, int>(); var byType = new Dictionary<string, (int c, double m)>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || f.Layer.Name != "SCHEDULER\\TASK SOLIDS") continue;
    var key = A(f, "DIS Parent ID") + "|" + A(f, "DIS Activity Number") + "|" + A(f, "DIS Activity Type");
    keys[key] = keys.TryGetValue(key, out var kc) ? kc + 1 : 1;
    int nv = 0; double x0 = 1e18, y0 = 1e18, z0 = 1e18, x1 = -1e18, y1 = -1e18, z1 = -1e18;
    if (f.IsPolyface)
    {
        var vl = f.asPolyface.GetType().GetProperty("VertexList")?.GetValue(f.asPolyface) as System.Collections.IEnumerable;
        if (vl != null) foreach (var p in vl) { var t = p.GetType(); double x = Convert.ToDouble(t.GetProperty("x").GetValue(p)), y = Convert.ToDouble(t.GetProperty("y").GetValue(p)), z = Convert.ToDouble(t.GetProperty("z").GetValue(p)); nv++; x0 = Math.Min(x0, x); y0 = Math.Min(y0, y); z0 = Math.Min(z0, z); x1 = Math.Max(x1, x); y1 = Math.Max(y1, y); z1 = Math.Max(z1, z); }
    }
    var ci = System.Globalization.CultureInfo.InvariantCulture;
    string B(double v) => nv == 0 ? "" : v.ToString("F3", ci);
    sb.Append(key).Append('\t').Append(f.Handle).Append('\t').Append(nv).Append('\t').Append(B(x0)).Append('\t').Append(B(y0)).Append('\t').Append(B(z0)).Append('\t').Append(B(x1)).Append('\t').Append(B(y1)).Append('\t').Append(B(z1));
    foreach (var nm in names) sb.Append('\t').Append(A(f, nm));
    sb.AppendLine(); n++;
    var at = A(f, "ACT_TYPE"); double m; double.TryParse(A(f, "Metres"), System.Globalization.NumberStyles.Float, ci, out m);
    (int c, double m) cur = byType.TryGetValue(at, out var c0) ? c0 : (0, 0.0); byType[at] = (cur.c + 1, cur.m + m);
}
System.IO.Directory.CreateDirectory(@"C:\dswtmp");
var path = @"C:\dswtmp\TaskSolids_Snapshot_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".tsv";
System.IO.File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
Out.AppendLine("FILE " + path);
Out.AppendLine("SOLIDS " + n + "  ATTRS " + names.Count + "  UNIQUE_KEYS " + keys.Count + "  DUP_KEYS " + keys.Count(k => k.Value > 1));
foreach (var k in keys.Where(k => k.Value > 1).Take(10)) Out.AppendLine("  DUP " + k.Key + " x" + k.Value);
foreach (var kv in byType.OrderBy(k => k.Key)) Out.AppendLine("TYPE " + (kv.Key == "" ? "(blank)" : kv.Key) + "\t" + kv.Value.c + "\t" + kv.Value.m.ToString("F1"));
return null;
