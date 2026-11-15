// Read-only: compare levelled schedule with the pre-replace snapshot (monthly physicals, heading first cuts, stope starts).
var ci = System.Globalization.CultureInfo.InvariantCulture;
double N(string s) { double v; return double.TryParse(s, System.Globalization.NumberStyles.Float, ci, out v) ? v : 0; }
DateTime D(string s) { DateTime v; return DateTime.TryParse(s, ci, System.Globalization.DateTimeStyles.None, out v) ? v : DateTime.MinValue; }
List<Dictionary<string, string>> Load(string p) { var l = System.IO.File.ReadAllLines(p); var h = l[0].TrimStart('\uFEFF').Split('\t'); var r = new List<Dictionary<string, string>>(); var seen = new HashSet<string>(); foreach (var x in l.Skip(1)) { var c = x.Split('\t'); if (!seen.Add(c[0])) continue; var d = new Dictionary<string, string>(); for (int i = 0; i < h.Length; i++) d[h[i]] = i < c.Length ? c[i] : ""; r.Add(d); } return r; }
var o = Load(@"C:\dswtmp\Schedule_Tasks_20261113_090226.tsv");
var n = Load(System.IO.Directory.GetFiles(@"C:\dswtmp", "Schedule_AfterLevel_*.tsv").OrderBy(x => x).Last());
Out.AppendLine("tasks old " + o.Count + " new " + n.Count + "   latest finish old " + o.Max(r => D(r["Finish"])).ToString("d/MM/yyyy") + " new " + n.Max(r => D(r["Finish"])).ToString("d/MM/yyyy"));
bool Lat(Dictionary<string, string> r) => r["LAT_VERT"] == "LAT" && r["STO_DEV"] == "DEV" && N(r["Sch_metres"]) > 0;
Out.AppendLine("Month\tLat m old\tnew\tVert m old\tnew\tt old\tnew\toz old\tnew");
for (var m = new DateTime(2026, 10, 1); m < new DateTime(2027, 7, 1); m = m.AddMonths(1))
{
    Func<List<Dictionary<string, string>>, Func<Dictionary<string, string>, bool>, string, double> S = (L, f, col) => L.Where(r => { var s = D(r["Start"]); return s >= m && s < m.AddMonths(1) && f(r); }).Sum(r => N(r[col]));
    Func<Dictionary<string, string>, bool> vert = r => r["LAT_VERT"] == "VERT" && r["STO_DEV"] == "DEV";
    Func<Dictionary<string, string>, bool> all = r => true;
    Out.AppendLine(m.ToString("MMM yy") + "\t" + S(o, Lat, "Sch_metres").ToString("F0") + "\t" + S(n, Lat, "Sch_metres").ToString("F0") + "\t" + S(o, vert, "Sch_metres").ToString("F0") + "\t" + S(n, vert, "Sch_metres").ToString("F0") + "\t" + S(o, all, "Sch_Diluted tonnes").ToString("F0") + "\t" + S(n, all, "Sch_Diluted tonnes").ToString("F0") + "\t" + S(o, all, "Sch_Diluted Au Ounces").ToString("F0") + "\t" + S(n, all, "Sch_Diluted Au Ounces").ToString("F0"));
}
Dictionary<string, DateTime> First(List<Dictionary<string, string>> L) => L.Where(r => r["DIS Activity Type"] == "Tunnel" && N(r["Sch_metres"]) > 0 && D(r["Start"]) > DateTime.MinValue).GroupBy(r => r["MINE"] + " | " + r["Name"]).ToDictionary(g => g.Key, g => g.Min(r => D(r["Start"])));
var fo = First(o); var fn = First(n);
var sh = fo.Where(k => fn.ContainsKey(k.Key)).Select(k => (k.Key, old: k.Value, nw: fn[k.Key], d: (fn[k.Key] - k.Value).TotalDays)).ToList();
var ds = sh.Select(x => x.d).OrderBy(x => x).ToList();
Out.AppendLine("headings " + sh.Count + "  median shift " + ds[ds.Count / 2].ToString("F0") + " d  |shift|>30 d: " + sh.Count(x => Math.Abs(x.d) > 30) + "  missing in new: " + fo.Keys.Count(k => !fn.ContainsKey(k)));
var near = sh.Where(x => x.old < new DateTime(2027, 3, 1)).OrderBy(x => x.old).ToList();
Out.AppendLine("first cut before Mar 27: " + near.Count + "  within 7 d: " + near.Count(x => Math.Abs(x.d) <= 7));
foreach (var x in near.Where(x => Math.Abs(x.d) > 7).Take(30)) Out.AppendLine("  " + x.Key + "\t" + x.old.ToString("d/MM/yyyy") + " -> " + x.nw.ToString("d/MM/yyyy") + "\t" + (x.d > 0 ? "+" : "") + x.d.ToString("F0") + " d");
var ns = n.ToDictionary(r => r["TaskID"]);
var st = o.Where(r => r["DIS Activity Type"] == "Solid" && ns.ContainsKey(r["TaskID"])).Select(r => (D(ns[r["TaskID"]]["Start"]) - D(r["Start"])).TotalDays).OrderBy(x => x).ToList();
Out.AppendLine("stopes " + st.Count + "  median shift " + st[st.Count / 2].ToString("F0") + " d  |shift|>30 d: " + st.Count(x => Math.Abs(x) > 30) + "  min " + st.First().ToString("F0") + "  max " + st.Last().ToString("F0"));
Out.AppendLine("dev tasks with no physical resource after level: " + n.Count(r => r["DIS Activity Type"] == "Tunnel" && r["SCHEDULED"] == "1" && r["ResourceAssignmentDetails"] != "" && r["PhysicalResource"] == ""));
return null;
