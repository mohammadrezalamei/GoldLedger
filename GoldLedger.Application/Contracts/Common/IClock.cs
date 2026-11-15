namespace GoldLedger.Application.Contracts.Common;

public interface IClock
{
    DateTimeOffset Now { get; }

    DateTimeOffset ToLocalTime(DateTime utcTime);
}
