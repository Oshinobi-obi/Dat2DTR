using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Dat2DTR
{
    /// <summary>
    /// This is where the scans of every .dat file ever opened are kept on this computer, so a month stays in the
    /// "Choose the month" list after a file of another month is opened (the August file, then the September file:
    /// both months are there). The scans are kept in %AppData%\Dat2DTR\scans, one file per month
    /// (2026-08.dat, 2026-09.dat...) in the same layout as the device writes, plus covered.csv: the dates each
    /// opened file covered (from its first to its last scan), so a day that no file covered is never ABSENT.
    ///
    /// Opening a file ADDS its scans: a scan that is already kept (same ID and time) is not added twice, and
    /// nothing is ever removed by opening a file. The .dat files themselves are never changed.
    /// </summary>
    public static class ScanArchive
    {
        /// <summary>This is the name of the folder inside the data folder.</summary>
        public const string FolderName = "scans";

        /// <summary>This is the file with the dates the opened files covered.</summary>
        public const string CoveredName = "covered.csv";

        private static readonly Regex MonthFile = new Regex(@"^\d{4}-\d{2}\.dat$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        public static string DefaultFolder
        {
            get { return Path.Combine(DtrSettings.DataFolder, FolderName); }
        }

        /// <summary>This is true for a file that belongs to the kept scans, e.g. "scans/2026-08.dat" or "scans/covered.csv" (for the backup).</summary>
        public static bool IsArchiveEntry(string name)
        {
            string n = name.Replace('\\', '/');
            if (!n.StartsWith(FolderName + "/", StringComparison.OrdinalIgnoreCase)) return false;
            string file = n.Substring(FolderName.Length + 1);
            return MonthFile.IsMatch(file) || string.Equals(file, CoveredName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>This is every kept file, as "scans/2026-08.dat" names (relative to the data folder).</summary>
        public static List<string> EntriesIn(string dataFolder)
        {
            var list = new List<string>();
            string folder = Path.Combine(dataFolder, FolderName);
            if (!Directory.Exists(folder)) return list;
            foreach (string path in Directory.GetFiles(folder))
            {
                string entry = FolderName + "/" + Path.GetFileName(path);
                if (IsArchiveEntry(entry)) list.Add(entry);
            }
            list.Sort(StringComparer.OrdinalIgnoreCase);
            return list;
        }

        /// <summary>This is the kept scans of every month together, or null when nothing is kept yet.</summary>
        public static AttendanceLog? Load()
        {
            return Load(DefaultFolder);
        }

        public static AttendanceLog? Load(string folder)
        {
            if (!Directory.Exists(folder)) return null;
            List<string> files = Directory.GetFiles(folder)
                .Where(p => MonthFile.IsMatch(Path.GetFileName(p)))
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();
            if (files.Count == 0) return null;
            AttendanceLog log = AttendanceLog.LoadMany(files, ReadCovered(folder));
            return log.RecordCount > 0 ? log : null;
        }

        /// <summary>
        /// This is where the scans of an opened file are added to the kept ones (month by month), with the dates the
        /// file covered. Returns all the kept scans together (the months of before and the new ones).
        /// </summary>
        public static AttendanceLog Add(AttendanceLog file)
        {
            return Add(file, DefaultFolder);
        }

        public static AttendanceLog Add(AttendanceLog file, string folder)
        {
            Directory.CreateDirectory(folder);
            foreach (DateTime month in file.GetMonths())
            {
                string path = Path.Combine(folder, month.ToString("yyyy-MM", CultureInfo.InvariantCulture) + ".dat");
                var lines = new List<string>();
                if (File.Exists(path))
                    lines.AddRange(File.ReadAllLines(path, Encoding.UTF8).Where(l => l.Trim().Length > 0));
                lines.AddRange(file.LinesOf(month));

                // This is the month again, with every scan once, in time order (the log removes the same scan twice).
                string temp = path + ".part";
                File.WriteAllLines(temp, lines, new UTF8Encoding(false));
                AttendanceLog merged = AttendanceLog.Load(temp);
                File.WriteAllLines(temp, merged.LinesOf(month), new UTF8Encoding(false));
                if (File.Exists(path)) File.Delete(path);
                File.Move(temp, path);
            }

            var covered = ReadCovered(folder);
            covered.Add(new KeyValuePair<DateTime, DateTime>(file.FirstDate, file.LastDate));
            var joined = AttendanceLog.LoadMany(new string[0], covered).GetCovered();
            var sb = new StringBuilder();
            sb.AppendLine("From,To");
            foreach (var range in joined)
                sb.AppendLine(range.Key.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "," +
                              range.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            File.WriteAllText(Path.Combine(folder, CoveredName), sb.ToString(), new UTF8Encoding(false));

            return Load(folder) ?? file;
        }

        /// <summary>
        /// This is where every kept scan of these employees is removed (Employee Names, Delete), from every month file.
        /// A month file left with no scans is removed. Returns how many scans were removed.
        /// </summary>
        public static int RemoveEmployees(IEnumerable<string> employeeIds, string folder)
        {
            var gone = new HashSet<string>(employeeIds.Select(EmployeeDirectory.NormalizeId), StringComparer.OrdinalIgnoreCase);
            if (gone.Count == 0 || !Directory.Exists(folder)) return 0;
            int removed = 0;
            foreach (string path in Directory.GetFiles(folder).Where(p => MonthFile.IsMatch(Path.GetFileName(p))))
            {
                var kept = new List<string>();
                foreach (string line in File.ReadAllLines(path, Encoding.UTF8))
                {
                    string[] parts = line.Trim().Split(new[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0 && gone.Contains(EmployeeDirectory.NormalizeId(parts[0]))) { removed++; continue; }
                    if (line.Trim().Length > 0) kept.Add(line);
                }
                if (kept.Count == 0) File.Delete(path);
                else File.WriteAllLines(path, kept, new UTF8Encoding(false));
            }
            return removed;
        }

        /// <summary>This is the ranges of dates in covered.csv (empty when there is none).</summary>
        private static List<KeyValuePair<DateTime, DateTime>> ReadCovered(string folder)
        {
            var list = new List<KeyValuePair<DateTime, DateTime>>();
            string path = Path.Combine(folder, CoveredName);
            if (!File.Exists(path)) return list;
            foreach (string line in File.ReadAllLines(path, Encoding.UTF8))
            {
                string[] cells = line.Split(',');
                DateTime from, to;
                if (cells.Length >= 2 &&
                    DateTime.TryParseExact(cells[0].Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out from) &&
                    DateTime.TryParseExact(cells[1].Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out to))
                    list.Add(new KeyValuePair<DateTime, DateTime>(from, to));
            }
            return list;
        }
    }
}
