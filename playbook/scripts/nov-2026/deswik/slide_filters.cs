if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Add/refresh one task filter per 3MS deck level slide (13-17): Name = <each task name in that picture>, joined with Or.
// Names come from scheduled tasks (SCHEDULED = 1) matching each slide's pattern. The active filter is not changed. No task data changes. File not saved.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
object G(object o, string n) { var p = o.GetType().GetProperty(n, BF); if (p != null) return p.GetValue(o); return o.GetType().GetField(n, BF)?.GetValue(o); }
string FV(Deswik.Scheduler.Engine.Task t, string f) { try { return System.Convert.ToString(t.GetNullaleFieldFullValue(f)); } catch { return ""; } }
var specs = new (string name, string regex)[] {
  ("3MS S13 - G6410 Level",    @"^(G410 |W(40[4-9]|41[0-2]) DEC )"),
  ("3MS S14 - G6390 Level",    @"^(G390 |W(39[0-9]|40[0-4]) DEC )"),
  ("3MS S15 - G6380 Level",    @"^(G380 SMP S$|G400 SUB |W390 EWD |W390 RAD |W(37[6-9]|38[0-9]|39[0-2]) DEC )"),
  ("3MS S16 - G6370 Level",    @"^(G370 |G380 SMP S$|W(36[2-9]|37[0-9]|380) DEC )"),
  ("3MS S17 - the mine Decline",  @"^W(36[2-9]|3[7-9][0-9]|40[0-9]) DEC ") };
var filters = Schedule.GetType().GetField("<TaskFilters>k__BackingField", BF).GetValue(Schedule);
var fl = ((System.Collections.IEnumerable)filters).Cast<object>().ToList();
var ftype = fl[0].GetType(); var itype = ftype.GetField("mFilter", BF).FieldType.GetGenericArguments()[0];
var ctor = itype.GetConstructor(new[] { typeof(string), typeof(string), typeof(string), typeof(string), typeof(string) });
var add = filters.GetType().GetMethods().First(m => m.Name == "Add" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType.IsAssignableFrom(ftype));
var tif = Schedule.GetType().GetMethods(BF).First(m => m.Name == "TaskIsInFilter" && m.GetParameters().Length == 2);
// Lateral development only (what the pictures show): no cable bolting, stope or vertical tasks.
var live = Schedule.Tasks.Where(t => !t.Deleted && FV(t, "SCHEDULED") == "1" && FV(t, "STO_DEV") == "DEV" && FV(t, "LAT_VERT") == "LAT").ToList();
foreach (var (fname, rx) in specs) {
  var re = new System.Text.RegularExpressions.Regex(rx);
  var names = live.Where(t => re.IsMatch(t.Name)).Select(t => t.Name).Distinct().OrderBy(n => n).ToList();
  var f = ((System.Collections.IEnumerable)filters).Cast<object>().FirstOrDefault(x => System.Convert.ToString(G(x, "Name")) == fname);
  if (f == null) { f = System.Activator.CreateInstance(ftype, new object[] { fname }); add.Invoke(filters, new[] { f }); }
  var items = (System.Collections.IList)ftype.GetField("mFilter", BF).GetValue(f); items.Clear();
  // (SCHEDULED = 1 And Name = n) Or ... for each name, same pattern as the existing "Active levels" filter
  for (int i = 0; i < names.Count; i++) {
    items.Add(ctor.Invoke(new object[] { "CustomField[\"SCHEDULED\"]", "=", "1", "Double", "And" }));
    items.Add(ctor.Invoke(new object[] { "Name", "=", names[i], "Text", i == names.Count - 1 ? "And" : "Or" }));
  }
  var where = string.Join(" Or ", names.Select(n => $"(NumericCustomField[\"SCHEDULED\"]=1 And Name=\"{n}\")"));
  ftype.GetField("where", BF).SetValue(f, where); ftype.GetField("recalculateWhereClause", BF).SetValue(f, false);
  var w = System.Convert.ToString(G(f, "where"));
  int hits = Schedule.Tasks.Count(t => !t.Deleted && (bool)tif.Invoke(Schedule, new object[] { t, fname }));
  int expect = live.Count(t => names.Contains(t.Name));
  int expectAll = Schedule.Tasks.Count(t => !t.Deleted && FV(t, "SCHEDULED") == "1" && names.Contains(t.Name));
  Out.AppendLine($"{fname}: {names.Count} names, {hits} tasks match (expected {expectAll}; lateral dev {expect}) | " + string.Join(", ", names.Where(n => !n.Contains(" DEC "))) + (names.Any(n => n.Contains(" DEC ")) ? $" + decline {names.Where(n => n.Contains(" DEC ")).First()} .. {names.Where(n => n.Contains(" DEC ")).Last()}" : ""));
  if (w.Length < 20) Out.AppendLine("   where clause not built: " + w);
}
var cache = Schedule.GetType().GetField("<matchingFilterCache>k__BackingField", BF).GetValue(Schedule);
cache?.GetType().GetMethod("Update", BF, null, System.Type.EmptyTypes, null)?.Invoke(cache, null);
return null;
