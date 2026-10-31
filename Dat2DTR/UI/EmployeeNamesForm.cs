namespace Dat2DTR
{
    /// <summary>
    /// This is the "Employee Names" window: a simple table where the user types the name
    /// that belongs to each biometric ID and chooses the work schedule (A, B, C or D).
    /// Employees can also be deleted: tick their Delete box and click "Delete ticked...". When the list is saved,
    /// everything about them is erased (see Form1.EraseEmployees); a backup is made first.
    /// </summary>
    public partial class EmployeeNamesForm : Form
    {
        // This is the column numbers of the table.
        private const int ColDelete = 0, ColId = 1, ColName = 2, ColSchedule = 3;

        private readonly Button btnDelete = new Button();

        // This is the IDs deleted (normalized), and what is done just before they are saved as deleted (the backup).
        private readonly List<string> _deleted = new List<string>();
        private readonly Func<IList<string>, bool>? _beforeDelete;
        private readonly DtrSettings _settings;

        // This is shown only when the second question before deleting was turned off ("Remember my choice"): ticking it turns it back on.
        private readonly CheckBox chkAskTwice = new CheckBox();

        private readonly EmployeeDirectory _names;
        private readonly string[] _scheduleTexts; // This is what the Schedule boxes show: [0] = Schedule A ... [3] = Schedule D

        /// <param name="names">This is the saved list. It is only changed when the user clicks Save.</param>
        /// <param name="idsInFile">This is the IDs found in the opened .dat file, so they can be listed automatically.</param>
        /// <param name="settings">This is used to show what each schedule (A to D) is.</param>
        /// <param name="beforeDelete">This is called with the deleted IDs just before saving (it makes the backup); false = do not save.</param>
        public EmployeeNamesForm(EmployeeDirectory names, IList<string> idsInFile, DtrSettings settings,
                                 Func<IList<string>, bool>? beforeDelete = null)
        {
            InitializeComponent();
            _beforeDelete = beforeDelete;
            _settings = settings;

            // This is the Delete tick box in front of each row, and the "Delete ticked..." button beside Export.
            var colDelete = new DataGridViewCheckBoxColumn();
            colDelete.HeaderText = "Delete";
            colDelete.Width = 60;
            colDelete.SortMode = DataGridViewColumnSortMode.NotSortable;
            dgvNames.Columns.Insert(ColDelete, colDelete);
            btnDelete.Text = "Delete ticked...";
            btnDelete.SetBounds(lblCount.Left, btnExport.Top, 130, btnExport.Height);
            btnDelete.UseVisualStyleBackColor = true;
            btnDelete.Click += btnDelete_Click;
            pnlButtons.Controls.Add(btnDelete);
            int shift = btnDelete.Width + (btnExport.Left - btnImport.Right);
            lblCount.SetBounds(lblCount.Left + shift, lblCount.Top, lblCount.Width - shift, lblCount.Height);

            // This is the line under the table to ask the second question again, when it was turned off.
            chkAskTwice.Text = "Ask a second time before deleting (it was turned off with \"Remember my choice\")";
            chkAskTwice.Dock = DockStyle.Bottom;
            chkAskTwice.Height = 26;
            chkAskTwice.Padding = new Padding(12, 0, 0, 0);
            chkAskTwice.BackColor = Theme.NoticeBack;
            chkAskTwice.Visible = settings.SkipSecondDeleteQuestion;
            chkAskTwice.CheckedChanged += (sender, e) =>
            {
                if (!chkAskTwice.Checked) return;
                _settings.SkipSecondDeleteQuestion = false;
                _settings.Save();
                chkAskTwice.Text = "The second question before deleting is turned on again.";
                chkAskTwice.Enabled = false;
            };
            Controls.Add(chkAskTwice);
            chkAskTwice.BringToFront();
            dgvNames.BringToFront();   // This is so the table fills the space left between the top text and the bottom lines
            Icon = Theme.GetAppIcon();
            GridFit.Attach(this, dgvNames);    // This is so the headings and columns fit the screen it is shown on (125%, 150%...)
            Theme.StylePrimary(btnOk);
            _names = names;

            // This is the empty "add a new row" line at the bottom: it is switched off while the table is filled.
            dgvNames.AllowUserToAddRows = false;

            _scheduleTexts = settings.Schedules.Select(s => s.Description).ToArray();
            foreach (string text in _scheduleTexts)
                colSchedule.Items.Add(text);

            // This is the IDs that still need a name: they come first, so they are easy to find.
            var listed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string id in idsInFile)
            {
                if (names.GetName(id).Length == 0 && listed.Add(EmployeeDirectory.NormalizeId(id)))
                    dgvNames.Rows.Add(false, id, "", _scheduleTexts[names.GetSchedule(id)]);
            }

            // This is everyone who is already in the saved list, after them.
            foreach (EmployeeEntry entry in names.GetAll())
            {
                if (listed.Add(EmployeeDirectory.NormalizeId(entry.Id)))
                    dgvNames.Rows.Add(false, entry.Id, entry.Name, _scheduleTexts[entry.Schedule]);
            }

            dgvNames.AllowUserToAddRows = true;
            UpdateCount();
        }

        private static string TextOf(DataGridViewCell cell)
        {
            return (Convert.ToString(cell.Value) ?? "").Trim();
        }

        /// <summary>This is which schedule a row shows: 0 = A (also when the box is still empty) ... 3 = D.</summary>
        private int ScheduleOf(DataGridViewRow row)
        {
            int index = Array.IndexOf(_scheduleTexts, TextOf(row.Cells[ColSchedule]));
            return index >= 0 ? index : 0;
        }

        /// <summary>This is where the table is read into a fresh list (rows without an ID are skipped).</summary>
        private EmployeeDirectory ReadTable()
        {
            dgvNames.EndEdit();

            var result = new EmployeeDirectory();
            foreach (DataGridViewRow row in dgvNames.Rows)
            {
                if (row.IsNewRow) continue;
                string id = TextOf(row.Cells[ColId]);
                if (id.Length == 0) continue;

                result.SetName(id, TextOf(row.Cells[ColName]));
                result.SetSchedule(id, ScheduleOf(row));
            }
            return result;
        }

        private void UpdateCount()
        {
            int total = 0, named = 0;
            foreach (DataGridViewRow row in dgvNames.Rows)
            {
                if (row.IsNewRow) continue;
                total++;
                if (TextOf(row.Cells[ColName]).Length > 0) named++;
            }
            lblCount.Text = named + " of " + total + " IDs have a name";
        }

        private void dgvNames_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            UpdateCount();
        }

        /// <summary>This is for a box that ever holds something the table cannot show: ignore it instead of showing an error window.</summary>
        private void dgvNames_DataError(object? sender, DataGridViewDataErrorEventArgs e)
        {
            e.ThrowException = false;
        }

        private void btnImport_Click(object? sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Import a list of employees";
                dialog.Filter = "List of employees (*.csv;*.txt)|*.csv;*.txt|All files (*.*)|*.*";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                var imported = new EmployeeDirectory();
                int count;
                try
                {
                    count = imported.Import(dialog.FileName);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    MessageBox.Show(this, "The file could not be opened.\n\n" + ex.Message,
                        "Employee Names", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (count == 0)
                {
                    MessageBox.Show(this,
                        "No employees were found in that file.\n\n" +
                        "The file should have the biometric ID, then the name, and (if you want) the schedule letter (A, B, C or D). For example:\n\n" +
                        "135,\"DELA CRUZ, JUAN M\",B",
                        "Employee Names", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // This is where the imported employees are put into the table: fill in rows that exist, add the rest.
                dgvNames.EndEdit();
                foreach (EmployeeEntry entry in imported.GetAll())
                {
                    DataGridViewRow? match = null;
                    foreach (DataGridViewRow row in dgvNames.Rows)
                    {
                        if (!row.IsNewRow && EmployeeDirectory.SameId(TextOf(row.Cells[ColId]), entry.Id))
                        {
                            match = row;
                            break;
                        }
                    }

                    if (match == null)
                    {
                        dgvNames.Rows.Add(false, entry.Id, entry.Name, _scheduleTexts[entry.Schedule]);
                    }
                    else
                    {
                        if (entry.Name.Length > 0)
                            match.Cells[ColName].Value = entry.Name;
                        if (entry.ScheduleGiven)   // This is because a file without a schedule column leaves the schedule alone
                            match.Cells[ColSchedule].Value = _scheduleTexts[entry.Schedule];
                    }
                }

                UpdateCount();
                MessageBox.Show(this, count + " employees were imported.\n\nClick Save to keep them.",
                    "Employee Names", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnExport_Click(object? sender, EventArgs e)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Title = "Export the list of employees";
                dialog.Filter = "List of employees (*.csv)|*.csv";
                dialog.FileName = "employee-names.csv";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                try
                {
                    ReadTable().Export(dialog.FileName);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    MessageBox.Show(this, "The list could not be saved.\n\n" + ex.Message,
                        "Employee Names", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        /// <summary>This is the IDs deleted in this window (after Save), so the main window erases everything about them.</summary>
        public List<string> GetDeletedIds()
        {
            return new List<string>(_deleted);
        }

        /// <summary>
        /// This is "Delete ticked...": the rows with a tick in Delete are taken out of the table, after asking.
        /// Nothing is erased until Save is clicked.
        /// </summary>
        private void btnDelete_Click(object? sender, EventArgs e)
        {
            dgvNames.EndEdit();
            var ticked = new List<DataGridViewRow>();
            foreach (DataGridViewRow row in dgvNames.Rows)
                if (!row.IsNewRow && Equals(row.Cells[ColDelete].Value, true) && TextOf(row.Cells[ColId]).Length > 0)
                    ticked.Add(row);
            if (ticked.Count == 0)
            {
                MessageBox.Show(this, "Tick the Delete box of each employee you want to delete first.",
                                "Employee Names", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var shown = ticked.Take(15).Select(r => "   " + TextOf(r.Cells[ColId]) + "   " +
                                                    (TextOf(r.Cells[ColName]).Length > 0 ? TextOf(r.Cells[ColName]) : "(no name yet)")).ToList();
            if (ticked.Count > 15) shown.Add("   ... and " + (ticked.Count - 15) + " more");
            if (MessageBox.Show(this,
                    "Delete " + (ticked.Count == 1 ? "this employee" : "these " + ticked.Count + " employees") + "?\n\n" +
                    string.Join("\n", shown.ToArray()) + "\n\n" +
                    "When you click Save, EVERYTHING about them is erased: the name and schedule, leaves, leave balances, CTO, " +
                    "Solo Parent Leave, times typed in, Flexi-Time and their kept scans.\n\n" +
                    "A backup of everything is saved first in Documents (Dat2DTR-before-delete-....zip), so it can be brought " +
                    "back with Backup / Restore. If a .dat file with their scans is opened again, they show up again (with no name).",
                    "Employee Names", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;

            // This is the second question (unless it was turned off with "Remember my choice").
            if (!_settings.SkipSecondDeleteQuestion && !AskSecondTime(ticked.Count))
                return;

            foreach (DataGridViewRow row in ticked)
            {
                string id = EmployeeDirectory.NormalizeId(TextOf(row.Cells[ColId]));
                if (!_deleted.Contains(id, StringComparer.OrdinalIgnoreCase)) _deleted.Add(id);
                dgvNames.Rows.Remove(row);
            }
            UpdateCount();
            lblCount.Text += "   (" + _deleted.Count + " to delete when you click Save)";
        }

        /// <summary>
        /// This is the second question before deleting: "Are you really sure?", with "Remember my choice". Returns true for
        /// "Yes, delete". When the box is ticked and Yes is clicked, this second question is not asked again (the setting
        /// can be turned back on below the table). No is never remembered.
        /// </summary>
        private bool AskSecondTime(int count)
        {
            using (var dialog = new Form())
            {
                // This is the same order as a designer-made form, so it scales correctly on 125% / 150% screens.
                dialog.SuspendLayout();
                dialog.Text = "Employee Names - are you sure?";
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

                const int margin = 20, width = 420;
                var icon = new PictureBox();
                icon.Image = SystemIcons.Warning.ToBitmap();
                icon.SizeMode = PictureBoxSizeMode.AutoSize;
                icon.Location = new Point(margin, margin);
                dialog.Controls.Add(icon);

                var heading = new Label();
                heading.Text = "Are you really sure?";
                heading.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
                heading.ForeColor = Theme.Red;
                heading.SetBounds(margin + 44, margin, width - 44, 26);
                dialog.Controls.Add(heading);

                var text = new Label();
                text.Text = (count == 1 ? "This employee" : "These " + count + " employees") + " and everything about them will be erased " +
                            "when you click Save. Only the backup in Documents can bring them back.";
                int textHeight = TextRenderer.MeasureText(text.Text, dialog.Font, new Size(width - 44, 0), TextFormatFlags.WordBreak).Height + 4;
                text.SetBounds(margin + 44, margin + 30, width - 44, textHeight);
                dialog.Controls.Add(text);
                int y = margin + 30 + textHeight + 12;

                var remember = new CheckBox();
                remember.Text = "Remember my choice (do not ask this second question again)";
                remember.SetBounds(margin + 44, y, width - 44, 22);
                dialog.Controls.Add(remember);
                y += 22 + 16;

                var yes = new Button();
                yes.Text = "Yes, delete";
                yes.DialogResult = DialogResult.Yes;
                yes.BackColor = Theme.Red;
                yes.ForeColor = Color.White;
                yes.FlatStyle = FlatStyle.Flat;
                yes.SetBounds(margin + width - 110 - 8 - 96, y, 110, 32);
                dialog.Controls.Add(yes);

                var no = new Button();
                no.Text = "No";
                no.DialogResult = DialogResult.No;
                no.SetBounds(margin + width - 96, y, 96, 32);
                dialog.Controls.Add(no);
                dialog.CancelButton = no;
                dialog.AcceptButton = no;   // This is so Enter means No: deleting needs a click on "Yes, delete"

                dialog.ClientSize = new Size(width + margin * 2, y + 32 + margin);
                dialog.ResumeLayout(false);
                dialog.PerformLayout();
                no.Select();

                if (dialog.ShowDialog(this) != DialogResult.Yes)
                    return false;
                if (remember.Checked)
                {
                    _settings.SkipSecondDeleteQuestion = true;
                    _settings.Save();
                    chkAskTwice.Checked = false;
                    chkAskTwice.Enabled = true;
                    chkAskTwice.Text = "Ask a second time before deleting (it was turned off with \"Remember my choice\")";
                    chkAskTwice.Visible = true;
                }
                return true;
            }
        }

        private void btnOk_Click(object? sender, EventArgs e)
        {
            EmployeeDirectory edited = ReadTable();

            // This is an ID deleted, then typed in again: it is kept, not erased.
            _deleted.RemoveAll(id => edited.GetAll().Any(x => EmployeeDirectory.SameId(x.Id, id)));
            if (_deleted.Count > 0 && _beforeDelete != null && !_beforeDelete(_deleted))
                return;   // This is the backup that could not be made: nothing is saved or erased

            _names.Clear();
            foreach (EmployeeEntry entry in edited.GetAll())
            {
                _names.SetName(entry.Id, entry.Name);
                _names.SetSchedule(entry.Id, entry.Schedule);
            }

            try
            {
                _names.SaveDefault();
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                MessageBox.Show(this,
                    "The list will be used now, but it could not be saved for next time.\n\n" + ex.Message,
                    "Employee Names", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            DialogResult = DialogResult.OK;
        }
    }
}
