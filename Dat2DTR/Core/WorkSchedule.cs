namespace Dat2DTR
{
    /// <summary>
    /// One set of office hours. The office has four:
    ///   Schedule A:  7:00 AM - 6:00 PM, Monday to Thursday  (4-day week)
    ///   Schedule B:  8:00 AM - 5:00 PM, Monday to Friday    (5-day week)
    ///   Schedule C:  7:00 AM - 4:00 PM, Monday to Friday    (5-day week)
    ///   Schedule D:  9:00 AM - 6:00 PM, Monday to Friday    (5-day week)
    /// (A Friday spent visiting schools is filed in Edit Leave as Official Business.)
    /// Every employee follows one of them (see EmployeeDirectory.GetSchedule).
    /// </summary>
    public sealed class WorkSchedule
    {
        /// <summary>"A", "B", "C" or "D".</summary>
        public string Letter = "A";

        public TimeSpan TimeIn = new TimeSpan(7, 0, 0);
        public TimeSpan LunchStart = new TimeSpan(12, 0, 0);
        public TimeSpan LunchEnd = new TimeSpan(13, 0, 0);
        public TimeSpan TimeOut = new TimeSpan(18, 0, 0);

        /// <summary>Arriving up to this many minutes after TimeIn is NOT late.</summary>
        public int GraceMinutes = 15;

        /// <summary>Working days, indexed by DayOfWeek (0 = Sunday ... 6 = Saturday).</summary>
        public bool[] WorkDays = { false, true, true, true, true, false, false };

        public static WorkSchedule DefaultA()
        {
            return new WorkSchedule(); // 7:00 AM - 6:00 PM, Monday to Thursday
        }

        public static WorkSchedule DefaultB()
        {
            var b = new WorkSchedule();
            b.Letter = "B";
            b.TimeIn = new TimeSpan(8, 0, 0);
            b.TimeOut = new TimeSpan(17, 0, 0);
            b.WorkDays = new[] { false, true, true, true, true, true, false }; // Monday to Friday
            return b;
        }

        public static WorkSchedule DefaultC()
        {
            var c = DefaultB();
            c.Letter = "C";
            c.TimeIn = new TimeSpan(7, 0, 0);
            c.TimeOut = new TimeSpan(16, 0, 0);    // 7:00 AM - 4:00 PM, Monday to Friday
            return c;
        }

        public static WorkSchedule DefaultD()
        {
            var d = DefaultB();
            d.Letter = "D";
            d.TimeIn = new TimeSpan(9, 0, 0);
            d.TimeOut = new TimeSpan(18, 0, 0);    // 9:00 AM - 6:00 PM, Monday to Friday
            return d;
        }

        /// <summary>The four schedules as they are before anyone changes them in "Office Hours".</summary>
        public static WorkSchedule[] Defaults()
        {
            return new[] { DefaultA(), DefaultB(), DefaultC(), DefaultD() };
        }

        /// <summary>The letters of the schedules, in order: index 0 = A ... 3 = D.</summary>
        public const string Letters = "ABCD";

        public bool IsWorkDay(DayOfWeek day)
        {
            return WorkDays[(int)day];
        }

        /// <summary>
        /// True for a day that is marked ABSENT when it has no scans: every working day of this
        /// schedule, and also every Monday to Friday. So the Friday of a Monday-to-Thursday
        /// employee still says ABSENT on the DTR (that is how the office fills in the form),
        /// even though it has no office hours and no late or undertime is computed for it.
        /// </summary>
        public bool ShowsAbsent(DayOfWeek day)
        {
            return IsWorkDay(day) || (day != DayOfWeek.Saturday && day != DayOfWeek.Sunday);
        }

        /// <summary>Text printed on the form beside "Regular days", e.g. "7:00 AM - 6:00 PM".</summary>
        public string OfficialHoursText
        {
            get { return DtrSettings.FormatTime(TimeIn) + " - " + DtrSettings.FormatTime(TimeOut); }
        }

        /// <summary>Very short form for the employee list, e.g. "7-6" or "8-5".</summary>
        public string ShortText
        {
            get { return ShortTime(TimeIn) + "-" + ShortTime(TimeOut); }
        }

        /// <summary>e.g. "Mon to Thu" or "Mon, Wed, Fri".</summary>
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
                bool oneRun = true; // true while the working days are next to each other
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

        /// <summary>e.g. "Schedule A:  7:00 AM - 6:00 PM, Mon to Thu".</summary>
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

        /// <summary>Returns a message if the times are not in a sensible order, or "" if they are fine.</summary>
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
