using System.Text;

namespace Dat2DTR
{
    /// <summary>This is one line of the employee list.</summary>
    public sealed class EmployeeEntry
    {
        /// <summary>This is the biometric ID exactly as it was typed (keeps leading zeros).</summary>
        public string Id = "";

        public string Name = "";

        /// <summary>This is which work schedule the person follows: 0 = Schedule A, 1 = B, 2 = C, 3 = D.</summary>
        public int Schedule;

        /// <summary>This is true when an imported file said which schedule (so "A" is a real choice, not just a blank).</summary>
        public bool ScheduleGiven;
    }

    /// <summary>
    /// This is the list that says which biometric ID belongs to which person, and which
    /// work schedule that person follows. The .dat file only contains ID numbers,
    /// so this is kept here.
    ///
    /// It is saved as a simple three-column file (ID, Name, Schedule) that also opens in Excel.
    /// </summary>
    public sealed class EmployeeDirectory
    {
        // This is normalized ID -> entry
        private readonly Dictionary<string, EmployeeEntry> _entries =
            new Dictionary<string, EmployeeEntry>(StringComparer.OrdinalIgnoreCase);

        public static string DefaultPath
        {
            get { return Path.Combine(DtrSettings.DataFolder, "employees.csv"); }
        }

        /// <summary>
        /// This is why "070126010" and "70126010" are treated as the same ID: Excel
        /// removes leading zeros when someone edits the list there.
        /// </summary>
        public static string NormalizeId(string id)
        {
            string trimmed = id.Trim().TrimStart('0');
            return trimmed.Length == 0 ? "0" : trimmed;
        }

        /// <summary>This is true when two IDs mean the same person (leading zeros are ignored).</summary>
        public static bool SameId(string a, string b)
        {
            return string.Equals(NormalizeId(a), NormalizeId(b), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>This is the name for an ID, or "" when no name has been typed in yet.</summary>
        public string GetName(string id)
        {
            EmployeeEntry? entry;
            return _entries.TryGetValue(NormalizeId(id), out entry) ? entry.Name : "";
        }

        /// <summary>This is the work schedule for an ID: 0 = Schedule A (also when nothing was chosen), 1 = B, 2 = C, 3 = D.</summary>
        public int GetSchedule(string id)
        {
            EmployeeEntry? entry;
            return _entries.TryGetValue(NormalizeId(id), out entry) && entry.Schedule >= 0 && entry.Schedule < WorkSchedule.Letters.Length ? entry.Schedule : 0;
        }

        public void SetName(string id, string name)
        {
            if (id.Trim().Length == 0)
                return;

            EmployeeEntry entry = GetOrAdd(id);
            entry.Name = name.Trim();
            RemoveIfEmpty(id);
        }

        public void SetSchedule(string id, int schedule)
        {
            if (id.Trim().Length == 0)
                return;

            EmployeeEntry entry = GetOrAdd(id);
            entry.Schedule = schedule >= 0 && schedule < WorkSchedule.Letters.Length ? schedule : 0;
            RemoveIfEmpty(id);
        }

        private EmployeeEntry GetOrAdd(string id)
        {
            string key = NormalizeId(id);
            EmployeeEntry? entry;
            if (!_entries.TryGetValue(key, out entry))
            {
                entry = new EmployeeEntry();
                entry.Id = id.Trim();
                _entries[key] = entry;
            }
            return entry;
        }

        /// <summary>This is where an entry with no name and the default schedule is dropped: it says nothing, so it is not kept.</summary>
        private void RemoveIfEmpty(string id)
        {
            string key = NormalizeId(id);
            EmployeeEntry? entry;
            if (_entries.TryGetValue(key, out entry) && entry.Name.Length == 0 && entry.Schedule == 0)
                _entries.Remove(key);
        }

        public void Clear()
        {
            _entries.Clear();
        }

        /// <summary>This is every entry, sorted by ID.</summary>
        public List<EmployeeEntry> GetAll()
        {
            List<EmployeeEntry> list = _entries.Values.ToList();
            list.Sort((a, b) => AttendanceLog.CompareIds(a.Id, b.Id));
            return list;
        }

        // This is the part that reads and saves the files -------------------------------------------

        public static EmployeeDirectory LoadDefault()
        {
            var directory = new EmployeeDirectory();
            try
            {
                if (File.Exists(DefaultPath))
                    directory.Import(DefaultPath);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            return directory;
        }

        public void SaveDefault()
        {
            Export(DefaultPath);
        }

        /// <summary>
        /// This is where the employees found in a file are added to this list. Returns how many were read.
        /// The file has the ID, then the name, then (optional) the schedule letter (A, B, C or D).
        /// </summary>
        public int Import(string path)
        {
            int count = 0;
            bool first = true;

            foreach (string line in File.ReadAllLines(path, Encoding.UTF8))
            {
                if (line.Trim().Length == 0)
                    continue;

                List<string> cells = SplitLine(line);
                if (cells.Count < 2)
                    continue;

                string id = cells[0].Trim();
                string name = cells[1].Trim();
                int schedule = cells.Count > 2 ? ParseSchedule(cells[2]) : -1; // This is -1 when the file does not say

                // This is where a heading row such as "ID,Name,Schedule" is skipped.
                if (first)
                {
                    first = false;
                    if (id.StartsWith("id", StringComparison.OrdinalIgnoreCase) ||
                        id.IndexOf("biometric", StringComparison.OrdinalIgnoreCase) >= 0)
                        continue;
                }

                if (id.Length == 0 || (name.Length == 0 && schedule < 0))
                    continue;

                EmployeeEntry entry = GetOrAdd(id);
                if (name.Length > 0)
                    entry.Name = name;
                if (schedule >= 0)
                {
                    entry.Schedule = schedule;
                    entry.ScheduleGiven = true;
                }
                RemoveIfEmpty(id);
                count++;
            }
            return count;
        }

        public void Export(string path)
        {
            var sb = new StringBuilder();
            sb.AppendLine("ID,Name,Schedule");
            foreach (EmployeeEntry entry in GetAll())
                sb.AppendLine(Quote(entry.Id) + "," + Quote(entry.Name) + "," + WorkSchedule.Letters[entry.Schedule].ToString());

            // This is UTF-8 with a "BOM", so Excel shows accented letters (like the Spanish N in "PENA") correctly.
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        }

        /// <summary>This is "A" to "D" or "1" to "4" (any letter case) turned into 0 to 3, or -1 when it is something else.</summary>
        private static int ParseSchedule(string text)
        {
            // This is "A" to "D", "1" to "4", or "Schedule C".
            string value = text.Trim().ToUpperInvariant();
            if (value.StartsWith("SCHEDULE ", StringComparison.Ordinal)) value = value.Substring(9).Trim();
            if (value.Length != 1) return -1;
            int letter = WorkSchedule.Letters.IndexOf(value[0]);
            if (letter >= 0) return letter;
            int number = value[0] - '1';
            return number >= 0 && number < WorkSchedule.Letters.Length ? number : -1;
        }

        private static string Quote(string value)
        {
            if (value.IndexOfAny(new[] { ',', '"', ';', '\t' }) < 0)
                return value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        /// <summary>
        /// This is where one line is split into cells. Works with commas, semicolons or tabs, and
        /// understands "quoted, names" that contain a comma.
        /// </summary>
        internal static List<string> SplitLine(string line)
        {
            char separator = ',';
            if (line.IndexOf('\t') >= 0) separator = '\t';
            else if (line.IndexOf(',') < 0 && line.IndexOf(';') >= 0) separator = ';';

            var cells = new List<string>();
            var current = new StringBuilder();
            bool quoted = false;
            bool nameWasQuoted = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (quoted)
                {
                    if (c == '"' && i + 1 < line.Length && line[i + 1] == '"') { current.Append('"'); i++; }
                    else if (c == '"') quoted = false;
                    else current.Append(c);
                }
                else if (c == '"' && current.Length == 0)
                {
                    quoted = true;
                    if (cells.Count == 1) nameWasQuoted = true;
                }
                else if (c == separator)
                {
                    cells.Add(current.ToString());
                    current.Length = 0;
                }
                else
                {
                    current.Append(c);
                }
            }
            cells.Add(current.ToString());

            // This is "135,DELA CRUZ, JUAN M" typed without quotes: glue the name back together.
            // If the last cell is a schedule ("135,DELA CRUZ, JUAN M,B") it stays separate.
            if (separator == ',' && cells.Count > 2 && !nameWasQuoted)
            {
                string last = cells[cells.Count - 1].Trim().ToUpperInvariant();
                bool endsWithSchedule = ParseSchedule(last) >= 0;   // This is "B", "4", "Schedule C"...

                int nameCells = cells.Count - 1 - (endsWithSchedule ? 1 : 0);
                string name = string.Join(",", cells.Skip(1).Take(nameCells).ToArray());

                var glued = new List<string> { cells[0], name };
                if (endsWithSchedule) glued.Add(last);
                cells = glued;
            }
            return cells;
        }
    }
}
