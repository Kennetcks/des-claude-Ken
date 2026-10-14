using System;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using STasks = System.Threading.Tasks;

namespace Kennet.DeswikClaude
{
    /// <summary>
    /// Local link for the Claude desktop app. The bridge (deswik-claude.mcpb) connects to
    /// \\.\pipe\KennetDeswikClaude and sends one JSON line per request:
    ///   {"op":"ping"} | {"op":"list"} | {"op":"call","name":"...","input":{...}}
    /// and gets one JSON line back. Only the current Windows user can connect; network access is denied.
    /// Tools run on Deswik's UI thread, exactly as in the chat panel, including the approval dialogs.
    /// </summary>
    public class McpPipeServer : IDisposable
    {
        public const string PipeName = "KennetDeswikClaude";

        private readonly Control _ui;
        private readonly DeswikTools _tools;
        private readonly Action<string, string, bool?> _activity; // (tool, detail, null = started / true = ok / false = error)
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public int CallsServed { get; private set; }
        public string LastError { get; private set; }

        public McpPipeServer(Control uiThreadControl, DeswikTools tools, Action<string, string, bool?> activity)
        {
            _ui = uiThreadControl;
            _tools = tools;
            _activity = activity;
        }

        public void Start()
        {
            var t = new Thread(AcceptLoop) { IsBackground = true, Name = "DeswikClaude pipe" };
            t.Start();
        }

        private static PipeSecurity CreateSecurity()
        {
            var ps = new PipeSecurity();
            var me = WindowsIdentity.GetCurrent().User;
            ps.AddAccessRule(new PipeAccessRule(me, PipeAccessRights.FullControl, AccessControlType.Allow));
            ps.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.NetworkSid, null),
                                                PipeAccessRights.FullControl, AccessControlType.Deny));
            return ps;
        }

        private void AcceptLoop()
        {
            while (!_cts.IsCancellationRequested)
            {
                NamedPipeServerStream pipe = null;
                try
                {
                    pipe = NamedPipeServerStreamAcl.Create(PipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances,
                                                           PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 1 << 16, 1 << 16, CreateSecurity());
                    pipe.WaitForConnectionAsync(_cts.Token).GetAwaiter().GetResult();
                    var connected = pipe;
                    pipe = null;
                    STasks.Task.Run(() => Handle(connected)); // next instance is created immediately
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    LastError = ex.Message;
                    if (pipe != null) pipe.Dispose();
                    Thread.Sleep(1000); // e.g. another Deswik instance owns the pipe
                }
            }
        }

        private void Handle(NamedPipeServerStream pipe)
        {
            using (pipe)
            {
                try
                {
                    var reader = new StreamReader(pipe, new UTF8Encoding(false), false, 1 << 16, true);
                    string line = reader.ReadLine();
                    if (line == null) return;
                    JObject reply;
                    try { reply = Process(JObject.Parse(line)); }
                    catch (Exception ex)
                    {
                        Exception inner = ex.InnerException ?? ex;
                        reply = new JObject { ["text"] = "Deswik plugin error: " + inner.GetType().Name + ": " + inner.Message, ["isError"] = true };
                    }
                    byte[] bytes = new UTF8Encoding(false).GetBytes(reply.ToString(Formatting.None) + "\n");
                    pipe.Write(bytes, 0, bytes.Length);
                    pipe.Flush();
                    pipe.WaitForPipeDrain();
                }
                catch (Exception ex) { LastError = ex.Message; }
            }
        }

        private JObject Process(JObject req)
        {
            string op = (string)req["op"] ?? "";
            switch (op)
            {
                case "ping":
                    return new JObject { ["ok"] = true, ["deswik"] = OnUi(() => _tools.DocumentName()) };

                case "list":
                    return new JObject { ["tools"] = DeswikTools.McpToolList() };

                case "call":
                    string name = (string)req["name"] ?? "";
                    var input = req["input"] as JObject ?? new JObject();
                    string detail = ClaudeAgent.Describe(name, input);
                    ToolResult r = OnUi(() =>
                    {
                        if (_activity != null) _activity(name, detail, null);
                        ToolResult res = _tools.Execute(name, input);
                        if (_activity != null) _activity(name, detail, !res.IsError);
                        return res;
                    });
                    CallsServed++;
                    return new JObject { ["text"] = r.Text ?? "", ["isError"] = r.IsError };

                default:
                    return new JObject { ["text"] = "Unknown op: " + op, ["isError"] = true };
            }
        }

        /// <summary>Runs on Deswik's UI thread and waits (approval dialogs may take a while).</summary>
        private T OnUi<T>(Func<T> f)
        {
            if (_ui.IsDisposed) throw new ObjectDisposedException("Deswik panel");
            return (T)_ui.Invoke(f);
        }

        public void Dispose()
        {
            _cts.Cancel();
        }
    }
}
