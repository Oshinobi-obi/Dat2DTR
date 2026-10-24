using System.Globalization;
using System.Text;

namespace Dat2DTR
{
    public enum DtrPaper
    {
        A4,
        Letter,
        Long,   // This is 8.5 x 13 inches ("long bond paper")
        Legal   // This is 8.5 x 14 inches
    }

    /// <summary>
    /// This is the set of office rules used to compute Late and Undertime: the four work schedules
    /// and how Late is counted. They are saved in a small text file so the user
    /// only sets them once.
    /// </summary>
    public sealed class DtrSettings
    {
        /// <summary>This is Schedule A (index 0), B (1), C (2) and D (3).</summary>
        public WorkSchedule[] Schedules = WorkSchedule.Defaults();

        /// <summary>
        /// This is true  = once past the grace period, late is counted from Time In   (7:20 -> 20 minutes late)
        ///      false = late is counted only after the grace period                (7:20 ->  5 minutes late)
        /// </summary>
        public bool CountLateFromOfficialTime = true;

        public DtrPaper Paper = DtrPaper.A4;

        /// <summary>This is the folder that was used last time, so the Open/Save windows start there.</summary>
        public string LastFolder = "";

        /// <summary>This is how a number becomes a schedule: 0 = A, 1 = B, 2 = C, 3 = D. Any other number gives Schedule A.</summary>
        public WorkSchedule GetSchedule(int index)
        {
            return index >= 0 && index < Schedules.Length ? Schedules[index] : Schedules[0];
        }

        public static string FormatTime(TimeSpan time)
        {
            return DateTime.Today.Add(time).ToString("h:mm tt", CultureInfo.InvariantCulture);
        }

        public DtrSettings Clone()
        {
            var copy = (DtrSettings)MemberwiseClone();
            copy.Schedules = Schedules.Select(s => s.Clone()).ToArray();
            return copy;
        }

        /// <summary>This is the check of the schedules: a message if one is not sensible, or "" if everything is fine.</summary>
        public string Validate()
        {
            foreach (WorkSchedule schedule in Schedules)
            {
                string problem = schedule.Validate();
                if (problem.Length > 0)
                    return "Schedule " + schedule.Letter + ": " + problem;
            }
            return "";
        }

        // This is the saving part ----------------------------------------------------------------

        /// <summary>This is the folder where the app keeps its settings, the employee list, the hand-typed corrections and the leaves.</summary>
        public static string DataFolder
        {
            get
            {
                string folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Dat2DTR");
                Directory.CreateDirectory(folder);
                return folder;
            }
        }

        private static string SettingsPath
        {
            get { return Path.Combine(DataFolder, "settings.ini"); }
        }

        // This is the file: one "name=value" per line. Schedule lines start with the
        // schedule letter, for example:   A.TimeIn=07:00     B.WorkDays=0111110
        /// <summary>This is version 3 = 15-minute grace period again (version 2 had briefly changed the default to none).</summary>
        private const int SettingsVersion = 3;

        public static DtrSettings Load()
        {
            var s = new DtrSettings();
            int version = 1;
            try
            {
                if (!File.Exists(SettingsPath))
                    return s;

                foreach (string raw in File.ReadAllLines(SettingsPath))
                {
                    int eq = raw.IndexOf('=');
                    if (eq <= 0) continue;
                    string key = raw.Substring(0, eq).Trim();
                    string value = raw.Substring(eq + 1).Trim();

                    if (key.Length > 2 && key[1] == '.')
                    {
                        WorkSchedule? schedule = s.Schedules.FirstOrDefault(x => x.Letter == key.Substring(0, 1));
                        if (schedule != null)
                            ReadScheduleLine(schedule, key.Substring(2), value);
                        continue;
                    }

                    switch (key)
                    {
                        case "CountLateFromOfficialTime": s.CountLateFromOfficialTime = value == "1"; break;
                        case "Paper":
                            DtrPaper paper;
                            if (Enum.TryParse(value, out paper)) s.Paper = paper;
                            break;
                        case "LastFolder": s.LastFolder = value; break;
                        case "Version": int.TryParse(value, out version); break;
                    }
                }

                // This is for version 2 files: version 2 changed the 15-minute grace period to none. The office rule is 15 minutes that
                // must be made up at the end of the day, so a version-2 file with no grace period gets its
                // 15 minutes back, once. (Version 1 files already have 15.)
                if (version == 2)
                    foreach (WorkSchedule schedule in s.Schedules)
                        if (schedule.GraceMinutes == 0) schedule.GraceMinutes = 15;

                // This is the fallback: if the file was edited into nonsense, the default schedules are used.
                if (s.Validate().Length > 0)
                    s.Schedules = WorkSchedule.Defaults();
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            return s;
        }

        private static void ReadScheduleLine(WorkSchedule schedule, string name, string value)
        {
            switch (name)
            {
                case "TimeIn": schedule.TimeIn = ParseTime(value, schedule.TimeIn); break;
                case "LunchStart": schedule.LunchStart = ParseTime(value, schedule.LunchStart); break;
                case "LunchEnd": schedule.LunchEnd = ParseTime(value, schedule.LunchEnd); break;
                case "TimeOut": schedule.TimeOut = ParseTime(value, schedule.TimeOut); break;
                case "GraceMinutes":
                    int grace;
                    if (int.TryParse(value, out grace) && grace >= 0 && grace <= 120) schedule.GraceMinutes = grace;
                    break;
                case "WorkDays":
                    if (value.Length == 7)
                        for (int i = 0; i < 7; i++) schedule.WorkDays[i] = value[i] == '1';
                    break;
            }
        }

        public void Save()
        {
            var sb = new StringBuilder();
            foreach (WorkSchedule schedule in Schedules)
            {
                string p = schedule.Letter + ".";
                sb.AppendLine(p + "TimeIn=" + schedule.TimeIn.ToString(@"hh\:mm", CultureInfo.InvariantCulture));
                sb.AppendLine(p + "LunchStart=" + schedule.LunchStart.ToString(@"hh\:mm", CultureInfo.InvariantCulture));
                sb.AppendLine(p + "LunchEnd=" + schedule.LunchEnd.ToString(@"hh\:mm", CultureInfo.InvariantCulture));
                sb.AppendLine(p + "TimeOut=" + schedule.TimeOut.ToString(@"hh\:mm", CultureInfo.InvariantCulture));
                sb.AppendLine(p + "GraceMinutes=" + schedule.GraceMinutes.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine(p + "WorkDays=" + new string(schedule.WorkDays.Select(d => d ? '1' : '0').ToArray()));
            }
            sb.AppendLine("CountLateFromOfficialTime=" + (CountLateFromOfficialTime ? "1" : "0"));
            sb.AppendLine("Paper=" + Paper);
            sb.AppendLine("LastFolder=" + LastFolder);
            sb.AppendLine("Version=" + SettingsVersion);

            try
            {
                File.WriteAllText(SettingsPath, sb.ToString());
            }
            catch (IOException) { }                 // This is on purpose: not being able to save settings
            catch (UnauthorizedAccessException) { } // This is because it should never stop the user from working
        }

        private static TimeSpan ParseTime(string text, TimeSpan fallback)
        {
            TimeSpan value;
            if (TimeSpan.TryParseExact(text, @"hh\:mm", CultureInfo.InvariantCulture, out value))
                return value;
            return fallback;
        }
    }
}
