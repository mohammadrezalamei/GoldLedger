using GoldLedger.Domain.Aggregates.GoldOrder;
using GoldLedger.Domain.Common;
using GoldLedger.Domain.ValueObjects;
namespace GoldLedger.Tests.Domain.Aggregates;
public sealed class GoldOrderValidationTests
{
    [Fact]
    public void Create_with_empty_user_id_returns_validation_error()
    {
        var result = GoldOrder.Create(Guid.Empty, OrderType.Buy, GoldWeight.Create(1).Value!, Money.Create(100).Value!, Percentage.Create(1).Value!, []);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }
    [Fact]
    public void Create_with_long_note_returns_validation_error()
    {
        var note = new string('x', 1001);
        var result = GoldOrder.Create(Guid.NewGuid(), OrderType.Buy, GoldWeight.Create(1).Value!, Money.Create(100).Value!, Percentage.Create(1).Value!, [], note);
        Assert.False(result.IsSuccess);
    }
    [Fact]
    public void Settled_order_cannot_be_cancelled()
    {
        var order = CreateOrder();
        order.Complete();
        var result = order.Cancel();
        Assert.False(result.IsSuccess);
    }
    private static GoldOrder CreateOrder()
    {
        return GoldOrder.Create(Guid.NewGuid(), OrderType.Buy, GoldWeight.Create(1).Value!, Money.Create(100).Value!, Percentage.Create(1).Value!, []).Value!;
    }
}

