using GoldLedger.Domain.Aggregates.GoldOrder;
using GoldLedger.Domain.Common;
using GoldLedger.Domain.ValueObjects;
namespace GoldLedger.Tests.Domain.Aggregates;
public sealed class GoldOrderStateTests
{
    [Fact]
    public void Create_buy_order_raises_purchase_event()
    {
        var order = CreateOrder(OrderType.Buy, 2m, 100m, 5m, new[] { "investment" });
        var domainEvent = Assert.Single(order.DomainEvents);
        var purchaseEvent = Assert.IsType<GoldPurchasedDomainEvent>(domainEvent);
        Assert.Equal(order.Id, purchaseEvent.OrderId);
        Assert.Equal(order.TotalAmount, purchaseEvent.TotalAmount);
    }

    [Fact]
    public void Create_sell_order_raises_sale_event()
    {
        var order = CreateOrder(OrderType.Sell, 2m, 100m, 5m, Array.Empty<string>());
        var domainEvent = Assert.Single(order.DomainEvents);
        var saleEvent = Assert.IsType<GoldSoldDomainEvent>(domainEvent);
        Assert.Equal(order.Id, saleEvent.OrderId);
        Assert.Equal(order.Weight, saleEvent.Weight);
    }

    [Fact]
    public void Create_preserves_unique_tags_and_note()
    {
        var order = CreateOrder(OrderType.Buy, 1m, 100m, 0m, new[] { "gold", "gold", "desk" }, "memo");
        Assert.Equal(2, order.Tags.Count);
        Assert.Contains(order.Tags, tag => tag.Value == "gold");
        Assert.Equal("memo", order.UserNote);
    }

    [Fact]
    public void UpdateMetadata_replaces_tags_and_note()
    {
        var order = CreateOrder(OrderType.Buy, 1m, 100m, 0m, new[] { "old" }, "old note");
        var result = order.UpdateMetadata(new[] { Tag.Create("new").Value! }, "new note");
        Assert.True(result.IsSuccess);
        Assert.Single(order.Tags);
        Assert.Equal("new", Assert.Single(order.Tags).Value);
        Assert.Equal("new note", order.UserNote);
    }

    [Fact]
    public void UpdateMetadata_allows_empty_tags()
    {
        var order = CreateOrder(OrderType.Buy, 1m, 100m, 0m, new[] { "old" }, "old note");
        var result = order.UpdateMetadata(Array.Empty<Tag>(), null);
        Assert.True(result.IsSuccess);
        Assert.Empty(order.Tags);
        Assert.Null(order.UserNote);
    }

    [Fact]
    public void UpdateMetadata_rejects_long_note_without_mutating_order()
    {
        var order = CreateOrder(OrderType.Buy, 1m, 100m, 0m, new[] { "old" }, "old note");
        var result = order.UpdateMetadata(Array.Empty<Tag>(), new string('x', 1001));
        Assert.False(result.IsSuccess);
        Assert.Single(order.Tags);
        Assert.Equal("old note", order.UserNote);
    }

    [Fact]
    public void UpdateMetadata_rejects_cancelled_order()
    {
        var order = CreateOrder(OrderType.Buy, 1m, 100m, 0m, Array.Empty<string>());
        order.Cancel();
        var result = order.UpdateMetadata(Array.Empty<Tag>(), "new");
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Complete_clears_pending_state_only_once()
    {
        var order = CreateOrder(OrderType.Buy, 1m, 100m, 0m, Array.Empty<string>());
        Assert.True(order.Complete().IsSuccess);
        Assert.False(order.Complete().IsSuccess);
        Assert.Equal(OrderStatus.Settled, order.Status);
    }

    private static GoldOrder CreateOrder(OrderType orderType, decimal weight, decimal unitPrice, decimal fee, IEnumerable<string> tagValues, string? note = null)
    {
        var tags = tagValues.Select(value => Tag.Create(value).Value!).ToArray();
        return GoldOrder.Create(Guid.NewGuid(), orderType, GoldWeight.Create(weight).Value!, Money.Create(unitPrice).Value!, Percentage.Create(fee).Value!, tags, note).Value!;
    }
}
