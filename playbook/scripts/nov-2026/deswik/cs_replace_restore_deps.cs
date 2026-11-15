// Re-add old dependencies onto the new tasks. Old->new task map: unchanged IDs, Replace_Map.tsv (tunnels), derived via their parent tunnel + ACT_TYPE.
// Skips links that already exist, collapse onto one task, or would make a loop. APPLY=false = dry run. LIMIT caps adds (0 = all).
const bool APPLY = false; const int LIMIT = 0;
var ci = System.Globalization.CultureInfo.InvariantCulture;
string C(object v) => v == null ? "" : (v is IFormattable fm ? fm.ToString(null, ci) : v.ToString());
var tl = System.IO.File.ReadAllLines(@"C:\dswtmp\Schedule_Tasks_20261113_090226.tsv"); var th = tl[0].Split('\t').ToList();
int iAT = th.IndexOf("ACT_TYPE"), iDT = th.IndexOf("DerivedTasksDetails"), iTy = th.IndexOf("DIS Activity Type");
var old = new Dictionary<string, string[]>(); foreach (var l in tl.Skip(1)) { var c = l.Split('\t'); old[c[0]] = c; }
var cur = Schedule.Tasks.Where(t => !t.Deleted).ToList(); var curById = cur.ToDictionary(t => t.Id); var uidToId = cur.ToDictionary(t => t.UniqueId, t => t.Id);
var o2n = new Dictionary<string, string>();
foreach (var id in old.Keys) if (curById.ContainsKey(id)) o2n[id] = id;
foreach (var m in System.IO.File.ReadAllLines(@"C:\dswtmp\Replace_Map.tsv").Skip(1).Select(l => l.Split('\t'))) o2n[m[2]] = m[0];
char[] sep = { ',', ';', '|', ' ' };
var newDerivedByParent = new Dictionary<string, List<string>>();
foreach (var t in cur) { var s = C(t.GetFieldValueAndCacheIt("DerivedTasksDetails")); if (s != "") newDerivedByParent[t.Id] = s.Split(sep, StringSplitOptions.RemoveEmptyEntries).Where(curById.ContainsKey).ToList(); }
string AT(string id) => curById.TryGetValue(id, out var t) ? C(t.GetFieldValueAndCacheIt("ACT_TYPE")) : "";
int derivedMapped = 0;
foreach (var o in old.Values)
{
    if (o[iDT] == "" || !o2n.TryGetValue(o[0], out var np) || np == o[0] || !newDerivedByParent.TryGetValue(np, out var nds)) continue;
    foreach (var od in o[iDT].Split(sep, StringSplitOptions.RemoveEmptyEntries)) { if (o2n.ContainsKey(od) || !old.TryGetValue(od, out var orow)) continue; var nd = nds.FirstOrDefault(x => AT(x) == orow[iAT]); if (nd != null) { o2n[od] = nd; derivedMapped++; } }
}
// graph for loop checks
var succ = new Dictionary<string, HashSet<string>>(); var have = new HashSet<string>();
void Link(string a, string b) { if (!succ.ContainsKey(a)) succ[a] = new HashSet<string>(); succ[a].Add(b); have.Add(a + ">" + b); }
foreach (var d in Schedule.Dependencies) if (!d.Deleted && uidToId.ContainsKey(d.From) && uidToId.ContainsKey(d.To)) Link(uidToId[d.From], uidToId[d.To]);
bool Reach(string a, string b) { var st = new Stack<string>(); var seen = new HashSet<string>(); st.Push(a); while (st.Count > 0) { var x = st.Pop(); if (x == b) return true; if (!seen.Add(x) || !succ.TryGetValue(x, out var ns)) continue; foreach (var n in ns) st.Push(n); } return false; }
var dl = System.IO.File.ReadAllLines(@"C:\dswtmp\Schedule_Dependencies_20261113_090226.tsv").Skip(1).Select(l => l.Split('\t')).ToList();
int exist = 0, unm = 0, self = 0, loop = 0, add = 0, errs = 0; var unmT = new Dictionary<string, int>(); var addT = new Dictionary<string, int>(); var rep = new StringBuilder("Result\tFromNew\tToNew\tFromOld\tToOld\tFromName\tToName\tLag\tLayer\n"); var err = new List<string>();
foreach (var c in dl)
{
    string tyA = old.TryGetValue(c[1], out var ra) ? ra[iTy] : "?", tyB = old.TryGetValue(c[6], out var rb) ? rb[iTy] : "?";
    if (!o2n.TryGetValue(c[1], out var a) || !o2n.TryGetValue(c[6], out var b)) { unm++; var k = tyA + (o2n.ContainsKey(c[1]) ? "" : "*") + "->" + tyB + (o2n.ContainsKey(c[6]) ? "" : "*"); unmT[k] = unmT.TryGetValue(k, out var z) ? z + 1 : 1; rep.Append("UNMAPPED\t\t\t" + c[1] + "\t" + c[6] + "\t" + c[2] + "\t" + c[7] + "\t" + c[11] + "\t" + c[12] + "\n"); continue; }
    if (a == b) { self++; continue; }
    if (have.Contains(a + ">" + b)) { exist++; continue; }
    if (Reach(b, a)) { loop++; rep.Append("LOOP\t" + a + "\t" + b + "\t" + c[1] + "\t" + c[6] + "\t" + c[2] + "\t" + c[7] + "\t" + c[11] + "\t" + c[12] + "\n"); continue; }
    if (LIMIT > 0 && add >= LIMIT) continue;
    var kk = tyA + "->" + tyB; addT[kk] = addT.TryGetValue(kk, out var z2) ? z2 + 1 : 1; add++;
    rep.Append("ADD\t" + a + "\t" + b + "\t" + c[1] + "\t" + c[6] + "\t" + c[2] + "\t" + c[7] + "\t" + c[11] + "\t" + c[12] + "\n");
    Link(a, b);
    if (APPLY)
    {
        try
        {
            var dep = new Deswik.Scheduler.Engine.Dependency(-1, curById[a].UniqueId, curById[b].UniqueId);
            dep.LinkType = Deswik.Scheduler.API.Contracts.LinkType.FinishStart;
            var lagDays = double.Parse(c[11].TrimEnd('d'), ci); if (lagDays != 0) dep.Lag = new Deswik.Scheduler.Engine.Lag(lagDays, Deswik.Scheduler.Engine.TimeUnit.d, null);
            int lay; if (int.TryParse(c[12], out lay)) dep.LayerId = lay;
            Schedule.Add(dep, true, true, false);
        }
        catch (Exception e) { errs++; if (err.Count < 5) err.Add(a + ">" + b + ": " + e.Message); }
    }
}
System.IO.File.WriteAllText(@"C:\dswtmp\Replace_DepsRestore" + (APPLY ? "_Applied" : "_DryRun") + ".tsv", rep.ToString(), new UTF8Encoding(true));
if (APPLY) { Schedule.ScheduleCalculator.CalculateSchedule(true, false); Schedule.ScheduleCalculator.WaitForCalculationsToComplete(false, 300000); }
Out.AppendLine((APPLY ? "APPLIED" : "DRY RUN") + "  old deps " + dl.Count + "  derived mapped " + derivedMapped);
Out.AppendLine("  already there " + exist + "  to add " + add + "  would loop " + loop + "  same task " + self + "  unmapped " + unm + "  errors " + errs);
foreach (var kv in addT.OrderByDescending(k => k.Value)) Out.AppendLine("  ADD " + kv.Key + "\t" + kv.Value);
foreach (var kv in unmT.OrderByDescending(k => k.Value)) Out.AppendLine("  UNMAPPED " + kv.Key + "\t" + kv.Value + "   (* = end with no new task)");
foreach (var e in err) Out.AppendLine("  ERR " + e);
Out.AppendLine("DEPS NOW " + Schedule.Dependencies.Count(d => !d.Deleted));
return null;
