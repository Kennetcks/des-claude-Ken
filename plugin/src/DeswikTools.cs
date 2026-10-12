using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Deswik.Common.Utilities;
using Deswik.InteractiveScheduler;
using Deswik.Scheduler.API.Contracts;
using Deswik.Scheduler.Engine;
using Newtonsoft.Json.Linq;
using DApp = Deswik.Graphics.Application;
using STask = Deswik.Scheduler.Engine.Task;

namespace Kennet.DeswikClaude
{
    public class ToolResult
    {
        public string Text;
        public bool IsError;
        public static ToolResult Ok(string t) { return new ToolResult { Text = t }; }
        public static ToolResult Err(string t) { return new ToolResult { Text = t, IsError = true }; }
    }

    /// <summary>One field value Claude changed, so it can be put back.</summary>
    public class FieldChange
    {
        public STask Task;
        public string TaskId;
        public string Property;
        public string OldValue;
        public string NewValue;
    }

    /// <summary>
    /// The tools Claude can call. Every read is free; every change asks the user first (ApprovalDialog).
    /// All methods run on the UI thread.
    /// </summary>
    public class DeswikTools
    {
        private const int MaxResultChars = 60000;
        private readonly ClaudeChatPlugin _plugin;
        private readonly IWin32Window _owner;
        private readonly Stack<List<FieldChange>> _undo = new Stack<List<FieldChange>>();

        public DeswikTools(ClaudeChatPlugin plugin, IWin32Window owner)
        {
            _plugin = plugin;
            _owner = owner;
        }

        public int UndoDepth { get { return _undo.Count; } }

        // ===================================================================================== access
        private DApp App { get { return _plugin.Application; } }

        private InteractiveScheduler IsPlugin
        {
            get
            {
                try
                {
                    if (App == null || App.Plugins == null) return null;
                    var p = App.Plugins.FindName("Deswik.InteractiveScheduler");
                    return p == null ? null : p.Instance as InteractiveScheduler;
                }
                catch { return null; }
            }
        }

        private ApplicationScheduler AppScheduler
        {
            get
            {
                var isp = IsPlugin;
                return (isp == null || isp.MainPanel == null) ? null : isp.MainPanel.AppScheduler;
            }
        }

        public Schedule Schedule
        {
            get
            {
                var a = AppScheduler;
                return a == null ? null : a.Project;
            }
        }

        private Schedule RequireSchedule()
        {
            var s = Schedule;
            if (s == null)
                throw new UserFacingException("No IS schedule is loaded. Open the Interactive Scheduler panel and load the schedule (or call run_is_command with IS_LoadSchedule), then try again.");
            return s;
        }

        private void AfterScheduleEdit(Schedule s)
        {
            try { s.ScheduleCalculator.CalculateSchedule(true, false); } catch { }
            try { s.ScheduleCalculator.WaitForCalculationsToComplete(false, 120000); } catch { }
            try { var a = AppScheduler; if (a != null) a.OnTasksUpdated(); } catch { }
            try { if (App != null) App.Redraw(true); } catch { }
        }

        // ===================================================================================== schema
        public static JArray Definitions()
        {
            return JArray.Parse(@"[
{ ""name"": ""get_deswik_guide"",
  ""description"": ""Returns the working rules and the verified Deswik CAD / Scheduler API cheat sheet (needed before writing run_csharp code). Call once per conversation, together with get_context."",
  ""input_schema"": { ""type"": ""object"", ""properties"": {} } },

{ ""name"": ""get_context"",
  ""description"": ""Snapshot of what is open: CAD document, active layer, selection count, whether the Interactive Scheduler (IS) has a schedule loaded, task/dependency counts, resources, target start. Call this first in a new conversation."",
  ""input_schema"": { ""type"": ""object"", ""properties"": {} } },

{ ""name"": ""list_layers"",
  ""description"": ""List CAD layer names (with visibility). Optional case-insensitive filter."",
  ""input_schema"": { ""type"": ""object"", ""properties"": {
      ""contains"": { ""type"": ""string"" },
      ""max"": { ""type"": ""integer"", ""default"": 300 } } } },

{ ""name"": ""get_selection"",
  ""description"": ""Describe the figures currently selected in CAD (type and layer)."",
  ""input_schema"": { ""type"": ""object"", ""properties"": { ""max"": { ""type"": ""integer"", ""default"": 200 } } } },

{ ""name"": ""is_list_fields"",
  ""description"": ""List all task fields/columns in the loaded schedule (display name, internal property name, category). Use the names returned here in other tools."",
  ""input_schema"": { ""type"": ""object"", ""properties"": { ""contains"": { ""type"": ""string"" } } } },

{ ""name"": ""is_query_tasks"",
  ""description"": ""Query tasks in the loaded IS schedule as a table. Always returns Id, Start, Finish, Duration plus the requested fields. Filters are ANDed. Numeric ops (gt, lt, ge, le) compare as numbers; equals/contains/ne/starts compare as text, case-insensitive."",
  ""input_schema"": { ""type"": ""object"", ""properties"": {
      ""fields"": { ""type"": ""array"", ""items"": { ""type"": ""string"" }, ""description"": ""Extra fields to return (display or property names)."" },
      ""id_contains"": { ""type"": ""string"" },
      ""where"": { ""type"": ""array"", ""items"": { ""type"": ""object"", ""properties"": {
            ""field"": { ""type"": ""string"" },
            ""op"": { ""type"": ""string"", ""enum"": [""equals"", ""ne"", ""contains"", ""starts"", ""gt"", ""lt"", ""ge"", ""le"", ""empty"", ""notempty""] },
            ""value"": { ""type"": ""string"" } }, ""required"": [""field"", ""op""] } },
      ""include_parents"": { ""type"": ""boolean"", ""default"": false },
      ""offset"": { ""type"": ""integer"", ""default"": 0 },
      ""max_rows"": { ""type"": ""integer"", ""default"": 100 } } } },

{ ""name"": ""is_task_details"",
  ""description"": ""All non-empty field values for one task, plus its predecessors and successors (link type, lag, layer)."",
  ""input_schema"": { ""type"": ""object"", ""properties"": { ""task_id"": { ""type"": ""string"" } }, ""required"": [""task_id""] } },

{ ""name"": ""is_set_task_fields"",
  ""description"": ""Change field values on one or more tasks (e.g. rate, duration, custom/production fields, constraint, notes). The user approves the full list first. Old values are kept so the user can press 'Undo last AI change'. Schedule recalculates afterwards."",
  ""input_schema"": { ""type"": ""object"", ""properties"": {
      ""reason"": { ""type"": ""string"", ""description"": ""One line explaining why, shown to the user."" },
      ""changes"": { ""type"": ""array"", ""items"": { ""type"": ""object"", ""properties"": {
            ""task_id"": { ""type"": ""string"" },
            ""field"": { ""type"": ""string"" },
            ""value"": { ""type"": ""string"" } }, ""required"": [""task_id"", ""field"", ""value""] } } },
    ""required"": [""changes"", ""reason""] } },

{ ""name"": ""is_add_dependencies"",
  ""description"": ""Add dependencies between tasks (native Deswik undo applies). link_type: FS, SS, FF, SF. lag examples: '0d', '2d', '12h', '-1d'. The user approves first."",
  ""input_schema"": { ""type"": ""object"", ""properties"": {
      ""reason"": { ""type"": ""string"" },
      ""dependencies"": { ""type"": ""array"", ""items"": { ""type"": ""object"", ""properties"": {
            ""from_id"": { ""type"": ""string"" },
            ""to_id"": { ""type"": ""string"" },
            ""link_type"": { ""type"": ""string"", ""enum"": [""FS"", ""SS"", ""FF"", ""SF""], ""default"": ""FS"" },
            ""lag"": { ""type"": ""string"" },
            ""layer_id"": { ""type"": ""integer"" } }, ""required"": [""from_id"", ""to_id""] } } },
    ""required"": [""dependencies"", ""reason""] } },

{ ""name"": ""is_level_schedule"",
  ""description"": ""Run resource levelling on the loaded schedule with its current levelling settings. Can take a while. The user approves first."",
  ""input_schema"": { ""type"": ""object"", ""properties"": { ""reason"": { ""type"": ""string"" } }, ""required"": [""reason""] } },

{ ""name"": ""list_is_commands"",
  ""description"": ""List Interactive Scheduler command IDs (e.g. IS_LoadSchedule, IS_UpdateStartAndFinishDatesAll)."",
  ""input_schema"": { ""type"": ""object"", ""properties"": {} } },

{ ""name"": ""run_is_command"",
  ""description"": ""Run an Interactive Scheduler command by ID (opens the same dialog/action as the IS menu). The user approves first."",
  ""input_schema"": { ""type"": ""object"", ""properties"": { ""command_id"": { ""type"": ""string"" }, ""reason"": { ""type"": ""string"" } }, ""required"": [""command_id"", ""reason""] } },

{ ""name"": ""run_deswik_command"",
  ""description"": ""Run a built-in Deswik CAD command by its CID (e.g. CID_View_Zoom_All, CID_EDIT_UNDO). The user approves first."",
  ""input_schema"": { ""type"": ""object"", ""properties"": { ""command_id"": { ""type"": ""string"" }, ""reason"": { ""type"": ""string"" } }, ""required"": [""command_id"", ""reason""] } },

{ ""name"": ""run_csharp"",
  ""description"": ""Write and run C# inside Deswik for anything the other tools can't do: create/modify CAD geometry and layers, bulk analysis, reports, custom schedule edits. You supply the BODY of: object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out). Schedule may be null. Write results with Out.AppendLine(...). C# 5 ONLY (no $\""\"" interpolation, no ?. , no nameof, no => members, no out var). The user sees the code and approves it; it is also saved to the Scripts folder."",
  ""input_schema"": { ""type"": ""object"", ""properties"": {
      ""description"": { ""type"": ""string"", ""description"": ""Short title of what the code does."" },
      ""usings"": { ""type"": ""array"", ""items"": { ""type"": ""string"" }, ""description"": ""Extra namespaces, e.g. Deswik.Graphics.Figures"" },
      ""code"": { ""type"": ""string"" },
      ""modifies"": { ""type"": ""boolean"", ""description"": ""true if the code changes the drawing or schedule."" } },
    ""required"": [""description"", ""code"", ""modifies""] } }
]");
        }

        private static readonly string[] ReadOnlyTools =
        {
            "get_deswik_guide", "get_context", "list_layers", "get_selection", "is_list_fields",
            "is_query_tasks", "is_task_details", "list_is_commands"
        };

        /// <summary>Same tools in MCP format (inputSchema + annotations), for the Claude desktop app bridge.</summary>
        public static JArray McpToolList()
        {
            var list = new JArray();
            foreach (JObject d in Definitions())
            {
                string name = (string)d["name"];
                bool ro = ReadOnlyTools.Contains(name);
                list.Add(new JObject
                {
                    ["name"] = name,
                    ["description"] = d["description"],
                    ["inputSchema"] = d["input_schema"],
                    ["annotations"] = new JObject
                    {
                        ["readOnlyHint"] = ro,
                        ["destructiveHint"] = !ro,
                        ["openWorldHint"] = false
                    }
                });
            }
            return list;
        }

        public string DocumentName()
        {
            try { return App == null ? "(no document)" : (string.IsNullOrEmpty(App.Filename) ? "(unsaved drawing)" : App.Filename); }
            catch { return "(unknown)"; }
        }

        // ===================================================================================== dispatch
        public ToolResult Execute(string name, JObject input)
        {
            try
            {
                string text;
                switch (name)
                {
                    case "get_deswik_guide": text = SystemPrompt.Text; break;
                    case "get_context": text = GetContext(); break;
                    case "list_layers": text = ListLayers(Str(input, "contains"), Int(input, "max", 300)); break;
                    case "get_selection": text = GetSelection(Int(input, "max", 200)); break;
                    case "is_list_fields": text = ListFields(Str(input, "contains")); break;
                    case "is_query_tasks": text = QueryTasks(input); break;
                    case "is_task_details": text = TaskDetails(Str(input, "task_id")); break;
                    case "is_set_task_fields": text = SetTaskFields(input); break;
                    case "is_add_dependencies": text = AddDependencies(input); break;
                    case "is_level_schedule": text = LevelSchedule(Str(input, "reason")); break;
                    case "list_is_commands": text = ListIsCommands(); break;
                    case "run_is_command": text = RunIsCommand(Str(input, "command_id"), Str(input, "reason")); break;
                    case "run_deswik_command": text = RunDeswikCommand(Str(input, "command_id"), Str(input, "reason")); break;
                    case "run_csharp": text = RunCSharp(input); break;
                    default: return ToolResult.Err("Unknown tool: " + name);
                }
                if (text.Length > MaxResultChars)
                    text = text.Substring(0, MaxResultChars) + "\n…[truncated: narrow the query or use offset]";
                return ToolResult.Ok(text);
            }
            catch (DeclinedException)
            {
                return ToolResult.Err("The user declined this action. Ask what they would like instead.");
            }
            catch (UserFacingException ex)
            {
                return ToolResult.Err(ex.Message);
            }
            catch (Exception ex)
            {
                return ToolResult.Err(ex.GetType().Name + ": " + ex.Message);
            }
        }

        // ===================================================================================== reads
        private string GetContext()
        {
            var sb = new StringBuilder();
            var app = App;
            if (app == null) return "No CAD document is active.";
            sb.AppendLine("CAD document: " + (string.IsNullOrEmpty(app.Filename) ? "(unsaved)" : app.Filename));
            try { sb.AppendLine("Active layer: " + (app.ActiveLayer != null ? app.ActiveLayer.Name : "(none)")); } catch { }
            try { sb.AppendLine("Layers: " + app.Layers.Count); } catch { }
            try { sb.AppendLine("Selected figures: " + app.Selections.MainSelection.Count); } catch { }

            var isp = IsPlugin;
            sb.AppendLine("Interactive Scheduler plugin loaded: " + (isp != null));
            var s = Schedule;
            if (s == null)
            {
                sb.AppendLine("IS schedule: none loaded");
                return sb.ToString();
            }
            sb.AppendLine("IS schedule file: " + s.FileName);
            try { sb.AppendLine("Target start: " + s.AsConstrainedDate(s.TargetStart).ToString("yyyy-MM-dd")); } catch { }
            var tasks = s.Tasks.Where(t => !t.Deleted).ToList();
            sb.AppendLine("Tasks: " + tasks.Count + " (parents: " + tasks.Count(t => t.IsParent) + ")");
            if (tasks.Count > 0)
            {
                sb.AppendLine("Earliest start: " + tasks.Min(t => t.Start).ToString("yyyy-MM-dd HH:mm"));
                sb.AppendLine("Latest finish: " + tasks.Max(t => t.Finish).ToString("yyyy-MM-dd HH:mm"));
            }
            sb.AppendLine("Dependencies: " + s.Dependencies.Count(d => !d.Deleted));
            try
            {
                var res = s.GetResourceNames();
                sb.AppendLine("Resources (" + res.Count + "): " + string.Join(", ", res.Take(80)));
            }
            catch { }
            try { sb.AppendLine("Task fields: " + s.GetTaskFields().Count + " (use is_list_fields)"); } catch { }
            sb.AppendLine("AI changes that can be undone: " + _undo.Count);
            return sb.ToString();
        }

        private string ListLayers(string contains, int max)
        {
            if (App == null) return "No CAD document is active.";
            var sb = new StringBuilder();
            int n = 0, total = 0;
            foreach (var layer in App.Layers)
            {
                if (!string.IsNullOrEmpty(contains) && layer.Name.IndexOf(contains, StringComparison.OrdinalIgnoreCase) < 0) continue;
                total++;
                if (n >= max) continue;
                sb.AppendLine(layer.Name + (layer.Visible ? "" : "  [hidden]"));
                n++;
            }
            sb.Insert(0, "Layers matching: " + total + (total > n ? " (showing " + n + ")" : "") + "\n");
            return sb.ToString();
        }

        private string GetSelection(int max)
        {
            if (App == null) return "No CAD document is active.";
            var sel = App.Selections.MainSelection;
            if (sel == null || sel.Count == 0) return "Nothing is selected.";
            var sb = new StringBuilder("Selected: " + sel.Count + "\n");
            var byType = new Dictionary<string, int>();
            int i = 0;
            foreach (var f in sel)
            {
                string type = f.GetType().Name;
                string layer = "";
                try { layer = f.Layer != null ? f.Layer.Name : ""; } catch { }
                string key = type + " on " + layer;
                int c; byType.TryGetValue(key, out c); byType[key] = c + 1;
                if (++i >= 100000) break;
            }
            foreach (var kv in byType.OrderByDescending(k => k.Value).Take(max))
                sb.AppendLine(kv.Value + " × " + kv.Key);
            return sb.ToString();
        }

        private string ListFields(string contains)
        {
            var s = RequireSchedule();
            var sb = new StringBuilder("DisplayName | PropertyName | Category\n");
            foreach (var col in s.GetTaskFields())
            {
                string dn = Safe(() => col.DisplayName), pn = Safe(() => col.PropertyName), cat = Safe(() => col.DisplayCategory);
                if (!string.IsNullOrEmpty(contains) &&
                    dn.IndexOf(contains, StringComparison.OrdinalIgnoreCase) < 0 &&
                    pn.IndexOf(contains, StringComparison.OrdinalIgnoreCase) < 0) continue;
                sb.AppendLine(dn + " | " + pn + " | " + cat);
            }
            return sb.ToString();
        }

        private string QueryTasks(JObject input)
        {
            var s = RequireSchedule();
            var cols = new List<TaskGridColumn>();
            var unknown = new List<string>();
            foreach (string f in StrList(input, "fields"))
            {
                var c = ResolveField(s, f);
                if (c == null) unknown.Add(f); else cols.Add(c);
            }

            var filters = new List<Tuple<TaskGridColumn, string, string>>();
            var where = input["where"] as JArray;
            if (where != null)
                foreach (JObject w in where.OfType<JObject>())
                {
                    string fname = (string)w["field"];
                    var c = ResolveField(s, fname);
                    if (c == null) { unknown.Add(fname); continue; }
                    filters.Add(Tuple.Create(c, ((string)w["op"] ?? "equals").ToLowerInvariant(), (string)w["value"] ?? ""));
                }
            if (unknown.Count > 0)
                return "Unknown field(s): " + string.Join(", ", unknown.Distinct()) + ". Use is_list_fields to see valid names.";

            string idContains = Str(input, "id_contains");
            bool includeParents = Bool(input, "include_parents", false);
            int offset = Int(input, "offset", 0), maxRows = Math.Min(Int(input, "max_rows", 100), 2000);

            var matches = new List<STask>();
            foreach (var t in s.Tasks)
            {
                if (t.Deleted) continue;
                if (!includeParents && t.IsParent) continue;
                if (!string.IsNullOrEmpty(idContains) && (t.Id ?? "").IndexOf(idContains, StringComparison.OrdinalIgnoreCase) < 0) continue;
                bool ok = true;
                foreach (var flt in filters)
                    if (!Matches(Value(t, flt.Item1), flt.Item2, flt.Item3)) { ok = false; break; }
                if (ok) matches.Add(t);
            }

            var sb = new StringBuilder();
            sb.AppendLine("Matching tasks: " + matches.Count + (matches.Count > maxRows ? " (rows " + offset + "–" + Math.Min(offset + maxRows, matches.Count) + ")" : ""));
            sb.Append("Id\tStart\tFinish\tDuration");
            foreach (var c in cols) sb.Append('\t').Append(Safe(() => c.DisplayName));
            sb.AppendLine();
            foreach (var t in matches.Skip(offset).Take(maxRows))
            {
                sb.Append(t.Id).Append('\t').Append(t.Start.ToString("yyyy-MM-dd HH:mm")).Append('\t')
                  .Append(t.Finish.ToString("yyyy-MM-dd HH:mm")).Append('\t').Append(Safe(() => t.DurationDesc));
                foreach (var c in cols) sb.Append('\t').Append(Value(t, c));
                sb.AppendLine();
            }
            return sb.ToString();
        }

        private string TaskDetails(string taskId)
        {
            var s = RequireSchedule();
            var t = FindTask(s, taskId);
            var sb = new StringBuilder("Task " + t.Id + " (UniqueId " + t.UniqueId + ")\n");
            foreach (var col in s.GetTaskFields())
            {
                string v = Value(t, col);
                if (!string.IsNullOrEmpty(v)) sb.AppendLine(Safe(() => col.DisplayName) + " = " + v);
            }
            sb.AppendLine("\nPredecessors:");
            AppendDeps(sb, s, s.Dependencies.Where(d => !d.Deleted && d.To == t.UniqueId), true);
            sb.AppendLine("Successors:");
            AppendDeps(sb, s, s.Dependencies.Where(d => !d.Deleted && d.From == t.UniqueId), false);
            return sb.ToString();
        }

        private void AppendDeps(StringBuilder sb, Schedule s, IEnumerable<Dependency> deps, bool pred)
        {
            int n = 0;
            foreach (var d in deps)
            {
                STask other;
                string otherId = s.TryGetTaskUniqueId(pred ? d.From : d.To, out other) ? other.Id : "#" + (pred ? d.From : d.To);
                string lag = "";
                try { if (d.HasLag) lag = " lag " + d.Lag; } catch { }
                string layer = "";
                try { layer = " layer " + new Deswik.Scheduler.API.ScheduleQuery(s).DependencyLayerName(d.LayerId); } catch { }
                sb.AppendLine("  " + otherId + "  " + d.LinkType + lag + layer);
                n++;
            }
            if (n == 0) sb.AppendLine("  (none)");
        }

        private string ListIsCommands()
        {
            string[] ids, names;
            InteractiveScheduler.GetCommands(out ids, out names);
            var sb = new StringBuilder();
            for (int i = 0; i < Math.Min(ids.Length, names.Length); i++) sb.AppendLine(ids[i] + " — " + names[i]);
            return sb.ToString();
        }

        // ===================================================================================== writes
        private string SetTaskFields(JObject input)
        {
            var s = RequireSchedule();
            var arr = input["changes"] as JArray;
            if (arr == null || arr.Count == 0) return "No changes supplied.";

            var batch = new List<FieldChange>();
            var problems = new List<string>();
            foreach (JObject c in arr.OfType<JObject>())
            {
                string id = (string)c["task_id"], field = (string)c["field"], value = (string)c["value"] ?? "";
                STask t = s.GetTask(id);
                if (t == null || t.Deleted) { problems.Add("task not found: " + id); continue; }
                var col = ResolveField(s, field);
                if (col == null) { problems.Add("field not found: " + field); continue; }
                batch.Add(new FieldChange { Task = t, TaskId = t.Id, Property = col.PropertyName, OldValue = Value(t, col), NewValue = value });
            }
            if (problems.Count > 0)
                return "Nothing changed. Fix these first: " + string.Join("; ", problems.Distinct());

            var details = new StringBuilder("Task\tField\tOld → New\n");
            foreach (var b in batch) details.AppendLine(b.TaskId + "\t" + b.Property + "\t" + b.OldValue + " → " + b.NewValue);
            Approve("Change " + batch.Count + " task field value(s)", Str(input, "reason"), details.ToString());

            var applied = new List<FieldChange>();
            var failed = new List<string>();
            foreach (var b in batch)
            {
                try { b.Task.SetFieldValue(b.Property, b.NewValue); applied.Add(b); }
                catch (Exception ex) { failed.Add(b.TaskId + "." + b.Property + ": " + ex.Message); }
            }
            if (applied.Count > 0) _undo.Push(applied);
            AfterScheduleEdit(s);

            var res = new StringBuilder("Applied " + applied.Count + " of " + batch.Count + " change(s). Schedule recalculated.");
            if (failed.Count > 0) res.Append("\nFailed: ").Append(string.Join("; ", failed));
            // Read back so Claude can confirm what Deswik actually stored.
            foreach (var b in applied.Take(50))
            {
                var col = ResolveField(s, b.Property);
                res.Append("\n").Append(b.TaskId).Append('.').Append(b.Property).Append(" now = ").Append(col == null ? "?" : Value(b.Task, col));
            }
            return res.ToString();
        }

        public string UndoLast()
        {
            var s = Schedule;
            if (_undo.Count == 0) return "Nothing to undo.";
            if (s == null) return "Schedule is not loaded; cannot undo.";
            var batch = _undo.Pop();
            int n = 0;
            foreach (var b in Enumerable.Reverse(batch))
            {
                try { b.Task.SetFieldValue(b.Property, b.OldValue); n++; } catch { }
            }
            AfterScheduleEdit(s);
            return "Restored " + n + " field value(s).";
        }

        private string AddDependencies(JObject input)
        {
            var s = RequireSchedule();
            var arr = input["dependencies"] as JArray;
            if (arr == null || arr.Count == 0) return "No dependencies supplied.";

            var list = new List<Tuple<STask, STask, LinkType, string, int?>>();
            var problems = new List<string>();
            foreach (JObject d in arr.OfType<JObject>())
            {
                STask from = s.GetTask((string)d["from_id"]), to = s.GetTask((string)d["to_id"]);
                if (from == null) { problems.Add("from task not found: " + d["from_id"]); continue; }
                if (to == null) { problems.Add("to task not found: " + d["to_id"]); continue; }
                LinkType lt;
                switch (((string)d["link_type"] ?? "FS").ToUpperInvariant())
                {
                    case "SS": lt = LinkType.StartStart; break;
                    case "FF": lt = LinkType.FinishFinish; break;
                    case "SF": lt = LinkType.StartFinish; break;
                    default: lt = LinkType.FinishStart; break;
                }
                int? layer = d["layer_id"] != null && d["layer_id"].Type == JTokenType.Integer ? (int?)(int)d["layer_id"] : null;
                list.Add(Tuple.Create(from, to, lt, (string)d["lag"], layer));
            }
            if (problems.Count > 0) return "Nothing added. Fix these first: " + string.Join("; ", problems);

            var details = new StringBuilder("From\tTo\tType\tLag\n");
            foreach (var x in list) details.AppendLine(x.Item1.Id + "\t" + x.Item2.Id + "\t" + x.Item3 + "\t" + (x.Item4 ?? ""));
            Approve("Add " + list.Count + " dependenc" + (list.Count == 1 ? "y" : "ies"), Str(input, "reason"), details.ToString());

            int ok = 0;
            var failed = new List<string>();
            foreach (var x in list)
            {
                var dep = new Dependency(-1, x.Item1.UniqueId, x.Item2.UniqueId)
                {
                    LinkType = x.Item3,
                    FromId = x.Item1.Id,
                    ToId = x.Item2.Id
                };
                if (x.Item5.HasValue) dep.LayerId = x.Item5.Value;
                if (!string.IsNullOrWhiteSpace(x.Item4)) dep.Lag = new Duration(x.Item4.Trim(), TimeUnit.d);
                if (s.Add(dep, false, true, false)) ok++;
                else failed.Add(x.Item1.Id + "→" + x.Item2.Id + " (" + s.LastErrorCode + ")");
            }
            AfterScheduleEdit(s);
            return "Added " + ok + " of " + list.Count + " dependencies (Ctrl+Z in the scheduler undoes them)." +
                   (failed.Count > 0 ? "\nFailed: " + string.Join("; ", failed) : "");
        }

        private string LevelSchedule(string reason)
        {
            var s = RequireSchedule();
            Approve("Run resource levelling", reason, "Levels " + s.Tasks.Count(t => !t.Deleted) + " tasks in " + s.FileName +
                    " using the schedule's current levelling settings.\nSave the schedule first if you want a restore point.");
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                var leveller = new ResourceLeveller(s, null, LevellingSource.Standard);
                IList<string> errors;
                bool ok = leveller.Level(out errors, true);
                AfterScheduleEdit(s);
                return (ok ? "Levelling finished." : "Levelling reported failure.") +
                       (errors != null && errors.Count > 0 ? "\nMessages:\n" + string.Join("\n", errors.Take(50)) : "");
            }
            finally { Cursor.Current = Cursors.Default; }
        }

        private string RunIsCommand(string id, string reason)
        {
            var isp = IsPlugin;
            if (isp == null) return "The Interactive Scheduler plugin is not loaded in CAD.";
            Approve("Run IS command " + id, reason, id);
            isp.ExecuteById(id);
            return "Ran " + id + ". If it opened a dialog, the user is completing it.";
        }

        private string RunDeswikCommand(string id, string reason)
        {
            if (App == null) return "No CAD document is active.";
            Approve("Run Deswik command " + id, reason, id);
            bool ok = App.MenuCommand.ExecuteByID(id, CommandOrigin.OtherCommand);
            return ok ? "Ran " + id + "." : "Deswik did not run " + id + " (unknown or unavailable command).";
        }

        private string RunCSharp(JObject input)
        {
            string desc = Str(input, "description"), code = Str(input, "code");
            var usings = StrList(input, "usings");
            bool modifies = Bool(input, "modifies", true);
            if (string.IsNullOrWhiteSpace(code)) return "No code supplied.";

            string shown = "// " + desc + "\n// usings: " + string.Join(", ", usings) + "\n\n" + code;
            Approve((modifies ? "Run code that CHANGES your data: " : "Run read-only code: ") + desc,
                    modifies ? "This code modifies the drawing or schedule. Save first if you want a restore point." : "Claude says this code only reads data.",
                    shown, "Run code");

            string saved = "";
            try { saved = CodeRunner.SaveScript(desc, code, usings); } catch { }
            string result = CodeRunner.CompileAndRun(code, usings, App, Schedule);
            if (modifies && Schedule != null) AfterScheduleEdit(Schedule);
            return result + (saved.Length > 0 ? "\n[script saved: " + saved + "]" : "");
        }

        // ===================================================================================== helpers
        private void Approve(string title, string summary, string details, string approveText = "Approve")
        {
            if (!ApprovalDialog.Ask(_owner, title, string.IsNullOrWhiteSpace(summary) ? title : summary, details, approveText))
                throw new DeclinedException();
        }

        private static STask FindTask(Schedule s, string id)
        {
            var t = s.GetTask(id);
            if (t == null || t.Deleted) throw new UserFacingException("Task not found: " + id);
            return t;
        }

        private static TaskGridColumn ResolveField(Schedule s, string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            name = name.Trim();
            var cols = s.GetTaskFields();
            Func<TaskGridColumn, string>[] keys =
            {
                c => c.PropertyName, c => c.DisplayName, c => c.Field, c => c.NiceTitle, c => c.ExternalName
            };
            foreach (var key in keys)
                foreach (var c in cols)
                {
                    string k = Safe(() => key(c));
                    if (k.Length > 0 && string.Equals(k, name, StringComparison.OrdinalIgnoreCase)) return c;
                }
            return null;
        }

        private static string Value(STask t, TaskGridColumn c)
        {
            try
            {
                var v = t.GetNullaleFieldFullValue(c.PropertyName);
                return v == null ? "" : (v.ToString() ?? "");
            }
            catch { return ""; }
        }

        private static bool Matches(string actual, string op, string value)
        {
            actual = actual ?? "";
            switch (op)
            {
                case "empty": return actual.Trim().Length == 0;
                case "notempty": return actual.Trim().Length > 0;
                case "ne": return !string.Equals(actual, value, StringComparison.OrdinalIgnoreCase);
                case "contains": return actual.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
                case "starts": return actual.StartsWith(value, StringComparison.OrdinalIgnoreCase);
                case "gt": case "lt": case "ge": case "le":
                    double a, b;
                    if (!TryNum(actual, out a) || !TryNum(value, out b))
                    {
                        DateTime da, db;
                        if (DateTime.TryParse(actual, out da) && DateTime.TryParse(value, out db)) { a = da.Ticks; b = db.Ticks; }
                        else return false;
                    }
                    return op == "gt" ? a > b : op == "lt" ? a < b : op == "ge" ? a >= b : a <= b;
                default: return string.Equals(actual.Trim(), value.Trim(), StringComparison.OrdinalIgnoreCase);
            }
        }

        private static bool TryNum(string s, out double d)
        {
            return double.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out d) ||
                   double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out d);
        }

        private static string Safe(Func<string> f) { try { return f() ?? ""; } catch { return ""; } }
        private static string Str(JObject o, string k) { var t = o == null ? null : o[k]; return t == null || t.Type == JTokenType.Null ? "" : t.ToString(); }
        private static int Int(JObject o, string k, int def) { var t = o == null ? null : o[k]; int v; return t != null && int.TryParse(t.ToString(), out v) ? v : def; }
        private static bool Bool(JObject o, string k, bool def) { var t = o == null ? null : o[k]; bool v; return t != null && bool.TryParse(t.ToString(), out v) ? v : def; }
        private static List<string> StrList(JObject o, string k)
        {
            var a = o == null ? null : o[k] as JArray;
            return a == null ? new List<string>() : a.Select(x => x.ToString()).Where(x => x.Length > 0).ToList();
        }
    }

    public class DeclinedException : Exception { }
    public class UserFacingException : Exception { public UserFacingException(string m) : base(m) { } }
}
