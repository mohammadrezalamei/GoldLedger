using GoldLedger.Application.Contracts.Common;
namespace GoldLedger.Infrastructure.Time;
public sealed class TehranClock : IClock
{
    private static readonly TimeZoneInfo Tehran = TimeZoneInfo.FindSystemTimeZoneById("Iran Standard Time");
    public DateTimeOffset Now => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Tehran);
}
