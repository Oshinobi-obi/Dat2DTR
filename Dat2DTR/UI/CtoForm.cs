using System.Globalization;

namespace Dat2DTR
{
    /// <summary>
    /// The "CTO given" window (opened from Edit Leave): a table of every time CTO
    /// (Compensatory Time-Off) was given to the employee - the date, how many days,
    /// and the date it expires. Using CTO is filed in Edit Leave like any other leave.
    /// An employee can have at most LeaveKinds.CtoMaxDays at one time.
    /// </summary>
    internal sealed class CtoForm : Form
    {
        // Column numbers of the table.
        private const int ColGiven = 0, ColDays = 1, ColExpires = 2;

        private static readonly string[] DateFormats =
        {
            "MMM d, yyyy", "MMMM d, yyyy", "MMM d yyyy", "MMMM d yyyy", "M/d/yyyy", "M-d-yyyy", "yyyy-MM-dd", "d MMMM yyyy", "d MMM yyyy"
        };

        private readonly List<KeyValuePair<DateTime, LeaveEntry>> _uses;   // the CTO the employee filed
        private readonly DataGridView _grid = new DataGridView();
        private readonly Label _message = new Label();
        private List<CtoGrant> _result = new List<CtoGrant>();

        /// <param name="who">Name of the employee, for the title.</param>
        /// <param name="grants">The CTO given so far.</param>
        /// <param name="uses">The CTO filed so far (all months), to check the maximum.</param>
        public CtoForm(string who, List<CtoGrant> grants, List<KeyValuePair<DateTime, LeaveEntry>> uses)
        {
            _uses = uses;

            // Same order as a designer-made form, so it scales correctly on 125% / 150% screens.
            SuspendLayout();
            Text = "CTO given";
            Icon = Theme.GetAppIcon();
            Font = new Font("Segoe UI", 9f);
            AutoScaleDimensions = new SizeF(7f, 15f);
            AutoScaleMode = AutoScaleMode.Font;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(620, 470);
            MinimumSize = new Size(560, 380);

            var title = new Label();
            title.Text = "CTO given to " + who;
            title.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            title.ForeColor = Theme.Navy;
            title.AutoEllipsis = true;
            title.SetBounds(16, 10, 588, 26);
            title.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            var how = new Label();
            how.Text = "One line for each time CTO was given: the date, how many days (for example 1 or 0.5), and the date it expires. " +
                       "Leave \"Expires on\" empty to make it one year.\r\n" +
                       "An employee can have at most " + LeaveRules.FormatShort(LeaveKinds.CtoMaxDays) + " days of CTO at one time. " +
                       "CTO not used by its expiry date is lost. Using CTO is filed in Edit Leave.";
            how.ForeColor = Theme.TextMuted;
            how.SetBounds(16, 40, 588, 62);
            how.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            var top = new Panel();
            top.Dock = DockStyle.Top;
            top.Size = new Size(620, 106);
            top.BackColor = Color.White;
            top.Controls.Add(title);
            top.Controls.Add(how);
            GridFit.GrowWithText(this, how, top);   // the instructions are never cut off

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

            AddColumn("Given on", 170);
            AddColumn("Days", 80).DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            DataGridViewColumn expires = AddColumn("Expires on", 170);
            expires.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            GridFit.Attach(this, _grid);    // fits the screen it is shown on (125%, 150%...)

            foreach (CtoGrant g in grants)
                _grid.Rows.Add(ShowDate(g.Given), LeaveRules.FormatShort(g.Days), ShowDate(g.Expires));
            _grid.AllowUserToAddRows = true;     // the empty line at the bottom adds a new one

            var remove = new Button();
            remove.Text = "Remove this line";
            remove.SetBounds(16, 34, 130, 32);
            remove.Click += (sender, e) => HolidaysForm.RemoveCurrentLine(_grid);

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

            Say("Example:   Sep 15, 2026    2    (Expires on left empty = Sep 14, 2027)", false);
        }

        /// <summary>The CTO given, after the user clicked Save.</summary>
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

        /// <summary>"Sep 15, 2026" - the way dates are shown in the table.</summary>
        private static string ShowDate(DateTime date)
        {
            return date.ToString("MMM d, yyyy", CultureInfo.InvariantCulture);
        }

        /// <summary>Reads a typed date such as "9/15/2026", "Sep 15, 2026" or "2026-09-15".</summary>
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
                    continue; // an empty line

                int line = row.Index + 1;
                DateTime given, expires;
                decimal? days;

                if (!TryReadDate(givenText, out given))
                {
                    Problem(row, ColGiven, "Line " + line + ": \"" + givenText + "\" is not a date. Type it like  Sep 15, 2026  or  9/15/2026.");
                    return;
                }
                if (!LeaveRules.TryParseCredits(daysText, out days) || !days.HasValue || days.Value <= 0 ||
                    days.Value > LeaveKinds.CtoMaxDays)
                {
                    Problem(row, ColDays, "Line " + line + ": type how many days of CTO were given, from 0.5 to " +
                            LeaveRules.FormatShort(LeaveKinds.CtoMaxDays) + ".");
                    return;
                }
                if (expiresText.Length == 0)
                {
                    expires = given.AddMonths(LeaveKinds.CtoValidMonths).AddDays(-1);
                }
                else if (!TryReadDate(expiresText, out expires) || expires < given)
                {
                    Problem(row, ColExpires, "Line " + line + ": the expiry date must be a date on or after the date it was given.");
                    return;
                }

                grants.Add(new CtoGrant { Given = given.Date, Days = days.Value, Expires = expires.Date });
            }

            // At most so many days at one time: check right after each time CTO is given.
            grants = grants.OrderBy(g => g.Given).ToList();
            foreach (CtoGrant g in grants)
            {
                decimal balance = CtoCalculator.BalanceOn(grants, _uses, g.Given);
                if (balance > LeaveKinds.CtoMaxDays)
                {
                    MessageBox.Show(this,
                        "On " + ShowDate(g.Given) + " the employee would have " + LeaveRules.FormatShort(balance) +
                        " days of CTO.\n\nThe most is " + LeaveRules.FormatShort(LeaveKinds.CtoMaxDays) +
                        " days at one time. Please give fewer days.",
                        "CTO given", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            MessageBox.Show(this, text, "CTO given", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
