namespace Dat2DTR
{
    /// <summary>
    /// This is a small window with a question and one big button per answer (a bold first line and a smaller line
    /// under it), like "What do you want to print?". Used by the Reports window and "Save the scans as .dat".
    /// </summary>
    internal static class OptionDialog
    {
        /// <summary>This is the question asked; returns the number of the answer clicked (0, 1, 2...), or -1 for Cancel.</summary>
        /// <param name="answers">This is each answer: Key = the first line, Value = the smaller line under it.</param>
        public static int Ask(IWin32Window owner, string question, IList<KeyValuePair<string, string>> answers)
        {
            int picked = -1;
            using (var dialog = new Form())
            {
                // This is the same order as a designer-made form, so it scales correctly on 125% / 150% screens.
                dialog.SuspendLayout();
                dialog.Text = "Dat2DTR";
                dialog.Icon = Theme.GetAppIcon();
                dialog.Font = new Font("Segoe UI", 9f);
                dialog.AutoScaleDimensions = new SizeF(7f, 15f);
                dialog.AutoScaleMode = AutoScaleMode.Font;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;
                dialog.ShowInTaskbar = false;
                dialog.BackColor = Color.White;

                const int margin = 20, width = 440, height = 58;
                var heading = new Label();
                heading.Text = question;
                heading.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
                heading.ForeColor = Theme.TextStrong;
                heading.SetBounds(margin, margin, width, 28);
                dialog.Controls.Add(heading);

                int y = margin + 28 + 14;
                for (int i = 0; i < answers.Count; i++)
                {
                    var button = new Button();
                    button.Text = answers[i].Key + (answers[i].Value.Length > 0 ? "\r\n" + answers[i].Value : "");
                    button.TextAlign = ContentAlignment.MiddleLeft;
                    button.Padding = new Padding(12, 0, 12, 0);
                    button.Font = new Font("Segoe UI", 10f);
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderColor = Theme.Line;
                    button.FlatAppearance.MouseOverBackColor = Theme.Highlight;
                    button.BackColor = Theme.Panel;
                    button.ForeColor = Theme.TextStrong;
                    button.Cursor = Cursors.Hand;
                    button.SetBounds(margin, y, width, height);
                    int number = i;
                    button.Click += (sender, e) => { picked = number; dialog.DialogResult = DialogResult.OK; };
                    dialog.Controls.Add(button);
                    y += height + 10;
                }

                var cancel = new Button();
                cancel.Text = "Cancel";
                cancel.DialogResult = DialogResult.Cancel;
                cancel.SetBounds(margin + width - 96, y + 6, 96, 32);
                dialog.Controls.Add(cancel);
                dialog.CancelButton = cancel;
                dialog.ClientSize = new Size(width + margin * 2, y + 6 + 32 + margin);
                dialog.ResumeLayout(false);
                dialog.PerformLayout();

                if (dialog.ShowDialog(owner) != DialogResult.OK)
                    return -1;
            }
            return picked;
        }
    }
}
