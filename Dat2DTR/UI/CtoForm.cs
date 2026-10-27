using System.Globalization;

namespace Dat2DTR
{
    /// <summary>
    /// This is the "CTO given" and "Solo Parent given" windows (opened from Edit Leave): a table of every
    /// time CTO (Compensatory Time-Off) or Solo Parent Leave was given to the employee - the date,
    /// how many days, and the date it expires. Typing the date given fills in the expiry date by
    /// itself (one year). Using it is filed in Edit Leave like any other leave.
    /// An employee can have at most LeaveKinds.MaxGiven days at one time.
    /// </summary>
    internal sealed class CtoForm : Form
    {
        // This is the column numbers of the table.
        private const int ColGiven = 0, ColDays = 1, ColExpires = 2, ColLeft = 3, ColStatus = 4;

        /// <summary>This is the Tag of the Status cell of a line that is used up or expired: grey and locked.</summary>
        private const string Locked = "locked";

        private static readonly string[] DateFormats =
        {
            "MMM d, yyyy", "MMMM d, yyyy", "MMM d yyyy", "MMMM d yyyy", "M/d/yyyy", "M-d-yyyy", "yyyy-MM-dd", "d MMMM yyyy", "d MMM yyyy"
        };

        /// <summary>This is the Row.Tag of a line whose expiry date was typed by hand: it is not filled in again.</summary>
        private const string TypedExpiry = "typed";

        private readonly LeaveType _type;                                  // This is CTO or Solo Parent Leave
        private readonly string _name;                                     // This is "CTO" or "Solo Parent Leave"
        private readonly decimal _maxDays;
        private readonly List<KeyValuePair<DateTime, LeaveEntry>> _uses;   // This is what the employee filed
        private readonly DataGridView _grid = new DataGridView();
        private readonly Label _message = new Label();
        private List<CtoGrant> _result = new List<CtoGrant>();
        private bool _filling;

        /// <param name="who">This is the name of the employee, for the title.</param>
        /// <param name="grants">This is what was given so far.</param>
        /// <param name="uses">This is what was filed so far (all months), to check the maximum.</param>
        /// <param name="type">This is LeaveType.Cto or LeaveType.SoloParent.</param>
        public CtoForm(string who, List<CtoGrant> grants, List<KeyValuePair<DateTime, LeaveEntry>> uses,
                       LeaveType type = LeaveType.Cto)
        {
            _uses = uses;
            _type = type == LeaveType.SoloParent ? LeaveType.SoloParent : LeaveType.Cto;
            _name = _type == LeaveType.SoloParent ? "Solo Parent Leave" : "CTO";
            _maxDays = LeaveKinds.MaxGiven(_type);

            // This is the same order as a designer-made form, so it scales correctly on 125% / 150% screens.
            SuspendLayout();
            Text = _name + " given";
            Icon = Theme.GetAppIcon();
            Font = new Font("Segoe UI", 9f);
            AutoScaleDimensions = new SizeF(7f, 15f);
            AutoScaleMode = AutoScaleMode.Font;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(780, 470);
            MinimumSize = new Size(700, 380);

            var title = new Label();
            title.Text = _name + " given to " + who;
            title.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            title.ForeColor = Theme.Navy;
            title.AutoEllipsis = true;
            title.SetBounds(16, 10, 588, 26);
            title.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            var how = new Label();
            how.Text = "One line for each time " + _name + " was given: the date, how many days (for example 1 or 0.5), and the date it expires. " +
                       "\"Expires on\" is filled in by itself when you type the date given: one year. Type over it if it is different.\r\n" +
                       "An employee can have at most " + LeaveRules.FormatShort(_maxDays) + " days of " + _name + " at one time. " +
                       _name + " not used by its expiry date is lost (taken off the balance by itself). Using it is filed in Edit Leave. " +
                       "Lines that are used up or expired are grey: they are kept as a record and cannot be changed.";
            how.ForeColor = Theme.TextMuted;
            how.SetBounds(16, 40, 588, 62);
            how.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            var top = new Panel();
            top.Dock = DockStyle.Top;
            top.Size = new Size(620, 106);
            top.BackColor = Color.White;
            top.Controls.Add(title);
            top.Controls.Add(how);
            GridFit.GrowWithText(this, how, top);   // This is so the instructions are never cut off

            _grid.Dock = DockStyle.Fill;
            _grid.BackgroundColor = Color.White;
            _grid.BorderStyle = BorderStyle.None;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = true;
            _grid.AllowUserToResizeRows = false;
            _grid.RowHeadersWidth = 24;
            _grid.MultiSelect = false;
            _grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            _grid.EditMode = DataGridViewEditMode.EditOnEnter;
            _grid.RowTemplate.Height = 25;

            AddColumn("Given on", 150);
            AddColumn("Days", 70).DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            AddColumn("Expires on", 150);
            DataGridViewColumn leftColumn = AddColumn("Left", 70);
            leftColumn.ReadOnly = true;
            leftColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            DataGridViewColumn status = AddColumn("Status", 220);
            status.ReadOnly = true;
            status.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            status.Tag = GridFit.FreeText;
            GridFit.Attach(this, _grid);    // This is so it fits the screen it is shown on (125%, 150%...)

            decimal[] left = CtoCalculator.LeftOfEach(grants, _uses);
            for (int k = 0; k < grants.Count; k++)
            {
                CtoGrant g = grants[k];
                int index = _grid.Rows.Add(ShowDate(g.Given), LeaveRules.FormatShort(g.Days), ShowDate(g.Expires));
                if (g.Expires.Date != LeaveKinds.DefaultExpiry(_type, g.Given))
                    _grid.Rows[index].Tag = TypedExpiry;    // This is when a different expiry was typed: keep it
                ShowStatus(_grid.Rows[index], g.Expires, left[k], true);
            }
            _grid.AllowUserToAddRows = true;     // This is the empty line at the bottom: typing in it adds a new one
            _grid.CellValueChanged += Grid_CellValueChanged;

            var remove = new Button();
            remove.Text = "Remove this line";
            remove.SetBounds(16, 34, 130, 32);
            remove.Click += RemoveLine_Click;

            _message.AutoEllipsis = true;
            _message.SetBounds(16, 6, 588, 22);
            _message.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            var save = new Button();
            save.Text = "Save";
            save.SetBounds(620 - 16 - 96 - 8 - 96, 34, 96, 32);
            save.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            save.Click += Save_Click;
            Theme.StylePrimary(save);

            var cancel = new Button();
            cancel.Text = "Cancel";
            cancel.DialogResult = DialogResult.Cancel;
            cancel.SetBounds(620 - 16 - 96, 34, 96, 32);
            cancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            var bottom = new Panel();
            bottom.Dock = DockStyle.Bottom;
            bottom.Size = new Size(620, 78);
            bottom.BackColor = Theme.Panel;
            bottom.Controls.Add(_message);
            bottom.Controls.Add(remove);
            bottom.Controls.Add(save);
            bottom.Controls.Add(cancel);

            Controls.Add(_grid);
            Controls.Add(bottom);
            Controls.Add(top);
            CancelButton = cancel;
            ResumeLayout(false);
            PerformLayout();

            Say("Example:   Sep 15, 2026    2    (Expires on is filled in: Sep 14, 2027)", false);
        }

        /// <summary>
        /// This is where typing the date given fills in "Expires on" with one year, unless an expiry date was typed
        /// by hand on that line. Done just after the change, so the cell being edited is not disturbed.
        /// </summary>
        private void Grid_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (_filling || e.RowIndex < 0 || e.RowIndex >= _grid.Rows.Count)
                return;
            DataGridViewRow row = _grid.Rows[e.RowIndex];

            if (e.ColumnIndex == ColExpires)
                row.Tag = TypedExpiry;       // This is typed by hand: keep it
            if (e.ColumnIndex == ColDays || e.ColumnIndex == ColExpires)
            {
                RefreshLeft();
                return;
            }
            if (e.ColumnIndex != ColGiven)
                return;

            if (IsHandleCreated)
                BeginInvoke(new Action(() => FillExpiry(row)));
            else
                FillExpiry(row);
        }

        /// <summary>This is where the date given is written the usual way, with its expiry date: one year.</summary>
        internal void FillExpiry(DataGridViewRow row)
        {
            if (row.DataGridView != _grid || row.IsNewRow)
                return;
            DateTime given;
            if (!TryReadDate(Convert.ToString(row.Cells[ColGiven].Value), out given))
                return;

            string expiresText = (Convert.ToString(row.Cells[ColExpires].Value) ?? "").Trim();
            bool keepTyped = row.Tag as string == TypedExpiry && expiresText.Length > 0;

            _filling = true;
            try
            {
                row.Cells[ColGiven].Value = ShowDate(given);
                if (!keepTyped)
                    row.Cells[ColExpires].Value = ShowDate(LeaveKinds.DefaultExpiry(_type, given));
                // This is for a cell of that line that may be open for typing: show it the new text
                if (_grid.IsCurrentCellInEditMode && _grid.CurrentCell != null && _grid.CurrentCell.RowIndex == row.Index)
                    _grid.RefreshEdit();
            }
            finally
            {
                _filling = false;
            }

            if (!keepTyped)
                Say("Expires on " + ShowDate(LeaveKinds.DefaultExpiry(_type, given)) + ": one year from the date given. " +
                    "Type over it if it is different.", false);
            RefreshLeft();
        }

        /// <summary>
        /// This is where the Left and Status of a line are written: "Active", "Used up", or "Expired ... (N not used)".
        /// A line that is used up or expired when the window opens is made grey and locked (lock = true).
        /// </summary>
        private void ShowStatus(DataGridViewRow row, DateTime expires, decimal left, bool lockIt)
        {
            bool usedUp = left <= 0m;
            bool expired = !usedUp && expires.Date < DateTime.Today;
            string text = usedUp ? "Used up" :
                          expired ? "Expired " + ShowDate(expires) + " (" + LeaveRules.FormatShort(left) + " not used)" :
                          "Active";

            _filling = true;
            try
            {
                row.Cells[ColLeft].Value = LeaveRules.FormatShort(expired ? 0m : left);
                row.Cells[ColStatus].Value = text;
            }
            finally
            {
                _filling = false;
            }

            if (lockIt && (usedUp || expired))
            {
                row.ReadOnly = true;
                row.Cells[ColStatus].Tag = Locked;
                row.DefaultCellStyle.BackColor = Theme.Panel;
                row.DefaultCellStyle.ForeColor = Theme.TextMuted;
                row.DefaultCellStyle.SelectionBackColor = Theme.Line;
                row.DefaultCellStyle.SelectionForeColor = Theme.TextMuted;
            }
            else if (row.Cells[ColStatus].Tag as string != Locked)
            {
                row.Cells[ColStatus].Style.ForeColor = expired ? Theme.Red : Color.Empty;
            }
        }

        /// <summary>This is where Left and Status are worked out again after a line was typed or changed.</summary>
        private void RefreshLeft()
        {
            var rows = new List<DataGridViewRow>();
            var grants = new List<CtoGrant>();
            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (row.IsNewRow) continue;
                DateTime given, expires;
                decimal? days;
                if (!TryReadDate(Convert.ToString(row.Cells[ColGiven].Value), out given) ||
                    !LeaveRules.TryParseCredits(Convert.ToString(row.Cells[ColDays].Value), out days) || !days.HasValue || days.Value <= 0)
                {
                    if (row.Cells[ColStatus].Tag as string != Locked) ClearStatus(row);
                    continue;
                }
                string expiresText = (Convert.ToString(row.Cells[ColExpires].Value) ?? "").Trim();
                if (expiresText.Length == 0) expires = LeaveKinds.DefaultExpiry(_type, given);
                else if (!TryReadDate(expiresText, out expires))
                {
                    if (row.Cells[ColStatus].Tag as string != Locked) ClearStatus(row);
                    continue;
                }
                rows.Add(row);
                grants.Add(new CtoGrant { Given = given.Date, Days = days.Value, Expires = expires.Date });
            }

            decimal[] left = CtoCalculator.LeftOfEach(grants, _uses);
            for (int k = 0; k < rows.Count; k++)
                ShowStatus(rows[k], grants[k].Expires, left[k], false);
        }

        /// <summary>This is an empty Left and Status, for a line that is not finished yet.</summary>
        private void ClearStatus(DataGridViewRow row)
        {
            _filling = true;
            try
            {
                row.Cells[ColLeft].Value = "";
                row.Cells[ColStatus].Value = "";
            }
            finally
            {
                _filling = false;
            }
        }

        /// <summary>This is "Remove this line": a grey (used up or expired) line is only removed after asking.</summary>
        private void RemoveLine_Click(object? sender, EventArgs e)
        {
            DataGridViewRow? row = _grid.CurrentRow;
            if (row != null && !row.IsNewRow && row.Cells[ColStatus].Tag as string == Locked)
            {
                DialogResult answer = MessageBox.Show(this,
                    "This line is " + Convert.ToString(row.Cells[ColStatus].Value) + ". It is kept as a record of the " + _name +
                    " that was given and used.\n\nRemove it anyway? (Only if it was typed by mistake.)",
                    Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                if (answer != DialogResult.Yes)
                    return;
            }
            HolidaysForm.RemoveCurrentLine(_grid);
            RefreshLeft();
        }

        /// <summary>This is what was given, after the user clicked Save.</summary>
        public List<CtoGrant> GetGrants()
        {
            return _result;
        }

        private DataGridViewColumn AddColumn(string heading, int width)
        {
            var column = new DataGridViewTextBoxColumn();
            column.HeaderText = heading;
            column.Width = width;
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
            _grid.Columns.Add(column);
            return column;
        }

        /// <summary>This is "Sep 15, 2026" - the way dates are shown in the table.</summary>
        private static string ShowDate(DateTime date)
        {
            return date.ToString("MMM d, yyyy", CultureInfo.InvariantCulture);
        }

        /// <summary>This is where a typed date such as "9/15/2026", "Sep 15, 2026" or "2026-09-15" is read.</summary>
        internal static bool TryReadDate(string? text, out DateTime date)
        {
            string typed = (text ?? "").Trim();
            return DateTime.TryParseExact(typed, DateFormats, CultureInfo.InvariantCulture,
                                          DateTimeStyles.AllowWhiteSpaces, out date);
        }

        private void Say(string text, bool problem)
        {
            _message.Text = text;
            _message.ForeColor = problem ? Theme.Red : Theme.TextMuted;
        }

        private void Save_Click(object? sender, EventArgs e)
        {
            _grid.EndEdit();
            var grants = new List<CtoGrant>();

            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (row.IsNewRow) continue;
                string givenText = (Convert.ToString(row.Cells[ColGiven].Value) ?? "").Trim();
                string daysText = (Convert.ToString(row.Cells[ColDays].Value) ?? "").Trim();
                string expiresText = (Convert.ToString(row.Cells[ColExpires].Value) ?? "").Trim();
                if (givenText.Length == 0 && daysText.Length == 0 && expiresText.Length == 0)
                    continue; // This is an empty line

                int line = row.Index + 1;
                DateTime given, expires;
                decimal? days;

                if (!TryReadDate(givenText, out given))
                {
                    Problem(row, ColGiven, "Line " + line + ": \"" + givenText + "\" is not a date. Type it like  Sep 15, 2026  or  9/15/2026.");
                    return;
                }
                if (!LeaveRules.TryParseCredits(daysText, out days) || !days.HasValue || days.Value <= 0 ||
                    days.Value > _maxDays)
                {
                    Problem(row, ColDays, "Line " + line + ": type how many days of " + _name + " were given, from 0.5 to " +
                            LeaveRules.FormatShort(_maxDays) + ".");
                    return;
                }
                if (expiresText.Length == 0)
                {
                    expires = LeaveKinds.DefaultExpiry(_type, given);
                }
                else if (!TryReadDate(expiresText, out expires) || expires < given)
                {
                    Problem(row, ColExpires, "Line " + line + ": the expiry date must be a date on or after the date it was given.");
                    return;
                }

                grants.Add(new CtoGrant { Given = given.Date, Days = days.Value, Expires = expires.Date });
            }

            // This is the limit: at most so many days at one time. Check right after each time it is given.
            grants = grants.OrderBy(g => g.Given).ToList();
            foreach (CtoGrant g in grants)
            {
                decimal balance = CtoCalculator.BalanceOn(grants, _uses, g.Given);
                if (balance > _maxDays)
                {
                    MessageBox.Show(this,
                        "On " + ShowDate(g.Given) + " the employee would have " + LeaveRules.FormatShort(balance) +
                        " days of " + _name + ".\n\nThe most is " + LeaveRules.FormatShort(_maxDays) +
                        " days at one time. Please give fewer days.",
                        Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            _result = grants;
            DialogResult = DialogResult.OK;
        }

        private void Problem(DataGridViewRow row, int column, string text)
        {
            _grid.CurrentCell = row.Cells[column];
            Say(text, true);
            MessageBox.Show(this, text, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
