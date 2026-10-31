namespace Dat2DTR
{
    /// <summary>
    /// This is one set of office hours. The office has four:
    ///   Schedule A:  7:00 AM - 6:00 PM, Monday to Thursday  (4-day week)
    ///   Schedule B:  8:00 AM - 5:00 PM, Monday to Friday    (5-day week)
    ///   Schedule C:  7:00 AM - 4:00 PM, Monday to Friday    (5-day week)
    ///   Schedule D:  9:00 AM - 6:00 PM, Monday to Friday    (5-day week)
    /// (A Friday spent visiting schools is filed in Edit Leave as Official Business.)
    /// Every employee follows one of them (see EmployeeDirectory.GetSchedule).
    /// </summary>
    public sealed class WorkSchedule
    {
        /// <summary>This is "A", "B", "C" or "D".</summary>
        public string Letter = "A";

        public TimeSpan TimeIn = new TimeSpan(7, 0, 0);
        public TimeSpan LunchStart = new TimeSpan(12, 0, 0);
        public TimeSpan LunchEnd = new TimeSpan(13, 0, 0);
        public TimeSpan TimeOut = new TimeSpan(18, 0, 0);

        /// <summary>
        /// This is how many minutes after TimeIn are NOT late - as long as the minutes are made up
        /// at the end of the day: arriving 7:14 means leaving at 6:14 PM or later. Leaving earlier than
        /// that loses the grace period, and the 14 minutes are late (see DtrBuilder rule 2).
        /// </summary>
        public int GraceMinutes = 15;

        /// <summary>
        /// This is the Monday rule: on a Monday the grace period never goes past 7:15 AM. So a 7:00 AM start
        /// keeps its 15 minutes, but 8:00 AM and 9:00 AM must be met sharp on Mondays (8:01 = 1 minute late).
        /// Tuesday to Friday have the full grace period.
        /// </summary>
        public static readonly TimeSpan MondayGraceUntil = new TimeSpan(7, 15, 0);

        /// <summary>
        /// This is the hours of work in a day, without the lunch break: 8:00 AM - 5:00 PM = 8 hours, Schedule A = 10 hours.
        /// A Flexi-Time day may use any schedule with the same working hours (see DtrBuilder rule 10).
        /// </summary>
        public TimeSpan WorkingHours
        {
            get { return (TimeOut - TimeIn) - (LunchEnd - LunchStart); }
        }

        /// <summary>This is the grace period on that day of the week, in minutes (shorter on a Monday, see MondayGraceUntil).</summary>
        public int GraceOn(DayOfWeek day)
        {
            if (day != DayOfWeek.Monday)
                return GraceMinutes;
            int untilMondayLimit = (int)(MondayGraceUntil - TimeIn).TotalMinutes;
            return Math.Max(0, Math.Min(GraceMinutes, untilMondayLimit));
        }

        /// <summary>This is the working days, indexed by DayOfWeek (0 = Sunday ... 6 = Saturday).</summary>
        public bool[] WorkDays = { false, true, true, true, true, false, false };

        public static WorkSchedule DefaultA()
        {
            return new WorkSchedule(); // This is 7:00 AM - 6:00 PM, Monday to Thursday
        }

        public static WorkSchedule DefaultB()
        {
            var b = new WorkSchedule();
            b.Letter = "B";
            b.TimeIn = new TimeSpan(8, 0, 0);
            b.TimeOut = new TimeSpan(17, 0, 0);
            b.WorkDays = new[] { false, true, true, true, true, true, false }; // This is Monday to Friday
            return b;
        }

        public static WorkSchedule DefaultC()
        {
            var c = DefaultB();
            c.Letter = "C";
            c.TimeIn = new TimeSpan(7, 0, 0);
            c.TimeOut = new TimeSpan(16, 0, 0);    // This is 7:00 AM - 4:00 PM, Monday to Friday
            return c;
        }

        public static WorkSchedule DefaultD()
        {
            var d = DefaultB();
            d.Letter = "D";
            d.TimeIn = new TimeSpan(9, 0, 0);
            d.TimeOut = new TimeSpan(18, 0, 0);    // This is 9:00 AM - 6:00 PM, Monday to Friday
            return d;
        }

        /// <summary>This is the four schedules as they are before anyone changes them in "Office Hours".</summary>
        public static WorkSchedule[] Defaults()
        {
            return new[] { DefaultA(), DefaultB(), DefaultC(), DefaultD() };
        }

        /// <summary>This is the letters of the schedules, in order: index 0 = A ... 3 = D.</summary>
        public const string Letters = "ABCD";

        public bool IsWorkDay(DayOfWeek day)
        {
            return WorkDays[(int)day];
        }

        /// <summary>
        /// This is true for a day that is marked ABSENT when it has no scans: every working day of this
        /// schedule, and also every Monday to Friday. So the Friday of a Monday-to-Thursday
        /// employee still says ABSENT on the DTR (that is how the office fills in the form),
        /// even though it has no office hours and no late or undertime is computed for it.
        /// </summary>
        public bool ShowsAbsent(DayOfWeek day)
        {
            return IsWorkDay(day) || (day != DayOfWeek.Saturday && day != DayOfWeek.Sunday);
        }

        /// <summary>This is the text printed on the form beside "Regular days", e.g. "7:00 AM - 6:00 PM".</summary>
        public string OfficialHoursText
        {
            get { return DtrSettings.FormatTime(TimeIn) + " - " + DtrSettings.FormatTime(TimeOut); }
        }

        /// <summary>This is a very short form for the employee list, e.g. "7-6" or "8-5".</summary>
        public string ShortText
        {
            get { return ShortTime(TimeIn) + "-" + ShortTime(TimeOut); }
        }

        /// <summary>This is the working days as text, e.g. "Mon to Thu" or "Mon, Wed, Fri".</summary>
        public string WorkDaysText
        {
            get
            {
                DayOfWeek[] week =
                {
                    DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
                    DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
                };

                var names = new List<string>();
                int last = -1;
                bool oneRun = true; // This is true while the working days are next to each other
                for (int i = 0; i < week.Length; i++)
                {
                    if (!IsWorkDay(week[i])) continue;
                    if (names.Count > 0 && i != last + 1) oneRun = false;
                    last = i;
                    names.Add(week[i].ToString().Substring(0, 3));
                }

                if (names.Count >= 3 && oneRun)
                    return names[0] + " to " + names[names.Count - 1];
                return string.Join(", ", names.ToArray());
            }
        }

        /// <summary>This is the full description, e.g. "Schedule A:  7:00 AM - 6:00 PM, Mon to Thu".</summary>
        public string Description
        {
            get { return "Schedule " + Letter + ":  " + OfficialHoursText + ", " + WorkDaysText; }
        }

        public WorkSchedule Clone()
        {
            var copy = (WorkSchedule)MemberwiseClone();
            copy.WorkDays = (bool[])WorkDays.Clone();
            return copy;
        }

        /// <summary>This is the check of the times: a message if they are not in a sensible order, or "" if they are fine.</summary>
        public string Validate()
        {
            if (!(TimeIn < LunchStart))
                return "Time In must be earlier than the start of the lunch break.";
            if (!(LunchStart <= LunchEnd))
                return "The lunch break cannot end before it starts.";
            if (!(LunchEnd < TimeOut))
                return "Time Out must be later than the end of the lunch break.";
            if (!WorkDays.Any(day => day))
                return "Please tick at least one working day.";
            return "";
        }

        private static string ShortTime(TimeSpan time)
        {
            int hour = time.Hours % 12;
            if (hour == 0) hour = 12;
            return time.Minutes == 0 ? hour.ToString() : hour + ":" + time.Minutes.ToString("00");
        }
    }
}
