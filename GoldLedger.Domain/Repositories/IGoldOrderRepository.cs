using GoldLedger.Domain.Aggregates.GoldOrder;

namespace GoldLedger.Domain.Repositories;

public interface IGoldOrderRepository
{
    public Task<GoldOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    public Task AddAsync(GoldOrder order, CancellationToken cancellationToken = default);
    public Task<IEnumerable<GoldOrder>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    public void Update(GoldOrder order);
}