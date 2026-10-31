using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Dat2DTR
{
    /// <summary>
    /// This is what a person typed in by hand for one day of one employee: the four times
    /// (any of them may be empty) and an optional remark such as HOLIDAY or LEAVE.
    /// When a day has a correction, it REPLACES what the biometrics file says for that day.
    /// </summary>
    public sealed class DayCorrection
    {
        public TimeSpan? AmArrival;
        public TimeSpan? AmDeparture;
        public TimeSpan? PmArrival;
        public TimeSpan? PmDeparture;
        public string Remark = "";

        public TimeSpan?[] Times
        {
            get { return new[] { AmArrival, AmDeparture, PmArrival, PmDeparture }; }
        }

        public DayCorrection Clone()
        {
            return (DayCorrection)MemberwiseClone();
        }

        /// <summary>This is true when both hold the same four times and the same remark.</summary>
        public bool SameAs(DayCorrection other)
        {
            return AmArrival == other.AmArrival && AmDeparture == other.AmDeparture &&
                   PmArrival == other.PmArrival && PmDeparture == other.PmDeparture &&
                   string.Equals(Remark.Trim(), other.Remark.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// This is the Late and Undertime of one day typed in by hand, in place of what the program computed
    /// (for example when work was suspended at 3:00 PM and everyone went home at 3:00).
    /// null = keep the computed value. The times of the day are not changed.
    /// It also holds the Flexi-Time box of the day (see DtrBuilder rule 10).
    /// </summary>
    public sealed class DayAdjustment
    {
        public int? LateMinutes;
        public int? UndertimeMinutes;

        /// <summary>This is why it was changed, e.g. "Work suspended at 3:00 PM". Listed on screen, not printed.</summary>
        public string Reason = "";

        /// <summary>This is the Flexi-Time box: the day starts when the person arrived (7:00 - 9:00 AM).</summary>
        public bool Flexi;

        /// <summary>This is true when Late or Undertime was typed in by hand (then a reason is needed).</summary>
        public bool IsTyped
        {
            get { return LateMinutes.HasValue || UndertimeMinutes.HasValue; }
        }

        public bool IsEmpty
        {
            get { return !IsTyped && !Flexi; }
        }

        public DayAdjustment Clone()
        {
            return (DayAdjustment)MemberwiseClone();
        }
    }

    /// <summary>
    /// This is the list of all hand-typed corrections, kept in a small file so they are still there
    /// the next time the same month is opened. The .dat file itself is never changed.
    /// </summary>
    public sealed class CorrectionStore
    {
        // This is "normalized ID|yyyy-MM-dd" -> correction
        private readonly Dictionary<string, DayCorrection> _items =
            new Dictionary<string, DayCorrection>(StringComparer.OrdinalIgnoreCase);

        public static string DefaultPath
        {
            get { return Path.Combine(DtrSettings.DataFolder, "corrections.csv"); }
        }

        private static string Key(string employeeId, DateTime date)
        {
            return EmployeeDirectory.NormalizeId(employeeId) + "|" + date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        /// <summary>This is the correction for that day, or null when nothing was typed in for it.</summary>
        public DayCorrection? Get(string employeeId, DateTime date)
        {
            DayCorrection? correction;
            return _items.TryGetValue(Key(employeeId, date), out correction) ? correction : null;
        }

        /// <summary>This is where a correction is stored. Passing null removes it (the day goes back to the biometrics data).</summary>
        public void Set(string employeeId, DateTime date, DayCorrection? correction)
        {
            string key = Key(employeeId, date);
            if (correction == null) _items.Remove(key);
            else _items[key] = correction.Clone();
        }

        public int Count
        {
            get { return _items.Count; }
        }

        /// <summary>This is where the times and the Late / Undertime / Flexi-Time typed for one employee are removed (Employee Names, Delete).</summary>
        public int RemoveEmployee(string employeeId)
        {
            string prefix = EmployeeDirectory.NormalizeId(employeeId) + "|";
            int removed = 0;
            foreach (string key in _items.Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList())
                removed += _items.Remove(key) ? 1 : 0;
            foreach (string key in _adjustments.Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList())
                removed += _adjustments.Remove(key) ? 1 : 0;
            return removed;
        }

        // This is the part with the Late / Undertime typed by hand ---------------------------------------

        // This is "normalized ID|yyyy-MM-dd" -> Late / Undertime typed by hand
        private readonly Dictionary<string, DayAdjustment> _adjustments =
            new Dictionary<string, DayAdjustment>(StringComparer.OrdinalIgnoreCase);

        public static string AdjustmentsPath
        {
            get { return Path.Combine(DtrSettings.DataFolder, "adjustments.csv"); }
        }

        /// <summary>This is the Late / Undertime typed by hand for that day, or null.</summary>
        public DayAdjustment? GetAdjustment(string employeeId, DateTime date)
        {
            DayAdjustment? adjustment;
            return _adjustments.TryGetValue(Key(employeeId, date), out adjustment) ? adjustment : null;
        }

        /// <summary>This is where the Late / Undertime typed by hand is stored. null (or nothing typed) removes it.</summary>
        public void SetAdjustment(string employeeId, DateTime date, DayAdjustment? adjustment)
        {
            string key = Key(employeeId, date);
            if (adjustment == null || adjustment.IsEmpty) _adjustments.Remove(key);
            else _adjustments[key] = adjustment.Clone();
        }

        public int AdjustmentCount
        {
            get { return _adjustments.Count; }
        }

        // This is the file: one line per day:   ID,date,late minutes,undertime minutes,flexi,reason   (empty = the computed value)
        //                                    e.g. 070126005,2026-09-08,,0,,"Work suspended at 3:00 PM"
        //                                         070126005,2026-09-09,,,FLEXI,""
        // This is also how older files without the flexi column are read: there the reason (in "quotes") comes right after the minutes.

        public void LoadAdjustments(string path)
        {
            foreach (string line in File.ReadAllLines(path, Encoding.UTF8))
            {
                string[] cells = line.Split(new[] { ',' }, 5);
                if (cells.Length < 4) continue;

                DateTime date;
                if (!DateTime.TryParseExact(cells[1].Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                                            DateTimeStyles.None, out date))
                    continue; // This is the heading line, or a damaged line

                var adjustment = new DayAdjustment();
                adjustment.LateMinutes = ReadMinutes(cells[2]);
                adjustment.UndertimeMinutes = ReadMinutes(cells[3]);
                string rest = cells.Length > 4 ? cells[4] : "";
                if (rest.Trim().Length > 0 && !rest.TrimStart().StartsWith("\""))
                {
                    // This is the newer file: the flexi column, then the reason.
                    int comma = rest.IndexOf(',');
                    string flexi = comma < 0 ? rest : rest.Substring(0, comma);
                    adjustment.Flexi = flexi.Trim().Equals("FLEXI", StringComparison.OrdinalIgnoreCase);
                    rest = comma < 0 ? "" : rest.Substring(comma + 1);
                }
                adjustment.Reason = Unquote(rest);
                if (cells[0].Trim().Length > 0 && !adjustment.IsEmpty)
                    _adjustments[Key(cells[0], date)] = adjustment;
            }
        }

        public void SaveAdjustments(string path)
        {
            var sb = new StringBuilder();
            sb.AppendLine("ID,Date,Late (minutes),Undertime (minutes),Flexi-Time,Reason");
            foreach (var pair in _adjustments.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
            {
                string[] key = pair.Key.Split('|');
                DayAdjustment a = pair.Value;
                sb.AppendLine(key[0] + "," + key[1] + "," + WriteMinutes(a.LateMinutes) + "," + WriteMinutes(a.UndertimeMinutes) +
                              "," + (a.Flexi ? "FLEXI" : "") + ",\"" + a.Reason.Replace("\"", "\"\"") + "\"");
            }
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        }

        private static int? ReadMinutes(string text)
        {
            int value;
            if (int.TryParse(text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out value) && value <= 24 * 60)
                return value;
            return null;
        }

        private static string WriteMinutes(int? minutes)
        {
            return minutes.HasValue ? minutes.Value.ToString(CultureInfo.InvariantCulture) : "";
        }

        // This is the part that reads and saves the files -------------------------------------------
        // This is the file: one line per corrected day:   ID,date,AM in,AM out,PM in,PM out,remark
        //                                             e.g. 070126005,2026-09-08,06:55,12:03,12:50,18:05,

        public static CorrectionStore LoadDefault()
        {
            var store = new CorrectionStore();
            try
            {
                if (File.Exists(DefaultPath))
                    store.Load(DefaultPath);
                if (File.Exists(AdjustmentsPath))
                    store.LoadAdjustments(AdjustmentsPath);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            return store;
        }

        public void SaveDefault()
        {
            Save(DefaultPath);
            SaveAdjustments(AdjustmentsPath);
        }

        public void Load(string path)
        {
            foreach (string line in File.ReadAllLines(path, Encoding.UTF8))
            {
                string[] cells = line.Split(new[] { ',' }, 7);
                if (cells.Length < 7) continue;

                DateTime date;
                if (!DateTime.TryParseExact(cells[1].Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                                            DateTimeStyles.None, out date))
                    continue; // This is the heading line, or a damaged line

                var correction = new DayCorrection();
                correction.AmArrival = ReadTime(cells[2]);
                correction.AmDeparture = ReadTime(cells[3]);
                correction.PmArrival = ReadTime(cells[4]);
                correction.PmDeparture = ReadTime(cells[5]);
                correction.Remark = Unquote(cells[6]);
                if (cells[0].Trim().Length > 0)
                    _items[Key(cells[0], date)] = correction;
            }
        }

        public void Save(string path)
        {
            var sb = new StringBuilder();
            sb.AppendLine("ID,Date,AM Arrival,AM Departure,PM Arrival,PM Departure,Remark");
            foreach (var pair in _items.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
            {
                string[] key = pair.Key.Split('|');
                DayCorrection c = pair.Value;
                sb.AppendLine(key[0] + "," + key[1] + "," + WriteTime(c.AmArrival) + "," + WriteTime(c.AmDeparture) + "," +
                              WriteTime(c.PmArrival) + "," + WriteTime(c.PmDeparture) + ",\"" +
                              c.Remark.Replace("\"", "\"\"") + "\"");
            }
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        }

        /// <summary>This is where the "quotes" that Save puts around the remark are removed.</summary>
        private static string Unquote(string text)
        {
            text = text.Trim();
            if (text.Length >= 2 && text[0] == '"' && text[text.Length - 1] == '"')
                text = text.Substring(1, text.Length - 2).Replace("\"\"", "\"");
            return text;
        }

        private static TimeSpan? ReadTime(string text)
        {
            TimeSpan value;
            if (TimeSpan.TryParseExact(text.Trim(), @"hh\:mm", CultureInfo.InvariantCulture, out value))
                return value;
            return null;
        }

        private static string WriteTime(TimeSpan? time)
        {
            return time.HasValue ? time.Value.ToString(@"hh\:mm", CultureInfo.InvariantCulture) : "";
        }
    }

    /// <summary>This is how a time is understood the way a person types it: "7", "7:05", "705", "12:30 pm", "18:10".</summary>
    public static class TimeText
    {
        private static readonly Regex DurationPattern = new Regex(
            @"^(?:(\d{1,2}):(\d{2})|(?:(\d{1,2})\s*h(?:ours?|rs?)?)?\s*(?:(\d{1,4})\s*m(?:in(?:utes?)?)?)?|(\d{1,4}))$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>
        /// This is where a typed length of time for Late or Undertime is read: "45" (minutes), "1:30", "1h 30m", "2h", "0".
        /// An empty box gives null (= use what the program computed).
        /// </summary>
        public static bool TryParseDuration(string? text, out int? minutes)
        {
            minutes = null;
            string typed = (text ?? "").Trim();
            if (typed.Length == 0)
                return true;

            Match m = DurationPattern.Match(typed);
            if (!m.Success)
                return false;

            int total;
            if (m.Groups[1].Success)
            {
                int mm = int.Parse(m.Groups[2].Value);
                if (mm > 59) return false;
                total = int.Parse(m.Groups[1].Value) * 60 + mm;
            }
            else if (m.Groups[5].Success)
            {
                total = int.Parse(m.Groups[5].Value);
            }
            else
            {
                if (!m.Groups[3].Success && !m.Groups[4].Success) return false;
                total = (m.Groups[3].Success ? int.Parse(m.Groups[3].Value) * 60 : 0) +
                        (m.Groups[4].Success ? int.Parse(m.Groups[4].Value) : 0);
            }

            if (total > 24 * 60)
                return false;
            minutes = total;
            return true;
        }

        /// <summary>This is "0:45", "2:00" - how Late / Undertime is shown in the Edit DTR table ("" for none).</summary>
        public static string FormatDuration(int minutes)
        {
            if (minutes <= 0) return "";
            return (minutes / 60).ToString(CultureInfo.InvariantCulture) + ":" + (minutes % 60).ToString("00", CultureInfo.InvariantCulture);
        }

        private static readonly Regex Pattern = new Regex(
            @"^(\d{1,2})[:.]?(\d{2})?\s*([ap])?\.?m?\.?$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>
        /// This is where a typed time is read. An empty box is fine and gives "no time".
        /// column: 0 = A.M. Arrival, 1 = A.M. Departure, 2 = P.M. Arrival, 3 = P.M. Departure.
        /// The column is used to guess AM or PM when the person did not type it:
        /// "6:05" in P.M. Departure means 6:05 PM, "6:05" in A.M. Arrival means 6:05 AM.
        /// </summary>
        public static bool TryParse(string? text, int column, out TimeSpan? time)
        {
            time = null;
            string typed = (text ?? "").Trim();
            if (typed.Length == 0)
                return true;

            Match m = Pattern.Match(typed);
            if (!m.Success)
                return false;

            int hour = int.Parse(m.Groups[1].Value);
            int minute = m.Groups[2].Success ? int.Parse(m.Groups[2].Value) : 0;
            if (hour > 23 || minute > 59)
                return false;

            if (m.Groups[3].Success)
            {
                // This is when AM or PM was typed.
                if (hour < 1 || hour > 12) return false;
                bool pm = m.Groups[3].Value.ToLowerInvariant() == "p";
                if (hour == 12) hour = pm ? 12 : 0;
                else if (pm) hour += 12;
            }
            else if (hour >= 1 && hour <= 11)
            {
                // This is when it was not typed: guess from the column.
                bool afternoon;
                switch (column)
                {
                    case 0: afternoon = false; break;          // This is arriving in the morning
                    case 1: afternoon = hour <= 6; break;      // This is leaving for lunch: 1-6 means PM, 7-11 means AM
                    case 2: afternoon = hour <= 9; break;      // This is back from lunch: 10 and 11 mean AM
                    default: afternoon = true; break;          // This is going home
                }
                if (afternoon) hour += 12;
            }

            time = new TimeSpan(hour, minute, 0);
            return true;
        }
    }
}
