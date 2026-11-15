// Read-only: export all IS tasks (every field) and all dependencies to C:\dswtmp. Nothing in Deswik changes.
var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
string C(object v) => v == null ? "" : (v is IFormattable fm ? fm.ToString(null, System.Globalization.CultureInfo.InvariantCulture) : v.ToString()).Replace("\t", " ").Replace("\r", " ").Replace("\n", " ");
var fields = Schedule.GetTaskFields().Select(c => c.PropertyName).Where(p => !string.IsNullOrEmpty(p)).Distinct().ToList();
var rows = Schedule.ExportTaskData(fields);
var sb = new StringBuilder();
sb.AppendLine("TaskID\t" + string.Join("\t", fields));
int n = 0; foreach (var r in rows) { sb.AppendLine(string.Join("\t", ((IEnumerable<object>)r).Select(C))); n++; }
System.IO.Directory.CreateDirectory(@"C:\dswtmp");
var p1 = @"C:\dswtmp\Schedule_Tasks_" + stamp + ".tsv"; System.IO.File.WriteAllText(p1, sb.ToString(), new UTF8Encoding(true));
var byUid = new Dictionary<int, Deswik.Scheduler.Engine.Task>(); foreach (var t in Schedule.Tasks) if (!t.Deleted) byUid[t.UniqueId] = t;
string F(Deswik.Scheduler.Engine.Task t, string f) { try { return C(t.GetType().GetMethod("GetFieldValue", new[] { typeof(string) })?.Invoke(t, new object[] { f })); } catch { return ""; } }
var db = new StringBuilder("FromUID\tFromID\tFromName\tFromParent\tFromActNo\tToUID\tToID\tToName\tToParent\tToActNo\tLinkType\tLag\tLayerId\n");
int nd = 0;
foreach (var d in Schedule.Dependencies)
{
    if (d.Deleted) continue; byUid.TryGetValue(d.From, out var a); byUid.TryGetValue(d.To, out var b);
    db.Append(d.From).Append('\t').Append(a?.Id).Append('\t').Append(C(a?.Name)).Append('\t').Append(a == null ? "" : F(a, "DIS Parent ID")).Append('\t').Append(a == null ? "" : F(a, "DIS Activity Number")).Append('\t')
      .Append(d.To).Append('\t').Append(b?.Id).Append('\t').Append(C(b?.Name)).Append('\t').Append(b == null ? "" : F(b, "DIS Parent ID")).Append('\t').Append(b == null ? "" : F(b, "DIS Activity Number")).Append('\t')
      .Append(d.LinkType).Append('\t').Append(C(d.Lag)).Append('\t').Append(d.LayerId).Append('\n'); nd++;
}
var p2 = @"C:\dswtmp\Schedule_Dependencies_" + stamp + ".tsv"; System.IO.File.WriteAllText(p2, db.ToString(), new UTF8Encoding(true));
Out.AppendLine("TASKS " + n + " FIELDS " + fields.Count + " -> " + p1);
Out.AppendLine("DEPS " + nd + " -> " + p2);
Out.AppendLine("PARENT_FIELD_SAMPLE " + string.Join(" | ", Schedule.Tasks.Where(t => !t.Deleted).Take(3).Select(t => t.Id + ":" + F(t, "DIS Parent ID") + "/" + F(t, "DIS Activity Number"))));
Out.AppendLine("FIELDS_WITH_DIS " + string.Join(" | ", Schedule.GetTaskFields().Where(c => (c.DisplayName ?? "").Contains("DIS") || (c.PropertyName ?? "").Contains("DIS")).Select(c => c.DisplayName + "=" + c.PropertyName)));
return null;
