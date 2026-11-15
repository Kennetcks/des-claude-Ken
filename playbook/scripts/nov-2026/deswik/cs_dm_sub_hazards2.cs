// MA UG Design Master.duf: replace the G6390 SUB hazard strings (458373F, 4583740) and BHID labels (4583742, 4583741),
// generated on the old SUB position, with the ones regenerated in the Level Design Drafter on the new SUB
// (C:\dswtmp\sub_hazards_export.txt: WHRC23026, WHSH26001A, WHRC25035). New figures are copies of the Design Master SUB
// hazards (keeping Name G6390 SUB / ACT_TYPE SUB and the rest), with the draft geometry, BHID, GROUT_TYPE and DRILL_STATUS.
// Other hazards (G6390 ACC, MAIN DEC) are not touched. Old geometry to C:\dswtmp\dm_sub_hazards_backup2.txt. Dry run unless APPLY.
const bool APPLY = false;
var inv = System.Globalization.CultureInfo.InvariantCulture;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
if (!App.Filename.EndsWith("MA UG Design Master.duf")) { Out.AppendLine("WRONG WINDOW " + App.Filename); return null; }
var H = new Dictionary<string, Deswik.Graphics.Primaries.Figure>(StringComparer.OrdinalIgnoreCase); foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (!f.Deleted && f.Layer != null) H[f.Handle] = f;
var oldS = new[] { "45891E1", "45891E2", "45891E3" }; var oldT = new[] { "45891E4", "45891E5", "45891E6" };
foreach (var h in oldS.Concat(oldT)) if (!H.ContainsKey(h) || A(H[h], "Name") != "G6390 SUB") { Out.AppendLine(h + " missing or not a G6390 SUB hazard - nothing done"); return null; }
var rows = System.IO.File.ReadAllLines(@"C:\dswtmp\sub_hazards_export.txt").Where(l => l.Contains('\t')).Select(l => l.Split('\t')).ToList();
Dictionary<string, string> Attr(string[] r) => r[^1].Split('|').Where(s => s.Contains('=')).ToDictionary(s => s.Substring(0, s.IndexOf('=')), s => s.Substring(s.IndexOf('=') + 1));
var S = rows.Where(r => r[1] == "PL").ToList(); var T = rows.Where(r => r[1] == "TX").ToList();
if (S.Count != 3 || T.Count != 3) { Out.AppendLine("export does not hold 3 strings + 3 labels - nothing done"); return null; }
foreach (var r in S) { var a = Attr(r); Out.AppendLine($"new string {a["BHID"]}: {r[3].Split(';').Length} pts, closed {r[2]}, GROUT_TYPE {a.GetValueOrDefault("GROUT_TYPE")}, DRILL_STATUS {a.GetValueOrDefault("DRILL_STATUS")}"); }
foreach (var r in T) Out.AppendLine($"new label '{r[2]}' at {r[3]} rot {r[4]} h {r[5]}");
Out.AppendLine("old SUB hazards to remove: " + string.Join(", ", oldS.Concat(oldT).Select(h => h + " " + A(H[h], "BHID"))));
Out.AppendLine("DM template attributes kept: " + string.Join("|", new[] { "Name", "ACT_TYPE", "MINE", "Description", "PROFILE", "GSS", "GROUT_TYPE", "DRILL_STATUS" }.Select(k => k + "=" + A(H["45891E1"], k))));
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }
var log = new System.Text.StringBuilder();
foreach (var h in oldS) { log.AppendLine("== " + h + " " + A(H[h], "BHID")); for (int i = 0; i < H[h].asPolyline.VertexCount; i++) { var q = H[h].asPolyline.GetVertex(i); log.AppendLine($"{q.x:R},{q.y:R},{q.z:R}"); } }
foreach (var h in oldT) { var t = H[h].asText; log.AppendLine($"== {h} '{t.TextString}' {t.InsertionPoint.x:R},{t.InsertionPoint.y:R},{t.InsertionPoint.z:R} rot {t.Rotation:R}"); }
System.IO.File.WriteAllText(@"C:\dswtmp\dm_sub_hazards_backup2.txt", log.ToString());
var added = new List<string>();
foreach (var r in S)
{
    var a = Attr(r); var k = (Deswik.Graphics.Primaries.Figure)H["45891E1"].Clone(); k.Deleted = false; var pl = k.asPolyline;
    while (pl.VertexCount > 0) pl.RemoveVertexAt(0);
    foreach (var p in r[3].Split(';')) { var v = p.Split(',').Select(x => double.Parse(x, inv)).ToArray(); pl.AddVertexWithoutAutomaticClose(v[0], v[1], v[2]); }
    pl.Closed = r[2] == "True";
    foreach (var n in new[] { "BHID", "GROUT_TYPE", "DRILL_STATUS" }) if (a.ContainsKey(n)) k.AttributeValueSet(n, a[n]);
    App.ModelSpace.Entities.AddItem(k, null, null, true, false); added.Add(k.Handle + " " + a["BHID"]);
}
foreach (var r in T)
{
    var a = Attr(r); var k = (Deswik.Graphics.Primaries.Figure)H["45891E4"].Clone(); k.Deleted = false; var t = k.asText;
    var ip = r[3].Split(',').Select(x => double.Parse(x, inv)).ToArray();
    t.TextString = r[2]; t.Rotation = double.Parse(r[4], inv);
    var hp = t.GetType().GetProperty("Height"); double hh = double.Parse(r[5], inv); if (hp != null && hp.CanWrite && hh > 0) hp.SetValue(t, hh);
    t.InsertionPoint = new Deswik.Graphics.Geometry.Point(ip[0], ip[1], ip[2]);
    foreach (var n in new[] { "BHID", "GROUT_TYPE", "DRILL_STATUS" }) if (a.ContainsKey(n)) k.AttributeValueSet(n, a[n]);
    App.ModelSpace.Entities.AddItem(k, null, null, true, false); added.Add(k.Handle + " '" + r[2] + "'");
}
foreach (var h in oldS.Concat(oldT)) H[h].Deleted = true;
App.Redraw(true);
Out.AppendLine("applied: removed " + string.Join(", ", oldS.Concat(oldT)) + "; added " + string.Join(", ", added) + "; backup C:\\dswtmp\\dm_sub_hazards_backup2.txt; NOT saved");
return null;
