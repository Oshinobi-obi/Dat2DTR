using System.Diagnostics;
using System.Drawing.Printing;

namespace Dat2DTR
{
    /// <summary>
    /// The main window. It follows three steps, from left to right:
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

        /// <summary>Times that were typed in by hand with "Edit DTR".</summary>
        private CorrectionStore _corrections = new CorrectionStore();

        /// <summary>Leaves that were filed with "Edit Leave".</summary>
        private LeaveStore _leaves = new LeaveStore();

        /// <summary>What is remembered of each month, so the leave balance carries over to the next month.</summary>
        private LeaveLedger _ledger = new LeaveLedger();

        /// <summary>The CTO given to each employee.</summary>
        private CtoStore _cto = new CtoStore();

        /// <summary>The holidays of the year, used for every employee ("Holidays...").</summary>
        private HolidayCalendar _holidays = new HolidayCalendar();

        /// <summary>True when holidays.csv could not be read at start: then it is not written over this session.</summary>
        private bool _holidaysUnreadable;

        private AttendanceLog? _log;

        /// <summary>One DTR per employee for the chosen month.</summary>
        private List<DtrSheet> _sheets = new List<DtrSheet>();

        /// <summary>The Computation of Leaves of each employee in _sheets, by employee ID.</summary>
        private readonly Dictionary<string, LeaveCard> _cards =
            new Dictionary<string, LeaveCard>(StringComparer.OrdinalIgnoreCase);

        /// <summary>false = the preview shows the DTR, true = it shows the Computation of Leaves.</summary>
        private bool _showLeave;

        /// <summary>IDs of the employees whose box is ticked.</summary>
        private readonly HashSet<string> _ticked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>True while the program itself is changing the list (so events are ignored).</summary>
        private bool _updating;

        public Form1()
        {
            InitializeComponent();
            Icon = Theme.GetAppIcon();

            // The DepEd NCR seal and the "One NCR" banner are built into the program.
            picSeal.SetImage(Theme.GetImage(Theme.SealImage));
            picBanner.SetImage(Theme.GetImage(Theme.BannerImage));
            preview.SetEmptyImage(Theme.GetImage(Theme.BannerImage));

            // Keep the chosen employee highlighted even while the mouse is used elsewhere.
            lvEmployees.HideSelection = false;

            Theme.StylePrimary(btnOpen);
            Theme.StylePrimary(btnPrint);
            Theme.StylePrimary(btnSave);
            Theme.StylePrimary(btnEdit);
            Theme.StylePrimary(btnLeave);
            ShowViewButtons();
            Theme.StyleHeaderButton(btnSettings);
            Theme.StyleHeaderButton(btnHelp);
#if NET
            txtSearch.PlaceholderText = "Search name or ID";
#endif
        }

        // ================================================================ start-up

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

            // If the saved leaves cannot be read (the file is open in Excel, for example), nothing
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
            preview.SetPaper(_settings.Paper);
            ShowRules();
            ShowEmployeeList();
        }

        /// <summary>
        /// Makes the narrow columns of the employee list just wide enough for their text.
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

        /// <summary>Puts the two work schedules into the "Work schedule" list above the DTR.</summary>
        private void ShowRules()
        {
            _updating = true;
            cboSchedule.Items.Clear();
            foreach (WorkSchedule schedule in _settings.Schedules)
                cboSchedule.Items.Add(schedule.Description);
            _updating = false;
        }

        // ================================================================ step 1: open the file

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

            _log = log;
            _ticked.Clear();

            _settings.LastFolder = Path.GetDirectoryName(path) ?? "";
            _settings.Save();

            lblFile.Text = Path.GetFileName(path) + "\r\n" +
                           log.RecordCount.ToString("N0") + " scans,  " + log.EmployeeCount + " employees,  " +
                           log.FirstDate.ToString("MMM d, yyyy") + " to " + log.LastDate.ToString("MMM d, yyyy");
            lblFile.ForeColor = Theme.TextStrong;
            toolTip.SetToolTip(lblFile, path);

            // Fill the month list and pick the most likely month.
            DateTime suggested = log.SuggestMonth();
            _updating = true;
            cboMonth.Items.Clear();
            foreach (DateTime month in log.GetMonths())
            {
                var item = new MonthItem(month);
                cboMonth.Items.Add(item);
                if (month == suggested)
                    cboMonth.SelectedItem = item;
            }
            if (cboMonth.SelectedIndex < 0 && cboMonth.Items.Count > 0)
                cboMonth.SelectedIndex = 0;
            cboMonth.Enabled = true;
            _updating = false;

            txtSearch.Text = "";
            BuildSheets();

            lblStatus.Text = log.SkippedLineCount == 0
                ? "File opened."
                : "File opened. " + log.SkippedLineCount + " line(s) that were not attendance records were skipped.";
        }

        // ================================================================ step 2: month and employees

        private void cboMonth_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_updating) return;
            _ticked.Clear();
            BuildSheets();
        }

        /// <summary>Computes the DTR and the Computation of Leaves of every employee for the chosen month.</summary>
        private void BuildSheets()
        {
            _leaves.Holidays = _holidays;   // the holiday calendar is used for everyone
            _sheets = new List<DtrSheet>();
            _cards.Clear();
            var chosen = cboMonth.SelectedItem as MonthItem;

            if (_log != null && chosen != null)
            {
                // The last day of the chosen month that the file covers.
                DateTime monthEnd = chosen.Month.AddMonths(1).AddDays(-1);
                DateTime dataThrough = _log.LastDate < monthEnd ? _log.LastDate.Date : monthEnd;
                bool ledgerChanged = false;

                foreach (string id in GetIdsToList(_log, chosen.Month))
                {
                    // Each employee follows Schedule A, B, C or D (chosen above the DTR or in "Employee Names").
                    WorkSchedule schedule = _settings.GetSchedule(_names.GetSchedule(id));
                    DtrSheet sheet = DtrBuilder.Build(_log, id, _names.GetName(id), chosen.Month,
                                                      schedule, _settings, _corrections, _leaves);
                    _sheets.Add(sheet);

                    // Remember the late and undertime of this month, so that next month
                    // the leave balance can be carried over without opening this file again.
                    // (Not for someone with no scans and no leave at all: there is nothing to remember.
                    //  The ledger itself refuses a file that holds only part of the month.)
                    bool hasSomething = sheet.LeaveDays.Count > 0 || sheet.Days.Any(d => d.ScanCount > 0);
                    if (hasSomething &&
                        _ledger.Remember(id, chosen.Month.Year, chosen.Month.Month, sheet.TotalLateMinutes,
                                         sheet.TotalUndertimeMinutes, LeaveRules.DayCharge(schedule),
                                         _log.FirstDate, dataThrough))
                        ledgerChanged = true;

                    _cards[id] = LeaveCalculator.Build(sheet, schedule, _leaves, _ledger, _cto, _log.FirstDate, _log.LastDate);
                }

                if (ledgerChanged)
                    SaveLeaveFiles(false);

                // People with a name first (A to Z), then the IDs that have no name yet.
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
        /// The employees to list for the month: everyone who scanned at least once, and everyone
        /// who has a leave filed in the month (someone on leave for the whole month does not scan).
        /// With "Also show employees with no scans" ticked, everyone the program knows is listed.
        /// </summary>
        private List<string> GetIdsToList(AttendanceLog log, DateTime month)
        {
            var ids = new List<string>(log.GetEmployeeIds(month));
            var listed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string id in ids)
                listed.Add(EmployeeDirectory.NormalizeId(id));

            // How each ID is written in full. (The leave files keep IDs without leading zeros.)
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

        /// <summary>The tick box "Also show employees with no scans this month".</summary>
        private void chkNoScans_CheckedChanged(object? sender, EventArgs e)
        {
            if (_updating) return;
            BuildSheets();
        }

        /// <summary>Fills the list on the left (only the employees that match the search box).</summary>
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

                // The list above the names: everyone, or only the ones with  !  *  or  L.
                int filter = cboFilter.SelectedIndex;
                if ((filter == 1 && !needsCheck) || (filter == 2 && !wasEdited) || (filter == 3 && !hasLeave))
                    continue;

                var item = new ListViewItem(hasName ? sheet.EmployeeName : "(no name yet)");
                item.SubItems.Add(sheet.EmployeeId);
                item.SubItems.Add(sheet.ScheduleShort);
                item.SubItems.Add(DtrSheet.FormatDuration(sheet.TotalLateMinutes));
                item.SubItems.Add(DtrSheet.FormatDuration(sheet.TotalUndertimeMinutes));
                item.SubItems.Add((needsCheck ? "!" : "") + (wasEdited ? "*" : "") + (hasLeave ? "L" : ""));  // ! = check,  * = typed in by hand,  L = on leave
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

        /// <summary>The DTR of the employee that is selected in the list (or null).</summary>
        private DtrSheet? GetCurrentSheet()
        {
            if (lvEmployees.SelectedItems.Count == 0) return null;
            return lvEmployees.SelectedItems[0].Tag as DtrSheet;
        }

        private void lvEmployees_SelectedIndexChanged(object? sender, EventArgs e)
        {
            // When the user clicks another name, Windows first reports "nothing selected"
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
            // Let the Name column use whatever width is left over.
            int others = colId.Width + colSchedule.Width + colLate.Width + colUndertime.Width + colCheck.Width;
            int width = lvEmployees.ClientSize.Width - others - SystemInformation.VerticalScrollBarWidth;
            colName.Width = Math.Max(140, width);
        }

        private void txtSearch_TextChanged(object? sender, EventArgs e)
        {
            ShowEmployeeList();
        }

        /// <summary>The list above the names: show everyone, or only the ones that need checking, were typed in, or have a leave.</summary>
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

        /// <summary>Ticks or unticks every employee that is currently shown in the list.</summary>
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

        /// <summary>Shows the selected employee's DTR and switches the buttons on or off.</summary>
        private void ShowCurrentSheet()
        {
            DtrSheet? sheet = GetCurrentSheet();
            LeaveCard? card = null;
            if (sheet != null)
                _cards.TryGetValue(sheet.EmployeeId, out card);
            preview.ShowPages(sheet, card, _showLeave);

            // The yellow bar: days a person should double-check, days typed in by hand, and days on leave.
            var notices = new List<string>();
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

            lblNotice.Text = string.Join("        ", notices.ToArray());
            toolTip.SetToolTip(lblNotice, string.Join("\n\n", notices.ToArray()).Replace(";   ", "\n"));
            lblNotice.Visible = notices.Count > 0;

            // The "Work schedule" list and the status bar show the schedule of this employee.
            _updating = true;
            cboSchedule.SelectedIndex = sheet == null ? -1 : sheet.ScheduleIndex;
            _updating = false;
            cboSchedule.Enabled = sheet != null;
            btnEdit.Enabled = sheet != null;
            btnLeave.Enabled = sheet != null;

            if (sheet != null)
            {
                WorkSchedule schedule = _settings.GetSchedule(sheet.ScheduleIndex);
                lblRules.Text = schedule.Description + "   |   Grace period " + schedule.GraceMinutes + " min";
            }
            else
            {
                // All the schedules, short: "A  7-6 Mon to Thu   |   B  8-5 Mon to Fri ..."
                lblRules.Text = string.Join("   |   ",
                    _settings.Schedules.Select(x => x.Letter + "  " + x.ShortText + " " + x.WorkDaysText).ToArray());
            }

            // The words in the middle of the empty preview.
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

        // ================================================================ work schedule, Edit DTR and Edit Leave

        /// <summary>The user picked a schedule (A to D) in the list above the DTR.</summary>
        private void cboSchedule_SelectionChangeCommitted(object? sender, EventArgs e)
        {
            DtrSheet? current = GetCurrentSheet();
            int chosen = cboSchedule.SelectedIndex;
            if (_updating || current == null || chosen < 0)
                return;

            // With ticked employees, ask whether the change is for this one, the ticked ones or everyone.
            var who = new List<DtrSheet> { current };
            if (_ticked.Count > 0)
                who = AskWhichSheets("Whose work schedule do you want to change?");

            foreach (DtrSheet sheet in who)
                _names.SetSchedule(sheet.EmployeeId, chosen);

            if (who.Count > 0)
                SaveEmployeeList();

            BuildSheets(); // also puts the list back to the saved schedule if the question was cancelled
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

        /// <summary>e.g. "undertime 1h -> none (Work suspended at 3:00 PM)" for the yellow notice.</summary>
        private static string AdjustedText(DtrDay day)
        {
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

        /// <summary>Opens the table where the times of the shown employee can be typed in or corrected.</summary>
        private void btnEdit_Click(object? sender, EventArgs e)
        {
            // "Next >" and "< Previous" in the window save and go on with the employee below / above.
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

        /// <summary>Edit DTR for one employee. Returns -1 / +1 to go on with the previous / next one, else 0.</summary>
        private int EditDtrOf(DtrSheet current, AttendanceLog log)
        {
            WorkSchedule schedule = _settings.GetSchedule(current.ScheduleIndex);

            // What the biometrics file says, without anything typed in by hand.
            DtrSheet fromFile = DtrBuilder.Build(log, current.EmployeeId, current.EmployeeName, current.Month,
                                                 schedule, _settings, null, _leaves);

            int move;
            using (var dialog = new EditTimesForm(fromFile, _corrections, _leaves, schedule, _settings,
                                                  log.FirstDate, log.LastDate))
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

        /// <summary>True when the list (as it is shown now) has an employee above (-1) or below (+1) this one.</summary>
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

        /// <summary>The ID of the employee above (-1) or below (+1) the given place in the list, or null.</summary>
        private string? NeighbourId(int index, int move)
        {
            int other = index + move;
            if (index < 0 || other < 0 || other >= lvEmployees.Items.Count) return null;
            return ((DtrSheet)lvEmployees.Items[other].Tag!).EmployeeId;
        }

        /// <summary>
        /// After Next / Previous (the list was made again by then): selects the employee that was below / above,
        /// and returns the DTR, or null at the end of the list. If the edited employee is not in the list any
        /// more (for example the filter "Needs checking" and the day was fixed), the one now at its place is next.
        /// </summary>
        private DtrSheet? MoveAfterEdit(string editedId, int oldIndex, string? targetId, int move)
        {
            int index = targetId == null ? -1 : IndexInList(targetId);
            if (index < 0 && IndexInList(editedId) < 0 && oldIndex >= 0)
                index = move > 0 ? oldIndex : oldIndex - 1;   // the edited one dropped out of the list
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

        /// <summary>Selects the employee above (-1) or below (+1) in the list and returns the DTR, or null at the end.</summary>
        private DtrSheet? SelectNeighbour(string employeeId, int move)
        {
            int index = IndexInList(employeeId);
            return MoveAfterEdit(employeeId, index, NeighbourId(index, move), move);
        }

        /// <summary>Opens the table where a leave can be filed for the shown employee.</summary>
        private void btnLeave_Click(object? sender, EventArgs e)
        {
            DtrSheet? current = GetCurrentSheet();
            AttendanceLog? log = _log;
            if (current == null || log == null)
                return;

            // With employees ticked: this one only, or the same leave for all the ticked ones?
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

            // "Next >" and "< Previous" in the window save and go on with the employee below / above.
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

        /// <summary>Edit Leave for one employee. Returns -1 / +1 to go on with the previous / next one, else 0.</summary>
        private int EditLeaveOf(DtrSheet current, AttendanceLog log)
        {
            WorkSchedule schedule = _settings.GetSchedule(current.ScheduleIndex);

            // The DTR without any leave: what the person scanned (or what was typed in by hand).
            DtrSheet attendance = DtrBuilder.Build(log, current.EmployeeId, current.EmployeeName, current.Month,
                                                   schedule, _settings, _corrections, null);

            // The window works on copies. This is how it asks for the computation while the user is choosing.
            Func<LeaveStore, LeaveLedger, CtoStore, LeaveCard> compute = (leaves, ledger, cto) =>
            {
                DtrSheet sheet = DtrBuilder.Build(log, current.EmployeeId, current.EmployeeName, current.Month,
                                                  schedule, _settings, _corrections, leaves);
                return LeaveCalculator.Build(sheet, schedule, leaves, ledger, cto, log.FirstDate, log.LastDate);
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

            _showLeave = true;      // show the result right away
            ShowViewButtons();
            BuildSheets();
            lblStatus.Text = "Leave updated for " + current.NameLine;
            return move;
        }

        /// <summary>
        /// The same leave (for example Holiday or Suspended) for a range of days, for all the
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

            // Days that already have something else filed: replace them, or leave them as they are?
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

            int changed = ApplyLeaveToMany(_leaves, ticked.Select(s => s.EmployeeId).ToList(),
                ticked.Select(s => _settings.GetSchedule(s.ScheduleIndex)).ToList(), from, to, entry, replace);

            SaveLeaveFiles(true);
            _showLeave = false;     // a holiday or a suspension is seen on the DTR
            ShowViewButtons();
            BuildSheets();
            lblStatus.Text = (entry == null ? "Leave removed" : (entry.Type == LeaveType.WorkFromHome ? "Work from home" : entry.Kind.Name) +
                             (entry.From.HasValue ? " from " + DtrSettings.FormatTime(entry.From.Value) : "") + " filed") +
                             " for " + ticked.Count + " employees (" + changed + (changed == 1 ? " day" : " days") + " changed)";
        }

        /// <summary>
        /// Files one leave (or removes what is filed, when entry is null) on every working day
        /// from..to for each of the employees. Returns how many days were changed.
        /// </summary>
        /// <param name="replace">false = keep a different leave that is already filed on a day.</param>
        internal static int ApplyLeaveToMany(LeaveStore leaves, List<string> ids, List<WorkSchedule> schedules,
                                             DateTime from, DateTime to, LeaveEntry? entry, bool replace)
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
                    leaves.Set(ids[n], date, entry);
                    changed++;
                }
            }
            return changed;
        }

        /// <summary>Saves the filed leaves and the remembered months.</summary>
        /// <param name="tellUser">true = show a message when the files cannot be written.</param>
        private void SaveLeaveFiles(bool tellUser)
        {
            // The saved files could not be read when the program started (see Form1_Load).
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

        // ================================================================ DTR or Computation of Leaves

        private void btnViewDtr_Click(object? sender, EventArgs e)
        {
            _showLeave = false;
            ShowViewButtons();
            ShowCurrentSheet();
        }

        private void btnViewLeave_Click(object? sender, EventArgs e)
        {
            _showLeave = true;
            ShowViewButtons();
            ShowCurrentSheet();
        }

        /// <summary>Makes the two buttons above the preview look like tabs: the chosen one is navy.</summary>
        private void ShowViewButtons()
        {
            StyleViewButton(btnViewDtr, !_showLeave);
            StyleViewButton(btnViewLeave, _showLeave);
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

        /// <summary>Asks what to print or save: the DTR, the Computation of Leaves, or both.</summary>
        private DtrPages AskWhichPages(string question, bool offerDatFile = false)
        {
            using (var dialog = new PagesDialog(question, offerDatFile))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return DtrPages.Cancel;
                return dialog.GetChoice();
            }
        }

        /// <summary>"DTR", "Computation of Leaves" or "DTR and Computation of Leaves" - for file names and messages.</summary>
        private static string PagesName(DtrPages pages)
        {
            if (pages == DtrPages.LeaveOnly) return "Computation of Leaves";
            if (pages == DtrPages.Both) return "DTR and Computation of Leaves";
            return "DTR";
        }

        /// <summary>The Computation of Leaves of each of these DTRs, in the same order.</summary>
        private List<LeaveCard> GetCards(List<DtrSheet> sheets)
        {
            var cards = new List<LeaveCard>();
            foreach (DtrSheet sheet in sheets)
                cards.Add(_cards[sheet.EmployeeId]);
            return cards;
        }

        // ================================================================ names, office hours, help

        /// <summary>Opens the holiday calendar (used for every employee).</summary>
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
            using (var dialog = new EmployeeNamesForm(_names, ids, _settings))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    BuildSheets();
            }
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

        private void Form1_KeyDown(object? sender, KeyEventArgs e)
        {
            if (!e.Control) return;

            if (e.KeyCode == Keys.O) { e.SuppressKeyPress = true; btnOpen.PerformClick(); }
            else if (e.KeyCode == Keys.P) { e.SuppressKeyPress = true; btnPrint.PerformClick(); }
            else if (e.KeyCode == Keys.S) { e.SuppressKeyPress = true; btnSave.PerformClick(); }
        }

        // ================================================================ step 3: print and save

        /// <summary>
        /// Asks whose DTR is wanted (this one / the ticked ones / everyone).
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

                    // The pages in the order they are printed: for each employee the DTR,
                    // the Computation of Leaves, or the DTR followed by the Computation of Leaves.
                    var pages = new List<object>();
                    foreach (DtrSheet sheet in sheets)
                    {
                        if (what != DtrPages.LeaveOnly)
                            pages.Add(sheet);
                        if (what != DtrPages.DtrOnly)
                            pages.Add(_cards[sheet.EmployeeId]);
                    }

                    int next = 0; // which page is printed next
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

        /// <summary>Asks the printer for the paper size chosen in "Office Hours", if it has that size.</summary>
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

        /// <param name="page">A DtrSheet (the DTR) or a LeaveCard (the Computation of Leaves).</param>
        private static void PrintOnePage(PrintPageEventArgs e, object page)
        {
            Graphics g = e.Graphics!;

            // The printer measures in 1/100 inch; the DTR page is laid out in points (1/72 inch).
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
                if (card != null)
                    LeavePageRenderer.Draw(canvas, card, pageWidth, pageHeight);
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
                dialog.Filter = "PDF document (*.pdf)|*.pdf|Excel workbook (*.xlsx)|*.xlsx";
                dialog.FileName = CleanFileName(PagesName(what) + " " + sheets[0].MonthText + " - " + who);
                dialog.AddExtension = true;
                dialog.OverwritePrompt = true;
                if (Directory.Exists(_settings.LastFolder))
                    dialog.InitialDirectory = _settings.LastFolder;

                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                // Decide the file type from the ending the user chose.
                string path = dialog.FileName;
                string extension = Path.GetExtension(path).ToLowerInvariant();
                if (extension != ".pdf" && extension != ".xlsx")
                {
                    extension = dialog.FilterIndex == 2 ? ".xlsx" : ".pdf";
                    path += extension;
                }

                try
                {
                    Cursor = Cursors.WaitCursor;
                    // DTR only: no leave cards.  Computation of Leaves only: no DTRs.  Both: each DTR, then its leave card.
                    List<LeaveCard>? cards = what == DtrPages.DtrOnly ? null : GetCards(sheets);
                    bool withDtr = what != DtrPages.LeaveOnly;
                    if (extension == ".xlsx")
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
        /// Saves the scans of the chosen employees in the chosen month as a new .dat file,
        /// line for line the same as in the opened file.
        /// </summary>
        private void SaveDatFile()
        {
            var chosen = cboMonth.SelectedItem as MonthItem;
            if (_log == null || chosen == null)
                return;

            List<DtrSheet> sheets = AskWhichSheets("Whose scans do you want to save?");
            if (sheets.Count == 0)
                return;

            string who = sheets.Count == 1
                ? (sheets[0].EmployeeName.Length > 0 ? sheets[0].EmployeeName : "ID " + sheets[0].EmployeeId)
                : sheets.Count + " employees";

            using (var dialog = new SaveFileDialog())
            {
                dialog.Title = "Save the scans as a .dat file";
                dialog.Filter = "Biometrics file (*.dat)|*.dat";
                dialog.FileName = CleanFileName("Scans " + chosen.ToString() + " - " + who);
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

                // Never write over the file that is open right now.
                if (_log.FilePath.Length > 0 &&
                    string.Equals(Path.GetFullPath(path), Path.GetFullPath(_log.FilePath), StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show(this, "That is the file that is open right now. Please choose another name.",
                        "Dat2DTR", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                int lines;
                try
                {
                    lines = _log.SaveDat(path, sheets.Select(s => s.EmployeeId), chosen.Month);
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
                start.UseShellExecute = true; // let Windows pick the program (Excel, PDF reader, ...)
                Process.Start(start);
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception || ex is InvalidOperationException)
            {
                MessageBox.Show(this,
                    "The file was saved, but Windows could not open it.\n\n" + ex.Message,
                    "Dat2DTR", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>Removes characters that Windows does not allow in a file name.</summary>
        private static string CleanFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, ' ');
            return name.Trim();
        }

        /// <summary>An entry of the month list. ToString() is what the list shows.</summary>
        private sealed class MonthItem
        {
            public MonthItem(DateTime month)
            {
                Month = month;
            }

            public DateTime Month { get; private set; }

            public override string ToString()
            {
                return Month.ToString("MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        private void statusStrip_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }
    }
}
