namespace Dat2DTR
{
    /// <summary>
    /// The "Office Hours" window: the work schedules (A, B, C and D) and how Late is counted.
    ///
    /// The list at the top chooses which schedule is shown. The time boxes, the
    /// grace period and the working days belong to that schedule. The "how is Late
    /// counted" choice and the paper size are the same for both.
    /// </summary>
    public partial class SettingsForm : Form
    {
        private DtrSettings _settings;          // the copy being edited
        private int _showing;                   // which schedule the boxes show: 0 = A ... 3 = D
        private bool _loading;                  // true while the program fills the boxes itself
        private readonly CheckBox[] _dayBoxes;  // index = DayOfWeek (0 = Sunday)

        public SettingsForm(DtrSettings current)
        {
            InitializeComponent();
            Icon = Theme.GetAppIcon();
            Theme.StylePrimary(btnOk);

            _settings = current.Clone();
            _dayBoxes = new[] { chkSun, chkMon, chkTue, chkWed, chkThu, chkFri, chkSat };

            foreach (DtrPaper paper in Enum.GetValues(typeof(DtrPaper)))
                cboPaper.Items.Add(PaperInfo.NameOf(paper));

            ShowSettings();
        }

        /// <summary>The settings the user chose. Call after ShowDialog() returned OK.</summary>
        public DtrSettings GetSettings()
        {
            return _settings;
        }

        /// <summary>Fills every box from _settings.</summary>
        private void ShowSettings()
        {
            _loading = true;
            rdoFromOfficial.Checked = _settings.CountLateFromOfficialTime;
            rdoAfterGrace.Checked = !_settings.CountLateFromOfficialTime;
            cboPaper.SelectedIndex = (int)_settings.Paper;
            _loading = false;

            ShowScheduleList();
            ShowSchedule();
        }

        /// <summary>Refreshes the list at the top, so its words match the times that were typed.</summary>
        private void ShowScheduleList()
        {
            _loading = true;
            cboWhich.Items.Clear();
            foreach (WorkSchedule schedule in _settings.Schedules)
                cboWhich.Items.Add(schedule.Description);
            cboWhich.SelectedIndex = _showing;
            _loading = false;
        }

        /// <summary>Fills the schedule boxes from the schedule that is chosen at the top.</summary>
        private void ShowSchedule()
        {
            WorkSchedule s = _settings.Schedules[_showing];

            _loading = true;
            dtpTimeIn.Value = DateTime.Today + s.TimeIn;
            dtpLunchStart.Value = DateTime.Today + s.LunchStart;
            dtpLunchEnd.Value = DateTime.Today + s.LunchEnd;
            dtpTimeOut.Value = DateTime.Today + s.TimeOut;
            nudGrace.Value = s.GraceMinutes;
            for (int i = 0; i < 7; i++)
                _dayBoxes[i].Checked = s.WorkDays[i];
            grpHours.Text = "Office hours of Schedule " + s.Letter;
            grpDays.Text = "Working days of Schedule " + s.Letter + " (a working day with no scans is marked ABSENT)";
            _loading = false;

            UpdateExamples();
        }

        /// <summary>Copies what is in the boxes into _settings.</summary>
        private void ReadBoxes()
        {
            WorkSchedule s = _settings.Schedules[_showing];
            s.TimeIn = TimeOf(dtpTimeIn);
            s.LunchStart = TimeOf(dtpLunchStart);
            s.LunchEnd = TimeOf(dtpLunchEnd);
            s.TimeOut = TimeOf(dtpTimeOut);
            s.GraceMinutes = (int)nudGrace.Value;
            for (int i = 0; i < 7; i++)
                s.WorkDays[i] = _dayBoxes[i].Checked;

            _settings.CountLateFromOfficialTime = rdoFromOfficial.Checked;
            _settings.Paper = (DtrPaper)Math.Max(0, cboPaper.SelectedIndex);
        }

        private static TimeSpan TimeOf(DateTimePicker picker)
        {
            return new TimeSpan(picker.Value.Hour, picker.Value.Minute, 0);
        }

        /// <summary>Keeps the "(arriving 7:20 AM = 20 minutes late)" examples in step with what is typed.</summary>
        private void UpdateExamples()
        {
            TimeSpan timeIn = TimeOf(dtpTimeIn);
            int grace = (int)nudGrace.Value;
            string example = DtrSettings.FormatTime(timeIn + TimeSpan.FromMinutes(grace + 5));

            rdoFromOfficial.Text = "Count from Time In   (arriving " + example + " = " + (grace + 5) + " minutes late)";
            rdoAfterGrace.Text = "Count only the minutes after the grace period   (arriving " + example + " = 5 minutes late)";
        }

        private void RuleChanged(object? sender, EventArgs e)
        {
            if (!_loading)
                UpdateExamples();
        }

        /// <summary>The user picked the other schedule in the list at the top.</summary>
        private void cboWhich_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_loading || cboWhich.SelectedIndex < 0)
                return;

            ReadBoxes();                        // keep what was typed for the schedule we are leaving
            _showing = cboWhich.SelectedIndex;
            ShowScheduleList();
            ShowSchedule();
        }

        private void btnDefaults_Click(object? sender, EventArgs e)
        {
            var defaults = new DtrSettings();
            defaults.LastFolder = _settings.LastFolder;
            _settings = defaults;
            ShowSettings();
        }

        private void btnOk_Click(object? sender, EventArgs e)
        {
            ReadBoxes();

            string problem = _settings.Validate();
            if (problem.Length > 0)
            {
                MessageBox.Show(this, problem, "Office Hours", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult = DialogResult.OK;
        }
    }
}
