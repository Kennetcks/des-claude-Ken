// New mine grid: LEVEL -> 6000 + LEVEL, and the level number in names (<letters><number>...) -> 6000 + number.
// Applies to: all schedule tasks (LEVEL field + Name), and CAD figures on the schedule-linked layers (LEVEL + Name attributes).
// Values already >= 1000 are left alone. Writes a backup of every old value to C:\dswtmp first.
const bool APPLY = false;
var ci = System.Globalization.CultureInfo.InvariantCulture;
var layers = new[] { "SCHEDULER\\TASK SOLIDS", "SCHEDULER\\TASK POLYGONS", "0\\06 SITE\\03 ENGINEERING\\01 DEV STRINGS\\COMBINED 12.11", "0\\06 SITE\\03 ENGINEERING\\02 STOPES\\COMBINED USED" };
bool OnLayer(string l) => layers.Any(x => l == x || l.StartsWith(x + "\\"));
var rx = new System.Text.RegularExpressions.Regex("^([A-Z]+)(\\d{1,3})(?!\\d)(.*)$");
string NewLevel(string s) { int v; return int.TryParse(s, out v) && v < 1000 ? (6000 + v).ToString() : null; }
string NewName(string s) { var m = rx.Match(s ?? ""); if (!m.Success) return null; return m.Groups[1].Value + (6000 + int.Parse(m.Groups[2].Value)) + m.Groups[3].Value; }
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
var bak = new StringBuilder("Kind\tId\tField\tOld\tNew\n"); int tL = 0, tN = 0, fL = 0, fN = 0; var skipName = new HashSet<string>(); var samples = new List<string>();
var ts = Schedule.Tasks.Where(t => !t.Deleted).ToList();
foreach (var t in ts)
{
    var ol = t.GetFieldValueAndCacheIt("LEVEL") ?? ""; var nl = NewLevel(ol);
    if (nl != null) { tL++; bak.Append("TASK\t" + t.Id + "\tLEVEL\t" + ol + "\t" + nl + "\n"); if (APPLY) t.SetFieldValue("LEVEL", nl, ci); }
    var nn = NewName(t.Name);
    if (nn != null) { tN++; bak.Append("TASK\t" + t.Id + "\tName\t" + t.Name + "\t" + nn + "\n"); if (samples.Count < 8) samples.Add(t.Name + " -> " + nn); if (APPLY) t.Name = nn; }
    else if (!string.IsNullOrEmpty(t.Name)) skipName.Add(t.Name);
}
var byLayer = new Dictionary<string, int>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || !OnLayer(f.Layer.Name)) continue; bool ch = false;
    var ol = A(f, "LEVEL"); var nl = NewLevel(ol); if (nl != null) { fL++; ch = true; bak.Append("FIG\t" + f.Handle + "\tLEVEL\t" + ol + "\t" + nl + "\n"); if (APPLY) f.AttributeValueSet("LEVEL", nl); }
    var on = A(f, "Name"); var nn = NewName(on); if (nn != null) { fN++; ch = true; bak.Append("FIG\t" + f.Handle + "\tName\t" + on + "\t" + nn + "\n"); if (APPLY) f.AttributeValueSet("Name", nn); } else if (on != "") skipName.Add(on);
    if (ch) byLayer[f.Layer.Name] = byLayer.TryGetValue(f.Layer.Name, out var c) ? c + 1 : 1;
}
if (APPLY) { System.IO.File.WriteAllText(@"C:\dswtmp\MineGrid_Backup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".tsv", bak.ToString(), new UTF8Encoding(true)); Schedule.ScheduleCalculator.CalculateSchedule(true, false); Schedule.ScheduleCalculator.WaitForCalculationsToComplete(false, 300000); }
Out.AppendLine((APPLY ? "APPLIED" : "DRY RUN") + "  tasks: LEVEL " + tL + ", Name " + tN + " of " + ts.Count + "   CAD: LEVEL " + fL + ", Name " + fN);
foreach (var kv in byLayer.OrderByDescending(k => k.Value)) Out.AppendLine("  " + kv.Value + "\t" + kv.Key);
Out.AppendLine("samples: " + string.Join(" | ", samples));
Out.AppendLine("names left unchanged (no 1-3 digit level after the letters): " + skipName.Count + "  e.g. " + string.Join(" | ", skipName.Take(15)));
if (APPLY) { var t0 = ts.First(t => t.Name.StartsWith("W6")); Out.AppendLine("check: " + t0.Name + " LEVEL " + t0.GetFieldValueAndCacheIt("LEVEL")); }
return null;
