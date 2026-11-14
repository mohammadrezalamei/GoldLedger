using GoldLedger.Domain.ValueObjects;
namespace GoldLedger.Tests.Domain.ValueObjects;
public sealed class GoldWeightTests
{
 [Fact] public void Negative_fails()=>Assert.False(GoldWeight.Create(-1).IsSuccess);
 [Fact] public void Addition_works()=>Assert.Equal(3m,(GoldWeight.Create(1).Value!+GoldWeight.Create(2).Value!).Value);
}
