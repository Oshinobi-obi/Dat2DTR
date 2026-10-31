using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace Dat2DTR
{
    /// <summary>
    /// This is the backup of everything Dat2DTR keeps on this computer: the small files in the data folder
    /// (%AppData%\Dat2DTR) - settings, employee names, times typed in, leaves, balances, CTO, Solo Parent
    /// Leave and holidays - and the scans of every .dat file opened (the "scans" folder, see ScanArchive).
    /// A backup is one .zip file. Restoring puts those files back, so another computer
    /// (or this one after it was reformatted) has the same leave history.
    /// </summary>
    public static class DataBackup
    {
        /// <summary>This is the list of files that belong to a backup (only these are saved and restored).</summary>
        public static readonly string[] FileNames =
        {
            "settings.ini", "employees.csv", "corrections.csv", "adjustments.csv", "leaves.csv",
            "leave-ledger.csv", "cto.csv", "solo-parent.csv", "holidays.csv"
        };

        /// <summary>This is the small note inside every backup, so a restore can tell it is a Dat2DTR backup.</summary>
        public const string InfoName = "dat2dtr-backup.txt";

        /// <summary>This is the file name offered for a backup made on that day, e.g. "Dat2DTR-backup-2026-10-27.zip".</summary>
        public static string SuggestedName(DateTime day)
        {
            return "Dat2DTR-backup-" + day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ".zip";
        }

        /// <summary>
        /// This is where a backup is made: every file of the data folder that exists goes into the .zip.
        /// Returns how many files were saved. The files are read even when another program has them open.
        /// </summary>
        public static int Create(string zipPath)
        {
            return Create(zipPath, DtrSettings.DataFolder, DateTime.Now);
        }

        /// <summary>This is the same, for any folder (used by the self test).</summary>
        public static int Create(string zipPath, string folder, DateTime made)
        {
            int count = 0;
            string temp = zipPath + ".part";
            using (var file = new FileStream(temp, FileMode.Create, FileAccess.Write))
            using (var zip = new ZipArchive(file, ZipArchiveMode.Create))
            {
                foreach (string name in FileNames.Concat(ScanArchive.EntriesIn(folder)))
                {
                    string path = Path.Combine(folder, name.Replace('/', Path.DirectorySeparatorChar));
                    if (!File.Exists(path))
                        continue;
                    ZipArchiveEntry entry = zip.CreateEntry(name, CompressionLevel.Optimal);
                    using (var source = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (Stream target = entry.Open())
                        source.CopyTo(target);
                    count++;
                }

                ZipArchiveEntry info = zip.CreateEntry(InfoName, CompressionLevel.Optimal);
                using (var writer = new StreamWriter(info.Open(), new UTF8Encoding(false)))
                {
                    writer.WriteLine("Dat2DTR backup");
                    writer.WriteLine("Made=" + made.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
                    writer.WriteLine("Computer=" + Environment.MachineName);
                    writer.WriteLine("Files=" + count);
                }
            }

            // This is so a half-written backup never replaces a good one with the same name.
            if (File.Exists(zipPath))
                File.Delete(zipPath);
            File.Move(temp, zipPath);
            return count;
        }

        /// <summary>This is what a backup holds: its files, and when and where it was made (empty when not known).</summary>
        public sealed class Contents
        {
            public List<string> Files = new List<string>();
            public string Made = "";
            public string Computer = "";

            /// <summary>This is true when the .zip holds at least one Dat2DTR file.</summary>
            public bool IsBackup
            {
                get { return Files.Count > 0; }
            }
        }

        /// <summary>This is where a .zip is looked into, to see whether it is a Dat2DTR backup and what it holds.</summary>
        public static Contents Read(string zipPath)
        {
            var result = new Contents();
            using (var file = new FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var zip = new ZipArchive(file, ZipArchiveMode.Read))
            {
                foreach (ZipArchiveEntry entry in zip.Entries)
                {
                    if (IsKnown(entry.FullName))
                        result.Files.Add(entry.FullName);
                    else if (string.Equals(entry.FullName, InfoName, StringComparison.OrdinalIgnoreCase))
                    {
                        using (var reader = new StreamReader(entry.Open(), Encoding.UTF8))
                        {
                            string? line;
                            while ((line = reader.ReadLine()) != null)
                            {
                                if (line.StartsWith("Made=", StringComparison.Ordinal)) result.Made = line.Substring(5).Trim();
                                if (line.StartsWith("Computer=", StringComparison.Ordinal)) result.Computer = line.Substring(9).Trim();
                            }
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// This is the restore: the files of the data folder are made exactly the same as in the backup.
        /// A Dat2DTR file that is not in the backup is removed (the backup did not have it). Only the
        /// known files are touched. Returns how many files were put back.
        /// </summary>
        public static int Restore(string zipPath)
        {
            return Restore(zipPath, DtrSettings.DataFolder);
        }

        /// <summary>This is the same, for any folder (used by the self test).</summary>
        public static int Restore(string zipPath, string folder)
        {
            // This is the check first: nothing is changed when the .zip is not a Dat2DTR backup.
            Contents contents = Read(zipPath);
            if (!contents.IsBackup)
                throw new InvalidDataException("This file is not a Dat2DTR backup.");

            // This is where every file is unpacked next to its place first, so a broken .zip changes nothing.
            var unpacked = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using (var file = new FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var zip = new ZipArchive(file, ZipArchiveMode.Read))
                {
                    foreach (ZipArchiveEntry entry in zip.Entries)
                    {
                        string? name = FileNames.FirstOrDefault(n => string.Equals(n, entry.FullName, StringComparison.OrdinalIgnoreCase));
                        if (name == null && ScanArchive.IsArchiveEntry(entry.FullName))
                        {
                            name = entry.FullName.Replace('\\', '/');
                            Directory.CreateDirectory(Path.Combine(folder, ScanArchive.FolderName));
                        }
                        if (name == null)
                            continue;
                        string temp = Path.Combine(folder, name.Replace('/', Path.DirectorySeparatorChar) + ".restoring");
                        using (Stream source = entry.Open())
                        using (var target = new FileStream(temp, FileMode.Create, FileAccess.Write))
                            source.CopyTo(target);
                        unpacked[name] = temp;
                    }
                }
            }
            catch
            {
                foreach (string temp in unpacked.Values)
                    TryDelete(temp);
                throw;
            }

            // This is where the unpacked files take the place of the old ones. The kept scans are only replaced when the
            // backup has them (a backup made before the scans were kept leaves the scans of this computer as they are).
            var names = new List<string>(FileNames);
            if (unpacked.Keys.Any(ScanArchive.IsArchiveEntry))
                names.AddRange(ScanArchive.EntriesIn(folder).Concat(unpacked.Keys.Where(ScanArchive.IsArchiveEntry))
                                          .Distinct(StringComparer.OrdinalIgnoreCase));
            foreach (string name in names)
            {
                string path = Path.Combine(folder, name.Replace('/', Path.DirectorySeparatorChar));
                string? temp;
                if (unpacked.TryGetValue(name, out temp))
                {
                    if (File.Exists(path))
                        File.Delete(path);
                    File.Move(temp, path);
                }
                else if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            return unpacked.Count;
        }

        /// <summary>This is true for a file that belongs in a backup: a known file, or a kept scans file.</summary>
        private static bool IsKnown(string name)
        {
            return FileNames.Contains(name, StringComparer.OrdinalIgnoreCase) || ScanArchive.IsArchiveEntry(name);
        }

        private static void TryDelete(string path)
        {
            try { File.Delete(path); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
