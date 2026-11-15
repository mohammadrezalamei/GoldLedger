using GoldLedger.Application.Contracts.Common;
using GoldLedger.Application.Contracts.Services;
using GoldLedger.Application.DTOs.Orders;
using GoldLedger.Domain.Aggregates.GoldOrder;
using GoldLedger.Domain.Common;
using GoldLedger.Domain.Repositories;
using GoldLedger.Domain.ValueObjects;

namespace GoldLedger.Application.Services;

public class OrderService : IOrderService
{
    private readonly IGoldOrderRepository _orderRepository;
    private readonly IGoldWalletRepository _walletRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock? _clock;

    public OrderService(
        IGoldOrderRepository orderRepository,
        IGoldWalletRepository walletRepository,
        IUnitOfWork unitOfWork,
        IClock? clock = null)
    {
        _orderRepository = orderRepository;
        _walletRepository = walletRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<GoldOrderResponseDto>> GetByIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(
            orderId,
            cancellationToken);

        if (order is null)
        {
            return Result<GoldOrderResponseDto>.Failure(
                "سفارش مورد نظر یافت نشد.",
                ErrorType.NotFound);
        }

        return Result<GoldOrderResponseDto>.Success(MapToDto(order));
    }

    public async Task<Result<Guid>> CreateOrderAsync(
        CreateOrderRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return await UnitOfWorkOperation.ExecuteAsync(_unitOfWork, async () =>
        {
            var weightResult = GoldWeight.Create(request.WeightValue);
            if (!weightResult.IsSuccess)
            {
                return Result<Guid>.Failure(
                    weightResult.Message!,
                    weightResult.ErrorType);
            }

            var priceResult = Money.Create(request.UnitPriceAmount);
            if (!priceResult.IsSuccess)
            {
                return Result<Guid>.Failure(
                    priceResult.Message!,
                    priceResult.ErrorType);
            }

            var feeResult = Percentage.Create(request.FeePercent);
            if (!feeResult.IsSuccess)
            {
                return Result<Guid>.Failure(
                    feeResult.Message!,
                    feeResult.ErrorType);
            }

            var tags = new List<Tag>();

            foreach (var rawTag in request.Tags ?? Enumerable.Empty<string>())
            {
                var tagResult = Tag.Create(rawTag);
                if (!tagResult.IsSuccess)
                {
                    return Result<Guid>.Failure(
                        tagResult.Message!,
                        tagResult.ErrorType);
                }

                tags.Add(tagResult.Value!);
            }

            var orderResult = GoldOrder.Create(
                request.UserId,
                request.Type,
                weightResult.Value!,
                priceResult.Value!,
                feeResult.Value!,
                tags,
                request.UserNote,
                _clock?.Now.UtcDateTime);

            if (!orderResult.IsSuccess)
            {
                return Result<Guid>.Failure(
                    orderResult.Message!,
                    orderResult.ErrorType);
            }

            var order = orderResult.Value!;

            var wallet = await _walletRepository.GetByUserIdAsync(
                request.UserId,
                cancellationToken);

            if (wallet is null)
            {
                return Result<Guid>.Failure(
                    "کیف پول کاربر یافت نشد.",
                    ErrorType.NotFound);
            }

            if (order.Type == OrderType.Sell)
            {
                var lockResult = wallet.Lock(order.Weight, _clock?.Now.UtcDateTime);
                if (!lockResult.IsSuccess)
                {
                    return Result<Guid>.Failure(
                        lockResult.Message!,
                        lockResult.ErrorType);
                }

                _walletRepository.Update(wallet);
            }

            await _orderRepository.AddAsync(order, cancellationToken);

            var saveResult = await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            if (!saveResult.IsSuccess)
            {
                return Result<Guid>.Failure(
                    saveResult.Message!,
                    saveResult.ErrorType);
            }

            return Result<Guid>.Success(order.Id);
        }, cancellationToken);
    }

    public async Task<Result> CancelOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return await UnitOfWorkOperation.ExecuteAsync(_unitOfWork, async () =>
        {
            var order = await _orderRepository.GetByIdAsync(
                orderId,
                cancellationToken);

            if (order is null)
            {
                return Result.Failure(
                    "سفارش مورد نظر یافت نشد.",
                    ErrorType.NotFound);
            }

            var cancelResult = order.Cancel();
            if (!cancelResult.IsSuccess)
                return cancelResult;

            if (order.Type == OrderType.Sell)
            {
                var wallet = await _walletRepository.GetByUserIdAsync(
                    order.UserId,
                    cancellationToken);

                if (wallet is null)
                {
                    return Result.Failure(
                        "کیف پول کاربر یافت نشد.",
                        ErrorType.NotFound);
                }

                var releaseResult = wallet.ReleaseLocked(
                    order.Weight,
                    isSettled: false,
                    occurredAt: _clock?.Now.UtcDateTime);

                if (!releaseResult.IsSuccess)
                    return releaseResult;

                _walletRepository.Update(wallet);
            }

            _orderRepository.Update(order);

            var saveResult = await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            return saveResult;
        }, cancellationToken);
    }

    public async Task<Result> CompleteOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return await UnitOfWorkOperation.ExecuteAsync(_unitOfWork, async () =>
        {
            var order = await _orderRepository.GetByIdAsync(
                orderId,
                cancellationToken);

            if (order is null)
            {
                return Result.Failure(
                    "سفارش مورد نظر یافت نشد.",
                    ErrorType.NotFound);
            }

            var wallet = await _walletRepository.GetByUserIdAsync(
                order.UserId,
                cancellationToken);

            if (wallet is null)
            {
                return Result.Failure(
                    "کیف پول کاربر یافت نشد.",
                    ErrorType.NotFound);
            }

            var completeResult = order.Complete(_clock?.Now.UtcDateTime);
            if (!completeResult.IsSuccess)
                return completeResult;

            if (order.Type == OrderType.Sell)
            {
                var releaseResult = wallet.ReleaseLocked(
                    order.Weight,
                    isSettled: true,
                    occurredAt: _clock?.Now.UtcDateTime);

                if (!releaseResult.IsSuccess)
                    return releaseResult;
            }
            else if (order.Type == OrderType.Buy)
            {
                var depositResult = wallet.Deposit(order.Weight, _clock?.Now.UtcDateTime);
                if (!depositResult.IsSuccess)
                    return depositResult;
            }

            _walletRepository.Update(wallet);
            _orderRepository.Update(order);

            var saveResult = await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            return saveResult;
        }, cancellationToken);
    }

    public async Task<Result<IEnumerable<GoldOrderResponseDto>>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var orders = await _orderRepository.GetByUserIdAsync(
            userId,
            cancellationToken);

        var response = orders.Select(MapToDto).ToList();

        return Result<IEnumerable<GoldOrderResponseDto>>.Success(response);
    }

    private GoldOrderResponseDto MapToDto(GoldOrder order)
    {
        return new GoldOrderResponseDto(
            order.Id,
            order.UserId,
            order.Type,
            order.Weight.Value,
            order.UnitPrice.Amount,
            order.FeePercent.Value,
            order.TotalAmount.Amount,
            order.Status,
            order.UserNote,
            order.Tags.Select(tag => tag.Value).ToList(),
            _clock?.ToLocalTime(order.CreatedAt)
                ?? new DateTimeOffset(DateTime.SpecifyKind(order.CreatedAt, DateTimeKind.Utc)));
    }
}
