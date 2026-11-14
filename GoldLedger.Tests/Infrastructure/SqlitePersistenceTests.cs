using GoldLedger.Domain.Aggregates.GoldWallet;
using GoldLedger.Domain.ValueObjects;
using GoldLedger.Infrastructure.Persistence;
using GoldLedger.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
namespace GoldLedger.Tests.Infrastructure;
public sealed class SqlitePersistenceTests
{
 [Fact] public async Task Wallet_round_trips_through_sqlite(){await using var connection=new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync(); var options=new DbContextOptionsBuilder<GoldLedgerDbContext>().UseSqlite(connection).Options; await using(var setup=new GoldLedgerDbContext(options)){await setup.Database.EnsureCreatedAsync(); var repo=new GoldWalletRepository(setup); var wallet=GoldWallet.Create(Guid.NewGuid()).Value!; wallet.Deposit(GoldWeight.Create(3).Value!); await repo.AddAsync(wallet); await setup.SaveChangesAsync();} await using(var read=new GoldLedgerDbContext(options)){var wallet=await read.Wallets.SingleAsync(); Assert.Equal(3m,wallet.Balance.Value);}}
}
