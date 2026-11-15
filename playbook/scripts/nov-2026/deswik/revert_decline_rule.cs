if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
// Undo decline_rule.cs: delete every link listed as "ADD hold:" / "ADD jstp:" in C:\dswtmp\h_dec_links_added.txt ("... [fromId] -> ... [toId]").
var byId = Schedule.Tasks.Where(t => !t.Deleted).ToDictionary(t => t.Id);
var rx = new System.Text.RegularExpressions.Regex(@"ADD (hold|jstp): .*?\[([^\]]+)\] -> .*?\[([^\]]+)\]");
int del = 0, miss = 0;
foreach (var line in System.IO.File.ReadAllLines(@"C:\dswtmp\h_dec_links_added.txt")) {
  var m = rx.Match(line); if (!m.Success) continue;
  if (!byId.TryGetValue(m.Groups[2].Value, out var a) || !byId.TryGetValue(m.Groups[3].Value, out var b)) { miss++; Out.AppendLine("task not found: " + line.Trim()); continue; }
  var d = Schedule.Dependencies.FirstOrDefault(x => !x.Deleted && x.From == System.Convert.ToInt64(a.UniqueId) && x.To == System.Convert.ToInt64(b.UniqueId));
  if (d == null) { miss++; Out.AppendLine("link not found: " + line.Trim()); continue; }
  Schedule.Delete(d, false, false); del++;
}
Out.AppendLine($"deleted {del} links, not found {miss}");
return null;
