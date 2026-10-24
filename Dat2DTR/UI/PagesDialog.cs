namespace Dat2DTR
{
    public enum DtrPages
    {
        Cancel,
        DtrOnly,
        LeaveOnly,
        Both,
        DatFile         // This is the scans of the month, saved again as a .dat file (Save To only)
    }

    /// <summary>
    /// This is the small window that asks "What do you want to print?" (or save):
    /// only the DTR, only the Computation of Leaves, or both - and, when saving,
    /// also the scans as a .dat file.
    /// </summary>
    internal sealed class PagesDialog : Form
    {
        private DtrPages _choice = DtrPages.Cancel;

        /// <param name="questionText">This is the question at the top, e.g. "What do you want to print?"</param>
        /// <param name="offerDatFile">This is true to also offer "Biometrics file (.dat)" (for Save To).</param>
        public PagesDialog(string questionText, bool offerDatFile = false)
        {
            // This is the same order as a designer-made form, so it scales correctly on 125% / 150% screens.
            SuspendLayout();
            Text = "Dat2DTR";
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

            const int margin = 20, width = 440, buttonHeight = 58, gap = 10;
            int y = margin;

            var question = new Label();
            question.Text = questionText;
            question.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            question.ForeColor = Theme.TextStrong;
            question.SetBounds(margin, y, width, 28);
            Controls.Add(question);
            y += 28 + 14;

            y = AddChoice(DtrPages.DtrOnly, "DTR only", "The Daily Time Record (CS Form No. 48)", y, margin, width, buttonHeight) + gap;
            y = AddChoice(DtrPages.LeaveOnly, "Computation of Leaves only", "The leave card and how the month was computed",
                          y, margin, width, buttonHeight) + gap;
            y = AddChoice(DtrPages.Both, "Both", "Each DTR, followed by its Computation of Leaves",
                          y, margin, width, buttonHeight) + gap;
            if (offerDatFile)
                y = AddChoice(DtrPages.DatFile, "Biometrics file (.dat)", "The scans of the month, the same way the device saves them",
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
        public DtrPages GetChoice()
        {
            return _choice;
        }

        private int AddChoice(DtrPages choice, string title, string detail, int y, int x, int width, int height)
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
