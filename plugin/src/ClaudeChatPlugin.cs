using System;
using System.Net;
using System.Windows.Forms;
using DApp = Deswik.Graphics.Application;

namespace Kennet.DeswikClaude
{
    /// <summary>
    /// Deswik CAD plugin entry point. Deswik finds this class by convention (reflection on member
    /// signatures), not by interface: ctor(Application), Application property, Load(IWin32Window),
    /// RegisterMainControl(), Unload().
    /// </summary>
    public class ClaudeChatPlugin
    {
        private ChatPanel _panel;
        private DApp _app;

        // ---- Properties read by the Deswik plugin host -----------------------------------------
        public string ProductName { get { return "Claude Assistant"; } }
        public string ProductVersion { get { return "0.3.1"; } }
        public string DockStyle { get { return "Tab"; } }
        public string DockPosition { get { return "Right"; } }
        public bool AllowFloating { get { return true; } }
        public bool PreserveDockWidth { get { return true; } }
        public bool UnloadOnNullApp { get { return false; } }
        public int MinimumStartWidth { get { return 380; } }

        /// <summary>Host sets this again whenever the active CAD document changes.</summary>
        public DApp Application
        {
            get { return _app; }
            set
            {
                _app = value;
                if (_panel != null) _panel.OnApplicationChanged(value);
            }
        }

        public ClaudeChatPlugin(DApp app)
        {
            _app = app;
            // .NET Framework 4.7.2 defaults can omit TLS 1.2, which api.anthropic.com requires.
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        public void Load(IWin32Window owner)
        {
            _panel = new ChatPanel(this);
            // Force the window handle now so the Claude-app link starts even if the panel tab is hidden.
            IntPtr unused = _panel.Handle;
        }

        public object RegisterMainControl()
        {
            return _panel;
        }

        public bool Unloading()
        {
            // Returning true would cancel the unload. Block only while a request is running.
            return _panel != null && _panel.IsBusy;
        }

        public void Unload()
        {
            if (_panel != null)
            {
                _panel.Dispose();
                _panel = null;
            }
        }

        // ---- Commands (callable from ribbon customisation, macros, Process Maps) -----------------
        public static void GetCommands(out string[] commandIds, out string[] commandNames)
        {
            commandIds = new[] { "CLAUDE_NEW_CHAT", "CLAUDE_UNDO_LAST" };
            commandNames = new[] { "Claude: New chat", "Claude: Undo last AI schedule change" };
        }

        public void ExecuteByID(string commandId)
        {
            if (_panel == null) return;
            switch ((commandId ?? "").ToUpperInvariant())
            {
                case "CLAUDE_NEW_CHAT": _panel.NewChat(); break;
                case "CLAUDE_UNDO_LAST": _panel.UndoLast(); break;
            }
        }
    }
}
