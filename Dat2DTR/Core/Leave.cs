using System.Globalization;
using System.Text;

namespace Dat2DTR
{
    /// <summary>This is the list of kinds of leave that can be filed with "Edit Leave".</summary>
    public enum LeaveType
    {
        None = 0,
        Vacation = 1,
        Sick = 2,
        Forced = 3,            // This is Mandatory / Forced Leave
        SpecialPrivilege = 4,
        Wellness = 5,
        Cto = 6,               // This is Compensatory Time-Off
        Holiday = 7,           // This is a holiday: not a leave, nothing is deducted
        Suspended = 8,         // This is when work was suspended (typhoon, flood...): nothing is deducted
        WorkFromHome = 9,      // This is worked from home on an ordinary working day (the "WFH" box with no leave)
        OfficialBusiness = 10, // This is out of the office for work, e.g. visiting / inspecting schools: counted as present
        SoloParent = 11        // This is Solo Parent Leave: taken from the days given (like CTO), each with an expiry date
    }

    /// <summary>This is how much of the day the leave covers.</summary>
    public enum LeavePart
    {
        WholeDay = 0,
        Morning = 1,           // This is half day, the morning
        Afternoon = 2          // This is half day, the afternoon
    }

    /// <summary>This is which leave credits a kind of leave is taken from.</summary>
    public enum LeaveCharge
    {
        NotCharged,            // This is only counted (Special Privilege, Wellness, CTO, Solo Parent), or not a leave at all (Holiday...)
        Vacation,
        Sick
    }

    /// <summary>
    /// This is what the program knows about one kind of leave.
    ///
    /// THE LEAVE RULES (from the Civil Service Commission rules on leave):
    ///   Vacation Leave            - deducted from the Vacation Leave credits
    ///   Sick Leave                - deducted from the Sick Leave credits
    ///   Mandatory / Forced Leave  - deducted from the Vacation Leave credits, 5 days a year
    ///   Special Privilege Leave   - NOT deducted, only counted, 3 days a year
    ///   Wellness Leave            - NOT deducted, only counted, 5 days a year
    ///   CTO                       - taken from the CTO the employee was given (see CtoStore),
    ///                               at most LeaveKinds.CtoMaxDays at a time; unused CTO expires
    ///   Solo Parent Leave         - the same way as CTO: taken from the Solo Parent Leave days given
    ///                               (typed by hand, "Solo Parent given..."), at most LeaveKinds.SoloMaxDays
    ///                               at a time; days not used by their expiry date are lost
    /// Not leaves, but chosen in the same list (nothing is deducted, nothing is counted):
    ///   Holiday                   - the office is closed
    ///   Suspended                 - work was suspended; "Is it WFH?" + IDLAR = worked from home
    ///   Work from home            - an ordinary working day worked from home ("WFH" + IDLAR)
    ///   Official Business         - out of the office for work (for example inspecting schools on a Friday);
    ///                               the day counts as present
    /// To change a yearly limit, change the last number of that line in LeaveKinds.All below.
    /// </summary>
    public sealed class LeaveKind
    {
        public LeaveKind(LeaveType type, string name, string code, string label, LeaveCharge charge, int yearlyLimit)
        {
            Type = type;
            Name = name;
            Code = code;
            Label = label;
            Charge = charge;
            YearlyLimit = yearlyLimit;
        }

        public LeaveType Type { get; private set; }

        /// <summary>This is the name, e.g. "Vacation Leave" - shown in the lists on screen.</summary>
        public string Name { get; private set; }

        /// <summary>This is the short form, e.g. "VL" - used in notes and in the saved file.</summary>
        public string Code { get; private set; }

        /// <summary>This is the label, e.g. "VACATION LEAVE" - printed on the DTR in place of the times.</summary>
        public string Label { get; private set; }

        public LeaveCharge Charge { get; private set; }

        /// <summary>This is how many days a year are allowed (0 = no limit).</summary>
        public int YearlyLimit { get; private set; }
    }

    public static class LeaveKinds
    {
        /// <summary>This is the most CTO days an employee can have at one time (15 days).</summary>
        public const decimal CtoMaxDays = 15m;

        /// <summary>This is how long given CTO stays usable when nothing else is typed (one year).</summary>
        public const int CtoValidMonths = 12;

        /// <summary>This is the most Solo Parent Leave days an employee can have at one time (7 working days a year).</summary>
        public const decimal SoloMaxDays = 7m;

        /// <summary>This is how long given Solo Parent Leave stays usable when nothing else is typed (one year).</summary>
        public const int SoloValidMonths = 12;

        /// <summary>This is true for the leaves taken from days that were given by hand: CTO and Solo Parent Leave.</summary>
        public static bool IsGiven(LeaveType type)
        {
            return type == LeaveType.Cto || type == LeaveType.SoloParent;
        }

        /// <summary>This is the most days of CTO / Solo Parent Leave an employee can have at one time.</summary>
        public static decimal MaxGiven(LeaveType type)
        {
            return type == LeaveType.SoloParent ? SoloMaxDays : CtoMaxDays;
        }

        /// <summary>This is the expiry date when only the date given is typed: one year (Sep 15, 2026 -> Sep 14, 2027).</summary>
        public static DateTime DefaultExpiry(LeaveType type, DateTime given)
        {
            int months = type == LeaveType.SoloParent ? SoloValidMonths : CtoValidMonths;
            return given.Date.AddMonths(months).AddDays(-1);
        }

        public static readonly LeaveKind[] All =
        {
            new LeaveKind(LeaveType.Vacation, "Vacation Leave", "VL", "VACATION LEAVE", LeaveCharge.Vacation, 0),
            new LeaveKind(LeaveType.Sick, "Sick Leave", "SL", "SICK LEAVE", LeaveCharge.Sick, 0),
            new LeaveKind(LeaveType.Forced, "Mandatory / Forced Leave", "FL", "FORCED LEAVE", LeaveCharge.Vacation, 5),
            new LeaveKind(LeaveType.SpecialPrivilege, "Special Privilege Leave", "SPL", "SPECIAL PRIVILEGE LEAVE", LeaveCharge.NotCharged, 3),
            new LeaveKind(LeaveType.Wellness, "Wellness Leave", "WL", "WELLNESS LEAVE", LeaveCharge.NotCharged, 5),
            new LeaveKind(LeaveType.Cto, "CTO (Compensatory Time-Off)", "CTO", "CTO", LeaveCharge.NotCharged, 0),
            new LeaveKind(LeaveType.SoloParent, "Solo Parent Leave", "SOLO", "SOLO PARENT LEAVE", LeaveCharge.NotCharged, 0),
            new LeaveKind(LeaveType.Holiday, "Holiday", "HOL", "HOLIDAY", LeaveCharge.NotCharged, 0),
            new LeaveKind(LeaveType.Suspended, "Suspended (work suspension)", "SUS", "WORK SUSPENDED", LeaveCharge.NotCharged, 0),
            new LeaveKind(LeaveType.OfficialBusiness, "Official Business (school visit)", "OB", "OFFICIAL BUSINESS", LeaveCharge.NotCharged, 0),
            new LeaveKind(LeaveType.WorkFromHome, "Work from home", "WFH", "WORK FROM HOME", LeaveCharge.NotCharged, 0),
        };

        /// <summary>
        /// This is true for Holiday, Suspended and Work from home: days that are not a leave at all.
        /// Nothing is deducted or counted for them, and they may have the WFH and IDLAR boxes ticked.
        /// </summary>
        public static bool IsNotALeave(LeaveType type)
        {
            return type == LeaveType.Holiday || type == LeaveType.Suspended || type == LeaveType.WorkFromHome;
        }

        /// <summary>
        /// This is true for the days that are not deducted AND not counted against a yearly limit:
        /// Holiday, Suspended, Work from home and Official Business.
        /// </summary>
        public static bool IsNotCounted(LeaveType type)
        {
            return IsNotALeave(type) || type == LeaveType.OfficialBusiness;
        }

        public static LeaveKind Get(LeaveType type)
        {
            foreach (LeaveKind kind in All)
                if (kind.Type == type) return kind;
            return All[0];
        }

        /// <summary>This is where a kind of leave is found by its short form ("VL", "SL", ...), or null.</summary>
        public static LeaveKind? FromCode(string code)
        {
            code = code.Trim();
            foreach (LeaveKind kind in All)
                if (string.Equals(kind.Code, code, StringComparison.OrdinalIgnoreCase)) return kind;
            return null;
        }
    }

    /// <summary>
    /// This is one filed leave: the kind, and whether it is a whole day or half a day.
    /// For Holiday, Suspended and Work from home also: was the day worked from home (WFH),
    /// and was the IDLAR (Individual Daily Log and Accomplishment Report) handed in.
    /// With the IDLAR the day counts as a day at work.
    /// </summary>
    public sealed class LeaveEntry
    {
        public LeaveType Type = LeaveType.Vacation;
        public LeavePart Part = LeavePart.WholeDay;

        /// <summary>This is true when the day was worked from home (only for Holiday, Suspended and Work from home).</summary>
        public bool Wfh;

        /// <summary>This is true when the IDLAR was handed in (only when Wfh): the day counts as present.</summary>
        public bool Idlar;

        /// <summary>
        /// This is for Suspended only: work was suspended FROM this time (for example 3:00 PM). The day is an
        /// ordinary working day until then; no late or undertime is counted after it. null = the
        /// whole day (or the half day in Part) was suspended.
        /// </summary>
        public TimeSpan? From;

        /// <summary>This is true for a holiday that comes from the holiday calendar (not filed, not saved).</summary>
        public bool FromCalendar;

        /// <summary>This is the name of a holiday from the calendar, e.g. "Independence Day".</summary>
        public string HolidayName = "";

        public LeaveEntry()
        {
        }

        public LeaveEntry(LeaveType type, LeavePart part)
        {
            Type = type;
            Part = part;
        }

        public LeaveEntry(LeaveType type, LeavePart part, bool wfh, bool idlar)
        {
            Type = type;
            Part = part;
            // This is the WFH rule: WFH only goes with the days that are not a leave; IDLAR only goes with WFH.
            // (Work from home is always WFH.)
            Wfh = LeaveKinds.IsNotALeave(type) && (wfh || type == LeaveType.WorkFromHome);
            Idlar = Wfh && idlar;
        }

        /// <summary>
        /// This is false only for work from home without the IDLAR: the day is then not counted as
        /// present, and the DTR shows the times (or ABSENT) as if nothing was filed.
        /// </summary>
        public bool Excuses
        {
            get { return Type != LeaveType.WorkFromHome || Idlar; }
        }

        /// <summary>This is true when the day was worked from home, but the IDLAR is not ticked yet.</summary>
        public bool WaitsForIdlar
        {
            get { return Wfh && !Idlar; }
        }

        /// <summary>
        /// This is what the DTR shows in place of the times, e.g. "VACATION LEAVE", "HOLIDAY - WORK FROM HOME",
        /// or for a work suspension worked from home: "PD - WFH", and "PD - WFH/IDLAR" once the IDLAR is in.
        /// </summary>
        public string DtrLabel
        {
            get
            {
                if (Type == LeaveType.Holiday && Wfh && Idlar) return "HOLIDAY - WORK FROM HOME";
                if (Type == LeaveType.Suspended && Wfh) return Idlar ? "PD - WFH/IDLAR" : "PD - WFH";
                return Kind.Label;
            }
        }

        /// <summary>This is 1 for a whole day, 0.5 for half a day.</summary>
        public decimal Days
        {
            get { return Part == LeavePart.WholeDay ? 1m : 0.5m; }
        }

        public LeaveKind Kind
        {
            get { return LeaveKinds.Get(Type); }
        }

        public LeaveEntry Clone()
        {
            var copy = new LeaveEntry(Type, Part);
            copy.Wfh = Wfh;
            copy.Idlar = Idlar;
            copy.From = From;
            copy.FromCalendar = FromCalendar;
            copy.HolidayName = HolidayName;
            return copy;
        }

        public bool SameAs(LeaveEntry? other)
        {
            return other != null && other.Type == Type && other.Part == Part && other.Wfh == Wfh && other.Idlar == Idlar &&
                   other.From == From;
        }
    }

    /// <summary>
    /// This is the reader of the cells of the two leave files. It also understands the way Excel
    /// re-writes dates when someone opens one of the files in Excel and saves it again.
    /// </summary>
    internal static class LeaveFileText
    {
        private static readonly string[] DateFormats =
            { "yyyy-MM-dd", "M/d/yyyy", "yyyy/M/d", "d-MMM-yy", "d-MMM-yyyy" };

        private static readonly string[] MonthFormats =
            { "yyyy-MM", "yyyy-M", "MMM-yy", "MMM-yyyy", "yyyy/M", "M/yyyy" };

        /// <summary>This is the text of a cell without spaces and without "quotes" around it.</summary>
        public static string Clean(string cell)
        {
            cell = cell.Trim();
            if (cell.Length >= 2 && cell[0] == '"' && cell[cell.Length - 1] == '"')
                cell = cell.Substring(1, cell.Length - 2).Trim();
            return cell;
        }

        /// <summary>
        /// This is where a time of day is read: "15:00", "9:30", "15:00:00", "3:00 PM", or the fraction of a day
        /// Excel sometimes writes (0.625 = 3:00 PM). "W", "AM", "PM" are not times.
        /// </summary>
        public static bool TryReadTime(string cell, out TimeSpan time)
        {
            time = TimeSpan.Zero;
            string text = Clean(cell);
            if (text.Length == 0 || text == "W" || text == "AM" || text == "PM")
                return false;
            if (TimeSpan.TryParseExact(text, new[] { @"h\:mm", @"hh\:mm", @"h\:mm\:ss", @"hh\:mm\:ss" },
                                       CultureInfo.InvariantCulture, out time))
                return time > TimeSpan.Zero && time < TimeSpan.FromDays(1);
            DateTime parsed;
            if (DateTime.TryParseExact(text, new[] { "h:mm tt", "h:mm:ss tt", "hh:mm tt" }, CultureInfo.InvariantCulture,
                                       DateTimeStyles.AllowWhiteSpaces, out parsed))
            {
                time = parsed.TimeOfDay;
                return true;
            }
            double fraction;
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out fraction) && fraction > 0 && fraction < 1)
            {
                time = TimeSpan.FromMinutes(Math.Round(fraction * 24 * 60));
                return true;
            }
            return false;
        }

        public static bool TryReadDate(string cell, out DateTime date)
        {
            return DateTime.TryParseExact(Clean(cell), DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        }

        /// <summary>This is where "2026-09" (or Excel's "Sep-26") is read as the first day of that month.</summary>
        public static bool TryReadMonth(string cell, out DateTime month)
        {
            return DateTime.TryParseExact(Clean(cell), MonthFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out month);
        }
    }

    /// <summary>
    /// This is every leave that was filed with "Edit Leave", kept in a small file
    /// (leaves.csv) so it is still there the next time the month is opened.
    /// </summary>
    public sealed class LeaveStore
    {
        // This is "normalized ID|yyyy-MM-dd" -> leave
        private readonly Dictionary<string, LeaveEntry> _items =
            new Dictionary<string, LeaveEntry>(StringComparer.OrdinalIgnoreCase);

        public static string DefaultPath
        {
            get { return Path.Combine(DtrSettings.DataFolder, "leaves.csv"); }
        }

        private static string Key(string employeeId, DateTime date)
        {
            return EmployeeDirectory.NormalizeId(employeeId) + "|" + date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        /// <summary>This is the leave filed for that day, or null.</summary>
        public LeaveEntry? Get(string employeeId, DateTime date)
        {
            LeaveEntry? entry;
            return _items.TryGetValue(Key(employeeId, date), out entry) ? entry : null;
        }

        /// <summary>This is the holidays that apply to everyone (see HolidayCalendar), or null. Not saved in leaves.csv.</summary>
        public HolidayCalendar? Holidays;

        /// <summary>
        /// This is what the DTR uses for that day: the leave filed for the employee, or else the holiday
        /// from the holiday calendar (only on Monday to Friday).
        /// </summary>
        public LeaveEntry? GetForDtr(string employeeId, DateTime date, WorkSchedule schedule)
        {
            LeaveEntry? filed = Get(employeeId, date);
            // This is the order: a filed leave comes first - but not work from home still waiting for the IDLAR,
            // which does not count yet (the holiday of the calendar is shown then).
            bool weekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
            if ((filed != null && filed.Excuses) || Holidays == null || weekend)
                return filed;

            string? name = Holidays.GetName(date);
            if (name == null)
                return filed;
            var holiday = new LeaveEntry(LeaveType.Holiday, LeavePart.WholeDay);
            holiday.FromCalendar = true;
            holiday.HolidayName = name;
            return holiday;
        }

        /// <summary>This is where a leave is filed for that day. Passing null removes it.</summary>
        public void Set(string employeeId, DateTime date, LeaveEntry? entry)
        {
            string key = Key(employeeId, date);
            if (entry == null || entry.Type == LeaveType.None) _items.Remove(key);
            else _items[key] = entry.Clone();
        }

        /// <summary>This is the leaves of one employee in one month, in date order.</summary>
        public List<KeyValuePair<DateTime, LeaveEntry>> GetMonth(string employeeId, int year, int month)
        {
            var result = new List<KeyValuePair<DateTime, LeaveEntry>>();
            int days = DateTime.DaysInMonth(year, month);
            for (int d = 1; d <= days; d++)
            {
                var date = new DateTime(year, month, d);
                LeaveEntry? entry = Get(employeeId, date);
                if (entry != null)
                    result.Add(new KeyValuePair<DateTime, LeaveEntry>(date, entry));
            }
            return result;
        }

        /// <summary>This is every leave of one kind filed for one employee, in date order (any month, any year).</summary>
        public List<KeyValuePair<DateTime, LeaveEntry>> GetAllOfKind(string employeeId, LeaveType type)
        {
            string prefix = EmployeeDirectory.NormalizeId(employeeId) + "|";
            var result = new List<KeyValuePair<DateTime, LeaveEntry>>();
            foreach (var pair in _items)
            {
                if (pair.Value.Type != type || !pair.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;
                DateTime date;
                if (DateTime.TryParseExact(pair.Key.Substring(prefix.Length), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                                           DateTimeStyles.None, out date))
                    result.Add(new KeyValuePair<DateTime, LeaveEntry>(date, pair.Value));
            }
            result.Sort((a, b) => a.Key.CompareTo(b.Key));
            return result;
        }

        /// <summary>
        /// This is the list of employees (IDs without leading zeros) who have a leave filed in that month.
        /// </summary>
        public List<string> GetIdsWithLeave(int year, int month)
        {
            string wanted = "|" + year.ToString("0000", CultureInfo.InvariantCulture) + "-" +
                            month.ToString("00", CultureInfo.InvariantCulture) + "-";
            var ids = new List<string>();
            foreach (string key in _items.Keys)
            {
                int bar = key.IndexOf('|');
                if (bar > 0 && string.CompareOrdinal(key, bar, wanted, 0, wanted.Length) == 0)
                {
                    string id = key.Substring(0, bar);
                    if (!ids.Contains(id)) ids.Add(id);
                }
            }
            return ids;
        }

        /// <summary>This is where everything kept about one employee is removed (Employee Names, Delete). Returns how many were removed.</summary>
        public int RemoveEmployee(string employeeId)
        {
            string prefix = EmployeeDirectory.NormalizeId(employeeId) + "|";
            List<string> keys = _items.Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (string key in keys)
                _items.Remove(key);
            return keys.Count;
        }

        public int Count
        {
            get { return _items.Count; }
        }

        /// <summary>
        /// This is true when the saved file exists but could not be read (for example because it is open
        /// in Excel). The program then must not save, or it would wipe out what is in the file.
        /// </summary>
        public bool LoadFailed { get; private set; }

        public LeaveStore Clone()
        {
            var copy = new LeaveStore();
            copy.LoadFailed = LoadFailed;
            copy.Holidays = Holidays;
            foreach (var pair in _items)
                copy._items[pair.Key] = pair.Value.Clone();
            return copy;
        }

        // This is the part that reads and saves the files -------------------------------------------
        // This is the file: one line per day of leave:   ID,date,kind,part,WFH,IDLAR
        //                                           e.g. 070126010,2026-09-21,VL,W,,      (W = whole day, AM / PM = half day)
        //                                                070126010,2026-09-24,SUS,W,Y,Y   (Y = the box is ticked)
        //                                                070126010,2026-09-25,SUS,15:00,,  (work suspended from 3:00 PM)

        public static LeaveStore LoadDefault()
        {
            var store = new LeaveStore();
            try
            {
                if (File.Exists(DefaultPath))
                    store.Load(DefaultPath);
            }
            catch (IOException) { store.LoadFailed = true; }
            catch (UnauthorizedAccessException) { store.LoadFailed = true; }
            return store;
        }

        public void SaveDefault()
        {
            Save(DefaultPath);
        }

        public void Load(string path)
        {
            foreach (string line in File.ReadAllLines(path, Encoding.UTF8))
            {
                string[] cells = line.Split(',');
                if (cells.Length < 3) continue;

                DateTime date;
                if (!LeaveFileText.TryReadDate(cells[1], out date))
                    continue; // This is the heading line, or a damaged line

                string id = LeaveFileText.Clean(cells[0]);
                LeaveKind? kind = LeaveKinds.FromCode(LeaveFileText.Clean(cells[2]));
                if (kind == null || id.Length == 0) continue;

                string part = cells.Length > 3 ? LeaveFileText.Clean(cells[3]).ToUpperInvariant() : "W";
                bool wfh = cells.Length > 4 && IsTicked(cells[4]);
                bool idlar = cells.Length > 5 && IsTicked(cells[5]);
                var entry = new LeaveEntry(kind.Type,
                    part == "AM" ? LeavePart.Morning : part == "PM" ? LeavePart.Afternoon : LeavePart.WholeDay,
                    wfh, idlar);
                // This is "15:00" in the Part column = Suspended from 3:00 PM.
                TimeSpan from;
                if (kind.Type == LeaveType.Suspended && LeaveFileText.TryReadTime(LeaveFileText.Clean(cells.Length > 3 ? cells[3] : ""), out from))
                    entry.From = from;
                _items[Key(id, date)] = entry;
            }
        }

        private static bool IsTicked(string cell)
        {
            string text = LeaveFileText.Clean(cell).ToUpperInvariant();
            return text == "Y" || text == "YES" || text == "1" || text == "TRUE";
        }

        public void Save(string path)
        {
            var sb = new StringBuilder();
            sb.AppendLine("ID,Date,Leave,Part,WFH,IDLAR");
            foreach (var pair in _items.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
            {
                string[] key = pair.Key.Split('|');
                LeaveEntry entry = pair.Value;
                string part = entry.Part == LeavePart.Morning ? "AM" : entry.Part == LeavePart.Afternoon ? "PM" : "W";
                if (entry.From.HasValue)
                    part = entry.From.Value.ToString(@"hh\:mm", CultureInfo.InvariantCulture);
                sb.AppendLine(key[0] + "," + key[1] + "," + entry.Kind.Code + "," + part + "," +
                              (entry.Wfh ? "Y" : "") + "," + (entry.Idlar ? "Y" : ""));
            }
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        }
    }

    /// <summary>This is CTO (Compensatory Time-Off) that was given to an employee: how many days, and until when it can be used.</summary>
    public sealed class CtoGrant
    {
        public DateTime Given;
        public decimal Days;
        public DateTime Expires;

        public CtoGrant Clone()
        {
            return (CtoGrant)MemberwiseClone();
        }
    }

    /// <summary>
    /// This is the CTO and the Solo Parent Leave given to each employee, kept in two small files
    /// (cto.csv and solo-parent.csv). Using CTO or Solo Parent Leave (filed in "Edit Leave")
    /// takes it from these, the one that expires first first.
    /// </summary>
    public sealed class CtoStore
    {
        // This is normalized ID -> what was given, in date order. One for CTO, one for Solo Parent Leave.
        private readonly Dictionary<string, List<CtoGrant>> _items =
            new Dictionary<string, List<CtoGrant>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<CtoGrant>> _solo =
            new Dictionary<string, List<CtoGrant>>(StringComparer.OrdinalIgnoreCase);

        public static string DefaultPath
        {
            get { return Path.Combine(DtrSettings.DataFolder, "cto.csv"); }
        }

        /// <summary>This is the file of the Solo Parent Leave given.</summary>
        public static string SoloPath
        {
            get { return Path.Combine(DtrSettings.DataFolder, "solo-parent.csv"); }
        }

        private Dictionary<string, List<CtoGrant>> Of(LeaveType type)
        {
            return type == LeaveType.SoloParent ? _solo : _items;
        }

        /// <summary>This is a copy of the CTO given to that employee, in date order (empty when none).</summary>
        public List<CtoGrant> Get(string employeeId)
        {
            return Get(employeeId, LeaveType.Cto);
        }

        /// <summary>This is a copy of the CTO or Solo Parent Leave given to that employee, in date order.</summary>
        public List<CtoGrant> Get(string employeeId, LeaveType type)
        {
            List<CtoGrant>? list;
            if (!Of(type).TryGetValue(EmployeeDirectory.NormalizeId(employeeId), out list))
                return new List<CtoGrant>();
            return list.Select(g => g.Clone()).ToList();
        }

        /// <summary>This is where the CTO given to that employee is replaced.</summary>
        public void Set(string employeeId, IEnumerable<CtoGrant> grants)
        {
            Set(employeeId, LeaveType.Cto, grants);
        }

        /// <summary>This is where the CTO or Solo Parent Leave given to that employee is replaced.</summary>
        public void Set(string employeeId, LeaveType type, IEnumerable<CtoGrant> grants)
        {
            List<CtoGrant> list = grants.Where(g => g.Days > 0).Select(g => g.Clone())
                                        .OrderBy(g => g.Given).ThenBy(g => g.Expires).ToList();
            string key = EmployeeDirectory.NormalizeId(employeeId);
            if (list.Count == 0) Of(type).Remove(key);
            else Of(type)[key] = list;
        }

        /// <summary>This is the same as LeaveStore.LoadFailed.</summary>
        public bool LoadFailed { get; private set; }

        /// <summary>This is where the CTO and Solo Parent Leave given to one employee are removed (Employee Names, Delete).</summary>
        public int RemoveEmployee(string employeeId)
        {
            string key = EmployeeDirectory.NormalizeId(employeeId);
            int removed = (_items.Remove(key) ? 1 : 0) + (_solo.Remove(key) ? 1 : 0);
            return removed;
        }

        public int Count
        {
            get { return _items.Values.Sum(l => l.Count) + _solo.Values.Sum(l => l.Count); }
        }

        public CtoStore Clone()
        {
            var copy = new CtoStore();
            copy.LoadFailed = LoadFailed;
            foreach (var pair in _items)
                copy._items[pair.Key] = pair.Value.Select(g => g.Clone()).ToList();
            foreach (var pair in _solo)
                copy._solo[pair.Key] = pair.Value.Select(g => g.Clone()).ToList();
            return copy;
        }

        // This is the file: one line per time CTO (or Solo Parent Leave) was given:   ID,given,days,expires
        //                                                                      e.g. 070126010,2026-09-15,2,2027-09-14

        public static CtoStore LoadDefault()
        {
            var store = new CtoStore();
            try
            {
                if (File.Exists(DefaultPath))
                    store.Load(DefaultPath);
                if (File.Exists(SoloPath))
                    store.Load(SoloPath, LeaveType.SoloParent);
            }
            catch (IOException) { store.LoadFailed = true; }
            catch (UnauthorizedAccessException) { store.LoadFailed = true; }
            return store;
        }

        public void SaveDefault()
        {
            Save(DefaultPath);
            // This is because the Solo Parent file is only made once someone was given Solo Parent Leave
            if (_solo.Count > 0 || File.Exists(SoloPath))
                Save(SoloPath, LeaveType.SoloParent);
        }

        /// <summary>This is where a file of CTO given is read.</summary>
        public void Load(string path)
        {
            Load(path, LeaveType.Cto);
        }

        /// <summary>This is where a file of CTO or Solo Parent Leave given is read.</summary>
        public void Load(string path, LeaveType type)
        {
            Dictionary<string, List<CtoGrant>> items = Of(type);
            foreach (string line in File.ReadAllLines(path, Encoding.UTF8))
            {
                string[] cells = line.Split(',');
                if (cells.Length < 4) continue;

                string id = LeaveFileText.Clean(cells[0]);
                DateTime given, expires;
                decimal days;
                if (id.Length == 0 || !LeaveFileText.TryReadDate(cells[1], out given) ||
                    !LeaveFileText.TryReadDate(cells[3], out expires) ||
                    !decimal.TryParse(LeaveFileText.Clean(cells[2]), NumberStyles.Float, CultureInfo.InvariantCulture, out days) ||
                    days <= 0 || days > 99)
                    continue; // This is the heading line, or a damaged line

                string key = EmployeeDirectory.NormalizeId(id);
                List<CtoGrant>? list;
                if (!items.TryGetValue(key, out list))
                {
                    list = new List<CtoGrant>();
                    items[key] = list;
                }
                list.Add(new CtoGrant { Given = given, Days = Math.Round(days, 3), Expires = expires });
            }
            foreach (List<CtoGrant> list in items.Values)
                list.Sort((a, b) => a.Given.CompareTo(b.Given));
        }

        /// <summary>This is where the file of CTO given is written.</summary>
        public void Save(string path)
        {
            Save(path, LeaveType.Cto);
        }

        /// <summary>This is where the file of CTO or Solo Parent Leave given is written.</summary>
        public void Save(string path, LeaveType type)
        {
            var sb = new StringBuilder();
            sb.AppendLine("ID,Given,Days,Expires");
            foreach (var pair in Of(type).OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
                foreach (CtoGrant g in pair.Value)
                    sb.AppendLine(pair.Key + "," + g.Given.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "," +
                                  g.Days.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                                  g.Expires.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        }
    }

    /// <summary>
    /// This is what the program remembers about one month of one employee, so the leave
    /// balance can be carried from month to month.
    /// </summary>
    public sealed class LedgerMonth
    {
        /// <summary>This is the balance at the START of the month, typed by a person (null = not typed).</summary>
        public decimal? OpeningVl, OpeningSl;

        /// <summary>
        /// This is the Special Privilege Leave and Wellness Leave LEFT at the start of the month, typed by a person in Edit Leave
        /// (null = not typed: worked out from the leaves filed this year). Used for days taken before the program was used.
        /// </summary>
        public decimal? OpeningSpl, OpeningWl;

        /// <summary>
        /// This is the Mandatory / Forced Leave LEFT at the start of the month (still to be taken this year, 5 days a year),
        /// typed by a person in Edit Leave (null = not typed: worked out from the leaves filed this year).
        /// </summary>
        public decimal? OpeningFl;

        /// <summary>This is the credits earned in the month, typed by a person (null = the usual 1.25).</summary>
        public decimal? EarnedVl, EarnedSl;

        /// <summary>
        /// This is what is remembered from the last time the month was open: total late and undertime (minutes),
        /// how many leave credits one day of leave costs, and the last date the .dat file covered.
        /// </summary>
        public int? LateMinutes, UndertimeMinutes;
        public decimal? DayCharge;
        public DateTime? DataThrough;

        /// <summary>This is true when the month was opened in the program at least once.</summary>
        public bool IsRemembered
        {
            get { return LateMinutes.HasValue && UndertimeMinutes.HasValue && DayCharge.HasValue; }
        }

        public bool IsEmpty
        {
            get
            {
                return !OpeningVl.HasValue && !OpeningSl.HasValue && !OpeningSpl.HasValue && !OpeningWl.HasValue && !OpeningFl.HasValue &&
                       !EarnedVl.HasValue && !EarnedSl.HasValue &&
                       !LateMinutes.HasValue && !UndertimeMinutes.HasValue && !DayCharge.HasValue;
            }
        }

        public LedgerMonth Clone()
        {
            return (LedgerMonth)MemberwiseClone();
        }
    }

    /// <summary>
    /// This is the remembered months of every employee, kept in a small file (leave-ledger.csv).
    /// </summary>
    public sealed class LeaveLedger
    {
        // This is "normalized ID|yyyy-MM" -> month
        private readonly Dictionary<string, LedgerMonth> _items =
            new Dictionary<string, LedgerMonth>(StringComparer.OrdinalIgnoreCase);

        public static string DefaultPath
        {
            get { return Path.Combine(DtrSettings.DataFolder, "leave-ledger.csv"); }
        }

        private static string Key(string employeeId, int year, int month)
        {
            return EmployeeDirectory.NormalizeId(employeeId) + "|" +
                   year.ToString("0000", CultureInfo.InvariantCulture) + "-" + month.ToString("00", CultureInfo.InvariantCulture);
        }

        /// <summary>This is what is remembered about that month, or null.</summary>
        public LedgerMonth? Get(string employeeId, int year, int month)
        {
            LedgerMonth? item;
            return _items.TryGetValue(Key(employeeId, year, month), out item) ? item : null;
        }

        /// <summary>This is the month's entry, made new when there is none yet.</summary>
        public LedgerMonth GetOrAdd(string employeeId, int year, int month)
        {
            string key = Key(employeeId, year, month);
            LedgerMonth? item;
            if (!_items.TryGetValue(key, out item))
            {
                item = new LedgerMonth();
                _items[key] = item;
            }
            return item;
        }

        /// <summary>
        /// This is true when the opened file starts too late to hold the whole month: its first scan
        /// is after the first week. (A few days are allowed for weekends and holidays.)
        /// </summary>
        public static bool StartsLate(int year, int month, DateTime dataFrom)
        {
            return dataFrom.Date > new DateTime(year, month, 1).AddDays(6);
        }

        /// <summary>
        /// This is true when that month was already remembered from a file that went further into the
        /// month than this one. The earlier, more complete result is then kept.
        /// </summary>
        public bool HasMoreComplete(string employeeId, int year, int month, DateTime dataThrough)
        {
            LedgerMonth? item = Get(employeeId, year, month);
            return item != null && item.IsRemembered && item.DataThrough.HasValue &&
                   item.DataThrough.Value > dataThrough.Date;
        }

        /// <summary>
        /// This is where the late and undertime totals of a month are remembered, so the balance can be carried over
        /// next month. Returns true when something changed (the file is only written when needed).
        ///
        /// Nothing is remembered when the opened file holds only a part of the month:
        /// when it starts late (StartsLate), or when the month is already remembered from
        /// a file that went further (HasMoreComplete). Looking at a month through the wrong
        /// file must never replace a good result with half a month.
        /// </summary>
        /// <param name="dataFrom">This is the date of the first scan in the opened file.</param>
        /// <param name="dataThrough">This is the last day of this month that the opened file covers.</param>
        public bool Remember(string employeeId, int year, int month, int lateMinutes, int undertimeMinutes,
                             decimal dayCharge, DateTime dataFrom, DateTime dataThrough)
        {
            if (StartsLate(year, month, dataFrom) || HasMoreComplete(employeeId, year, month, dataThrough))
                return false;

            LedgerMonth item = GetOrAdd(employeeId, year, month);
            if (item.LateMinutes == lateMinutes && item.UndertimeMinutes == undertimeMinutes &&
                item.DayCharge == dayCharge && item.DataThrough == dataThrough.Date)
                return false;

            item.LateMinutes = lateMinutes;
            item.UndertimeMinutes = undertimeMinutes;
            item.DayCharge = dayCharge;
            item.DataThrough = dataThrough.Date;
            return true;
        }

        /// <summary>
        /// This is the list of employees (IDs without leading zeros) whose month is remembered.
        /// </summary>
        public List<string> GetRememberedIds(int year, int month)
        {
            string wanted = "|" + year.ToString("0000", CultureInfo.InvariantCulture) + "-" +
                            month.ToString("00", CultureInfo.InvariantCulture);
            var ids = new List<string>();
            foreach (var pair in _items)
            {
                if (pair.Value.IsRemembered && pair.Key.EndsWith(wanted, StringComparison.Ordinal))
                    ids.Add(pair.Key.Substring(0, pair.Key.Length - wanted.Length));
            }
            return ids;
        }

        /// <summary>This is where everything kept about one employee is removed (Employee Names, Delete). Returns how many were removed.</summary>
        public int RemoveEmployee(string employeeId)
        {
            string prefix = EmployeeDirectory.NormalizeId(employeeId) + "|";
            List<string> keys = _items.Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (string key in keys)
                _items.Remove(key);
            return keys.Count;
        }

        public int Count
        {
            get { return _items.Count; }
        }

        /// <summary>
        /// This is true when the saved file exists but could not be read (for example because it is open
        /// in Excel). The program then must not save, or it would wipe out what is in the file.
        /// </summary>
        public bool LoadFailed { get; private set; }

        public LeaveLedger Clone()
        {
            var copy = new LeaveLedger();
            copy.LoadFailed = LoadFailed;
            foreach (var pair in _items)
                copy._items[pair.Key] = pair.Value.Clone();
            return copy;
        }

        // This is the part that reads and saves the files -------------------------------------------
        // This is the file: one line per employee and month:
        //   ID,Month,Opening VL,Opening SL,Earned VL,Earned SL,Late minutes,Undertime minutes,Day charge,Data through
        //   070126010,2026-09,20.027,9.75,,,195,300,1.25,2026-09-30

        public static LeaveLedger LoadDefault()
        {
            var ledger = new LeaveLedger();
            try
            {
                if (File.Exists(DefaultPath))
                    ledger.Load(DefaultPath);
            }
            catch (IOException) { ledger.LoadFailed = true; }
            catch (UnauthorizedAccessException) { ledger.LoadFailed = true; }
            return ledger;
        }

        public void SaveDefault()
        {
            Save(DefaultPath);
        }

        public void Load(string path)
        {
            foreach (string line in File.ReadAllLines(path, Encoding.UTF8))
            {
                string[] cells = line.Split(',');
                string id = LeaveFileText.Clean(cells[0]);
                if (cells.Length < 10 || id.Length == 0) continue;

                DateTime month;
                if (!LeaveFileText.TryReadMonth(cells[1], out month))
                    continue; // This is the heading line, or a damaged line

                var item = new LedgerMonth();
                item.OpeningVl = ReadDecimal(cells[2]);
                item.OpeningSl = ReadDecimal(cells[3]);
                item.EarnedVl = ReadDecimal(cells[4]);
                item.EarnedSl = ReadDecimal(cells[5]);
                item.LateMinutes = ReadInt(cells[6]);
                item.UndertimeMinutes = ReadInt(cells[7]);
                item.DayCharge = ReadDecimal(cells[8]);

                DateTime through;
                if (LeaveFileText.TryReadDate(cells[9], out through))
                    item.DataThrough = through;

                // This is the SPL and WL left (added later: older files do not have these two columns).
                if (cells.Length > 11)
                {
                    item.OpeningSpl = ReadDecimal(cells[10]);
                    item.OpeningWl = ReadDecimal(cells[11]);
                }

                // This is the Forced Leave left (added after SPL and WL: older files do not have this column).
                if (cells.Length > 12)
                    item.OpeningFl = ReadDecimal(cells[12]);

                if (!item.IsEmpty)
                    _items[Key(id, month.Year, month.Month)] = item;
            }
        }

        public void Save(string path)
        {
            var sb = new StringBuilder();
            sb.AppendLine("ID,Month,Opening VL,Opening SL,Earned VL,Earned SL,Late minutes,Undertime minutes,Day charge,Data through,Opening SPL,Opening WL,Opening FL");
            foreach (var pair in _items.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
            {
                LedgerMonth m = pair.Value;
                if (m.IsEmpty) continue;

                string[] key = pair.Key.Split('|');
                sb.AppendLine(string.Join(",", new[]
                {
                    key[0], key[1],
                    Write(m.OpeningVl), Write(m.OpeningSl), Write(m.EarnedVl), Write(m.EarnedSl),
                    m.LateMinutes.HasValue ? m.LateMinutes.Value.ToString(CultureInfo.InvariantCulture) : "",
                    m.UndertimeMinutes.HasValue ? m.UndertimeMinutes.Value.ToString(CultureInfo.InvariantCulture) : "",
                    Write(m.DayCharge),
                    m.DataThrough.HasValue ? m.DataThrough.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "",
                    Write(m.OpeningSpl), Write(m.OpeningWl), Write(m.OpeningFl)
                }));
            }
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        }

        /// <summary>This is how leave credits are kept: with three decimals, so anything finer is rounded away.</summary>
        private static decimal? ReadDecimal(string text)
        {
            decimal value;
            if (decimal.TryParse(LeaveFileText.Clean(text), NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
                value > -1000m && value < 1000m)
                return Math.Round(value, 3, MidpointRounding.AwayFromZero);
            return null;
        }

        private static int? ReadInt(string text)
        {
            int value;
            if (int.TryParse(LeaveFileText.Clean(text), NumberStyles.Integer, CultureInfo.InvariantCulture, out value) && value >= 0)
                return value;
            return null;
        }

        private static string Write(decimal? value)
        {
            return value.HasValue ? value.Value.ToString("0.###", CultureInfo.InvariantCulture) : "";
        }
    }
}
