namespace GoldLedger.Infrastructure.Persistence;

public sealed class DomainEventRecord
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;

    private DomainEventRecord()
    {
    }

    public DomainEventRecord(Guid orderId, string eventType, string payload)
    {
        Id = Guid.NewGuid();
        OrderId = orderId;
        EventType = eventType;
        Payload = payload;
    }
}
