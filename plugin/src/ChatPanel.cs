using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using DApp = Deswik.Graphics.Application;

namespace Kennet.DeswikClaude
{
    /// <summary>Docked chat panel: transcript, input box, model picker, API key, new chat, undo.</summary>
    public class ChatPanel : UserControl
    {
        private readonly PluginSettings _settings;
        private readonly DeswikTools _tools;
        private readonly ClaudeAgent _agent;

        private readonly RichTextBox _log;
        private readonly TextBox _input;
        private readonly Button _send, _stop, _undo;
        private readonly ComboBox _model;
        private readonly Label _status;
        private CancellationTokenSource _cts;

        private McpPipeServer _pipe;

        public bool IsBusy { get { return _cts != null; } }

        public ChatPanel(ClaudeChatPlugin plugin)
        {
            _settings = PluginSettings.Load();
            _tools = new DeswikTools(plugin, this, _settings);
            _agent = new ClaudeAgent(_tools, _settings);

            Dock = DockStyle.Fill;
            Font = new Font("Segoe UI", 9f);

            // ---- top bar: wraps onto more rows when the panel is narrow (a fixed-height strip with a
            // scrollbar squashed it to nothing), and can be hidden with the Options toggle.
            var top = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(2), WrapContents = true };
            _model = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown, Width = 150 };
            _model.Items.AddRange(PluginSettings.Models);
            _model.Text = _settings.Model;
            _model.TextChanged += (s, e) => { _settings.Model = _model.Text.Trim(); SafeSave(); };
            var key = new Button { Text = "API key…", AutoSize = true };
            key.Click += (s, e) => AskApiKey();
            var newChat = new Button { Text = "New chat", AutoSize = true };
            newChat.Click += (s, e) => NewChat();
            _undo = new Button { Text = "Undo last AI change", AutoSize = true, Enabled = false };
            _undo.Click += (s, e) => UndoLast();
            var scripts = new Button { Text = "Scripts", AutoSize = true };
            scripts.Click += (s, e) => { try { System.Diagnostics.Process.Start(PluginSettings.ScriptsFolder); } catch { } };
            var guides = new Button { Text = "Guides", AutoSize = true };
            guides.Click += (s, e) => { try { System.Diagnostics.Process.Start(PluginSettings.KnowledgeFolder); } catch { } };
            var autoRead = new CheckBox { Text = "Auto-run reads", AutoSize = true, Checked = _settings.AutoApproveReadOnly, Padding = new Padding(4, 4, 0, 0) };
            autoRead.CheckedChanged += (s, e) => { _settings.AutoApproveReadOnly = autoRead.Checked; SafeSave(); };
            var autoChange = new CheckBox { Text = "Auto-approve changes", AutoSize = true, Checked = _settings.AutoApproveChanges, Padding = new Padding(4, 4, 0, 0) };
            autoChange.CheckedChanged += (s, e) => { _settings.AutoApproveChanges = autoChange.Checked; SafeSave(); };
            top.Controls.AddRange(new Control[] { _model, key, newChat, _undo, scripts, guides, autoRead, autoChange });
            top.Visible = !_settings.ToolbarCollapsed;

            var toggle = new LinkLabel { Dock = DockStyle.Top, Height = 20, Padding = new Padding(4, 3, 0, 0), LinkBehavior = LinkBehavior.HoverUnderline };
            Action setToggleText = () => toggle.Text = top.Visible ? "▴ Hide options" : "▾ Options";
            setToggleText();
            toggle.LinkClicked += (s, e) =>
            {
                top.Visible = !top.Visible;
                _settings.ToolbarCollapsed = !top.Visible;
                SafeSave();
                setToggleText();
            };

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

            // ---- input area
            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 110, Padding = new Padding(4) };
            _input = new TextBox { Multiline = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical, AcceptsReturn = true };
            _input.KeyDown += Input_KeyDown;
            var btns = new FlowLayoutPanel { Dock = DockStyle.Right, Width = 86, FlowDirection = FlowDirection.TopDown };
            _send = new Button { Text = "Send", Width = 78, Height = 32 };
            _send.Click += async (s, e) => await SendAsync();
            _stop = new Button { Text = "Stop", Width = 78, Height = 28, Enabled = false };
            _stop.Click += (s, e) => { if (_cts != null) _cts.Cancel(); };
            btns.Controls.Add(_send);
            btns.Controls.Add(_stop);
            bottom.Controls.Add(_input);
            bottom.Controls.Add(btns);

            _status = new Label { Dock = DockStyle.Bottom, Height = 20, ForeColor = Color.DimGray, Text = "Ready", Padding = new Padding(4, 2, 0, 0) };

            Controls.Add(_log);
            Controls.Add(top);
            Controls.Add(toggle);
            Controls.Add(_status);
            Controls.Add(bottom);

            _agent.AssistantText += t => Append("Claude", t, Color.FromArgb(30, 30, 30), Color.FromArgb(204, 120, 50));
            _agent.ToolActivity += (n, d) => AppendNote("⚙ " + n + (string.IsNullOrEmpty(d) ? "" : " — " + d));
            _agent.Status += s => _status.Text = s;

            AppendNote("Ask about the open drawing or the IS schedule, or ask Claude to build something. " +
                       "Changes are shown to you for approval first unless 'Auto-approve changes' is ticked. Enter = send, Shift+Enter = new line.");
            if (string.IsNullOrWhiteSpace(_settings.ApiKey))
                AppendNote("No API key needed if you chat from the Claude desktop app (Deswik extension installed). " +
                           "To chat here in the panel instead, click 'API key…'.");
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
            Append("You", text, Color.Black, Color.FromArgb(40, 90, 160));

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
                _status.Text = "Ready";
            }
            catch (UserFacingException ex)
            {
                _agent.RepairHistory();
                AppendNote(ex.Message, Color.Firebrick);
                _status.Text = "Ready";
            }
            catch (Exception ex)
            {
                _agent.RepairHistory();
                AppendNote("Error: " + ex.Message, Color.Firebrick);
                _status.Text = "Ready";
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
            _stop.Enabled = busy;
            _model.Enabled = !busy;
        }

        private void SafeSave() { try { _settings.Save(); } catch { } }

        // ---- transcript helpers
        private void Append(string who, string text, Color body, Color label)
        {
            _lastNote = null;
            _log.SelectionStart = _log.TextLength;
            _log.SelectionFont = new Font(Font, FontStyle.Bold);
            _log.SelectionColor = label;
            _log.AppendText(who + "\n");
            _log.SelectionFont = Font;
            _log.SelectionColor = body;
            _log.AppendText((text ?? "").Trim() + "\n\n");
            _log.ScrollToCaret();
        }

        // Identical notes in a row (e.g. one "[Claude app] run_csharp" per tool call) are shown once with a count.
        private string _lastNote;
        private int _lastNoteStart = -1, _lastNoteCount;

        private void AppendNote(string text, Color? color = null)
        {
            if (text == _lastNote && _lastNoteStart >= 0 && _lastNoteStart <= _log.TextLength)
            {
                _lastNoteCount++;
                _log.Select(_lastNoteStart, _log.TextLength - _lastNoteStart);
                _log.SelectionFont = new Font(Font.FontFamily, 8.5f, FontStyle.Italic);
                _log.SelectionColor = color ?? Color.Gray;
                _log.SelectedText = text + "  (×" + _lastNoteCount + ")\n\n";
                _log.SelectionFont = Font;
                _log.ScrollToCaret();
                return;
            }
            _lastNote = text;
            _lastNoteCount = 1;
            _lastNoteStart = _log.TextLength;
            _log.SelectionStart = _log.TextLength;
            _log.SelectionFont = new Font(Font.FontFamily, 8.5f, FontStyle.Italic);
            _log.SelectionColor = color ?? Color.Gray;
            _log.AppendText(text + "\n\n");
            _log.SelectionFont = Font;
            _log.ScrollToCaret();
        }

        // The pipe server marshals tool calls onto this control's UI thread, so start it once the handle exists.
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (_pipe != null) return;
            try
            {
                _pipe = new McpPipeServer(this, _tools, t => AppendNote(t));
                _pipe.Start();
                AppendNote("Claude desktop app link is on: tools are available to the Deswik extension in the Claude app.");
            }
            catch (Exception ex)
            {
                AppendNote("Could not start the Claude desktop app link: " + ex.Message, Color.Firebrick);
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
