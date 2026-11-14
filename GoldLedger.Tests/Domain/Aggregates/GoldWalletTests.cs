using GoldLedger.Domain.Aggregates.GoldWallet;
using GoldLedger.Domain.ValueObjects;
namespace GoldLedger.Tests.Domain.Aggregates;
public sealed class GoldWalletTests
{
 [Fact] public void Deposit_and_withdraw_update_balance(){var w=GoldWallet.Create(Guid.NewGuid()).Value!; Assert.True(w.Deposit(GoldWeight.Create(10).Value!).IsSuccess); Assert.True(w.Withdraw(GoldWeight.Create(3).Value!).IsSuccess); Assert.Equal(7m,w.AvailableBalance.Value);}
 [Fact] public void Lock_reduces_available_balance(){var w=GoldWallet.Create(Guid.NewGuid()).Value!; w.Deposit(GoldWeight.Create(10).Value!); w.Lock(GoldWeight.Create(4).Value!); Assert.Equal(6m,w.AvailableBalance.Value);}
}
