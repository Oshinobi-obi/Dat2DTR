using System.Globalization;

namespace Dat2DTR
{
    /// <summary>
    /// This is the date and time in the Philippines (UTC+8, no daylight saving time), for the clock at the bottom
    /// right of the main window. It comes from this computer's clock, turned into Philippine time, so it is right
    /// even when Windows is set to another time zone (but not when the computer's clock itself is wrong).
    /// </summary>
    public static class PhilippineTime
    {
        /// <summary>This is how far the Philippines is ahead of UTC: 8 hours, all year.</summary>
        public static readonly TimeSpan Offset = TimeSpan.FromHours(8);

        /// <summary>This is the date and time in the Philippines right now.</summary>
        public static DateTime Now
        {
            get { return DateTime.SpecifyKind(DateTime.UtcNow + Offset, DateTimeKind.Unspecified); }
        }

        /// <summary>This is the clock as shown, e.g. "Saturday, October 31, 2026   10:35 AM  (PH time, UTC+8)".</summary>
        public static string ClockText(DateTime time)
        {
            return time.ToString("dddd, MMMM d, yyyy", CultureInfo.InvariantCulture) + "   " +
                   time.ToString("h:mm tt", CultureInfo.InvariantCulture) + "  (PH time, UTC+8)";
        }
    }
}
