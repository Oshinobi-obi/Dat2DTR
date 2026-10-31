using System.Diagnostics;
using System.Drawing.Printing;

namespace Dat2DTR
{
    /// <summary>
    /// This is the main window. It follows three steps, from left to right:
    ///   1. open the .dat file   2. choose the month   3. print or save.
    /// The list on the left shows the employees, the right side shows the DTR.
    ///
    /// This file only handles the buttons and the list. The real work is in:
    ///   Core/AttendanceLog.cs       - reads the .dat file
    ///   Core/DtrBuilder.cs          - Late / Undertime rules
    ///   Core/WorkSchedule.cs        - the sets of office hours (Schedule A, B, C and D)
    ///   Core/DtrCorrections.cs      - times typed in by hand ("Edit DTR")
    ///   Core/Leave.cs               - the leaves filed with "Edit Leave"
    ///   Core/LeaveCalculator.cs     - how leave is deducted from the leave credits
    ///   Rendering/DtrPageRenderer.cs   - how the DTR looks
    ///   Rendering/LeavePageRenderer.cs - how the Computation of Leaves looks
    ///   Export/                     - PDF and Excel files
    /// </summary>
    public partial class Form1 : Form
    {
        private DtrSettings _settings = new DtrSettings();
        private EmployeeDirectory _names = new EmployeeDirectory();

        /// <summary>This is the times that were typed in by hand with "Edit DTR".</summary>
        private CorrectionStore _corrections = new CorrectionStore();

        /// <summary>This is the leaves that were filed with "Edit Leave".</summary>
        private LeaveStore _leaves = new LeaveStore();

        /// <summary>This is what is remembered of each month, so the leave balance carries over to the next month.</summary>
        private LeaveLedger _ledger = new LeaveLedger();

        /// <summary>This is the CTO and Solo Parent Leave given to each employee.</summary>
        private CtoStore _cto = new CtoStore();

        /// <summary>This is the holidays of the year, used for every employee ("Holidays...").</summary>
        private HolidayCalendar _holidays = new HolidayCalendar();

        /// <summary>This is true when holidays.csv could not be read at start: then it is not written over this session.</summary>
        private bool _holidaysUnreadable;

        private AttendanceLog? _log;

        /// <summary>This is the .dat file opened last ("" = none yet): "Save the scans" never writes over it.</summary>
        private string _openedPath = "";

        /// <summary>This is one DTR per employee for the chosen month.</summary>
        private List<DtrSheet> _sheets = new List<DtrSheet>();

        /// <summary>This is the Computation of Leaves of each employee in _sheets, by employee ID.</summary>
        private readonly Dictionary<string, LeaveCard> _cards =
            new Dictionary<string, LeaveCard>(StringComparer.OrdinalIgnoreCase);

        /// <summary>This is false when the preview shows the DTR, true when it shows the Computation of Leaves.</summary>
        private bool _showLeave;

        /// <summary>This is true when the preview shows the Certificate of Leave Credits (then _showLeave is false).</summary>
        private bool _showCertificate;

        /// <summary>
        /// This is the third tab above the preview, "Certificate of Leave Credits", and the "Edit Certificate Details..." button
        /// shown beside it while the certificate is on screen (see PlaceCertificateTab).
        /// </summary>
        private readonly Button btnViewCertificate = new Button();
        private readonly Button btnCertificateDetails = new Button();

        /// <summary>This is the IDs of the employees whose box is ticked.</summary>
        private readonly HashSet<string> _ticked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>This is true while the program itself is changing the list (so events are ignored).</summary>
        private bool _updating;

        /// <summary>This is the "Leave Summary..." and "Backup / Restore..." buttons, in a second row below the list.</summary>
        private readonly Button btnSummary = new Button();
        private readonly Button btnBackup = new Button();
        private readonly Button btnReports = new Button();

        /// <summary>This is "About" on the navy bar at the top: the version, the developer and the version history.</summary>
        private readonly Button btnAbout = new Button();

        /// <summary>This is the clock at the bottom right: the date and time in the Philippines (see PhilippineTime).</summary>
        private readonly ToolStripStatusLabel lblClock = new ToolStripStatusLabel();
        private readonly System.Windows.Forms.Timer _clockTimer = new System.Windows.Forms.Timer();

        /// <summary>This is the year beside the month list: the month list shows only the months of that year.</summary>
        private readonly ComboBox cboYear = new ComboBox();

        public Form1()
        {
            InitializeComponent();
            Icon = Theme.GetAppIcon();

            // This is the clock at the right end of the status bar, after the office hours. It is checked every second,
            // so it changes as soon as the minute changes.
            lblClock.BorderSides = ToolStripStatusLabelBorderSides.Left;
            lblClock.BorderStyle = Border3DStyle.Etched;
            lblClock.Padding = new Padding(6, 0, 0, 0);
            lblClock.ToolTipText = "The date and time in the Philippines (UTC+8), from this computer's clock";
            statusStrip.ShowItemToolTips = true;
            statusStrip.Items.Add(lblClock);
            ShowClock();
            _clockTimer.Interval = 1000;
            _clockTimer.Tick += (sender, e) => ShowClock();
            _clockTimer.Start();
            FormClosed += (sender, e) => { _clockTimer.Stop(); _clockTimer.Dispose(); };

            // This is the DepEd NCR seal and the "One NCR" banner, built into the program.
            picSeal.SetImage(Theme.GetImage(Theme.SealImage));
            picBanner.SetImage(Theme.GetImage(Theme.BannerImage));
            preview.SetEmptyImage(Theme.GetImage(Theme.BannerImage));

            // This is so the chosen employee stays highlighted even while the mouse is used elsewhere.
            lvEmployees.HideSelection = false;

            Theme.StylePrimary(btnOpen);
            Theme.StylePrimary(btnPrint);
            Theme.StylePrimary(btnSave);
            Theme.StylePrimary(btnEdit);
            Theme.StylePrimary(btnLeave);
            ShowViewButtons();
            Theme.StyleHeaderButton(btnSettings);
            Theme.StyleHeaderButton(btnHelp);

            // This is the second row of buttons below the list. They are put in place when the window opens
            // (see PlaceSecondRow), after Windows has scaled the window for the screen.
            btnSummary.Text = "Leave Summary...";
            btnSummary.UseVisualStyleBackColor = true;
            btnSummary.Enabled = false;
            btnSummary.Click += btnSummary_Click;
            toolTip.SetToolTip(btnSummary, "Everyone's VL, SL, CTO and Solo Parent balance, and the leave used this year");
            btnBackup.Text = "Backup / Restore...";
            btnBackup.UseVisualStyleBackColor = true;
            btnBackup.Click += btnBackup_Click;
            toolTip.SetToolTip(btnBackup, "Save everything Dat2DTR keeps on this computer in one .zip file, or put a backup back");
            btnReports.Text = "Reports...";
            btnReports.UseVisualStyleBackColor = true;
            btnReports.Enabled = false;
            btnReports.Click += btnReports_Click;
            toolTip.SetToolTip(btnReports, "Late and no Time-In on Mondays, late and undertime this month, for the whole office");
            pnlLeftBottom.Controls.Add(btnSummary);
            pnlLeftBottom.Controls.Add(btnReports);
            pnlLeftBottom.Controls.Add(btnBackup);
#if NET
            txtSearch.PlaceholderText = "Search name or ID";
#endif
        }

        // This is the start-up ================================================================

        private void Form1_Load(object? sender, EventArgs e)
        {
            _settings = DtrSettings.Load();
            _names = EmployeeDirectory.LoadDefault();
            _corrections = CorrectionStore.LoadDefault();
            _leaves = LeaveStore.LoadDefault();
            _ledger = LeaveLedger.LoadDefault();
            _cto = CtoStore.LoadDefault();
            _holidays = HolidayCalendar.LoadDefault();
            _holidaysUnreadable = _holidays.LoadFailed;
            cboFilter.SelectedIndex = 0;

            // This is for saved leaves that cannot be read (the file is open in Excel, for example): nothing
            // is saved over them in this session. Otherwise they would be replaced by an empty list.
            if (_holidays.LoadFailed)
                MessageBox.Show(this,
                    "The saved holidays could not be read. The file is probably open in another program, for example Excel.\n\n" +
                    "Close that program and start Dat2DTR again. Until then the holidays are not shown, and changes to them are not saved.",
                    "Dat2DTR", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            if (_leaves.LoadFailed || _ledger.LoadFailed || _cto.LoadFailed)
                MessageBox.Show(this,
                    "The saved leaves and leave balances could not be read. " +
                    "The file is probably open in another program, for example Excel.\n\n" +
                    "Close that program and start Dat2DTR again.\n\n" +
                    "You can keep working, but leaves and leave balances will not be saved this time, so that nothing is lost.",
                    "Dat2DTR", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            SizeListColumns();
            PlaceSecondRow();
            PlaceYearBox();
            PlaceAboutButton();
            PlaceCertificateTab();
            preview.SetPaper(_settings.Paper);
            ShowRules();
            ShowEmployeeList();
            ShowKeptScans(true);
        }

        /// <summary>This is the clock text, written again only when it changes (once a minute).</summary>
        private void ShowClock()
        {
            string text = PhilippineTime.ClockText(PhilippineTime.Now);
            if (lblClock.Text != text)
                lblClock.Text = text;
        }

        /// <summary>
        /// This is where "About" is put on the navy bar, left of "Office Hours...", with the banner moved left to make room.
        /// The version is also written in the title bar, e.g. "Dat2DTR 1.5.0 - Daily Time Record Maker".
        /// </summary>
        private void PlaceAboutButton()
        {
            Text = "Dat2DTR " + AppInfo.Version + " - Daily Time Record Maker";
            btnAbout.Text = "About";
            btnAbout.Font = btnHelp.Font;
            Theme.StyleHeaderButton(btnAbout);
            btnAbout.Click += (sender, e) =>
            {
                using (var dialog = new AboutForm())
                    dialog.ShowDialog(this);
            };
            toolTip.SetToolTip(btnAbout, "Version " + AppInfo.Version + ", who made the program and how to contact them, and the version history");
            int gap = btnHelp.Left - btnSettings.Right;
            int width = TextRenderer.MeasureText("About", btnHelp.Font).Width + btnHelp.Height;
            btnAbout.SetBounds(btnSettings.Left - gap - width, btnSettings.Top, width, btnSettings.Height);
            btnAbout.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            picBanner.Left -= width + gap;
            pnlHeader.Controls.Add(btnAbout);
        }

        /// <summary>
        /// This is where the "Certificate of Leave Credits" tab is put after the "Computation of Leaves" tab, the same height
        /// and font, measured after Windows has scaled the window. "Edit Certificate Details..." goes beside it, shown only
        /// while the certificate is on screen.
        /// </summary>
        private void PlaceCertificateTab()
        {
            int gap = btnViewLeave.Left - btnViewDtr.Right;
            int padding = btnViewLeave.Height;   // This is the room around the words, like the other two tabs

            btnViewCertificate.Text = "Certificate of Leave Credits";
            btnViewCertificate.Font = btnViewLeave.Font;
            btnViewCertificate.UseVisualStyleBackColor = false;
            btnViewCertificate.TabIndex = btnViewLeave.TabIndex + 1;
            int width = Math.Max(btnViewLeave.Width, TextRenderer.MeasureText(btnViewCertificate.Text, btnViewLeave.Font).Width + padding);
            btnViewCertificate.SetBounds(btnViewLeave.Right + gap, btnViewLeave.Top, width, btnViewLeave.Height);
            btnViewCertificate.Click += btnViewCertificate_Click;
            toolTip.SetToolTip(btnViewCertificate, "Show the Certificate of Leave Credits of this employee, as it will be printed");
            pnlView.Controls.Add(btnViewCertificate);

            // This is an ordinary button (grey, regular letters), so it does not look like a fourth tab.
            btnCertificateDetails.Text = "Edit Certificate Details...";
            btnCertificateDetails.Font = new Font(btnViewLeave.Font, FontStyle.Regular);
            btnCertificateDetails.FlatStyle = FlatStyle.Flat;
            btnCertificateDetails.FlatAppearance.BorderColor = Theme.Line;
            btnCertificateDetails.FlatAppearance.MouseOverBackColor = Theme.Highlight;
            btnCertificateDetails.BackColor = Theme.Panel;
            btnCertificateDetails.ForeColor = Theme.TextStrong;
            btnCertificateDetails.Cursor = Cursors.Hand;
            btnCertificateDetails.TabIndex = btnViewCertificate.TabIndex + 1;
            int detailsWidth = TextRenderer.MeasureText(btnCertificateDetails.Text, btnCertificateDetails.Font).Width + padding;
            btnCertificateDetails.SetBounds(btnViewCertificate.Right + gap * 4, btnViewLeave.Top, detailsWidth, btnViewLeave.Height);
            btnCertificateDetails.Visible = false;
            btnCertificateDetails.Click += (sender, e) =>
            {
                EditCertificateDetails(this);
                ShowCurrentSheet();   // This is so the new names and positions are seen at once
            };
            toolTip.SetToolTip(btnCertificateDetails, "Change the names and positions of \"Prepared by\" and \"Certified correct\" on the certificate");
            pnlView.Controls.Add(btnCertificateDetails);
            ShowViewButtons();
        }

        /// <summary>
        /// This is where the year list is put in front of the month list (in the space the month list had), measured
        /// from the month list after Windows has scaled the window, so it fits on any screen scaling.
        /// </summary>
        private void PlaceYearBox()
        {
            cboYear.DropDownStyle = ComboBoxStyle.DropDownList;
            cboYear.Font = cboMonth.Font;
            cboYear.Enabled = false;
            cboYear.MaxDropDownItems = 12;
            cboYear.TabIndex = cboMonth.TabIndex;
            cboMonth.TabIndex = cboYear.TabIndex + 1;   // This is so Tab goes from the year to the month
            cboYear.SelectedIndexChanged += cboYear_SelectedIndexChanged;
            toolTip.SetToolTip(cboYear, "The year: the month list beside it shows the months of this year");
            cboMonth.MaxDropDownItems = 12;
            cboMonth.Parent!.Controls.Add(cboYear);

            int gap = cboMonth.Height / 4;
            int yearWidth = TextRenderer.MeasureText("2026", cboMonth.Font).Width + SystemInformation.VerticalScrollBarWidth + cboMonth.Height / 2;
            int right = cboMonth.Right;
            cboYear.SetBounds(cboMonth.Left, cboMonth.Top, yearWidth, cboMonth.Height);
            cboMonth.SetBounds(cboYear.Right + gap, cboMonth.Top, right - cboYear.Right - gap, cboMonth.Height);
            cboYear.Anchor = cboMonth.Anchor;
        }

        /// <summary>This is a new year picked: its months are shown, and the newest of them is picked.</summary>
        private void cboYear_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_updating || _log == null || cboYear.SelectedItem == null) return;
            int year = (int)cboYear.SelectedItem;
            DateTime newest = _log.GetMonths().FirstOrDefault(m => m.Year == year);
            _updating = true;
            FillMonths(year, newest);
            _updating = false;
            _ticked.Clear();
            BuildSheets();
        }

        /// <summary>This is where the month list is filled with the months of one year (newest first), with one picked.</summary>
        private void FillMonths(int year, DateTime picked)
        {
            cboMonth.Items.Clear();
            if (_log == null) return;
            foreach (DateTime month in _log.GetMonths().Where(m => m.Year == year))
            {
                var item = new MonthItem(month);
                cboMonth.Items.Add(item);
                if (month == picked)
                    cboMonth.SelectedItem = item;
            }
            if (cboMonth.SelectedIndex < 0 && cboMonth.Items.Count > 0)
                cboMonth.SelectedIndex = 0;
        }

        /// <summary>This is "October 2026".</summary>
        private static string MonthName(DateTime month)
        {
            return month.ToString("MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// This is where the scans kept on this computer (every .dat file opened before, see ScanArchive) are shown, so
        /// their months can be chosen without opening a file. Nothing happens when nothing is kept yet.
        /// </summary>
        /// <param name="atStart">This is true when the program starts (the newest month is picked); false keeps the chosen month.</param>
        private void ShowKeptScans(bool atStart)
        {
            AttendanceLog? kept;
            try
            {
                kept = ScanArchive.Load();
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                lblStatus.Text = "The scans kept on this computer could not be read: " + ex.Message;
                return;
            }
            if (kept == null)
                return;

            var chosen = cboMonth.SelectedItem as MonthItem;
            DateTime month = !atStart && chosen != null ? chosen.Month : kept.SuggestMonth();
            List<DateTime> months = kept.GetMonths();
            ShowLog(kept, months.Contains(month) ? month : kept.SuggestMonth(),
                    "Scans kept on this computer\r\n" + KeptText(kept));
            lblStatus.Text = "The scans of the files opened before are ready: " + months.Count +
                             (months.Count == 1 ? " month" : " months") + " to choose from. Open a .dat file to add more.";
        }

        /// <summary>This is "12,345 scans, 85 employees, August 2026 to October 2026".</summary>
        private static string KeptText(AttendanceLog log)
        {
            List<DateTime> months = log.GetMonths();
            string first = months.Count > 0 ? months[months.Count - 1].ToString("MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture) : "";
            string last = months.Count > 0 ? months[0].ToString("MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture) : "";
            return log.RecordCount.ToString("N0") + " scans,  " + log.EmployeeCount + " employees,  " +
                   (first == last ? first : first + " to " + last);
        }

        /// <summary>
        /// This is where the second row of buttons (Leave Summary, Reports, Backup / Restore) is put under the first row,
        /// measured from the buttons already there, so it fits on any screen scaling. The tick box and the hint
        /// move down to make room.
        /// </summary>
        private void PlaceSecondRow()
        {
            int gap = btnUntickAll.Left - btnTickAll.Right;
            int top = btnTickAll.Bottom + gap;
            int shift = btnTickAll.Height + gap;
            btnSummary.SetBounds(btnTickAll.Left, top, btnUntickAll.Right - btnTickAll.Left, btnTickAll.Height);
            btnSummary.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            btnReports.SetBounds(btnHolidays.Left, top, btnHolidays.Width, btnTickAll.Height);
            btnReports.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            btnBackup.SetBounds(btnNames.Left, top, btnNames.Width, btnTickAll.Height);
            btnBackup.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            chkNoScans.Top += shift;
            lblHint.Top += shift;
            pnlLeftBottom.Height += shift;
        }

        /// <summary>
        /// This is where every saved file is read again (after a backup was restored): settings, names,
        /// times typed in, leaves, balances, CTO, Solo Parent Leave and holidays. The DTRs are made again.
        /// </summary>
        private void ReloadAllData()
        {
            _settings = DtrSettings.Load();
            _names = EmployeeDirectory.LoadDefault();
            _corrections = CorrectionStore.LoadDefault();
            _leaves = LeaveStore.LoadDefault();
            _ledger = LeaveLedger.LoadDefault();
            _cto = CtoStore.LoadDefault();
            _holidays = HolidayCalendar.LoadDefault();
            _holidaysUnreadable = _holidays.LoadFailed;
            preview.SetPaper(_settings.Paper);
            ShowRules();
            if (_log != null)
                BuildSheets();
            else
                ShowEmployeeList();
            ShowKeptScans(false);   // This is the kept scans of the backup (a restore can bring other months)
        }

        /// <summary>
        /// This is where the narrow columns of the employee list are made just wide enough for their text.
        /// Measuring the text (instead of fixed numbers) keeps it right on any screen scaling.
        /// </summary>
        private void SizeListColumns()
        {
            Font font = lvEmployees.Font;
            int padding = TextRenderer.MeasureText("0", font).Width / 2 + 2;

            colId.Width = TextRenderer.MeasureText("070126010", font).Width + padding;
            colSchedule.Width = Math.Max(TextRenderer.MeasureText("Hours", font).Width,
                                         TextRenderer.MeasureText("7:30-6", font).Width) + padding;
            colLate.Width = TextRenderer.MeasureText("12h 59m", font).Width + padding;
            colUndertime.Width = Math.Max(TextRenderer.MeasureText("Undertime", font).Width,
                                          TextRenderer.MeasureText("12h 59m", font).Width) + padding;
            colCheck.Width = TextRenderer.MeasureText("!*L", font).Width + padding;

            lvEmployees_Resize(lvEmployees, EventArgs.Empty);
        }

        /// <summary>This is where the work schedules are put into the "Work schedule" list above the DTR.</summary>
        private void ShowRules()
        {
            _updating = true;
            cboSchedule.Items.Clear();
            foreach (WorkSchedule schedule in _settings.Schedules)
                cboSchedule.Items.Add(schedule.Description);
            _updating = false;
        }

        // This is step 1: open the file ================================================================

        private void btnOpen_Click(object? sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Open the biometrics file";
                dialog.Filter = "Biometrics file (*.dat)|*.dat|Text file (*.txt)|*.txt|All files (*.*)|*.*";
                if (Directory.Exists(_settings.LastFolder))
                    dialog.InitialDirectory = _settings.LastFolder;

                if (dialog.ShowDialog(this) == DialogResult.OK)
                    OpenFile(dialog.FileName);
            }
        }

        private void Form1_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void Form1_DragDrop(object? sender, DragEventArgs e)
        {
            string[]? files = e.Data == null ? null : e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files != null && files.Length > 0)
                OpenFile(files[0]);
        }

        private void OpenFile(string path)
        {
            AttendanceLog log;
            try
            {
                Cursor = Cursors.WaitCursor;
                log = AttendanceLog.Load(path);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                MessageBox.Show(this, "The file could not be opened.\n\n" + ex.Message,
                    "Dat2DTR", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            finally
            {
                Cursor = Cursors.Default;
            }

            if (log.RecordCount == 0)
            {
                MessageBox.Show(this,
                    "No attendance records were found in this file.\n\n" +
                    "Please choose the .dat file that was exported from the biometrics device.",
                    "Dat2DTR", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // This is where the scans of the file are kept with the ones of the files opened before (see ScanArchive),
            // so the months of before stay in the list. If they cannot be kept, only this file is used.
            AttendanceLog all = log;
            string keptProblem = "";
            try
            {
                Cursor = Cursors.WaitCursor;
                all = ScanArchive.Add(log);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                keptProblem = ex.Message;
            }
            finally
            {
                Cursor = Cursors.Default;
            }

            _settings.LastFolder = Path.GetDirectoryName(path) ?? "";
            _settings.Save();
            _openedPath = path;

            ShowLog(all, log.SuggestMonth(),
                    Path.GetFileName(path) + "\r\n" +
                    log.RecordCount.ToString("N0") + " scans,  " + log.EmployeeCount + " employees,  " +
                    log.FirstDate.ToString("MMM d, yyyy") + " to " + log.LastDate.ToString("MMM d, yyyy"));
            toolTip.SetToolTip(lblFile, path);

            int months = all.GetMonths().Count;
            string skipped = log.SkippedLineCount == 0 ? "" :
                " " + log.SkippedLineCount + " line(s) that were not attendance records were skipped.";
            lblStatus.Text = keptProblem.Length > 0
                ? "File opened, but its scans could not be kept for next time: " + keptProblem + skipped
                : "File opened and its scans kept: " + months + (months == 1 ? " month" : " months") + " to choose from." + skipped;
            if (keptProblem.Length > 0)
                MessageBox.Show(this, "The file is open, but its scans could not be kept on this computer for next time, " +
                                "so the months of the files opened before are not in the list now.\n\n" + keptProblem,
                                "Dat2DTR", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        /// <summary>This is where a log is shown: the text under "Open", the month list (with that month picked) and the DTRs.</summary>
        private void ShowLog(AttendanceLog log, DateTime suggested, string fileText)
        {
            _log = log;
            _ticked.Clear();

            lblFile.Text = fileText;
            lblFile.ForeColor = Theme.TextStrong;

            // This is the year list (newest first) with the year of the suggested month, then the months of that year.
            _updating = true;
            List<DateTime> months = log.GetMonths();
            if (!months.Contains(suggested) && months.Count > 0)
                suggested = months[0];
            cboYear.Items.Clear();
            foreach (int year in months.Select(m => m.Year).Distinct())
                cboYear.Items.Add(year);
            cboYear.SelectedItem = suggested.Year;
            if (cboYear.SelectedIndex < 0 && cboYear.Items.Count > 0)
                cboYear.SelectedIndex = 0;
            FillMonths(suggested.Year, suggested);
            cboYear.Enabled = true;
            cboMonth.Enabled = true;
            _updating = false;

            txtSearch.Text = "";
            BuildSheets();
        }

        // This is step 2: month and employees ================================================================

        private void cboMonth_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_updating) return;
            _ticked.Clear();
            BuildSheets();
        }

        /// <summary>This is where the DTR and the Computation of Leaves of every employee are computed for the chosen month.</summary>
        private void BuildSheets()
        {
            _leaves.Holidays = _holidays;   // This is the holiday calendar, used for everyone
            _sheets = new List<DtrSheet>();
            _cards.Clear();
            var chosen = cboMonth.SelectedItem as MonthItem;

            if (_log != null && chosen != null)
            {
                // This is the last day of the chosen month that the file covers.
                DateTime monthEnd = chosen.Month.AddMonths(1).AddDays(-1);
                DateTime dataFrom = _log.CoveredFrom(chosen.Month), coveredTo = _log.CoveredThrough(chosen.Month);
                DateTime dataThrough = coveredTo < monthEnd ? coveredTo.Date : monthEnd;
                bool ledgerChanged = false;

                foreach (string id in GetIdsToList(_log, chosen.Month))
                {
                    // This is the schedule: each employee follows Schedule A, B, C or D (chosen above the DTR or in "Employee Names").
                    WorkSchedule schedule = _settings.GetSchedule(_names.GetSchedule(id));
                    DtrSheet sheet = DtrBuilder.Build(_log, id, _names.GetName(id), chosen.Month,
                                                      schedule, _settings, _corrections, _leaves);
                    _sheets.Add(sheet);

                    // This is where the late and undertime of this month are remembered, so that next month
                    // the leave balance can be carried over without opening this file again.
                    // (Not for someone with no scans and no leave at all: there is nothing to remember.
                    //  The ledger itself refuses a file that holds only part of the month.)
                    bool hasSomething = sheet.LeaveDays.Count > 0 || sheet.Days.Any(d => d.ScanCount > 0);
                    if (hasSomething &&
                        _ledger.Remember(id, chosen.Month.Year, chosen.Month.Month, sheet.TotalLateMinutes,
                                         sheet.TotalUndertimeMinutes, LeaveRules.DayCharge(schedule),
                                         dataFrom, dataThrough))
                        ledgerChanged = true;

                    _cards[id] = LeaveCalculator.Build(sheet, schedule, _leaves, _ledger, _cto, dataFrom, coveredTo);
                }

                if (ledgerChanged)
                    SaveLeaveFiles(false);

                // This is the order: people with a name first (A to Z), then the IDs that have no name yet.
                _sheets.Sort((a, b) =>
                {
                    bool aNamed = a.EmployeeName.Length > 0, bNamed = b.EmployeeName.Length > 0;
                    if (aNamed != bNamed) return aNamed ? -1 : 1;
                    if (aNamed)
                    {
                        int byName = string.Compare(a.EmployeeName, b.EmployeeName, StringComparison.CurrentCultureIgnoreCase);
                        if (byName != 0) return byName;
                    }
                    return AttendanceLog.CompareIds(a.EmployeeId, b.EmployeeId);
                });
            }

            ShowEmployeeList();
        }

        /// <summary>
        /// This is the list of employees for the month: everyone who scanned at least once, and everyone
        /// who has a leave filed in the month (someone on leave for the whole month does not scan).
        /// With "Also show employees with no scans" ticked, everyone the program knows is listed.
        /// </summary>
        private List<string> GetIdsToList(AttendanceLog log, DateTime month)
        {
            var ids = new List<string>(log.GetEmployeeIds(month));
            var listed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string id in ids)
                listed.Add(EmployeeDirectory.NormalizeId(id));

            // This is how each ID is written in full. (The leave files keep IDs without leading zeros.)
            var written = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string id in log.GetAllEmployeeIds())
                written[EmployeeDirectory.NormalizeId(id)] = id;
            foreach (EmployeeEntry entry in _names.GetAll())
            {
                string key = EmployeeDirectory.NormalizeId(entry.Id);
                if (!written.ContainsKey(key))
                    written[key] = entry.Id;
            }

            var more = new List<string>(_leaves.GetIdsWithLeave(month.Year, month.Month));
            if (chkNoScans.Checked)
                more.AddRange(written.Keys);

            foreach (string key in more)
            {
                if (!listed.Add(key))
                    continue;
                string? full;
                ids.Add(written.TryGetValue(key, out full) ? full : key);
            }
            return ids;
        }

        /// <summary>This is the tick box "Also show employees with no scans this month".</summary>
        private void chkNoScans_CheckedChanged(object? sender, EventArgs e)
        {
            if (_updating) return;
            BuildSheets();
        }

        /// <summary>This is where the list on the left is filled (only the employees that match the search box).</summary>
        private void ShowEmployeeList()
        {
            string search = txtSearch.Text.Trim();
            DtrSheet? selected = GetCurrentSheet();
            string selectedId = selected == null ? "" : selected.EmployeeId;

            _updating = true;
            lvEmployees.BeginUpdate();
            lvEmployees.Items.Clear();

            foreach (DtrSheet sheet in _sheets)
            {
                bool matches = search.Length == 0 ||
                    sheet.EmployeeName.IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                    sheet.EmployeeId.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
                if (!matches)
                    continue;

                bool hasName = sheet.EmployeeName.Length > 0;
                bool needsCheck = sheet.DaysToCheck.Count > 0;
                bool wasEdited = sheet.EditedDays.Count > 0 || sheet.AdjustedDays.Count > 0;
                bool hasLeave = sheet.LeaveDays.Count > 0;

                // This is the list above the names: everyone, or only the ones with  !  *  or  L.
                int filter = cboFilter.SelectedIndex;
                if ((filter == 1 && !needsCheck) || (filter == 2 && !wasEdited) || (filter == 3 && !hasLeave))
                    continue;

                var item = new ListViewItem(hasName ? sheet.EmployeeName : "(no name yet)");
                item.SubItems.Add(sheet.EmployeeId);
                item.SubItems.Add(sheet.ScheduleShort);
                item.SubItems.Add(DtrSheet.FormatDuration(sheet.TotalLateMinutes));
                item.SubItems.Add(DtrSheet.FormatDuration(sheet.TotalUndertimeMinutes));
                item.SubItems.Add((needsCheck ? "!" : "") + (wasEdited ? "*" : "") + (hasLeave ? "L" : ""));  // This is ! = check,  * = typed in by hand,  L = on leave
                item.Tag = sheet;
                item.Checked = _ticked.Contains(sheet.EmployeeId);
                if (!hasName)
                    item.ForeColor = Theme.TextMuted;
                if (needsCheck)
                    item.ToolTipText = "Some days have a missing or extra scan. Click to see which days.";
                else if (wasEdited)
                    item.ToolTipText = "Some days (times, or Late / Undertime) were typed in by hand.";
                else if (hasLeave)
                    item.ToolTipText = "A leave is filed for " + sheet.LeaveDays.Count + (sheet.LeaveDays.Count == 1 ? " day." : " days.");
                else if (!sheet.Days.Any(d => d.ScanCount > 0))
                    item.ToolTipText = "No scans this month.";

                lvEmployees.Items.Add(item);
                if (sheet.EmployeeId == selectedId)
                    item.Selected = true;
            }

            lvEmployees.EndUpdate();
            _updating = false;

            if (lvEmployees.SelectedItems.Count == 0 && lvEmployees.Items.Count > 0)
                lvEmployees.Items[0].Selected = true;
            if (lvEmployees.SelectedItems.Count > 0)
                lvEmployees.SelectedItems[0].EnsureVisible();

            ShowCurrentSheet();
        }

        /// <summary>This is the DTR of the employee that is selected in the list (or null).</summary>
        private DtrSheet? GetCurrentSheet()
        {
            if (lvEmployees.SelectedItems.Count == 0) return null;
            return lvEmployees.SelectedItems[0].Tag as DtrSheet;
        }

        private void lvEmployees_SelectedIndexChanged(object? sender, EventArgs e)
        {
            // This is because, when the user clicks another name, Windows first reports "nothing selected"
            // and then the new selection. Ignoring the first one avoids a flicker.
            if (_updating || lvEmployees.SelectedItems.Count == 0) return;
            ShowCurrentSheet();
        }

        private void lvEmployees_ItemChecked(object? sender, ItemCheckedEventArgs e)
        {
            if (_updating) return;

            var sheet = e.Item.Tag as DtrSheet;
            if (sheet == null) return;

            if (e.Item.Checked) _ticked.Add(sheet.EmployeeId);
            else _ticked.Remove(sheet.EmployeeId);
            ShowCounts();
        }

        private void lvEmployees_Resize(object? sender, EventArgs e)
        {
            // This is so the Name column uses whatever width is left over.
            int others = colId.Width + colSchedule.Width + colLate.Width + colUndertime.Width + colCheck.Width;
            int width = lvEmployees.ClientSize.Width - others - SystemInformation.VerticalScrollBarWidth;
            colName.Width = Math.Max(140, width);
        }

        private void txtSearch_TextChanged(object? sender, EventArgs e)
        {
            ShowEmployeeList();
        }

        /// <summary>This is the list above the names: show everyone, or only the ones that need checking, were typed in, or have a leave.</summary>
        private void cboFilter_SelectedIndexChanged(object? sender, EventArgs e)
        {
            ShowEmployeeList();
            if (cboFilter.SelectedIndex > 0 && lvEmployees.Items.Count == 0 && _sheets.Count > 0)
                lblStatus.Text = "No employee matches \"" + cboFilter.Text.Trim() + "\".";
        }

        private void btnTickAll_Click(object? sender, EventArgs e)
        {
            SetAllTicks(true);
        }

        private void btnUntickAll_Click(object? sender, EventArgs e)
        {
            SetAllTicks(false);
        }

        /// <summary>This is where every employee that is currently shown in the list is ticked or unticked.</summary>
        private void SetAllTicks(bool ticked)
        {
            _updating = true;
            foreach (ListViewItem item in lvEmployees.Items)
            {
                var sheet = (DtrSheet)item.Tag!;
                item.Checked = ticked;
                if (ticked) _ticked.Add(sheet.EmployeeId);
                else _ticked.Remove(sheet.EmployeeId);
            }
            _updating = false;
            ShowCounts();
        }

        /// <summary>This is where the selected employee's DTR is shown and the buttons are switched on or off.</summary>
        private void ShowCurrentSheet()
        {
            DtrSheet? sheet = GetCurrentSheet();
            LeaveCard? card = null;
            if (sheet != null)
                _cards.TryGetValue(sheet.EmployeeId, out card);

            // This is the certificate of this employee, the same as Print / Save To > Certificate of Leave Credits makes today.
            LeaveCertificate? certificate = _showCertificate && card != null
                ? LeaveCertificate.Make(card, DateTime.Today).SignedBy(_settings) : null;
            preview.ShowPages(sheet, card, certificate,
                              _showCertificate ? PreviewPage.Certificate : _showLeave ? PreviewPage.Leave : PreviewPage.Dtr);

            // This is the yellow bar: days a person should double-check, days typed in by hand, and days on leave.
            var notices = new List<string>();
            if (certificate != null && !certificate.IsComplete)
                notices.Add("Certificate: the Vacation / Sick Leave balance is not known yet, so the box is empty to fill in by hand. " +
                            "Type the balance brought forward once in Edit Leave to have it filled in.");
            if (sheet != null && sheet.DaysToCheck.Count > 0)
                notices.Add("Please check (use Edit DTR or Edit Leave to fix):   " +
                    string.Join(";   ", sheet.DaysToCheck.Select(d => "Day " + d.Day + " - " + d.Warning).ToArray()));
            if (sheet != null && sheet.EditedDays.Count > 0)
                notices.Add("Typed in by hand:   " +
                    string.Join(", ", sheet.EditedDays.Select(d => "Day " + d.Day).ToArray()));
            if (sheet != null && sheet.SuspendedFromDays.Count > 0)
                notices.Add("Work suspended:   " +
                    string.Join(", ", sheet.SuspendedFromDays.Select(d => "Day " + d.Day + " from " +
                        DtrSettings.FormatTime(d.SuspendedFrom!.Value)).ToArray()));
            if (sheet != null && sheet.CalendarHolidays.Count > 0)
                notices.Add("Holidays:   " +
                    string.Join(", ", sheet.CalendarHolidays.Select(d => "Day " + d.Day + " (" + d.HolidayName + ")").ToArray()));
            if (sheet != null && sheet.AdjustedDays.Count > 0)
                notices.Add("Late / Undertime typed in by hand:   " +
                    string.Join(";   ", sheet.AdjustedDays.Select(d => "Day " + d.Day + " - " + AdjustedText(d)).ToArray()));
            if (sheet != null && sheet.LeaveDays.Count > 0)
                notices.Add("Filed in Edit Leave:   " +
                    string.Join(", ", sheet.LeaveDays.Select(d => "Day " + d.Day + " (" + LeaveKinds.Get(d.Leave).Code +
                        (d.LeavePart == LeavePart.WholeDay ? "" : " 1/2") + ")").ToArray()));

            // This is the reminders: Forced Leave not used yet (from November), CTO / Solo Parent Leave about to expire.
            if (sheet != null && card != null)
                foreach (string reminder in LeaveReminders.For(card, _cto, _leaves, DateTime.Today))
                    notices.Add("Reminder - " + reminder);

            lblNotice.Text = string.Join("        ", notices.ToArray());
            toolTip.SetToolTip(lblNotice, string.Join("\n\n", notices.ToArray()).Replace(";   ", "\n"));
            lblNotice.Visible = notices.Count > 0;

            // This is the "Work schedule" list and the status bar: they show the schedule of this employee.
            _updating = true;
            cboSchedule.SelectedIndex = sheet == null ? -1 : sheet.ScheduleIndex;
            _updating = false;
            cboSchedule.Enabled = sheet != null;
            btnEdit.Enabled = sheet != null;
            btnLeave.Enabled = sheet != null;

            if (sheet != null)
            {
                WorkSchedule schedule = _settings.GetSchedule(sheet.ScheduleIndex);
                int monday = schedule.GraceOn(DayOfWeek.Monday);
                lblRules.Text = schedule.Description + "   |   Grace period " + schedule.GraceMinutes + " min" +
                                (monday == schedule.GraceMinutes ? "" : monday == 0 ? " (none on Mondays)" : " (" + monday + " min on Mondays)");
            }
            else
            {
                // This is all the schedules, short: "A  7-6 Mon to Thu   |   B  8-5 Mon to Fri ..."
                lblRules.Text = string.Join("   |   ",
                    _settings.Schedules.Select(x => x.Letter + "  " + x.ShortText + " " + x.WorkDaysText).ToArray());
            }

            // This is the words in the middle of the empty preview.
            if (_log == null)
                preview.SetEmptyMessage("Start with Step 1",
                    "Click \"Open .dat File...\" at the top left,\nor drag the .dat file from the biometrics device into this window.");
            else if (_sheets.Count > 0)
                preview.SetEmptyMessage("No employee matches your search",
                    "Clear the search box to see everyone again.\nSomeone who did not scan at all this month? Tick the box below the list.");
            else
                preview.SetEmptyMessage("No scans in this month", "Choose another month in Step 2.");

            bool hasSheet = sheet != null;
            btnPrint.Enabled = hasSheet;
            btnSave.Enabled = hasSheet;
            btnTickAll.Enabled = lvEmployees.Items.Count > 0;
            btnUntickAll.Enabled = lvEmployees.Items.Count > 0;
            btnSummary.Enabled = _sheets.Count > 0;
            btnReports.Enabled = _sheets.Count > 0;
            ShowCounts();
        }

        private void ShowCounts()
        {
            if (_sheets.Count == 0)
            {
                lblEmployees.Text = "Employees";
                return;
            }

            string text = "Employees (" + _sheets.Count + ")";
            if (_ticked.Count > 0)
                text += "  -  " + _ticked.Count + " ticked";
            lblEmployees.Text = text;
        }

        // This is the work schedule, Edit DTR and Edit Leave ================================================================

        /// <summary>This is when the user picked a schedule (A to D) in the list above the DTR.</summary>
        private void cboSchedule_SelectionChangeCommitted(object? sender, EventArgs e)
        {
            DtrSheet? current = GetCurrentSheet();
            int chosen = cboSchedule.SelectedIndex;
            if (_updating || current == null || chosen < 0)
                return;

            // This is for ticked employees: ask whether the change is for this one, the ticked ones or everyone.
            var who = new List<DtrSheet> { current };
            if (_ticked.Count > 0)
                who = AskWhichSheets("Whose work schedule do you want to change?");

            foreach (DtrSheet sheet in who)
                _names.SetSchedule(sheet.EmployeeId, chosen);

            if (who.Count > 0)
                SaveEmployeeList();

            BuildSheets(); // This is also what puts the list back to the saved schedule if the question was cancelled
        }

        private void SaveEmployeeList()
        {
            try
            {
                _names.SaveDefault();
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                MessageBox.Show(this,
                    "The change is used now, but it could not be saved for next time.\n\n" + ex.Message,
                    "Dat2DTR", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>This is the text for the yellow notice, e.g. "undertime 1h -> none (Work suspended at 3:00 PM)".</summary>
        private static string AdjustedText(DtrDay day)
        {
            if (day.NoLateUndertime)
                return "No Late / Undertime (not counted: late " + DurationOrNone(day.ComputedLateMinutes) + ", undertime " +
                       DurationOrNone(day.ComputedUndertimeMinutes) + ")" + (day.AdjustReason.Length > 0 ? " (" + day.AdjustReason + ")" : "");
            var parts = new List<string>();
            if (day.LateMinutes != day.ComputedLateMinutes)
                parts.Add("late " + DurationOrNone(day.ComputedLateMinutes) + " -> " + DurationOrNone(day.LateMinutes));
            if (day.UndertimeMinutes != day.ComputedUndertimeMinutes)
                parts.Add("undertime " + DurationOrNone(day.ComputedUndertimeMinutes) + " -> " + DurationOrNone(day.UndertimeMinutes));
            return string.Join(", ", parts.ToArray()) + (day.AdjustReason.Length > 0 ? " (" + day.AdjustReason + ")" : "");
        }

        private static string DurationOrNone(int minutes)
        {
            return minutes == 0 ? "none" : DtrSheet.FormatDuration(minutes);
        }

        /// <summary>This is where the table opens in which the times of the shown employee can be typed in or corrected.</summary>
        private void btnEdit_Click(object? sender, EventArgs e)
        {
            // This is "Next >" and "< Previous" in the window: they save and go on with the employee below / above.
            DtrSheet? current = GetCurrentSheet();
            while (current != null && _log != null)
            {
                string id = current.EmployeeId;
                int index = IndexInList(id);
                string? above = NeighbourId(index, -1), below = NeighbourId(index, +1);
                int move = EditDtrOf(current, _log);
                if (move == 0) break;
                current = MoveAfterEdit(id, index, move > 0 ? below : above, move);
            }
        }

        /// <summary>This is Edit DTR for one employee. Returns -1 / +1 to go on with the previous / next one, else 0.</summary>
        private int EditDtrOf(DtrSheet current, AttendanceLog log)
        {
            WorkSchedule schedule = _settings.GetSchedule(current.ScheduleIndex);

            // This is what the biometrics file says, without anything typed in by hand.
            DtrSheet fromFile = DtrBuilder.Build(log, current.EmployeeId, current.EmployeeName, current.Month,
                                                 schedule, _settings, null, _leaves);

            int move;
            using (var dialog = new EditTimesForm(fromFile, _corrections, _leaves, schedule, _settings,
                                                  log.CoveredFrom(current.Month), log.CoveredThrough(current.Month)))
            {
                dialog.SetNavigation(HasNeighbour(current.EmployeeId, -1), HasNeighbour(current.EmployeeId, +1));
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return 0;
                move = dialog.GetMove();

                foreach (var change in dialog.GetChanges())
                    _corrections.Set(current.EmployeeId, change.Key, change.Value);
                foreach (var change in dialog.GetAdjustments())
                    _corrections.SetAdjustment(current.EmployeeId, change.Key, change.Value);
            }

            try
            {
                _corrections.SaveDefault();
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                MessageBox.Show(this,
                    "The typed times are used now, but they could not be saved for next time.\n\n" + ex.Message,
                    "Dat2DTR", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            BuildSheets();
            lblStatus.Text = "Times updated for " + current.NameLine;
            return move;
        }

        /// <summary>This is true when the list (as it is shown now) has an employee above (-1) or below (+1) this one.</summary>
        private bool HasNeighbour(string employeeId, int move)
        {
            int index = IndexInList(employeeId);
            return index >= 0 && index + move >= 0 && index + move < lvEmployees.Items.Count;
        }

        private int IndexInList(string employeeId)
        {
            for (int i = 0; i < lvEmployees.Items.Count; i++)
                if (((DtrSheet)lvEmployees.Items[i].Tag!).EmployeeId == employeeId)
                    return i;
            return -1;
        }

        /// <summary>This is the ID of the employee above (-1) or below (+1) the given place in the list, or null.</summary>
        private string? NeighbourId(int index, int move)
        {
            int other = index + move;
            if (index < 0 || other < 0 || other >= lvEmployees.Items.Count) return null;
            return ((DtrSheet)lvEmployees.Items[other].Tag!).EmployeeId;
        }

        /// <summary>
        /// This is for after Next / Previous (the list was made again by then): selects the employee that was below / above,
        /// and returns the DTR, or null at the end of the list. If the edited employee is not in the list any
        /// more (for example the filter "Needs checking" and the day was fixed), the one now at its place is next.
        /// </summary>
        private DtrSheet? MoveAfterEdit(string editedId, int oldIndex, string? targetId, int move)
        {
            int index = targetId == null ? -1 : IndexInList(targetId);
            if (index < 0 && IndexInList(editedId) < 0 && oldIndex >= 0)
                index = move > 0 ? oldIndex : oldIndex - 1;   // This is when the edited one dropped out of the list
            if (index < 0 || index >= lvEmployees.Items.Count)
            {
                lblStatus.Text = move > 0 ? "That was the last employee in the list." : "That was the first employee in the list.";
                return null;
            }
            ListViewItem item = lvEmployees.Items[index];
            item.Selected = true;
            item.Focused = true;
            item.EnsureVisible();
            return GetCurrentSheet();
        }

        /// <summary>This is where the employee above (-1) or below (+1) in the list is selected; returns the DTR, or null at the end.</summary>
        private DtrSheet? SelectNeighbour(string employeeId, int move)
        {
            int index = IndexInList(employeeId);
            return MoveAfterEdit(employeeId, index, NeighbourId(index, move), move);
        }

        /// <summary>This is where the table opens in which a leave can be filed for the shown employee.</summary>
        private void btnLeave_Click(object? sender, EventArgs e)
        {
            DtrSheet? current = GetCurrentSheet();
            AttendanceLog? log = _log;
            if (current == null || log == null)
                return;

            // This is for employees ticked: this one only, or the same leave for all the ticked ones?
            List<DtrSheet> ticked = _sheets.Where(s => _ticked.Contains(s.EmployeeId)).ToList();
            if (ticked.Count > 1 || (ticked.Count == 1 && ticked[0] != current))
            {
                string currentName = current.EmployeeName.Length > 0 ? current.EmployeeName : "ID " + current.EmployeeId;
                DtrChoice choice;
                using (var ask = new ChoiceDialog("Whose leave do you want to edit?", currentName, ticked.Count, _sheets.Count, false))
                {
                    if (ask.ShowDialog(this) != DialogResult.OK)
                        return;
                    choice = ask.GetChoice();
                }
                if (choice == DtrChoice.Ticked)
                {
                    EditTickedLeave(ticked, current.Month);
                    return;
                }
            }

            // This is "Next >" and "< Previous" in the window: they save and go on with the employee below / above.
            DtrSheet? next = current;
            while (next != null)
            {
                string id = next.EmployeeId;
                int index = IndexInList(id);
                string? above = NeighbourId(index, -1), below = NeighbourId(index, +1);
                int move = EditLeaveOf(next, log);
                if (move == 0) break;
                next = MoveAfterEdit(id, index, move > 0 ? below : above, move);
            }
        }

        /// <summary>This is Edit Leave for one employee. Returns -1 / +1 to go on with the previous / next one, else 0.</summary>
        private int EditLeaveOf(DtrSheet current, AttendanceLog log)
        {
            WorkSchedule schedule = _settings.GetSchedule(current.ScheduleIndex);

            // This is the DTR without any leave: what the person scanned (or what was typed in by hand).
            DtrSheet attendance = DtrBuilder.Build(log, current.EmployeeId, current.EmployeeName, current.Month,
                                                   schedule, _settings, _corrections, null);

            // This is how the window asks for the computation while the user is choosing (the window works on copies).
            Func<LeaveStore, LeaveLedger, CtoStore, LeaveCard> compute = (leaves, ledger, cto) =>
            {
                DtrSheet sheet = DtrBuilder.Build(log, current.EmployeeId, current.EmployeeName, current.Month,
                                                  schedule, _settings, _corrections, leaves);
                return LeaveCalculator.Build(sheet, schedule, leaves, ledger, cto, log.CoveredFrom(current.Month), log.CoveredThrough(current.Month));
            };

            int move;
            using (var dialog = new EditLeaveForm(attendance, schedule, _leaves, _ledger, _cto, compute))
            {
                dialog.SetNavigation(HasNeighbour(current.EmployeeId, -1), HasNeighbour(current.EmployeeId, +1));
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return 0;
                move = dialog.GetMove();

                _leaves = dialog.GetLeaves();
                _ledger = dialog.GetLedger();
                _cto = dialog.GetCto();
            }

            SaveLeaveFiles(true);

            _showLeave = true;      // This is to show the result right away
            _showCertificate = false;
            ShowViewButtons();
            BuildSheets();
            lblStatus.Text = "Leave updated for " + current.NameLine;
            return move;
        }

        /// <summary>
        /// This is the same leave (for example Holiday or Suspended) for a range of days, for all the
        /// ticked employees. Days that are not working days in an employee's schedule are skipped.
        /// </summary>
        private void EditTickedLeave(List<DtrSheet> ticked, DateTime month)
        {
            List<string> names = ticked.Select(s => s.EmployeeName.Length > 0 ? s.EmployeeName : "ID " + s.EmployeeId).ToList();
            DateTime from, to;
            LeaveEntry? entry;
            using (var dialog = new BulkLeaveForm(month, names, month))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;
                from = dialog.GetFrom();
                to = dialog.GetTo();
                entry = dialog.GetEntry();
            }

            // This is for days that already have something else filed: replace them, or leave them as they are?
            int different = 0;
            foreach (DtrSheet sheet in ticked)
            {
                WorkSchedule schedule = _settings.GetSchedule(sheet.ScheduleIndex);
                for (DateTime date = from; date <= to; date = date.AddDays(1))
                {
                    LeaveEntry? existing = _leaves.Get(sheet.EmployeeId, date);
                    if (schedule.IsWorkDay(date.DayOfWeek) && entry != null && existing != null && !existing.SameAs(entry))
                        different++;
                }
            }

            bool replace = true;
            if (different > 0)
            {
                DialogResult answer = MessageBox.Show(this,
                    different + (different == 1 ? " day already has" : " days already have") +
                    " another leave filed for one of the ticked employees.\n\n" +
                    "Yes = replace it with the new one\nNo = keep what is filed on " + (different == 1 ? "that day" : "those days"),
                    "Edit Leave", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (answer == DialogResult.Cancel)
                    return;
                replace = answer == DialogResult.Yes;
            }

            var skipped = new List<KeyValuePair<int, DateTime>>();
            int changed = ApplyLeaveToMany(_leaves, ticked.Select(s => s.EmployeeId).ToList(),
                ticked.Select(s => _settings.GetSchedule(s.ScheduleIndex)).ToList(), from, to, entry, replace, _cto, skipped);

            // This is the list of employees who did not have enough CTO / Solo Parent Leave: those days were not filed.
            if (skipped.Count > 0 && entry != null)
            {
                string name = entry.Type == LeaveType.SoloParent ? "Solo Parent Leave" : "CTO";
                var lines = skipped.GroupBy(k => k.Key).Select(g =>
                    "- " + names[g.Key] + ":  day" + (g.Count() == 1 ? " " : "s ") +
                    string.Join(", ", g.Select(k => k.Value.Day.ToString(System.Globalization.CultureInfo.InvariantCulture)).ToArray())).ToList();
                MessageBox.Show(this,
                    "Not filed - there was not enough " + name + " given for:\n\n" + string.Join("\n", lines.Take(15).ToArray()) +
                    (lines.Count > 15 ? "\n... and " + (lines.Count - 15) + " more" : "") +
                    "\n\nAdd the " + name + " given in Edit Leave (\"" + (entry.Type == LeaveType.SoloParent ? "Solo Parent given..." : "CTO given...") +
                    "\") first, then file it again.",
                    "Edit Leave", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            SaveLeaveFiles(true);
            _showLeave = false;     // This is so a holiday or a suspension is seen on the DTR
            _showCertificate = false;
            ShowViewButtons();
            BuildSheets();
            lblStatus.Text = (entry == null ? "Leave removed" : (entry.Type == LeaveType.WorkFromHome ? "Work from home" : entry.Kind.Name) +
                             (entry.From.HasValue ? " from " + DtrSettings.FormatTime(entry.From.Value) : "") + " filed") +
                             " for " + ticked.Count + " employees (" + changed + (changed == 1 ? " day" : " days") + " changed)";
        }

        /// <summary>
        /// This is where one leave is filed (or what is filed is removed, when entry is null) on every working day
        /// from..to for each of the employees. Returns how many days were changed.
        /// </summary>
        /// <param name="replace">This is false to keep a different leave that is already filed on a day.</param>
        /// <param name="given">This is the CTO / Solo Parent Leave given, to check the balance before filing it (null = no check).</param>
        /// <param name="skipped">This is where the days not filed for lack of balance are listed (employee number, date), or null.</param>
        internal static int ApplyLeaveToMany(LeaveStore leaves, List<string> ids, List<WorkSchedule> schedules,
                                             DateTime from, DateTime to, LeaveEntry? entry, bool replace,
                                             CtoStore? given, List<KeyValuePair<int, DateTime>>? skipped)
        {
            int changed = 0;
            for (int n = 0; n < ids.Count; n++)
            {
                for (DateTime date = from.Date; date <= to.Date; date = date.AddDays(1))
                {
                    if (!schedules[n].IsWorkDay(date.DayOfWeek))
                        continue;
                    LeaveEntry? existing = leaves.Get(ids[n], date);
                    if (entry == null ? existing == null : entry.SameAs(existing))
                        continue;
                    if (entry != null && existing != null && !replace)
                        continue;
                    if (entry != null && given != null && !CtoCalculator.CanFile(given, leaves, ids[n], date, entry))
                    {
                        if (skipped != null) skipped.Add(new KeyValuePair<int, DateTime>(n, date));
                        continue;
                    }
                    leaves.Set(ids[n], date, entry);
                    changed++;
                }
            }
            return changed;
        }

        /// <summary>This is where the filed leaves and the remembered months are saved.</summary>
        /// <param name="tellUser">This is true to show a message when the files cannot be written.</param>
        private void SaveLeaveFiles(bool tellUser)
        {
            // This is for saved files that could not be read when the program started (see Form1_Load).
            // Writing now would replace them with an almost empty list, so nothing is written.
            if (_leaves.LoadFailed || _ledger.LoadFailed || _cto.LoadFailed)
            {
                if (tellUser)
                    MessageBox.Show(this,
                        "The leave is used now, but it is NOT saved.\n\n" +
                        "The saved leaves could not be read when Dat2DTR started (the file was open in another program). " +
                        "Close that program, start Dat2DTR again and enter the leave once more.",
                        "Dat2DTR", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _leaves.SaveDefault();
                _ledger.SaveDefault();
                _cto.SaveDefault();
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                if (tellUser)
                    MessageBox.Show(this,
                        "The leave is used now, but it could not be saved for next time.\n\n" + ex.Message,
                        "Dat2DTR", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // This is the DTR or Computation of Leaves buttons ================================================================

        private void btnViewDtr_Click(object? sender, EventArgs e)
        {
            _showLeave = false;
            _showCertificate = false;
            ShowViewButtons();
            ShowCurrentSheet();
        }

        private void btnViewLeave_Click(object? sender, EventArgs e)
        {
            _showLeave = true;
            _showCertificate = false;
            ShowViewButtons();
            ShowCurrentSheet();
        }

        private void btnViewCertificate_Click(object? sender, EventArgs e)
        {
            _showLeave = false;
            _showCertificate = true;
            ShowViewButtons();
            ShowCurrentSheet();
        }

        /// <summary>This is where the three buttons above the preview are made to look like tabs: the chosen one is navy.</summary>
        private void ShowViewButtons()
        {
            StyleViewButton(btnViewDtr, !_showLeave && !_showCertificate);
            StyleViewButton(btnViewLeave, _showLeave && !_showCertificate);
            StyleViewButton(btnViewCertificate, _showCertificate);
            btnCertificateDetails.Visible = _showCertificate;
        }

        private static void StyleViewButton(Button button, bool chosen)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = chosen ? Theme.Navy : Theme.Line;
            button.FlatAppearance.MouseOverBackColor = chosen ? Theme.AccentHover : Theme.Highlight;
            button.FlatAppearance.MouseDownBackColor = chosen ? Theme.AccentHover : Theme.Highlight;
            button.BackColor = chosen ? Theme.Navy : Color.White;
            button.ForeColor = chosen ? Color.White : Theme.Navy;
            button.Cursor = Cursors.Hand;
        }

        /// <summary>This is where the user is asked what to print or save: the DTR, the Computation of Leaves, or both.</summary>
        private DtrPages AskWhichPages(string question, bool offerDatFile = false)
        {
            using (var dialog = new PagesDialog(question, offerDatFile, EditCertificateDetails))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return DtrPages.Cancel;
                return dialog.GetChoice();
            }
        }

        /// <summary>
        /// This is "Edit Certificate Details...": the names and positions printed on the Certificate of Leave Credits
        /// ("Prepared by" and "Certified correct"). They are saved with the settings, so they stay for next time.
        /// </summary>
        private void EditCertificateDetails(IWin32Window owner)
        {
            // This is the certificate of the employee on screen, for the live preview (a sample one when there is none).
            DtrSheet? current = GetCurrentSheet();
            LeaveCertificate? shown = current != null && _cards.ContainsKey(current.EmployeeId)
                ? LeaveCertificate.Make(_cards[current.EmployeeId], DateTime.Today) : null;
            using (var dialog = new CertificateDetailsForm(_settings.PreparedByName, _settings.PreparedByPosition,
                                                           _settings.CertifiedByName, _settings.CertifiedByPosition, shown))
            {
                if (dialog.ShowDialog(owner) != DialogResult.OK)
                    return;
                string[] values = dialog.GetValues();
                _settings.PreparedByName = values[0];
                _settings.PreparedByPosition = values[1];
                _settings.CertifiedByName = values[2];
                _settings.CertifiedByPosition = values[3];
                _settings.Save();
                lblStatus.Text = "Certificate details saved.";
            }
        }

        /// <summary>This is "DTR", "Computation of Leaves" or "DTR and Computation of Leaves" - for file names and messages.</summary>
        private static string PagesName(DtrPages pages)
        {
            if (pages == DtrPages.LeaveOnly) return "Computation of Leaves";
            if (pages == DtrPages.Certificate) return "Certificate of Leave Credits";
            if (pages == DtrPages.Both) return "DTR and Computation of Leaves";
            return "DTR";
        }

        /// <summary>This is the Computation of Leaves of each of these DTRs, in the same order.</summary>
        private List<LeaveCard> GetCards(List<DtrSheet> sheets)
        {
            var cards = new List<LeaveCard>();
            foreach (DtrSheet sheet in sheets)
                cards.Add(_cards[sheet.EmployeeId]);
            return cards;
        }

        // This is names, office hours, help ================================================================

        /// <summary>This is where the holiday calendar (used for every employee) is opened.</summary>
        private void btnHolidays_Click(object? sender, EventArgs e)
        {
            var chosen = cboMonth.SelectedItem as MonthItem;
            int year = chosen != null ? chosen.Month.Year : DateTime.Today.Year;

            using (var dialog = new HolidaysForm(_holidays, year))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;
                _holidays = dialog.GetCalendar();
            }

            bool saved = false;
            if (_holidaysUnreadable)
            {
                MessageBox.Show(this,
                    "The holidays are used now, but they are NOT saved, because the saved holidays could not be read " +
                    "when Dat2DTR started (the file was open in another program). Close that program and start Dat2DTR again.",
                    "Dat2DTR", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                try
                {
                    _holidays.SaveDefault();
                    saved = true;
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    MessageBox.Show(this, "The holidays are used now, but they could not be saved for next time.\n\n" + ex.Message,
                        "Dat2DTR", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            BuildSheets();
            lblStatus.Text = (saved ? "Holidays saved (" : "Holidays used, but NOT saved (") + _holidays.Count + ")";
        }

        private void btnNames_Click(object? sender, EventArgs e)
        {
            IList<string> ids = _log == null ? new List<string>() : _log.GetAllEmployeeIds();
            string safety = "";
            Func<IList<string>, bool> backUpFirst = deleted =>
            {
                // This is the backup of everything, made just before employees are deleted, so it can be undone.
                safety = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "Dat2DTR-before-delete-" + DateTime.Now.ToString("yyyy-MM-dd-HHmm", System.Globalization.CultureInfo.InvariantCulture) + ".zip");
                try
                {
                    DataBackup.Create(safety);
                    return true;
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    MessageBox.Show(this, "The backup before deleting could not be made, so nothing was deleted or saved.\n\n" + ex.Message,
                        "Employee Names", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            };
            using (var dialog = new EmployeeNamesForm(_names, ids, _settings, backUpFirst))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;
                List<string> deleted = dialog.GetDeletedIds();
                if (deleted.Count > 0)
                    EraseEmployees(deleted, safety);
                else
                    BuildSheets();
            }
        }

        /// <summary>
        /// This is where everything about deleted employees is erased (Employee Names, Delete): their leaves, leave balances,
        /// CTO, Solo Parent Leave, times typed in, Late / Undertime / Flexi-Time and kept scans. (Their names were already
        /// removed by the Employee Names window.) A backup was made just before, in "safety".
        /// </summary>
        private void EraseEmployees(List<string> ids, string safety)
        {
            foreach (string id in ids)
            {
                _names.Remove(id);
                _leaves.RemoveEmployee(id);
                _ledger.RemoveEmployee(id);
                _cto.RemoveEmployee(id);
                _corrections.RemoveEmployee(id);
                _ticked.RemoveWhere(t => EmployeeDirectory.SameId(t, id));
            }

            string problem = "";
            try
            {
                Cursor = Cursors.WaitCursor;
                _names.SaveDefault();
                _corrections.SaveDefault();
                ScanArchive.RemoveEmployees(ids, ScanArchive.DefaultFolder);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                problem = ex.Message;
            }
            finally
            {
                Cursor = Cursors.Default;
            }
            SaveLeaveFiles(true);

            // This is the kept scans read again (without the deleted employees); if nothing is kept, the list is emptied.
            AttendanceLog? kept = null;
            try { kept = ScanArchive.Load(); }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException) { problem = ex.Message; }
            if (kept != null)
                ShowKeptScans(false);
            else if (_log != null)
                BuildSheets();

            lblStatus.Text = (ids.Count == 1 ? "1 employee" : ids.Count + " employees") + " deleted. Backup made first: " + safety;
            if (problem.Length > 0)
                MessageBox.Show(this, "The employees were deleted, but not everything could be saved:\n\n" + problem +
                                "\n\nThe backup made just before is here:\n" + safety, "Employee Names", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void btnSettings_Click(object? sender, EventArgs e)
        {
            using (var dialog = new SettingsForm(_settings))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                _settings = dialog.GetSettings();
                _settings.Save();
                preview.SetPaper(_settings.Paper);
                ShowRules();
                BuildSheets();
            }
        }

        private void btnHelp_Click(object? sender, EventArgs e)
        {
            using (var dialog = new HelpForm(_settings))
                dialog.ShowDialog(this);
        }

        /// <summary>This is the Leave Summary of everyone in the list for the chosen month.</summary>
        private void btnSummary_Click(object? sender, EventArgs e)
        {
            if (_sheets.Count == 0)
                return;
            List<LeaveSummaryRow> rows = LeaveSummary.Make(_sheets, _cards, _cto, _leaves, DateTime.Today);
            using (var dialog = new LeaveSummaryForm(_sheets[0].MonthText, rows, _settings.Paper, _settings.LastFolder))
            {
                dialog.ShowDialog(this);
                if (dialog.GetSavedFolder().Length > 0)
                {
                    _settings.LastFolder = dialog.GetSavedFolder();
                    _settings.Save();
                }
            }
        }

        /// <summary>
        /// This is "Reports...": late and no Time-In on Mondays, both together, and late and undertime in the month,
        /// for everyone in the list (the whole office) in the chosen month.
        /// </summary>
        private void btnReports_Click(object? sender, EventArgs e)
        {
            if (_sheets.Count == 0)
                return;
            List<AttendanceReport> reports = AttendanceReports.MakeAll(_sheets, _settings);
            using (var dialog = new ReportsForm(_sheets[0].MonthText, reports, _settings.Paper, _settings.LastFolder))
            {
                dialog.ShowDialog(this);
                if (dialog.GetSavedFolder().Length > 0)
                {
                    _settings.LastFolder = dialog.GetSavedFolder();
                    _settings.Save();
                }
            }
        }

        /// <summary>This is "Backup / Restore...": back up the saved files into one .zip, or put a backup back.</summary>
        private void btnBackup_Click(object? sender, EventArgs e)
        {
            BackupChoice choice;
            using (var dialog = new BackupForm())
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;
                choice = dialog.GetChoice();
            }
            if (choice == BackupChoice.BackUp) BackUpNow();
            else if (choice == BackupChoice.Restore) RestoreBackup();
        }

        /// <summary>This is where everything is saved in one .zip, with today's date in its name.</summary>
        private void BackUpNow()
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Title = "Back up Dat2DTR";
                dialog.Filter = "Dat2DTR backup (*.zip)|*.zip";
                dialog.FileName = DataBackup.SuggestedName(DateTime.Today);
                dialog.AddExtension = true;
                dialog.OverwritePrompt = true;
                dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                string path = dialog.FileName;
                if (!path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    path += ".zip";
                try
                {
                    Cursor = Cursors.WaitCursor;
                    int count = DataBackup.Create(path);
                    lblStatus.Text = "Backup saved: " + path;
                    MessageBox.Show(this, "The backup was saved (" + count + (count == 1 ? " file" : " files") + "):\n\n" + path +
                        "\n\nKeep a copy somewhere else too, for example on a USB drive or in Google Drive.",
                        "Backup / Restore", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    MessageBox.Show(this, "The backup could not be saved.\n\n" + ex.Message,
                        "Backup / Restore", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                finally
                {
                    Cursor = Cursors.Default;
                }
            }
        }

        /// <summary>
        /// This is where a backup is put back. What is on this computer now is saved first in Documents
        /// ("Dat2DTR-before-restore-...zip"), so the restore can be undone by restoring that file.
        /// </summary>
        private void RestoreBackup()
        {
            string path;
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Restore a Dat2DTR backup";
                dialog.Filter = "Dat2DTR backup (*.zip)|*.zip";
                dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;
                path = dialog.FileName;
            }

            DataBackup.Contents contents;
            try
            {
                contents = DataBackup.Read(path);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidDataException)
            {
                MessageBox.Show(this, "The file could not be read as a backup.\n\n" + ex.Message,
                    "Backup / Restore", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!contents.IsBackup)
            {
                MessageBox.Show(this, "This .zip file is not a Dat2DTR backup, so nothing was changed.",
                    "Backup / Restore", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string safety = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Dat2DTR-before-restore-" + DateTime.Now.ToString("yyyy-MM-dd-HHmm", System.Globalization.CultureInfo.InvariantCulture) + ".zip");
            DialogResult answer = MessageBox.Show(this,
                "Restore this backup?\n\n" + Path.GetFileName(path) +
                (contents.Made.Length > 0 ? "\nMade on " + contents.Made + (contents.Computer.Length > 0 ? " on " + contents.Computer : "") : "") +
                "\n\nThe names, times typed in, leaves, balances, CTO, Solo Parent Leave, holidays and office hours on this computer " +
                "are replaced by the ones in the backup.\n\nWhat is here now is saved first, here:\n" + safety,
                "Backup / Restore", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            if (answer != DialogResult.Yes)
                return;

            try
            {
                Cursor = Cursors.WaitCursor;
                DataBackup.Create(safety);
                int count = DataBackup.Restore(path);
                ReloadAllData();
                lblStatus.Text = "Backup restored: " + Path.GetFileName(path);
                MessageBox.Show(this, "The backup was restored (" + count + (count == 1 ? " file" : " files") + ").\n\n" +
                    "What was here before is saved in:\n" + safety,
                    "Backup / Restore", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidDataException)
            {
                MessageBox.Show(this, "The backup could not be restored. If one of the files is open in Excel, close it and try again.\n\n" +
                    ex.Message + "\n\nWhat was here before is saved in:\n" + safety,
                    "Backup / Restore", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ReloadAllData();
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        /// <summary>
        /// This is the Certificate of Leave Credits of each of these employees. When some balances are not known
        /// yet, the user is asked first (those certificates have empty boxes). Returns null when the user stops.
        /// </summary>
        private List<LeaveCertificate>? GetCertificates(List<DtrSheet> sheets)
        {
            List<LeaveCertificate> certificates = sheets.Select(s => LeaveCertificate.Make(_cards[s.EmployeeId], DateTime.Today).SignedBy(_settings)).ToList();
            int unknown = certificates.Count(c => !c.IsComplete);
            if (unknown > 0)
            {
                DialogResult answer = MessageBox.Show(this,
                    (certificates.Count == 1 ? "The" : unknown + " of the " + certificates.Count) +
                    " leave balance" + (unknown == 1 ? " is" : "s are") + " not known yet, so the VL / SL box" + (unknown == 1 ? " is" : "es are") +
                    " left empty to fill in by hand.\n\n(Type the balance brought forward once in Edit Leave to have it filled in.)\n\nContinue?",
                    "Certificate of Leave Credits", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (answer != DialogResult.Yes)
                    return null;
            }
            return certificates;
        }

        private void Form1_KeyDown(object? sender, KeyEventArgs e)
        {
            if (!e.Control) return;

            if (e.KeyCode == Keys.O) { e.SuppressKeyPress = true; btnOpen.PerformClick(); }
            else if (e.KeyCode == Keys.P) { e.SuppressKeyPress = true; btnPrint.PerformClick(); }
            else if (e.KeyCode == Keys.S) { e.SuppressKeyPress = true; btnSave.PerformClick(); }
        }

        // This is step 3: print and save ================================================================

        /// <summary>
        /// This is where the user is asked whose DTR is wanted (this one / the ticked ones / everyone).
        /// Returns an empty list when the user cancels.
        /// </summary>
        private List<DtrSheet> AskWhichSheets(string question)
        {
            var result = new List<DtrSheet>();
            DtrSheet? current = GetCurrentSheet();
            if (current == null)
                return result;

            if (_sheets.Count == 1)
            {
                result.Add(current);
                return result;
            }

            List<DtrSheet> ticked = _sheets.Where(s => _ticked.Contains(s.EmployeeId)).ToList();
            string currentName = current.EmployeeName.Length > 0 ? current.EmployeeName : "ID " + current.EmployeeId;

            using (var dialog = new ChoiceDialog(question, currentName, ticked.Count, _sheets.Count))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return result;

                switch (dialog.GetChoice())
                {
                    case DtrChoice.CurrentOnly: result.Add(current); break;
                    case DtrChoice.Ticked: result.AddRange(ticked); break;
                    case DtrChoice.All: result.AddRange(_sheets); break;
                }
            }
            return result;
        }

        private void btnPrint_Click(object? sender, EventArgs e)
        {
            if (GetCurrentSheet() == null)
                return;

            DtrPages what = AskWhichPages("What do you want to print?");
            if (what == DtrPages.Cancel)
                return;

            List<DtrSheet> sheets = AskWhichSheets("Whose " + PagesName(what) + " do you want to print?");
            if (sheets.Count == 0)
                return;

            if (PrinterSettings.InstalledPrinters.Count == 0)
            {
                MessageBox.Show(this,
                    "No printer is installed on this computer.\n\nYou can use \"Save To...\" to save the DTR as a PDF instead.",
                    "Dat2DTR", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                using (var document = new PrintDocument())
                using (var dialog = new PrintDialog())
                {
                    document.DocumentName = PagesName(what) + " " + sheets[0].MonthText;
                    SelectPaper(document);

                    // This is the pages in the order they are printed: for each employee the DTR,
                    // the Computation of Leaves, or the DTR followed by the Computation of Leaves.
                    var pages = new List<object>();
                    if (what == DtrPages.Certificate)
                    {
                        List<LeaveCertificate>? certificates = GetCertificates(sheets);
                        if (certificates == null)
                            return;
                        pages.AddRange(certificates);
                    }
                    else
                    {
                        foreach (DtrSheet sheet in sheets)
                        {
                            if (what != DtrPages.LeaveOnly)
                                pages.Add(sheet);
                            if (what != DtrPages.DtrOnly)
                                pages.Add(_cards[sheet.EmployeeId]);
                        }
                    }

                    int next = 0; // This is which page is printed next
                    document.BeginPrint += (s, args) => next = 0;
                    document.PrintPage += (s, args) =>
                    {
                        PrintOnePage(args, pages[next]);
                        next++;
                        args.HasMorePages = next < pages.Count;
                    };

                    dialog.Document = document;
                    dialog.UseEXDialog = true;
                    if (dialog.ShowDialog(this) != DialogResult.OK)
                        return;

                    Cursor = Cursors.WaitCursor;
                    document.Print();
                    lblStatus.Text = sheets.Count == 1
                        ? "Sent to the printer: " + PagesName(what) + " of " + sheets[0].NameLine
                        : "Sent " + pages.Count + " pages to the printer (" + PagesName(what) + " of " + sheets.Count + " employees).";
                }
            }
            catch (Exception ex) when (ex is InvalidPrinterException || ex is System.ComponentModel.Win32Exception)
            {
                MessageBox.Show(this, "The DTR could not be printed.\n\n" + ex.Message,
                    "Dat2DTR", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        /// <summary>This is where the printer is asked for the paper size chosen in "Office Hours", if it has that size.</summary>
        private void SelectPaper(PrintDocument document)
        {
            PaperKind wanted;
            switch (_settings.Paper)
            {
                case DtrPaper.Letter: wanted = PaperKind.Letter; break;
                case DtrPaper.Long: wanted = PaperKind.Folio; break;
                case DtrPaper.Legal: wanted = PaperKind.Legal; break;
                default: wanted = PaperKind.A4; break;
            }

            foreach (PaperSize size in document.PrinterSettings.PaperSizes)
            {
                if (size.Kind == wanted)
                {
                    document.DefaultPageSettings.PaperSize = size;
                    break;
                }
            }
        }

        /// <param name="page">This is a DtrSheet (the DTR), a LeaveCard (the Computation of Leaves) or a LeaveCertificate.</param>
        private static void PrintOnePage(PrintPageEventArgs e, object page)
        {
            Graphics g = e.Graphics!;

            // This is the printer's unit: 1/100 inch; the DTR page is laid out in points (1/72 inch).
            // A printer cannot print on the very edge of the paper, and its drawing area
            // starts after that edge ("hard margin"), so we shift back by that amount.
            g.PageUnit = GraphicsUnit.Display;
            g.TranslateTransform(-e.PageSettings.HardMarginX, -e.PageSettings.HardMarginY);
            g.ScaleTransform(100f / 72f, 100f / 72f);

            double pageWidth = e.PageBounds.Width * 0.72;
            double pageHeight = e.PageBounds.Height * 0.72;

            using (var canvas = new GdiCanvas(g))
            {
                var card = page as LeaveCard;
                var certificate = page as LeaveCertificate;
                if (card != null)
                    LeavePageRenderer.Draw(canvas, card, pageWidth, pageHeight);
                else if (certificate != null)
                    CertificateRenderer.Draw(canvas, certificate, pageWidth, pageHeight);
                else
                    DtrPageRenderer.Draw(canvas, (DtrSheet)page, pageWidth, pageHeight);
            }
        }

        private void btnSave_Click(object? sender, EventArgs e)
        {
            if (GetCurrentSheet() == null)
                return;

            DtrPages what = AskWhichPages("What do you want to save?", true);
            if (what == DtrPages.Cancel)
                return;

            if (what == DtrPages.DatFile)
            {
                SaveDatFile();
                return;
            }

            List<DtrSheet> sheets = AskWhichSheets("Whose " + PagesName(what) + " do you want to save?");
            if (sheets.Count == 0)
                return;

            string who = sheets.Count == 1
                ? (sheets[0].EmployeeName.Length > 0 ? sheets[0].EmployeeName : "ID " + sheets[0].EmployeeId)
                : sheets.Count + " employees";

            using (var dialog = new SaveFileDialog())
            {
                dialog.Title = "Save the " + PagesName(what);
                dialog.Filter = what == DtrPages.Certificate
                    ? "PDF document (*.pdf)|*.pdf"
                    : "PDF document (*.pdf)|*.pdf|Excel workbook (*.xlsx)|*.xlsx";
                dialog.FileName = CleanFileName(PagesName(what) + " " + sheets[0].MonthText + " - " + who);
                dialog.AddExtension = true;
                dialog.OverwritePrompt = true;
                if (Directory.Exists(_settings.LastFolder))
                    dialog.InitialDirectory = _settings.LastFolder;

                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                // This is where the file type is decided from the ending the user chose.
                string path = dialog.FileName;
                string extension = Path.GetExtension(path).ToLowerInvariant();
                if (extension != ".pdf" && extension != ".xlsx")
                {
                    extension = dialog.FilterIndex == 2 ? ".xlsx" : ".pdf";
                    path += extension;
                }

                // This is the certificate: always a PDF, one page per employee.
                List<LeaveCertificate>? certificates = null;
                if (what == DtrPages.Certificate)
                {
                    certificates = GetCertificates(sheets);
                    if (certificates == null)
                        return;
                    if (extension != ".pdf")
                    {
                        path = Path.ChangeExtension(path, ".pdf");
                        extension = ".pdf";
                    }
                }

                try
                {
                    Cursor = Cursors.WaitCursor;
                    // This is DTR only: no leave cards.  Computation of Leaves only: no DTRs.  Both: each DTR, then its leave card.
                    List<LeaveCard>? cards = what == DtrPages.DtrOnly ? null : GetCards(sheets);
                    bool withDtr = what != DtrPages.LeaveOnly;
                    if (certificates != null)
                        DtrExporter.SaveCertificatesPdf(path, certificates, _settings.Paper);
                    else if (extension == ".xlsx")
                        DtrExporter.SaveExcel(path, sheets, _settings.Paper, cards, withDtr);
                    else
                        DtrExporter.SavePdf(path, sheets, _settings.Paper, cards, withDtr);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    Cursor = Cursors.Default;
                    MessageBox.Show(this,
                        "The file could not be saved.\n\n" +
                        "If a file with the same name is open in Excel or in a PDF reader, close it first and try again.\n\n" +
                        ex.Message,
                        "Dat2DTR", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                finally
                {
                    Cursor = Cursors.Default;
                }

                _settings.LastFolder = Path.GetDirectoryName(path) ?? "";
                _settings.Save();
                lblStatus.Text = "Saved: " + path;

                DialogResult open = MessageBox.Show(this,
                    "The " + PagesName(what) + " was saved here:\n\n" + path + "\n\nDo you want to open it now?",
                    "Dat2DTR", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (open == DialogResult.Yes)
                    OpenWithDefaultProgram(path);
            }
        }

        /// <summary>
        /// This is where the scans of the chosen employees are saved as a new .dat file, line for line the same as
        /// the device wrote them: the chosen month, the whole year, or every month kept on this computer.
        /// </summary>
        private void SaveDatFile()
        {
            var chosen = cboMonth.SelectedItem as MonthItem;
            if (_log == null || chosen == null)
                return;

            // This is which months: the one on screen, its whole year, or every month kept.
            List<DateTime> months = _log.GetMonths();
            List<DateTime> yearMonths = months.Where(m => m.Year == chosen.Month.Year).ToList();
            string monthName = MonthName(chosen.Month);
            string allText = months.Count == 0 ? "" : MonthName(months[months.Count - 1]) + " to " + MonthName(months[0]);
            int period = OptionDialog.Ask(this, "Which months do you want to save?", new[]
            {
                new KeyValuePair<string, string>("Only " + monthName, "The month on screen"),
                new KeyValuePair<string, string>("The whole year " + chosen.Month.Year,
                    yearMonths.Count + (yearMonths.Count == 1 ? " month" : " months") + " of " + chosen.Month.Year + " kept on this computer"),
                new KeyValuePair<string, string>("All months kept (" + months.Count + ")", allText)
            });
            if (period < 0)
                return;
            DateTime from = chosen.Month, to = chosen.Month.AddMonths(1).AddDays(-1);
            string periodName = monthName;
            if (period == 1)
            {
                from = new DateTime(chosen.Month.Year, 1, 1);
                to = new DateTime(chosen.Month.Year, 12, 31);
                periodName = chosen.Month.Year.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            else if (period == 2)
            {
                from = DateTime.MinValue;
                to = DateTime.MaxValue;
                periodName = allText;
            }

            List<DtrSheet> sheets = AskWhichSheets("Whose scans do you want to save?");
            if (sheets.Count == 0)
                return;

            // This is "everyone": for more than one month, also the people who scanned in other months only.
            bool everyone = sheets.Count == _sheets.Count && _sheets.Count > 1;
            List<string> ids = everyone && period > 0 ? _log.GetAllEmployeeIds() : sheets.Select(x => x.EmployeeId).ToList();

            string who = everyone ? "everyone" : sheets.Count == 1
                ? (sheets[0].EmployeeName.Length > 0 ? sheets[0].EmployeeName : "ID " + sheets[0].EmployeeId)
                : sheets.Count + " employees";

            using (var dialog = new SaveFileDialog())
            {
                dialog.Title = "Save the scans as a .dat file";
                dialog.Filter = "Biometrics file (*.dat)|*.dat";
                dialog.FileName = CleanFileName("Scans " + periodName + " - " + who);
                dialog.DefaultExt = "dat";
                dialog.AddExtension = true;
                dialog.OverwritePrompt = true;
                if (Directory.Exists(_settings.LastFolder))
                    dialog.InitialDirectory = _settings.LastFolder;

                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                string path = dialog.FileName;
                if (!path.EndsWith(".dat", StringComparison.OrdinalIgnoreCase))
                    path += ".dat";

                // This is the rule: never write over the file that is open right now.
                if (_openedPath.Length > 0 &&
                    string.Equals(Path.GetFullPath(path), Path.GetFullPath(_openedPath), StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show(this, "That is the file that is open right now. Please choose another name.",
                        "Dat2DTR", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                int lines;
                try
                {
                    lines = _log.SaveDat(path, ids, from, to);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    MessageBox.Show(this, "The file could not be saved.\n\n" + ex.Message,
                        "Dat2DTR", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _settings.LastFolder = Path.GetDirectoryName(path) ?? "";
                _settings.Save();
                lblStatus.Text = "Saved " + lines + " scans: " + path;
                MessageBox.Show(this,
                    lines + " scans were saved here:\n\n" + path + "\n\n" +
                    "Only the scans of the biometrics device are in it. Times typed in with Edit DTR and filed leaves are not, " +
                    "because a .dat file has no place for them.",
                    "Dat2DTR", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void OpenWithDefaultProgram(string path)
        {
            try
            {
                var start = new ProcessStartInfo(path);
                start.UseShellExecute = true; // This is to let Windows pick the program (Excel, PDF reader, ...)
                Process.Start(start);
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception || ex is InvalidOperationException)
            {
                MessageBox.Show(this,
                    "The file was saved, but Windows could not open it.\n\n" + ex.Message,
                    "Dat2DTR", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>This is where characters that Windows does not allow in a file name are removed.</summary>
        private static string CleanFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, ' ');
            return name.Trim();
        }

        /// <summary>This is an entry of the month list. ToString() is what the list shows.</summary>
        private sealed class MonthItem
        {
            public MonthItem(DateTime month)
            {
                Month = month;
            }

            public DateTime Month { get; private set; }

            /// <summary>This is "October": the year is in the year list beside it.</summary>
            public override string ToString()
            {
                return Month.ToString("MMMM", System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        private void statusStrip_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }
    }
}
