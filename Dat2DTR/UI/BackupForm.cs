namespace Dat2DTR
{
    /// <summary>This is what the user picked in the Backup / Restore window.</summary>
    public enum BackupChoice
    {
        Cancel,
        BackUp,
        Restore
    }

    /// <summary>
    /// This is the small "Backup / Restore" window: back up everything Dat2DTR keeps on this computer
    /// into one .zip file, or put a backup back. The work itself is done by the main window.
    /// </summary>
    internal sealed class BackupForm : Form
    {
        private BackupChoice _choice = BackupChoice.Cancel;

        public BackupForm()
        {
            // This is the same order as a designer-made form, so it scales correctly on 125% / 150% screens.
            SuspendLayout();
            Text = "Backup / Restore";
            Icon = Theme.GetAppIcon();
            Font = new Font("Segoe UI", 9f);
            AutoScaleDimensions = new SizeF(7f, 15f);
            AutoScaleMode = AutoScaleMode.Font;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = Color.White;

            const int margin = 20, width = 460, buttonHeight = 58, gap = 10;
            int y = margin;

            var question = new Label();
            question.Text = "Backup / Restore";
            question.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            question.ForeColor = Theme.TextStrong;
            question.SetBounds(margin, y, width, 28);
            Controls.Add(question);
            y += 28 + 6;

            var how = new Label();
            how.Text = "The names, times typed in, leaves, balances, CTO, Solo Parent Leave, holidays, office hours and the scans of " +
                       "every .dat file opened are kept " +
                       "on this computer only. Back them up now and then (for example to a USB drive or Google Drive), " +
                       "so nothing is lost if the computer breaks or is reformatted.";
            how.ForeColor = Theme.TextMuted;
            how.Font = Font;
            // This is the height the text really needs at this width, so it is never cut off.
            int howHeight = TextRenderer.MeasureText(how.Text, how.Font, new Size(width, 0), TextFormatFlags.WordBreak).Height + 6;
            how.SetBounds(margin, y, width, howHeight);
            Controls.Add(how);
            y += howHeight + 10;

            y = AddChoice(BackupChoice.BackUp, "Back up now", "Save everything in one .zip file, with today's date in its name",
                          y, margin, width, buttonHeight) + gap;
            y = AddChoice(BackupChoice.Restore, "Restore from a backup", "Put a backup .zip back (what is here now is saved first)",
                          y, margin, width, buttonHeight) + gap;

            var cancel = new Button();
            cancel.Text = "Cancel";
            cancel.DialogResult = DialogResult.Cancel;
            cancel.SetBounds(margin + width - 96, y + 6, 96, 32);
            Controls.Add(cancel);
            CancelButton = cancel;

            ClientSize = new Size(width + margin * 2, y + 6 + 32 + margin);
            ResumeLayout(false);
            PerformLayout();
        }

        /// <summary>This is what the user picked. Call after ShowDialog().</summary>
        public BackupChoice GetChoice()
        {
            return _choice;
        }

        /// <summary>This is one big button with a bold first line and a smaller line under it.</summary>
        private int AddChoice(BackupChoice choice, string title, string detail, int y, int x, int width, int height)
        {
            var button = new Button();
            button.Text = title + "\r\n" + detail;
            button.TextAlign = ContentAlignment.MiddleLeft;
            button.Padding = new Padding(12, 0, 12, 0);
            button.Font = new Font("Segoe UI", 10f);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = Theme.Line;
            button.FlatAppearance.MouseOverBackColor = Theme.Highlight;
            button.BackColor = Theme.Panel;
            button.ForeColor = Theme.TextStrong;
            button.Cursor = Cursors.Hand;
            button.SetBounds(x, y, width, height);
            button.Click += (sender, e) =>
            {
                _choice = choice;
                DialogResult = DialogResult.OK;
            };
            Controls.Add(button);
            return y + height;
        }
    }
}
