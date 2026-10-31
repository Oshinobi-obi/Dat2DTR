using System.Globalization;

namespace Dat2DTR
{
    /// <summary>This is which of the five attendance reports (see AttendanceReports).</summary>
    public enum AttendanceReportKind
    {
        MondayLate = 1,        // This is 1. late at Time-In on Mondays
        MondayNoTimeIn = 2,    // This is 2. no Time-In on Mondays
        MondayCombined = 3,    // This is 3. reports 1 and 2 together
        MonthLate = 4,         // This is 4. late in the month
        MonthUndertime = 5     // This is 5. undertime in the month
    }

    /// <summary>
    /// This is one finished report: a title, a short explanation, the column headings and one row per employee.
    /// A value in a row is a string, or an int (a number that is centred and sorts as a number).
    /// The Reports window and the Excel file both show this same table.
    /// </summary>
    public sealed class AttendanceReport
    {
        public AttendanceReportKind Kind;

        /// <summary>This is the short name, used for the tab and the Excel worksheet, e.g. "1 Late on Mondays".</summary>
        public string ShortName = "";

        public string Title = "";
        public string Note = "";
        public string[] Headings = new string[0];

        /// <summary>This is the width of each column in Excel (in characters).</summary>
        public double[] Widths = new double[0];

        public List<object[]> Rows = new List<object[]>();
    }

    /// <summary>
    /// This is where the five attendance reports of the whole office are made from the DTRs of one month
    /// (everyone in the list):
    ///   1. Late on Mondays       - late at Time-In (A.M. Arrival) on a Monday, and how many Mondays
    ///   2. No Time-In on Mondays - a Monday with no A.M. Arrival (absent, or came in only in the afternoon)
    ///   3. Mondays (1 + 2)       - both together, one row per employee
    ///   4. Late this month       - every day late (Time-In or back from lunch), how many days and how long
    ///   5. Undertime this month  - every day with undertime, how many days and how long
    /// A Monday on leave, a holiday or a whole-day suspension is not counted. Late and undertime are the ones on
    /// the DTR (typed in by hand when they were changed in Edit DTR). Employees with nothing to report are left out.
    /// The rows are sorted by how many times (most first), then by name.
    /// </summary>
    public static class AttendanceReports
    {
        public static List<AttendanceReport> MakeAll(IList<DtrSheet> sheets, DtrSettings settings)
        {
            var all = new List<AttendanceReport>();
            foreach (AttendanceReportKind kind in new[] { AttendanceReportKind.MondayLate, AttendanceReportKind.MondayNoTimeIn,
                                                          AttendanceReportKind.MondayCombined, AttendanceReportKind.MonthLate,
                                                          AttendanceReportKind.MonthUndertime })
                all.Add(Make(kind, sheets, settings));
            return all;
        }

        public static AttendanceReport Make(AttendanceReportKind kind, IList<DtrSheet> sheets, DtrSettings settings)
        {
            string month = sheets.Count > 0 ? sheets[0].MonthText : "";
            var report = new AttendanceReport();
            report.Kind = kind;
            var found = new List<Found>();

            foreach (DtrSheet sheet in sheets)
            {
                WorkSchedule schedule = settings.GetSchedule(sheet.ScheduleIndex);
                var f = new Found { Sheet = sheet, Schedule = schedule.Letter };
                foreach (DtrDay day in sheet.Days)
                {
                    bool monday = day.Date.DayOfWeek == DayOfWeek.Monday && schedule.IsWorkDay(day.Date.DayOfWeek);
                    if (monday)
                    {
                        int late = TimeInLate(day);
                        if (late > 0)
                        {
                            f.LateCount++;
                            f.LateMinutes += late;
                            f.LateDates.Add(ShortDate(day.Date) + " - In " + Clock(day.AmArrival) + ", Out " + TimeOut(day) +
                                            " (" + DtrSheet.FormatDuration(late) + ")");
                        }
                        if (HasNoTimeIn(day))
                        {
                            f.NoTimeInCount++;
                            f.NoTimeInDates.Add(ShortDate(day.Date) + " - " + (day.IsAbsent ? "absent" : CameIn(day)));
                        }
                    }
                    if (day.LateMinutes > 0)
                    {
                        f.MonthLateCount++;
                        f.MonthLateMinutes += day.LateMinutes;
                        f.MonthLateDates.Add(ShortDate(day.Date) + " - " + InOut(day, true) + " (" + DtrSheet.FormatDuration(day.LateMinutes) + ")");
                    }
                    if (day.UndertimeMinutes > 0)
                    {
                        f.UndertimeCount++;
                        f.UndertimeMinutes += day.UndertimeMinutes;
                        f.UndertimeDates.Add(ShortDate(day.Date) + " - " + InOut(day, false) + " (" + DtrSheet.FormatDuration(day.UndertimeMinutes) + ")");
                    }
                }
                found.Add(f);
            }

            string mondays = Together(MondaysOf(sheets).Select(d => d.Day.ToString(CultureInfo.InvariantCulture)).ToArray());
            string mondaysText = "Mondays of " + month + ": " + (mondays.Length > 0 ? mondays : "none") + ".";
            switch (kind)
            {
                case AttendanceReportKind.MondayLate:
                    report.ShortName = "1 Late on Mondays";
                    report.Title = "Late on Mondays (Time-In) - " + month;
                    report.Note = mondaysText + " Late at Time-In (A.M. Arrival) on a Monday, for example  Sep 21 - In 7:11, Out 6:05 PM (11m). " +
                                  "A Monday on leave or a holiday is not counted.";
                    report.Headings = new[] { "No.", "ID", "Name", "Schedule", "Mondays late", "Total late", "Mondays (Time-In, Time-Out and how late)" };
                    report.Widths = new double[] { 6, 13, 34, 9, 10, 11, 60 };
                    foreach (Found f in Sorted(found, x => x.LateCount, x => x.LateMinutes))
                        report.Rows.Add(new object[] { 0, f.Sheet.EmployeeId, NameOf(f.Sheet), f.Schedule, f.LateCount,
                                                       Duration(f.LateMinutes), Together(f.LateDates.ToArray()) });
                    break;

                case AttendanceReportKind.MondayNoTimeIn:
                    report.ShortName = "2 No Time-In on Mondays";
                    report.Title = "No Time-In on Mondays - " + month;
                    report.Note = mondaysText + " A Monday with no A.M. Arrival: absent, or came in only in the afternoon. " +
                                  "A Monday on leave or a holiday is not counted.";
                    report.Headings = new[] { "No.", "ID", "Name", "Schedule", "Mondays with no Time-In", "Mondays" };
                    report.Widths = new double[] { 6, 13, 34, 9, 14, 60 };
                    foreach (Found f in Sorted(found, x => x.NoTimeInCount, x => 0))
                        report.Rows.Add(new object[] { 0, f.Sheet.EmployeeId, NameOf(f.Sheet), f.Schedule, f.NoTimeInCount,
                                                       Together(f.NoTimeInDates.ToArray()) });
                    break;

                case AttendanceReportKind.MondayCombined:
                    report.ShortName = "3 Mondays (1 + 2)";
                    report.Title = "Mondays: Late and No Time-In - " + month;
                    report.Note = mondaysText + " Reports 1 and 2 together: late at Time-In, and no Time-In, on Mondays.";
                    report.Headings = new[] { "No.", "ID", "Name", "Schedule", "Mondays late", "Mondays with no Time-In",
                                              "Total times", "Mondays late (Time-In, Time-Out and how late)", "Mondays with no Time-In" };
                    report.Widths = new double[] { 6, 13, 34, 9, 10, 14, 10, 40, 34 };
                    foreach (Found f in Sorted(found, x => x.LateCount + x.NoTimeInCount, x => x.LateMinutes))
                        report.Rows.Add(new object[] { 0, f.Sheet.EmployeeId, NameOf(f.Sheet), f.Schedule, f.LateCount, f.NoTimeInCount,
                                                       f.LateCount + f.NoTimeInCount, Together(f.LateDates.ToArray()),
                                                       Together(f.NoTimeInDates.ToArray()) });
                    break;

                case AttendanceReportKind.MonthLate:
                    report.ShortName = "4 Late this month";
                    report.Title = "Late - " + month;
                    report.Note = "Every day late in " + month + " (late at Time-In, or back late from lunch), for everyone in the office, " +
                                  "for example  Oct 7 - In 8:20, Out 5:00 PM (20m). Back late from lunch shows the time back: In 8:00, Back 1:15 PM, Out 5:00 PM.";
                    report.Headings = new[] { "No.", "ID", "Name", "Schedule", "Days late", "Total late", "Days (Time-In, Time-Out and how late)" };
                    report.Widths = new double[] { 6, 13, 34, 9, 10, 11, 70 };
                    foreach (Found f in Sorted(found, x => x.MonthLateCount, x => x.MonthLateMinutes))
                        report.Rows.Add(new object[] { 0, f.Sheet.EmployeeId, NameOf(f.Sheet), f.Schedule, f.MonthLateCount,
                                                       Duration(f.MonthLateMinutes), Together(f.MonthLateDates.ToArray()) });
                    break;

                default:
                    report.ShortName = "5 Undertime this month";
                    report.Title = "Undertime - " + month;
                    report.Note = "Every day with undertime in " + month + " (left early, or a half day with no scans), for everyone in the office, " +
                                  "for example  Oct 8 - In 8:00, Out 4:30 PM (30m).";
                    report.Headings = new[] { "No.", "ID", "Name", "Schedule", "Days with undertime", "Total undertime", "Days (Time-In, Time-Out and how much)" };
                    report.Widths = new double[] { 6, 13, 34, 9, 12, 13, 70 };
                    foreach (Found f in Sorted(found, x => x.UndertimeCount, x => x.UndertimeMinutes))
                        report.Rows.Add(new object[] { 0, f.Sheet.EmployeeId, NameOf(f.Sheet), f.Schedule, f.UndertimeCount,
                                                       Duration(f.UndertimeMinutes), Together(f.UndertimeDates.ToArray()) });
                    break;
            }

            // This is the row number in the first column: 1, 2, 3...
            for (int i = 0; i < report.Rows.Count; i++)
                report.Rows[i][0] = i + 1;
            return report;
        }

        /// <summary>
        /// This is the late at Time-In (A.M. Arrival) of a day, in minutes. When the Late of the day was typed in by hand,
        /// it is never more than what was typed (typed 0 = not late).
        /// </summary>
        public static int TimeInLate(DtrDay day)
        {
            int late = day.MorningLateMinutes;
            if (day.IsAdjusted)
                late = Math.Min(late, day.LateMinutes);
            return Math.Max(0, late);
        }

        /// <summary>
        /// This is true for a working day with no Time-In: absent, or times only in the afternoon (with the morning
        /// not on leave). A whole-day leave, a holiday or a day with only a remark (HOLIDAY...) does not count.
        /// </summary>
        public static bool HasNoTimeIn(DtrDay day)
        {
            if (day.IsAbsent)
                return true;
            if (!day.HasTimes || day.AmArrival.HasValue)
                return false;
            bool morningOnLeave = day.HasLeave && (day.LeavePart == LeavePart.WholeDay || day.LeavePart == LeavePart.Morning);
            return !morningOnLeave;
        }

        /// <summary>This is the Mondays of the month of the DTRs.</summary>
        private static List<DateTime> MondaysOf(IList<DtrSheet> sheets)
        {
            var list = new List<DateTime>();
            if (sheets.Count == 0) return list;
            foreach (DtrDay day in sheets[0].Days)
                if (day.Date.DayOfWeek == DayOfWeek.Monday) list.Add(day.Date);
            return list;
        }

        private static IEnumerable<Found> Sorted(List<Found> found, Func<Found, int> count, Func<Found, int> minutes)
        {
            return found.Where(f => count(f) > 0)
                        .OrderByDescending(count)
                        .ThenByDescending(minutes)
                        .ThenBy(f => f.Sheet.EmployeeName.Length == 0 ? 1 : 0)
                        .ThenBy(f => f.Sheet.EmployeeName, StringComparer.CurrentCultureIgnoreCase)
                        .ThenBy(f => f.Sheet.EmployeeId, StringComparer.OrdinalIgnoreCase);
        }

        private static string NameOf(DtrSheet sheet)
        {
            return sheet.EmployeeName.Length > 0 ? sheet.EmployeeName : "(no name yet)";
        }

        /// <summary>This is "Oct 5".</summary>
        private static string ShortDate(DateTime date)
        {
            return date.ToString("MMM d", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// This is the list of dates joined with ", ", each one kept on one line (its spaces are no-break spaces), so a long
        /// list wraps only between the dates: "Oct 19 - came in 1:00 PM" is never cut after "1:00".
        /// </summary>
        private static string Together(IEnumerable<string> dates)
        {
            return string.Join(", ", dates.Select(d => d.Replace(' ', '\u00A0')).ToArray());
        }

        /// <summary>This is the Time-In as it is written in the reports: "7:11" in the morning, "1:05 PM" in the afternoon.</summary>
        private static string Clock(TimeSpan? time)
        {
            if (!time.HasValue) return "";
            int hour = time.Value.Hours % 12;
            if (hour == 0) hour = 12;
            string text = hour + ":" + time.Value.Minutes.ToString("00", CultureInfo.InvariantCulture);
            return time.Value.Hours >= 12 ? text + " PM" : text;
        }

        /// <summary>
        /// This is "In 8:20, Out 5:00 PM": the first and the last time of the day. For a late day that is late because of
        /// coming back from lunch, the time back is shown too: "In 8:00, Back 1:15 PM, Out 5:00 PM".
        /// </summary>
        private static string InOut(DtrDay day, bool late)
        {
            TimeSpan? first = day.AmArrival ?? day.AmDeparture ?? day.PmArrival ?? day.PmDeparture;
            string text = "In " + (first.HasValue ? Clock(first) : "none");
            bool backLate = late && day.PmArrival.HasValue && day.AmArrival.HasValue && day.LateMinutes > TimeInLate(day);
            if (backLate)
                text += ", Back " + Clock(day.PmArrival);
            return text + ", Out " + TimeOut(day);
        }

        /// <summary>This is the Time-Out of the day as written in the reports, e.g. "6:05 PM", or "none" when there is no departure.</summary>
        private static string TimeOut(DtrDay day)
        {
            TimeSpan? last = day.PmDeparture ?? day.AmDeparture;
            return last.HasValue ? Clock(last) : "none";
        }

        /// <summary>This is "came in 1:00 PM" for a day with no Time-In: the first time of the day (or "no times" when there is none).</summary>
        private static string CameIn(DtrDay day)
        {
            TimeSpan? first = day.AmDeparture ?? day.PmArrival ?? day.PmDeparture;
            return first.HasValue ? "came in " + Clock(first) : "no times";
        }

        /// <summary>This is "1h 5m", or "" for none.</summary>
        private static string Duration(int minutes)
        {
            return DtrSheet.FormatDuration(minutes);
        }

        /// <summary>This is what was found for one employee while going through the month.</summary>
        private sealed class Found
        {
            public DtrSheet Sheet = null!;
            public string Schedule = "";
            public int LateCount, LateMinutes, NoTimeInCount;
            public int MonthLateCount, MonthLateMinutes, UndertimeCount, UndertimeMinutes;
            public List<string> LateDates = new List<string>(), NoTimeInDates = new List<string>();
            public List<string> MonthLateDates = new List<string>(), UndertimeDates = new List<string>();
        }
    }
}
