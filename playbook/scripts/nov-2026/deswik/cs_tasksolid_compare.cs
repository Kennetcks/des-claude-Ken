// Read-only: compare current SCHEDULER\TASK SOLIDS against the latest snapshot in C:\dswtmp. Writes a report TSV. Nothing in Deswik changes.
if (!(App.Filename ?? "").Contains("06 Dec\\MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG_DOC " + App.Filename); return null; }
var snapPath = System.IO.Directory.GetFiles(@"C:\dswtmp", "TaskSolids_Snapshot_*.tsv").OrderBy(p => p).Last();
var lines = System.IO.File.ReadAllLines(snapPath);
var hdr = lines[0].Split('\t'); const int FIRST = 9; // KEY,Handle,Verts,bbox(6) then attributes
var snap = new Dictionary<string, string[]>();
foreach (var l in lines.Skip(1)) { var c = l.Split('\t'); snap[c[0]] = c; }
// Expected to change when geometry/section changes - reported separately, not as "lost"
var geom = new HashSet<string> { "VOLUME", "TONNES", "Insitu Tonnes", "Insitu Volume", "Diluted Tonnage", "Diluted Au_Oz", "Diluted Au_Grade", "VOID VOLUME", "<=0.5g/t DOM_DIL", "<=0.5g/t Tonnes", "<=0.5g/t Volume", ">0.5g/t DOM_DIL", ">0.5g/t Tonnes", ">0.5g/t Volume", "DOM_DIL", "AU", "AU_OK", "DENSITY", "EASTING", "NORTHING", "InterrogationDate", "HAUL_TOT", "HAUL_TOT KM", "Loading Dist", "ID", "ID_TEMP", "DIS Activity ID" };
string A(Deswik.Graphics.Primaries.Figure f, string k) { var v = f.AttributeValueGet(k, ""); if (v == null) return ""; var s = v is IFormattable fm ? fm.ToString(null, System.Globalization.CultureInfo.InvariantCulture) : v.ToString(); return s.Replace("\t", " ").Replace("\r", " ").Replace("\n", " "); }
bool Same(string a, string b) { if (a == b) return true; double x, y; var ci = System.Globalization.CultureInfo.InvariantCulture; return double.TryParse(a, System.Globalization.NumberStyles.Float, ci, out x) && double.TryParse(b, System.Globalization.NumberStyles.Float, ci, out y) && Math.Abs(x - y) <= 1e-6 * Math.Max(1, Math.Abs(x)); }
var seen = new HashSet<string>(); var rep = new StringBuilder("KIND\tKEY\tName\tAttribute\tBefore\tAfter\n");
var lost = new Dictionary<string, int>(); var changed = new Dictionary<string, int>(); var gchg = new Dictionary<string, int>(); int nNew = 0, nCur = 0, hnew = 0; var shp = new Dictionary<string, int>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || f.Layer.Name != "SCHEDULER\\TASK SOLIDS") continue; nCur++;
    var key = A(f, "DIS Parent ID") + "|" + A(f, "DIS Activity Number") + "|" + A(f, "DIS Activity Type");
    if (!snap.TryGetValue(key, out var old)) { nNew++; rep.Append("NEW\t" + key + "\t" + A(f, "Name") + "\t\t\t\n"); continue; }
    seen.Add(key);
    if (f.IsPolyface)
    {
        var vl = f.asPolyface.GetType().GetProperty("VertexList")?.GetValue(f.asPolyface) as System.Collections.IEnumerable; int nv = 0; double x0 = 1e18, y0 = 1e18, z0 = 1e18, x1 = -1e18, y1 = -1e18, z1 = -1e18;
        if (vl != null) foreach (var p in vl) { var t = p.GetType(); double x = Convert.ToDouble(t.GetProperty("x").GetValue(p)), y = Convert.ToDouble(t.GetProperty("y").GetValue(p)), z = Convert.ToDouble(t.GetProperty("z").GetValue(p)); nv++; x0 = Math.Min(x0, x); y0 = Math.Min(y0, y); z0 = Math.Min(z0, z); x1 = Math.Max(x1, x); y1 = Math.Max(y1, y); z1 = Math.Max(z1, z); }
        var ci2 = System.Globalization.CultureInfo.InvariantCulture; double P(int i) => double.Parse(old[i], ci2);
        bool gd = old[2] != nv.ToString() || (old[3] != "" && (Math.Abs(P(3) - x0) > 0.01 || Math.Abs(P(4) - y0) > 0.01 || Math.Abs(P(5) - z0) > 0.01 || Math.Abs(P(6) - x1) > 0.01 || Math.Abs(P(7) - y1) > 0.01 || Math.Abs(P(8) - z1) > 0.01));
        if (gd) { var at = A(f, "ACT_TYPE"); shp[at] = shp.TryGetValue(at, out var s0) ? s0 + 1 : 1; if (old[1] != f.Handle) hnew++; }
    }
    for (int i = FIRST; i < hdr.Length; i++)
    {
        var nm = hdr[i]; var b = i < old.Length ? old[i] : ""; var a = A(f, nm);
        if (Same(a, b)) continue;
        var d = geom.Contains(nm) ? gchg : (a == "" ? lost : changed);
        d[nm] = d.TryGetValue(nm, out var k0) ? k0 + 1 : 1;
        if (!geom.Contains(nm)) rep.Append((a == "" ? "LOST" : "CHANGED") + "\t" + key + "\t" + old[FIRST + Array.IndexOf(hdr, "Name") - FIRST] + "\t" + nm + "\t" + b + "\t" + a + "\n");
    }
}
var gone = snap.Keys.Where(k => !seen.Contains(k)).ToList();
foreach (var k in gone) rep.Append("GONE\t" + k + "\t" + snap[k][Array.IndexOf(hdr, "Name")] + "\t\t\t\n");
var outp = @"C:\dswtmp\TaskSolids_Compare_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".tsv";
System.IO.File.WriteAllText(outp, rep.ToString(), new UTF8Encoding(true));
Out.AppendLine("SNAPSHOT " + snapPath + "  (" + snap.Count + " solids)");
Out.AppendLine("CURRENT " + nCur + "  MATCHED " + seen.Count + "  NEW " + nNew + "  GONE " + gone.Count);
Out.AppendLine("REPORT " + outp);
Out.AppendLine("SHAPE_CHANGED " + shp.Values.Sum() + "  (new handle on " + hnew + ")");
foreach (var kv in shp.OrderBy(k => k.Key)) Out.AppendLine("SHAPE\t" + kv.Key + "\t" + kv.Value);
foreach (var kv in lost.OrderByDescending(k => k.Value)) Out.AppendLine("LOST\t" + kv.Key + "\t" + kv.Value);
foreach (var kv in changed.OrderByDescending(k => k.Value)) Out.AppendLine("CHANGED\t" + kv.Key + "\t" + kv.Value);
foreach (var kv in gchg.OrderByDescending(k => k.Value)) Out.AppendLine("GEOM\t" + kv.Key + "\t" + kv.Value);
return null;
