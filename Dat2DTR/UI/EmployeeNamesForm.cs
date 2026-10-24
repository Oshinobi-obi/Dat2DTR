namespace Dat2DTR
{
    /// <summary>
    /// This is the "Employee Names" window: a simple table where the user types the name
    /// that belongs to each biometric ID and chooses the work schedule (A, B, C or D).
    /// </summary>
    public partial class EmployeeNamesForm : Form
    {
        // This is the column numbers of the table.
        private const int ColId = 0, ColName = 1, ColSchedule = 2;

        private readonly EmployeeDirectory _names;
        private readonly string[] _scheduleTexts; // This is what the Schedule boxes show: [0] = Schedule A ... [3] = Schedule D

        /// <param name="names">This is the saved list. It is only changed when the user clicks Save.</param>
        /// <param name="idsInFile">This is the IDs found in the opened .dat file, so they can be listed automatically.</param>
        /// <param name="settings">This is used to show what each schedule (A to D) is.</param>
        public EmployeeNamesForm(EmployeeDirectory names, IList<string> idsInFile, DtrSettings settings)
        {
            InitializeComponent();
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
                    dgvNames.Rows.Add(id, "", _scheduleTexts[names.GetSchedule(id)]);
            }

            // This is everyone who is already in the saved list, after them.
            foreach (EmployeeEntry entry in names.GetAll())
            {
                if (listed.Add(EmployeeDirectory.NormalizeId(entry.Id)))
                    dgvNames.Rows.Add(entry.Id, entry.Name, _scheduleTexts[entry.Schedule]);
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
                        dgvNames.Rows.Add(entry.Id, entry.Name, _scheduleTexts[entry.Schedule]);
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

        private void btnOk_Click(object? sender, EventArgs e)
        {
            EmployeeDirectory edited = ReadTable();

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
