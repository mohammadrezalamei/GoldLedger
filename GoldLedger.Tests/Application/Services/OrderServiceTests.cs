using GoldLedger.Application.Contracts.Common;
using GoldLedger.Application.DTOs.Orders;
using GoldLedger.Application.Services;
using GoldLedger.Domain.Aggregates.GoldOrder;
using GoldLedger.Domain.Aggregates.GoldWallet;
using GoldLedger.Domain.Common;
using GoldLedger.Domain.Repositories;
using GoldLedger.Domain.ValueObjects;
using Moq;
using Xunit;

namespace GoldLedger.Application.Tests.Services;

public class OrderServiceTests
{
    private readonly Mock<IGoldOrderRepository> _orderRepoMock;
    private readonly Mock<IGoldWalletRepository> _walletRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly OrderService _orderService;

    public OrderServiceTests()
    {
        _orderRepoMock = new Mock<IGoldOrderRepository>();
        _walletRepoMock = new Mock<IGoldWalletRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _orderService = new OrderService(_orderRepoMock.Object, _walletRepoMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task CreateOrderAsync_Should_Succeed_When_SellOrder_And_WalletHasEnoughBalance()
    {
        var userId = Guid.NewGuid();
        var request = new CreateOrderRequestDto(
            userId,
            OrderType.Sell,
            WeightValue: 10m,
            UnitPriceAmount: 5000m,
            Currency: "IRR",
            FeePercent: 1m,
            Tags: new[] { "Gold" },
            UserNote: "Test Sell"
        );

        var walletResult = GoldWallet.Create(userId, GoldWeight.Create(20m).Value);
        var wallet = walletResult.Value!;

        _walletRepoMock.Setup(w => w.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _orderService.CreateOrderAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        _orderRepoMock.Verify(r => r.AddAsync(It.IsAny<GoldOrder>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrderAsync_Should_Fail_When_SellOrder_And_WalletBalanceNotEnough()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new CreateOrderRequestDto(
            userId,
            OrderType.Sell,
            WeightValue: 30m,
            UnitPriceAmount: 5000m,
            Currency: "IRR",
            FeePercent: 1m,
            Tags: null,
            UserNote: null
        );

        var walletResult = GoldWallet.Create(userId, GoldWeight.Create(10m).Value);
        var wallet = walletResult.Value!;

        _walletRepoMock.Setup(w => w.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        var result = await _orderService.CreateOrderAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _orderRepoMock.Verify(r => r.AddAsync(It.IsAny<GoldOrder>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CompleteOrderAsync_Should_DepositToWallet_When_BuyOrder()
    {
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var orderResult = GoldOrder.Create(
            userId,
            OrderType.Buy,
            GoldWeight.Create(5m).Value!,
            Money.Create(1000m, "IRR").Value!,
            Percentage.Create(1m).Value!,
            Enumerable.Empty<Tag>(),
            null
        );
        var order = orderResult.Value!;

        var walletResult = GoldWallet.Create(userId, GoldWeight.Create(10m).Value);
        var wallet = walletResult.Value!;

        _orderRepoMock.Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _walletRepoMock.Setup(w => w.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _orderService.CompleteOrderAsync(orderId);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Settled, order.Status);
        Assert.Equal(15m, wallet.Balance.Value);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelOrderAsync_Should_ReleaseLockedBalance_When_SellOrder()
    {
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var orderResult = GoldOrder.Create(
            userId,
            OrderType.Sell,
            GoldWeight.Create(5m).Value!,
            Money.Create(1000m, "IRR").Value!,
            Percentage.Create(1m).Value!,
            Enumerable.Empty<Tag>(),
            null
        );
        var order = orderResult.Value!;

        var walletResult = GoldWallet.Create(userId, GoldWeight.Create(10m).Value);
        var wallet = walletResult.Value!;
        wallet.Lock(GoldWeight.Create(5m).Value!);

        _orderRepoMock.Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _walletRepoMock.Setup(w => w.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _orderService.CancelOrderAsync(orderId);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(0m, wallet.LockedBalance.Value);
        Assert.Equal(10m, wallet.Balance.Value);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_Should_ReturnOrder_When_OrderExists()
    {
        var orderId = Guid.NewGuid();
        var orderResult = GoldOrder.Create(
            Guid.NewGuid(),
            OrderType.Buy,
            GoldWeight.Create(5m).Value!,
            Money.Create(1000m, "IRR").Value!,
            Percentage.Create(1m).Value!,
            Enumerable.Empty<Tag>(),
            null
        );
        var order = orderResult.Value!;

        _orderRepoMock.Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await _orderService.GetByIdAsync(orderId);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(order.Id, result.Value.Id);
    }

    [Fact]
    public async Task GetByIdAsync_Should_ReturnNotFound_When_OrderDoesNotExist()
    {
        var orderId = Guid.NewGuid();
        _orderRepoMock.Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GoldOrder?)null);

        var result = await _orderService.GetByIdAsync(orderId);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task CreateOrderAsync_Should_Succeed_When_BuyOrder_WithoutLockingWallet()
    {
        var userId = Guid.NewGuid();
        var request = new CreateOrderRequestDto(
            userId,
            OrderType.Buy,
            WeightValue: 5m,
            UnitPriceAmount: 1000m,
            Currency: "IRR",
            FeePercent: 1m,
            Tags: null,
            UserNote: "Buy test"
        );

        var walletResult = GoldWallet.Create(userId, GoldWeight.Create(10m).Value);
        var wallet = walletResult.Value!;

        _walletRepoMock.Setup(w => w.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _orderService.CreateOrderAsync(request);

        Assert.True(result.IsSuccess);
        _orderRepoMock.Verify(r => r.AddAsync(It.IsAny<GoldOrder>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrderAsync_Should_Fail_When_WalletNotFound()
    {
        var userId = Guid.NewGuid();
        var request = new CreateOrderRequestDto(
            userId,
            OrderType.Sell,
            WeightValue: 5m,
            UnitPriceAmount: 1000m,
            Currency: "IRR",
            FeePercent: 1m,
            Tags: null,
            UserNote: null
        );

        _walletRepoMock.Setup(w => w.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GoldWallet?)null);

        var result = await _orderService.CreateOrderAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task CancelOrderAsync_Should_Fail_When_OrderNotPending()
    {
        var orderId = Guid.NewGuid();
        var orderResult = GoldOrder.Create(
            Guid.NewGuid(),
            OrderType.Sell,
            GoldWeight.Create(5m).Value!,
            Money.Create(1000m, "IRR").Value!,
            Percentage.Create(1m).Value!,
            Enumerable.Empty<Tag>(),
            null
        );
        var order = orderResult.Value!;
        order.Cancel();

        _orderRepoMock.Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await _orderService.CancelOrderAsync(orderId);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task CompleteOrderAsync_Should_Fail_When_OrderNotFound()
    {
        var orderId = Guid.NewGuid();
        _orderRepoMock.Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GoldOrder?)null);

        var result = await _orderService.CompleteOrderAsync(orderId);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task GetByUserIdAsync_Should_ReturnOrdersList()
    {
        var userId = Guid.NewGuid();
        var orders = new List<GoldOrder>
        {
            GoldOrder.Create(userId, OrderType.Buy, GoldWeight.Create(2m).Value!, Money.Create(1000m, "IRR").Value!, Percentage.Create(1m).Value!, Enumerable.Empty<Tag>(), null).Value!
        };

        _orderRepoMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        var result = await _orderService.GetByUserIdAsync(userId);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
    }

    [Fact]
    public void Create_BuyOrder_ShouldRaiseGoldPurchasedDomainEvent()
    {
        var userId = Guid.NewGuid();
        var weight = GoldWeight.Create(1.0m).Value!;
        var unitPrice = Money.Create(10_000_000m, "IRR").Value!;
        var feePercent = Percentage.Create(1.0m).Value!;

        var result = GoldOrder.Create(userId, OrderType.Buy, weight, unitPrice, feePercent, Enumerable.Empty<Tag>());

        Assert.True(result.IsSuccess);
        var order = result.Value!;

        Assert.Contains(order.DomainEvents, e => e is GoldPurchasedDomainEvent);
    }

    [Fact]
    public void Create_SellOrder_ShouldRaiseGoldSoldDomainEvent()
    {
        var userId = Guid.NewGuid();
        var weight = GoldWeight.Create(1.0m).Value!;
        var unitPrice = Money.Create(10_000_000m, "IRR").Value!;
        var feePercent = Percentage.Create(1.0m).Value!;

        var result = GoldOrder.Create(userId, OrderType.Sell, weight, unitPrice, feePercent, Enumerable.Empty<Tag>());

        Assert.True(result.IsSuccess);
        var order = result.Value!;
        Assert.Contains(order.DomainEvents, e => e is GoldSoldDomainEvent);
    }
}