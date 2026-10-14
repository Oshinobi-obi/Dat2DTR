namespace Dat2DTR
{
    /// <summary>
    /// The "Edit DTR" window: one row per day, where a person can type in a
    /// missing time, correct a wrong one, or add a remark such as HOLIDAY.
    /// The Late and Undertime of a day can also be typed in (for example when work was
    /// suspended at 3:00 PM); a reason is then required, and the computed value stays visible.
    /// (A leave is filed in the "Edit Leave" window instead, so that it is deducted.)
    ///
    /// Nothing is changed in the .dat file. What is typed here is kept separately
    /// (see CorrectionStore) and replaces the scans of that day when the DTR is made.
    /// </summary>
    internal sealed class EditTimesForm : Form
    {
        // Column numbers of the table.
        private const int ColDay = 0, ColFirstTime = 1, ColLastTime = 4, ColRemark = 5,
                          ColLateHours = 6, ColLateMinutes = 7, ColUndertimeHours = 8, ColUndertimeMinutes = 9,
                          ColReason = 10, ColResult = 11;

        private readonly DtrSheet _fromFile;        // the DTR made from the scans only
        private readonly WorkSchedule _schedule;
        private readonly DtrSettings _settings;
        private readonly LeaveStore _leaves;        // filed leaves: a day on leave has no late or undertime
        private readonly DateTime _firstCovered, _lastCovered;

        private readonly DayCorrection[] _original; // per day: what the scans say
        private readonly DayCorrection[] _now;      // per day: what the table shows now
        private readonly DayAdjustment[] _adjusted; // per day: Late / Undertime typed in by hand (empty = computed)
        private readonly DayCorrection[] _startNow;       // what the table showed when it opened
        private readonly DayAdjustment[] _startAdjusted;  //   (to know whether something was changed)

        /// <summary>-1 / +1 when the user clicked Previous / Next (after saving), 0 otherwise.</summary>
        private int _move;
        private readonly Button _previous = new Button(), _next = new Button();

        private readonly DataGridView _grid = new DataGridView();
        private readonly Label _message = new Label();
        private readonly Label _totals = new Label();
        private readonly Font _boldFont;

        public EditTimesForm(DtrSheet fromFile, CorrectionStore saved, LeaveStore leaves, WorkSchedule schedule,
                             DtrSettings settings, DateTime firstCovered, DateTime lastCovered)
        {
            _fromFile = fromFile;
            _leaves = leaves;
            _schedule = schedule;
            _settings = settings;
            _firstCovered = firstCovered;
            _lastCovered = lastCovered;

            // Start from what was typed in before; days without that start from the scans.
            int days = fromFile.Days.Count;
            _original = new DayCorrection[days];
            _now = new DayCorrection[days];
            _adjusted = new DayAdjustment[days];
            _startNow = new DayCorrection[days];
            _startAdjusted = new DayAdjustment[days];
            for (int i = 0; i < days; i++)
            {
                DtrDay day = fromFile.Days[i];
                _original[i] = new DayCorrection
                {
                    AmArrival = day.AmArrival,
                    AmDeparture = day.AmDeparture,
                    PmArrival = day.PmArrival,
                    PmDeparture = day.PmDeparture
                };

                DayCorrection? typed = saved.Get(fromFile.EmployeeId, day.Date);
                _now[i] = typed != null ? typed.Clone() : _original[i].Clone();

                DayAdjustment? adjustment = saved.GetAdjustment(fromFile.EmployeeId, day.Date);
                _adjusted[i] = adjustment != null ? adjustment.Clone() : new DayAdjustment();
                _startNow[i] = _now[i].Clone();
                _startAdjusted[i] = _adjusted[i].Clone();
            }

            // Same order as a designer-made form, so it scales correctly on 125% / 150% screens.
            SuspendLayout();
            Text = "Edit DTR";
            Icon = Theme.GetAppIcon();
            Font = new Font("Segoe UI", 9f);
            _boldFont = new Font(Font, FontStyle.Bold);
            AutoScaleDimensions = new SizeF(7f, 15f);
            AutoScaleMode = AutoScaleMode.Font;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(1260, 660);
            MinimumSize = new Size(980, 460);

            // ---- top: who and how
            var title = new Label();
            title.Text = (fromFile.EmployeeName.Length > 0 ? fromFile.EmployeeName : "ID " + fromFile.EmployeeId) +
                         "   -   " + fromFile.MonthText;
            title.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            title.ForeColor = Theme.Navy;
            title.AutoEllipsis = true;
            title.SetBounds(16, 10, 1228, 26);
            title.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            var how = new Label();
            how.Text = "Click a box and type the time, for example  12:05 PM.  Leave it empty for \"no time\".  A remark such as HOLIDAY is printed " +
                       "on a day with no times. For a leave, use \"Edit Leave...\".\r\n" +
                       "Late / Undertime: type the Hours and Minutes, like on the DTR, and a Reason (for example 0 and 0 when work was suspended at 3:00 PM). " +
                       "Empty both boxes to use the computed value.\r\n" +
                       schedule.Description;
            how.ForeColor = Theme.TextMuted;
            how.SetBounds(16, 38, 1228, 54);
            how.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            var top = new Panel();
            top.Dock = DockStyle.Top;
            top.Size = new Size(1260, 98);
            top.BackColor = Color.White;
            top.Controls.Add(title);
            top.Controls.Add(how);
            GridFit.GrowWithText(this, how, top);   // the instructions are never cut off

            // ---- middle: the table
            _grid.Dock = DockStyle.Fill;
            _grid.BackgroundColor = Color.White;
            _grid.BorderStyle = BorderStyle.None;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.AllowUserToResizeRows = false;
            _grid.RowHeadersVisible = false;
            _grid.MultiSelect = false;
            _grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            _grid.RowTemplate.Height = 25;

            AddColumn("Day", 80, true);
            AddColumn("A.M. Arrival", 98, false);
            AddColumn("A.M. Departure", 98, false);
            AddColumn("P.M. Arrival", 98, false);
            AddColumn("P.M. Departure", 98, false);
            AddColumn("Remark", 120, false).Tag = GridFit.FreeText;
            AddNumberColumn("LATE\r\nHours", 56);
            AddNumberColumn("LATE\r\nMinutes", 62);
            AddNumberColumn("UNDERTIME\r\nHours", 78);
            AddNumberColumn("UNDERTIME\r\nMinutes", 78);
            AddColumn("Reason (if Late / Undertime typed)", 180, false).Tag = GridFit.FreeText;
            DataGridViewColumn result = AddColumn("Result", 100, true);
            result.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            // A.M. Arrival to UNDERTIME Minutes: heading and values centred.
            // Day, Reason and Result: only the heading centred (the values stay on the left).
            _grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            foreach (DataGridViewColumn column in _grid.Columns)
            {
                column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                if (column.Index >= ColFirstTime && column.Index <= ColUndertimeMinutes)
                    column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            // Headings as tall as needed and columns wide enough on any screen (125%, 150%...).
            GridFit.Attach(this, _grid);

            for (int i = 0; i < days; i++)
                _grid.Rows.Add();

            _grid.CellEndEdit += Grid_CellEndEdit;

            // ---- bottom: buttons
            var undoDay = new Button();
            undoDay.Text = "Undo this day";
            undoDay.SetBounds(16, 34, 118, 32);
            undoDay.Click += (sender, e) =>
            {
                if (_grid.CurrentCell == null) return;
                int i = _grid.CurrentCell.RowIndex;
                _grid.CancelEdit();
                _now[i] = _original[i].Clone();
                _adjusted[i] = new DayAdjustment();
                ShowRow(i);
                ShowTotals();
                Say("Day " + (i + 1) + " is back to what the biometrics file says.", false);
            };

            var undoAll = new Button();
            undoAll.Text = "Undo all";
            undoAll.SetBounds(140, 34, 96, 32);
            undoAll.Click += (sender, e) =>
            {
                _grid.CancelEdit();
                for (int i = 0; i < _now.Length; i++)
                {
                    _now[i] = _original[i].Clone();
                    _adjusted[i] = new DayAdjustment();
                    ShowRow(i);
                }
                ShowTotals();
                Say("Every day is back to what the biometrics file says.", false);
            };

            _message.AutoEllipsis = true;
            _message.SetBounds(16, 6, 1228, 22);
            _message.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            _totals.TextAlign = ContentAlignment.MiddleLeft;
            _totals.ForeColor = Theme.Navy;
            _totals.Font = _boldFont;
            _totals.SetBounds(250, 34, 1260 - 250 - 16 - 96 - 8 - 96 - 16 - 96 - 8 - 110 - 8, 32);
            _totals.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            _previous.Text = "< Previous";
            _previous.SetBounds(1260 - 16 - 96 - 8 - 96 - 16 - 96 - 8 - 110, 34, 110, 32);
            _previous.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _previous.Enabled = false;
            _previous.Click += (sender, e) => SaveAndMove(-1);

            _next.Text = "Next >";
            _next.SetBounds(1260 - 16 - 96 - 8 - 96 - 16 - 96, 34, 96, 32);
            _next.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _next.Enabled = false;
            _next.Click += (sender, e) => SaveAndMove(+1);

            var save = new Button();
            save.Text = "Save";
            save.SetBounds(1260 - 16 - 96 - 8 - 96, 34, 96, 32);
            save.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            save.Click += Save_Click;
            Theme.StylePrimary(save);

            var cancel = new Button();
            cancel.Text = "Cancel";
            cancel.DialogResult = DialogResult.Cancel;
            cancel.SetBounds(1260 - 16 - 96, 34, 96, 32);
            cancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            var bottom = new Panel();
            bottom.Dock = DockStyle.Bottom;
            bottom.Size = new Size(1260, 78);
            bottom.BackColor = Theme.Panel;
            bottom.Controls.Add(_message);
            bottom.Controls.Add(undoDay);
            bottom.Controls.Add(undoAll);
            bottom.Controls.Add(_totals);
            bottom.Controls.Add(_previous);
            bottom.Controls.Add(_next);
            bottom.Controls.Add(save);
            bottom.Controls.Add(cancel);

            Controls.Add(_grid);
            Controls.Add(bottom);
            Controls.Add(top);
            CancelButton = cancel;
            ResumeLayout(false);
            PerformLayout();

            for (int i = 0; i < days; i++)
                ShowRow(i);
            ShowTotals();
        }

        /// <summary>
        /// What to store for each day, after the user clicked Save:
        /// the typed values, or null when the day is the same as the scans (nothing to store).
        /// </summary>
        public Dictionary<DateTime, DayCorrection?> GetChanges()
        {
            var changes = new Dictionary<DateTime, DayCorrection?>();
            for (int i = 0; i < _now.Length; i++)
                changes[_fromFile.Days[i].Date] = IsChanged(i) ? _now[i].Clone() : null;
            return changes;
        }

        /// <summary>
        /// The Late / Undertime typed in for each day, after the user clicked Save
        /// (null when the day uses the computed values).
        /// </summary>
        public Dictionary<DateTime, DayAdjustment?> GetAdjustments()
        {
            var changes = new Dictionary<DateTime, DayAdjustment?>();
            for (int i = 0; i < _adjusted.Length; i++)
                changes[_fromFile.Days[i].Date] = _adjusted[i].IsEmpty ? null : _adjusted[i].Clone();
            return changes;
        }

        /// <summary>Switches the Previous / Next buttons on (there is an employee above / below in the list).</summary>
        public void SetNavigation(bool previous, bool next)
        {
            _previous.Enabled = previous;
            _next.Enabled = next;
        }

        /// <summary>After Save: -1 = go to the previous employee, +1 = the next one, 0 = stay.</summary>
        public int GetMove()
        {
            return _move;
        }

        /// <summary>Previous / Next: saves (with the same checks as Save), then the main window opens the other employee.</summary>
        private void SaveAndMove(int move)
        {
            _move = move;
            Save_Click(this, EventArgs.Empty);
            if (DialogResult != DialogResult.OK)
                _move = 0;
        }

        /// <summary>True when the table is different from what it showed when the window opened.</summary>
        internal bool HasChanges()
        {
            _grid.EndEdit();
            for (int i = 0; i < _now.Length; i++)
            {
                if (!_now[i].SameAs(_startNow[i])) return true;
                DayAdjustment a = _adjusted[i], b = _startAdjusted[i];
                if (a.LateMinutes != b.LateMinutes || a.UndertimeMinutes != b.UndertimeMinutes || a.Reason.Trim() != b.Reason.Trim())
                    return true;
            }
            return false;
        }

        /// <summary>Asks before throwing away changes (Cancel, Esc or the X of the window).</summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Cancel and Esc close with CloseReason.None, the X with UserClosing: ask for both.
            bool byUser = e.CloseReason == CloseReason.UserClosing || e.CloseReason == CloseReason.None;
            if (DialogResult != DialogResult.OK && byUser && HasChanges() &&
                MessageBox.Show(this, "You changed this DTR.\n\nClose without saving?",
                                "Edit DTR", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            {
                e.Cancel = true;
                DialogResult = DialogResult.None;
            }
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _boldFont.Dispose();
            base.Dispose(disposing);
        }

        // ================================================================ the table

        private DataGridViewColumn AddColumn(string heading, int width, bool readOnly)
        {
            var column = new DataGridViewTextBoxColumn();
            column.HeaderText = heading;
            column.Width = width;
            column.ReadOnly = readOnly;
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
            if (readOnly)
                column.DefaultCellStyle.ForeColor = Theme.TextMuted;
            _grid.Columns.Add(column);
            return column;
        }

        /// <summary>A small centred column for the Hours or Minutes of Late / Undertime.</summary>
        private void AddNumberColumn(string heading, int width)
        {
            DataGridViewColumn column = AddColumn(heading, width, false);
            column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
        }

        private bool IsChanged(int i)
        {
            return !_now[i].SameAs(_original[i]);
        }

        /// <summary>The day as the DTR will show it, with the Late / Undertime typed in by hand.</summary>
        private DtrDay Compute(int i)
        {
            DtrDay day = ComputeTimes(i);
            if (_adjusted[i].IsEmpty)
                return day;
            day = day.Clone();
            DtrBuilder.ApplyAdjustment(day, _adjusted[i]);
            return day;
        }

        /// <summary>The day from the scans, or from the typed times (Late / Undertime as computed).</summary>
        private DtrDay ComputeTimes(int i)
        {
            DtrDay fromFile = _fromFile.Days[i];
            if (!IsChanged(i))
                return fromFile;

            var day = new DtrDay();
            day.Day = fromFile.Day;
            day.Date = fromFile.Date;
            day.Remark = fromFile.Remark;
            bool covered = day.Date >= _firstCovered && day.Date <= _lastCovered;
            DtrBuilder.FillDay(day, _now[i], _schedule, _settings, covered, _leaves.GetForDtr(_fromFile.EmployeeId, day.Date, _schedule));
            return day;
        }

        /// <summary>Writes one day into its row and colours the boxes that were changed.</summary>
        private void ShowRow(int i)
        {
            DataGridViewRow row = _grid.Rows[i];
            DtrDay day = Compute(i);
            bool restDay = !_schedule.IsWorkDay(day.Date.DayOfWeek);
            Color normalBack = restDay ? Theme.Panel : Color.White;

            row.Cells[ColDay].Value = day.Day + "   " + day.Date.DayOfWeek.ToString().Substring(0, 3);
            row.Cells[ColDay].Style.BackColor = normalBack;

            TimeSpan?[] now = _now[i].Times, original = _original[i].Times;
            for (int t = 0; t < 4; t++)
            {
                DataGridViewCell cell = row.Cells[ColFirstTime + t];
                cell.Value = DtrSheet.FormatTime(now[t]);
                Mark(cell, now[t] != original[t], normalBack);
            }

            row.Cells[ColRemark].Value = _now[i].Remark;
            Mark(row.Cells[ColRemark], _now[i].Remark.Trim().Length > 0, normalBack);

            // Late and Undertime: computed, or typed in by hand (then yellow).
            DayAdjustment adjusted = _adjusted[i];
            bool lateTyped = adjusted.LateMinutes.HasValue, undertimeTyped = adjusted.UndertimeMinutes.HasValue;
            ShowHoursMinutes(row.Cells[ColLateHours], row.Cells[ColLateMinutes], day.LateMinutes, lateTyped, normalBack);
            ShowHoursMinutes(row.Cells[ColUndertimeHours], row.Cells[ColUndertimeMinutes], day.UndertimeMinutes, undertimeTyped, normalBack);
            row.Cells[ColReason].Value = adjusted.Reason;
            Mark(row.Cells[ColReason], adjusted.Reason.Trim().Length > 0, normalBack);

            // The last column says what the day comes to.
            var parts = new List<string>();
            if (day.IsAbsent) parts.Add("Absent");
            if (day.IsCalendarHoliday)
                parts.Add("Holiday (" + day.HolidayName + ")");
            else if (day.HasLeave)
                parts.Add(LeaveKinds.Get(day.Leave).Name + (day.LeavePart == LeavePart.WholeDay ? "" : " (half day)"));
            if (day.SuspendedFrom.HasValue)
                parts.Add("Work suspended from " + DtrSettings.FormatTime(day.SuspendedFrom.Value));
            if (lateTyped || undertimeTyped)
                parts.Add("Computed: late " + Duration(day.IsAdjusted ? day.ComputedLateMinutes : day.LateMinutes) +
                          ", undertime " + Duration(day.IsAdjusted ? day.ComputedUndertimeMinutes : day.UndertimeMinutes));
            if (day.Warning.Length > 0) parts.Add("Check: " + day.Warning);
            if (restDay && parts.Count == 0 && !day.HasTimes) parts.Add(day.Remark.Length > 0 ? day.Remark : "Not a working day");
            row.Cells[ColResult].Value = string.Join(",  ", parts.ToArray());
            row.Cells[ColResult].Style.BackColor = normalBack;
            row.Cells[ColResult].Style.ForeColor = day.Warning.Length > 0 ? Theme.Red : Theme.TextMuted;
        }

        /// <summary>
        /// Writes minutes as Hours and Minutes, the way the DTR prints them (empty for 0).
        /// A typed value shows both boxes, with 0 where it is zero, so the yellow boxes are never empty.
        /// </summary>
        private void ShowHoursMinutes(DataGridViewCell hoursCell, DataGridViewCell minutesCell, int minutes, bool typed, Color normalBack)
        {
            int h = minutes / 60, m = minutes % 60;
            hoursCell.Value = h > 0 || typed ? h.ToString() : "";
            minutesCell.Value = m > 0 || typed ? m.ToString() : "";
            Mark(hoursCell, typed, normalBack);
            Mark(minutesCell, typed, normalBack);
        }

        /// <summary>Reads an Hours or Minutes box: empty = null, otherwise a whole number up to the given most.</summary>
        private static bool TryReadPart(object? value, int most, out int? number)
        {
            number = null;
            string text = (Convert.ToString(value) ?? "").Trim();
            if (text.Length == 0) return true;
            int n;
            if (!int.TryParse(text, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out n) || n > most)
                return false;
            number = n;
            return true;
        }

        private static string Duration(int minutes)
        {
            return minutes == 0 ? "none" : DtrSheet.FormatDuration(minutes);
        }

        private void Mark(DataGridViewCell cell, bool changed, Color normalBack)
        {
            cell.Style.BackColor = changed ? Theme.Highlight : normalBack;
            cell.Style.Font = changed ? _boldFont : null;
        }

        private void ShowTotals()
        {
            int late = 0, undertime = 0, changed = 0;
            for (int i = 0; i < _now.Length; i++)
            {
                DtrDay day = Compute(i);
                late += day.LateMinutes;
                undertime += day.UndertimeMinutes;
                if (IsChanged(i) || !_adjusted[i].IsEmpty) changed++;
            }

            string lateText = late == 0 ? "none" : DtrSheet.FormatDuration(late);
            string undertimeText = undertime == 0 ? "none" : DtrSheet.FormatDuration(undertime);
            _totals.Text = "Late: " + lateText + "     Undertime: " + undertimeText +
                           (changed == 0 ? "" : "     (" + changed + (changed == 1 ? " day" : " days") + " changed)");
        }

        private void Say(string text, bool problem)
        {
            _message.Text = text;
            _message.ForeColor = problem ? Theme.Red : Theme.TextMuted;
        }

        /// <summary>The user finished typing in a box.</summary>
        private void Grid_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            int i = e.RowIndex;
            string typed = (Convert.ToString(_grid.Rows[i].Cells[e.ColumnIndex].Value) ?? "").Trim();
            Say("", false);

            if (e.ColumnIndex == ColRemark)
            {
                _now[i].Remark = typed;
            }
            else if (e.ColumnIndex == ColReason)
            {
                _adjusted[i].Reason = typed;
            }
            else if (e.ColumnIndex >= ColLateHours && e.ColumnIndex <= ColUndertimeMinutes)
            {
                // The Hours and the Minutes box of Late (or of Undertime) are read together.
                bool late = e.ColumnIndex <= ColLateMinutes;
                DataGridViewRow row = _grid.Rows[i];
                object? hoursText = row.Cells[late ? ColLateHours : ColUndertimeHours].Value;
                object? minutesText = row.Cells[late ? ColLateMinutes : ColUndertimeMinutes].Value;
                int? hours, minutes;
                if (!TryReadPart(hoursText, 24, out hours))
                {
                    Say("Day " + (i + 1) + ": Hours must be a whole number from 0 to 24, so the day was not changed.", true);
                }
                else if (!TryReadPart(minutesText, 59, out minutes))
                {
                    Say("Day " + (i + 1) + ": Minutes must be a whole number from 0 to 59, so the day was not changed.", true);
                }
                else
                {
                    // Both boxes empty = back to the computed value. The same as computed = nothing typed in.
                    int? total = !hours.HasValue && !minutes.HasValue ? (int?)null : (hours ?? 0) * 60 + (minutes ?? 0);
                    DtrDay computed = ComputeTimes(i);
                    int computedMinutes = late ? computed.LateMinutes : computed.UndertimeMinutes;
                    if (total.HasValue && total.Value == computedMinutes) total = null;
                    if (late) _adjusted[i].LateMinutes = total;
                    else _adjusted[i].UndertimeMinutes = total;
                }
            }
            else if (e.ColumnIndex >= ColFirstTime && e.ColumnIndex <= ColLastTime)
            {
                int which = e.ColumnIndex - ColFirstTime; // 0 = A.M. Arrival ... 3 = P.M. Departure
                TimeSpan? time;
                if (TimeText.TryParse(typed, which, out time))
                {
                    if (which == 0) _now[i].AmArrival = time;
                    else if (which == 1) _now[i].AmDeparture = time;
                    else if (which == 2) _now[i].PmArrival = time;
                    else _now[i].PmDeparture = time;
                }
                else
                {
                    // Not a time: the box goes back to what it was, and the reason is shown below the table.
                    Say("\"" + typed + "\" is not a time, so Day " + (i + 1) +
                        " was not changed. Type it like  7:05 AM  or  12:30 PM.", true);
                }
            }

            ShowRow(i);
            ShowTotals();
        }

        private void Save_Click(object? sender, EventArgs e)
        {
            _grid.EndEdit();

            // The four times of a day must follow each other: arrive, leave, arrive, leave.
            for (int i = 0; i < _now.Length; i++)
            {
                if (!IsChanged(i)) continue;

                TimeSpan? previous = null;
                foreach (TimeSpan? time in _now[i].Times)
                {
                    if (!time.HasValue) continue;
                    if (previous.HasValue && time.Value <= previous.Value)
                    {
                        _grid.CurrentCell = _grid.Rows[i].Cells[ColFirstTime];
                        MessageBox.Show(this,
                            "Day " + (i + 1) + ": the times are not in order.\n\n" +
                            "Each time must be later than the one before it " +
                            "(A.M. Arrival, A.M. Departure, P.M. Arrival, P.M. Departure).\n\n" +
                            "Tip: add AM or PM when you type, for example 1:05 PM.",
                            "Edit DTR", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    previous = time;
                }
            }

            // Late / Undertime typed in by hand needs a reason, so that anyone can see why it differs.
            for (int i = 0; i < _adjusted.Length; i++)
            {
                if (_adjusted[i].IsEmpty || _adjusted[i].Reason.Trim().Length > 0) continue;
                _grid.CurrentCell = _grid.Rows[i].Cells[ColReason];
                MessageBox.Show(this,
                    "Day " + (i + 1) + ": the Late or Undertime was typed in by hand.\n\n" +
                    "Please type the reason in the Reason box, for example:  Work suspended at 3:00 PM",
                    "Edit DTR", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult = DialogResult.OK;
        }
    }
}
