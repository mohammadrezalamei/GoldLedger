using GoldLedger.Domain.Aggregates.GoldOrder;
using GoldLedger.Domain.ValueObjects;
namespace GoldLedger.Tests.Domain.Aggregates;
public sealed class GoldOrderTests
{
 [Fact] public void Create_sets_pending_status(){var r=GoldOrder.Create(Guid.NewGuid(),OrderType.Buy,GoldWeight.Create(2).Value!,Money.Create(1000).Value!,Percentage.Create(1).Value!,[]); Assert.True(r.IsSuccess); Assert.Equal(OrderStatus.Pending,r.Value!.Status); Assert.Equal(2020m,r.Value.TotalAmount.Amount);}
 [Fact] public void Cancel_changes_status(){var o=GoldOrder.Create(Guid.NewGuid(),OrderType.Buy,GoldWeight.Create(1).Value!,Money.Create(100).Value!,Percentage.Create(1).Value!,[]).Value!; Assert.True(o.Cancel().IsSuccess); Assert.Equal(OrderStatus.Cancelled,o.Status);}
}
