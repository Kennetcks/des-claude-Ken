using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using STasks = System.Threading.Tasks;

namespace Kennet.DeswikClaude
{
    /// <summary>
    /// Calls the Claude Messages API and runs the tool-use loop. Awaits resume on the UI thread
    /// (WinForms SynchronizationContext), so tools execute safely against Deswik objects.
    /// </summary>
    public class ClaudeAgent
    {
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        private const string Endpoint = "https://api.anthropic.com/v1/messages";
        private const int MaxToolRounds = 40;

        private readonly DeswikTools _tools;
        private readonly JArray _toolDefs;
        private JArray _messages = new JArray();

        public PluginSettings Settings { get; set; }

        public event Action<string> AssistantText;
        public event Action<string, string, bool?> ToolActivity;   // (tool name, short note, null = started / true = ok / false = error)
        public event Action<string> Status;

        public ClaudeAgent(DeswikTools tools, PluginSettings settings)
        {
            _tools = tools;
            Settings = settings;
            _toolDefs = DeswikTools.Definitions();
        }

        public void Reset() { _messages = new JArray(); }

        /// <summary>After a cancel/error, drop a dangling assistant tool_use turn so the next request is valid.</summary>
        public void RepairHistory()
        {
            if (_messages.Count == 0) return;
            var last = _messages.Last as JObject;
            if (last != null && (string)last["role"] == "assistant")
            {
                var c = last["content"] as JArray;
                if (c != null && c.OfType<JObject>().Any(b => (string)b["type"] == "tool_use"))
                    _messages.RemoveAt(_messages.Count - 1);
            }
        }

        public async STasks.Task SendAsync(string userText, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(Settings.ApiKey))
                throw new UserFacingException("No API key set. Click 'API key…' and paste your Anthropic API key.");

            _messages.Add(new JObject { ["role"] = "user", ["content"] = userText });

            for (int round = 0; round < MaxToolRounds; round++)
            {
                RaiseStatus(round == 0 ? "Thinking…" : "Working… (step " + (round + 1) + ")");
                JObject response = await PostAsync(ct);

                var content = response["content"] as JArray ?? new JArray();
                _messages.Add(new JObject { ["role"] = "assistant", ["content"] = content });

                foreach (var block in content.OfType<JObject>())
                    if ((string)block["type"] == "text" && AssistantText != null)
                        AssistantText((string)block["text"]);

                string stop = (string)response["stop_reason"];
                if (stop != "tool_use")
                {
                    if (stop == "max_tokens") RaiseStatus("Reply was cut off (max tokens).");
                    else RaiseStatus("Ready");
                    return;
                }

                var results = new JArray();
                foreach (var block in content.OfType<JObject>().Where(b => (string)b["type"] == "tool_use"))
                {
                    ct.ThrowIfCancellationRequested();
                    string name = (string)block["name"];
                    var input = block["input"] as JObject ?? new JObject();
                    string note = Describe(name, input);
                    if (ToolActivity != null) ToolActivity(name, note, null);
                    RaiseStatus("Running " + name + "…");

                    ToolResult r = _tools.Execute(name, input); // UI thread
                    if (ToolActivity != null) ToolActivity(name, note, !r.IsError);
                    var tr = new JObject
                    {
                        ["type"] = "tool_result",
                        ["tool_use_id"] = (string)block["id"],
                        ["content"] = string.IsNullOrEmpty(r.Text) ? "(empty)" : r.Text
                    };
                    if (r.IsError) tr["is_error"] = true;
                    results.Add(tr);
                }
                _messages.Add(new JObject { ["role"] = "user", ["content"] = results });
            }
            RaiseStatus("Stopped after " + MaxToolRounds + " tool steps.");
        }

        private async STasks.Task<JObject> PostAsync(CancellationToken ct)
        {
            var body = new JObject
            {
                ["model"] = Settings.Model,
                ["max_tokens"] = Settings.MaxTokens,
                // Cache the large, static system prompt + tool list to cut cost and latency.
                ["system"] = new JArray(new JObject
                {
                    ["type"] = "text",
                    ["text"] = SystemPrompt.Full,
                    ["cache_control"] = new JObject { ["type"] = "ephemeral" }
                }),
                ["tools"] = _toolDefs,
                ["messages"] = _messages
            };

            using (var req = new HttpRequestMessage(HttpMethod.Post, Endpoint))
            {
                req.Headers.Add("x-api-key", Settings.ApiKey);
                req.Headers.Add("anthropic-version", "2023-06-01");
                req.Content = new StringContent(body.ToString(Formatting.None), Encoding.UTF8, "application/json");

                using (var resp = await Http.SendAsync(req, ct))
                {
                    string text = await resp.Content.ReadAsStringAsync();
                    if (!resp.IsSuccessStatusCode)
                    {
                        string msg = text;
                        try { msg = (string)JObject.Parse(text)["error"]["message"] ?? text; } catch { }
                        // Drop the user turn that failed so the conversation stays valid.
                        if (_messages.Count > 0 && (string)_messages.Last["role"] == "user" && _messages.Last["content"].Type == JTokenType.String)
                            _messages.RemoveAt(_messages.Count - 1);
                        throw new UserFacingException("Claude API error " + (int)resp.StatusCode + ": " + msg);
                    }
                    return JObject.Parse(text);
                }
            }
        }

        internal static string Describe(string name, JObject input)
        {
            string d = (string)input["description"] ?? (string)input["reason"] ?? (string)input["task_id"] ?? (string)input["command_id"] ?? "";
            return d.Length > 120 ? d.Substring(0, 120) + "…" : d;
        }

        private void RaiseStatus(string s) { if (Status != null) Status(s); }
    }
}
