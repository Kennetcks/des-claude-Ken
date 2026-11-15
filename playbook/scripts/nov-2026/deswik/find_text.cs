if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Read-only: find tasks whose Id, UniqueId, Name or any field contains the text in C:\dswtmp\find_text.txt (case-insensitive).
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
string FV(Deswik.Scheduler.Engine.Task t, string f) { try { return System.Convert.ToString(t.GetNullaleFieldFullValue(f)); } catch { return ""; } }
var q = System.IO.File.ReadAllText(@"C:\dswtmp\find_text.txt").Trim();
bool Has(string s) => s != null && s.IndexOf(q, System.StringComparison.OrdinalIgnoreCase) >= 0;
// field names: custom fields used by the schedule
var fieldNames = new List<string>();
try { var fl = Schedule.GetType().GetMethod("GetFieldNames", BF, null, System.Type.EmptyTypes, null)?.Invoke(Schedule, null) as System.Collections.IEnumerable; if (fl != null) foreach (var x in fl) fieldNames.Add(System.Convert.ToString(x)); } catch { }
if (fieldNames.Count == 0) fieldNames.AddRange(new[] { "MINE", "LEVEL", "ACT_TYPE", "AREA", "HEADING", "ID", "GUID", "SOLID_ID", "Notes", "WBS", "Description", "Text1", "Text2" });
int n = 0;
foreach (var t in Schedule.Tasks.Where(t => !t.Deleted)) {
  var hit = new List<string>();
  if (Has(System.Convert.ToString(t.Id))) hit.Add("Id=" + t.Id);
  if (Has(System.Convert.ToString(t.UniqueId))) hit.Add("UniqueId=" + t.UniqueId);
  if (Has(t.Name)) hit.Add("Name");
  try { if (Has(t.Notes)) hit.Add("Notes"); } catch { }
  foreach (var p in t.GetType().GetProperties(BF)) { if (p.PropertyType != typeof(string) || p.GetIndexParameters().Length > 0) continue; string v = null; try { v = (string)p.GetValue(t); } catch { } if (Has(v)) hit.Add(p.Name + "=" + v); }
  foreach (var fn in fieldNames) { var v = FV(t, fn); if (Has(v)) hit.Add(fn + "=" + v); }
  if (hit.Count > 0 && n++ < 30) Out.AppendLine($"[{t.Id}] {t.Name} ({FV(t, "MINE")}/{FV(t, "ACT_TYPE")}) {t.Start:dd/MM/yy}-{t.Finish:dd/MM/yy} SCH={FV(t, "SCHEDULED")} | {string.Join("; ", hit.Distinct())}");
}
Out.AppendLine($"matches: {n} (fields checked: {fieldNames.Count})");
return null;
