// Rebuild Deswik IS crosstab report rows from an unzipped .dsf (no Deswik needed).
// Tasks + task filters + production-field weights come from the .tmp.dxf text; report rows (field, title, calc) from the same file,
// and each row's formulas (filter + expression) from the .tmp.dsf settings XML. Quantities are spread over each task's
// Start-Finish in elapsed time. A global filter (e.g. "Mine=MB+MA") is ANDed onto every row. C# 5 (PowerShell 5.1 Add-Type).
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

public class DsfRow { public string Field, Title, Calc; public List<string[]> Formulas = new List<string[]>(); }

public static class DsfReports
{
    static CultureInfo IC = CultureInfo.InvariantCulture;
    public static StringBuilder Log = new StringBuilder();
    static string[] Csv(string s)
    {
        var o = new List<string>(); var sb = new StringBuilder(); bool q = false;
        for (int i = 0; i < s.Length; i++) { char c = s[i]; if (q) { if (c == '"') { if (i + 1 < s.Length && s[i + 1] == '"') { sb.Append('"'); i++; } else q = false; } else sb.Append(c); } else { if (c == '"') q = true; else if (c == ',') { o.Add(sb.ToString()); sb.Clear(); } else sb.Append(c); } }
        o.Add(sb.ToString()); return o.ToArray();
    }
    static string[] L; static Dictionary<string, int> Col; static List<string[]> Tasks; static DateTime[] TS, TF;
    static Dictionary<string, List<string[]>> Filters = new Dictionary<string, List<string[]>>();
    static Dictionary<string, string> Weight = new Dictionary<string, string>();
    static Dictionary<string, bool[]> FilterCache = new Dictionary<string, bool[]>();

    public static void Load(string dir)
    {
        Log.Clear(); FilterCache.Clear(); Filters.Clear(); Weight.Clear();
        var dxf = Directory.GetFiles(dir, "*.tmp.dxf")[0]; L = File.ReadAllLines(dxf);
        Func<string, int> Sec = n => Array.IndexOf(L, "1," + n);
        // production field weights
        int pf = Sec("Production Fields");
        for (int i = pf + 2; i < L.Length && !L[i].StartsWith("1,"); i++) { var t = L[i].TrimStart('\t'); if (t.StartsWith("3,")) { var c = Csv(t.Substring(2)); if (c.Length > 2 && c[2] != "") Weight[c[0]] = c[2]; } }
        // task filters
        int fs = Sec("Task Filters"); string cur = null;
        for (int i = fs + 2; i < L.Length && !L[i].StartsWith("1,"); i++) { var t = L[i].TrimStart('\t'); if (t.StartsWith("3,")) { cur = Csv(t.Substring(2))[0]; Filters[cur] = new List<string[]>(); } else if (t.StartsWith("5,") && cur != null) Filters[cur].Add(Csv(t.Substring(2))); }
        // tasks
        int ts = Sec("Tasks"); var hdr = Csv(L[ts + 1].TrimStart('\t').Substring(2)); Col = new Dictionary<string, int>(); for (int i = 0; i < hdr.Length; i++) if (!Col.ContainsKey(hdr[i])) Col[hdr[i]] = i;
        Tasks = new List<string[]>();
        for (int i = ts + 2; i < L.Length && !L[i].StartsWith("1,"); i++) { var t = L[i]; if (!t.StartsWith("\t3,")) continue; var c = Csv(t.Substring(3)); if (c.Length < hdr.Length) Array.Resize(ref c, hdr.Length); for (int k = 0; k < c.Length; k++) { if (c[k] == null) c[k] = ""; else if (c[k].Contains("__")) c[k] = Unesc(c[k]); } Tasks.Add(c); }
        TS = new DateTime[Tasks.Count]; TF = new DateTime[Tasks.Count];
        for (int i = 0; i < Tasks.Count; i++) { DateTime a, b; DateTime.TryParse(Tasks[i][Col["Start"]], IC, DateTimeStyles.None, out a); DateTime.TryParse(Tasks[i][Col["Finish"]], IC, DateTimeStyles.None, out b); TS[i] = a; TF[i] = b; }
        Log.AppendLine("tasks " + Tasks.Count + ", columns " + hdr.Length + ", filters " + Filters.Count + ", weighted fields " + Weight.Count + "; sample start '" + Tasks[0][Col["Start"]] + "' -> " + TS[0].ToString("yyyy-MM-dd HH:mm"));
    }

    // Deswik escapes some characters in stored text: __w__ = "_" (LH__w__Stope = LH_Stope), __d__ = "-"
    public static Dictionary<string, string> Esc = new Dictionary<string, string> { { "__w__", "_" }, { "__d__", "-" } };
    static string Unesc(string s) { foreach (var kv in Esc) s = s.Replace(kv.Key, kv.Value); return s; }
    public static int GroupMode = 0;   // GroupedSum_Average: 0 = distinct groups active in the period, 1 = sum of task-fraction
    static double Num(string s) { double v; return double.TryParse(s, NumberStyles.Float, IC, out v) ? v : 0; }
    static bool Like(string v, string pat) { return Regex.IsMatch(v ?? "", "^" + Regex.Escape(pat).Replace("\\*", ".*").Replace("\\?", ".") + "$", RegexOptions.IgnoreCase); }
    static bool Cond(string[] t, string[] c)
    {
        string f = c[1], test = c[2], val = c[3], type = c.Length > 4 ? c[4] : "Text"; int ci; string v = Col.TryGetValue(f, out ci) ? t[ci] : "";
        bool num = type == "Double" || type == "Integer";
        if (test == "=" || test == "Not =") { bool eq = num ? (v == "" ? val == "" : Math.Abs(Num(v) - Num(val)) < 1e-9) : Like(v.Trim(), val.Trim()); return test == "=" ? eq : !eq; }
        double a = Num(v), b = Num(val); if (!num && v.Trim() == "") return false;
        switch (test) { case "<": return a < b; case ">": return a > b; case "<=": return a <= b; case ">=": return a >= b; }
        throw new Exception("unknown test " + test);
    }
    // AND binds tighter than OR
    public static bool[] Match(string name)
    {
        bool[] r; if (FilterCache.TryGetValue(name, out r)) return r;
        r = new bool[Tasks.Count];
        if (name == "" || name == "<No Filtering>") { for (int i = 0; i < r.Length; i++) r[i] = true; }
        else
        {
            List<string[]> cs; if (!Filters.TryGetValue(name, out cs)) throw new Exception("filter not found: " + name);
            for (int i = 0; i < Tasks.Count; i++) { bool any = false, grp = true; for (int k = 0; k < cs.Count; k++) { if (k > 0 && cs[k][0] == "Or") { any |= grp; grp = true; } grp = grp && Cond(Tasks[i], cs[k]); } any |= grp; r[i] = any; }
        }
        FilterCache[name] = r; return r;
    }

    public static List<DsfRow> Rows(string dir, string view)
    {
        // titles / fields / calc from the dxf view block (only the Totaled Fields sub-table)
        int s = Array.FindIndex(L, x => x.TrimStart('\t').StartsWith("3,\"" + view + "\","));
        var rows = new List<DsfRow>(); bool inTf = false;
        for (int i = s + 1; i < L.Length; i++)
        {
            var t = L[i].TrimStart('\t'); if (t.StartsWith("3,") || t.StartsWith("1,")) break;
            if (t.StartsWith("4,")) { inTf = t.StartsWith("4,Totaled Fields"); continue; }
            if (inTf && t.StartsWith("5,")) { var c = Csv(t.Substring(2)); rows.Add(new DsfRow { Field = c[0], Title = c[1], Calc = c[21] }); }
        }
        var xml = new XmlDocument(); xml.LoadXml(File.ReadAllText(Directory.GetFiles(dir, "*.tmp.dsf")[0], Encoding.Unicode));
        XmlNode v = null; foreach (XmlNode n in xml.SelectNodes("//Crosstab__d__Views_Data")) if (n.SelectSingleNode("Name").InnerText == view) v = n;
        var tfs = v.SelectNodes("Totaled__d__Fields");
        if (tfs.Count != rows.Count) throw new Exception(view + ": " + rows.Count + " rows in dxf vs " + tfs.Count + " in xml");
        for (int k = 0; k < rows.Count; k++) foreach (XmlNode f in tfs[k].SelectNodes("Totaled__d__Fields__d__Formulas")) rows[k].Formulas.Add(new[] { f.SelectSingleNode("Filter").InnerText, f.SelectSingleNode("Expression").InnerText, f.SelectSingleNode("Resource").InnerText });
        return rows;
    }

    static double Eval(string expr) { var r = new DataTable().Compute(expr, ""); return r == DBNull.Value ? double.NaN : Convert.ToDouble(r, IC); }
    static Regex FieldRef = new Regex(@"\[([^\]#][^\]]*)\]"), RowRef = new Regex(@"\[#\((\d+),\d+\):[^\]]*\]");

    // value of every row for every period [row, period]; rowRefs resolved after the task pass
    public static double[,] Compute(List<DsfRow> rows, DateTime[] p0, DateTime[] p1, string globalFilter)
    {
        var g = Match(globalFilter); int np = p0.Length; var val = new double[rows.Count, np]; var wsum = new double[rows.Count, np];
        var later = new List<int>(); var exprCache = new Dictionary<string, double[]>();
        for (int r = 0; r < rows.Count; r++)
        {
            var row = rows[r]; if (row.Calc == "0" || row.Calc == "") { for (int p = 0; p < np; p++) val[r, p] = double.NaN; continue; }
            if (row.Calc == "GroupedSum_Average")
            {   // number of distinct groups (task Name) with work in the period
                var sets = new HashSet<string>[np]; for (int p = 0; p < np; p++) sets[p] = new HashSet<string>();
                foreach (var fm in row.Formulas) { var m = Match(fm[0]); for (int i = 0; i < Tasks.Count; i++) { if (!m[i] || !g[i]) continue; if (Col.ContainsKey(row.Field) && Num(Tasks[i][Col[row.Field]]) == 0) continue;
                    for (int p = 0; p < np; p++) if (Math.Min(TF[i].Ticks, p1[p].Ticks) - Math.Max(TS[i].Ticks, p0[p].Ticks) > 0) sets[p].Add(Tasks[i][Col["Name"]]); } }
                for (int p = 0; p < np; p++) val[r, p] = sets[p].Count; continue;
            }
            bool avg = row.Calc == "Average"; string wf = null; if (avg) Weight.TryGetValue(row.Field, out wf);
            bool anyTask = false;
            foreach (var fm in row.Formulas)
            {
                string expr = fm[1];
                if (RowRef.IsMatch(expr)) { later.Add(r); continue; }
                if (row.Field == "" && expr == "") continue;
                anyTask = true; var m = Match(fm[0]);
                for (int i = 0; i < Tasks.Count; i++)
                {
                    if (!m[i] || !g[i]) continue; var t = Tasks[i];
                    double q; if (expr == "") q = Num(t[Col[row.Field]]); else q = Eval(FieldRef.Replace(expr, mm => { int ci; return Col.TryGetValue(mm.Groups[1].Value, out ci) ? Num(t[ci]).ToString("R", IC) : "0"; }));
                    if (q == 0 || double.IsNaN(q)) continue;
                    double w = 1; if (avg) { w = wf != null && Col.ContainsKey(wf) ? Num(t[Col[wf]]) : 1; if (w == 0) continue; }
                    double a = TS[i].Ticks, b = TF[i].Ticks, dur = b - a;
                    for (int p = 0; p < np; p++)
                    {
                        double frac; if (dur <= 0) frac = (a >= p0[p].Ticks && a < p1[p].Ticks) ? 1 : 0;
                        else { double ov = Math.Min(b, p1[p].Ticks) - Math.Max(a, p0[p].Ticks); frac = ov > 0 ? ov / dur : 0; }
                        if (frac <= 0) continue;
                        if (avg) { val[r, p] += q * w * frac; wsum[r, p] += w * frac; } else val[r, p] += q * frac;
                    }
                }
            }
            if (avg) for (int p = 0; p < np; p++) val[r, p] = wsum[r, p] > 0 ? val[r, p] / wsum[r, p] : 0;
            if (!anyTask && !later.Contains(r)) for (int p = 0; p < np; p++) val[r, p] = 0;
        }
        foreach (int r in later.Distinct())
            for (int p = 0; p < np; p++)
            {
                double sum = 0; foreach (var fm in rows[r].Formulas) { if (!RowRef.IsMatch(fm[1])) continue; int pp = p;
                    var e = RowRef.Replace(fm[1], mm => { int k = int.Parse(mm.Groups[1].Value) - 1; double x = (k >= 0 && k < rows.Count) ? val[k, pp] : 0; return double.IsNaN(x) ? "0" : x.ToString("R", IC); });
                    double x2; try { x2 = Eval(e); } catch { x2 = 0; } if (double.IsNaN(x2) || double.IsInfinity(x2)) x2 = 0; sum += x2; }
                val[r, p] = sum;
            }
        return val;
    }

    // field by task Name (e.g. lateral metres by heading) for the Dev Dump tab: Name \t p1 \t p2 ...
    public static string ByName(string field, string filter, DateTime[] p0, DateTime[] p1, string globalFilter, string outFile)
    {
        var m = Match(filter); var g = Match(globalFilter); var acc = new SortedDictionary<string, double[]>(StringComparer.Ordinal);
        for (int i = 0; i < Tasks.Count; i++)
        {
            if (!m[i] || !g[i]) continue; double q = Num(Tasks[i][Col[field]]); if (q == 0) continue; double a = TS[i].Ticks, b = TF[i].Ticks, dur = b - a;
            for (int p = 0; p < p0.Length; p++) { double frac = dur <= 0 ? ((a >= p0[p].Ticks && a < p1[p].Ticks) ? 1 : 0) : Math.Max(0, Math.Min(b, p1[p].Ticks) - Math.Max(a, p0[p].Ticks)) / dur; if (frac <= 0) continue;
                string n = Tasks[i][Col["Name"]]; double[] v; if (!acc.TryGetValue(n, out v)) { v = new double[p0.Length]; acc[n] = v; } v[p] += q * frac; }
        }
        var sb = new StringBuilder(); foreach (var kv in acc) { if (kv.Value.Sum() < 1e-9) continue; sb.Append(kv.Key); foreach (var x in kv.Value) sb.Append('\t').Append(x.ToString("R", IC)); sb.Append('\n'); }
        File.WriteAllText(outFile, sb.ToString()); return "by name: " + acc.Count + " headings -> " + outFile;
    }

    public static string Dump(string dir, string view, DateTime[] p0, DateTime[] p1, string globalFilter, string outFile)
    {
        var rows = Rows(dir, view); var v = Compute(rows, p0, p1, globalFilter);
        var sb = new StringBuilder("Row\tTitle\tField\tCalculate"); foreach (var d in p0) sb.Append('\t').Append(d.ToString("yyyy-MM-dd")); sb.Append('\n');
        for (int r = 0; r < rows.Count; r++) { sb.Append(r).Append('\t').Append(rows[r].Title).Append('\t').Append(rows[r].Field).Append('\t').Append(rows[r].Calc); for (int p = 0; p < p0.Length; p++) sb.Append('\t').Append(double.IsNaN(v[r, p]) ? "" : v[r, p].ToString("R", IC)); sb.Append('\n'); }
        File.WriteAllText(outFile, sb.ToString()); return view + ": " + rows.Count + " rows x " + p0.Length + " periods -> " + outFile;
    }
}
