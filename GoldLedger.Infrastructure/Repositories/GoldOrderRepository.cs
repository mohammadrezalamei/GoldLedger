using GoldLedger.Domain.Aggregates.GoldOrder;
using GoldLedger.Domain.Repositories;
using GoldLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GoldLedger.Infrastructure.Repositories;

public sealed class GoldOrderRepository(GoldLedgerDbContext context) : IGoldOrderRepository
{
    public Task<GoldOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => context.Orders.Include(x => x.Tags).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    public async Task<IEnumerable<GoldOrder>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) => await context.Orders.Include(x => x.Tags).Where(x => x.UserId == userId).OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
    public Task AddAsync(GoldOrder order, CancellationToken cancellationToken = default) => context.Orders.AddAsync(order, cancellationToken).AsTask();
    public void Update(GoldOrder order) => context.Orders.Update(order);
}
