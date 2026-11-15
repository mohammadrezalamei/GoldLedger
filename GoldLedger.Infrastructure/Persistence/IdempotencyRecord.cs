namespace GoldLedger.Infrastructure.Persistence;

public sealed class IdempotencyRecord
{
    public string Key { get; private set; } = string.Empty;
    public string RequestHash { get; private set; } = string.Empty;
    public int StatusCode { get; private set; }
    public string Body { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public string? Location { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private IdempotencyRecord()
    {
    }

    public IdempotencyRecord(
        string key,
        string requestHash,
        int statusCode,
        string body,
        string contentType,
        string? location,
        DateTime createdAt)
    {
        Key = key;
        RequestHash = requestHash;
        StatusCode = statusCode;
        Body = body;
        ContentType = contentType;
        Location = location;
        CreatedAt = createdAt;
    }
}
