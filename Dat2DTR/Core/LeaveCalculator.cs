using System.Globalization;

namespace Dat2DTR
{
    /// <summary>
    /// This is the set of numbers used to compute leave. They follow the Civil Service Commission
    /// rules on leave (Omnibus Rules on Leave, CSC MC No. 41 s. 1998 as amended).
    /// </summary>
    public static class LeaveRules
    {
        /// <summary>This is the Vacation Leave and Sick Leave credits earned in one month (15 days a year each).</summary>
        public const decimal MonthlyCredit = 1.25m;

        // This is the CSC table "Conversion of working hours/minutes into fractions of a day"
        // (based on an 8-hour day). Position = minutes, value = thousandths of a day.
        //   1 minute = .002     30 minutes = .062     60 minutes (1 hour) = .125     8 hours = 1.000
        private static readonly int[] MinuteTable =
        {
              0,   2,   4,   6,   8,  10,  12,  15,  17,  19,
             21,  23,  25,  27,  29,  31,  33,  35,  37,  40,
             42,  44,  46,  48,  50,  52,  54,  56,  58,  60,
             62,  65,  67,  69,  71,  73,  75,  77,  79,  81,
             83,  85,  87,  90,  92,  94,  96,  98, 100, 102,
            104, 106, 108, 110, 112, 115, 117, 119, 121, 123,
            125
        };

        // This is the CSC "Table of computation - for leave without pay": the leave credits earned in a month
        // by an employee who has no Vacation Leave credits left, by days without pay.
        // Position i = (i + 1) / 2 days without pay  (0.5, 1.0, 1.5 ... 30.0), value = thousandths of a day.
        private static readonly int[] EarnedWithoutPayTable =
        {
            1229, 1208, 1188, 1167, 1146, 1125, 1104, 1083, 1063, 1042,
            1021, 1000,  979,  958,  938,  917,  896,  875,  854,  833,
             813,  792,  771,  750,  729,  708,  687,  667,  646,  625,
             604,  583,  562,  542,  521,  500,  479,  458,  437,  417,
             396,  375,  354,  333,  312,  292,  271,  250,  229,  208,
             187,  167,  146,  125,  104,   83,   62,   42,   21,    0
        };

        // This is the CSC "Table of computation - for leave with pay": leave credits earned for days of service
        // (used for an employee who started or left in the middle of a month). Position = days - 1.
        private static readonly int[] EarnedPerDayTable =
        {
              42,   83,  125,  167,  208,  250,  292,  333,  375,  417,
             458,  500,  542,  583,  625,  667,  708,  750,  792,  833,
             875,  917,  958, 1000, 1042, 1083, 1125, 1167, 1208, 1250
        };

        /// <summary>
        /// This is the credits earned in a month with that many days without pay (whole and half days;
        /// anything in between is rounded down to the half day). 0 days = the usual 1.25.
        /// </summary>
        public static decimal EarnedWithoutPay(decimal daysWithoutPay)
        {
            int halves = (int)Math.Floor(daysWithoutPay * 2m);
            if (halves <= 0) return MonthlyCredit;
            if (halves > EarnedWithoutPayTable.Length) halves = EarnedWithoutPayTable.Length;
            return EarnedWithoutPayTable[halves - 1] / 1000m;
        }

        /// <summary>This is the credits earned for days of service in a month (1 to 30 days).</summary>
        public static decimal EarnedForDays(int days)
        {
            if (days <= 0) return 0m;
            if (days >= 30) return MonthlyCredit;
            return EarnedPerDayTable[days - 1] / 1000m;
        }

        /// <summary>
        /// This is where late or undertime minutes are turned into a fraction of a day, the way it is
        /// written on the leave card. Example: 3 hours 15 minutes = .375 + .031 = .406
        /// </summary>
        public static decimal MinutesToDays(int minutes)
        {
            if (minutes <= 0) return 0m;
            int hours = minutes / 60, rest = minutes % 60;
            return (hours * 125 + MinuteTable[rest]) / 1000m;
        }

        /// <summary>
        /// This is how many leave credits ONE whole day of leave costs for this schedule.
        /// A day of leave is counted in 8-hour days, so a 10-hour working day
        /// (Schedule A, 7:00 AM - 6:00 PM) costs 1.25 and an 8-hour day costs 1.
        /// </summary>
        public static decimal DayCharge(WorkSchedule schedule)
        {
            double hours = ((schedule.LunchStart - schedule.TimeIn) + (schedule.TimeOut - schedule.LunchEnd)).TotalHours;
            decimal charge = Math.Round((decimal)hours / 8m, 3);
            return charge < 1m ? 1m : charge;
        }

        /// <summary>This is "1.250" - the way leave credits are written.</summary>
        public static string Format(decimal credits)
        {
            return credits.ToString("0.000", CultureInfo.InvariantCulture);
        }

        /// <summary>This is "1.25" or "1" - a number without useless zeros.</summary>
        public static string FormatShort(decimal number)
        {
            return number.ToString("0.###", CultureInfo.InvariantCulture);
        }

        /// <summary>This is a number of days as text: "1 day", "1/2 day", "2.5 days"</summary>
        public static string FormatDays(decimal days)
        {
            if (days == 0.5m) return "1/2 day";
            if (days == 1m) return "1 day";
            return FormatShort(days) + " days";
        }

        /// <summary>This is minutes as text: "3 h 15 min", "5 h", "26 min"</summary>
        public static string FormatMinutes(int minutes)
        {
            int h = minutes / 60, m = minutes % 60;
            if (h == 0) return m + " min";
            if (m == 0) return h + " h";
            return h + " h " + m + " min";
        }

        /// <summary>This is where a typed number such as "20.027" is read (a comma is also accepted). Empty gives null.</summary>
        public static bool TryParseCredits(string? text, out decimal? value)
        {
            value = null;
            string typed = (text ?? "").Trim().Replace(',', '.');
            if (typed.Length == 0)
                return true;

            decimal number;
            if (!decimal.TryParse(typed, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                                  CultureInfo.InvariantCulture, out number))
                return false;
            if (number < -999m || number > 999m)
                return false;

            value = Math.Round(number, 3);
            return true;
        }
    }

    /// <summary>This is one month of the leave card of one employee (one row of the card).</summary>
    public sealed class LeaveMonth
    {
        public int Year, Month;

        /// <summary>This is true for the month that is open now; false for a month the program remembers.</summary>
        public bool IsCurrent;

        /// <summary>This is the leave credits that one whole day of leave costs (1.25 for a 10-hour day).</summary>
        public decimal DayCharge = 1m;

        public decimal VlEarned = LeaveRules.MonthlyCredit, SlEarned = LeaveRules.MonthlyCredit;

        /// <summary>This is true when a person typed the credits earned; otherwise they come from the CSC tables.</summary>
        public bool VlEarnedTyped, SlEarnedTyped;

        /// <summary>This is the days without pay used to look up the credits earned (0 = the usual 1.25 was earned).</summary>
        public decimal WithoutPayDays;

        public int LateMinutes, UndertimeMinutes;
        public decimal LateDays, UndertimeDays;

        /// <summary>This is the days of each kind of leave in the month (1 = whole day, 0.5 = half day).</summary>
        public decimal VacationDays, ForcedDays, SickDays, SplDays, WellnessDays, CtoDays, SoloDays;

        /// <summary>This is the credits wanted from Vacation Leave for Vacation and Forced leave, and from Sick Leave.</summary>
        public decimal VlLeave, SlLeave;

        /// <summary>This is the part of the sick leave that was charged to Vacation Leave because Sick Leave ran out.</summary>
        public decimal SlToVl;

        /// <summary>This is what was really taken from the credits ("Enjoyed" on the leave card).</summary>
        public decimal VlEnjoyed, SlEnjoyed;

        /// <summary>This is the days without pay: what the Vacation Leave credits could not cover.</summary>
        public decimal Lwop;

        /// <summary>This is the balance at the start and at the end ("Net") of the month. null = not known.</summary>
        public decimal? VlStart, SlStart, VlNet, SlNet;

        /// <summary>This is true when a person typed the starting balance (instead of it being carried over).</summary>
        public bool VlStartTyped, SlStartTyped;

        /// <summary>This is "Balance below zero: make it 0" (ticked in Edit Leave): the balance stops at 0, the rest is without pay.</summary>
        public bool ZeroBalance;

        /// <summary>This is the last date the .dat file covered when this month was computed.</summary>
        public DateTime? DataThrough;

        /// <summary>This is the days of leave, e.g. "21, 22, 25 (1/2)" - one text per kind.</summary>
        public string VacationDates = "", ForcedDates = "", SickDates = "", SplDates = "", WellnessDates = "", CtoDates = "",
                      SoloDates = "";

        /// <summary>This is all the days of leave of the month together (whole day = 1, half day = 0.5).</summary>
        public decimal AllLeaveDays
        {
            get { return VacationDays + ForcedDays + SickDays + SplDays + WellnessDays + CtoDays + SoloDays; }
        }

        /// <summary>This is what goes in "Leave Date/s" of the VACATION part: vacation and forced leave together.</summary>
        public string VlDatesText
        {
            get
            {
                if (ForcedDates.Length == 0) return VacationDates;
                string forced = ForcedDates + " (FL)";
                return VacationDates.Length == 0 ? forced : VacationDates + "; " + forced;
            }
        }

        public DateTime FirstDay
        {
            get { return new DateTime(Year, Month, 1); }
        }

        public string MonthName
        {
            get { return FirstDay.ToString("MMMM", CultureInfo.InvariantCulture); }
        }

        /// <summary>This is true when the .dat file may not have covered the whole month.</summary>
        public bool MayBeIncomplete
        {
            get
            {
                if (!DataThrough.HasValue) return false;

                // This is the last Monday-to-Friday of the month. One day earlier is still fine:
                // in a Monday-to-Thursday office nobody scans on a Friday.
                var last = new DateTime(Year, Month, DateTime.DaysInMonth(Year, Month));
                while (last.DayOfWeek == DayOfWeek.Saturday || last.DayOfWeek == DayOfWeek.Sunday)
                    last = last.AddDays(-1);
                return DataThrough.Value < last.AddDays(-1);
            }
        }
    }

    public enum LeaveLineKind
    {
        Heading,
        Item,
        Total
    }

    /// <summary>This is one line of the written-out computation ("Late, 3 h 15 min ..... - 0.406").</summary>
    public sealed class LeaveLine
    {
        public LeaveLine(LeaveLineKind kind, string text, string value)
        {
            Kind = kind;
            Text = text;
            Value = value;
        }

        public LeaveLineKind Kind { get; private set; }
        public string Text { get; private set; }
        public string Value { get; private set; }
    }

    /// <summary>
    /// This is the "Computation of Leaves" of one employee: the leave card of the year
    /// (one row per month) and the written-out computation of the open month.
    /// </summary>
    public sealed class LeaveCard
    {
        public string EmployeeId = "";
        public string EmployeeName = "";

        /// <summary>This is the first day of the open month.</summary>
        public DateTime MonthStart;

        /// <summary>This is January to December of the year. null = the program knows nothing about that month.</summary>
        public LeaveMonth?[] Months = new LeaveMonth?[12];

        /// <summary>This is the days used from January up to the open month.</summary>
        public decimal SplUsed, ForcedUsed, WellnessUsed, CtoUsed, SoloUsed;

        /// <summary>This is the SPL and Wellness Leave left at the start of the open month when it was typed in Edit Leave (else null).</summary>
        public decimal? SplStartTyped, WellnessStartTyped, ForcedStartTyped;

        /// <summary>This is the CTO of the open month: given, used, expired, and what is left.</summary>
        public CtoMonth Cto = new CtoMonth();

        /// <summary>This is the Solo Parent Leave of the open month: given, used, expired, and what is left.</summary>
        public CtoMonth Solo = new CtoMonth();

        /// <summary>
        /// This is the History of Availment of Leave: the leaves filed in each month of the year, January to
        /// December (index 0 = January), with the days they were used. Only the days and dates are filled in.
        /// </summary>
        public LeaveMonth[] History = new LeaveMonth[12];

        /// <summary>This is the schedule as text, e.g. "Schedule A:  7:00 AM - 6:00 PM, Mon to Thu"</summary>
        public string ScheduleText = "";

        /// <summary>This is the computation of the open month, line by line.</summary>
        public List<LeaveLine> Breakdown = new List<LeaveLine>();

        /// <summary>This is the list of things a person should know or check.</summary>
        public List<string> Notes = new List<string>();

        public int Year
        {
            get { return MonthStart.Year; }
        }

        /// <summary>This is the open month. It always exists.</summary>
        public LeaveMonth Current
        {
            get { return Months[MonthStart.Month - 1]!; }
        }

        public string NameLine
        {
            get { return EmployeeName.Length == 0 ? EmployeeId : EmployeeId + " -> " + EmployeeName; }
        }

        public string MonthText
        {
            get { return MonthStart.ToString("MMMM yyyy", CultureInfo.InvariantCulture); }
        }

        public string PreviousMonthText
        {
            get { return MonthStart.AddMonths(-1).ToString("MMMM yyyy", CultureInfo.InvariantCulture); }
        }

        /// <summary>This is "One whole day of leave = 1.25 days of leave credits."</summary>
        public string DayChargeText
        {
            get
            {
                decimal charge = Current.DayCharge;
                return "One whole day of leave = " + LeaveRules.FormatShort(charge) +
                       (charge == 1m ? " day" : " days") + " of leave credits.";
            }
        }
    }

    /// <summary>This is the CTO (or Solo Parent Leave) of one employee in one month.</summary>
    public sealed class CtoMonth
    {
        /// <summary>This is the CTO left from before the month, that can still be used in it.</summary>
        public decimal Start;

        /// <summary>This is the CTO given in the month.</summary>
        public decimal Given;

        /// <summary>This is the CTO used in the month (and covered by CTO that was given).</summary>
        public decimal Used;

        /// <summary>This is the CTO that expired in the month without being used.</summary>
        public decimal Expired;

        /// <summary>This is the CTO left after the month.</summary>
        public decimal End;

        /// <summary>This is the CTO filed in the month for which there was no CTO left.</summary>
        public decimal NotCovered;

        /// <summary>This is what is left after the month: days, and the date they expire, the soonest first.</summary>
        public List<KeyValuePair<decimal, DateTime>> Left = new List<KeyValuePair<decimal, DateTime>>();

        /// <summary>This is true when the employee has or had any CTO, or filed some.</summary>
        public bool HasAny;
    }

    /// <summary>
    /// This is where CTO is worked out. CTO is given in days (at most LeaveKinds.CtoMaxDays at a time) and can be used
    /// until its expiry date. Using CTO takes it from what was given, the one that expires first first.
    /// CTO not used by its expiry date is lost: it is taken off the balance by itself, in the month it expires.
    /// Solo Parent Leave is worked out the same way, with its own days given.
    /// </summary>
    public static class CtoCalculator
    {
        private sealed class Lot
        {
            public DateTime Given, Expires;
            public decimal Left;
        }

        /// <summary>
        /// This is where all CTO given and used up to the end of that date is played. Returns the lots with what is
        /// left in each, and how much of the CTO used could not be covered.
        /// </summary>
        private static List<Lot> PlayUntil(List<CtoGrant> grants, List<KeyValuePair<DateTime, LeaveEntry>> uses,
                                           DateTime until, DateTime countFrom, out decimal usedFrom, out decimal notCoveredFrom)
        {
            var lots = grants.Select(g => new Lot { Given = g.Given.Date, Expires = g.Expires.Date, Left = g.Days }).ToList();
            usedFrom = 0m;
            notCoveredFrom = 0m;

            foreach (var use in uses)
            {
                DateTime day = use.Key.Date;
                if (day > until) break;

                decimal wanted = use.Value.Days;
                foreach (Lot lot in lots.Where(l => l.Given <= day && l.Expires >= day && l.Left > 0).OrderBy(l => l.Expires))
                {
                    decimal take = Math.Min(wanted, lot.Left);
                    lot.Left -= take;
                    wanted -= take;
                    if (day >= countFrom) usedFrom += take;
                    if (wanted == 0) break;
                }
                if (day >= countFrom) notCoveredFrom += wanted;
            }
            return lots;
        }

        /// <summary>
        /// This is what is left of each time it was given (same order as grants), after every day filed so far -
        /// in any month. 0 = used up. Shown in the "CTO given" / "Solo Parent given" window.
        /// </summary>
        public static decimal[] LeftOfEach(List<CtoGrant> grants, List<KeyValuePair<DateTime, LeaveEntry>> uses)
        {
            decimal used, notCovered;
            return PlayUntil(grants, uses, DateTime.MaxValue, DateTime.MinValue, out used, out notCovered)
                .Select(l => l.Left).ToArray();
        }

        /// <summary>This is how much of the days filed could not be taken from what was given (all months).</summary>
        public static decimal NotCoveredAtAll(List<CtoGrant> grants, List<KeyValuePair<DateTime, LeaveEntry>> uses)
        {
            decimal used, notCovered;
            PlayUntil(grants, uses, DateTime.MaxValue, DateTime.MinValue, out used, out notCovered);
            return notCovered;
        }

        /// <summary>
        /// This is the check made before CTO or Solo Parent Leave is filed on a day: true when there is enough
        /// given for it, without leaving another day that is already filed uncovered. Other kinds of leave are
        /// always fine.
        /// </summary>
        public static bool CanFile(CtoStore given, LeaveStore leaves, string employeeId, DateTime date, LeaveEntry entry)
        {
            if (!LeaveKinds.IsGiven(entry.Type))
                return true;
            List<CtoGrant> grants = given.Get(employeeId, entry.Type);
            List<KeyValuePair<DateTime, LeaveEntry>> uses = leaves.GetAllOfKind(employeeId, entry.Type);
            decimal before = NotCoveredAtAll(grants, uses);

            uses = uses.Where(u => u.Key.Date != date.Date).ToList();
            uses.Add(new KeyValuePair<DateTime, LeaveEntry>(date.Date, entry));
            uses.Sort((a, b) => a.Key.CompareTo(b.Key));
            return NotCoveredAtAll(grants, uses) <= before;
        }

        /// <summary>This is the CTO (or Solo Parent Leave) left on that day, not counting what is filed on the day itself.</summary>
        public static decimal LeftOn(CtoStore given, LeaveStore leaves, string employeeId, DateTime date, LeaveType type)
        {
            List<KeyValuePair<DateTime, LeaveEntry>> uses = leaves.GetAllOfKind(employeeId, type)
                .Where(u => u.Key.Date != date.Date).ToList();
            return BalanceOn(given.Get(employeeId, type), uses, date);
        }

        /// <summary>This is the CTO the employee can still use on that day (after the CTO used up to that day).</summary>
        public static decimal BalanceOn(List<CtoGrant> grants, List<KeyValuePair<DateTime, LeaveEntry>> uses, DateTime day)
        {
            decimal used, notCovered;
            return PlayUntil(grants, uses, day.Date, day.Date, out used, out notCovered)
                .Where(l => l.Given <= day.Date && l.Expires >= day.Date).Sum(l => l.Left);
        }

        public static CtoMonth Compute(List<CtoGrant> grants, List<KeyValuePair<DateTime, LeaveEntry>> uses, DateTime monthStart)
        {
            var result = new CtoMonth();
            DateTime first = new DateTime(monthStart.Year, monthStart.Month, 1);
            DateTime last = first.AddMonths(1).AddDays(-1);
            decimal ignore1, ignore2;

            // This is what could be used at the start of the month
            List<Lot> before = PlayUntil(grants, uses, first.AddDays(-1), first, out ignore1, out ignore2);
            result.Start = before.Where(l => l.Given < first && l.Expires >= first).Sum(l => l.Left);

            result.Given = grants.Where(g => g.Given.Date >= first && g.Given.Date <= last).Sum(g => g.Days);

            decimal used, notCovered;
            List<Lot> after = PlayUntil(grants, uses, last, first, out used, out notCovered);
            result.Used = used;
            result.NotCovered = notCovered;

            List<Lot> stillGood = after.Where(l => l.Given <= last && l.Expires > last && l.Left > 0).OrderBy(l => l.Expires).ToList();
            result.End = stillGood.Sum(l => l.Left);
            result.Expired = result.Start + result.Given - result.Used - result.End;
            if (result.Expired < 0) result.Expired = 0;   // This is something that cannot happen, but never show a negative number

            foreach (Lot lot in stillGood)
                result.Left.Add(new KeyValuePair<decimal, DateTime>(lot.Left, lot.Expires));

            result.HasAny = grants.Count > 0 || uses.Count > 0;
            return result;
        }
    }

    /// <summary>
    /// This is where the leave card is computed.
    ///
    /// HOW ONE MONTH IS COMPUTED
    ///   1. Every month the employee EARNS 1.25 Vacation Leave and 1.25 Sick Leave credits.
    ///   2. Vacation Leave and Mandatory / Forced Leave are deducted from the Vacation Leave credits.
    ///      One whole day costs (working hours of the day / 8): 1.25 for a 10-hour day, 1 for an 8-hour day.
    ///      Half a day costs half of that.
    ///   3. Sick Leave is deducted from the Sick Leave credits. If there are not enough,
    ///      the rest is deducted from the Vacation Leave credits.
    ///   4. LATE and UNDERTIME of the month are converted to a fraction of a day
    ///      (see LeaveRules.MinutesToDays) and deducted from the Vacation Leave credits.
    ///   5. If the Vacation Leave credits are not enough, the balance goes BELOW ZERO (with its minus sign, e.g. -1.750)
    ///      and is carried to the next month, where the credits earned bring it back up.
    ///      When "Balance below zero: make it 0" is ticked for the month in Edit Leave, the balance stops at 0 instead and
    ///      the rest is WITHOUT PAY (LWOP) - a balance below zero brought forward too. The computation then shows the
    ///      balance it would have come to ("Not enough credits: the balance would be -1.812") and the days without pay.
    ///   6. Special Privilege Leave (3 days a year) and Wellness Leave (5 days a year) are not deducted,
    ///      only counted. CTO and Solo Parent Leave are taken from the days the employee was given
    ///      (see CtoCalculator); days not used by their expiry date are taken off the balance.
    ///   7. Balance at the end (Net) = balance brought forward + earned - enjoyed.
    ///      The balance is carried to the next month automatically.
    ///   8. An employee with days without pay earns less than 1.25 that month, by the CSC table
    ///      (LeaveRules.EarnedWithoutPay). Credits earned that a person typed are used as typed.
    /// </summary>
    public static class LeaveCalculator
    {
        /// <summary>This is how far back the balance is followed, at most (600 months = 50 years).</summary>
        private const int MostMonthsBack = 600;

        /// <param name="sheet">This is the finished DTR of the open month (gives the late and undertime totals).</param>
        /// <param name="schedule">This is the work schedule of the employee.</param>
        /// <param name="dataFrom">This is the date of the first scan in the opened .dat file.</param>
        /// <param name="dataThrough">This is the date of the last scan in the opened .dat file.</param>
        /// <param name="cto">This is the CTO (and Solo Parent Leave) given to the employees.</param>
        public static LeaveCard Build(DtrSheet sheet, WorkSchedule schedule, LeaveStore leaves, LeaveLedger ledger,
                                      CtoStore cto, DateTime dataFrom, DateTime dataThrough)
        {
            var card = new LeaveCard();
            card.EmployeeId = sheet.EmployeeId;
            card.EmployeeName = sheet.EmployeeName;
            card.MonthStart = new DateTime(sheet.Month.Year, sheet.Month.Month, 1);
            card.ScheduleText = schedule.Description;

            DateTime target = card.MonthStart;
            DateTime monthEnd = target.AddMonths(1).AddDays(-1);
            string id = sheet.EmployeeId;

            // This is where the month to start from is found. The balance is followed back through the unbroken row
            // of remembered months, as far as it goes. It is not followed across a month the program
            // has never seen, and not further than a month in an earlier year where both balances
            // were typed. (The months of the open year are always gone through, to show them on the card.)
            DateTime first = target;
            for (int i = 0; i < MostMonthsBack; i++)
            {
                LedgerMonth? here = ledger.Get(id, first.Year, first.Month);
                bool bothTyped = here != null && here.OpeningVl.HasValue && here.OpeningSl.HasValue;
                if (bothTyped && first.Year < target.Year)
                    break;

                DateTime before = first.AddMonths(-1);
                LedgerMonth? earlier = ledger.Get(id, before.Year, before.Month);
                bool isRemembered = earlier != null && earlier.IsRemembered;
                if (!isRemembered && before.Year < target.Year)
                    break;
                first = before;
            }

            // This is where those months are gone through one after the other, carrying the balance along.
            // A month the program has never seen breaks the chain: the balance is then not known.
            decimal? vl = null, sl = null;
            for (DateTime m = first; m <= target; m = m.AddMonths(1))
            {
                bool isCurrent = m == target;
                LedgerMonth? remembered = ledger.Get(id, m.Year, m.Month);

                if (!isCurrent && (remembered == null || !remembered.IsRemembered))
                {
                    vl = null;
                    sl = null;
                    continue;
                }

                var month = new LeaveMonth();
                month.Year = m.Year;
                month.Month = m.Month;
                month.IsCurrent = isCurrent;

                if (isCurrent)
                {
                    month.LateMinutes = sheet.TotalLateMinutes;
                    month.UndertimeMinutes = sheet.TotalUndertimeMinutes;
                    month.DayCharge = LeaveRules.DayCharge(schedule);
                    month.DataThrough = dataThrough < monthEnd ? dataThrough.Date : monthEnd;
                }
                else
                {
                    month.LateMinutes = remembered!.LateMinutes ?? 0;
                    month.UndertimeMinutes = remembered.UndertimeMinutes ?? 0;
                    month.DayCharge = remembered.DayCharge ?? 1m;
                    month.DataThrough = remembered.DataThrough;
                }

                if (remembered != null)
                {
                    if (remembered.EarnedVl.HasValue) { month.VlEarned = remembered.EarnedVl.Value; month.VlEarnedTyped = true; }
                    if (remembered.EarnedSl.HasValue) { month.SlEarned = remembered.EarnedSl.Value; month.SlEarnedTyped = true; }

                    // This is the rule: a typed balance always wins over the carried one.
                    if (remembered.OpeningVl.HasValue) { vl = remembered.OpeningVl; month.VlStartTyped = true; }
                    if (remembered.OpeningSl.HasValue) { sl = remembered.OpeningSl; month.SlStartTyped = true; }
                    month.ZeroBalance = remembered.ZeroBalance;
                }

                month.VlStart = vl;
                month.SlStart = sl;
                ComputeMonth(month, leaves.GetMonth(id, m.Year, m.Month));

                vl = month.VlNet;
                sl = month.SlNet;

                if (m.Year == target.Year)
                    card.Months[m.Month - 1] = month;
            }

            // This is the days used so far this year (January up to the open month).
            for (int m = 1; m <= target.Month; m++)
            {
                foreach (var pair in leaves.GetMonth(id, target.Year, m))
                {
                    decimal days = pair.Value.Days;
                    switch (pair.Value.Type)
                    {
                        case LeaveType.SpecialPrivilege: card.SplUsed += days; break;
                        case LeaveType.Forced: card.ForcedUsed += days; break;
                        case LeaveType.Wellness: card.WellnessUsed += days; break;
                        case LeaveType.Cto: card.CtoUsed += days; break;
                        case LeaveType.SoloParent: card.SoloUsed += days; break;
                    }
                }
            }

            // This is the SPL, Wellness and Forced Leave left that was typed in Edit Leave (see ApplyTypedYearly).
            card.SplUsed = ApplyTypedYearly(card, ledger, leaves, id, target, LeaveType.SpecialPrivilege, card.SplUsed);
            card.WellnessUsed = ApplyTypedYearly(card, ledger, leaves, id, target, LeaveType.Wellness, card.WellnessUsed);
            card.ForcedUsed = ApplyTypedYearly(card, ledger, leaves, id, target, LeaveType.Forced, card.ForcedUsed);

            card.Cto = CtoCalculator.Compute(cto.Get(id, LeaveType.Cto), leaves.GetAllOfKind(id, LeaveType.Cto), target);
            card.Solo = CtoCalculator.Compute(cto.Get(id, LeaveType.SoloParent), leaves.GetAllOfKind(id, LeaveType.SoloParent), target);

            // This is the History of Availment: every month of the year, January to December, from the leaves filed.
            for (int m = 1; m <= 12; m++)
            {
                var history = new LeaveMonth { Year = target.Year, Month = m, IsCurrent = m == target.Month };
                CountLeaves(history, leaves.GetMonth(id, target.Year, m));
                card.History[m - 1] = history;
            }

            WriteBreakdown(card);
            WriteNotes(card, sheet, schedule, ledger, dataFrom);
            return card;
        }

        /// <summary>
        /// This is the SPL, Wellness or Forced Leave used this year (January up to the open month) when a balance was typed in Edit
        /// Leave: the latest month of the year (up to the open month) with a typed balance wins. The days used before it are
        /// what the typed balance says (3 SPL a year, 1 typed as left = 2 used before), and the leaves filed from that month
        /// on are added. With nothing typed this year, the days filed are used as they are.
        /// </summary>
        private static decimal ApplyTypedYearly(LeaveCard card, LeaveLedger ledger, LeaveStore leaves, string id, DateTime target,
                                                LeaveType type, decimal filedUsed)
        {
            for (int m = target.Month; m >= 1; m--)
            {
                LedgerMonth? typed = ledger.Get(id, target.Year, m);
                decimal? left = typed == null ? null : TypedLeft(typed, type);
                if (!left.HasValue)
                    continue;

                decimal used = LeaveKinds.Get(type).YearlyLimit - left.Value;
                for (int k = m; k <= target.Month; k++)
                    foreach (var pair in leaves.GetMonth(id, target.Year, k))
                        if (pair.Value.Type == type) used += pair.Value.Days;

                if (m == target.Month)
                {
                    if (type == LeaveType.SpecialPrivilege) card.SplStartTyped = left;
                    else if (type == LeaveType.Wellness) card.WellnessStartTyped = left;
                    else card.ForcedStartTyped = left;
                }
                return used;
            }
            return filedUsed;
        }

        /// <summary>This is the balance LEFT typed in Edit Leave for a leave that is allowed so many days a year (null = not typed).</summary>
        private static decimal? TypedLeft(LedgerMonth month, LeaveType type)
        {
            switch (type)
            {
                case LeaveType.SpecialPrivilege: return month.OpeningSpl;
                case LeaveType.Wellness: return month.OpeningWl;
                case LeaveType.Forced: return month.OpeningFl;
                default: return null;
            }
        }

        /// <summary>
        /// This is where one month is filled in from its leaves, its late / undertime minutes and its starting balance.
        /// (VlStart, SlStart, VlEarned, SlEarned, LateMinutes, UndertimeMinutes and DayCharge must be set.)
        /// </summary>
        public static void ComputeMonth(LeaveMonth month, List<KeyValuePair<DateTime, LeaveEntry>> entries)
        {
            CountLeaves(month, entries);

            // This is Rule 2 and 3: what is wanted from each kind of credit.
            month.VlLeave = Round((month.VacationDays + month.ForcedDays) * month.DayCharge);
            month.SlLeave = Round(month.SickDays * month.DayCharge);

            // This is Rule 4: late and undertime as a fraction of a day.
            month.LateDays = LeaveRules.MinutesToDays(month.LateMinutes);
            month.UndertimeDays = LeaveRules.MinutesToDays(month.UndertimeMinutes);

            // This is Rule 8: an employee with days without pay earns less (CSC table). The days without pay
            // depend on what was earned, so this is worked out again until it no longer changes.
            decimal earned = LeaveRules.MonthlyCredit;
            for (int round = 0; round < 10; round++)
            {
                if (!month.VlEarnedTyped) month.VlEarned = earned;
                if (!month.SlEarnedTyped) month.SlEarned = earned;
                Settle(month);

                decimal withoutPay = Math.Floor(month.Lwop * 2m) / 2m;
                decimal next = LeaveRules.EarnedWithoutPay(withoutPay);
                month.WithoutPayDays = withoutPay;
                if (next == earned || (month.VlEarnedTyped && month.SlEarnedTyped))
                    break;
                earned = next;
            }
        }

        /// <summary>This is where the days of each kind of leave in the month are counted and their dates written ("3, 8 (1/2)").</summary>
        public static void CountLeaves(LeaveMonth month, List<KeyValuePair<DateTime, LeaveEntry>> entries)
        {
            var dates = new Dictionary<LeaveType, List<KeyValuePair<int, bool>>>();   // This is day, whole day?

            foreach (var pair in entries)
            {
                LeaveEntry entry = pair.Value;
                decimal days = entry.Days;

                switch (entry.Type)
                {
                    case LeaveType.Vacation: month.VacationDays += days; break;
                    case LeaveType.Forced: month.ForcedDays += days; break;
                    case LeaveType.Sick: month.SickDays += days; break;
                    case LeaveType.SpecialPrivilege: month.SplDays += days; break;
                    case LeaveType.Wellness: month.WellnessDays += days; break;
                    case LeaveType.Cto: month.CtoDays += days; break;
                    case LeaveType.SoloParent: month.SoloDays += days; break;
                    default: continue;
                }

                List<KeyValuePair<int, bool>>? list;
                if (!dates.TryGetValue(entry.Type, out list))
                {
                    list = new List<KeyValuePair<int, bool>>();
                    dates[entry.Type] = list;
                }
                list.Add(new KeyValuePair<int, bool>(pair.Key.Day, entry.Part == LeavePart.WholeDay));
            }

            month.VacationDates = Join(dates, LeaveType.Vacation);
            month.ForcedDates = Join(dates, LeaveType.Forced);
            month.SickDates = Join(dates, LeaveType.Sick);
            month.SplDates = Join(dates, LeaveType.SpecialPrivilege);
            month.WellnessDates = Join(dates, LeaveType.Wellness);
            month.CtoDates = Join(dates, LeaveType.Cto);
            month.SoloDates = Join(dates, LeaveType.SoloParent);
        }

        /// <summary>This is Rules 3 to 5 and 7: what is taken from each kind of credit, and the balances.</summary>
        private static void Settle(LeaveMonth month)
        {
            // This is the SICK LEAVE part ----
            if (month.SlStart.HasValue)
            {
                decimal available = month.SlStart.Value + month.SlEarned;
                month.SlEnjoyed = Math.Min(month.SlLeave, Math.Max(0m, available));
                month.SlToVl = month.SlLeave - month.SlEnjoyed;     // This is rule 3: the rest goes to Vacation Leave
                month.SlNet = available - month.SlEnjoyed;
            }
            else
            {
                // This is when the balance is not known: we cannot tell whether the credits are enough.
                month.SlEnjoyed = month.SlLeave;
                month.SlToVl = 0m;
                month.SlNet = null;
            }

            // This is the VACATION LEAVE part ----
            decimal wanted = month.VlLeave + month.SlToVl + month.LateDays + month.UndertimeDays;
            if (month.VlStart.HasValue)
            {
                decimal available = month.VlStart.Value + month.VlEarned;
                if (month.ZeroBalance)
                {
                    // This is rule 5 with "make it 0": the credits stop at 0; what they do not cover is without pay,
                    // and so is a balance below zero brought forward from the month before.
                    month.VlEnjoyed = Math.Min(wanted, Math.Max(0m, available));
                    month.Lwop = wanted - month.VlEnjoyed + Math.Max(0m, -available);
                    month.VlNet = Math.Max(0m, available - month.VlEnjoyed);
                }
                else
                {
                    // This is rule 5, the usual case: everything is taken from the credits, even below zero.
                    month.VlEnjoyed = wanted;
                    month.Lwop = 0m;
                    month.VlNet = available - wanted;
                }
            }
            else
            {
                month.VlEnjoyed = wanted;
                month.Lwop = 0m;
                month.VlNet = null;
            }
        }

        /// <summary>
        /// This is where the days of one kind of leave are written the short way: "3, 8 (1/2), 21-25".
        /// Three or more whole days in a row are written as a range.
        /// </summary>
        private static string Join(Dictionary<LeaveType, List<KeyValuePair<int, bool>>> dates, LeaveType type)
        {
            List<KeyValuePair<int, bool>>? list;
            if (!dates.TryGetValue(type, out list))
                return "";

            var parts = new List<string>();
            int i = 0;
            while (i < list.Count)
            {
                if (!list[i].Value)
                {
                    parts.Add(list[i].Key + " (1/2)");
                    i++;
                    continue;
                }

                // This is how far the row of whole days goes
                int last = i;
                while (last + 1 < list.Count && list[last + 1].Value && list[last + 1].Key == list[last].Key + 1)
                    last++;

                if (last - i >= 2)
                {
                    parts.Add(list[i].Key + "-" + list[last].Key);
                }
                else
                {
                    for (int k = i; k <= last; k++)
                        parts.Add(list[k].Key.ToString(CultureInfo.InvariantCulture));
                }
                i = last + 1;
            }
            return string.Join(", ", parts.ToArray());
        }

        private static decimal Round(decimal value)
        {
            return Math.Round(value, 3, MidpointRounding.AwayFromZero);
        }

        // This is the written-out computation ---------------------------------------------------------

        private static void WriteBreakdown(LeaveCard card)
        {
            LeaveMonth m = card.Current;
            List<LeaveLine> lines = card.Breakdown;
            string charge = LeaveRules.FormatShort(m.DayCharge);
            const string notKnown = "not known";

            // This is the Vacation Leave part ----
            lines.Add(new LeaveLine(LeaveLineKind.Heading, "VACATION LEAVE", ""));
            lines.Add(new LeaveLine(LeaveLineKind.Item, "Balance brought forward from " + card.PreviousMonthText,
                m.VlStart.HasValue ? LeaveRules.Format(m.VlStart.Value) : notKnown));
            lines.Add(new LeaveLine(LeaveLineKind.Item, EarnedText(m, m.VlEarnedTyped), "+ " + LeaveRules.Format(m.VlEarned)));

            if (m.VacationDays > 0)
                lines.Add(new LeaveLine(LeaveLineKind.Item,
                    "Vacation Leave on " + m.VacationDates + "   (" + LeaveRules.FormatDays(m.VacationDays) + " x " + charge + ")",
                    "- " + LeaveRules.Format(Round(m.VacationDays * m.DayCharge))));
            if (m.ForcedDays > 0)
            {
                lines.Add(new LeaveLine(LeaveLineKind.Item,
                    "Forced Leave on " + m.ForcedDates + "   (" + LeaveRules.FormatDays(m.ForcedDays) + " x " + charge + ")",
                    "- " + LeaveRules.Format(Round(m.ForcedDays * m.DayCharge))));
                lines.Add(new LeaveLine(LeaveLineKind.Item,
                    "   Forced Leave used this year: " + UsedText(card.ForcedUsed, LeaveType.Forced).Replace(" this year", " days"), ""));
            }
            if (card.ForcedStartTyped.HasValue)
                lines.Add(new LeaveLine(LeaveLineKind.Item,
                    "   Forced Leave left on " + card.MonthStart.ToString("MMM d", CultureInfo.InvariantCulture) + " (typed): " +
                    LeaveRules.FormatDays(card.ForcedStartTyped.Value), ""));
            if (m.SlToVl > 0)
                lines.Add(new LeaveLine(LeaveLineKind.Item,
                    "Sick Leave charged here, because the Sick Leave credits ran out", "- " + LeaveRules.Format(m.SlToVl)));
            if (m.LateMinutes > 0)
                lines.Add(new LeaveLine(LeaveLineKind.Item,
                    "Late, " + LeaveRules.FormatMinutes(m.LateMinutes), "- " + LeaveRules.Format(m.LateDays)));
            if (m.UndertimeMinutes > 0)
                lines.Add(new LeaveLine(LeaveLineKind.Item,
                    "Undertime, " + LeaveRules.FormatMinutes(m.UndertimeMinutes), "- " + LeaveRules.Format(m.UndertimeDays)));
            if (m.Lwop > 0)
            {
                // This is the balance the deductions come to, before the days without pay: below zero, with its minus sign,
                // so the lines add up. The credits never go below zero (CSC rules): the part not covered is without pay.
                if (m.VlNet.HasValue)
                    lines.Add(new LeaveLine(LeaveLineKind.Item,
                        "Not enough credits: the balance would be", LeaveRules.Format(m.VlNet.Value - m.Lwop)));
                lines.Add(new LeaveLine(LeaveLineKind.Item,
                    "Not covered by the credits: without pay", LeaveRules.Format(m.Lwop)));
            }

            lines.Add(new LeaveLine(LeaveLineKind.Total, "Balance at the end of " + card.MonthText,
                m.VlNet.HasValue ? LeaveRules.Format(m.VlNet.Value) : notKnown));

            // This is the Sick Leave part ----
            lines.Add(new LeaveLine(LeaveLineKind.Heading, "SICK LEAVE", ""));
            lines.Add(new LeaveLine(LeaveLineKind.Item, "Balance brought forward from " + card.PreviousMonthText,
                m.SlStart.HasValue ? LeaveRules.Format(m.SlStart.Value) : notKnown));
            lines.Add(new LeaveLine(LeaveLineKind.Item, EarnedText(m, m.SlEarnedTyped), "+ " + LeaveRules.Format(m.SlEarned)));

            if (m.SickDays > 0)
            {
                lines.Add(new LeaveLine(LeaveLineKind.Item,
                    "Sick Leave on " + m.SickDates + "   (" + LeaveRules.FormatDays(m.SickDays) + " x " + charge + ")",
                    "- " + LeaveRules.Format(m.SlLeave)));
                if (m.SlToVl > 0)
                    lines.Add(new LeaveLine(LeaveLineKind.Item,
                        "Moved to Vacation Leave, because the Sick Leave credits ran out", "+ " + LeaveRules.Format(m.SlToVl)));
            }

            lines.Add(new LeaveLine(LeaveLineKind.Total, "Balance at the end of " + card.MonthText,
                m.SlNet.HasValue ? LeaveRules.Format(m.SlNet.Value) : notKnown));

            // This is Special Privilege Leave and Wellness Leave: so many days a year ----
            AddYearlyLeave(lines, card, LeaveType.SpecialPrivilege, "SPECIAL PRIVILEGE LEAVE (SPL)", card.SplUsed, m.SplDays, m.SplDates, card.SplStartTyped);
            AddYearlyLeave(lines, card, LeaveType.Wellness, "WELLNESS LEAVE (WL)", card.WellnessUsed, m.WellnessDays, m.WellnessDates, card.WellnessStartTyped);

            // This is CTO and Solo Parent Leave: given, used, expired ----
            AddGivenLeave(lines, card, card.Cto, "CTO",
                "CTO (COMPENSATORY TIME-OFF, AT MOST " + LeaveRules.FormatShort(LeaveKinds.CtoMaxDays) + " DAYS)",
                m.CtoDays, m.CtoDates);
            AddGivenLeave(lines, card, card.Solo, "Solo Parent Leave",
                "SOLO PARENT LEAVE (SOLO, AT MOST " + LeaveRules.FormatShort(LeaveKinds.SoloMaxDays) + " DAYS)",
                m.SoloDays, m.SoloDates);
        }

        /// <summary>This is CTO or Solo Parent Leave: the balance, given, used, expired, and what is left.</summary>
        private static void AddGivenLeave(List<LeaveLine> lines, LeaveCard card, CtoMonth c, string name, string heading,
                                          decimal usedDays, string usedDates)
        {
            lines.Add(new LeaveLine(LeaveLineKind.Heading, heading, ""));
            lines.Add(new LeaveLine(LeaveLineKind.Item, "Balance brought forward from " + card.PreviousMonthText, Days(c.Start)));
            if (c.Given > 0)
                lines.Add(new LeaveLine(LeaveLineKind.Item, name + " given this month", "+ " + Days(c.Given)));
            if (usedDays > 0)
                lines.Add(new LeaveLine(LeaveLineKind.Item,
                    name + " used on " + usedDates + "   (" + LeaveRules.FormatDays(usedDays) + ")", "- " + Days(c.Used)));
            if (c.NotCovered > 0)
                lines.Add(new LeaveLine(LeaveLineKind.Item, name + " used but there was none left", Days(c.NotCovered)));
            if (c.Expired > 0)
                lines.Add(new LeaveLine(LeaveLineKind.Item, "Expired, not used in time", "- " + Days(c.Expired)));
            lines.Add(new LeaveLine(LeaveLineKind.Total, "Balance at the end of " + card.MonthText, Days(c.End)));
            foreach (var left in c.Left)
                lines.Add(new LeaveLine(LeaveLineKind.Item,
                    "   " + LeaveRules.FormatDays(left.Key) + " of it expire" + (left.Key == 1m || left.Key == 0.5m ? "s" : "") +
                    " on " + left.Value.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture), ""));
        }

        /// <summary>This is "Earned this month", with where the number comes from when it is not the usual 1.25.</summary>
        private static string EarnedText(LeaveMonth m, bool typed)
        {
            if (typed) return "Earned this month (typed)";
            if (m.WithoutPayDays > 0)
                return "Earned this month (CSC table: " + LeaveRules.FormatDays(m.WithoutPayDays) + " without pay)";
            return "Earned this month";
        }

        /// <summary>This is a leave that is allowed so many days a year: used before, used now, and what is left.</summary>
        /// <param name="typedStart">This is the balance left at the start of the month typed in Edit Leave, or null.</param>
        private static void AddYearlyLeave(List<LeaveLine> lines, LeaveCard card, LeaveType type, string heading,
                                           decimal usedThisYear, decimal thisMonth, string dates, decimal? typedStart)
        {
            int limit = LeaveKinds.Get(type).YearlyLimit;
            decimal before = usedThisYear - thisMonth;
            decimal left = limit - usedThisYear;

            lines.Add(new LeaveLine(LeaveLineKind.Heading, heading + ",  " + limit + " DAYS A YEAR", ""));
            if (typedStart.HasValue)
                lines.Add(new LeaveLine(LeaveLineKind.Item, "Left at the start of " + card.Current.MonthName + " (typed)", Days(typedStart.Value)));
            else
                lines.Add(new LeaveLine(LeaveLineKind.Item, "Used this year before " + card.Current.MonthName, Days(before)));
            lines.Add(new LeaveLine(LeaveLineKind.Item,
                thisMonth > 0 ? "Used this month on " + dates : "Used this month",
                thisMonth > 0 ? "- " + Days(thisMonth) : Days(0m)));
            lines.Add(new LeaveLine(LeaveLineKind.Total, "Left for the rest of " + card.Year,
                left >= 0 ? Days(left) : "0  (" + LeaveRules.FormatShort(-left) + " over)"));
        }

        /// <summary>This is "2", "0.5", "0" - days, written without useless zeros.</summary>
        private static string Days(decimal days)
        {
            return LeaveRules.FormatShort(days);
        }

        /// <summary>This is "2 of 5 this year", or "2 this year" when the kind has no yearly limit.</summary>
        public static string UsedText(decimal used, LeaveType type)
        {
            int limit = LeaveKinds.Get(type).YearlyLimit;
            return LeaveRules.FormatShort(used) + (limit > 0 ? " of " + limit : "") + " this year";
        }

        private static void WriteNotes(LeaveCard card, DtrSheet sheet, WorkSchedule schedule,
                                       LeaveLedger ledger, DateTime dataFrom)
        {
            LeaveMonth m = card.Current;
            List<string> notes = card.Notes;
            string monthName = m.MonthName;

            // This is when the opened file does not hold the whole month ----
            if (LeaveLedger.StartsLate(m.Year, m.Month, dataFrom))
            {
                notes.Add("The opened file starts on " + dataFrom.ToString("MMMM d", CultureInfo.InvariantCulture) +
                          ", so the first part of " + monthName + " is missing from it. This computation is not complete, " +
                          "and it is not kept for next month. Open the file that has all of " + monthName + ".");
            }
            else if (m.DataThrough.HasValue && ledger.HasMoreComplete(card.EmployeeId, m.Year, m.Month, m.DataThrough.Value))
            {
                LedgerMonth kept = ledger.Get(card.EmployeeId, m.Year, m.Month)!;
                notes.Add("The opened file ends on " + m.DataThrough.Value.ToString("MMMM d", CultureInfo.InvariantCulture) +
                          ", but " + monthName + " was already computed from a file that went up to " +
                          kept.DataThrough!.Value.ToString("MMMM d", CultureInfo.InvariantCulture) +
                          ". That earlier result is kept for next month. What you see here is only part of the month.");
            }
            else if (m.MayBeIncomplete)
            {
                notes.Add("The opened file ends on " + m.DataThrough!.Value.ToString("MMMM d", CultureInfo.InvariantCulture) +
                          ". If " + monthName + " is not over yet, this computation is not final.");
            }

            if (!m.VlStart.HasValue || !m.SlStart.HasValue)
                notes.Add("The leave balance is not known yet. In \"Edit Leave...\", type the balance brought forward " +
                          "from " + card.PreviousMonthText + " (copy it from the employee's leave card). You only do this once.");

            if (m.Lwop > 0)
                notes.Add("The Vacation Leave credits were not enough. " + LeaveRules.Format(m.Lwop) +
                          " day(s) are WITHOUT PAY (LWOP), because \"Balance below zero: make it 0\" is ticked. Check the credits " +
                          "earned this month: an employee with days without pay earns less than 1.25.");
            else if (m.VlNet.HasValue && m.VlNet.Value < 0)
                notes.Add("The Vacation Leave balance is below zero: " + LeaveRules.Format(m.VlNet.Value) + ". It is carried to next month. " +
                          "To make it 0 instead (the rest without pay), tick \"Balance below zero: make it 0\".");

            if (m.SlToVl > 0)
                notes.Add("The Sick Leave credits were not enough. " + LeaveRules.Format(m.SlToVl) +
                          " day(s) of sick leave were charged to Vacation Leave.");

            if (card.Cto.NotCovered > 0)
                notes.Add("CTO was filed for " + LeaveRules.FormatDays(card.Cto.NotCovered) +
                          " but there was no CTO left for it. Check the CTO given (\"CTO given...\" in Edit Leave).");
            if (card.Cto.Expired > 0)
                notes.Add(LeaveRules.FormatDays(card.Cto.Expired) + " of CTO expired this month without being used. " +
                          "It was taken off the CTO balance.");
            if (card.Solo.NotCovered > 0)
                notes.Add("Solo Parent Leave was filed for " + LeaveRules.FormatDays(card.Solo.NotCovered) +
                          " but there was none left for it. Check the Solo Parent Leave given (\"Solo Parent given...\" in Edit Leave).");
            if (card.Solo.Expired > 0)
                notes.Add(LeaveRules.FormatDays(card.Solo.Expired) + " of Solo Parent Leave expired this month without being used. " +
                          "It was taken off the SOLO balance.");

            AddLimitNote(notes, card.ForcedUsed, LeaveType.Forced);
            AddLimitNote(notes, card.SplUsed, LeaveType.SpecialPrivilege);
            AddLimitNote(notes, card.WellnessUsed, LeaveType.Wellness);

            // This is absent on a working day with no leave filed: nothing is deducted, a person decides.
            List<string> absent = sheet.Days
                .Where(d => d.IsAbsent && schedule.IsWorkDay(d.Date.DayOfWeek))
                .Select(d => d.Day.ToString(CultureInfo.InvariantCulture)).ToList();
            if (absent.Count > 0)
                notes.Add("ABSENT with no leave filed on " + (absent.Count == 1 ? "day " : "days ") + string.Join(", ", absent.ToArray()) +
                          ". Nothing was deducted for " + (absent.Count == 1 ? "this day" : "these days") +
                          ". File a leave on that day if there is one.");

            // This is a leave that does not match what the DTR shows (times on a day of leave,
            // a half-day leave with nothing in the other half...). The DTR warning says what.
            foreach (DtrDay day in sheet.Days)
            {
                if (day.HasLeave && day.Warning.Length > 0)
                    notes.Add("Day " + day.Day + ": " + day.Warning + ". Check the leave and the times of this day.");
            }

            // This is a leave on a day that is not a working day: it is still deducted, so it is pointed out.
            List<string> restDays = sheet.Days
                .Where(d => d.HasLeave && !LeaveKinds.IsNotCounted(d.Leave) && !schedule.IsWorkDay(d.Date.DayOfWeek))
                .Select(d => d.Day.ToString(CultureInfo.InvariantCulture)).ToList();
            if (restDays.Count > 0)
                notes.Add("A leave is filed on " + (restDays.Count == 1 ? "day " : "days ") + string.Join(", ", restDays.ToArray()) +
                          ", which is not a working day in this schedule. It is still counted. " +
                          "Remove it with \"Edit Leave...\" if it is a mistake.");

            // This is worked from home, but the IDLAR is not ticked yet: the day is not counted as present.
            List<string> noIdlar = sheet.Days
                .Where(d => d.Warning.Contains("IDLAR"))
                .Select(d => d.Day.ToString(CultureInfo.InvariantCulture)).ToList();
            if (noIdlar.Count > 0)
                notes.Add("Work from home without the IDLAR on " + (noIdlar.Count == 1 ? "day " : "days ") +
                          string.Join(", ", noIdlar.ToArray()) + ". Tick IDLAR when it is handed in, " +
                          "so the day counts as a day at work.");

            // This is an earlier month that was computed from a file that stopped before the end of the month.
            foreach (LeaveMonth? earlier in card.Months)
            {
                if (earlier != null && !earlier.IsCurrent && earlier.MayBeIncomplete)
                    notes.Add(earlier.MonthName + " " + earlier.Year + " was computed from a file that ended on " +
                              earlier.DataThrough!.Value.ToString("MMMM d", CultureInfo.InvariantCulture) +
                              ". If scans are missing, open the complete " + earlier.MonthName + " file once to update it.");
            }
        }

        private static void AddLimitNote(List<string> notes, decimal used, LeaveType type)
        {
            LeaveKind kind = LeaveKinds.Get(type);
            if (kind.YearlyLimit > 0 && used > kind.YearlyLimit)
                notes.Add(kind.Name + ": " + LeaveRules.FormatShort(used) + " days used this year, but only " +
                          kind.YearlyLimit + " are allowed.");
        }
    }
}
