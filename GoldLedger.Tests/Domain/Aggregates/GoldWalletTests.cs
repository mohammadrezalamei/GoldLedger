using GoldLedger.Domain.Aggregates.GoldWallet;
using GoldLedger.Domain.Common;
using GoldLedger.Domain.ValueObjects;

namespace GoldLedger.Tests.Domain.Aggregates;

public sealed class GoldWalletTests
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
    public void Deposit_increases_balance_from_empty_wallet()
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

    [Fact]
    public void Withdraw_more_than_available_balance_fails_returns_validation_failure()
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
