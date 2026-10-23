using System.Collections.Concurrent;
using GoldLedger.Domain.Aggregates.GoldOrder;
using GoldLedger.Domain.Repositories;

namespace GoldLedger.Infrastructure.Repositories;

public class InMemoryGoldOrderRepository : IGoldOrderRepository
{
    private readonly ConcurrentDictionary<Guid, GoldOrder> _orders = new();

    public Task<GoldOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _orders.TryGetValue(id, out var order);
        return Task.FromResult(order);
    }

    public Task<IEnumerable<GoldOrder>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var userOrders = _orders.Values
            .Where(o => o.UserId == userId)
            .ToList() as IEnumerable<GoldOrder>;

        return Task.FromResult(userOrders ?? Enumerable.Empty<GoldOrder>());
    }

    public Task AddAsync(GoldOrder order, CancellationToken cancellationToken = default)
    {
        _orders[order.Id] = order;
        return Task.CompletedTask;
    }

    public void Update(GoldOrder order)
    {
        _orders[order.Id] = order;
    }
}