using System.Globalization;

namespace Dat2DTR
{
    /// <summary>This is one row of the DTR (one day of the month).</summary>
    public sealed class DtrDay
    {
        public int Day;
        public DateTime Date;

        public TimeSpan? AmArrival;
        public TimeSpan? AmDeparture;
        public TimeSpan? PmArrival;
        public TimeSpan? PmDeparture;

        public int LateMinutes;
        public int UndertimeMinutes;

        /// <summary>This is true for a working day with no scans at all.</summary>
        public bool IsAbsent;

        /// <summary>This is "Sat" or "Sun" (or empty).</summary>
        public string Remark = "";

        /// <summary>This is how many scans the day had (after removing double scans).</summary>
        public int ScanCount;

        /// <summary>This is something the personnel staff should double check, or "".</summary>
        public string Warning = "";

        /// <summary>This is true when a person typed this day in by hand (see DayCorrection).</summary>
        public bool IsEdited;

        /// <summary>This is true when Late or Undertime was typed in by hand (see DayAdjustment).</summary>
        public bool IsAdjusted;

        /// <summary>This is what the program computed, before Late / Undertime was typed in by hand.</summary>
        public int ComputedLateMinutes, ComputedUndertimeMinutes;

        /// <summary>This is why Late / Undertime was typed in by hand.</summary>
        public string AdjustReason = "";

        /// <summary>This is a hand-typed remark such as HOLIDAY. Printed when the day has no times.</summary>
        public string Note = "";

        /// <summary>This is the leave filed for this day with "Edit Leave" (LeaveType.None = no leave).</summary>
        public LeaveType Leave = LeaveType.None;

        /// <summary>This is whole day, or which half of the day the leave covers.</summary>
        public LeavePart LeavePart = LeavePart.WholeDay;

        /// <summary>This is what is printed for the leave, e.g. "VACATION LEAVE" or "PD - WFH/IDLAR".</summary>
        public string LeaveLabel = "";

        /// <summary>This is true when the leave of this day is a holiday from the holiday calendar (not filed).</summary>
        public bool IsCalendarHoliday;

        /// <summary>This is the name of that holiday, e.g. "Independence Day".</summary>
        public string HolidayName = "";

        /// <summary>This is the time work was suspended from (no late or undertime after it), or null.</summary>
        public TimeSpan? SuspendedFrom;

        public bool HasLeave
        {
            get { return Leave != LeaveType.None; }
        }

        /// <summary>This is a copy of this day (the times are values, so changing the copy leaves this one alone).</summary>
        public DtrDay Clone()
        {
            return (DtrDay)MemberwiseClone();
        }

        public bool HasTimes
        {
            get { return AmArrival.HasValue || AmDeparture.HasValue || PmArrival.HasValue || PmDeparture.HasValue; }
        }

        /// <summary>
        /// This is the words printed across the row instead of times: "A B S E N T", the leave
        /// (VACATION LEAVE...), or the hand-typed remark (HOLIDAY...) when the day has no times.
        /// Otherwise "".
        /// </summary>
        public string RowLabel
        {
            get
            {
                if (IsAbsent) return "A B S E N T";
                if (HasTimes) return "";
                if (HasLeave)
                    return ShownLeave + (LeavePart == LeavePart.WholeDay ? "" : " (1/2 DAY)");
                return Note.ToUpperInvariant();
            }
        }

        /// <summary>
        /// This is for a half-day leave on a day that also has times: the words printed across the
        /// half that is on leave (morning = the two A.M. columns, otherwise the two P.M. columns).
        /// "" when that half is not on leave, or when it has times anyway.
        /// </summary>
        public string HalfLabel(bool morning)
        {
            if (!HasLeave || !HasTimes) return "";

            if (morning)
            {
                if (LeavePart != LeavePart.Morning || AmArrival.HasValue || AmDeparture.HasValue) return "";
            }
            else
            {
                if (LeavePart != LeavePart.Afternoon || PmArrival.HasValue || PmDeparture.HasValue) return "";
            }
            return ShownLeave;
        }

        private string ShownLeave
        {
            get { return LeaveLabel.Length > 0 ? LeaveLabel : LeaveKinds.Get(Leave).Label; }
        }
    }

    /// <summary>This is one finished Daily Time Record: one employee, one month.</summary>
    public sealed class DtrSheet
    {
        public string EmployeeId = "";
        public string EmployeeName = "";

        /// <summary>This is the first day of the month this DTR is for.</summary>
        public DateTime Month;

        /// <summary>This is one entry per day of the month (28 to 31 entries).</summary>
        public List<DtrDay> Days = new List<DtrDay>();

        public int TotalLateMinutes;
        public int TotalUndertimeMinutes;

        /// <summary>This is the official hours, e.g. "7:00 AM - 6:00 PM"</summary>
        public string OfficialHours = "";

        /// <summary>This is which work schedule the employee follows: 0 = Schedule A, 1 = B, 2 = C, 3 = D.</summary>
        public int ScheduleIndex;

        /// <summary>This is the short form of the schedule for the employee list, e.g. "7-6".</summary>
        public string ScheduleShort = "";

        /// <summary>This is what is printed after "Name:", e.g. "070126010 -> INTERTAS, MICO D".</summary>
        public string NameLine
        {
            get { return EmployeeName.Length == 0 ? EmployeeId : EmployeeId + " -> " + EmployeeName; }
        }

        /// <summary>This is the month as text, e.g. "September 2026"</summary>
        public string MonthText
        {
            get { return Month.ToString("MMMM yyyy", CultureInfo.InvariantCulture); }
        }

        public List<DtrDay> DaysToCheck
        {
            get { return Days.Where(d => d.Warning.Length > 0).ToList(); }
        }

        /// <summary>This is the list of days that a person typed in by hand.</summary>
        public List<DtrDay> EditedDays
        {
            get { return Days.Where(d => d.IsEdited).ToList(); }
        }

        /// <summary>This is the list of days that are a holiday of the holiday calendar.</summary>
        public List<DtrDay> CalendarHolidays
        {
            get { return Days.Where(d => d.HasLeave && d.IsCalendarHoliday).ToList(); }
        }

        /// <summary>This is the list of days on which work was suspended from a time.</summary>
        public List<DtrDay> SuspendedFromDays
        {
            get { return Days.Where(d => d.SuspendedFrom.HasValue).ToList(); }
        }

        /// <summary>This is the list of days whose Late or Undertime was typed in by hand.</summary>
        public List<DtrDay> AdjustedDays
        {
            get { return Days.Where(d => d.IsAdjusted).ToList(); }
        }

        /// <summary>This is the list of days with a leave filed in "Edit Leave".</summary>
        public List<DtrDay> LeaveDays
        {
            get { return Days.Where(d => d.HasLeave && !d.IsCalendarHoliday).ToList(); }
        }

        /// <summary>This is "06:33 AM" - the way times are shown in the Edit DTR and Edit Leave tables (see FormatFormTime for the form).</summary>
        public static string FormatTime(TimeSpan? time)
        {
            if (!time.HasValue)
                return "";
            return DateTime.Today.Add(time.Value).ToString("hh:mm tt", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// This is "06:33" - the way times are printed ON THE FORM (preview, printing, PDF, Excel). No AM / PM:
        /// the columns already say A.M. and P.M. (The Edit DTR table keeps AM / PM, see FormatTime.)
        /// </summary>
        public static string FormatFormTime(TimeSpan? time)
        {
            if (!time.HasValue)
                return "";
            return DateTime.Today.Add(time.Value).ToString("hh:mm", CultureInfo.InvariantCulture);
        }

        /// <summary>This is "3h 15m", "26m", or "" for zero - used in the employee list on screen.</summary>
        public static string FormatDuration(int minutes)
        {
            if (minutes <= 0) return "";
            int h = minutes / 60, m = minutes % 60;
            if (h == 0) return m + "m";
            if (m == 0) return h + "h";
            return h + "h " + m + "m";
        }
    }
}
