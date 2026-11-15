// IS Create Tunnels without the dialog, for the strings whose handles are listed in C:\dswtmp\create_tunnels_handles.txt.
// Uses the project's saved Create Tunnels settings (layer, derived, solids layer) with DesignsToUse = Selected; selects just those strings,
// runs Deswik.InteractiveScheduler.Logic.CreateTunnels.Process, restores the selection. Refuses if any listed string already has tasks.
// Tested 15/11/2026 on 06 Dec MINE_UG_FY27 (7 strings -> 34 tasks, 27 solids).
var ps = new HashSet<string>(System.IO.File.ReadAllText(@"C:\dswtmp\create_tunnels_handles.txt").Split(new[] { ",", " ", "\r", "\n", "\t" }, StringSplitOptions.RemoveEmptyEntries));
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
string F(Deswik.Scheduler.Engine.Task t, string f) => t.GetFieldValueAndCacheIt(f) ?? "";
var strings = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (!f.Deleted && f.Layer != null && ps.Contains(f.Handle) && f.IsPolyline) strings.Add(f);
if (Schedule.Tasks.Any(t => !t.Deleted && ps.Contains(F(t, "DIS Parent ID")))) { Out.AppendLine("tasks already exist for these strings - not run"); return null; }
if (strings.Count != ps.Count) { Out.AppendLine("not all strings found - not run"); return null; }
// ---- 2. Create Tunnels
var asm = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Deswik.InteractiveScheduler");
object isPlugin = null; foreach (var it in (System.Collections.IEnumerable)App.Plugins) { object o = it; var ty = o.GetType(); foreach (var nm in new[] { "Value", "Instance", "Plugin", "PluginInstance" }) { var pp = ty.GetProperty(nm, BF); if (pp != null) { try { var v = pp.GetValue(o); if (v != null) { o = v; ty = o.GetType(); } } catch { } } } if (ty.Assembly == asm) isPlugin = o; }
var main = isPlugin.GetType().GetField("MainPanel", BF).GetValue(isPlugin); var appS = main.GetType().GetProperty("AppScheduler", BF)?.GetValue(main) ?? main.GetType().GetField("AppScheduler", BF)?.GetValue(main);
var tTu = asm.GetType("Deswik.InteractiveScheduler.ActivityTypes.Tunnels"); var tCTS = asm.GetType("Deswik.InteractiveScheduler.Objects.CreateTunnelsSettings"); var tCT = asm.GetType("Deswik.InteractiveScheduler.Logic.CreateTunnels");
var tDS = typeof(Deswik.Graphics.Application).Assembly.GetType("Deswik.Graphics.Settings.DataSource");
var tunnels = Activator.CreateInstance(tTu); tTu.GetMethod("LoadFromDocumentTempLayer").Invoke(tunnels, new object[] { App });
var st = Activator.CreateInstance(tCTS); tCTS.GetMethod("LoadFromSource").Invoke(st, new[] { Activator.CreateInstance(tDS, new object[] { App, false }) });
var dsel = tCTS.GetProperty("DesignsToUse"); dsel.SetValue(st, Enum.Parse(dsel.PropertyType, "Selected"));
tCTS.GetProperty("DeleteExisting").SetValue(st, false); tCTS.GetProperty("OpenScheduler").SetValue(st, false);
Out.AppendLine($"settings: layer {tCTS.GetProperty("TunnelsLayer").GetValue(st)}, designs {dsel.GetValue(st)}, derived {tCTS.GetProperty("CreateDerived").GetValue(st)}, solids -> {tCTS.GetProperty("SolidsLayer").GetValue(st)}");
var oldSel = App.Selections.MainSelection.Cast<Deswik.Graphics.Primaries.Figure>().ToList();
var errs = new List<string>(); bool ok = false;
try
{
    App.Selections.MainSelection.Clear(); App.Selections.MainSelection.AddItems(strings);
    var ct = Activator.CreateInstance(tCT, new object[] { App, appS, tunnels, st, null });
    var onErr = new Action<string>(m => errs.Add(m));
    var pm = tCT.GetMethod("Process"); var pt = pm.GetParameters()[0].ParameterType;
    var ga = pt.GetGenericArguments()[0]; Out.AppendLine("onError argument type: " + ga.FullName); var prm = System.Linq.Expressions.Expression.Parameter(ga, "x"); var call = System.Linq.Expressions.Expression.Call(System.Linq.Expressions.Expression.Constant(errs), typeof(List<string>).GetMethod("Add"), System.Linq.Expressions.Expression.Call(System.Linq.Expressions.Expression.Convert(prm, typeof(object)), typeof(object).GetMethod("ToString"))); object handler = System.Linq.Expressions.Expression.Lambda(pt, call, prm).Compile();
    ok = (bool)pm.Invoke(ct, new object[] { handler, true });
    Out.AppendLine($"Create Tunnels: {(ok ? "OK" : "FAILED")} {tCT.GetProperty("LastError")?.GetValue(ct)} {string.Join(" | ", errs)}");
}
catch (Exception e) { Out.AppendLine("Create Tunnels threw: " + (e.InnerException ?? e).Message); }
finally { App.Selections.MainSelection.Clear(); App.Selections.MainSelection.AddItems(oldSel); }
var nts = Schedule.Tasks.Where(t => !t.Deleted && ps.Contains(F(t, "DIS Parent ID"))).ToList();
foreach (var g in nts.GroupBy(t => F(t, "DIS Parent ID")).OrderBy(g => g.Key)) Out.AppendLine($"   {g.Key}: {g.Count()} tasks ({string.Join(", ", g.Select(t => t.Name + "/" + F(t, "DIS Activity Type")).Distinct())})");
int ns = 0; foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (!f.Deleted && f.Layer != null && f.Layer.Name == "SCHEDULER\\TASK SOLIDS" && ps.Contains((f.AttributeValueGet("DIS Parent ID", "") ?? "").ToString())) ns++;
Out.AppendLine($"new task solids {ns}; tasks now {Schedule.Tasks.Count(t => !t.Deleted)}");
App.Redraw(true); return null;
