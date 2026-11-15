// Read-only: why HG + MG + LG tonnes (Blend Forecast bins: Diluted Tonnage, SCHEDULED = 1, Diluted Au_Grade > 1) differ from
// "All ore - Tonnes" (Sch_Diluted tonnes, filter UNDERGROUND Ore = ORE_FLAG 1). Each task is spread over its Start-Finish
// span by time, month by month Oct-26..Sep-27 (as the report does). Also: grade range of ORE_FLAG = 1 tasks vs ORE_FLAG = 0.
var BF = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
double Num(Deswik.Scheduler.Engine.Task t, string f) { var v = t.GetNullaleFieldFullValue(f); if (v == null) return 0; var m = v.GetType().GetMethod("AsDouble", System.Type.EmptyTypes); return System.Convert.ToDouble(m.Invoke(v, null)); }
var tif = Schedule.GetType().GetMethods(BF).First(m => m.Name == "TaskIsInFilter" && m.GetParameters().Length == 2);
bool In(Deswik.Scheduler.Engine.Task t, string f) => (bool)tif.Invoke(Schedule, new object[] { t, f });
var m0 = new System.DateTime(2026, 10, 1); var months = Enumerable.Range(0, 13).Select(k => m0.AddMonths(k)).ToArray();
double Frac(Deswik.Scheduler.Engine.Task t, System.DateTime a, System.DateTime b) { var dur = (t.Finish - t.Start).TotalSeconds; if (dur <= 0) return (t.Start >= a && t.Start < b) ? 1 : 0; var s = t.Start > a ? t.Start : a; var e = t.Finish < b ? t.Finish : b; return e > s ? (e - s).TotalSeconds / dur : 0; }
var tasks = Schedule.Tasks.Where(t => !t.Deleted && t.Finish > months[0] && t.Start < months[12]).ToList();
// per month: bins (report def), all ore (report def), and the split
var binsT = new double[12]; var oreT = new double[12]; var both = new double[12]; var binNotOre = new double[12]; var oreNotBin = new double[12]; var fieldDiff = new double[12];
var oreLowG = new SortedDictionary<string, double>(); // ore not in HG/MG/LG, by grade band
double minOreG = 1e9, maxWasteG = -1; string minOreTask = "", maxWasteTask = ""; int nOre = 0, nOreLE1 = 0, nWasteGT1 = 0;
var wasteByType = new Dictionary<string, double>();
foreach (var t in tasks)
{
    double dt = Num(t, "Diluted Tonnage"), st = Num(t, "Sch_Diluted tonnes"), g = Num(t, "Diluted Au_Grade"); if (dt == 0 && st == 0) continue;
    bool bin = In(t, "HG + VHG") || In(t, "MG") || In(t, "LG"); bool ore = In(t, "UNDERGROUND Ore");
    string act = t.GetNullaleFieldFullValue("ACT_TYPE")?.ToString() ?? "?";
    if (ore) { nOre++; if (g < minOreG) { minOreG = g; minOreTask = t.Name; } if (g <= 1) nOreLE1++; }
    else if (g > 1 && bin) { nWasteGT1++; if (g > maxWasteG) { maxWasteG = g; maxWasteTask = t.Name + " (" + act + ")"; } }
    for (int i = 0; i < 12; i++)
    {
        double f = Frac(t, months[i], months[i + 1]); if (f == 0) continue;
        if (bin) binsT[i] += dt * f; if (ore) oreT[i] += st * f;
        if (bin && ore) { both[i] += dt * f; fieldDiff[i] += (st - dt) * f; }
        if (bin && !ore) { binNotOre[i] += dt * f; if (!wasteByType.ContainsKey(act)) wasteByType[act] = 0; wasteByType[act] += dt * f; }
        if (ore && !bin) { oreNotBin[i] += st * f; string band = g <= 0.5 ? "<=0.5" : g <= 1 ? "0.5-1" : "in a bin grade but SCHEDULED<>1"; if (!oreLowG.ContainsKey(band)) oreLowG[band] = 0; oreLowG[band] += st * f; }
    }
}
Out.AppendLine("month   | HG+MG+LG t | All ore t |  = both  + bins only (ORE_FLAG 0) - ore only (not HG/MG/LG) | field diff on both");
for (int i = 0; i < 12; i++) Out.AppendLine($"{months[i]:MMM-yy}  | {binsT[i],10:N0} | {oreT[i],9:N0} | {both[i],8:N0} | {binNotOre[i],10:N0} | {oreNotBin[i],10:N0} | {fieldDiff[i],8:N1}");
Out.AppendLine($"Oct-26..Sep-27 totals: bins {binsT.Sum():N0} t, all ore {oreT.Sum():N0} t; bins-only {binNotOre.Sum():N0} t, ore-only {oreNotBin.Sum():N0} t");
Out.AppendLine($"ORE_FLAG = 1 tasks: {nOre}, lowest Diluted Au_Grade {minOreG:F3} ({minOreTask}); {nOreLE1} of them at or below 1 g/t");
Out.AppendLine($"ORE_FLAG = 0 tasks in HG/MG/LG: {nWasteGT1}, highest grade {maxWasteG:F3} ({maxWasteTask})");
Out.AppendLine("bins-only tonnes by ACT_TYPE: " + string.Join(", ", wasteByType.OrderByDescending(k => k.Value).Select(k => $"{k.Key} {k.Value:N0}")));
Out.AppendLine("ore-only tonnes by grade: " + string.Join(", ", oreLowG.Select(k => $"{k.Key} {k.Value:N0}")));
return null;
