using GoldLedger.Domain.Aggregates.GoldWallet;

namespace GoldLedger.Domain.Repositories;

public interface IGoldWalletRepository
{
    public Task<GoldWallet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    public Task<GoldWallet?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    public Task AddAsync(GoldWallet wallet, CancellationToken cancellationToken = default);
    public void Update(GoldWallet wallet);
}

