namespace Dat2DTR
{
    /// <summary>
    /// This is the "About" window: the name and version of the program, who made it and how to reach them for
    /// changes, and the version history (what changed in each version, newest first). See AppInfo.
    /// </summary>
    internal sealed class AboutForm : Form
    {
        private readonly RichTextBox _history = new RichTextBox();
        private readonly Label _copied = new Label();   // This is the note beside the email after it was clicked
        private readonly Font _boldFont, _normalFont;

        public AboutForm()
        {
            // This is the same order as a designer-made form, so it scales correctly on 125% / 150% screens.
            SuspendLayout();
            Text = "About Dat2DTR";
            Icon = Theme.GetAppIcon();
            Font = new Font("Segoe UI", 9f);
            _boldFont = new Font("Segoe UI", 10f, FontStyle.Bold);
            _normalFont = new Font("Segoe UI", 9.75f);
            AutoScaleDimensions = new SizeF(7f, 15f);
            AutoScaleMode = AutoScaleMode.Font;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = Color.White;
            ClientSize = new Size(680, 640);
            MinimumSize = new Size(560, 480);

            // This is the top: the name and version of the program, on the navy bar ----
            var top = new Panel();
            top.Dock = DockStyle.Top;
            top.Size = new Size(680, 92);
            top.BackColor = Theme.Header;

            var name = new Label();
            name.Text = "Dat2DTR";
            name.Font = new Font("Segoe UI", 16f, FontStyle.Bold);
            name.ForeColor = Color.White;
            name.SetBounds(20, 10, 640, 34);
            name.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            top.Controls.Add(name);

            var version = new Label();
            version.Text = "Version " + AppInfo.Version + "   -   " + AppInfo.VersionDate;
            version.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            version.ForeColor = Color.FromArgb(255, 223, 0);
            version.SetBounds(22, 44, 640, 22);
            version.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            top.Controls.Add(version);

            var what = new Label();
            what.Text = "Turns the biometrics .dat file into a Daily Time Record (CS Form No. 48)";
            what.ForeColor = Color.FromArgb(255, 240, 170);
            what.SetBounds(22, 66, 640, 20);
            what.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            top.Controls.Add(what);

            // This is who made it and how to reach them ----
            var who = new Panel();
            who.Dock = DockStyle.Top;
            who.Size = new Size(680, 154);
            who.BackColor = Theme.Panel;

            var madeBy = new Label();
            madeBy.Text = "Developed by";
            madeBy.ForeColor = Theme.TextMuted;
            madeBy.SetBounds(20, 12, 300, 18);
            who.Controls.Add(madeBy);

            var developer = new Label();
            developer.Text = AppInfo.Developer;
            developer.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            developer.ForeColor = Theme.Navy;
            developer.SetBounds(20, 30, 640, 24);
            who.Controls.Add(developer);

            var role = new Label();
            role.Text = AppInfo.Role;
            role.ForeColor = Theme.TextStrong;
            role.SetBounds(20, 54, 640, 18);
            who.Controls.Add(role);

            var emailLabel = new Label();
            emailLabel.Text = "Email:";
            emailLabel.ForeColor = Theme.TextMuted;
            emailLabel.SetBounds(20, 78, 110, 18);
            who.Controls.Add(emailLabel);

            var email = new LinkLabel();
            email.Text = AppInfo.Email;
            email.AutoSize = true;
            email.Location = new Point(130, 78);
            email.LinkClicked += (sender, e) => OpenMail();
            who.Controls.Add(email);

            _copied.ForeColor = Theme.TextMuted;
            _copied.SetBounds(400, 78, 260, 18);
            _copied.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            who.Controls.Add(_copied);

            var phoneLabel = new Label();
            phoneLabel.Text = "Contact number:";
            phoneLabel.ForeColor = Theme.TextMuted;
            phoneLabel.SetBounds(20, 98, 110, 18);
            who.Controls.Add(phoneLabel);

            var phone = new Label();
            phone.Text = AppInfo.Phone;
            phone.ForeColor = Theme.TextStrong;
            phone.SetBounds(130, 98, 400, 18);
            who.Controls.Add(phone);

            var ask = new Label();
            ask.Text = "For changes to the program, or if something is not right, please contact the developer and mention the version above.";
            ask.ForeColor = Theme.TextMuted;
            ask.SetBounds(20, 120, 640, 32);   // This is two lines, so the sentence is never cut off
            ask.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            who.Controls.Add(ask);

            // This is the version history, newest first ----
            var heading = new Label();
            heading.Text = "Version history";
            heading.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            heading.ForeColor = Theme.Navy;
            heading.Dock = DockStyle.Top;
            heading.Size = new Size(680, 34);
            heading.Padding = new Padding(16, 10, 0, 0);

            _history.Dock = DockStyle.Fill;
            _history.ReadOnly = true;
            _history.BorderStyle = BorderStyle.None;
            _history.BackColor = Color.White;
            _history.DetectUrls = false;
            _history.ScrollBars = RichTextBoxScrollBars.Vertical;

            var historyBox = new Panel();
            historyBox.Dock = DockStyle.Fill;
            historyBox.Padding = new Padding(20, 0, 8, 0);
            historyBox.Controls.Add(_history);

            // This is the bottom: Close ----
            var close = new Button();
            close.Text = "Close";
            close.DialogResult = DialogResult.Cancel;
            close.SetBounds(680 - 16 - 96, 12, 96, 32);
            close.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            var bottom = new Panel();
            bottom.Dock = DockStyle.Bottom;
            bottom.Size = new Size(680, 56);
            bottom.BackColor = Theme.Panel;
            bottom.Controls.Add(close);

            Controls.Add(historyBox);
            Controls.Add(heading);
            Controls.Add(who);
            Controls.Add(top);
            Controls.Add(bottom);
            CancelButton = close;
            ResumeLayout(false);
            PerformLayout();

            WriteHistory();
        }

        /// <summary>This is where every version and its changes are written into the history box.</summary>
        private void WriteHistory()
        {
            _history.Clear();
            for (int v = 0; v < AppInfo.History.Length; v++)
            {
                VersionEntry entry = AppInfo.History[v];
                Write((v > 0 ? "\n" : "") + "Version " + entry.Number + "   -   " + entry.Date +
                      (v == 0 ? "   (this version)" : "") + "\n", _boldFont, Theme.Navy);
                foreach (string change in entry.Changes)
                    Write("  \u2022  " + change + "\n", _normalFont, Theme.TextStrong);
            }
            _history.SelectionStart = 0;
            _history.ScrollToCaret();
        }

        private void Write(string text, Font font, Color color)
        {
            _history.SelectionStart = _history.TextLength;
            _history.SelectionLength = 0;
            _history.SelectionFont = font;
            _history.SelectionColor = color;
            _history.SelectionIndent = text.StartsWith("  ") ? 6 : 0;
            _history.SelectionHangingIndent = text.StartsWith("  ") ? 14 : 0;
            _history.AppendText(text);
        }

        /// <summary>
        /// This is the click on the email address: the address is copied, and a new Gmail message to it (with the version
        /// in the subject) opens in the web browser. Gmail is used instead of a "mailto:" link because a mailto: link only
        /// works when an email program is set up in Windows, which office computers usually do not have.
        /// </summary>
        private void OpenMail()
        {
            bool copied = false;
            try
            {
                Clipboard.SetText(AppInfo.Email);
                copied = true;
            }
            catch (System.Runtime.InteropServices.ExternalException) { }   // This is the clipboard being used by another program

            string subject = Uri.EscapeDataString("Dat2DTR " + AppInfo.Version);
            string gmail = "https://mail.google.com/mail/?view=cm&fs=1&to=" + Uri.EscapeDataString(AppInfo.Email) + "&su=" + subject;
            try
            {
                var start = new System.Diagnostics.ProcessStartInfo(gmail);
                start.UseShellExecute = true;   // This is to let Windows open it in the web browser
                System.Diagnostics.Process.Start(start);
                _copied.Text = copied ? "Copied. Gmail opened in the browser." : "Gmail opened in the browser.";
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception || ex is InvalidOperationException)
            {
                _copied.Text = copied ? "Copied: paste it in your email." : "";
                MessageBox.Show(this, "The web browser could not be opened" + (copied ? ", so the address was copied instead" : "") + ":\n\n" +
                                AppInfo.Email + "\n\nPaste it in Gmail or any email you use.", "About Dat2DTR",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _boldFont.Dispose();
                _normalFont.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
