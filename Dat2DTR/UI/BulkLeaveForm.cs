using System.Globalization;

namespace Dat2DTR
{
    /// <summary>
    /// "Edit Leave" for several employees at once (the ones ticked in the list): one kind of
    /// leave - for example Holiday or Suspended - for a range of days, for everyone ticked.
    /// Days that are not working days in an employee's schedule are skipped for that employee.
    /// Choosing "(no leave)" without WFH removes what is filed on those days.
    /// For Suspended, "How long" can also be "From a time": for example work suspended from
    /// 3:00 PM - no undertime is counted after that time for anyone ticked.
    /// </summary>
    internal sealed class BulkLeaveForm : Form
    {
        private const string NoLeave = "(no leave - remove what is filed)";
        private static readonly string[] PartTexts = { "Whole day", "Half day - morning", "Half day - afternoon" };
        private const string FromATime = "From a time (work suspended from...)";

        private readonly DateTimePicker _from = new DateTimePicker(), _to = new DateTimePicker();
        private readonly ComboBox _leave = new ComboBox(), _part = new ComboBox();
        private readonly DateTimePicker _time = new DateTimePicker();   // "From a time": the time work was suspended
        private readonly CheckBox _wfh = new CheckBox(), _idlar = new CheckBox();
        private readonly Label _what = new Label();
        private readonly DateTime _month;

        /// <param name="month">First day of the month on screen.</param>
        /// <param name="names">The names (or IDs) of the ticked employees.</param>
        /// <param name="firstDay">The day the dates start on (the day shown, or the 1st).</param>
        public BulkLeaveForm(DateTime month, List<string> names, DateTime firstDay)
        {
            _month = new DateTime(month.Year, month.Month, 1);
            DateTime last = _month.AddMonths(1).AddDays(-1);
            if (firstDay < _month || firstDay > last) firstDay = _month;

            // Same order as a designer-made form, so it scales correctly on 125% / 150% screens.
            SuspendLayout();
            Text = "Edit Leave - ticked employees";
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

            const int left = 20, labelWidth = 92, boxLeft = left + labelWidth + 8, width = 560;

            var title = new Label();
            title.Text = "Set the same leave for the " + names.Count + " ticked employees";
            title.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            title.ForeColor = Theme.Navy;
            title.AutoEllipsis = true;
            title.SetBounds(left, 14, width, 26);
            Controls.Add(title);

            var how = new Label();
            how.Text = "For example a Holiday, a work suspension, or Official Business for a school visit. Days that are not working days " +
                       "in an employee's schedule are skipped for that employee.";
            how.ForeColor = Theme.TextMuted;
            how.SetBounds(left, 42, width, 36);
            Controls.Add(how);

            int y = 86;
            AddLabel("From", left, y, labelWidth);
            SetUpDate(_from, boxLeft, y, firstDay, last);
            AddLabel("to", boxLeft + 170, y, 24);
            SetUpDate(_to, boxLeft + 198, y, firstDay, last);

            y += 36;
            AddLabel("Leave", left, y, labelWidth);
            _leave.DropDownStyle = ComboBoxStyle.DropDownList;
            _leave.Items.Add(NoLeave);
            foreach (LeaveKind kind in LeaveKinds.All)
                if (kind.Type != LeaveType.WorkFromHome)
                    _leave.Items.Add(kind.Name);
            _leave.SelectedItem = LeaveKinds.Get(LeaveType.Holiday).Name;
            _leave.SetBounds(boxLeft, y, 260, 23);
            _leave.SelectedIndexChanged += (sender, e) => ShowBoxes();
            Controls.Add(_leave);

            y += 36;
            AddLabel("How long", left, y, labelWidth);
            _part.DropDownStyle = ComboBoxStyle.DropDownList;
            _part.Items.AddRange(PartTexts);
            _part.SelectedIndex = 0;
            _part.SetBounds(boxLeft, y, 260, 23);
            _part.SelectedIndexChanged += (sender, e) => ShowBoxes();
            Controls.Add(_part);

            _time.Format = DateTimePickerFormat.Custom;
            _time.CustomFormat = "h:mm tt";
            _time.ShowUpDown = true;
            _time.Value = DateTime.Today.AddHours(15);      // 3:00 PM, the usual time
            _time.SetBounds(boxLeft + 270, y, 110, 23);
            _time.ValueChanged += (sender, e) => ShowBoxes();
            Controls.Add(_time);

            y += 36;
            _wfh.Text = "WFH";
            _wfh.SetBounds(boxLeft, y, 130, 24);
            _wfh.CheckedChanged += (sender, e) => ShowBoxes();
            Controls.Add(_wfh);

            _idlar.Text = "IDLAR";
            _idlar.SetBounds(boxLeft + 140, y, 120, 24);
            _idlar.CheckedChanged += (sender, e) => ShowBoxes();
            Controls.Add(_idlar);

            y += 34;
            _what.ForeColor = Theme.TextStrong;
            _what.SetBounds(left, y, width, 38);
            Controls.Add(_what);

            y += 44;
            var who = new Label();
            who.Text = "Ticked:  " + string.Join(";  ", names.ToArray());
            who.ForeColor = Theme.TextMuted;
            who.AutoEllipsis = true;
            who.SetBounds(left, y, width, 54);
            Controls.Add(who);

            y += 62;
            var apply = new Button();
            apply.Text = "Apply";
            apply.SetBounds(left + width - 96 - 8 - 96, y, 96, 32);
            apply.Click += Apply_Click;
            Theme.StylePrimary(apply);
            Controls.Add(apply);
            AcceptButton = apply;

            var cancel = new Button();
            cancel.Text = "Cancel";
            cancel.DialogResult = DialogResult.Cancel;
            cancel.SetBounds(left + width - 96, y, 96, 32);
            Controls.Add(cancel);
            CancelButton = cancel;

            ClientSize = new Size(width + left * 2, y + 32 + 18);
            ResumeLayout(false);
            PerformLayout();

            ShowBoxes();
        }

        /// <summary>The first day to change.</summary>
        public DateTime GetFrom()
        {
            return _from.Value.Date;
        }

        /// <summary>The last day to change.</summary>
        public DateTime GetTo()
        {
            return _to.Value.Date;
        }

        /// <summary>What to file on each of those days, or null to remove what is filed.</summary>
        public LeaveEntry? GetEntry()
        {
            bool fromATime = (_part.SelectedItem as string) == FromATime;
            var part = fromATime ? LeavePart.WholeDay : (LeavePart)Math.Max(0, _part.SelectedIndex);
            LeaveKind? kind = LeaveKinds.All.FirstOrDefault(k => k.Name == (_leave.SelectedItem as string) &&
                                                               k.Type != LeaveType.WorkFromHome);
            if (kind != null)
            {
                var entry = new LeaveEntry(kind.Type, part, _wfh.Checked, _idlar.Checked);
                if (fromATime && kind.Type == LeaveType.Suspended)
                    entry.From = new TimeSpan(_time.Value.Hour, _time.Value.Minute, 0);
                return entry;
            }
            if (_wfh.Checked)
                return new LeaveEntry(LeaveType.WorkFromHome, part, true, _idlar.Checked);
            return null;
        }

        /// <summary>For the self test: chooses what the person would choose.</summary>
        internal void Choose(int fromDay, int toDay, LeaveType type, bool wfh, bool idlar)
        {
            _from.Value = new DateTime(_month.Year, _month.Month, fromDay);
            _to.Value = new DateTime(_month.Year, _month.Month, toDay);
            _leave.SelectedItem = type == LeaveType.None ? NoLeave : LeaveKinds.Get(type).Name;
            _wfh.Checked = wfh;
            _idlar.Checked = idlar;
        }

        /// <summary>For the self test: Suspended from a time.</summary>
        internal void ChooseSuspendedFrom(int fromDay, int toDay, TimeSpan time)
        {
            Choose(fromDay, toDay, LeaveType.Suspended, false, false);
            _part.SelectedItem = FromATime;
            _time.Value = DateTime.Today.Add(time);
        }

        /// <summary>For the self test: the text of the WFH box, and whether IDLAR can be ticked.</summary>
        internal string GetWfhText()
        {
            return _wfh.Text + (_idlar.Enabled ? " / IDLAR on" : " / IDLAR off");
        }

        private void AddLabel(string text, int x, int y, int width)
        {
            var label = new Label();
            label.Text = text;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.SetBounds(x, y, width, 23);
            Controls.Add(label);
        }

        private void SetUpDate(DateTimePicker picker, int x, int y, DateTime value, DateTime last)
        {
            picker.Format = DateTimePickerFormat.Custom;
            picker.CustomFormat = "MMM d, yyyy  (ddd)";
            picker.MinDate = _month;
            picker.MaxDate = last;
            picker.Value = value;
            picker.SetBounds(x, y, 160, 23);
            Controls.Add(picker);
        }

        /// <summary>
        /// WFH can go with "no leave", Holiday or Suspended (on a Suspended day it asks
        /// "Is it WFH?"); IDLAR can only be ticked after WFH.
        /// </summary>
        private void ShowBoxes()
        {
            LeaveKind? kind = LeaveKinds.All.FirstOrDefault(k => k.Name == (_leave.SelectedItem as string));
            bool wfhAllowed = kind == null || LeaveKinds.IsNotALeave(kind.Type);
            bool suspended = kind != null && kind.Type == LeaveType.Suspended;

            _wfh.Text = suspended ? "Is it WFH?" : "WFH";
            _wfh.Enabled = wfhAllowed;
            if (!wfhAllowed) _wfh.Checked = false;
            _idlar.Enabled = _wfh.Checked;
            if (!_wfh.Checked) _idlar.Checked = false;
            _part.Enabled = kind != null || _wfh.Checked;

            // "From a time" is only for Suspended.
            if (suspended && !_part.Items.Contains(FromATime))
                _part.Items.Add(FromATime);
            else if (!suspended && _part.Items.Contains(FromATime))
            {
                if ((_part.SelectedItem as string) == FromATime) _part.SelectedIndex = 0;
                _part.Items.Remove(FromATime);
            }
            _time.Visible = (_part.SelectedItem as string) == FromATime;
            if (_time.Visible)
            {
                // WFH / IDLAR do not go with a suspension from a time.
                _wfh.Checked = false;
                _wfh.Enabled = false;
                _idlar.Enabled = false;
            }

            LeaveEntry? entry = GetEntry();
            if (entry != null && entry.From.HasValue)
                _what.Text = "Work suspended from " + DtrSettings.FormatTime(entry.From.Value) + ": an ordinary day until then " +
                             "(late, undertime and ABSENT as usual), and no undertime after it. Nothing is deducted for the suspension.";
            else if (entry == null)
                _what.Text = "Removes whatever is filed on those days (leave, holiday, WFH).";
            else if (LeaveKinds.IsNotCounted(entry.Type))
                _what.Text = "Shown on the DTR as \"" + entry.DtrLabel + "\". Nothing is deducted." +
                             (entry.WaitsForIdlar ? "  Tick IDLAR once it is handed in, so the day counts as present." : "");
            else
                _what.Text = entry.Kind.Name + " is filed on those days for everyone ticked, and deducted the usual way.";
        }

        private void Apply_Click(object? sender, EventArgs e)
        {
            if (GetTo() < GetFrom())
            {
                MessageBox.Show(this, "The last day is before the first day.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            DialogResult = DialogResult.OK;
        }
    }
}
