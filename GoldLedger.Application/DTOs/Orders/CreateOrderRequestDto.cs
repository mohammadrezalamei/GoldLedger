namespace GoldLedger.Application.DTOs.Orders;

public record CreateOrderRequestDto(
    Guid UserId,
    OrderType Type,
    decimal WeightValue,
    decimal UnitPriceAmount,
    decimal FeePercent,
    IEnumerable<string>? Tags,
    string? UserNote
);

