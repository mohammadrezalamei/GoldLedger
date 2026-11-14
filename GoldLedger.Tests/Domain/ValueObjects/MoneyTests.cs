using GoldLedger.Domain.ValueObjects;
namespace GoldLedger.Tests.Domain.ValueObjects;
public sealed class MoneyTests
{
 [Fact] public void Negative_fails()=>Assert.False(Money.Create(-1).IsSuccess);
 [Fact] public void Arithmetic_works()=>Assert.Equal(1500m,(Money.Create(1000).Value!+Money.Create(500).Value!).Amount);
 [Fact] public void Percentage_works()=>Assert.Equal(10m,Money.Create(100).Value!.CalculatePercentage(Percentage.Create(10).Value!).Amount);
}
