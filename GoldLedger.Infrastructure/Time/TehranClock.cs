using GoldLedger.Application.Contracts.Common;

namespace GoldLedger.Infrastructure.Time;

public sealed class TehranClock(TimeProvider timeProvider) : IClock
{
    private static readonly TimeZoneInfo Tehran =
        TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran");

    public DateTimeOffset Now
    {
        get
        {
            return TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), Tehran);
        }
    }

    public DateTimeOffset ToLocalTime(DateTime utcTime)
    {
        var instant = new DateTimeOffset(DateTime.SpecifyKind(utcTime, DateTimeKind.Utc));
        return TimeZoneInfo.ConvertTime(instant, Tehran);
    }
}
