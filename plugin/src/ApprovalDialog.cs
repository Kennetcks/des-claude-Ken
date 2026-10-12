using System.Drawing;
using System.Windows.Forms;

namespace Kennet.DeswikClaude
{
    /// <summary>Every change Claude wants to make goes through this dialog first.</summary>
    public class ApprovalDialog : Form
    {
        private ApprovalDialog(string title, string summary, string details, string approveText)
        {
            Text = "Claude wants to: " + title;
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(820, 620);
            MinimizeBox = false;
            ShowIcon = false;
            // When the request comes from the Claude desktop app, Deswik is usually behind it: pop to the front.
            TopMost = true;
            Shown += (s, e) => { Activate(); BringToFront(); };

            var lbl = new Label
            {
                Text = summary,
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 56,
                Padding = new Padding(8),
                Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 9.5f, FontStyle.Bold)
            };

            var box = new TextBox
            {
                Text = (details ?? "").Replace("\r\n", "\n").Replace("\n", "\r\n"),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Dock = DockStyle.Fill,
                Font = new Font(FontFamily.GenericMonospace, 9f),
                BackColor = Color.White
            };

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, Height = 44, Padding = new Padding(6) };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 100, Height = 30 };
            var ok = new Button { Text = approveText, DialogResult = DialogResult.OK, Width = 140, Height = 30 };
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(ok);

            Controls.Add(box);
            Controls.Add(lbl);
            Controls.Add(buttons);
            AcceptButton = null; // force a deliberate click
            CancelButton = cancel;
        }

        public static bool Ask(IWin32Window owner, string title, string summary, string details, string approveText = "Approve")
        {
            using (var d = new ApprovalDialog(title, summary, details, approveText))
                return d.ShowDialog(owner) == DialogResult.OK;
        }
    }
}
