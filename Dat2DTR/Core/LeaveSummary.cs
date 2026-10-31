using System.Globalization;

namespace Dat2DTR
{
    /// <summary>This is one row of the leave summary: the balances and the leave used of one employee.</summary>
    public sealed class LeaveSummaryRow
    {
        public string EmployeeId = "";
        public string EmployeeName = "";

        /// <summary>This is the schedule letter, "A" to "D".</summary>
        public string Schedule = "";

        /// <summary>This is the Vacation Leave and Sick Leave balance at the end of the month (null = not known yet).</summary>
        public decimal? Vl, Sl;

        /// <summary>This is the CTO and Solo Parent Leave left at the end of the month, in days.</summary>
        public decimal Cto, Solo;

        /// <summary>This is the Special Privilege, Wellness and Forced Leave used this year, January up to the month.</summary>
        public decimal SplUsed, WellnessUsed, ForcedUsed;

        /// <summary>This is the reminders of this employee (see LeaveReminders), joined with "; ".</summary>
        public string Reminders = "";
    }

    /// <summary>
    /// This is the leave summary of everyone: one row per employee with the VL, SL, CTO and Solo Parent
    /// balance at the end of the month, and the SPL, Wellness and Forced Leave used this year. It comes
    /// from the Computation of Leaves of each employee, so the numbers are the same as on their page.
    /// </summary>
    public static class LeaveSummary
    {
        public static List<LeaveSummaryRow> Make(IList<DtrSheet> sheets, IDictionary<string, LeaveCard> cards,
                                                 CtoStore given, LeaveStore leaves, DateTime today)
        {
            var rows = new List<LeaveSummaryRow>();
            foreach (DtrSheet sheet in sheets)
            {
                LeaveCard? card;
                if (!cards.TryGetValue(sheet.EmployeeId, out card))
                    continue;

                rows.Add(new LeaveSummaryRow
                {
                    EmployeeId = sheet.EmployeeId,
                    EmployeeName = sheet.EmployeeName,
                    Schedule = WorkSchedule.Letters.Substring(Math.Max(0, Math.Min(3, sheet.ScheduleIndex)), 1),
                    Vl = card.Current.VlNet,
                    Sl = card.Current.SlNet,
                    Cto = card.Cto.End,
                    Solo = card.Solo.End,
                    SplUsed = card.SplUsed,
                    WellnessUsed = card.WellnessUsed,
                    ForcedUsed = card.ForcedUsed,
                    Reminders = string.Join("; ", LeaveReminders.For(card, given, leaves, today).ToArray())
                });
            }
            return rows;
        }
    }

    /// <summary>
    /// This is the list of reminders shown in the yellow bar and in the leave summary:
    ///   - Forced Leave: from November, when the 5 days of the year are not all used (unused days are
    ///     forfeited at the end of the year). Only for employees with 10 or more days of Vacation Leave,
    ///     or whose balance is not known yet - the CSC rule only asks it of them.
    ///   - CTO and Solo Parent Leave that expire within the next 30 days and are not used yet.
    /// </summary>
    public static class LeaveReminders
    {
        /// <summary>This is how many days ahead an expiry date is pointed out.</summary>
        public const int ExpiryDaysAhead = 30;

        /// <summary>This is the Vacation Leave balance from which the 5 days of Forced Leave are required.</summary>
        public const decimal ForcedLeaveFrom = 10m;

        public static List<string> For(LeaveCard card, CtoStore given, LeaveStore leaves, DateTime today)
        {
            var result = new List<string>();

            // This is Forced Leave: counted over the whole year (a day already filed for December counts too). The days used up to
            // this month are the leave card's (with a Forced Leave balance typed in Edit Leave), plus those filed for later months.
            int limit = LeaveKinds.Get(LeaveType.Forced).YearlyLimit;
            decimal forced = card.ForcedUsed +
                             card.History.Where(h => h != null && h.Month > card.MonthStart.Month).Sum(h => h.ForcedDays);
            decimal? vl = card.Current.VlNet;
            if (card.MonthStart.Month >= 11 && forced < limit && (!vl.HasValue || vl.Value >= ForcedLeaveFrom))
                result.Add("Forced Leave: " + LeaveRules.FormatShort(limit - forced) + " of " + limit +
                           " days not used yet in " + card.Year + " (unused days are forfeited after December 31)");

            // This is CTO and Solo Parent Leave about to expire, from everything filed so far.
            foreach (LeaveType type in new[] { LeaveType.Cto, LeaveType.SoloParent })
            {
                List<CtoGrant> grants = given.Get(card.EmployeeId, type);
                if (grants.Count == 0)
                    continue;
                decimal[] left = CtoCalculator.LeftOfEach(grants, leaves.GetAllOfKind(card.EmployeeId, type));
                for (int i = 0; i < grants.Count; i++)
                {
                    DateTime expires = grants[i].Expires.Date;
                    if (left[i] > 0m && expires >= today.Date && expires <= today.Date.AddDays(ExpiryDaysAhead))
                        result.Add((type == LeaveType.SoloParent ? "Solo Parent Leave" : "CTO") + ": " +
                                   LeaveRules.FormatDays(left[i]) + " not used yet expire" + (left[i] <= 1m ? "s" : "") +
                                   " on " + expires.ToString("MMM d, yyyy", CultureInfo.InvariantCulture));
                }
            }
            return result;
        }
    }
}
