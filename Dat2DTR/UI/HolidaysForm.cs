using System.Globalization;

namespace Dat2DTR
{
    /// <summary>
    /// The "Holidays" window: the holiday calendar, typed once a year and used for every employee.
    /// On a holiday the DTR says HOLIDAY instead of ABSENT and nothing is deducted.
    /// Two tables: Regular Holidays and Special (Non-Working) Days.
    /// "Add the holidays of ..." fills in the Philippine holidays whose dates are fixed by law
    /// (Holy Week is worked out); the ones announced by Proclamation each year are typed by hand.
    /// </summary>
    internal sealed class HolidaysForm : Form
    {
        private const int ColDate = 0, ColDay = 1, ColName = 2;

        private readonly DataGridView _regular = new DataGridView();
        private readonly DataGridView _special = new DataGridView();
        private DataGridView _lastUsed;                 // the table "Remove this line" works on
        private readonly ComboBox _year = new ComboBox();
        private readonly Label _message = new Label();
        private HolidayCalendar _result;

        /// <param name="calendar">The saved calendar. It is only changed when the user clicks Save.</param>
        /// <param name="year">The year to offer first (the year of the month on screen).</param>
        public HolidaysForm(HolidayCalendar calendar, int year)
        {
            _result = calendar.Clone();
            _lastUsed = _regular;

            // Same order as a designer-made form, so it scales correctly on 125% / 150% screens.
            SuspendLayout();
            Text = "Holidays";
            Icon = Theme.GetAppIcon();
            Font = new Font("Segoe UI", 9f);
            AutoScaleDimensions = new SizeF(7f, 15f);
            AutoScaleMode = AutoScaleMode.Font;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(720, 720);
            MinimumSize = new Size(600, 520);

            var title = new Label();
            title.Text = "Holidays (used for every employee)";
            title.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            title.ForeColor = Theme.Navy;
            title.SetBounds(16, 10, 688, 26);
            title.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            var how = new Label();
            how.Text = "On a holiday the DTR says HOLIDAY instead of ABSENT, and nothing is deducted. A leave filed in Edit Leave " +
                       "on the same day comes first. Holidays on a Saturday or Sunday are not shown on the DTR.\r\n" +
                       "\"Add the holidays of\" fills in the Philippine holidays whose dates are fixed by law (Holy Week included). " +
                       "Chinese New Year, Eid'l Fitr, Eid'l Adha and any other day declared by Proclamation change every year: " +
                       "type them in the empty line at the bottom of the table they belong to. Always check the Proclamation of the year - a holiday can be moved.";
            how.ForeColor = Theme.TextMuted;
            how.SetBounds(16, 38, 688, 80);
            how.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            var top = new Panel();
            top.Dock = DockStyle.Top;
            top.Size = new Size(720, 124);
            top.BackColor = Color.White;
            top.Controls.Add(title);
            top.Controls.Add(how);
            GridFit.GrowWithText(this, how, top);

            // ---- the two tables, one under the other, each with its heading
            SetUpGrid(_regular);
            SetUpGrid(_special);

            var tables = new TableLayoutPanel();
            tables.Dock = DockStyle.Fill;
            tables.ColumnCount = 1;
            tables.RowCount = 4;
            tables.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tables.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tables.RowStyles.Add(new RowStyle(SizeType.Percent, 55f));
            tables.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tables.RowStyles.Add(new RowStyle(SizeType.Percent, 45f));
            tables.BackColor = Color.White;
            tables.Controls.Add(TableHeading("Regular Holidays"), 0, 0);
            tables.Controls.Add(_regular, 0, 1);
            tables.Controls.Add(TableHeading("Special (Non-Working) Days"), 0, 2);
            tables.Controls.Add(_special, 0, 3);

            ShowAll();
            _regular.AllowUserToAddRows = true;     // the empty line at the bottom adds a new one
            _special.AllowUserToAddRows = true;

            // ---- bottom
            var yearLabel = new Label();
            yearLabel.Text = "Add the holidays of";
            yearLabel.TextAlign = ContentAlignment.MiddleLeft;
            yearLabel.SetBounds(16, 34, 118, 32);

            _year.DropDownStyle = ComboBoxStyle.DropDownList;
            for (int y = year - 1; y <= year + 1; y++)
                _year.Items.Add(y.ToString(CultureInfo.InvariantCulture));
            _year.SelectedIndex = 1;
            _year.SetBounds(136, 38, 70, 23);

            var add = new Button();
            add.Text = "Add";
            add.SetBounds(212, 34, 64, 32);
            add.Click += Add_Click;

            var remove = new Button();
            remove.Text = "Remove this line";
            remove.SetBounds(290, 34, 130, 32);
            remove.Click += (sender, e) => RemoveCurrentLine(_lastUsed);

            _message.AutoEllipsis = true;
            _message.SetBounds(16, 6, 688, 22);
            _message.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            var save = new Button();
            save.Text = "Save";
            save.SetBounds(720 - 16 - 96 - 8 - 96, 34, 96, 32);
            save.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            save.Click += Save_Click;
            Theme.StylePrimary(save);

            var cancel = new Button();
            cancel.Text = "Cancel";
            cancel.DialogResult = DialogResult.Cancel;
            cancel.SetBounds(720 - 16 - 96, 34, 96, 32);
            cancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            var bottom = new Panel();
            bottom.Dock = DockStyle.Bottom;
            bottom.Size = new Size(720, 78);
            bottom.BackColor = Theme.Panel;
            bottom.Controls.Add(_message);
            bottom.Controls.Add(yearLabel);
            bottom.Controls.Add(_year);
            bottom.Controls.Add(add);
            bottom.Controls.Add(remove);
            bottom.Controls.Add(save);
            bottom.Controls.Add(cancel);

            Controls.Add(tables);
            Controls.Add(bottom);
            Controls.Add(top);
            CancelButton = cancel;
            ResumeLayout(false);
            PerformLayout();

            Say(_result.Count == 0
                ? "No holidays yet. Choose the year and click Add, then add the ones declared by Proclamation."
                : CountText(_result) + " Type a date like  Jun 12, 2026  or  6/12/2026.", false);
        }

        /// <summary>The holidays after the user clicked Save.</summary>
        public HolidayCalendar GetCalendar()
        {
            return _result;
        }

        /// <summary>For the self test: clicks "Add" for a year.</summary>
        internal void AddYear(int year)
        {
            _year.SelectedItem = year.ToString(CultureInfo.InvariantCulture);
            Add_Click(this, EventArgs.Empty);
        }

        /// <summary>
        /// Removes the line with the cursor. The line being typed at the bottom is first committed (or
        /// dropped) - Windows does not allow removing a new line that is still being added.
        /// </summary>
        internal static void RemoveCurrentLine(DataGridView grid)
        {
            if (grid.CurrentCell == null) return;
            int index = grid.CurrentCell.RowIndex;
            grid.EndEdit();
            grid.CurrentCell = null;
            if (index >= 0 && index < grid.Rows.Count && !grid.Rows[index].IsNewRow)
                grid.Rows.RemoveAt(index);
        }

        private static string CountText(HolidayCalendar calendar)
        {
            List<Holiday> all = calendar.GetAll();
            int regular = all.Count(h => h.Kind == HolidayKind.Regular);
            int special = all.Count - regular;
            return regular + (regular == 1 ? " regular holiday" : " regular holidays") + " and " +
                   special + (special == 1 ? " special day." : " special days.");
        }

        private static Label TableHeading(string text)
        {
            var label = new Label();
            label.Text = text;
            label.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            label.ForeColor = Theme.Navy;
            label.AutoSize = true;
            label.Margin = new Padding(14, 10, 3, 4);
            return label;
        }

        private void SetUpGrid(DataGridView grid)
        {
            grid.Dock = DockStyle.Fill;
            grid.BackgroundColor = Color.White;
            grid.BorderStyle = BorderStyle.FixedSingle;
            grid.Margin = new Padding(12, 0, 12, 4);
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = true;
            grid.AllowUserToResizeRows = false;
            grid.RowHeadersWidth = 24;
            grid.MultiSelect = false;
            grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            grid.RowTemplate.Height = 25;
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            AddColumn(grid, "Date", 150, false);
            AddColumn(grid, "Day", 70, true);
            DataGridViewColumn name = AddColumn(grid, "Holiday", 300, false);
            name.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            GridFit.Attach(this, grid);

            grid.CellEndEdit += (sender, e) => ShowDayName(grid, e.RowIndex);
            grid.Enter += (sender, e) => _lastUsed = grid;
        }

        private static DataGridViewColumn AddColumn(DataGridView grid, string heading, int width, bool readOnly)
        {
            var column = new DataGridViewTextBoxColumn();
            column.HeaderText = heading;
            column.Width = width;
            column.ReadOnly = readOnly;
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
            if (readOnly) column.DefaultCellStyle.ForeColor = Theme.TextMuted;
            grid.Columns.Add(column);
            return column;
        }

        private static string ShowDate(DateTime date)
        {
            return date.ToString("MMM d, yyyy", CultureInfo.InvariantCulture);
        }

        /// <summary>Fills both tables from _result, in date order.</summary>
        private void ShowAll()
        {
            Fill(_regular, HolidayKind.Regular);
            Fill(_special, HolidayKind.Special);
        }

        private void Fill(DataGridView grid, HolidayKind kind)
        {
            bool adding = grid.AllowUserToAddRows;
            grid.AllowUserToAddRows = false;
            grid.Rows.Clear();
            foreach (Holiday h in _result.GetAll())
                if (h.Kind == kind)
                    grid.Rows.Add(ShowDate(h.Date), h.Date.ToString("ddd", CultureInfo.InvariantCulture), h.Name);
            grid.AllowUserToAddRows = adding;
        }

        private static void ShowDayName(DataGridView grid, int row)
        {
            if (row < 0 || row >= grid.Rows.Count) return;
            DateTime date;
            string text = Convert.ToString(grid.Rows[row].Cells[ColDate].Value) ?? "";
            grid.Rows[row].Cells[ColDay].Value = CtoForm.TryReadDate(text, out date)
                ? date.ToString("ddd", CultureInfo.InvariantCulture) : "";
        }

        private void Say(string text, bool problem)
        {
            _message.Text = text;
            _message.ForeColor = problem ? Theme.Red : Theme.TextMuted;
        }

        /// <summary>Reads both tables into a calendar. Returns false (and says why) when a line is wrong.</summary>
        private bool ReadTables(out HolidayCalendar calendar)
        {
            calendar = new HolidayCalendar();
            return ReadTable(_regular, HolidayKind.Regular, "Regular Holidays", calendar) &&
                   ReadTable(_special, HolidayKind.Special, "Special Days", calendar);
        }

        private bool ReadTable(DataGridView grid, HolidayKind kind, string tableName, HolidayCalendar calendar)
        {
            grid.EndEdit();
            grid.CurrentCell = null;   // commits the line being typed at the bottom
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow) continue;
                string dateText = (Convert.ToString(row.Cells[ColDate].Value) ?? "").Trim();
                string name = (Convert.ToString(row.Cells[ColName].Value) ?? "").Trim();
                if (dateText.Length == 0 && name.Length == 0) continue;

                DateTime date;
                string problem = "";
                if (!CtoForm.TryReadDate(dateText, out date))
                    problem = "\"" + dateText + "\" is not a date. Type it like  Jun 12, 2026  or  6/12/2026.";
                else if (calendar.GetName(date) != null)
                    problem = ShowDate(date) + " is already in the list (" + calendar.GetName(date) + ").";

                if (problem.Length > 0)
                {
                    grid.CurrentCell = row.Cells[ColDate];
                    string text = tableName + ", line " + (row.Index + 1) + ": " + problem;
                    Say(text, true);
                    MessageBox.Show(this, text, "Holidays", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                calendar.Set(date, name, kind);
            }
            return true;
        }

        private void Add_Click(object? sender, EventArgs e)
        {
            HolidayCalendar now;
            if (!ReadTables(out now)) return;

            int year = int.Parse((string)_year.SelectedItem!, CultureInfo.InvariantCulture);
            int added = 0;
            foreach (Holiday h in HolidayCalendar.PhilippineHolidays(year))
            {
                if (now.GetName(h.Date) != null) continue;   // keep what is already there
                now.Set(h.Date, h.Name, h.Kind);
                added++;
            }
            _result = now;
            ShowAll();
            Say(added + " holidays of " + year + " added. Now add the ones declared by Proclamation (Chinese New Year, Eid'l Fitr, Eid'l Adha...).", false);
        }

        private void Save_Click(object? sender, EventArgs e)
        {
            HolidayCalendar now;
            if (!ReadTables(out now)) return;
            _result = now;
            DialogResult = DialogResult.OK;
        }
    }
}
