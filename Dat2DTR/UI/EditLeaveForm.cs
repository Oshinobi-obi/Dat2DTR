using System.Globalization;

namespace Dat2DTR
{
    /// <summary>
    /// This is the "Edit Leave" window: one row per day, where a person chooses the kind of
    /// leave that was filed for that day (Vacation, Sick, Forced, Special Privilege,
    /// Wellness, CTO, Solo Parent - or Holiday / Suspended, which are not deducted) and whether it was
    /// a whole day or half a day.
    ///
    /// The WFH and IDLAR boxes: WFH = worked from home. On a Suspended day the heading reads
    /// "Is it WFH?". IDLAR can only be ticked after WFH; with it the day counts as present.
    /// WFH on a day with no leave = an ordinary working day worked from home.
    ///
    /// The right side shows the leave balance: the balance brought forward (typed once,
    /// then carried over every month), the credits earned, and the computation of the month.
    /// It is worked out again after every change, so the user sees the result while choosing.
    ///
    /// The window works on COPIES of the leave data. Nothing is kept unless the user clicks Save.
    /// </summary>
    internal sealed class EditLeaveForm : Form
    {
        // This is the column numbers of the table.
        private const int ColDay = 0, ColDtr = 1, ColLeave = 2, ColPart = 3, ColWfh = 4, ColIdlar = 5, ColCharge = 6;

        private const string WfhHeading = "WFH", SuspendedWfhHeading = "Is it WFH?";

        private const string NoLeave = "(no leave)";
        private static readonly string[] PartTexts = { "Whole day", "Half day - morning", "Half day - afternoon" };

        /// <summary>This is "From 3:00 PM" in "How long" = work suspended from that time (Suspended only).</summary>
        private const string FromPrefix = "From ";

        private readonly DtrSheet _attendance;      // This is the DTR without any leave: what the person scanned
        private readonly WorkSchedule _schedule;
        private readonly LeaveStore _leaves;        // This is the working copy of the filed leaves
        private readonly LeaveLedger _ledger;       // This is the working copy of the remembered months
        private readonly CtoStore _cto;             // This is the working copy of the CTO and Solo Parent Leave given
        private readonly Func<LeaveStore, LeaveLedger, CtoStore, LeaveCard> _compute;

        private readonly DataGridView _grid = new DataGridView();
        private readonly DataGridView _summary = new DataGridView();
        private readonly TextBox _openingVl = new TextBox(), _openingSl = new TextBox();
        private readonly TextBox _openingSpl = new TextBox(), _openingWl = new TextBox();   // This is the SPL and WL left, typed by hand
        private readonly TextBox _openingFl = new TextBox();   // This is the Mandatory / Forced Leave left, typed by hand

        /// <summary>This is "Balance below zero: make it 0": the Vacation Leave balance stops at 0 and the rest is without pay.</summary>
        private readonly CheckBox _zeroBalance = new CheckBox();
        private readonly TextBox _earnedVl = new TextBox(), _earnedSl = new TextBox();
        private readonly Label _carried = new Label();
        private readonly TextBox _notes = new TextBox();
        private readonly Label _message = new Label();
        private readonly Font _boldFont;

        /// <summary>This is true while the program itself is filling the table or the boxes (so events are ignored).</summary>
        private bool _filling;

        /// <summary>This is true once the user changed something (for the "discard your changes?" question).</summary>
        private bool _changed;

        /// <summary>This is -1 / +1 when the user clicked Previous / Next (after saving), 0 otherwise.</summary>
        private int _move;
        private readonly Button _previous = new Button(), _next = new Button();

        /// <param name="attendance">This is the DTR of the employee made WITHOUT any leave.</param>
        /// <param name="compute">This is what works out the Computation of Leaves from the given (copied) leave data.</param>
        public EditLeaveForm(DtrSheet attendance, WorkSchedule schedule, LeaveStore leaves, LeaveLedger ledger,
                             CtoStore cto, Func<LeaveStore, LeaveLedger, CtoStore, LeaveCard> compute)
        {
            _attendance = attendance;
            _schedule = schedule;
            _leaves = leaves.Clone();
            _ledger = ledger.Clone();
            _cto = cto.Clone();
            _compute = compute;

            string previousMonth = attendance.Month.AddMonths(-1).ToString("MMMM yyyy", CultureInfo.InvariantCulture);
            decimal dayCharge = LeaveRules.DayCharge(schedule);

            // This is the same order as a designer-made form, so it scales correctly on 125% / 150% screens.
            SuspendLayout();
            Text = "Edit Leave";
            Icon = Theme.GetAppIcon();
            Font = new Font("Segoe UI", 9f);
            _boldFont = new Font(Font, FontStyle.Bold);
            AutoScaleDimensions = new SizeF(7f, 15f);
            AutoScaleMode = AutoScaleMode.Font;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(1200, 736);
            MinimumSize = new Size(1100, 626);

            // This is the top: who and how ----
            var title = new Label();
            title.Text = (attendance.EmployeeName.Length > 0 ? attendance.EmployeeName : "ID " + attendance.EmployeeId) +
                         "   -   " + attendance.MonthText;
            title.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            title.ForeColor = Theme.Navy;
            title.AutoEllipsis = true;
            title.SetBounds(16, 10, 1168, 26);
            title.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            var how = new Label();
            how.Text = "On the day of the leave, choose the kind of leave and how long it was. Holiday and Suspended are not deducted. " +
                       "Tick WFH for work from home, then IDLAR when it is handed in (the day then counts as present).\r\n" +
                       schedule.Description + ".   One whole day of leave = " + LeaveRules.FormatShort(dayCharge) +
                       (dayCharge == 1m ? " day" : " days") + " of leave credits.";
            how.ForeColor = Theme.TextMuted;
            how.SetBounds(16, 38, 1168, 34);
            how.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            var top = new Panel();
            top.Dock = DockStyle.Top;
            top.Size = new Size(1200, 78);
            top.BackColor = Color.White;
            top.Controls.Add(title);
            top.Controls.Add(how);
            GridFit.GrowWithText(this, how, top);   // This is so the instructions are never cut off

            // This is the left: the table, one row per day ----
            _grid.Dock = DockStyle.Fill;
            _grid.BackgroundColor = Color.White;
            _grid.BorderStyle = BorderStyle.None;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.AllowUserToResizeRows = false;
            _grid.RowHeadersVisible = false;
            _grid.MultiSelect = false;
            _grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            _grid.EditMode = DataGridViewEditMode.EditOnEnter;   // This is so one click opens the list
            _grid.RowTemplate.Height = 25;

            AddTextColumn(_grid, "Day", 70);
            AddTextColumn(_grid, "On the DTR", 132);

            DataGridViewComboBoxColumn leaveColumn = AddListColumn("Leave", 196);
            leaveColumn.Items.Add(NoLeave);
            foreach (LeaveKind kind in LeaveKinds.All)
                if (kind.Type != LeaveType.WorkFromHome)    // This is because that one is "(no leave)" with WFH ticked
                    leaveColumn.Items.Add(kind.Name);

            DataGridViewComboBoxColumn partColumn = AddListColumn("How long", 140);
            foreach (string text in PartTexts)
                partColumn.Items.Add(text);
            // This is for Suspended: work suspended from a time, every half hour of the working day
            // (and any other time already filed this month).
            var times = new List<TimeSpan>();
            for (TimeSpan t = schedule.TimeIn.Add(TimeSpan.FromMinutes(30)); t < schedule.TimeOut; t = t.Add(TimeSpan.FromMinutes(30)))
                times.Add(t);
            foreach (var pair in leaves.GetMonth(attendance.EmployeeId, attendance.Month.Year, attendance.Month.Month))
                if (pair.Value.From.HasValue && !times.Contains(pair.Value.From.Value))
                    times.Add(pair.Value.From.Value);
            foreach (TimeSpan t in times.OrderBy(t => t))
                partColumn.Items.Add(FromText(t));

            AddCheckColumn(WfhHeading, 78);
            AddCheckColumn("IDLAR", 56);

            DataGridViewColumn charge = AddTextColumn(_grid, "Deducted", 100);
            charge.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            // This is so the headings are as tall as needed and columns wide enough on any screen (125%, 150%...).
            GridFit.Attach(this, _grid);

            for (int i = 0; i < attendance.Days.Count; i++)
                _grid.Rows.Add();

            _grid.CurrentCellDirtyStateChanged += Grid_CurrentCellDirtyStateChanged;
            _grid.CellValueChanged += Grid_CellValueChanged;
            _grid.CurrentCellChanged += (sender, e) => ShowWfhHeading();
            _grid.DataError += (sender, e) => e.ThrowException = false;

            // This is the right: the balance and the computation ----
            var side = new Panel();
            side.Dock = DockStyle.Right;
            side.Size = new Size(420, 520);
            side.BackColor = Theme.Panel;

            side.Controls.Add(SideHeading("Balance brought forward from " + previousMonth, 10));
            AddBoxRow(side, 36, _openingVl, _openingSl);
            AddBoxRow(side, 64, _openingSpl, _openingWl, "Special", "Wellness");
            AddCenteredBox(side, 92, _openingFl, "Force/Mandatory");
            var tip = new ToolTip();
            tip.SetToolTip(_openingSpl, "Special Privilege Leave LEFT at the start of the month (3 days a year). Empty = worked out from the leaves filed.");
            tip.SetToolTip(_openingWl, "Wellness Leave LEFT at the start of the month (5 days a year). Empty = worked out from the leaves filed.");
            tip.SetToolTip(_openingFl, "Mandatory / Forced Leave LEFT at the start of the month: the days still to be taken this year (5 days a year). " +
                                       "Empty = worked out from the leaves filed.");

            _carried.ForeColor = Theme.TextMuted;
            _carried.SetBounds(12, 120, 396, 76);   // This is room for 5 lines, in case a line wraps on a small screen
            side.Controls.Add(_carried);

            side.Controls.Add(SideHeading("Credits earned this month", 200));
            AddBoxRow(side, 226, _earnedVl, _earnedSl);

            // This is the choice for a Vacation Leave balance that goes below zero: keep it (carried over) or make it 0.
            _zeroBalance.Text = "Balance below zero: make it 0 (the rest is without pay)";
            _zeroBalance.SetBounds(12, 254, 396, 22);
            tip.SetToolTip(_zeroBalance, "Not ticked: when the deductions are more than the Vacation Leave credits, the balance goes below zero " +
                                         "(for example -1.750) and is carried to next month. Ticked: the balance stops at 0 and the rest is without pay.");
            side.Controls.Add(_zeroBalance);

            side.Controls.Add(SideHeading("Computation of " + attendance.MonthText, 284));

            _summary.SetBounds(12, 308, 396, 126);
            _summary.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            _summary.BackgroundColor = Color.White;
            _summary.BorderStyle = BorderStyle.FixedSingle;
            _summary.AllowUserToAddRows = false;
            _summary.AllowUserToDeleteRows = false;
            _summary.AllowUserToResizeRows = false;
            _summary.AllowUserToResizeColumns = false;
            _summary.RowHeadersVisible = false;
            _summary.ColumnHeadersVisible = false;
            _summary.ReadOnly = true;
            _summary.MultiSelect = false;
            _summary.CellBorderStyle = DataGridViewCellBorderStyle.None;
            _summary.RowTemplate.Height = 19;
            _summary.DefaultCellStyle.SelectionBackColor = Color.White;       // This is so there is no blue "selected" bar
            _summary.DefaultCellStyle.SelectionForeColor = Theme.TextStrong;
            _summary.DefaultCellStyle.ForeColor = Theme.TextStrong;
            _summary.TabStop = false;

            DataGridViewColumn what = AddTextColumn(_summary, "", 100);
            what.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            DataGridViewColumn amount = AddTextColumn(_summary, "", 104);
            amount.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            side.Controls.Add(_summary);
            GridFit.Attach(this, _summary);     // This is so the rows are tall enough for the text on any screen

            _notes.Multiline = true;
            _notes.ReadOnly = true;
            _notes.ScrollBars = ScrollBars.Vertical;
            _notes.BorderStyle = BorderStyle.FixedSingle;
            _notes.TabStop = false;
            _notes.SetBounds(12, 442, 396, 70);
            _notes.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            side.Controls.Add(_notes);

            // This is the bottom: buttons ----
            _previous.Text = "< Previous";
            _previous.SetBounds(1200 - 16 - 96 - 8 - 96 - 16 - 96 - 8 - 110, 34, 110, 32);
            _previous.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _previous.Enabled = false;
            _previous.Click += (sender, e) => SaveAndMove(-1);

            _next.Text = "Next >";
            _next.SetBounds(1200 - 16 - 96 - 8 - 96 - 16 - 96, 34, 96, 32);
            _next.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _next.Enabled = false;
            _next.Click += (sender, e) => SaveAndMove(+1);

            var removeAll = new Button();
            removeAll.Text = "Remove all leave this month";
            removeAll.SetBounds(16, 34, 200, 32);
            removeAll.Click += RemoveAll_Click;

            var ctoGiven = new Button();
            ctoGiven.Text = "CTO given...";
            ctoGiven.SetBounds(224, 34, 120, 32);
            ctoGiven.Click += (sender, e) => Given_Click(LeaveType.Cto);

            var soloGiven = new Button();
            soloGiven.Text = "Solo Parent given...";
            soloGiven.SetBounds(352, 34, 150, 32);
            soloGiven.Click += (sender, e) => Given_Click(LeaveType.SoloParent);

            _message.AutoEllipsis = true;
            _message.SetBounds(16, 6, 1168, 22);
            _message.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            var save = new Button();
            save.Text = "Save";
            save.SetBounds(1200 - 16 - 96 - 8 - 96, 34, 96, 32);
            save.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            save.Click += Save_Click;
            Theme.StylePrimary(save);

            var cancel = new Button();
            cancel.Text = "Cancel";
            cancel.DialogResult = DialogResult.Cancel;
            cancel.SetBounds(1200 - 16 - 96, 34, 96, 32);
            cancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            var bottom = new Panel();
            bottom.Dock = DockStyle.Bottom;
            bottom.Size = new Size(1200, 78);
            bottom.BackColor = Theme.Panel;
            bottom.Controls.Add(_message);
            bottom.Controls.Add(removeAll);
            bottom.Controls.Add(ctoGiven);
            bottom.Controls.Add(soloGiven);
            bottom.Controls.Add(_previous);
            bottom.Controls.Add(_next);
            bottom.Controls.Add(save);
            bottom.Controls.Add(cancel);

            var sideLine = new Panel();
            sideLine.Dock = DockStyle.Right;
            sideLine.Size = new Size(1, 520);
            sideLine.BackColor = Theme.Line;

            Controls.Add(_grid);
            Controls.Add(sideLine);
            Controls.Add(side);
            Controls.Add(bottom);
            Controls.Add(top);
            CancelButton = cancel;
            ResumeLayout(false);
            PerformLayout();
            WindowState = FormWindowState.Maximized;   // This is so the window opens filling the screen (Restore gives the normal size)

            // This is where everything is filled in ----
            _filling = true;
            LedgerMonth? month = _ledger.Get(attendance.EmployeeId, attendance.Month.Year, attendance.Month.Month);
            _openingVl.Text = month != null && month.OpeningVl.HasValue ? LeaveRules.FormatShort(month.OpeningVl.Value) : "";
            _openingSl.Text = month != null && month.OpeningSl.HasValue ? LeaveRules.FormatShort(month.OpeningSl.Value) : "";
            _openingSpl.Text = month != null && month.OpeningSpl.HasValue ? LeaveRules.FormatShort(month.OpeningSpl.Value) : "";
            _openingWl.Text = month != null && month.OpeningWl.HasValue ? LeaveRules.FormatShort(month.OpeningWl.Value) : "";
            _openingFl.Text = month != null && month.OpeningFl.HasValue ? LeaveRules.FormatShort(month.OpeningFl.Value) : "";
            _zeroBalance.Checked = month != null && month.ZeroBalance;
            _earnedVl.Text = LeaveRules.FormatShort(month != null && month.EarnedVl.HasValue ? month.EarnedVl.Value : LeaveRules.MonthlyCredit);
            _earnedSl.Text = LeaveRules.FormatShort(month != null && month.EarnedSl.HasValue ? month.EarnedSl.Value : LeaveRules.MonthlyCredit);
            _filling = false;

            foreach (TextBox box in new[] { _openingVl, _openingSl, _openingSpl, _openingWl, _openingFl, _earnedVl, _earnedSl })
                box.TextChanged += Box_TextChanged;
            _zeroBalance.CheckedChanged += Box_TextChanged;

            for (int i = 0; i < attendance.Days.Count; i++)
                ShowRow(i);
            LockRestDays();
            Recompute();
        }

        /// <summary>This is the filed leaves after the user clicked Save.</summary>
        public LeaveStore GetLeaves()
        {
            return _leaves;
        }

        /// <summary>This is the remembered months (with the typed balance) after the user clicked Save.</summary>
        public LeaveLedger GetLedger()
        {
            return _ledger;
        }

        /// <summary>This is the CTO and Solo Parent Leave given (changed with "CTO given..." / "Solo Parent given...") after the user clicked Save.</summary>
        public CtoStore GetCto()
        {
            return _cto;
        }

        /// <summary>This is where the Previous / Next buttons are switched on (there is an employee above / below in the list).</summary>
        public void SetNavigation(bool previous, bool next)
        {
            _previous.Enabled = previous;
            _next.Enabled = next;
        }

        /// <summary>This is what to do after Save: -1 = go to the previous employee, +1 = the next one, 0 = stay.</summary>
        public int GetMove()
        {
            return _move;
        }

        /// <summary>This is Previous / Next: it saves (with the same checks as Save), then the main window opens the other employee.</summary>
        private void SaveAndMove(int move)
        {
            _move = move;
            Save_Click(this, EventArgs.Empty);
            if (DialogResult != DialogResult.OK)
                _move = 0;
        }

        /// <summary>This is where the user is asked before changes are thrown away (Cancel, Esc or the X of the window).</summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // This is because Cancel and Esc close with CloseReason.None, the X with UserClosing: ask for both.
            bool byUser = e.CloseReason == CloseReason.UserClosing || e.CloseReason == CloseReason.None;
            if (DialogResult != DialogResult.OK && _changed && byUser &&
                MessageBox.Show(this, "You changed the leave of this employee.\n\nClose without saving?",
                                "Edit Leave", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            {
                e.Cancel = true;
                DialogResult = DialogResult.None;
            }
            base.OnFormClosing(e);
        }

        private static string FromText(TimeSpan time)
        {
            return FromPrefix + DtrSettings.FormatTime(time);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _boldFont.Dispose();
            base.Dispose(disposing);
        }

        // This is building the window ================================================================

        private static DataGridViewColumn AddTextColumn(DataGridView grid, string heading, int width)
        {
            var column = new DataGridViewTextBoxColumn();
            column.HeaderText = heading;
            column.Width = width;
            column.ReadOnly = true;
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
            grid.Columns.Add(column);
            return column;
        }

        /// <summary>This is a column where each box is a list to choose from.</summary>
        private DataGridViewComboBoxColumn AddListColumn(string heading, int width)
        {
            var column = new DataGridViewComboBoxColumn();
            column.HeaderText = heading;
            column.Width = width;
            column.DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton;
            column.FlatStyle = FlatStyle.Flat;
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
            _grid.Columns.Add(column);
            return column;
        }

        /// <summary>This is a column of tick boxes (WFH, IDLAR).</summary>
        private void AddCheckColumn(string heading, int width)
        {
            var column = new DataGridViewCheckBoxColumn();
            column.HeaderText = heading;
            column.Width = width;
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
            column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _grid.Columns.Add(column);
        }

        private static Label SideHeading(string text, int y)
        {
            var label = new Label();
            label.Text = text;
            label.Font = new Font("Segoe UI", 9.75f, FontStyle.Bold);
            label.ForeColor = Theme.Navy;
            label.AutoEllipsis = true;
            label.SetBounds(12, y, 396, 22);
            return label;
        }

        /// <summary>This is one line with two boxes:  Vacation Leave [    ]   Sick Leave [    ]  (or other names given).</summary>
        private static void AddBoxRow(Panel side, int y, TextBox vacation, TextBox sick,
                                      string firstName = "Vacation Leave", string secondName = "Sick Leave")
        {
            var vacationLabel = new Label();
            vacationLabel.Text = firstName;
            vacationLabel.TextAlign = ContentAlignment.MiddleLeft;
            vacationLabel.SetBounds(12, y, 92, 23);

            vacation.SetBounds(106, y, 78, 23);
            vacation.TextAlign = HorizontalAlignment.Right;
            vacation.MaxLength = 9;

            var sickLabel = new Label();
            sickLabel.Text = secondName;
            sickLabel.TextAlign = ContentAlignment.MiddleLeft;
            sickLabel.SetBounds(190, y, 92, 23);

            sick.SetBounds(284, y, 78, 23);
            sick.TextAlign = HorizontalAlignment.Right;
            sick.MaxLength = 9;

            side.Controls.Add(vacationLabel);
            side.Controls.Add(vacation);
            side.Controls.Add(sickLabel);
            side.Controls.Add(sick);
        }

        /// <summary>This is one line with one box, centred below the two boxes of the lines above:   Force/Mandatory [    ]</summary>
        private static void AddCenteredBox(Panel side, int y, TextBox box, string name)
        {
            // This is the middle of the two pairs above (from the first label at 12 to the end of the second box at 362).
            const int middle = (12 + 362) / 2, labelWidth = 116, gap = 4, boxWidth = 78;
            int left = middle - (labelWidth + gap + boxWidth) / 2;

            var label = new Label();
            label.Text = name;
            label.TextAlign = ContentAlignment.MiddleRight;
            label.SetBounds(left, y, labelWidth, 23);

            box.SetBounds(left + labelWidth + gap, y, boxWidth, 23);
            box.TextAlign = HorizontalAlignment.Right;
            box.MaxLength = 9;

            side.Controls.Add(label);
            side.Controls.Add(box);
        }

        // This is the table of days ================================================================

        private LeaveEntry? LeaveOf(int i)
        {
            return _leaves.Get(_attendance.EmployeeId, _attendance.Days[i].Date);
        }

        /// <summary>This is what the DTR shows for the day when no leave is filed.</summary>
        private string DtrText(DtrDay day)
        {
            if (day.HasTimes)
            {
                var times = new[] { day.AmArrival, day.AmDeparture, day.PmArrival, day.PmDeparture }
                    .Where(t => t.HasValue).ToList();
                string first = DtrSheet.FormatTime(times[0]), last = DtrSheet.FormatTime(times[times.Count - 1]);
                return times.Count == 1 ? first : first + " - " + last;
            }

            if (day.Remark == "Sat") return "Saturday";
            if (day.Remark == "Sun") return "Sunday";
            if (!_schedule.IsWorkDay(day.Date.DayOfWeek)) return "Not a working day";
            if (day.Note.Length > 0) return day.Note.ToUpperInvariant();
            string? holiday = _leaves.Holidays == null ? null : _leaves.Holidays.GetName(day.Date);
            if (holiday != null) return "HOLIDAY (" + holiday + ")";
            if (day.IsAbsent) return "ABSENT";
            return "";
        }

        /// <summary>This is where one day is written into its row.</summary>
        private void ShowRow(int i)
        {
            DtrDay day = _attendance.Days[i];
            DataGridViewRow row = _grid.Rows[i];
            LeaveEntry? leave = LeaveOf(i);
            bool restDay = !_schedule.IsWorkDay(day.Date.DayOfWeek);
            Color back = restDay ? Theme.Panel : Color.White;

            _filling = true;
            row.Cells[ColDay].Value = day.Day + "   " + day.Date.DayOfWeek.ToString().Substring(0, 3);
            row.Cells[ColDtr].Value = DtrText(day);
            row.Cells[ColLeave].Value = leave == null || leave.Type == LeaveType.WorkFromHome ? NoLeave : leave.Kind.Name;
            row.Cells[ColPart].Value = leave != null && leave.From.HasValue ? FromText(leave.From.Value)
                                     : PartTexts[leave == null ? 0 : (int)leave.Part];
            row.Cells[ColWfh].Value = leave != null && leave.Wfh;
            row.Cells[ColIdlar].Value = leave != null && leave.Idlar;
            _filling = false;

            // This is the locking rule: "How long" only matters when something is filed. WFH only goes with no leave, Holiday
            // or Suspended, on a working day; IDLAR only after WFH. (The box that is being changed
            // is never locked here: locking only follows from changing another box of the row.)
            bool wfhAllowed = !restDay && (leave == null || LeaveKinds.IsNotALeave(leave.Type));
            bool idlarAllowed = leave != null && leave.Wfh;
            row.Cells[ColPart].ReadOnly = leave == null;
            row.Cells[ColWfh].ReadOnly = !wfhAllowed && !(leave != null && leave.Wfh);
            row.Cells[ColIdlar].ReadOnly = !idlarAllowed && !(leave != null && leave.Idlar);

            foreach (DataGridViewCell cell in row.Cells)
            {
                bool marked = leave != null && (cell.ColumnIndex == ColLeave || cell.ColumnIndex == ColPart ||
                              (cell.ColumnIndex == ColWfh && leave.Wfh) || (cell.ColumnIndex == ColIdlar && leave.Idlar));
                bool locked = (cell.ColumnIndex == ColWfh || cell.ColumnIndex == ColIdlar) && cell.ReadOnly;
                cell.Style.BackColor = marked ? Theme.Highlight : locked ? Theme.Panel : back;
                cell.Style.Font = marked ? _boldFont : null;
                cell.Style.ForeColor = cell.ColumnIndex == ColDay || marked ? Theme.TextStrong : Theme.TextMuted;
            }

            if (_grid.CurrentCell != null && _grid.CurrentCell.RowIndex == i)
                ShowWfhHeading();
        }

        /// <summary>This is the WFH heading: it reads "Is it WFH?" while the chosen day is a Suspended day.</summary>
        private void ShowWfhHeading()
        {
            bool suspended = false;
            if (_grid.CurrentCell != null && _grid.CurrentCell.RowIndex >= 0 && _grid.CurrentCell.RowIndex < _attendance.Days.Count)
            {
                LeaveEntry? leave = LeaveOf(_grid.CurrentCell.RowIndex);
                suspended = leave != null && leave.Type == LeaveType.Suspended;
            }
            string heading = suspended ? SuspendedWfhHeading : WfhHeading;
            if (_grid.Columns[ColWfh].HeaderText != heading)
                _grid.Columns[ColWfh].HeaderText = heading;
        }

        /// <summary>This is the WFH heading as it is now (for the self test).</summary>
        internal string GetWfhHeading()
        {
            return _grid.Columns[ColWfh].HeaderText;
        }

        /// <summary>
        /// This is because a leave cannot be filed on a day that is not a working day. (A leave that is already
        /// there can still be removed.) This is decided when the window opens and after
        /// "Remove all" - never while a box is being changed, because locking a box in the
        /// middle of a change confuses the table.
        /// </summary>
        private void LockRestDays()
        {
            for (int i = 0; i < _attendance.Days.Count; i++)
            {
                bool restDay = !_schedule.IsWorkDay(_attendance.Days[i].Date.DayOfWeek);
                _grid.Rows[i].Cells[ColLeave].ReadOnly = restDay && LeaveOf(i) == null;
            }
        }

        /// <summary>This is what commits a choice as soon as it is picked from the list (not only when leaving the box).</summary>
        private void Grid_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
        {
            if (_grid.IsCurrentCellDirty)
                _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        /// <summary>This is when the user picked something in "Leave" or "How long".</summary>
        private void Grid_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (_filling || e.RowIndex < 0 ||
                (e.ColumnIndex != ColLeave && e.ColumnIndex != ColPart && e.ColumnIndex != ColWfh && e.ColumnIndex != ColIdlar))
                return;

            int i = e.RowIndex;
            DataGridViewRow row = _grid.Rows[i];
            string leaveText = Convert.ToString(row.Cells[ColLeave].Value) ?? "";
            string partText = Convert.ToString(row.Cells[ColPart].Value) ?? "";
            bool wfh = IsTicked(row.Cells[ColWfh].Value);
            bool idlar = wfh && IsTicked(row.Cells[ColIdlar].Value);

            LeaveKind? kind = LeaveKinds.All.FirstOrDefault(k => k.Name == leaveText && k.Type != LeaveType.WorkFromHome);
            int part = Math.Max(0, Array.IndexOf(PartTexts, partText));

            // This is "From 3:00 PM" = Suspended from that time (for any other leave it means a whole day).
            TimeSpan? from = null;
            DateTime parsed;
            if (partText.StartsWith(FromPrefix, StringComparison.Ordinal) &&
                DateTime.TryParseExact(partText.Substring(FromPrefix.Length), "h:mm tt", CultureInfo.InvariantCulture,
                                       DateTimeStyles.None, out parsed))
                from = parsed.TimeOfDay;

            // This is "(no leave)" with WFH ticked = an ordinary working day worked from home.
            LeaveEntry? entry = null;
            if (kind != null) entry = new LeaveEntry(kind.Type, (LeavePart)part, wfh, idlar);
            else if (wfh) entry = new LeaveEntry(LeaveType.WorkFromHome, (LeavePart)part, true, idlar);
            bool fromIgnored = from.HasValue && (entry == null || entry.Type != LeaveType.Suspended);
            if (entry != null && from.HasValue && !fromIgnored)
            {
                entry.From = from;
                entry.Wfh = false;      // This is because WFH / IDLAR do not go with a suspension from a time
                entry.Idlar = false;
            }

            // This is the balance check: CTO and Solo Parent Leave can only be filed when enough was given.
            // Otherwise the choice is refused and the row goes back to what it was.
            if (entry != null && !CtoCalculator.CanFile(_cto, _leaves, _attendance.EmployeeId, _attendance.Days[i].Date, entry))
            {
                Say(NotEnoughText(i, entry), true);
                if (IsHandleCreated)
                    BeginInvoke(new Action(() => PutRowBack(i)));
                else
                    PutRowBack(i);
                return;
            }

            _leaves.Set(_attendance.EmployeeId, _attendance.Days[i].Date, entry);
            _changed = true;

            if (fromIgnored && entry != null)
                Say("\"From a time\" is only for Suspended (work suspended from that time). This day is filed as a whole day.", true);
            else
                Say("", false);
            ShowRow(i);
            Recompute();
        }

        /// <summary>This is the row shown again from what is filed (after a choice was refused).</summary>
        private void PutRowBack(int i)
        {
            ShowRow(i);
            if (_grid.IsCurrentCellInEditMode && _grid.CurrentCell != null && _grid.CurrentCell.RowIndex == i)
                _grid.RefreshEdit();
        }

        /// <summary>This is the message when there is not enough CTO / Solo Parent Leave for the day picked.</summary>
        private string NotEnoughText(int i, LeaveEntry entry)
        {
            DateTime date = _attendance.Days[i].Date;
            bool solo = entry.Type == LeaveType.SoloParent;
            string name = solo ? "Solo Parent Leave" : "CTO";
            string button = solo ? "\"Solo Parent given...\"" : "\"CTO given...\"";
            decimal left = CtoCalculator.LeftOn(_cto, _leaves, _attendance.EmployeeId, date, entry.Type);
            string day = "Day " + date.Day + ": ";

            if (left <= 0m)
                return day + "there is no " + name + " left on " + date.ToString("MMM d", CultureInfo.InvariantCulture) +
                       ", so it cannot be filed. Add the " + name + " given first with " + button + ".";
            if (left < entry.Days)
                return day + "only " + LeaveRules.FormatDays(left) + " of " + name + " is left on " +
                       date.ToString("MMM d", CultureInfo.InvariantCulture) + ", so a whole day cannot be filed. Choose half a day.";
            return day + name + " here would take what a later day already filed needs, so it cannot be filed. " +
                   "Add more with " + button + ", or remove the later day first.";
        }

        private static bool IsTicked(object? value)
        {
            return value is bool && (bool)value;
        }

        /// <summary>This is where the table of CTO (or Solo Parent Leave) given to this employee is opened.</summary>
        private void Given_Click(LeaveType type)
        {
            _grid.EndEdit();
            string id = _attendance.EmployeeId;
            string who = _attendance.EmployeeName.Length > 0 ? _attendance.EmployeeName : "ID " + id;

            using (var dialog = new CtoForm(who, _cto.Get(id, type), _leaves.GetAllOfKind(id, type), type))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;
                _cto.Set(id, type, dialog.GetGrants());
                _changed = true;
            }

            Recompute();
            Say("The " + (type == LeaveType.SoloParent ? "Solo Parent Leave" : "CTO") + " given was changed. Click Save to keep it.", false);
        }

        private void RemoveAll_Click(object? sender, EventArgs e)
        {
            _grid.EndEdit();
            _changed = true;
            for (int i = 0; i < _attendance.Days.Count; i++)
            {
                _leaves.Set(_attendance.EmployeeId, _attendance.Days[i].Date, null);
                ShowRow(i);
            }
            LockRestDays();
            Recompute();
            Say("Every leave of this month was removed. Click Save to keep it that way.", false);
        }

        // This is the balance boxes ================================================================

        /// <summary>This is where the seven boxes are read. Returns the name of the first box that is not a number, or "".</summary>
        private string ReadBoxes()
        {
            string problem = "";
            LedgerMonth month = _ledger.GetOrAdd(_attendance.EmployeeId, _attendance.Month.Year, _attendance.Month.Month);

            decimal? value;
            if (Check(_openingVl, out value)) month.OpeningVl = value;
            else problem = "the Vacation Leave balance";

            if (Check(_openingSl, out value)) month.OpeningSl = value;
            else if (problem.Length == 0) problem = "the Sick Leave balance";

            // This is the SPL and WL left: a number from 0 up to the days a year (3 and 5), or empty.
            if (CheckYearly(_openingSpl, LeaveType.SpecialPrivilege, out value)) month.OpeningSpl = value;
            else if (problem.Length == 0) problem = "the Special Privilege Leave left";

            if (CheckYearly(_openingWl, LeaveType.Wellness, out value)) month.OpeningWl = value;
            else if (problem.Length == 0) problem = "the Wellness Leave left";

            // This is the Mandatory / Forced Leave left: a number from 0 up to 5, or empty.
            if (CheckYearly(_openingFl, LeaveType.Forced, out value)) month.OpeningFl = value;
            else if (problem.Length == 0) problem = "the Forced Leave left";

            month.ZeroBalance = _zeroBalance.Checked;

            // This is the rule: an empty box, or the usual 1.25, means "nothing special typed".
            if (Check(_earnedVl, out value)) month.EarnedVl = value == LeaveRules.MonthlyCredit ? null : value;
            else if (problem.Length == 0) problem = "the Vacation Leave credits earned";

            if (Check(_earnedSl, out value)) month.EarnedSl = value == LeaveRules.MonthlyCredit ? null : value;
            else if (problem.Length == 0) problem = "the Sick Leave credits earned";

            return problem;
        }

        /// <summary>This is where one box is read, and turned pale red when it does not hold a number.</summary>
        private static bool Check(TextBox box, out decimal? value)
        {
            bool ok = LeaveRules.TryParseCredits(box.Text, out value);
            box.BackColor = ok ? Color.White : Color.FromArgb(255, 220, 220);
            return ok;
        }

        /// <summary>This is what to tell about a box that is not right, e.g. "Please type a number for the Sick Leave balance, for example 12.5".</summary>
        private static string ProblemText(string problem)
        {
            if (problem == "the Special Privilege Leave left")
                return "Please type how many days of Special Privilege Leave are left: a number from 0 to " +
                       LeaveKinds.Get(LeaveType.SpecialPrivilege).YearlyLimit + ", for example 2 or 1.5";
            if (problem == "the Wellness Leave left")
                return "Please type how many days of Wellness Leave are left: a number from 0 to " +
                       LeaveKinds.Get(LeaveType.Wellness).YearlyLimit + ", for example 4 or 2.5";
            if (problem == "the Forced Leave left")
                return "Please type how many days of Mandatory / Forced Leave are left: a number from 0 to " +
                       LeaveKinds.Get(LeaveType.Forced).YearlyLimit + ", for example 3 or 2.5";
            return "Please type a number for " + problem + ", for example 12.5";
        }

        /// <summary>This is one SPL / WL / Forced Leave box: empty, or a number from 0 up to the days a year of that leave (pale red otherwise).</summary>
        private static bool CheckYearly(TextBox box, LeaveType type, out decimal? value)
        {
            bool ok = LeaveRules.TryParseCredits(box.Text, out value) &&
                      (!value.HasValue || (value.Value >= 0 && value.Value <= LeaveKinds.Get(type).YearlyLimit));
            box.BackColor = ok ? Color.White : Color.FromArgb(255, 220, 220);
            if (!ok) value = null;
            return ok;
        }

        private void Box_TextChanged(object? sender, EventArgs e)
        {
            if (_filling) return;
            _changed = true;

            string problem = ReadBoxes();
            if (problem.Length > 0)
                Say(ProblemText(problem), true);
            else
                Say("", false);
            Recompute();
        }

        // This is the computation on the right ================================================================

        /// <summary>This is where everything is worked out again and shown.</summary>
        private void Recompute()
        {
            LeaveCard card = _compute(_leaves, _ledger, _cto);
            LeaveMonth month = card.Current;

            // This is the "Deducted" column of the table ----
            for (int i = 0; i < _attendance.Days.Count; i++)
            {
                LeaveEntry? leave = LeaveOf(i);
                string text = "";
                if (leave != null)
                {
                    LeaveKind kind = leave.Kind;
                    if (kind.Type == LeaveType.OfficialBusiness)
                        text = "not deducted - present";
                    else if (leave.From.HasValue)
                        text = "no undertime after " + DtrSettings.FormatTime(leave.From.Value);
                    else if (LeaveKinds.IsNotALeave(kind.Type))
                        text = (kind.Type == LeaveType.WorkFromHome ? "" : "not deducted") +
                               (!leave.Wfh ? "" : (kind.Type == LeaveType.WorkFromHome ? "" : ", ") +
                                                  (leave.Idlar ? "WFH - present" : "WFH - no IDLAR yet"));
                    else if (LeaveKinds.IsGiven(kind.Type))
                        text = "- " + LeaveRules.FormatShort(leave.Days) + "  " + kind.Code;
                    else if (kind.Charge == LeaveCharge.NotCharged)
                        text = "not deducted";
                    else
                        text = "- " + LeaveRules.Format(leave.Days * month.DayCharge) + "  " +
                               (kind.Charge == LeaveCharge.Sick ? "SL" : "VL");
                }
                _grid.Rows[i].Cells[ColCharge].Value = text;
            }

            // This is where the starting balance comes from ----
            _carried.Text =
                "Vacation Leave: " + StartText(month.VlStart, month.VlStartTyped) + "      " +
                "Sick Leave: " + StartText(month.SlStart, month.SlStartTyped) + "\r\n" +
                "Special left: " + YearlyStartText(card.SplUsed - month.SplDays, LeaveType.SpecialPrivilege, card.SplStartTyped) + "    " +
                "Wellness left: " + YearlyStartText(card.WellnessUsed - month.WellnessDays, LeaveType.Wellness, card.WellnessStartTyped) + "\r\n" +
                "Force/Mandatory left: " + YearlyStartText(card.ForcedUsed - month.ForcedDays, LeaveType.Forced, card.ForcedStartTyped) + "\r\n" +
                (month.VlStart.HasValue && month.SlStart.HasValue
                    ? "Type a number above only if the balance has to be corrected."
                    : "Type it once, from the employee's leave card.");

            // This is the computation, line by line ----
            _summary.Rows.Clear();
            foreach (LeaveLine line in card.Breakdown)
            {
                bool heading = line.Kind == LeaveLineKind.Heading;
                int index = _summary.Rows.Add((heading ? "" : "   ") + line.Text, line.Value);
                DataGridViewRow row = _summary.Rows[index];
                row.Cells[0].ToolTipText = line.Text;

                if (heading || line.Kind == LeaveLineKind.Total)
                {
                    row.DefaultCellStyle.Font = _boldFont;
                    if (heading)
                    {
                        row.DefaultCellStyle.ForeColor = Theme.Navy;
                        row.DefaultCellStyle.SelectionForeColor = Theme.Navy;
                    }
                }
            }
            _summary.ClearSelection();

            // This is the things to check ----
            _notes.Text = string.Join("\r\n\r\n", card.Notes.Select(n => "- " + n).ToArray());
            _notes.BackColor = card.Notes.Count > 0 ? Theme.NoticeBack : Theme.Panel;
        }

        /// <summary>This is "2 (typed)" or "3 (from the leaves filed)": the SPL / WL left at the start of the month.</summary>
        private static string YearlyStartText(decimal usedBefore, LeaveType type, decimal? typed)
        {
            if (typed.HasValue) return LeaveRules.FormatShort(typed.Value) + " (typed)";
            return LeaveRules.FormatShort(LeaveKinds.Get(type).YearlyLimit - usedBefore) + " (from leaves filed)";
        }

        private static string StartText(decimal? start, bool typed)
        {
            if (!start.HasValue) return "not known yet";
            return LeaveRules.Format(start.Value) + (typed ? " (typed)" : " (carried over)");
        }

        private void Say(string text, bool problem)
        {
            _message.Text = text;
            _message.ForeColor = problem ? Theme.Red : Theme.TextMuted;
        }

        private void Save_Click(object? sender, EventArgs e)
        {
            _grid.EndEdit();

            string problem = ReadBoxes();
            if (problem.Length > 0)
            {
                MessageBox.Show(this,
                    ProblemText(problem) + "\n\n" +
                    "Leave the box empty if you do not know it yet.",
                    "Edit Leave", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // This is worked from home but no IDLAR yet (not for a Holiday, which needs neither): ask once.
            List<string> waiting = new List<string>();
            for (int i = 0; i < _attendance.Days.Count; i++)
            {
                LeaveEntry? leave = LeaveOf(i);
                if (leave != null && leave.WaitsForIdlar && leave.Type != LeaveType.Holiday && !leave.From.HasValue)
                    waiting.Add(_attendance.Days[i].Day.ToString(CultureInfo.InvariantCulture));
            }
            if (waiting.Count > 0 &&
                MessageBox.Show(this,
                    "WFH is ticked but IDLAR is not, on " + (waiting.Count == 1 ? "day " : "days ") +
                    string.Join(", ", waiting.ToArray()) + ".\n\n" +
                    "Until IDLAR is ticked, " + (waiting.Count == 1 ? "that day is" : "those days are") +
                    " not counted as a day at work. Save anyway?",
                    "Edit Leave", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            DialogResult = DialogResult.OK;
        }
    }
}
