using GoldLedger.Domain.Aggregates.GoldWallet;
using GoldLedger.Domain.Repositories;
using GoldLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GoldLedger.Infrastructure.Repositories;

public sealed class GoldWalletRepository(GoldLedgerDbContext context) : IGoldWalletRepository
{
    public Task<GoldWallet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return context.Wallets.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<GoldWallet?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return context.Wallets.SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);
    }

    public Task AddAsync(GoldWallet wallet, CancellationToken cancellationToken = default)
    {
        return context.Wallets.AddAsync(wallet, cancellationToken).AsTask();
    }

    public void Update(GoldWallet wallet)
    {
        context.Wallets.Update(wallet);
    }
}


