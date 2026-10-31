namespace Dat2DTR
{
    /// <summary>
    /// This is the "Leave Summary" window: every employee of the month in one table, with the VL, SL,
    /// CTO and Solo Parent balance at the end of the month, the SPL, Wellness and Forced Leave used this
    /// year, and the reminders. Clicking a heading sorts the table. "Save to Excel..." saves it.
    /// </summary>
    internal sealed class LeaveSummaryForm : Form
    {
        private readonly DataGridView _grid = new DataGridView();
        private readonly List<LeaveSummaryRow> _rows;
        private readonly string _title;
        private readonly DtrPaper _paper;
        private readonly string _startFolder;

        /// <summary>This is the folder the file was saved in (empty when nothing was saved).</summary>
        private string _savedFolder = "";

        /// <param name="monthText">This is the month the balances are for, e.g. "October 2026".</param>
        /// <param name="rows">This is one row per employee (see LeaveSummary.Make).</param>
        /// <param name="startFolder">This is the folder the Save window starts in.</param>
        public LeaveSummaryForm(string monthText, List<LeaveSummaryRow> rows, DtrPaper paper, string startFolder)
        {
            _rows = rows;
            _title = "Leave balances at the end of " + monthText;
            _paper = paper;
            _startFolder = startFolder;

            // This is the same order as a designer-made form, so it scales correctly on 125% / 150% screens.
            SuspendLayout();
            Text = "Leave Summary";
            Icon = Theme.GetAppIcon();
            Font = new Font("Segoe UI", 9f);
            AutoScaleDimensions = new SizeF(7f, 15f);
            AutoScaleMode = AutoScaleMode.Font;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(1180, 640);
            MinimumSize = new Size(900, 420);

            var title = new Label();
            title.Text = _title + "   (" + rows.Count + (rows.Count == 1 ? " employee)" : " employees)");
            title.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            title.ForeColor = Theme.Navy;
            title.AutoEllipsis = true;
            title.SetBounds(16, 10, 1148, 26);
            title.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            var how = new Label();
            how.Text = "VL and SL are leave credits; CTO and SOLO are the days left. SPL, WL and Forced Leave are the days used this year " +
                       "(3, 5 and 5 days a year). An empty VL / SL means the balance is not known yet: type it once in Edit Leave. " +
                       "Click a heading to sort.";
            how.ForeColor = Theme.TextMuted;
            how.SetBounds(16, 40, 1148, 40);
            how.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            var top = new Panel();
            top.Dock = DockStyle.Top;
            top.Size = new Size(1180, 86);
            top.BackColor = Color.White;
            top.Controls.Add(title);
            top.Controls.Add(how);
            GridFit.GrowWithText(this, how, top);   // This is so the instructions are never cut off

            _grid.Dock = DockStyle.Fill;
            _grid.BackgroundColor = Color.White;
            _grid.BorderStyle = BorderStyle.None;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.AllowUserToResizeRows = false;
            _grid.ReadOnly = true;
            _grid.RowHeadersVisible = false;
            _grid.MultiSelect = false;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.RowTemplate.Height = 25;

            AddColumn("ID", 100, "", false);
            AddColumn("Name", 230, "", false).MinimumWidth = 120;
            AddColumn("Schedule", 70, "", true);
            AddColumn("VL balance", 90, "0.000", true);
            AddColumn("SL balance", 90, "0.000", true);
            AddColumn("CTO left", 70, "0.###", true);
            AddColumn("SOLO left", 70, "0.###", true);
            AddColumn("SPL used", 70, "0.###", true);
            AddColumn("WL used", 70, "0.###", true);
            AddColumn("Forced Leave used", 90, "0.###", true);
            DataGridViewColumn reminders = AddColumn("Reminders", 300, "", false);
            reminders.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            reminders.Tag = GridFit.FreeText;
            GridFit.Attach(this, _grid);    // This is so it fits the screen it is shown on (125%, 150%...)

            foreach (LeaveSummaryRow row in rows)
            {
                int index = _grid.Rows.Add(row.EmployeeId, row.EmployeeName.Length > 0 ? row.EmployeeName : "(no name yet)",
                    row.Schedule, row.Vl.HasValue ? (object)row.Vl.Value : null, row.Sl.HasValue ? (object)row.Sl.Value : null,
                    row.Cto, row.Solo, row.SplUsed, row.WellnessUsed, row.ForcedUsed, row.Reminders);
                if (row.Reminders.Length > 0)
                    _grid.Rows[index].Cells[10].Style.BackColor = Theme.NoticeBack;
                _grid.Rows[index].Cells[10].ToolTipText = row.Reminders.Replace("; ", "\n");
            }

            var save = new Button();
            save.Text = "Save to Excel...";
            save.SetBounds(16, 16, 150, 34);
            save.Click += Save_Click;
            Theme.StylePrimary(save);

            var close = new Button();
            close.Text = "Close";
            close.DialogResult = DialogResult.Cancel;
            close.SetBounds(1180 - 16 - 96, 16, 96, 34);
            close.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            var bottom = new Panel();
            bottom.Dock = DockStyle.Bottom;
            bottom.Size = new Size(1180, 66);
            bottom.BackColor = Theme.Panel;
            bottom.Controls.Add(save);
            bottom.Controls.Add(close);

            Controls.Add(_grid);
            Controls.Add(bottom);
            Controls.Add(top);
            CancelButton = close;
            ResumeLayout(false);
            PerformLayout();
        }

        /// <summary>This is the folder the summary was saved in, so the main window can remember it ("" = not saved).</summary>
        public string GetSavedFolder()
        {
            return _savedFolder;
        }

        /// <summary>This is one column; numbers keep their real value so sorting by them works.</summary>
        private DataGridViewColumn AddColumn(string heading, int width, string format, bool centred)
        {
            var column = new DataGridViewTextBoxColumn();
            column.HeaderText = heading;
            column.Width = width;
            column.SortMode = DataGridViewColumnSortMode.Automatic;
            if (format.Length > 0)
                column.DefaultCellStyle.Format = format;
            if (centred)
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _grid.Columns.Add(column);
            return column;
        }

        /// <summary>This is "Save to Excel...": the same table as an Excel workbook.</summary>
        private void Save_Click(object? sender, EventArgs e)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Title = "Save the leave summary";
                dialog.Filter = "Excel workbook (*.xlsx)|*.xlsx";
                dialog.FileName = _title.Replace("Leave balances at the end of", "Leave Summary") + ".xlsx";
                dialog.AddExtension = true;
                dialog.OverwritePrompt = true;
                if (Directory.Exists(_startFolder))
                    dialog.InitialDirectory = _startFolder;
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                string path = dialog.FileName;
                if (!path.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                    path += ".xlsx";
                try
                {
                    XlsxWriter.SaveSummary(path, _title, _rows, _paper);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    MessageBox.Show(this, "The file could not be saved.\n\nIf a file with the same name is open in Excel, close it first.\n\n" +
                                    ex.Message, "Leave Summary", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _savedFolder = Path.GetDirectoryName(path) ?? "";
                if (MessageBox.Show(this, "The leave summary was saved here:\n\n" + path + "\n\nDo you want to open it now?",
                                    "Leave Summary", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                {
                    try
                    {
                        var start = new System.Diagnostics.ProcessStartInfo(path);
                        start.UseShellExecute = true;   // This is to let Windows pick the program (Excel)
                        System.Diagnostics.Process.Start(start);
                    }
                    catch (Exception ex) when (ex is System.ComponentModel.Win32Exception || ex is InvalidOperationException)
                    {
                        MessageBox.Show(this, "The file was saved, but it could not be opened.\n\n" + ex.Message,
                                        "Leave Summary", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }
    }
}
