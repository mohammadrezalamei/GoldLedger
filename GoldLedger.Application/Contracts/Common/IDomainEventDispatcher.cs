using GoldLedger.Domain.Common;

namespace GoldLedger.Application.Contracts.Common;

public interface IDomainEventDispatcher
{
    Task<Result> DispatchAsync(
        IReadOnlyCollection<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default);
}
