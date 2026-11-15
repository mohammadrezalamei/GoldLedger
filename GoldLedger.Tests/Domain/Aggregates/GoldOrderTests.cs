using GoldLedger.Domain.Common;
using GoldLedger.Domain.Aggregates.GoldOrder;
using GoldLedger.Domain.ValueObjects;
namespace GoldLedger.Tests.Domain.Aggregates;
public sealed class GoldOrderTests
{
    [Fact]
    public void Create_buy_order_calculates_fee_and_pending_status()
    {
        var order = CreateOrder(OrderType.Buy, 2m, 1000m, 1m);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(2020m, order.TotalAmount.Amount);
    }
    [Fact]
    public void Create_sell_order_deducts_fee()
    {
        var order = CreateOrder(OrderType.Sell, 2m, 1000m, 1m);
        Assert.Equal(1980m, order.TotalAmount.Amount);
    }
    [Fact]
    public void Create_with_empty_user_id_fails()
    {
        var result = GoldOrder.Create(Guid.Empty, OrderType.Buy, GoldWeight.Create(1m).Value!, Money.Create(100m).Value!, Percentage.Create(1m).Value!, []);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }
    [Fact]
    public void Create_with_none_type_fails()
    {
        var result = GoldOrder.Create(Guid.NewGuid(), OrderType.None, GoldWeight.Create(1m).Value!, Money.Create(100m).Value!, Percentage.Create(1m).Value!, []);
        Assert.False(result.IsSuccess);
    }
    [Fact]
    public void Create_with_long_note_fails()
    {
        var longNote = new string('x', 1001);
        var result = GoldOrder.Create(Guid.NewGuid(), OrderType.Buy, GoldWeight.Create(1m).Value!, Money.Create(100m).Value!, Percentage.Create(1m).Value!, [], longNote);
        Assert.False(result.IsSuccess);
    }
    [Fact]
    public void Complete_pending_order_sets_settled_status()
    {
        var order = CreateOrder(OrderType.Buy, 1m, 100m, 1m);
        var result = order.Complete();
        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Settled, order.Status);
    }
    [Fact]
    public void Cancel_pending_order_sets_cancelled_status()
    {
        var order = CreateOrder(OrderType.Buy, 1m, 100m, 1m);
        var result = order.Cancel();
        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }
    [Fact]
    public void Settled_order_cannot_be_cancelled()
    {
        var order = CreateOrder(OrderType.Buy, 1m, 100m, 1m);
        order.Complete();
        Assert.False(order.Cancel().IsSuccess);
    }
    [Fact]
    public void Cancelled_order_cannot_be_completed()
    {
        var order = CreateOrder(OrderType.Buy, 1m, 100m, 1m);
        order.Cancel();
        Assert.False(order.Complete().IsSuccess);
    }
    private static GoldOrder CreateOrder(OrderType orderType, decimal weightValue, decimal unitPrice, decimal feePercent)
    {
        return GoldOrder.Create(Guid.NewGuid(), orderType, GoldWeight.Create(weightValue).Value!, Money.Create(unitPrice).Value!, Percentage.Create(feePercent).Value!, []).Value!;
    }
}

