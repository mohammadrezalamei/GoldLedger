using GoldLedger.Domain.Aggregates.GoldOrder;

namespace GoldLedger.Application.DTOs.Orders;

public record GoldOrderResponseDto(
    Guid Id,
    Guid UserId,
    OrderType Type,
    decimal WeightValue,
    decimal UnitPriceAmount,
    decimal FeePercent,
    decimal TotalAmount,
    OrderStatus Status,
    string? UserNote,
    IEnumerable<string> Tags,
    DateTimeOffset CreatedAt
);
