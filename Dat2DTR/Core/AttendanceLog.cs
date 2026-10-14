using System.Text.RegularExpressions;

namespace Dat2DTR
{
    /// <summary>
    /// Reads the .dat file exported by the biometrics device.
    ///
    /// Each line of the file looks like this (the gaps are TAB characters):
    ///
    ///     529000   2017-09-20 12:34:53   1   0   1   0
    ///     ^ID      ^date and time        ^the rest are device codes we do not need
    ///
    /// We only keep the ID and the date/time. The "in/out" code in the file is NOT
    /// used, because people often do not press the In/Out key on the device.
    /// </summary>
    public sealed class AttendanceLog
    {
        private static readonly Regex LinePattern = new Regex(
            @"^\s*(\S+)\s+(\d{4})[-/](\d{1,2})[-/](\d{1,2})[ T]+(\d{1,2}):(\d{2})(?::(\d{2}))?",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // ID -> every scan of that employee, sorted by time.
        private readonly Dictionary<string, List<DateTime>> _scans =
            new Dictionary<string, List<DateTime>>(StringComparer.OrdinalIgnoreCase);

        // Every line of the file that was understood as a scan, exactly as it was written,
        // so a part of the file can be saved again as a .dat file (see SaveDat).
        private readonly List<KeyValuePair<DateTime, KeyValuePair<string, string>>> _lines =
            new List<KeyValuePair<DateTime, KeyValuePair<string, string>>>();   // time -> (ID, line)

        public string FilePath { get; private set; } = "";

        /// <summary>How many lines were understood as a scan.</summary>
        public int RecordCount { get; private set; }

        /// <summary>How many non-empty lines could not be understood.</summary>
        public int SkippedLineCount { get; private set; }

        /// <summary>Date of the earliest scan in the file.</summary>
        public DateTime FirstDate { get; private set; }

        /// <summary>Date of the latest scan in the file.</summary>
        public DateTime LastDate { get; private set; }

        public int EmployeeCount
        {
            get { return _scans.Count; }
        }

        public static AttendanceLog Load(string path)
        {
            var log = new AttendanceLog();
            log.FilePath = path;

            // FileShare.ReadWrite lets us read the file even if another program has it open.
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(stream))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Trim().Length == 0)
                        continue;

                    DateTime time;
                    string id;
                    if (TryParseLine(line, out id, out time))
                        log.Add(id, time, line);
                    else
                        log.SkippedLineCount++;
                }
            }

            foreach (var list in log._scans.Values)
                list.Sort();

            return log;
        }

        /// <summary>Builds a log from scans that are already in memory (used for testing).</summary>
        public static AttendanceLog FromScans(IEnumerable<KeyValuePair<string, DateTime>> scans)
        {
            var log = new AttendanceLog();
            foreach (var scan in scans)
                log.Add(scan.Key, scan.Value, MakeLine(scan.Key, scan.Value));
            foreach (var list in log._scans.Values)
                list.Sort();
            return log;
        }

        private static bool TryParseLine(string line, out string id, out DateTime time)
        {
            id = "";
            time = DateTime.MinValue;

            Match m = LinePattern.Match(line);
            if (!m.Success)
                return false;

            try
            {
                int second = m.Groups[7].Success ? int.Parse(m.Groups[7].Value) : 0;
                time = new DateTime(
                    int.Parse(m.Groups[2].Value), int.Parse(m.Groups[3].Value), int.Parse(m.Groups[4].Value),
                    int.Parse(m.Groups[5].Value), int.Parse(m.Groups[6].Value), second);
            }
            catch (ArgumentOutOfRangeException)
            {
                return false; // for example month 13 or hour 25
            }

            id = m.Groups[1].Value;
            return true;
        }

        /// <summary>A line the way the biometrics device writes it:  ID, date and time, then 1 0 1 0.</summary>
        public static string MakeLine(string id, DateTime time)
        {
            return id + "\t" + time.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture) +
                   "\t1\t0\t1\t0";
        }

        /// <summary>
        /// Saves the scans of some employees in one month as a new .dat file, in time order.
        /// The lines are copied exactly as they were in the opened file. Times typed in by hand
        /// and leaves are NOT written, because they are not scans of the device.
        /// Returns how many lines were written.
        /// </summary>
        public int SaveDat(string path, IEnumerable<string> employeeIds, DateTime month)
        {
            var wanted = new HashSet<string>(employeeIds, StringComparer.OrdinalIgnoreCase);
            var sb = new System.Text.StringBuilder();
            int count = 0;

            foreach (var entry in _lines.OrderBy(l => l.Key))
            {
                if (entry.Key.Year != month.Year || entry.Key.Month != month.Month) continue;
                if (!wanted.Contains(entry.Value.Key)) continue;
                sb.Append(entry.Value.Value.TrimEnd('\r', '\n')).Append("\r\n");
                count++;
            }

            File.WriteAllText(path, sb.ToString(), new System.Text.UTF8Encoding(false));
            return count;
        }

        private void Add(string id, DateTime time, string line)
        {
            _lines.Add(new KeyValuePair<DateTime, KeyValuePair<string, string>>(time, new KeyValuePair<string, string>(id, line)));

            List<DateTime>? list;
            if (!_scans.TryGetValue(id, out list))
            {
                list = new List<DateTime>();
                _scans[id] = list;
            }
            list.Add(time);

            if (RecordCount == 0 || time.Date < FirstDate) FirstDate = time.Date;
            if (RecordCount == 0 || time.Date > LastDate) LastDate = time.Date;
            RecordCount++;
        }

        /// <summary>Every month that has at least one scan, newest first (as the 1st day of the month).</summary>
        public List<DateTime> GetMonths()
        {
            var months = new HashSet<DateTime>();
            foreach (var list in _scans.Values)
                foreach (DateTime t in list)
                    months.Add(new DateTime(t.Year, t.Month, 1));

            var result = months.ToList();
            result.Sort();
            result.Reverse();
            return result;
        }

        /// <summary>
        /// The month the user most likely wants. The file is usually exported a few days
        /// AFTER the month ends, so if the newest month only has its first week in it,
        /// we suggest the month before.
        /// </summary>
        public DateTime SuggestMonth()
        {
            List<DateTime> months = GetMonths();
            if (months.Count == 0)
                return new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            if (months.Count > 1 && LastDate.Day <= 7)
                return months[1];

            return months[0];
        }

        /// <summary>IDs of everyone who scanned at least once in the given month.</summary>
        public List<string> GetEmployeeIds(DateTime month)
        {
            var ids = new List<string>();
            foreach (var pair in _scans)
            {
                foreach (DateTime t in pair.Value)
                {
                    if (t.Year == month.Year && t.Month == month.Month)
                    {
                        ids.Add(pair.Key);
                        break;
                    }
                }
            }
            ids.Sort(CompareIds);
            return ids;
        }

        public List<string> GetAllEmployeeIds()
        {
            var ids = _scans.Keys.ToList();
            ids.Sort(CompareIds);
            return ids;
        }

        /// <summary>All scans of one employee on one date, earliest first.</summary>
        public List<DateTime> GetScans(string employeeId, DateTime date)
        {
            var result = new List<DateTime>();
            List<DateTime>? list;
            if (_scans.TryGetValue(employeeId, out list))
            {
                foreach (DateTime t in list)
                    if (t.Date == date.Date)
                        result.Add(t);
            }
            return result;
        }

        /// <summary>Sorts IDs like numbers (93 before 135) when they are numbers.</summary>
        public static int CompareIds(string a, string b)
        {
            long x, y;
            bool ax = long.TryParse(a, out x);
            bool by = long.TryParse(b, out y);
            if (ax && by && x != y) return x.CompareTo(y);
            if (ax != by) return ax ? -1 : 1;
            return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }
}
