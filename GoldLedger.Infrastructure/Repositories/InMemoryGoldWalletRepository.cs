using System.Collections.Concurrent;
using GoldLedger.Domain.Aggregates.GoldWallet;
using GoldLedger.Domain.Repositories;

namespace GoldLedger.Infrastructure.Repositories;

public class InMemoryGoldWalletRepository : IGoldWalletRepository
{
    private readonly ConcurrentDictionary<Guid, GoldWallet> _wallets = new();

    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> _userLocks = new();

    public Task<GoldWallet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var wallet = _wallets.Values.FirstOrDefault(w => w.Id == id);
        return Task.FromResult(wallet);
    }

    public async Task<GoldWallet?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var userLock = _userLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
        await userLock.WaitAsync(cancellationToken);
        try
        {
            var wallet = _wallets.Values.FirstOrDefault(w => w.UserId == userId);
            return wallet;
        }
        finally
        {
            userLock.Release();
        }
    }

    public async Task AddAsync(GoldWallet wallet, CancellationToken cancellationToken = default)
    {
        var userLock = _userLocks.GetOrAdd(wallet.UserId, _ => new SemaphoreSlim(1, 1));
        await userLock.WaitAsync(cancellationToken);
        try
        {
            _wallets[wallet.Id] = wallet;
        }
        finally
        {
            userLock.Release();
        }
    }

    public void Update(GoldWallet wallet)
    {
        var userLock = _userLocks.GetOrAdd(wallet.UserId, _ => new SemaphoreSlim(1, 1));
        userLock.Wait();
        try
        {
            _wallets[wallet.Id] = wallet;
        }
        finally
        {
            userLock.Release();
        }
    }
}