using GoldLedger.Application.Contracts.Common;
using GoldLedger.Application.DTOs.Wallets;
using GoldLedger.Application.Services;
using GoldLedger.Domain.Aggregates.GoldWallet;
using GoldLedger.Domain.Common;
using GoldLedger.Domain.Repositories;
using GoldLedger.Domain.ValueObjects;
using Moq;

namespace GoldLedger.Tests.Application.Services;

public class WalletServiceTests
{
    private readonly Mock<IGoldWalletRepository> _walletRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly WalletService _walletService;

    public WalletServiceTests()
    {
        _walletRepositoryMock = new Mock<IGoldWalletRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _walletService = new WalletService(_walletRepositoryMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnWallet_WhenWalletExists()
    {
        var userId = Guid.NewGuid();
        var wallet = GoldWallet.Create(userId).Value!;
        wallet.Deposit(GoldWeight.Create(10).Value!);

        _walletRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        var result = await _walletService.GetByUserIdAsync(userId);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(userId, result.Value.UserId);
        Assert.Equal(10, result.Value.BalanceValue);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnNotFound_WhenWalletDoesNotExist()
    {
        var userId = Guid.NewGuid();

        _walletRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GoldWallet?)null);

        var result = await _walletService.GetByUserIdAsync(userId);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal("کیف پول مورد نظر یافت نشد.", result.Message);
    }

    [Fact]
    public async Task DepositAsync_ShouldCreateWalletAndDeposit_WhenWalletDoesNotExist()
    {
        var userId = Guid.NewGuid();
        var request = new DepositRequestDto(5);

        _walletRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GoldWallet?)null);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _walletService.DepositAsync(userId, request);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value!.BalanceValue);

        _walletRepositoryMock.Verify(x => x.AddAsync(It.IsAny<GoldWallet>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DepositAsync_ShouldDepositToExistingWallet_WhenWalletExists()
    {
        var userId = Guid.NewGuid();
        var wallet = GoldWallet.Create(userId).Value!;
        var request = new DepositRequestDto(10);

        _walletRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _walletService.DepositAsync(userId, request);

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value!.BalanceValue);

        _walletRepositoryMock.Verify(x => x.Update(wallet), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task WithdrawAsync_ShouldFail_WhenBalanceIsInsufficient()
    {
        var userId = Guid.NewGuid();
        var wallet = GoldWallet.Create(userId).Value!;
        var request = new WithdrawRequestDto(5);

        _walletRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        var result = await _walletService.WithdrawAsync(userId, request);

        Assert.False(result.IsSuccess);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WithdrawAsync_ShouldSucceed_WhenBalanceIsSufficient()
    {
        var userId = Guid.NewGuid();
        var wallet = GoldWallet.Create(userId).Value!;
        wallet.Deposit(GoldWeight.Create(20).Value!);
        var request = new WithdrawRequestDto(5);

        _walletRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _walletService.WithdrawAsync(userId, request);

        Assert.True(result.IsSuccess);
        Assert.Equal(15, result.Value!.BalanceValue);

        _walletRepositoryMock.Verify(x => x.Update(wallet), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DepositAsync_ShouldFail_WhenAmountIsInvalid()
    {
        var userId = Guid.NewGuid();
        var request = new DepositRequestDto(-5);

        var result = await _walletService.DepositAsync(userId, request);

        Assert.False(result.IsSuccess);
        _walletRepositoryMock.Verify(x => x.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WithdrawAsync_ShouldReturnNotFound_WhenWalletDoesNotExist()
    {
        var userId = Guid.NewGuid();
        var request = new WithdrawRequestDto(5);

        _walletRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GoldWallet?)null);

        var result = await _walletService.WithdrawAsync(userId, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal("کیف پول مورد نظر یافت نشد.", result.Message);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DepositAsync_ShouldFail_WhenSaveChangesFails()
    {
        var userId = Guid.NewGuid();
        var wallet = GoldWallet.Create(userId).Value!;
        var request = new DepositRequestDto(10);

        _walletRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("خطا در ذخیره‌سازی داده‌ها.", ErrorType.Conflict));

        var result = await _walletService.DepositAsync(userId, request);

        Assert.False(result.IsSuccess);
        Assert.Equal("خطا در ذخیره‌سازی داده‌ها.", result.Message);
    }

    [Fact]
    public async Task WithdrawAsync_ShouldFail_WhenAmountIsInvalid()
    {
        var userId = Guid.NewGuid();
        var request = new WithdrawRequestDto(-5); 

        var result = await _walletService.WithdrawAsync(userId, request);

        Assert.False(result.IsSuccess);
        _walletRepositoryMock.Verify(x => x.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WithdrawAsync_ShouldFail_WhenSaveChangesFails()
    {
        var userId = Guid.NewGuid();
        var wallet = GoldWallet.Create(userId).Value!;
        wallet.Deposit(GoldWeight.Create(20).Value!);
        var request = new WithdrawRequestDto(5);

        _walletRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("خطا در ذخیره‌سازی داده‌ها.", ErrorType.Conflict));

        var result = await _walletService.WithdrawAsync(userId, request);

        Assert.False(result.IsSuccess);
        Assert.Equal("خطا در ذخیره‌سازی داده‌ها.", result.Message);
    }
}
