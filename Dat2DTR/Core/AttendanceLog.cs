using System.Text.RegularExpressions;

namespace Dat2DTR
{
    /// <summary>
    /// This is the reader of the .dat file exported by the biometrics device.
    ///
    /// Each line of the file looks like this (the gaps are TAB characters):
    ///
    ///     529000   2017-09-20 12:34:53   1   0   1   0
    ///     ^ID      ^date and time        ^the rest are device codes we do not need
    ///
    /// We only keep the ID and the date/time. The "in/out" code in the file is NOT
    /// used, because people often do not press the In/Out key on the device.
    ///
    /// A log can also hold the scans of SEVERAL files (all the files ever opened, see ScanArchive). Then it also
    /// knows which dates the files covered, so a day between two files is not marked ABSENT (see IsCovered).
    /// The same scan in two files (same ID and time) is kept once.
    /// </summary>
    public sealed class AttendanceLog
    {
        private static readonly Regex LinePattern = new Regex(
            @"^\s*(\S+)\s+(\d{4})[-/](\d{1,2})[-/](\d{1,2})[ T]+(\d{1,2}):(\d{2})(?::(\d{2}))?",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // This is ID -> every scan of that employee, sorted by time.
        private readonly Dictionary<string, List<DateTime>> _scans =
            new Dictionary<string, List<DateTime>>(StringComparer.OrdinalIgnoreCase);

        // This is every line of the file that was understood as a scan, exactly as it was written,
        // so a part of the file can be saved again as a .dat file (see SaveDat).
        private readonly List<KeyValuePair<DateTime, KeyValuePair<string, string>>> _lines =
            new List<KeyValuePair<DateTime, KeyValuePair<string, string>>>();   // This is time -> (ID, line)

        public string FilePath { get; private set; } = "";

        // This is "ID|time" of every scan, so the same scan from two files is kept once.
        private readonly HashSet<string> _seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // This is the dates the files covered (from the first to the last scan of each file), joined where they touch.
        private readonly List<KeyValuePair<DateTime, DateTime>> _covered = new List<KeyValuePair<DateTime, DateTime>>();

        /// <summary>This is how many lines were understood as a scan.</summary>
        public int RecordCount { get; private set; }

        /// <summary>This is how many non-empty lines could not be understood.</summary>
        public int SkippedLineCount { get; private set; }

        /// <summary>This is the date of the earliest scan in the file.</summary>
        public DateTime FirstDate { get; private set; }

        /// <summary>This is the date of the latest scan in the file.</summary>
        public DateTime LastDate { get; private set; }

        public int EmployeeCount
        {
            get { return _scans.Count; }
        }

        public static AttendanceLog Load(string path)
        {
            var log = new AttendanceLog();
            log.FilePath = path;
            log.ReadFile(path);
            foreach (var list in log._scans.Values)
                list.Sort();
            return log;
        }

        /// <summary>
        /// This is a log of several files together (the month files kept by ScanArchive), with the dates they covered.
        /// The same scan in two files is kept once.
        /// </summary>
        public static AttendanceLog LoadMany(IEnumerable<string> paths, IEnumerable<KeyValuePair<DateTime, DateTime>> covered)
        {
            var log = new AttendanceLog();
            foreach (string path in paths)
                log.ReadFile(path);
            foreach (var list in log._scans.Values)
                list.Sort();
            foreach (var range in covered)
                log.AddCovered(range.Key, range.Value);
            return log;
        }

        /// <summary>This is where the lines of one file are added to the log.</summary>
        private void ReadFile(string path)
        {
            AttendanceLog log = this;

            // This is FileShare.ReadWrite: it lets us read the file even if another program has it open.
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
        }

        /// <summary>This is a range of dates a file covered (from its first to its last scan).</summary>
        public void AddCovered(DateTime from, DateTime to)
        {
            DateTime a = from.Date, b = to.Date;
            if (b < a) return;
            // This is where ranges that overlap or touch are joined into one.
            var all = new List<KeyValuePair<DateTime, DateTime>>(_covered) { new KeyValuePair<DateTime, DateTime>(a, b) };
            all.Sort((x, y) => x.Key.CompareTo(y.Key));
            _covered.Clear();
            foreach (var range in all)
            {
                int last = _covered.Count - 1;
                if (last >= 0 && range.Key <= _covered[last].Value.AddDays(1))
                {
                    if (range.Value > _covered[last].Value)
                        _covered[last] = new KeyValuePair<DateTime, DateTime>(_covered[last].Key, range.Value);
                }
                else
                {
                    _covered.Add(range);
                }
            }
        }

        /// <summary>This is the ranges of dates the files covered, joined where they touch, oldest first.</summary>
        public List<KeyValuePair<DateTime, DateTime>> GetCovered()
        {
            if (_covered.Count == 0 && RecordCount > 0)
                return new List<KeyValuePair<DateTime, DateTime>> { new KeyValuePair<DateTime, DateTime>(FirstDate, LastDate) };
            return new List<KeyValuePair<DateTime, DateTime>>(_covered);
        }

        /// <summary>
        /// This is true when a file covered this date (it was between the first and the last scan of a file). Only such
        /// a day can be ABSENT: a day no file covered is unknown, so it stays blank.
        /// </summary>
        public bool IsCovered(DateTime date)
        {
            DateTime d = date.Date;
            return GetCovered().Any(r => d >= r.Key && d <= r.Value);
        }

        /// <summary>
        /// This is where the data of a month starts: the first date of the covered range that reaches into the month
        /// (it may start in the month before). Used to tell whether the whole month is there.
        /// </summary>
        public DateTime CoveredFrom(DateTime month)
        {
            var ranges = RangesOf(month);
            return ranges.Count > 0 ? ranges.Min(r => r.Key) : FirstDate;
        }

        /// <summary>This is where the data of a month ends: the last date of the covered range that reaches into the month.</summary>
        public DateTime CoveredThrough(DateTime month)
        {
            var ranges = RangesOf(month);
            return ranges.Count > 0 ? ranges.Max(r => r.Value) : LastDate;
        }

        private List<KeyValuePair<DateTime, DateTime>> RangesOf(DateTime month)
        {
            DateTime start = new DateTime(month.Year, month.Month, 1), end = start.AddMonths(1).AddDays(-1);
            return GetCovered().Where(r => r.Key <= end && r.Value >= start).ToList();
        }

        /// <summary>This is every scan line of one month, exactly as it was in the file, oldest first (for ScanArchive).</summary>
        public List<string> LinesOf(DateTime month)
        {
            return _lines.Where(l => l.Key.Year == month.Year && l.Key.Month == month.Month)
                         .OrderBy(l => l.Key)
                         .Select(l => l.Value.Value.TrimEnd('\r', '\n'))
                         .ToList();
        }

        /// <summary>This is where a log is built from scans that are already in memory (used for testing).</summary>
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
                return false; // This is for impossible dates, for example month 13 or hour 25
            }

            id = m.Groups[1].Value;
            return true;
        }

        /// <summary>This is a line the way the biometrics device writes it:  ID, date and time, then 1 0 1 0.</summary>
        public static string MakeLine(string id, DateTime time)
        {
            return id + "\t" + time.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture) +
                   "\t1\t0\t1\t0";
        }

        /// <summary>
        /// This is where the scans of some employees in one month are saved as a new .dat file, in time order.
        /// The lines are copied exactly as they were in the opened file. Times typed in by hand
        /// and leaves are NOT written, because they are not scans of the device.
        /// Returns how many lines were written.
        /// </summary>
        public int SaveDat(string path, IEnumerable<string> employeeIds, DateTime month)
        {
            DateTime first = new DateTime(month.Year, month.Month, 1);
            return SaveDat(path, employeeIds, first, first.AddMonths(1).AddDays(-1));
        }

        /// <summary>This is the same for the months from "from" to "to" (a whole year, or every month kept), in time order.</summary>
        public int SaveDat(string path, IEnumerable<string> employeeIds, DateTime from, DateTime to)
        {
            var wanted = new HashSet<string>(employeeIds, StringComparer.OrdinalIgnoreCase);
            var sb = new System.Text.StringBuilder();
            int count = 0;

            foreach (var entry in _lines.OrderBy(l => l.Key))
            {
                if (entry.Key.Date < from.Date || entry.Key.Date > to.Date) continue;
                if (!wanted.Contains(entry.Value.Key)) continue;
                sb.Append(entry.Value.Value.TrimEnd('\r', '\n')).Append("\r\n");
                count++;
            }

            File.WriteAllText(path, sb.ToString(), new System.Text.UTF8Encoding(false));
            return count;
        }

        private void Add(string id, DateTime time, string line)
        {
            if (!_seen.Add(id + "|" + time.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)))
                return;   // This is the same scan again (it was in another file too)
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

        /// <summary>This is every month that has at least one scan, newest first (as the 1st day of the month).</summary>
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
        /// This is the month the user most likely wants. The file is usually exported a few days
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

        /// <summary>This is the list of IDs of everyone who scanned at least once in the given month.</summary>
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

        /// <summary>This is all the scans of one employee on one date, earliest first.</summary>
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

        /// <summary>This is how IDs are sorted: like numbers (93 before 135) when they are numbers.</summary>
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
