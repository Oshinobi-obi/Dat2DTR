namespace Dat2DTR
{
    /// <summary>
    /// Turns the raw scans of one employee into a Daily Time Record and
    /// computes Late and Undertime.
    ///
    /// THE RULES (shown here for Schedule A: 7:00 AM - 6:00 PM, lunch 12:00 - 1:00 PM,
    /// 15 minutes grace period. Schedules B, C and D work the same way with their own times.)
    ///
    ///  1. Which scan goes in which column
    ///       - first scan of the day  -> A.M. Arrival   (P.M. Arrival if it is already past 12:00)
    ///       - last scan of the day   -> P.M. Departure (A.M. Departure if it is before 12:00)
    ///       - the two scans around lunch time -> A.M. Departure and P.M. Arrival
    ///       - two scans within one minute of each other count as one (double scan)
    ///
    ///  2. LATE
    ///       - A.M. Arrival up to 7:15 is NOT late. From 7:16 it is late, counted from 7:00.
    ///       - P.M. Arrival after 1:00 PM is late, counted from 1:00 PM.
    ///
    ///  3. UNDERTIME
    ///       - A.M. Departure before 12:00 -> the minutes missing before 12:00
    ///       - P.M. Departure before 6:00 PM -> the minutes missing before 6:00 PM
    ///       - a whole half day with no scans (when the other half has scans) -> 5 hours
    ///
    ///  4. A working day with no scans is ABSENT. Absences are not added to undertime.
    ///     Which days are working days depends on the employee's schedule.
    ///     A Monday to Friday that is NOT a working day of the schedule (the Friday of a
    ///     Monday-to-Thursday employee) also says ABSENT when it has no scans, because that
    ///     is how the office fills in the form. No late or undertime is computed for it.
    ///     Saturdays and Sundays are only labelled; no late or undertime is computed.
    ///
    ///  5. Days with a missing or extra scan get a Warning so a person can check them.
    ///
    ///  6. A day that a person typed in by hand (Edit DTR) uses the typed times
    ///     instead of the scans. Rules 2 to 4 are then applied to the typed times.
    ///
    ///  7. A day with a LEAVE filed (Edit Leave):
    ///       - whole day  -> the leave is printed instead of ABSENT, no late or undertime
    ///       - half day   -> the half on leave is excused: no late and no undertime for that half.
    ///                       The other half follows rules 2 and 3 as usual.
    ///     How the leave is deducted from the leave credits is in Core/LeaveCalculator.cs.
    ///
    ///  8. Late or Undertime typed in by hand (Edit DTR, the Late / Undertime boxes) replaces what
    ///     was computed for that day - for example when work was suspended at 3:00 PM and everyone
    ///     went home at 3:00. The typed value is printed, totalled and deducted from the leave credits.
    ///
    ///  9. Work suspended FROM a time (Suspended "from 3:00 PM", usually filed for the ticked
    ///     employees): an ordinary day until that time, no late or undertime after it.
    ///     A holiday in the holiday calendar counts as a whole-day Holiday for everyone,
    ///     unless a leave is filed for that day.
    /// </summary>
    public static class DtrBuilder
    {
        /// <param name="schedule">The office hours this employee follows.</param>
        /// <param name="corrections">Hand-typed days, or null to use only the biometrics data.</param>
        /// <param name="leaves">Filed leaves, or null to make the DTR without any leave.</param>
        public static DtrSheet Build(AttendanceLog log, string employeeId, string employeeName, DateTime month,
                                     WorkSchedule schedule, DtrSettings settings, CorrectionStore? corrections,
                                     LeaveStore? leaves = null)
        {
            var sheet = new DtrSheet();
            sheet.EmployeeId = employeeId;
            sheet.EmployeeName = employeeName;
            sheet.Month = new DateTime(month.Year, month.Month, 1);
            sheet.OfficialHours = schedule.OfficialHoursText;
            sheet.ScheduleIndex = Array.IndexOf(settings.Schedules, schedule);
            sheet.ScheduleShort = schedule.ShortText;

            int daysInMonth = DateTime.DaysInMonth(month.Year, month.Month);
            for (int d = 1; d <= daysInMonth; d++)
            {
                var day = new DtrDay();
                day.Day = d;
                day.Date = new DateTime(month.Year, month.Month, d);

                if (day.Date.DayOfWeek == DayOfWeek.Saturday) day.Remark = "Sat";
                if (day.Date.DayOfWeek == DayOfWeek.Sunday) day.Remark = "Sun";

                // Only dates the file actually covers can be ABSENT. Days before the first
                // scan or after the last scan in the file are unknown, so they stay blank.
                bool coveredByFile = day.Date >= log.FirstDate && day.Date <= log.LastDate;

                // Work from home without the IDLAR does not count yet: the day is shown as if nothing was filed.
                LeaveEntry? filed = leaves == null ? null : leaves.GetForDtr(employeeId, day.Date, schedule);
                LeaveEntry? leave = filed != null && filed.Excuses ? filed : null;
                DayCorrection? typed = corrections == null ? null : corrections.Get(employeeId, day.Date);
                if (typed != null)
                {
                    FillDay(day, typed, schedule, settings, coveredByFile, leave);
                    day.IsEdited = true;
                }
                else
                {
                    List<TimeSpan> scans = CleanScans(log.GetScans(employeeId, day.Date));
                    day.ScanCount = scans.Count;
                    if (scans.Count > 0)
                        AssignScans(scans, day, schedule);
                    if (leave != null && !leave.From.HasValue)
                        KeepScansInWorkedHalf(day, leave, schedule);
                    FinishDay(day, schedule, settings, coveredByFile, leave);
                }

                // Late / Undertime typed in by hand (Edit DTR) replaces what was computed.
                DayAdjustment? adjustment = corrections == null ? null : corrections.GetAdjustment(employeeId, day.Date);
                if (adjustment != null)
                    ApplyAdjustment(day, adjustment);

                if (filed != null && filed.WaitsForIdlar && filed.Type != LeaveType.Holiday && !filed.From.HasValue)
                {
                    string warning = "work from home, but the IDLAR is not ticked yet";
                    day.Warning = day.Warning.Length == 0 ? warning : day.Warning + "; " + warning;
                }

                sheet.TotalLateMinutes += day.LateMinutes;
                sheet.TotalUndertimeMinutes += day.UndertimeMinutes;
                sheet.Days.Add(day);
            }

            return sheet;
        }

        /// <summary>
        /// Rule 8: Late and Undertime typed in by hand (for example when work was suspended at
        /// 3:00 PM) replace the computed ones. The computed ones are kept to show beside them.
        /// </summary>
        public static void ApplyAdjustment(DtrDay day, DayAdjustment adjustment)
        {
            day.ComputedLateMinutes = day.LateMinutes;
            day.ComputedUndertimeMinutes = day.UndertimeMinutes;
            if (adjustment.LateMinutes.HasValue) day.LateMinutes = adjustment.LateMinutes.Value;
            if (adjustment.UndertimeMinutes.HasValue) day.UndertimeMinutes = adjustment.UndertimeMinutes.Value;
            day.IsAdjusted = day.LateMinutes != day.ComputedLateMinutes || day.UndertimeMinutes != day.ComputedUndertimeMinutes;
            day.AdjustReason = adjustment.Reason.Trim();
        }

        /// <summary>
        /// Rule 6: fills one day from hand-typed times and computes its Late and Undertime.
        /// Also used by the Edit DTR window to show the result while the person types.
        /// </summary>
        public static void FillDay(DtrDay day, DayCorrection typed, WorkSchedule schedule,
                                   DtrSettings settings, bool coveredByFile, LeaveEntry? leave = null)
        {
            day.AmArrival = typed.AmArrival;
            day.AmDeparture = typed.AmDeparture;
            day.PmArrival = typed.PmArrival;
            day.PmDeparture = typed.PmDeparture;
            day.Note = typed.Remark.Trim();
            day.ScanCount = typed.Times.Count(t => t.HasValue);
            day.Warning = "";
            FinishDay(day, schedule, settings, coveredByFile, leave);
        }

        /// <summary>
        /// Rule 7, half-day leave with one arrival and one departure scan: both scans belong to
        /// the half that was worked. Without this, leaving at 12:05 PM on a day with the afternoon
        /// on leave would be shown as "P.M. Departure" instead of "A.M. Departure".
        /// </summary>
        /// <remarks>
        /// A scan is only moved when its time fits: leaving in the first half of the afternoon,
        /// or arriving in the second half of the morning. Otherwise the person was clearly at work
        /// in the half that is on leave; the times stay where they are and the day gets a warning.
        /// </remarks>
        private static void KeepScansInWorkedHalf(DtrDay day, LeaveEntry leave, WorkSchedule s)
        {
            bool onlyFirstAndLast = day.AmArrival.HasValue && day.PmDeparture.HasValue &&
                                    !day.AmDeparture.HasValue && !day.PmArrival.HasValue;
            if (!onlyFirstAndLast)
                return;

            if (leave.Part == LeavePart.Afternoon && day.PmDeparture!.Value < Middle(s.LunchEnd, s.TimeOut))
            {
                day.AmDeparture = day.PmDeparture;   // worked the morning, went home around lunch time
                day.PmDeparture = null;
            }
            else if (leave.Part == LeavePart.Morning && day.AmArrival!.Value >= Middle(s.TimeIn, s.LunchStart))
            {
                day.PmArrival = day.AmArrival;       // came in late in the morning, for the afternoon
                day.AmArrival = null;
            }
        }

        /// <summary>
        /// Rules 2, 3, 4 and 7 for one day whose four times are already filled in:
        /// absent or not, late, undertime, and what a filed leave changes.
        /// </summary>
        private static void FinishDay(DtrDay day, WorkSchedule schedule, DtrSettings settings,
                                      bool coveredByFile, LeaveEntry? leave)
        {
            day.LateMinutes = 0;
            day.UndertimeMinutes = 0;
            day.IsAbsent = false;
            day.SuspendedFrom = null;
            day.IsCalendarHoliday = leave != null && leave.FromCalendar;
            day.HolidayName = leave != null ? leave.HolidayName : "";

            // Rule 9: work suspended FROM a time. Before that time it is an ordinary working day
            // (late, undertime, absent as usual); after it nothing is counted. From the start of the
            // day = the whole day is suspended.
            WorkSchedule hours = schedule;
            bool afternoonOff = false;
            if (leave != null && leave.From.HasValue)
            {
                TimeSpan from = leave.From.Value;
                if (from <= schedule.TimeIn + TimeSpan.FromMinutes(schedule.GraceMinutes))
                {
                    leave = new LeaveEntry(LeaveType.Suspended, LeavePart.WholeDay, leave.Wfh, leave.Idlar);
                }
                else
                {
                    day.SuspendedFrom = from;
                    if (from < schedule.TimeOut)
                    {
                        hours = schedule.Clone();
                        if (from <= schedule.LunchStart) { hours.LunchStart = from; afternoonOff = true; }
                        else if (from < schedule.LunchEnd) afternoonOff = true;
                        else hours.TimeOut = from;
                    }
                    leave = null;   // the day itself is not a leave
                }
            }

            day.Leave = leave == null ? LeaveType.None : leave.Type;
            day.LeavePart = leave == null ? LeavePart.WholeDay : leave.Part;
            day.LeaveLabel = leave == null ? "" : leave.DtrLabel;

            bool isWorkDay = schedule.IsWorkDay(day.Date.DayOfWeek);

            // ---- a whole day of leave: nothing to compute
            if (leave != null && leave.Part == LeavePart.WholeDay)
            {
                if (day.ScanCount > 0 && !leave.FromCalendar)   // working on a holiday of the calendar is fine
                    day.Warning = "filed as " + leave.Kind.Name + ", but there are times on this day";
                return;
            }

            // ---- no times at all
            if (day.ScanCount == 0)
            {
                if (leave != null)
                {
                    if (isWorkDay && coveredByFile)
                        day.Warning = "half-day " + leave.Kind.Name + ", but no times for the other half";
                    return;
                }

                // No times and no remark on a working day (or any Monday to Friday) = absent.
                // A remark (HOLIDAY...) explains the day instead.
                day.IsAbsent = day.Note.Length == 0 && schedule.ShowsAbsent(day.Date.DayOfWeek) && coveredByFile;
                return;
            }

            // ---- there are times: late and undertime (the half that is on leave is excused)
            if (isWorkDay)
                ComputeLateAndUndertime(day, hours, settings.CountLateFromOfficialTime,
                    leave != null && leave.Part == LeavePart.Morning,
                    afternoonOff || (leave != null && leave.Part == LeavePart.Afternoon));

            // ---- a half-day leave, but the person was at work in that half: someone should check
            if (leave != null)
            {
                bool morning = leave.Part == LeavePart.Morning;
                bool timesInThatHalf = morning
                    ? day.AmArrival.HasValue || day.AmDeparture.HasValue
                    : day.PmArrival.HasValue || day.PmDeparture.HasValue;

                if (timesInThatHalf)
                {
                    string warning = "half-day " + leave.Kind.Name + " in the " + (morning ? "morning" : "afternoon") +
                                     ", but there are times in that half";
                    day.Warning = day.Warning.Length == 0 ? warning : day.Warning + "; " + warning;
                }
            }
        }

        /// <summary>
        /// Drops the seconds (the form only shows hours and minutes) and removes
        /// double scans: a scan within one minute of the previous one is ignored.
        /// </summary>
        internal static List<TimeSpan> CleanScans(List<DateTime> scans)
        {
            var result = new List<TimeSpan>();
            foreach (DateTime scan in scans.OrderBy(s => s))
            {
                var time = new TimeSpan(scan.Hour, scan.Minute, 0);
                if (result.Count > 0 && (time - result[result.Count - 1]).TotalMinutes <= 1)
                    continue;
                result.Add(time);
            }
            return result;
        }

        /// <summary>Rule 1: decides which scan goes into which of the four columns.</summary>
        internal static void AssignScans(List<TimeSpan> scans, DtrDay day, WorkSchedule s)
        {
            TimeSpan noon = s.LunchStart;
            TimeSpan lunchMiddle = Middle(s.LunchStart, s.LunchEnd);
            int n = scans.Count;

            if (n == 1)
            {
                TimeSpan only = scans[0];
                if (only < noon) day.AmArrival = only;
                else if (only < Middle(s.LunchEnd, s.TimeOut)) day.PmArrival = only;
                else day.PmDeparture = only;

                day.Warning = "only one scan";
                return;
            }

            TimeSpan first = scans[0];
            TimeSpan last = scans[n - 1];

            if (first < noon) day.AmArrival = first; else day.PmArrival = first;
            if (last >= noon) day.PmDeparture = last; else day.AmDeparture = last;

            if (n == 2)
                return;

            // From here on there are scans in between the first and the last one.
            bool worksBothHalves = first < noon && last >= noon;
            TimeSpan windowStart = s.LunchStart - TimeSpan.FromHours(1);
            TimeSpan windowEnd = s.LunchEnd + TimeSpan.FromHours(1);

            if (!worksBothHalves)
            {
                day.Warning = n + " scans, only the first and last were used";
                return;
            }

            if (n == 3)
            {
                TimeSpan middle = scans[1];
                if (middle < windowStart || middle > windowEnd)
                {
                    day.Warning = "3 scans, the middle one is not near lunch time";
                    return;
                }

                if (middle < lunchMiddle) day.AmDeparture = middle; else day.PmArrival = middle;
                day.Warning = "one lunch scan is missing";
                return;
            }

            // 4 or more scans: use the pair of neighbouring scans that best surrounds lunch time.
            int best = -1;
            double bestDistance = double.MaxValue;
            for (int i = 1; i <= n - 3; i++)
            {
                TimeSpan a = scans[i], b = scans[i + 1];
                double distance = 0;
                if (lunchMiddle < a) distance = (a - lunchMiddle).TotalMinutes;
                else if (lunchMiddle > b) distance = (lunchMiddle - b).TotalMinutes;

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }

            TimeSpan lunchOut = scans[best], lunchIn = scans[best + 1];
            if (lunchIn < windowStart || lunchOut > windowEnd)
            {
                day.Warning = n + " scans, none of the middle ones are near lunch time";
                return;
            }

            day.AmDeparture = lunchOut;
            day.PmArrival = lunchIn;
            if (n > 4)
                day.Warning = n + " scans, the extra ones were ignored";
        }

        /// <summary>Rules 2 and 3.</summary>
        /// <param name="morningOnLeave">Half-day leave in the morning: nothing is counted for the morning.</param>
        /// <param name="afternoonOnLeave">Half-day leave in the afternoon: nothing is counted for the afternoon.</param>
        internal static void ComputeLateAndUndertime(DtrDay day, WorkSchedule s, bool countLateFromOfficialTime,
                                                     bool morningOnLeave = false, bool afternoonOnLeave = false)
        {
            TimeSpan late = TimeSpan.Zero;
            TimeSpan undertime = TimeSpan.Zero;
            TimeSpan graceEnd = s.TimeIn + TimeSpan.FromMinutes(s.GraceMinutes);

            // A half day with no scans at all counts as undertime. With only one scan we cannot
            // tell a half day from a forgotten scan, so that case is left for a person to check.
            bool canJudgeHalfDays = day.ScanCount >= 2;

            // ---- the morning
            if (!morningOnLeave)
            {
                if (day.AmArrival.HasValue && day.AmArrival.Value > graceEnd)
                {
                    TimeSpan countFrom = countLateFromOfficialTime ? s.TimeIn : graceEnd;
                    late += Earlier(day.AmArrival.Value, s.LunchStart) - countFrom;
                }

                if (day.AmDeparture.HasValue && day.AmDeparture.Value < s.LunchStart)
                    undertime += s.LunchStart - Later(day.AmDeparture.Value, s.TimeIn);

                if (canJudgeHalfDays && !day.AmArrival.HasValue && !day.AmDeparture.HasValue)
                    undertime += s.LunchStart - s.TimeIn;
            }

            // ---- the afternoon
            if (!afternoonOnLeave)
            {
                if (day.PmArrival.HasValue && day.PmArrival.Value > s.LunchEnd)
                    late += Earlier(day.PmArrival.Value, s.TimeOut) - s.LunchEnd;

                if (day.PmDeparture.HasValue && day.PmDeparture.Value < s.TimeOut)
                    undertime += s.TimeOut - Later(day.PmDeparture.Value, s.LunchEnd);

                if (canJudgeHalfDays && !day.PmArrival.HasValue && !day.PmDeparture.HasValue)
                    undertime += s.TimeOut - s.LunchEnd;
            }

            day.LateMinutes = Math.Max(0, (int)Math.Round(late.TotalMinutes));
            day.UndertimeMinutes = Math.Max(0, (int)Math.Round(undertime.TotalMinutes));
        }

        private static TimeSpan Middle(TimeSpan a, TimeSpan b)
        {
            return TimeSpan.FromTicks((a.Ticks + b.Ticks) / 2);
        }

        private static TimeSpan Earlier(TimeSpan a, TimeSpan b)
        {
            return a < b ? a : b;
        }

        private static TimeSpan Later(TimeSpan a, TimeSpan b)
        {
            return a > b ? a : b;
        }
    }
}
