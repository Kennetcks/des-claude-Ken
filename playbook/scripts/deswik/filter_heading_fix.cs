if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Fix filter "Claude - G390 MLA + successors": G390 MLA / ISP / SMP S1 by Name + the decline cuts of solids 44A92DA..44A92F2 by task Id.
// Writes the where clause explicitly, makes the filter active, refreshes the filter cache. No task data changes. File not saved.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
var filters = Schedule.GetType().GetField("<TaskFilters>k__BackingField", BF).GetValue(Schedule);
var fl = ((System.Collections.IEnumerable)filters).Cast<object>().ToList();
object G(object o, string n) { var p = o.GetType().GetProperty(n, BF); if (p != null) return p.GetValue(o); return o.GetType().GetField(n, BF)?.GetValue(o); }
const string NAME = "Claude - G390 MLA + successors";
var f = fl.FirstOrDefault(x => System.Convert.ToString(G(x, "Name")) == NAME);
if (f == null) { Out.AppendLine("filter not found - nothing changed"); return null; }
// decline solids by handle range -> task Ids
int h0 = System.Convert.ToInt32("44A92DA", 16), h1 = System.Convert.ToInt32("44A92F2", 16);
var dec = new List<(int h, string id)>();
foreach (var e in App.ModelSpace.Entities) { var fg = e as Deswik.Graphics.Primaries.Figure; if (fg == null || fg.Deleted) continue;
  var hs = $"{fg.GetType().GetProperty("Handle")?.GetValue(fg)}"; int h; if (!int.TryParse(hs, System.Globalization.NumberStyles.HexNumber, null, out h) || h < h0 || h > h1) continue;
  var id = System.Convert.ToString(fg.AttributeValueGet("ID", "")); if (id != "" && Schedule.GetTask(id) != null) dec.Add((h, id)); }
dec = dec.OrderBy(d => d.h).ToList();
if (dec.Count == 0) { Out.AppendLine("no tasks found for solids 44A92DA..44A92F2 - nothing changed"); return null; }
foreach (var d in dec) { var t = Schedule.GetTask(d.id); Out.AppendLine($"  {d.h:X} {t.Name} #{t.GetNullaleFieldFullValue("DIS Activity Number")} {t.Start:dd/MM/yy}"); }
var names = new[] { "G390 MLA -", "G390 ISP -", "G390 SMP S1" };
var where = string.Join(" Or ", names.Select(n => $"(Name=\"{n}\")").Concat(dec.Select(d => $"(Id=\"{d.id}\")")));
var ftype = f.GetType(); var items = (System.Collections.IList)ftype.GetField("mFilter", BF).GetValue(f);
var itype = ftype.GetField("mFilter", BF).FieldType.GetGenericArguments()[0]; var ctor = itype.GetConstructor(new[] { typeof(string), typeof(string), typeof(string), typeof(string), typeof(string) });
items.Clear(); foreach (var n in names) items.Add(ctor.Invoke(new object[] { "Name", "=", n, "Text", "Or" })); foreach (var d in dec) items.Add(ctor.Invoke(new object[] { "Id", "=", d.id, "Text", "Or" }));
ftype.GetField("where", BF).SetValue(f, where); ftype.GetField("recalculateWhereClause", BF).SetValue(f, false);
Schedule.GetType().GetMethod("SetAsActiveTaskFilter", BF, null, new[] { typeof(string) }, null).Invoke(Schedule, new object[] { NAME });
var cache = Schedule.GetType().GetField("<matchingFilterCache>k__BackingField", BF).GetValue(Schedule);
cache?.GetType().GetMethod("Update", BF, null, System.Type.EmptyTypes, null)?.Invoke(cache, null);
Out.AppendLine("where now: " + G(f, "where"));
object lst = null; try { var gf = Schedule.GetType().GetMethods(BF).FirstOrDefault(m => m.Name == "GetFilteredTasks" && m.GetParameters().Length == 0); lst = gf?.Invoke(Schedule, null); } catch (System.Exception ex) { Out.AppendLine("GetFilteredTasks: " + ex.InnerException?.Message); }
if (lst is System.Collections.IEnumerable en) { var ts = en.Cast<object>().ToList(); Out.AppendLine($"filtered tasks: {ts.Count}"); foreach (var g in ts.Select(t => System.Convert.ToString(t.GetType().GetProperty("Name")?.GetValue(t))).GroupBy(x => x)) Out.AppendLine($"  {g.Key}: {g.Count()}"); }
Out.AppendLine($"active filter: [{Schedule.GetType().GetField("m_activeTaskFilter", BF).GetValue(Schedule)}]");
return null;
