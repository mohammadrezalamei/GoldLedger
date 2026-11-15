using GoldLedger.Application.Contracts.Common;
using GoldLedger.Application.Contracts.Services;
using GoldLedger.Application.DTOs.Orders;
using GoldLedger.Domain.Aggregates.GoldOrder;
using GoldLedger.Domain.Aggregates.GoldWallet;
using GoldLedger.Domain.Common;
using GoldLedger.Domain.Repositories;
using GoldLedger.Domain.ValueObjects;

namespace GoldLedger.Application.Services;

public class OrderService : IOrderService
{
    private readonly IGoldOrderRepository _orderRepository;
    private readonly IGoldWalletRepository _walletRepository;
    private readonly IUnitOfWork _unitOfWork;

    public OrderService(
        IGoldOrderRepository orderRepository,
        IGoldWalletRepository walletRepository,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _walletRepository = walletRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<GoldOrderResponseDto>> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order is null)
            return Result<GoldOrderResponseDto>.Failure("Ø³ÙØ§Ø±Ø´ Ù…ÙˆØ±Ø¯ Ù†Ø¸Ø± ÛŒØ§ÙØª Ù†Ø´Ø¯.", ErrorType.NotFound);

        var response = new GoldOrderResponseDto(
            order.Id,
            order.UserId,
            order.Type,
            order.Weight.Value,
            order.UnitPrice.Amount,
            order.FeePercent.Value,
            order.TotalAmount.Amount,
            order.Status,
            order.UserNote,
            order.Tags.Select(t => t.Value),
            order.CreatedAt
        );

        return Result<GoldOrderResponseDto>.Success(response);
    }

    public async Task<Result<Guid>> CreateOrderAsync(CreateOrderRequestDto request, CancellationToken cancellationToken = default)
    {
        var weightResult = GoldWeight.Create(request.WeightValue);
        if (!weightResult.IsSuccess)
            return Result<Guid>.Failure(weightResult.Message!, weightResult.ErrorType);

        var priceResult = Money.Create(request.UnitPriceAmount);
        if (!priceResult.IsSuccess)
            return Result<Guid>.Failure(priceResult.Message!, priceResult.ErrorType);

        var feeResult = Percentage.Create(request.FeePercent);
        if (!feeResult.IsSuccess)
            return Result<Guid>.Failure(feeResult.Message!, feeResult.ErrorType);

        var tags = new List<Tag>();
        foreach (var rawTag in request.Tags ?? Enumerable.Empty<string>())
        {
            var tagResult = Tag.Create(rawTag);
            if (!tagResult.IsSuccess)
                return Result<Guid>.Failure(tagResult.Message!, tagResult.ErrorType);
            tags.Add(tagResult.Value!);
        }

        var orderResult = GoldOrder.Create(
            request.UserId,
            request.Type,
            weightResult.Value!,
            priceResult.Value!,
            feeResult.Value!,
            tags,
            request.UserNote
        );

        if (!orderResult.IsSuccess)
            return Result<Guid>.Failure(orderResult.Message!, orderResult.ErrorType);

        var order = orderResult.Value!;

        var wallet = await _walletRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (wallet is null)
            return Result<Guid>.Failure("Ú©ÛŒÙ Ù¾ÙˆÙ„ Ú©Ø§Ø±Ø¨Ø± ÛŒØ§ÙØª Ù†Ø´Ø¯.", ErrorType.NotFound);

        if (order.Type == OrderType.Sell)
        {
            var lockResult = wallet.Lock(order.Weight);
            if (!lockResult.IsSuccess)
                return Result<Guid>.Failure(lockResult.Message!, lockResult.ErrorType);

            _walletRepository.Update(wallet);
        }

        await _orderRepository.AddAsync(order, cancellationToken);

        var saveResult = await _unitOfWork.SaveChangesAsync(cancellationToken);
        if (!saveResult.IsSuccess)
            return Result<Guid>.Failure(saveResult.Message!, saveResult.ErrorType);

        return Result<Guid>.Success(order.Id);
    }

    public async Task<Result> CancelOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order is null)
            return Result.Failure("Ø³ÙØ§Ø±Ø´ Ù…ÙˆØ±Ø¯ Ù†Ø¸Ø± ÛŒØ§ÙØª Ù†Ø´Ø¯.", ErrorType.NotFound);

        var cancelResult = order.Cancel();
        if (!cancelResult.IsSuccess)
            return cancelResult;

        if (order.Type == OrderType.Sell)
        {
            var wallet = await _walletRepository.GetByUserIdAsync(order.UserId, cancellationToken);
            if (wallet is not null)
            {
                var releaseResult = wallet.ReleaseLocked(order.Weight, isSettled: false);
                if (!releaseResult.IsSuccess)
                    return Result.Failure(releaseResult.Message!, releaseResult.ErrorType);

                _walletRepository.Update(wallet);
            }
        }

        _orderRepository.Update(order);
        return await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<Result> CompleteOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order is null)
            return Result.Failure("Ø³ÙØ§Ø±Ø´ Ù…ÙˆØ±Ø¯ Ù†Ø¸Ø± ÛŒØ§ÙØª Ù†Ø´Ø¯.", ErrorType.NotFound);

        var wallet = await _walletRepository.GetByUserIdAsync(order.UserId, cancellationToken);
        if (wallet is null)
            return Result.Failure("Ú©ÛŒÙ Ù¾ÙˆÙ„ Ú©Ø§Ø±Ø¨Ø± ÛŒØ§ÙØª Ù†Ø´Ø¯.", ErrorType.NotFound);

        var completeResult = order.Complete();
        if (!completeResult.IsSuccess)
            return completeResult;

        if (order.Type == OrderType.Sell)
        {
            var releaseResult = wallet.ReleaseLocked(order.Weight, isSettled: true);
            if (!releaseResult.IsSuccess)
                return Result.Failure(releaseResult.Message!, releaseResult.ErrorType);
        }
        else if (order.Type == OrderType.Buy)
        {
            var depositResult = wallet.Deposit(order.Weight);
            if (!depositResult.IsSuccess)
                return Result.Failure(depositResult.Message!, depositResult.ErrorType);
        }

        _walletRepository.Update(wallet);
        _orderRepository.Update(order);

        return await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<Result<IEnumerable<GoldOrderResponseDto>>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var orders = await _orderRepository.GetByUserIdAsync(userId, cancellationToken);

        var response = orders.Select(order => new GoldOrderResponseDto(
            order.Id,
            order.UserId,
            order.Type,
            order.Weight.Value,
            order.UnitPrice.Amount,
            order.FeePercent.Value,
            order.TotalAmount.Amount,
            order.Status,
            order.UserNote,
            order.Tags.Select(t => t.Value),
            order.CreatedAt
        ));

        return Result<IEnumerable<GoldOrderResponseDto>>.Success(response);
    }
}



