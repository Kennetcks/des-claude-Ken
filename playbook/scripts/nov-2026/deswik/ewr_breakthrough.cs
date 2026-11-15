if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// For every H EWR heading: the lowest EWR solid (the bottom lift), the scheduled development solid nearest the rise bottom
// (the drive that breaks into it), that drive round's task, and whether the first EWR lift already waits for it.
// Set C:\dswtmp\ewr_links.txt to "apply" to add FS links drive round -> first EWR lift (lag 0, default layer, undo) where missing
// and the EWR currently starts before the drive round finishes. Otherwise report only.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
string FV(Deswik.Scheduler.Engine.Task t, string f) { try { return System.Convert.ToString(t.GetNullaleFieldFullValue(f)); } catch { return ""; } }
var apply = System.IO.File.Exists(@"C:\dswtmp\ewr_links.txt") && System.IO.File.ReadAllText(@"C:\dswtmp\ewr_links.txt").Trim() == "apply";
var live = Schedule.Tasks.Where(t => !t.Deleted && FV(t, "SCHEDULED") == "1").ToList();
var byUid = live.ToDictionary(t => System.Convert.ToInt64(t.UniqueId));
var byKey = new Dictionary<string, Deswik.Scheduler.Engine.Task>();
foreach (var t in live) { var k = FV(t, "DIS Parent ID") + "#" + FV(t, "DIS Activity Number"); if (k.Length > 1 && !byKey.ContainsKey(k)) byKey[k] = t; }
double[] Box(Deswik.Graphics.Primaries.Figure f) => System.Convert.ToString(f.BoundingBox).Split(',').Select(double.Parse).ToArray();
var devTypes = new HashSet<string> { "DEC", "MLA", "OD", "DSP", "SUB", "SMP", "RAD", "EWD", "ISP", "ACC", "DDD", "XC", "DRV", "FAD", "ESC" };
var solids = new List<(Deswik.Scheduler.Engine.Task t, double[] b)>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) {
  if (System.Convert.ToString(f.AttributeValueGet("SCHEDULED", "")) != "1") continue;
  var k = System.Convert.ToString(f.AttributeValueGet("DIS Parent ID", "")) + "#" + System.Convert.ToString(f.AttributeValueGet("DIS Activity Number", ""));
  if (byKey.TryGetValue(k, out var t)) solids.Add((t, Box(f)));
}
bool IsDev(Deswik.Scheduler.Engine.Task t) => !t.Name.Contains("Stope") && !"LH_Stope,PDR_LH_STOPE,SLOT,STO_CBL,JSTP_CBL,EWR,LADDER,RBH,RAR,LH_RAR".Split(',').Contains(FV(t, "ACT_TYPE"));
var otherTypes = solids.Where(s => IsDev(s.t)).Select(s => FV(s.t, "ACT_TYPE")).Distinct();
Out.AppendLine("dev ACT_TYPEs considered: " + string.Join(",", otherTypes));
int add = 0;
foreach (var g in live.Where(t => FV(t, "ACT_TYPE") == "EWR" && t.Name.StartsWith("H")).GroupBy(t => t.Name).OrderBy(g => g.Min(t => t.Start))) {
  var rs = g.ToList();
  var first = rs.Where(t => !((System.Collections.IEnumerable)t.Predecessors).Cast<object>().Any(o => byUid.TryGetValue(System.Convert.ToInt64(o.GetType().GetProperty("FromNumeric", BF).GetValue(o)), out var p) && p.Name == t.Name)).OrderBy(t => t.Start).First();
  var rsol = solids.Where(s => s.t.Name == g.Key && FV(s.t, "ACT_TYPE") == "EWR").ToList();
  if (rsol.Count == 0) { Out.AppendLine($"=== {g.Key}: no solids"); continue; }
  var low = rsol.OrderBy(s => s.b[2]).First();
  double cx = (low.b[0] + low.b[3]) / 2, cy = (low.b[1] + low.b[4]) / 2, z = low.b[2];
  double Dist(double[] b) { double dx = System.Math.Max(0, System.Math.Max(b[0] - cx, cx - b[3])), dy = System.Math.Max(0, System.Math.Max(b[1] - cy, cy - b[4])), dz = System.Math.Max(0, System.Math.Max(b[2] - z, z - b[5])); return System.Math.Sqrt(dx * dx + dy * dy + dz * dz); }
  var near = solids.Where(s => IsDev(s.t)).Select(s => (s.t, d: Dist(s.b))).OrderBy(x => x.d).Take(3).ToList();
  var drv = near.First();
  var preds = ((System.Collections.IEnumerable)first.Predecessors).Cast<object>().Select(o => byUid.TryGetValue(System.Convert.ToInt64(o.GetType().GetProperty("FromNumeric", BF).GetValue(o)), out var p) ? p : null).Where(p => p != null && p.Name != first.Name).ToList();
  bool linked = preds.Contains(drv.t);
  bool early = first.Start < drv.t.Finish;
  Out.AppendLine($"=== {g.Key}: {rs.Count} lifts, first lift {first.Id} {first.Start:dd/MM/yy}{(first == low.t ? "" : $" (lowest solid is {low.t.Id} {low.t.Start:dd/MM/yy})")} bottom RL {z:0.0}");
  Out.AppendLine($"    breakthrough drive: {drv.t.Name} [{drv.t.Id}] {FV(drv.t, "ACT_TYPE")} {drv.d:0.0} m away, finishes {drv.t.Finish:dd/MM/yy} -> {(linked ? "LINKED" : early ? "EWR STARTS EARLY BY " + (drv.t.Finish - first.Start).TotalDays.ToString("0") + " d" : "no link but EWR already after")}");
  Out.AppendLine($"    next nearest: {string.Join("; ", near.Skip(1).Select(x => $"{x.t.Name} [{x.t.Id}] {x.d:0.0} m fin {x.t.Finish:dd/MM/yy}"))}");
  Out.AppendLine($"    existing preds of first lift: {string.Join("; ", preds.Select(p => $"{p.Name} [{FV(p, "ACT_TYPE")}] fin {p.Finish:dd/MM/yy}"))}");
  if (!linked && drv.d <= 5) {
    add++; Out.AppendLine($"    ADD {drv.t.Id} -> {first.Id}");
    if (apply) Schedule.Add(new Deswik.Scheduler.Engine.Dependency(-1, System.Convert.ToInt32(drv.t.UniqueId), System.Convert.ToInt32(first.UniqueId)) { LinkType = Deswik.Scheduler.API.Contracts.LinkType.FinishStart }, false, true, false);
  } else if (!linked) Out.AppendLine("    SKIP: nearest drive more than 5 m from rise bottom - check by hand");
}
Out.AppendLine($"{add} links {(apply ? "added" : "proposed")}{(apply ? $"; cycles: {Schedule.Dependencies.Count(x => !x.Deleted && x.CausesCycle)}" : "")}");
return null;
