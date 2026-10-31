namespace Dat2DTR
{
    /// <summary>
    /// This is where the raw scans of one employee are turned into a Daily Time Record, and where it
    /// computes Late and Undertime.
    ///
    /// THE RULES (shown here for Schedule A: 7:00 AM - 6:00 PM, lunch 12:00 - 1:00 PM,
    /// grace period 15 minutes. Schedules B, C and D work the same way with their own times.)
    ///
    ///  1. Which scan goes in which column
    ///       - first scan of the day  -> A.M. Arrival   (P.M. Arrival if it is already past 12:00)
    ///       - last scan of the day   -> P.M. Departure (A.M. Departure if it is before 12:00)
    ///       - the two scans around lunch time -> A.M. Departure and P.M. Arrival
    ///       - two scans within one minute of each other count as one (double scan)
    ///       - forgetting to scan at lunch is fine: with only the arrival and the departure (or only one
    ///         lunch scan) the day is judged on the arrival and the departure, with no warning.
    ///
    ///  2. LATE
    ///       - A.M. Arrival up to 7:15 is NOT late, IF the minutes are made up at the end of the day:
    ///         arriving 7:14 means leaving at 6:14 PM or later. Leaving earlier (6:08 PM, or with no
    ///         P.M. Departure scan) loses the grace period: 14 minutes late. Undertime is still only
    ///         counted before 6:00 PM. (With the afternoon on leave or suspended there is nothing to
    ///         make up, so the grace period simply applies.)
    ///       - From 7:16 it is late, counted from 7:00.
    ///       - MONDAYS: the grace period never goes past 7:15 AM. A 7:00 AM start keeps it, but 8:00 AM
    ///         (Schedule B) and 9:00 AM (Schedule D) must be met sharp on a Monday: 8:01 = 1 minute late.
    ///         Tuesday to Friday have the full 15 minutes.
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
    ///     Leaving before that time is undertime, counted up to it. When the time is in the lunch break
    ///     (for example from 1:00 PM), the day ends at that time: leaving at 12:26 PM = 34 minutes undertime,
    ///     leaving at 11:30 AM = 1 hour 30 minutes.
    ///     A holiday in the holiday calendar counts as a whole-day Holiday for everyone,
    ///     unless a leave is filed for that day.
    ///
    /// 10. FLEXI-TIME (a day ticked "Flexi-Time" in Edit DTR): the person may work the office hours of another
    ///     schedule with the same number of working hours that day. The program picks the schedule that fits the
    ///     times best (the least late + undertime; a tie keeps the person's own schedule), and computes the day with it.
    ///     Example: Schedule B (8:00 - 5:00) working 7:00 AM - 4:00 PM -> Schedule C (7:00 - 4:00): not late, no undertime.
    ///     Working 7:30 AM - 4:30 PM -> B and C are both 30 minutes short, so B is kept: 30 minutes undertime.
    ///     Schedule A (10 working hours) has no other schedule of the same length, so it stays on Schedule A.
    ///     A day that is NOT ticked follows the person's own schedule: 7:00 AM - 4:00 PM on Schedule B = 1 hour undertime.
    ///
    /// 11. NO LATE / UNDERTIME (a day ticked "No Late / Undertime" in Edit DTR): the day keeps its times, but no Late and no
    ///     Undertime are counted, printed or deducted - for example a Friday when everyone went back to 7-4 or 8-5.
    ///     What would have been counted stays visible in Edit DTR. It does not change ABSENT (a day with no scans).
    /// </summary>
    public static class DtrBuilder
    {
        /// <param name="schedule">This is the office hours this employee follows.</param>
        /// <param name="corrections">This is the hand-typed days, or null to use only the biometrics data.</param>
        /// <param name="leaves">This is the filed leaves, or null to make the DTR without any leave.</param>
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

                // This is why only dates the file actually covers can be ABSENT: days before the first
                // scan or after the last scan in the file are unknown, so they stay blank.
                bool coveredByFile = log.IsCovered(day.Date);

                // This is for work from home without the IDLAR: it does not count yet, so the day is shown as if nothing was filed.
                LeaveEntry? filed = leaves == null ? null : leaves.GetForDtr(employeeId, day.Date, schedule);
                LeaveEntry? leave = filed != null && filed.Excuses ? filed : null;
                DayCorrection? typed = corrections == null ? null : corrections.Get(employeeId, day.Date);
                DayAdjustment? adjustment = corrections == null ? null : corrections.GetAdjustment(employeeId, day.Date);
                bool flexi = adjustment != null && adjustment.Flexi;
                if (typed != null)
                {
                    FillDay(day, typed, schedule, settings, coveredByFile, leave, flexi);
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
                    FinishDay(day, schedule, settings, coveredByFile, leave, flexi);
                }

                // This is the Late / Undertime typed in by hand (Edit DTR): it replaces what was computed.
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
        /// This is Rule 8: Late and Undertime typed in by hand (for example when work was suspended at
        /// 3:00 PM) replace the computed ones. The computed ones are kept to show beside them.
        /// </summary>
        public static void ApplyAdjustment(DtrDay day, DayAdjustment adjustment)
        {
            day.ComputedLateMinutes = day.LateMinutes;
            day.ComputedUndertimeMinutes = day.UndertimeMinutes;
            day.NoLateUndertime = adjustment.NoLateUndertime;
            if (adjustment.NoLateUndertime)
            {
                // This is Rule 11: a day ticked No Late / Undertime keeps its times, and nothing is counted.
                day.LateMinutes = 0;
                day.UndertimeMinutes = 0;
            }
            else
            {
                if (adjustment.LateMinutes.HasValue) day.LateMinutes = adjustment.LateMinutes.Value;
                if (adjustment.UndertimeMinutes.HasValue) day.UndertimeMinutes = adjustment.UndertimeMinutes.Value;
            }
            day.IsAdjusted = day.LateMinutes != day.ComputedLateMinutes || day.UndertimeMinutes != day.ComputedUndertimeMinutes;
            day.AdjustReason = adjustment.Reason.Trim();
        }

        /// <summary>
        /// This is Rule 6: it fills one day from hand-typed times and computes its Late and Undertime.
        /// Also used by the Edit DTR window to show the result while the person types.
        /// </summary>
        public static void FillDay(DtrDay day, DayCorrection typed, WorkSchedule schedule,
                                   DtrSettings settings, bool coveredByFile, LeaveEntry? leave = null, bool flexi = false)
        {
            day.AmArrival = typed.AmArrival;
            day.AmDeparture = typed.AmDeparture;
            day.PmArrival = typed.PmArrival;
            day.PmDeparture = typed.PmDeparture;
            day.Note = typed.Remark.Trim();
            day.ScanCount = typed.Times.Count(t => t.HasValue);
            day.Warning = "";
            FinishDay(day, schedule, settings, coveredByFile, leave, flexi);
        }

        /// <summary>
        /// This is Late and Undertime computed again for a day that is already made (from the scans), with or without
        /// Flexi-Time. Used by the Edit DTR window when the Flexi-Time box of a day is ticked or unticked.
        /// </summary>
        public static void Recompute(DtrDay day, WorkSchedule schedule, DtrSettings settings, bool coveredByFile,
                                     LeaveEntry? leave, bool flexi)
        {
            FinishDay(day, schedule, settings, coveredByFile, leave, flexi);
        }

        /// <summary>This is the leave the DTR uses for a day: work from home without the IDLAR does not count yet.</summary>
        public static LeaveEntry? LeaveForDtr(LeaveStore? leaves, string employeeId, DateTime date, WorkSchedule schedule)
        {
            LeaveEntry? filed = leaves == null ? null : leaves.GetForDtr(employeeId, date, schedule);
            return filed != null && filed.Excuses ? filed : null;
        }

        /// <summary>
        /// This is Rule 7, half-day leave with one arrival and one departure scan: both scans belong to
        /// the half that was worked. Without this, leaving at 12:05 PM on a day with the afternoon
        /// on leave would be shown as "P.M. Departure" instead of "A.M. Departure".
        /// </summary>
        /// <remarks>
        /// This is when a scan is moved: only when its time fits: leaving in the first half of the afternoon,
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
                day.AmDeparture = day.PmDeparture;   // This is someone who worked the morning and went home around lunch time
                day.PmDeparture = null;
            }
            else if (leave.Part == LeavePart.Morning && day.AmArrival!.Value >= Middle(s.TimeIn, s.LunchStart))
            {
                day.PmArrival = day.AmArrival;       // This is someone who came in late in the morning, for the afternoon
                day.AmArrival = null;
            }
        }

        /// <summary>
        /// This is Rules 2, 3, 4 and 7 for one day whose four times are already filled in:
        /// absent or not, late, undertime, and what a filed leave changes.
        /// </summary>
        private static void FinishDay(DtrDay day, WorkSchedule schedule, DtrSettings settings,
                                      bool coveredByFile, LeaveEntry? leave, bool flexi = false)
        {
            day.LateMinutes = 0;
            day.MorningLateMinutes = 0;
            day.UndertimeMinutes = 0;
            day.IsAbsent = false;
            day.SuspendedFrom = null;
            day.IsFlexi = false;
            day.IsCalendarHoliday = leave != null && leave.FromCalendar;
            day.HolidayName = leave != null ? leave.HolidayName : "";

            // This is Rule 10: a Flexi-Time day is computed with the schedule that fits its times best.
            if (flexi && day.ScanCount > 0 && schedule.IsWorkDay(day.Date.DayOfWeek) &&
                !(leave != null && !leave.From.HasValue && leave.Part == LeavePart.WholeDay))
            {
                WorkSchedule best = BestFlexiSchedule(day, schedule, settings, coveredByFile, leave);
                FinishDay(day, best, settings, coveredByFile, leave, false);
                day.IsFlexi = true;
                day.FlexiSchedule = best.Letter;
                day.FlexiIn = best.TimeIn;
                day.FlexiOut = best.TimeOut;
                return;
            }

            // This is Rule 9: work suspended FROM a time. Before that time it is an ordinary working day
            // (late, undertime, absent as usual); after it nothing is counted. From the start of the
            // day = the whole day is suspended.
            WorkSchedule hours = schedule;

            bool afternoonOff = false;
            TimeSpan? dismissedInLunch = null;   // This is a suspension time in the lunch break (after 12:00, up to 1:00 PM)
            if (leave != null && leave.From.HasValue)
            {
                TimeSpan from = leave.From.Value;
                if (from <= schedule.TimeIn + TimeSpan.FromMinutes(schedule.GraceOn(day.Date.DayOfWeek)))
                {
                    leave = new LeaveEntry(LeaveType.Suspended, LeavePart.WholeDay, leave.Wfh, leave.Idlar);
                }
                else
                {
                    day.SuspendedFrom = from;
                    if (from < hours.TimeOut)
                    {
                        hours = hours.Clone();
                        if (from <= schedule.LunchStart) { hours.LunchStart = from; afternoonOff = true; }
                        else if (from <= schedule.LunchEnd) { afternoonOff = true; dismissedInLunch = from; }
                        else hours.TimeOut = from;
                    }
                    leave = null;   // This is because the day itself is not a leave
                }
            }

            day.Leave = leave == null ? LeaveType.None : leave.Type;
            day.LeavePart = leave == null ? LeavePart.WholeDay : leave.Part;
            day.LeaveLabel = leave == null ? "" : leave.DtrLabel;

            bool isWorkDay = schedule.IsWorkDay(day.Date.DayOfWeek);

            // This is a whole day of leave: nothing to compute ----
            if (leave != null && leave.Part == LeavePart.WholeDay)
            {
                if (day.ScanCount > 0 && !leave.FromCalendar)   // This is because working on a holiday of the calendar is fine
                    day.Warning = "filed as " + leave.Kind.Name + ", but there are times on this day";
                return;
            }

            // This is a day with no times at all ----
            if (day.ScanCount == 0)
            {
                if (leave != null)
                {
                    if (isWorkDay && coveredByFile)
                        day.Warning = "half-day " + leave.Kind.Name + ", but no times for the other half";
                    return;
                }

                // This is the absent rule: no times and no remark on a working day (or any Monday to Friday) = absent.
                // A remark (HOLIDAY...) explains the day instead.
                day.IsAbsent = day.Note.Length == 0 && schedule.ShowsAbsent(day.Date.DayOfWeek) && coveredByFile;
                return;
            }

            // This is a day with times: late and undertime (the half that is on leave is excused) ----
            if (isWorkDay)
                ComputeLateAndUndertime(day, hours, settings.CountLateFromOfficialTime,
                    leave != null && leave.Part == LeavePart.Morning,
                    afternoonOff || (leave != null && leave.Part == LeavePart.Afternoon),
                    !day.SuspendedFrom.HasValue);   // This is false when released early by a suspension: nothing to make up

            // This is a suspension time in the lunch break (e.g. from 1:00 PM): the day ends at that time, so leaving before it
            // is undertime up to it. The morning (up to 12:00) is already counted above; this adds the part after 12:00.
            if (isWorkDay && dismissedInLunch.HasValue)
            {
                TimeSpan? left = day.PmDeparture ?? day.AmDeparture;
                if (left.HasValue && left.Value < dismissedInLunch.Value)
                {
                    TimeSpan from = left.Value > hours.LunchStart ? left.Value : hours.LunchStart;
                    day.UndertimeMinutes += (int)Math.Round((dismissedInLunch.Value - from).TotalMinutes);
                }
            }

            // This is a half-day leave, but the person was at work in that half: someone should check ----
            if (leave != null)
            {
                bool morning = leave.Part == LeavePart.Morning;
                bool timesInThatHalf = morning
                    ? day.AmArrival.HasValue || day.AmDeparture.HasValue
                    : day.PmArrival.HasValue || day.PmDeparture.HasValue;

                if (timesInThatHalf)
                {
                    AddWarning(day, "half-day " + leave.Kind.Name + " in the " + (morning ? "morning" : "afternoon") +
                                    ", but there are times in that half");
                }
            }
        }

        /// <summary>
        /// This is where the seconds are dropped (the form only shows hours and minutes) and
        /// double scans are removed: a scan within one minute of the previous one is ignored.
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

        /// <summary>This is Rule 1: it decides which scan goes into which of the four columns.</summary>
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

            // This is the part where there are scans in between the first and the last one.
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

                // This is one lunch scan missing: that is fine (the day is judged on the arrival and the departure).
                if (middle < lunchMiddle) day.AmDeparture = middle; else day.PmArrival = middle;
                return;
            }

            // This is for 4 or more scans: use the pair of neighbouring scans that best surrounds lunch time.
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

        /// <summary>This is Rules 2 and 3.</summary>
        /// <param name="morningOnLeave">This is a half-day leave in the morning: nothing is counted for the morning.</param>
        /// <param name="afternoonOnLeave">This is a half-day leave in the afternoon: nothing is counted for the afternoon.</param>
        /// <param name="graceMustBeMadeUp">This is false on a day work was suspended from a time: arriving inside the
        /// grace period is then simply not late.</param>
        internal static void ComputeLateAndUndertime(DtrDay day, WorkSchedule s, bool countLateFromOfficialTime,
                                                     bool morningOnLeave = false, bool afternoonOnLeave = false,
                                                     bool graceMustBeMadeUp = true)
        {
            TimeSpan late = TimeSpan.Zero;
            TimeSpan undertime = TimeSpan.Zero;
            // This is the grace period of the day: shorter on a Monday (see WorkSchedule.MondayGraceUntil).
            int grace = day.Date == default(DateTime) ? s.GraceMinutes : s.GraceOn(day.Date.DayOfWeek);
            TimeSpan graceEnd = s.TimeIn + TimeSpan.FromMinutes(grace);

            // This is the half-day rule: a half day with no scans at all counts as undertime. With only one scan we cannot
            // tell a half day from a forgotten scan, so that case is left for a person to check.
            bool canJudgeHalfDays = day.ScanCount >= 2;

            // This is the morning ----
            if (!morningOnLeave)
            {
                if (day.AmArrival.HasValue && day.AmArrival.Value > graceEnd)
                {
                    TimeSpan countFrom = countLateFromOfficialTime ? s.TimeIn : graceEnd;
                    late += Earlier(day.AmArrival.Value, s.LunchStart) - countFrom;
                }
                else if (graceMustBeMadeUp && !afternoonOnLeave && day.AmArrival.HasValue && day.AmArrival.Value > s.TimeIn)
                {
                    // This is inside the grace period: the minutes must be made up at the end of the day
                    // (7:14 in -> out at 6:14 PM or later). Otherwise they are late.
                    TimeSpan minutes = day.AmArrival.Value - s.TimeIn;
                    bool madeUp = day.PmDeparture.HasValue && day.PmDeparture.Value >= s.TimeOut + minutes;
                    if (!madeUp) late += minutes;
                }

                if (day.AmDeparture.HasValue && day.AmDeparture.Value < s.LunchStart)
                    undertime += s.LunchStart - Later(day.AmDeparture.Value, s.TimeIn);

                if (canJudgeHalfDays && !day.AmArrival.HasValue && !day.AmDeparture.HasValue)
                    undertime += s.LunchStart - s.TimeIn;
            }

            TimeSpan morningLate = late;   // This is the late of the A.M. Arrival only (for the Monday report)

            // This is the afternoon ----
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
            day.MorningLateMinutes = Math.Max(0, (int)Math.Round(morningLate.TotalMinutes));
            day.UndertimeMinutes = Math.Max(0, (int)Math.Round(undertime.TotalMinutes));
        }

        /// <summary>
        /// This is Rule 10: the schedule a Flexi-Time day fits best. Only schedules with the same working hours as the
        /// person's own (lunch not counted) and with that day as a working day are tried. The one with the least
        /// late + undertime wins; a tie keeps the person's own schedule, then the first in the list (A, B, C, D).
        /// </summary>
        public static WorkSchedule BestFlexiSchedule(DtrDay day, WorkSchedule own, DtrSettings settings,
                                                     bool coveredByFile, LeaveEntry? leave)
        {
            var candidates = new List<WorkSchedule> { own };
            foreach (WorkSchedule other in settings.Schedules)
                if (other != own && other.WorkingHours == own.WorkingHours && other.IsWorkDay(day.Date.DayOfWeek))
                    candidates.Add(other);

            WorkSchedule best = own;
            int bestMinutes = int.MaxValue;
            foreach (WorkSchedule candidate in candidates)
            {
                DtrDay trial = day.Clone();
                FinishDay(trial, candidate, settings, coveredByFile, leave, false);
                int minutes = trial.LateMinutes + trial.UndertimeMinutes;
                if (minutes < bestMinutes)
                {
                    best = candidate;
                    bestMinutes = minutes;
                }
            }
            return best;
        }

        /// <summary>This is a warning added to the day, once (a day computed again does not get it twice).</summary>
        private static void AddWarning(DtrDay day, string warning)
        {
            if (day.Warning.Length == 0) day.Warning = warning;
            else if (!day.Warning.Contains(warning)) day.Warning += "; " + warning;
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
