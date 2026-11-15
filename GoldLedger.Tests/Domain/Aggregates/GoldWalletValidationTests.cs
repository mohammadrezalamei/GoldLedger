using GoldLedger.Domain.Aggregates.GoldWallet;
using GoldLedger.Domain.ValueObjects;
namespace GoldLedger.Tests.Domain.Aggregates;
public sealed class GoldWalletValidationTests
{
    [Fact]
    public void Withdraw_more_than_available_balance_fails()
    {
        var wallet = GoldWallet.Create(Guid.NewGuid()).Value!;
        wallet.Deposit(GoldWeight.Create(2).Value!);
        var result = wallet.Withdraw(GoldWeight.Create(3).Value!);
        Assert.False(result.IsSuccess);
    }
    [Fact]
    public void Release_settled_lock_decreases_total_balance()
    {
        var wallet = GoldWallet.Create(Guid.NewGuid()).Value!;
        wallet.Deposit(GoldWeight.Create(5).Value!);
        wallet.Lock(GoldWeight.Create(2).Value!);
        var result = wallet.ReleaseLocked(GoldWeight.Create(2).Value!, true);
        Assert.True(result.IsSuccess);
        Assert.Equal(3m, wallet.Balance.Value);
    }
}

