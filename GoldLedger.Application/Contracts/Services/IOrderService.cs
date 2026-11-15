using GoldLedger.Application.DTOs.Orders;
using GoldLedger.Domain.Common;

namespace GoldLedger.Application.Contracts.Services;

public interface IOrderService
{
    public Task<Result<GoldOrderResponseDto>> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    public Task<Result<Guid>> CreateOrderAsync(CreateOrderRequestDto request, CancellationToken cancellationToken = default);
    public Task<Result> CancelOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
    public Task<Result> CompleteOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
    public Task<Result<IEnumerable<GoldOrderResponseDto>>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}

