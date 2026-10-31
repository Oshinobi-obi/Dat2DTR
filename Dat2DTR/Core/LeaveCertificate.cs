using System.Globalization;

namespace Dat2DTR
{
    /// <summary>
    /// This is what goes on the Certificate of Leave Credits of one employee: the Vacation Leave and
    /// Sick Leave balance as of a date, the Forced Leave, Special Privilege Leave and Wellness Leave left for
    /// that year, and the CTO and Solo Parent Leave left.
    ///
    /// The date is the end of the month on screen. When that month is not over yet on the day the
    /// certificate is made, the end of the month before is used instead (with its balance), so the
    /// certificate never shows credits that are not earned yet.
    /// </summary>
    public sealed class LeaveCertificate
    {
        public string EmployeeId = "";
        public string EmployeeName = "";

        /// <summary>This is the date the balances are as of.</summary>
        public DateTime AsOf;

        /// <summary>This is the date the certificate is issued (the day it is printed or saved).</summary>
        public DateTime Issued;

        /// <summary>This is the Vacation Leave and Sick Leave balance (null = not known yet: left blank to fill in by hand).</summary>
        public decimal? Vl, Sl;

        /// <summary>This is who prepares the certificates until it is changed with "Edit Certificate Details..." (see DtrSettings).</summary>
        public const string DefaultPreparedByName = "MARY GRACE E. SACRAMENTO";
        public const string DefaultPreparedByPosition = "Administrative Officer - IV";

        /// <summary>
        /// This is the names and positions printed on the two places to sign: "Prepared by" and "Certified correct"
        /// ("" = left blank, to fill in by hand). Taken from the settings with SignedBy.
        /// </summary>
        public string PreparedName = DefaultPreparedByName, PreparedPosition = DefaultPreparedByPosition;
        public string CertifiedName = "", CertifiedPosition = "";

        /// <summary>This is where the names and positions of the settings ("Edit Certificate Details...") are put on the certificate.</summary>
        public LeaveCertificate SignedBy(DtrSettings settings)
        {
            PreparedName = settings.PreparedByName.Trim();
            PreparedPosition = settings.PreparedByPosition.Trim();
            CertifiedName = settings.CertifiedByName.Trim();
            CertifiedPosition = settings.CertifiedByPosition.Trim();
            return this;
        }

        /// <summary>This is the CTO and Solo Parent Leave left, in days.</summary>
        public decimal Cto, Solo;

        /// <summary>
        /// This is the Mandatory / Forced Leave, Special Privilege Leave and Wellness Leave left for the year of the
        /// "as of" date (the days a year minus the days used up to that date). null = not known (the year before the open one).
        /// </summary>
        public decimal? ForcedLeft, SplLeft, WellnessLeft;

        /// <summary>This is the month the numbers come from, e.g. "October 2026".</summary>
        public string FromMonth = "";

        /// <summary>This is true when both balances are known.</summary>
        public bool IsComplete
        {
            get { return Vl.HasValue && Sl.HasValue; }
        }

        /// <summary>This is VL + SL, or null when one of them is not known.</summary>
        public decimal? Total
        {
            get { return IsComplete ? Vl!.Value + Sl!.Value : (decimal?)null; }
        }

        public static LeaveCertificate Make(LeaveCard card, DateTime issued)
        {
            var c = new LeaveCertificate();
            c.EmployeeId = card.EmployeeId;
            c.EmployeeName = card.EmployeeName;
            c.Issued = issued.Date;
            c.FromMonth = card.MonthText;

            DateTime monthEnd = card.MonthStart.AddMonths(1).AddDays(-1);
            LeaveMonth m = card.Current;
            if (monthEnd <= issued.Date)
            {
                c.AsOf = monthEnd;
                c.Vl = m.VlNet;
                c.Sl = m.SlNet;
                c.Cto = card.Cto.End;
                c.Solo = card.Solo.End;
            }
            else
            {
                // This is a month that is not over yet: the balance brought forward from the month before.
                c.AsOf = card.MonthStart.AddDays(-1);
                c.Vl = m.VlStart;
                c.Sl = m.SlStart;
                c.Cto = card.Cto.Start;
                c.Solo = card.Solo.Start;
            }

            // This is the leaves given every year (they start again on January 1): what is left as of that date.
            if (c.AsOf.Year == card.Year)
            {
                // This is the days used this year as the leave card counts them (with an SPL / WL / Forced Leave balance typed in Edit Leave),
                // without the open month when the certificate is as of the end of the month before.
                bool withOpenMonth = c.AsOf.Month == card.MonthStart.Month;
                LeaveMonth now = card.Current;
                c.ForcedLeft = Left(LeaveType.Forced, card.ForcedUsed - (withOpenMonth ? 0 : now.ForcedDays));
                c.SplLeft = Left(LeaveType.SpecialPrivilege, card.SplUsed - (withOpenMonth ? 0 : now.SplDays));
                c.WellnessLeft = Left(LeaveType.Wellness, card.WellnessUsed - (withOpenMonth ? 0 : now.WellnessDays));
            }
            return c;
        }

        /// <summary>This is the days a year of that leave minus the days used (never below 0).</summary>
        private static decimal Left(LeaveType type, decimal used)
        {
            return Math.Max(0, LeaveKinds.Get(type).YearlyLimit - used);
        }

        /// <summary>This is a date the way it is written on the certificate, e.g. "October 31, 2026".</summary>
        public static string LongDate(DateTime date)
        {
            return date.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture);
        }

        /// <summary>This is "27th day of October 2026", for "Issued this ...".</summary>
        public static string IssuedText(DateTime date)
        {
            int d = date.Day;
            string suffix = (d % 100 >= 11 && d % 100 <= 13) ? "th" : (d % 10 == 1 ? "st" : d % 10 == 2 ? "nd" : d % 10 == 3 ? "rd" : "th");
            return d + suffix + " day of " + date.ToString("MMMM yyyy", CultureInfo.InvariantCulture);
        }
    }
}
