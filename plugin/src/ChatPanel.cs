using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using DApp = Deswik.Graphics.Application;

namespace Kennet.DeswikClaude
{
    /// <summary>Docked chat panel: header with link status, collapsible toolbar, transcript, input box.</summary>
    public class ChatPanel : UserControl
    {
        // ---- palette
        private static readonly Color Accent = Color.FromArgb(217, 119, 87);      // Claude coral
        private static readonly Color AccentDark = Color.FromArgb(176, 88, 60);
        private static readonly Color Ink = Color.FromArgb(34, 34, 34);
        private static readonly Color Muted = Color.FromArgb(120, 120, 120);
        private static readonly Color Paper = Color.FromArgb(252, 251, 249);
        private static readonly Color Toolbar = Color.FromArgb(243, 240, 236);
        private static readonly Color Line = Color.FromArgb(222, 216, 208);
        private static readonly Color UserBlue = Color.FromArgb(40, 90, 160);
        private static readonly Color UserBack = Color.FromArgb(232, 240, 251);
        private static readonly Color Ok = Color.FromArgb(46, 140, 80);
        private static readonly Color Busy = Color.FromArgb(214, 140, 30);
        private static readonly Color Bad = Color.Firebrick;

        private readonly PluginSettings _settings;
        private readonly DeswikTools _tools;
        private readonly ClaudeAgent _agent;

        private readonly RichTextBox _log;
        private readonly TextBox _input;
        private readonly Button _send, _stop, _undo;
        private readonly ComboBox _model;
        private readonly Label _status, _statusDot, _link;
        private CancellationTokenSource _cts;

        private McpPipeServer _pipe;
        private int _appCalls, _appErrors;

        private readonly ToolTip _tips = new ToolTip();
        private Font _fBold, _fNote, _fSmall, _fSmallBold, _fSmallItalic, _fMono;

        public bool IsBusy { get { return _cts != null; } }

        public ChatPanel(ClaudeChatPlugin plugin)
        {
            _settings = PluginSettings.Load();
            _tools = new DeswikTools(plugin, this, _settings);
            _agent = new ClaudeAgent(_tools, _settings);

            Dock = DockStyle.Fill;
            Font = new Font("Segoe UI", 9f);
            BackColor = Paper;
            _fBold = new Font(Font, FontStyle.Bold);
            _fNote = new Font(Font.FontFamily, 8.5f, FontStyle.Italic);
            _fSmall = new Font(Font.FontFamily, 8.5f);
            _fSmallBold = new Font(Font.FontFamily, 8.5f, FontStyle.Bold);
            _fSmallItalic = _fNote;
            _fMono = new Font("Consolas", 8.5f);

            // ---- header: link status on the left, Options toggle on the right
            var header = new Panel { Dock = DockStyle.Top, Height = 28, BackColor = Accent, Padding = new Padding(6, 0, 6, 0) };
            _link = new Label { Dock = DockStyle.Fill, ForeColor = Color.White, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold), Text = "✦ Claude Assistant" };
            var toggle = new Label { Dock = DockStyle.Right, AutoSize = true, ForeColor = Color.White, Cursor = Cursors.Hand, TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(0, 6, 0, 0) };
            header.Controls.Add(_link);
            header.Controls.Add(toggle);

            // ---- toolbar: wraps onto more rows when the panel is narrow (a fixed-height strip with a
            // scrollbar squashed it to nothing), and can be hidden with the Options toggle.
            var top = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(4, 4, 4, 2), WrapContents = true, BackColor = Toolbar };
            _model = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown, Width = 150, Margin = new Padding(2, 3, 2, 2) };
            _model.Items.AddRange(PluginSettings.Models);
            _model.Text = _settings.Model;
            _model.TextChanged += (s, e) => { _settings.Model = _model.Text.Trim(); SafeSave(); };
            var key = ToolButton("⚿ API key", "Paste an Anthropic API key to chat here in the panel");
            key.Click += (s, e) => AskApiKey();
            var newChat = ToolButton("✚ New chat", "Clear the conversation");
            newChat.Click += (s, e) => NewChat();
            _undo = ToolButton("↶ Undo AI change", "Undo the last schedule change Claude made");
            _undo.Enabled = false;
            _undo.Click += (s, e) => UndoLast();
            var scripts = ToolButton("▤ Scripts", "Open the folder where every script Claude runs is saved");
            scripts.Click += (s, e) => { try { System.Diagnostics.Process.Start(PluginSettings.ScriptsFolder); } catch { } };
            var guides = ToolButton("ℹ Guides", "Open the folder for your own extra guides (*.md)");
            guides.Click += (s, e) => { try { System.Diagnostics.Process.Start(PluginSettings.KnowledgeFolder); } catch { } };
            var autoRead = new CheckBox { Text = "Auto-run reads", AutoSize = true, Checked = _settings.AutoApproveReadOnly, Margin = new Padding(6, 6, 2, 2) };
            autoRead.CheckedChanged += (s, e) => { _settings.AutoApproveReadOnly = autoRead.Checked; SafeSave(); };
            var autoChange = new CheckBox { AutoSize = true, Checked = _settings.AutoApproveChanges, Margin = new Padding(6, 6, 2, 2) };
            Action styleAutoChange = () =>
            {
                autoChange.Text = autoChange.Checked ? "⚠ Auto-approve changes (ON)" : "Auto-approve changes";
                autoChange.ForeColor = autoChange.Checked ? Bad : Ink;
                autoChange.Font = autoChange.Checked ? _fBold : Font;
            };
            styleAutoChange();
            autoChange.CheckedChanged += (s, e) => { _settings.AutoApproveChanges = autoChange.Checked; SafeSave(); styleAutoChange(); };
            top.Controls.AddRange(new Control[] { _model, key, newChat, _undo, scripts, guides, autoRead, autoChange });
            top.Visible = !_settings.ToolbarCollapsed;

            Action setToggleText = () => toggle.Text = top.Visible ? "⚙ Options ▴" : "⚙ Options ▾";
            setToggleText();
            toggle.Click += (s, e) =>
            {
                top.Visible = !top.Visible;
                _settings.ToolbarCollapsed = !top.Visible;
                SafeSave();
                setToggleText();
            };
            var topLine = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Line };

            // ---- transcript
            _log = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.White,
                BorderStyle = BorderStyle.None,
                DetectUrls = true,
                HideSelection = false
            };
            var logHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(8, 6, 4, 4) };
            logHost.Controls.Add(_log);

            // ---- status strip
            var statusBar = new Panel { Dock = DockStyle.Bottom, Height = 22, BackColor = Toolbar, Padding = new Padding(6, 0, 4, 0) };
            _statusDot = new Label { Dock = DockStyle.Left, Width = 16, Text = "●", ForeColor = Ok, TextAlign = ContentAlignment.MiddleLeft };
            _status = new Label { Dock = DockStyle.Fill, ForeColor = Muted, Text = "Ready", TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
            statusBar.Controls.Add(_status);
            statusBar.Controls.Add(_statusDot);

            // ---- input area
            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 96, Padding = new Padding(6), BackColor = Paper };
            _input = new TextBox { Multiline = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical, AcceptsReturn = true, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9.5f) };
            _input.KeyDown += Input_KeyDown;
            var btns = new FlowLayoutPanel { Dock = DockStyle.Right, Width = 90, FlowDirection = FlowDirection.TopDown, Padding = new Padding(6, 0, 0, 0) };
            _send = new Button { Text = "Send ➤", Width = 80, Height = 34, FlatStyle = FlatStyle.Flat, BackColor = Accent, ForeColor = Color.White, Font = new Font("Segoe UI", 9f, FontStyle.Bold), Cursor = Cursors.Hand };
            _send.FlatAppearance.BorderSize = 0;
            _send.FlatAppearance.MouseOverBackColor = AccentDark;
            _send.Click += async (s, e) => await SendAsync();
            _stop = new Button { Text = "■ Stop", Width = 80, Height = 28, Enabled = false, FlatStyle = FlatStyle.Flat, BackColor = Color.White };
            _stop.FlatAppearance.BorderColor = Line;
            _stop.Click += (s, e) => { if (_cts != null) _cts.Cancel(); };
            btns.Controls.Add(_send);
            btns.Controls.Add(_stop);
            bottom.Controls.Add(_input);
            bottom.Controls.Add(btns);
            var bottomLine = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Line };

            // Docking: last added is laid out first (outermost).
            Controls.Add(logHost);
            Controls.Add(topLine);
            Controls.Add(top);
            Controls.Add(header);
            Controls.Add(bottomLine);
            Controls.Add(bottom);
            Controls.Add(statusBar);

            _agent.AssistantText += t => Append("✦ Claude", t, Ink, Accent, null);
            _agent.ToolActivity += (n, d, ok) => Activity("", n, d, ok);
            _agent.Status += s => SetStatus(s, IsBusy ? Busy : Ok);

            AppendNote("Ask about the open drawing or the IS schedule, or ask Claude to build something. " +
                       "Changes are shown to you for approval first unless 'Auto-approve changes' is ticked. Enter = send, Shift+Enter = new line.");
            if (string.IsNullOrWhiteSpace(_settings.ApiKey))
                AppendNote("No API key needed if you chat from the Claude desktop app (Deswik extension installed). " +
                           "To chat here in the panel instead, open ⚙ Options and click 'API key'.");
        }

        private Button ToolButton(string text, string tip)
        {
            var b = new Button { Text = text, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlatStyle = FlatStyle.Flat, BackColor = Color.White, Margin = new Padding(2), Padding = new Padding(4, 1, 4, 1), Cursor = Cursors.Hand };
            b.FlatAppearance.BorderColor = Line;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(250, 236, 229);
            _tips.SetToolTip(b, tip);
            return b;
        }

        public void OnApplicationChanged(DApp app)
        {
            AppendNote("Active document changed" + (app != null && !string.IsNullOrEmpty(app.Filename) ? ": " + app.Filename : "") + ".");
        }

        private async void Input_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.SuppressKeyPress = true;
                await SendAsync();
            }
        }

        private async System.Threading.Tasks.Task SendAsync()
        {
            string text = _input.Text.Trim();
            if (text.Length == 0 || IsBusy) return;
            _input.Clear();
            Append("You", text, Ink, UserBlue, UserBack);

            _cts = new CancellationTokenSource();
            SetBusy(true);
            try
            {
                await _agent.SendAsync(text, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                _agent.RepairHistory();
                AppendNote("Stopped.");
                SetStatus("Ready", Ok);
            }
            catch (UserFacingException ex)
            {
                _agent.RepairHistory();
                AppendNote("✗ " + ex.Message, Bad);
                SetStatus("Ready", Ok);
            }
            catch (Exception ex)
            {
                _agent.RepairHistory();
                AppendNote("✗ Error: " + ex.Message, Bad);
                SetStatus("Error: " + ex.Message, Bad);
            }
            finally
            {
                _cts.Dispose();
                _cts = null;
                SetBusy(false);
                _undo.Enabled = _tools.UndoDepth > 0;
                _input.Focus();
            }
        }

        public void NewChat()
        {
            if (IsBusy) return;
            _agent.Reset();
            _log.Clear();
            _lastNote = null;
            _act = null;
            AppendNote("New chat started.");
        }

        public void UndoLast()
        {
            if (IsBusy) return;
            AppendNote(_tools.UndoLast());
            _undo.Enabled = _tools.UndoDepth > 0;
        }

        private void AskApiKey()
        {
            using (var f = new Form { Text = "Anthropic API key", Width = 520, Height = 160, StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MinimizeBox = false, MaximizeBox = false })
            {
                var lbl = new Label { Text = "Paste your key (stored encrypted for your Windows user only):", Dock = DockStyle.Top, Height = 24, Padding = new Padding(6, 6, 0, 0) };
                var box = new TextBox { Dock = DockStyle.Top, UseSystemPasswordChar = true };
                var ok = new Button { Text = "Save", DialogResult = DialogResult.OK, Dock = DockStyle.Bottom, Height = 32 };
                f.Controls.Add(ok); f.Controls.Add(box); f.Controls.Add(lbl);
                f.AcceptButton = ok;
                if (f.ShowDialog(this) == DialogResult.OK && box.Text.Trim().Length > 0)
                {
                    _settings.ApiKey = box.Text.Trim();
                    SafeSave();
                    AppendNote("API key saved.");
                }
            }
        }

        private void SetBusy(bool busy)
        {
            _send.Enabled = !busy;
            _send.BackColor = busy ? Color.FromArgb(225, 190, 175) : Accent;
            _stop.Enabled = busy;
            _stop.ForeColor = busy ? Bad : Muted;
            _model.Enabled = !busy;
            SetStatus(busy ? "Claude is working…" : "Ready", busy ? Busy : Ok);
        }

        private void SetStatus(string text, Color dot)
        {
            _status.Text = text;
            _statusDot.ForeColor = dot;
        }

        private void UpdateLink(bool working)
        {
            if (_pipe == null) { _link.Text = "✦ Claude Assistant"; return; }
            string calls = _appCalls == 0 ? "" : "  ·  " + _appCalls + " tool call" + (_appCalls == 1 ? "" : "s") + (_appErrors > 0 ? ", " + _appErrors + " failed" : "");
            _link.Text = (working ? "◉ Claude app working…" : "● Linked to Claude app") + calls;
        }

        private void SafeSave() { try { _settings.Save(); } catch { } }

        // ---- transcript helpers
        private void Write(string text, Font font, Color color, Color? back = null, int indent = 0)
        {
            _log.SelectionStart = _log.TextLength;
            _log.SelectionLength = 0;
            _log.SelectionFont = font;
            _log.SelectionColor = color;
            _log.SelectionBackColor = back ?? Color.White;
            _log.SelectionIndent = indent;
            _log.AppendText(text);
        }

        private void Append(string who, string text, Color body, Color label, Color? back)
        {
            _lastNote = null;
            if (_act != null) Write("\n", Font, body); // gap after a block of tool lines
            _act = null;
            Write(who + "\n", _fBold, label, back);
            Write((text ?? "").Trim() + "\n", Font, body, back);
            Write("\n", Font, body);
            _log.ScrollToCaret();
        }

        // Identical notes in a row are shown once with a count.
        private string _lastNote;
        private int _lastNoteStart = -1, _lastNoteCount;

        private void AppendNote(string text, Color? color = null)
        {
            if (_act != null) Write("\n", Font, Muted);
            _act = null;
            var font = _fNote;
            if (text == _lastNote && _lastNoteStart >= 0 && _lastNoteStart <= _log.TextLength)
            {
                _lastNoteCount++;
                ReplaceTail(_lastNoteStart);
                Write(text + "  (×" + _lastNoteCount + ")\n\n", font, color ?? Muted);
                _log.ScrollToCaret();
                return;
            }
            _lastNote = text;
            _lastNoteCount = 1;
            _lastNoteStart = _log.TextLength;
            Write(text + "\n\n", font, color ?? Muted);
            _log.ScrollToCaret();
        }

        // ---- tool activity: one compact line per tool; repeated calls of the same tool update that line in place.
        private class ActLine { public string Key; public int Start, Count, Errors; public string Detail; public bool? State; }
        private ActLine _act;

        private void Activity(string source, string tool, string detail, bool? ok)
        {
            string key = source + "|" + tool;
            if (_act == null || _act.Key != key || _act.Start > _log.TextLength)
            {
                _lastNote = null;
                _act = new ActLine { Key = key, Start = _log.TextLength };
            }
            if (ok == null) _act.Count++;
            else if (ok == false) _act.Errors++;
            _act.Detail = detail;
            _act.State = ok;

            ReplaceTail(_act.Start);
            string icon = ok == null ? "◌" : ok == true ? "✓" : "✗";
            Color iconColor = ok == null ? Busy : ok == true ? Ok : Bad;
            var small = _fSmall;
            Write("  " + icon + " ", _fSmallBold, iconColor, null, 4);
            if (source.Length > 0) Write(source + " · ", small, Muted, null, 4);
            Write(tool, _fMono, Ink, null, 4);
            string count = (_act.Count > 1 ? "  ×" + _act.Count : "") + (_act.Errors > 0 ? "  (" + _act.Errors + " failed)" : "");
            if (count.Length > 0) Write(count, _fSmallBold, _act.Errors > 0 ? Bad : Accent, null, 4);
            if (!string.IsNullOrEmpty(detail)) Write("  —  " + detail, _fSmallItalic, Muted, null, 4);
            Write("\n", small, Muted);
            _log.ScrollToCaret();
        }

        private void ReplaceTail(int start)
        {
            if (start >= _log.TextLength) return;
            bool ro = _log.ReadOnly;
            _log.ReadOnly = false;
            _log.Select(start, _log.TextLength - start);
            _log.SelectedText = "";
            _log.ReadOnly = ro;
        }

        // The pipe server marshals tool calls onto this control's UI thread, so start it once the handle exists.
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (_pipe != null) return;
            try
            {
                _pipe = new McpPipeServer(this, _tools, (tool, detail, ok) =>
                {
                    if (ok == null) _appCalls++;
                    else if (ok == false) _appErrors++;
                    Activity("Claude app", tool, detail, ok);
                    UpdateLink(ok == null);
                });
                _pipe.Start();
                UpdateLink(false);
                AppendNote("Claude desktop app link is on: tools are available to the Deswik extension in the Claude app.");
            }
            catch (Exception ex)
            {
                _link.Text = "○ Claude app not linked";
                AppendNote("Could not start the Claude desktop app link: " + ex.Message, Bad);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _pipe != null) { _pipe.Dispose(); _pipe = null; }
            if (disposing && _cts != null) _cts.Cancel();
            base.Dispose(disposing);
        }
    }
}
