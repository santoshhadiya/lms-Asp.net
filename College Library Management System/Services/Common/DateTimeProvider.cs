namespace College_Library_Management_System.Services.Common
{
    public interface IDateTimeProvider
    {
        DateTime Now { get; }
        DateTime UtcNow { get; }
        DateTime Today { get; }
        TimeZoneInfo LibraryTimeZone { get; }
        DateTime ToLocalDateTime(DateTime dateTime);
    }

    public class DateTimeProvider : IDateTimeProvider
    {
        private readonly TimeZoneInfo _timeZone;

        public DateTimeProvider()
        {
            // College local time zone is India Standard Time (UTC+05:30)
            try
            {
                _timeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                try
                {
                    _timeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
                }
                catch
                {
                    _timeZone = TimeZoneInfo.Local;
                }
            }
        }

        public TimeZoneInfo LibraryTimeZone => _timeZone;

        public DateTime UtcNow => DateTime.UtcNow;

        public DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone);

        public DateTime Today => Now.Date;

        public DateTime ToLocalDateTime(DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Utc)
            {
                return TimeZoneInfo.ConvertTimeFromUtc(dateTime, _timeZone);
            }
            return dateTime;
        }
    }
}
