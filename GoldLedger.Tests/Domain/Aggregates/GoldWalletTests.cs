using GoldLedger.Domain.Common;
using GoldLedger.Domain.Aggregates.GoldWallet;
using GoldLedger.Domain.ValueObjects;
namespace GoldLedger.Tests.Domain.Aggregates;
public sealed class GoldWalletTests
{
    [Fact]
    public void Create_with_empty_user_id_fails()
    {
        var result = GoldWallet.Create(Guid.Empty);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }
    [Fact]
    public void Create_initializes_zero_balances()
    {
        var wallet = GoldWallet.Create(Guid.NewGuid()).Value!;
        Assert.Equal(0m, wallet.Balance.Value);
        Assert.Equal(0m, wallet.LockedBalance.Value);
        Assert.Equal(0m, wallet.AvailableBalance.Value);
    }
    [Fact]
    public void Deposit_increases_balance()
    {
        var wallet = GoldWallet.Create(Guid.NewGuid()).Value!;
        var depositResult = wallet.Deposit(GoldWeight.Create(10m).Value!);
        Assert.True(depositResult.IsSuccess);
        Assert.Equal(10m, wallet.Balance.Value);
    }
    [Fact]
    public void Deposit_zero_weight_fails()
    {
        var wallet = GoldWallet.Create(Guid.NewGuid()).Value!;
        var depositResult = wallet.Deposit(GoldWeight.Create(0m).Value!);
        Assert.False(depositResult.IsSuccess);
    }
    [Fact]
    public void Withdraw_reduces_available_balance()
    {
        var wallet = CreateWalletWithBalance(10m);
        var withdrawResult = wallet.Withdraw(GoldWeight.Create(3m).Value!);
        Assert.True(withdrawResult.IsSuccess);
        Assert.Equal(7m, wallet.AvailableBalance.Value);
    }
    [Fact]
    public void Withdraw_more_than_available_balance_fails()
    {
        var wallet = CreateWalletWithBalance(2m);
        var withdrawResult = wallet.Withdraw(GoldWeight.Create(3m).Value!);
        Assert.False(withdrawResult.IsSuccess);
    }
    [Fact]
    public void Lock_reduces_available_balance_without_changing_balance()
    {
        var wallet = CreateWalletWithBalance(10m);
        var lockResult = wallet.Lock(GoldWeight.Create(4m).Value!);
        Assert.True(lockResult.IsSuccess);
        Assert.Equal(10m, wallet.Balance.Value);
        Assert.Equal(6m, wallet.AvailableBalance.Value);
    }
    [Fact]
    public void Release_unsettled_lock_restores_available_balance()
    {
        var wallet = CreateWalletWithBalance(10m);
        wallet.Lock(GoldWeight.Create(4m).Value!);
        var releaseResult = wallet.ReleaseLocked(GoldWeight.Create(4m).Value!, false);
        Assert.True(releaseResult.IsSuccess);
        Assert.Equal(10m, wallet.Balance.Value);
        Assert.Equal(10m, wallet.AvailableBalance.Value);
    }
    [Fact]
    public void Release_settled_lock_reduces_balance()
    {
        var wallet = CreateWalletWithBalance(10m);
        wallet.Lock(GoldWeight.Create(4m).Value!);
        var releaseResult = wallet.ReleaseLocked(GoldWeight.Create(4m).Value!, true);
        Assert.True(releaseResult.IsSuccess);
        Assert.Equal(6m, wallet.Balance.Value);
    }
    private static GoldWallet CreateWalletWithBalance(decimal balance)
    {
        var wallet = GoldWallet.Create(Guid.NewGuid()).Value!;
        wallet.Deposit(GoldWeight.Create(balance).Value!);
        return wallet;
    }
}
