using GoldLedger.Domain.Aggregates.GoldWallet;
using GoldLedger.Domain.ValueObjects;
using Xunit;

namespace GoldLedger.Tests.Domain.Aggregates;

public class GoldWalletTests
{
    [Fact]
    public void Create_ValidWallet_ShouldSucceedWithZeroOrInitialBalance()
    {
        var userId = Guid.NewGuid();
        var result = GoldWallet.Create(userId);

        Assert.True(result.IsSuccess);
        var wallet = result.Value!;
        Assert.Equal(userId, wallet.UserId);
        Assert.Equal(0m, wallet.Balance.Value);
        Assert.Equal(0m, wallet.LockedBalance.Value);
        Assert.Equal(0m, wallet.AvailableBalance.Value);
    }

    [Fact]
    public void Create_WithEmptyUserId_ShouldFail()
    {
        var result = GoldWallet.Create(Guid.Empty);

        Assert.False(result.IsSuccess);
        Assert.Equal("شناسه کاربر نامعتبر است.", result.Message);
    }

    [Fact]
    public void Deposit_ValidAmount_ShouldIncreaseBalance()
    {
        var wallet = GoldWallet.Create(Guid.NewGuid()).Value!;
        var depositAmount = GoldWeight.Create(5.5m).Value!;

        var result = wallet.Deposit(depositAmount);

        Assert.True(result.IsSuccess);
        Assert.Equal(5.5m, wallet.Balance.Value);
        Assert.Equal(5.5m, wallet.AvailableBalance.Value);
    }

    [Fact]
    public void Deposit_InvalidAmount_ShouldFail()
    {
        var wallet = GoldWallet.Create(Guid.NewGuid()).Value!;

        var result = wallet.Deposit(GoldWeight.Zero);

        Assert.False(result.IsSuccess);
        Assert.Equal("مقدار واریز باید بیشتر از صفر باشد.", result.Message);
    }

    [Fact]
    public void Withdraw_SufficientBalance_ShouldDecreaseBalance()
    {
        var initial = GoldWeight.Create(10m).Value!;
        var wallet = GoldWallet.Create(Guid.NewGuid(), initial).Value!;
        var withdrawAmount = GoldWeight.Create(3m).Value!;

        var result = wallet.Withdraw(withdrawAmount);

        Assert.True(result.IsSuccess);
        Assert.Equal(7m, wallet.Balance.Value);
        Assert.Equal(7m, wallet.AvailableBalance.Value);
    }

    [Fact]
    public void Withdraw_InsufficientBalance_ShouldFail()
    {
        var initial = GoldWeight.Create(5m).Value!;
        var wallet = GoldWallet.Create(Guid.NewGuid(), initial).Value!;
        var withdrawAmount = GoldWeight.Create(10m).Value!;

        var result = wallet.Withdraw(withdrawAmount);

        Assert.False(result.IsSuccess);
        Assert.Equal("موجودی آزاد کیف پول برای انجام این عملیات کافی نیست.", result.Message);
    }

    [Fact]
    public void Lock_ValidAmount_ShouldIncreaseLockedBalanceAndDecreaseAvailable()
    {
        var initial = GoldWeight.Create(10m).Value!;
        var wallet = GoldWallet.Create(Guid.NewGuid(), initial).Value!;
        var lockAmount = GoldWeight.Create(4m).Value!;

        var result = wallet.Lock(lockAmount);

        Assert.True(result.IsSuccess);
        Assert.Equal(10m, wallet.Balance.Value);
        Assert.Equal(4m, wallet.LockedBalance.Value);
        Assert.Equal(6m, wallet.AvailableBalance.Value);
    }

    [Fact]
    public void ReleaseLocked_SettledOrder_ShouldDecreaseBothBalanceAndLocked()
    {
        var initial = GoldWeight.Create(10m).Value!;
        var wallet = GoldWallet.Create(Guid.NewGuid(), initial).Value!;
        var amount = GoldWeight.Create(4m).Value!;

        wallet.Lock(amount);

        var result = wallet.ReleaseLocked(amount, isSettled: true);

        Assert.True(result.IsSuccess);
        Assert.Equal(6m, wallet.Balance.Value);
        Assert.Equal(0m, wallet.LockedBalance.Value);
        Assert.Equal(6m, wallet.AvailableBalance.Value);
    }

    [Fact]
    public void ReleaseLocked_CancelledOrder_ShouldRestoreAvailableBalance()
    {
        var initial = GoldWeight.Create(10m).Value!;
        var wallet = GoldWallet.Create(Guid.NewGuid(), initial).Value!;
        var amount = GoldWeight.Create(4m).Value!;

        wallet.Lock(amount);

        var result = wallet.ReleaseLocked(amount, isSettled: false);

        Assert.True(result.IsSuccess);
        Assert.Equal(10m, wallet.Balance.Value); 
        Assert.Equal(0m, wallet.LockedBalance.Value);
        Assert.Equal(10m, wallet.AvailableBalance.Value);
    }

    [Fact]
    public void Deposit_WithNullWeight_ShouldFail()
    {
        var wallet = GoldWallet.Create(Guid.NewGuid()).Value!;

        var result = wallet.Deposit(null!);

        Assert.False(result.IsSuccess);
        Assert.Equal("مقدار واریز نمی‌تواند خالی باشد.", result.Message);
    }

    [Fact]
    public void Withdraw_WithNullWeight_ShouldFail()
    {
        var wallet = GoldWallet.Create(Guid.NewGuid()).Value!;

        var result = wallet.Withdraw(null!);

        Assert.False(result.IsSuccess);
        Assert.Equal("مقدار برداشت نمی‌تواند خالی باشد.", result.Message);
    }

    [Fact]
    public void Lock_InsufficientAvailableBalance_ShouldFail()
    {
        var initial = GoldWeight.Create(5m).Value!;
        var wallet = GoldWallet.Create(Guid.NewGuid(), initial).Value!;
        var lockAmount = GoldWeight.Create(10m).Value!;

        var result = wallet.Lock(lockAmount);

        Assert.False(result.IsSuccess);
        Assert.Equal("موجودی آزاد کافی برای مسدودسازی وجود ندارد.", result.Message);
    }

    [Fact]
    public void ReleaseLocked_ExceedingLockedBalance_ShouldFail()
    {
        var initial = GoldWeight.Create(10m).Value!;
        var wallet = GoldWallet.Create(Guid.NewGuid(), initial).Value!;
        var lockedAmount = GoldWeight.Create(3m).Value!;

        wallet.Lock(lockedAmount);

        var excessiveAmount = GoldWeight.Create(5m).Value!;
        var result = wallet.ReleaseLocked(excessiveAmount, isSettled: true);

        Assert.False(result.IsSuccess);
        Assert.Equal("مقدار درخواستی بیشتر از موجودی مسدودشده است.", result.Message);
    }
}
