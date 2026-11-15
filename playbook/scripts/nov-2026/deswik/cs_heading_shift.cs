// Read-only: headings whose first cut moved > 30 d vs old. For each: metres old/new, first-cut predecessors old vs now (with their dates), priority, resource.
var ci = System.Globalization.CultureInfo.InvariantCulture;
double N(string s) { double v; return double.TryParse(s, System.Globalization.NumberStyles.Float, ci, out v) ? v : 0; }
DateTime D(string s) { DateTime v; return DateTime.TryParse(s, ci, System.Globalization.DateTimeStyles.None, out v) ? v : DateTime.MinValue; }
var tl = System.IO.File.ReadAllLines(@"C:\dswtmp\Schedule_Tasks_20261113_090226.tsv"); var th = tl[0].Split('\t').ToList();
string O(string[] r, string f) { int i = th.IndexOf(f); return i < 0 || i >= r.Length ? "" : r[i]; }
var old = new Dictionary<string, string[]>(); foreach (var l in tl.Skip(1)) { var c = l.Split('\t'); old[c[0]] = c; }
var oPred = new Dictionary<string, List<string[]>>();
foreach (var c in System.IO.File.ReadAllLines(@"C:\dswtmp\Schedule_Dependencies_20261113_090226.tsv").Skip(1).Select(l => l.Split('\t'))) { if (!oPred.ContainsKey(c[6])) oPred[c[6]] = new List<string[]>(); oPred[c[6]].Add(c); }
var n2o = System.IO.File.ReadAllLines(@"C:\dswtmp\Map2.tsv").Skip(1).Select(l => l.Split('\t')).ToDictionary(c => c[0], c => c[1]);
var cur = Schedule.Tasks.Where(t => !t.Deleted).ToList(); var byId = cur.ToDictionary(t => t.Id); var uid = cur.ToDictionary(t => t.UniqueId);
var nPred = new Dictionary<string, List<(string id, string lag)>>(); foreach (var d in Schedule.Dependencies) if (!d.Deleted && uid.ContainsKey(d.From) && uid.ContainsKey(d.To)) { var k = uid[d.To].Id; if (!nPred.ContainsKey(k)) nPred[k] = new List<(string, string)>(); nPred[k].Add((uid[d.From].Id, d.Lag == null ? "0d" : d.Lag.ToString())); }
string F(Deswik.Scheduler.Engine.Task t, string f) => t.GetFieldValueAndCacheIt(f) ?? "";
var oh = old.Values.Where(r => O(r, "DIS Activity Type") == "Tunnel" && N(O(r, "Sch_metres")) > 0).GroupBy(r => O(r, "MINE") + " | " + O(r, "Name")).ToDictionary(g => g.Key, g => g.OrderBy(r => D(O(r, "Start"))).ToList());
var nh = cur.Where(t => F(t, "DIS Activity Type") == "Tunnel" && N(F(t, "Sch_metres")) > 0).GroupBy(t => F(t, "MINE") + " | " + t.Name).ToDictionary(g => g.Key, g => g.OrderBy(t => t.Start).ToList());
var rows = new List<(double d, string txt)>(); var sb = new StringBuilder("Heading\tOldFirst\tNewFirst\tShift d\tOld m\tNew m\tOldPri\tNewPri\tOldRes\tNewRes\tOld first-cut preds (finish)\tNew first-cut preds (finish)\tNewFirstCutMapsTo\n");
foreach (var kv in oh)
{
    if (!nh.TryGetValue(kv.Key, out var nl)) continue; var o1 = kv.Value[0]; var n1 = nl[0]; var od = D(O(o1, "Start")); var sd = (n1.Start - od).TotalDays; if (Math.Abs(sd) <= 30) continue;
    string op = oPred.TryGetValue(o1[0], out var ps) ? string.Join("; ", ps.Select(p => (old.TryGetValue(p[1], out var r) ? O(r, "Name") + " " + D(O(r, "Finish")).ToString("d/MM/yy") : p[1]) + (p[11] != "0d" ? " +" + p[11] : ""))) : "(none)";
    string np = nPred.TryGetValue(n1.Id, out var qs) ? string.Join("; ", qs.Select(q => byId[q.id].Name + " " + byId[q.id].Finish.ToString("d/MM/yy") + (q.lag != "0d" ? " +" + q.lag : ""))) : "(none)";
    string maps = n2o.TryGetValue(n1.Id, out var mo) ? (mo == o1[0] ? "old first cut" : (old.TryGetValue(mo, out var mr) ? "old " + O(mr, "Name") + " #" + O(mr, "DIS Activity Number") + " (old first is #" + O(o1, "DIS Activity Number") + ")" : mo)) : "unmapped";
    var line = kv.Key + "\t" + od.ToString("d/MM/yy") + "\t" + n1.Start.ToString("d/MM/yy") + "\t" + sd.ToString("F0") + "\t" + kv.Value.Sum(r => N(O(r, "Sch_metres"))).ToString("F0") + "\t" + nl.Sum(t => N(F(t, "Sch_metres"))).ToString("F0") + "\t" + O(o1, "Priority") + "\t" + n1.Priority + "\t" + O(o1, "ResourceAssignmentDetails").Split(':')[0] + "\t" + C0(n1.ResourceAssignmentDetails).Split(':')[0] + "\t" + op + "\t" + np + "\t" + maps;
    sb.AppendLine(line); rows.Add((sd, line));
}
string C0(string s) => (s ?? "").Replace("{", "");
System.IO.File.WriteAllText(@"C:\dswtmp\Heading_Shifts.tsv", sb.ToString(), new UTF8Encoding(true));
Out.AppendLine("HEADINGS >30d: " + rows.Count + "  earlier " + rows.Count(r => r.d < 0) + "  later " + rows.Count(r => r.d > 0));
foreach (var r in rows.OrderBy(r => r.d)) Out.AppendLine(r.txt);
return null;
