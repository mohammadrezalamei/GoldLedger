using GoldLedger.Domain.Aggregates.GoldWallet;
using GoldLedger.Domain.ValueObjects;
namespace GoldLedger.Tests.Domain.Aggregates;
public sealed class GoldWalletEdgeCaseTests
{
    [Fact]
    public void Create_with_initial_balance_sets_available_balance()
    {
        var wallet = GoldWallet.Create(Guid.NewGuid(), GoldWeight.Create(3.5m).Value!).Value!;
        Assert.Equal(3.5m, wallet.Balance.Value);
        Assert.Equal(0m, wallet.LockedBalance.Value);
        Assert.Equal(3.5m, wallet.AvailableBalance.Value);
    }

    [Fact]
    public void Deposit_increases_balance()
    {
        var wallet = CreateWallet(2m);
        Assert.True(wallet.Deposit(GoldWeight.Create(1.25m).Value!).IsSuccess);
        Assert.Equal(3.25m, wallet.Balance.Value);
    }

    [Fact]
    public void Withdraw_reduces_balance()
    {
        var wallet = CreateWallet(2m);
        Assert.True(wallet.Withdraw(GoldWeight.Create(0.75m).Value!).IsSuccess);
        Assert.Equal(1.25m, wallet.Balance.Value);
    }

    [Fact]
    public void Withdraw_cannot_use_locked_balance()
    {
        var wallet = CreateWallet(2m);
        wallet.Lock(GoldWeight.Create(1.5m).Value!);
        var result = wallet.Withdraw(GoldWeight.Create(0.75m).Value!);
        Assert.False(result.IsSuccess);
        Assert.Equal(2m, wallet.Balance.Value);
    }

    [Fact]
    public void Lock_reduces_available_balance_without_reducing_total()
    {
        var wallet = CreateWallet(2m);
        Assert.True(wallet.Lock(GoldWeight.Create(0.8m).Value!).IsSuccess);
        Assert.Equal(2m, wallet.Balance.Value);
        Assert.Equal(0.8m, wallet.LockedBalance.Value);
        Assert.Equal(1.2m, wallet.AvailableBalance.Value);
    }

    [Fact]
    public void Release_locked_without_settlement_restores_available_balance()
    {
        var wallet = CreateWallet(2m);
        wallet.Lock(GoldWeight.Create(0.8m).Value!);
        Assert.True(wallet.ReleaseLocked(GoldWeight.Create(0.8m).Value!, false).IsSuccess);
        Assert.Equal(2m, wallet.Balance.Value);
        Assert.Equal(0m, wallet.LockedBalance.Value);
    }

    [Fact]
    public void Release_locked_with_settlement_reduces_balance()
    {
        var wallet = CreateWallet(2m);
        wallet.Lock(GoldWeight.Create(0.8m).Value!);
        Assert.True(wallet.ReleaseLocked(GoldWeight.Create(0.8m).Value!, true).IsSuccess);
        Assert.Equal(1.2m, wallet.Balance.Value);
        Assert.Equal(0m, wallet.LockedBalance.Value);
    }

    [Fact]
    public void Lock_rejects_amount_above_available_balance()
    {
        var wallet = CreateWallet(1m);
        var result = wallet.Lock(GoldWeight.Create(1.01m).Value!);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Release_locked_rejects_amount_above_locked_balance()
    {
        var wallet = CreateWallet(1m);
        wallet.Lock(GoldWeight.Create(0.4m).Value!);
        var result = wallet.ReleaseLocked(GoldWeight.Create(0.5m).Value!, false);
        Assert.False(result.IsSuccess);
        Assert.Equal(0.4m, wallet.LockedBalance.Value);
    }

    private static GoldWallet CreateWallet(decimal balance)
    {
        return GoldWallet.Create(Guid.NewGuid(), GoldWeight.Create(balance).Value!).Value!;
    }
}
