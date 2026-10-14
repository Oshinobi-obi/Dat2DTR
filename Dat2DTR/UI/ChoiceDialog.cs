namespace Dat2DTR
{
    public enum DtrChoice
    {
        Cancel,
        CurrentOnly,
        Ticked,
        All
    }

    /// <summary>
    /// The small window that asks "Whose DTR do you want to print?" (or save, or change)
    /// with one big button per answer.
    /// </summary>
    internal sealed class ChoiceDialog : Form
    {
        private DtrChoice _choice = DtrChoice.Cancel;

        /// <param name="questionText">The question at the top, e.g. "Whose DTR do you want to print?"</param>
        /// <param name="currentName">Name of the employee whose DTR is on screen.</param>
        /// <param name="tickedCount">How many employees are ticked in the list.</param>
        /// <param name="allCount">How many employees are in the list for this month.</param>
        /// <param name="offerAll">false = no "Everyone" button (Edit Leave only offers this one or the ticked ones).</param>
        public ChoiceDialog(string questionText, string currentName, int tickedCount, int allCount, bool offerAll = true)
        {
            // Same order as a designer-made form, so it scales correctly on 125% / 150% screens.
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

            y = AddChoice(DtrChoice.CurrentOnly, "Only this one", currentName, y, margin, width, buttonHeight) + gap;

            if (tickedCount > 0)
                y = AddChoice(DtrChoice.Ticked, "The " + tickedCount + " I ticked",
                    "Everyone with a tick in the list on the left", y, margin, width, buttonHeight) + gap;

            if (offerAll)
                y = AddChoice(DtrChoice.All, "Everyone (" + allCount + " employees)",
                    "All employees in the list for this month", y, margin, width, buttonHeight) + gap;

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

        /// <summary>What the user picked. Call after ShowDialog().</summary>
        public DtrChoice GetChoice()
        {
            return _choice;
        }

        private int AddChoice(DtrChoice choice, string title, string detail, int y, int x, int width, int height)
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
