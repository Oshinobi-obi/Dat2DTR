using System.Globalization;
using System.Text;

namespace Dat2DTR
{
    /// <summary>Regular holiday, or special (non-working) day.</summary>
    public enum HolidayKind
    {
        Regular = 0,
        Special = 1     // special (non-working) day
    }

    /// <summary>One holiday: the date, its name (for example "Independence Day") and its kind.</summary>
    public sealed class Holiday
    {
        public DateTime Date;
        public string Name = "";
        public HolidayKind Kind = HolidayKind.Regular;
    }

    /// <summary>
    /// The holidays of the year, typed once in "Holidays..." and used for EVERY employee:
    /// on a holiday the DTR says HOLIDAY instead of ABSENT and nothing is deducted.
    /// A leave filed in Edit Leave on the same day comes first.
    /// Kept in a small file (holidays.csv) next to the other saved files.
    /// </summary>
    public sealed class HolidayCalendar
    {
        private readonly Dictionary<DateTime, Holiday> _days = new Dictionary<DateTime, Holiday>();

        public static string DefaultPath
        {
            get { return Path.Combine(DtrSettings.DataFolder, "holidays.csv"); }
        }

        /// <summary>The name of the holiday on that date, or null when it is not a holiday.</summary>
        public string? GetName(DateTime date)
        {
            Holiday? holiday;
            return _days.TryGetValue(date.Date, out holiday) ? holiday.Name : null;
        }

        /// <summary>Regular holiday or special (non-working) day; null when the date is not a holiday.</summary>
        public HolidayKind? GetKind(DateTime date)
        {
            Holiday? holiday;
            return _days.TryGetValue(date.Date, out holiday) ? holiday.Kind : (HolidayKind?)null;
        }

        /// <summary>Adds or changes a holiday. An empty name is saved as "Holiday".</summary>
        public void Set(DateTime date, string name, HolidayKind kind)
        {
            name = (name ?? "").Trim();
            _days[date.Date] = new Holiday { Date = date.Date, Name = name.Length == 0 ? "Holiday" : name, Kind = kind };
        }

        /// <summary>Adds or changes a holiday; the kind is guessed from the name (see GuessKind).</summary>
        public void Set(DateTime date, string name)
        {
            Set(date, name, GuessKind(name));
        }

        /// <summary>
        /// The kind of a holiday from its name: the special (non-working) days the Proclamations
        /// usually declare. Everything else is taken as a regular holiday.
        /// </summary>
        public static HolidayKind GuessKind(string name)
        {
            string n = (name ?? "").ToLowerInvariant();
            string[] special = { "black saturday", "ninoy", "all saints", "all souls", "immaculate", "last day of the year",
                                 "chinese new year", "christmas eve", "edsa", "special" };
            foreach (string s in special)
                if (n.Contains(s)) return HolidayKind.Special;
            return HolidayKind.Regular;
        }

        public void Remove(DateTime date)
        {
            _days.Remove(date.Date);
        }

        public void Clear()
        {
            _days.Clear();
        }

        /// <summary>Every holiday, in date order.</summary>
        public List<Holiday> GetAll()
        {
            return _days.OrderBy(p => p.Key).Select(p => new Holiday { Date = p.Key, Name = p.Value.Name, Kind = p.Value.Kind }).ToList();
        }

        public int Count
        {
            get { return _days.Count; }
        }

        /// <summary>True when the saved file exists but could not be read: then nothing may be saved over it.</summary>
        public bool LoadFailed { get; private set; }

        public HolidayCalendar Clone()
        {
            var copy = new HolidayCalendar();
            copy.LoadFailed = LoadFailed;
            foreach (var pair in _days)
                copy._days[pair.Key] = new Holiday { Date = pair.Value.Date, Name = pair.Value.Name, Kind = pair.Value.Kind };
            return copy;
        }

        // ---------------------------------------------------------------- the holidays fixed by law

        /// <summary>
        /// The Philippine holidays whose dates can be worked out for any year (Republic Act No. 9492
        /// and the laws after it). Holidays whose date is only announced by Proclamation each year
        /// (Chinese New Year, Eid'l Fitr, Eid'l Adha, extra special days) are NOT in this list:
        /// they are added by hand. The Proclamation of the year should always be checked, because
        /// a holiday can be moved.
        /// </summary>
        public static List<Holiday> PhilippineHolidays(int year)
        {
            DateTime easter = EasterSunday(year);
            var lastMondayOfAugust = new DateTime(year, 8, 31);
            while (lastMondayOfAugust.DayOfWeek != DayOfWeek.Monday)
                lastMondayOfAugust = lastMondayOfAugust.AddDays(-1);

            var list = new List<Holiday>
            {
                // regular holidays
                new Holiday { Date = new DateTime(year, 1, 1), Name = "New Year's Day" },
                new Holiday { Date = easter.AddDays(-3), Name = "Maundy Thursday" },
                new Holiday { Date = easter.AddDays(-2), Name = "Good Friday" },
                new Holiday { Date = new DateTime(year, 4, 9), Name = "Araw ng Kagitingan" },
                new Holiday { Date = new DateTime(year, 5, 1), Name = "Labor Day" },
                new Holiday { Date = new DateTime(year, 6, 12), Name = "Independence Day" },
                new Holiday { Date = lastMondayOfAugust, Name = "National Heroes Day" },
                new Holiday { Date = new DateTime(year, 11, 30), Name = "Bonifacio Day" },
                new Holiday { Date = new DateTime(year, 12, 25), Name = "Christmas Day" },
                new Holiday { Date = new DateTime(year, 12, 30), Name = "Rizal Day" },
                // special (non-working) days
                new Holiday { Date = easter.AddDays(-1), Name = "Black Saturday", Kind = HolidayKind.Special },
                new Holiday { Date = new DateTime(year, 8, 21), Name = "Ninoy Aquino Day", Kind = HolidayKind.Special },
                new Holiday { Date = new DateTime(year, 11, 1), Name = "All Saints' Day", Kind = HolidayKind.Special },
                new Holiday { Date = new DateTime(year, 12, 8), Name = "Feast of the Immaculate Conception of Mary", Kind = HolidayKind.Special },
                new Holiday { Date = new DateTime(year, 12, 31), Name = "Last Day of the Year", Kind = HolidayKind.Special },
            };
            return list.OrderBy(h => h.Date).ToList();
        }

        /// <summary>Easter Sunday (Gregorian calendar), for Holy Week.</summary>
        public static DateTime EasterSunday(int year)
        {
            int a = year % 19, b = year / 100, c = year % 100, d = b / 4, e = b % 4;
            int f = (b + 8) / 25, g = (b - f + 1) / 3, h = (19 * a + b - d - g + 15) % 30;
            int i = c / 4, k = c % 4, l = (32 + 2 * e + 2 * i - h - k) % 7;
            int m = (a + 11 * h + 22 * l) / 451;
            int month = (h + l - 7 * m + 114) / 31, day = (h + l - 7 * m + 114) % 31 + 1;
            return new DateTime(year, month, day);
        }

        // ---------------------------------------------------------------- files
        // One line per holiday:   date,name,kind       e.g. 2026-06-12,"Independence Day",Regular
        //                                                2026-08-21,"Ninoy Aquino Day",Special
        // (A file without the kind, from before, still reads: the kind is guessed from the name.)

        public static HolidayCalendar LoadDefault()
        {
            var calendar = new HolidayCalendar();
            try
            {
                if (File.Exists(DefaultPath))
                    calendar.Load(DefaultPath);
            }
            catch (IOException) { calendar.LoadFailed = true; }
            catch (UnauthorizedAccessException) { calendar.LoadFailed = true; }
            return calendar;
        }

        public void SaveDefault()
        {
            Save(DefaultPath);
        }

        public void Load(string path)
        {
            foreach (string line in File.ReadAllLines(path, Encoding.UTF8))
            {
                string[] cells = line.Split(new[] { ',' }, 2);
                DateTime date;
                if (cells.Length < 1 || !LeaveFileText.TryReadDate(cells[0], out date))
                    continue; // the heading line, or a damaged line
                string rest = cells.Length > 1 ? cells[1] : "";
                HolidayKind? kind = null;
                int lastComma = rest.LastIndexOf(',');
                if (lastComma >= 0)
                {
                    string tail = rest.Substring(lastComma + 1).Trim().ToLowerInvariant();
                    if (tail == "regular") kind = HolidayKind.Regular;
                    else if (tail == "special") kind = HolidayKind.Special;
                    if (kind.HasValue) rest = rest.Substring(0, lastComma);
                }
                string name = LeaveFileText.Clean(rest).Replace("\"\"", "\"");
                if (kind.HasValue) Set(date, name, kind.Value);
                else Set(date, name);
            }
        }

        public void Save(string path)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Date,Name,Kind");
            foreach (Holiday h in GetAll())
                sb.AppendLine(h.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ",\"" + h.Name.Replace("\"", "\"\"") + "\"," + h.Kind);
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        }
    }
}
