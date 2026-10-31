namespace Dat2DTR
{
    /// <summary>This is one version of the program in the version history: its number, its date and what changed.</summary>
    public sealed class VersionEntry
    {
        public string Number = "";
        public string Date = "";
        public string[] Changes = new string[0];

        public VersionEntry(string number, string date, params string[] changes)
        {
            Number = number;
            Date = date;
            Changes = changes;
        }
    }

    /// <summary>
    /// This is the version of the program, who made it and how to reach them, and the version history (shown in the
    /// About window). The version of the program is ALWAYS the newest entry of the history, so they never differ.
    /// When something changes: add a new entry at the TOP of History, and put the same number in Dat2DTR.csproj
    /// (Version) so Windows shows it on Dat2DTR.exe too.
    /// </summary>
    public static class AppInfo
    {
        public const string Developer = "Mico D. Intertas";
        public const string Role = "Software Developer, BSIT, Quezon City University";
        public const string Email = "intertas.mico.dichoso@gmail.com";
        public const string Phone = "09098874204";

        /// <summary>This is every version, NEWEST FIRST.</summary>
        public static readonly VersionEntry[] History =
        {
            new VersionEntry("1.8.0", "October 31, 2026",
                "Vacation Leave below zero: when the deductions are more than the credits, the balance goes below zero with its minus " +
                "sign (e.g. -1.750) in the leave card table, the VL at the top right, the computation and the certificate, and is " +
                "carried to next month (the credits earned bring it back up).",
                "Edit Leave: \"Balance below zero: make it 0 (the rest is without pay)\", a tick box for the month. Ticked, the balance " +
                "stops at 0 and the rest is without pay, as before."),
            new VersionEntry("1.7.0", "October 31, 2026",
                "Edit DTR: a No Late / Undertime box beside Flexi-Time. A ticked day keeps its times, but no Late or Undertime " +
                "is counted, totalled, deducted or reported (for example a Friday when everyone went back to 7-4 or 8-5).",
                "Edit Leave: a Force/Mandatory box, centred below Special and Wellness, for the Mandatory / Forced Leave left at " +
                "the start of the month. The computation, Leave Summary, certificate and reminder use it.",
                "A Certificate of Leave Credits tab beside DTR and Computation of Leaves: the certificate of the employee on screen, " +
                "as it will be printed, with Edit Certificate Details... beside it.",
                "Computation of Leaves: when the Vacation Leave credits are not enough, the balance it would have come to is shown with " +
                "its minus sign (e.g. -1.812) before the days without pay.",
                "Certificate of Leave Credits: only the name of the employee, without the biometric ID."),
            new VersionEntry("1.6.0", "October 31, 2026",
                "Edit Leave: Special and Wellness boxes below Vacation Leave and Sick Leave, for the Special Privilege Leave and Wellness Leave " +
                "left at the start of the month (days used before the program was used). The computation, Leave Summary and " +
                "certificate use them.",
                "Work suspended from a time in the lunch break (e.g. from 1:00 PM): leaving before it is undertime, counted up to it " +
                "(out at 12:26 PM = 34 minutes).",
                "A clock at the bottom right of the main window: the date and time in the Philippines (UTC+8)."),
            new VersionEntry("1.5.0", "October 29, 2026",
                "The scans of every .dat file opened are kept on this computer. The months of before stay in the list, also after the program is closed.",
                "A year list beside the month list: the month list shows only the months of the year picked.",
                "Save To > .dat file: only the month on screen, the whole year, or every month kept.",
                "Backup / Restore also saves the kept scans.",
                "Edit Certificate Details: the names and positions of \"Prepared by\" and \"Certified correct\" on the certificate, with a live preview.",
                "Reports: the Time-In and Time-Out are written after each date, for example  Sep 21 - In 7:11, Out 6:05 PM (11m).",
                "Employee Names: Delete ticked employees, erasing everything about them (asked twice, and a backup is made first).",
                "About window with the version history; the version is shown in the title bar."),
            new VersionEntry("1.4.0", "October 28, 2026",
                "Flexi-Time in Edit DTR: the day is computed with the schedule that fits its times (for example 7-4 instead of 8-5), written in the Remark.",
                "Mondays: no grace period for the 8:00 AM and 9:00 AM schedules; 7:00 AM keeps it until 7:15 AM.",
                "Forgotten lunch scans are fine: a day with only the arrival and the departure has no warning.",
                "Reports: late and no Time-In on Mondays, both together, late and undertime in the month. Excel, PDF and print.",
                "Certificate of Leave Credits: every leave in the table, justified paragraphs, no footer note; Prepared by " +
                "MARY GRACE E. SACRAMENTO, Administrative Officer - IV; \"Position\" instead of \"Designation\"."),
            new VersionEntry("1.3.0", "October 27, 2026",
                "Backup / Restore of everything the program keeps, in one .zip file.",
                "Leave Summary of everyone (window and Excel).",
                "Reminders: Forced Leave not used yet (from November), and CTO / Solo Parent Leave about to expire.",
                "Certificate of Leave Credits (PDF and print) with the DepEd logo, Old English Text MT and Trajan Pro.",
                "Edit DTR and Edit Leave open maximized."),
            new VersionEntry("1.2.0", "October 27, 2026",
                "CTO: at most 15 days.",
                "CTO / Solo Parent window: what is left and the status of each line; used-up and expired lines are grey and locked.",
                "CTO and Solo Parent Leave cannot be filed without enough balance.",
                "VL and SL follow the CSC Omnibus Rules (carried over every year); SPL, Wellness and Forced Leave start again every January 1."),
            new VersionEntry("1.1.0", "October 24, 2026",
                "Solo Parent Leave: given by hand (at most 7 days), with its own column and balance.",
                "Typing the date given fills in a one-year expiry for CTO and Solo Parent Leave; expired days are deducted by themselves.",
                "History of Availment of Leave (January to December) below the Computation of Leaves.",
                "Grace period of 15 minutes, made up at the end of the day (7:14 AM in = out at 6:14 PM or later).",
                "Work suspended with work from home: \"PD - WFH\", or \"PD - WFH/IDLAR\" with the IDLAR."),
            new VersionEntry("1.0.0", "October 14, 2026",
                "First version: opens the .dat file of the biometrics device and makes the Daily Time Record (CS Form No. 48).",
                "Schedules A to D; Late, Undertime and ABSENT computed; Edit DTR for missing or wrong times.",
                "Edit Leave and the Computation of Leaves (VL, SL, SPL, Wellness, Forced Leave, CTO); holidays.",
                "Print, PDF and Excel."),
        };

        /// <summary>This is the version of the program: the newest entry of the history, e.g. "1.5.0".</summary>
        public static string Version
        {
            get { return History[0].Number; }
        }

        /// <summary>This is the date of this version, e.g. "October 29, 2026".</summary>
        public static string VersionDate
        {
            get { return History[0].Date; }
        }
    }
}
