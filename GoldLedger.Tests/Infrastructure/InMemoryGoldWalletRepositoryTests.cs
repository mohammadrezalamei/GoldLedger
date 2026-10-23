using GoldLedger.Domain.Aggregates.GoldOrder;
using GoldLedger.Domain.Aggregates.GoldWallet;
using GoldLedger.Domain.Common;
using GoldLedger.Domain.ValueObjects;
using GoldLedger.Infrastructure.Repositories;

namespace GoldLedger.Tests.Infrastructure
{
    public class InMemoryRepositoriesTests
    {
        [Fact]
        public async Task Wallet_Repository_Should_Add_And_Retrieve_Successfully()
        {
            var repository = new InMemoryGoldWalletRepository();
            var userId = Guid.NewGuid();
            var wallet = GoldWallet.Create(userId, GoldWeight.Create(50m).Value!).Value!;

            await repository.AddAsync(wallet);
            var fetchedWallet = await repository.GetByUserIdAsync(userId);
            var fetchedById = await repository.GetByIdAsync(wallet.Id);

            Assert.NotNull(fetchedWallet);
            Assert.Equal(wallet.Id, fetchedWallet.Id);
            Assert.NotNull(fetchedById);
            Assert.Equal(wallet.Id, fetchedById.Id);
        }

        [Fact]
        public async Task Wallet_Repository_Concurrent_Operations_Should_Be_Thread_Safe()
        {
            var repository = new InMemoryGoldWalletRepository();
            var userId = Guid.NewGuid();
            var initialWeight = GoldWeight.Create(100m).Value!;
            var wallet = GoldWallet.Create(userId, initialWeight).Value!;
            await repository.AddAsync(wallet);

            var tasks = Enumerable.Range(0, 50).Select(async _ =>
            {
                var currentWallet = await repository.GetByUserIdAsync(userId);
                if (currentWallet != null)
                {
                    currentWallet.Deposit(GoldWeight.Create(1m).Value!);
                    repository.Update(currentWallet);
                }
            });

            await Task.WhenAll(tasks);

            var finalWallet = await repository.GetByUserIdAsync(userId);
            Assert.NotNull(finalWallet);
            Assert.Equal(150m, finalWallet.Balance.Value);
        }

        [Fact]
        public async Task Order_Repository_Should_Store_And_Filter_By_UserId()
        {
            var repository = new InMemoryGoldOrderRepository();
            var userId1 = Guid.NewGuid();
            var userId2 = Guid.NewGuid();

            var order1 = GoldOrder.Create(
                userId1,
                OrderType.Buy,
                GoldWeight.Create(5m).Value!,
                Money.Create(1000m, "IRR").Value!,
                Percentage.Create(1m).Value!,
                Enumerable.Empty<Tag>()
            ).Value!;

            var order2 = GoldOrder.Create(
                userId2,
                OrderType.Sell,
                GoldWeight.Create(3m).Value!,
                Money.Create(1000m, "IRR").Value!,
                Percentage.Create(1m).Value!,
                Enumerable.Empty<Tag>()
            ).Value!;

            await repository.AddAsync(order1);
            await repository.AddAsync(order2);

            var user1Orders = (await repository.GetByUserIdAsync(userId1)).ToList();
            var fetchedOrder1 = await repository.GetByIdAsync(order1.Id);

            Assert.Single(user1Orders);
            Assert.Equal(order1.Id, user1Orders[0].Id);

            Assert.NotNull(fetchedOrder1);
            Assert.Equal(order1.Id, fetchedOrder1.Id);
        }
        [Fact]
        public async Task Withdraw_And_Update_Should_Reduce_Balance_Correctly()
        {
            var repository = new InMemoryGoldWalletRepository();
            var userId = Guid.NewGuid();
            var initialWallet = GoldWallet.Create(userId, GoldWeight.Create(50m).Value!).Value!;
            await repository.AddAsync(initialWallet);

            var wallet = await repository.GetByUserIdAsync(userId);
            Assert.NotNull(wallet);

            var withdrawResult = wallet.Withdraw(GoldWeight.Create(20m).Value!);
            Assert.True(withdrawResult.IsSuccess);

            repository.Update(wallet);

            var updatedWallet = await repository.GetByUserIdAsync(userId);
            Assert.NotNull(updatedWallet);
            Assert.Equal(30m, updatedWallet.Balance.Value);
        }

        [Fact]
        public async Task Withdraw_Should_Fail_When_Balance_Is_Not_Enough()
        {
            var repository = new InMemoryGoldWalletRepository();
            var userId = Guid.NewGuid();
            var initialWallet = GoldWallet.Create(userId, GoldWeight.Create(10m).Value!).Value!;
            await repository.AddAsync(initialWallet);

            var wallet = await repository.GetByUserIdAsync(userId);
            Assert.NotNull(wallet);

            var withdrawResult = wallet.Withdraw(GoldWeight.Create(20m).Value!);

            Assert.False(withdrawResult.IsSuccess);
            Assert.Equal(10m, wallet.Balance.Value);
        }
        [Fact]
        public async Task Concurrent_Withdraw_Should_Be_Thread_Safe_And_Respect_Balance()
        {
            var repository = new InMemoryGoldWalletRepository();
            var userId = Guid.NewGuid();
            var initialWallet = GoldWallet.Create(userId, GoldWeight.Create(10m).Value!).Value!;
            await repository.AddAsync(initialWallet);

            var tasks = Enumerable.Range(0, 5).Select(async _ =>
            {
                var wallet = await repository.GetByUserIdAsync(userId);
                if (wallet == null) return Result.Failure("Wallet not found", ErrorType.NotFound);

                var withdrawResult = wallet.Withdraw(GoldWeight.Create(6m).Value!);
                if (withdrawResult.IsSuccess)
                {
                    repository.Update(wallet);
                    return Result.Success();
                }

                return Result.Failure("Not enough balance", ErrorType.Validation);
            });

            var results = await Task.WhenAll(tasks);

            var finalWallet = await repository.GetByUserIdAsync(userId);
            Assert.NotNull(finalWallet);

            Assert.True(finalWallet.Balance.Value >= 0m);

            var successCount = results.Count(r => r.IsSuccess);
            Assert.Equal(1, successCount);
            Assert.Equal(4m, finalWallet.Balance.Value); 
        }

        [Fact]
        public async Task Per_User_Locks_Should_Not_Block_Different_Users()
        {
            var repository = new InMemoryGoldWalletRepository();
            var user1 = Guid.NewGuid();
            var user2 = Guid.NewGuid();

            await repository.AddAsync(GoldWallet.Create(user1, GoldWeight.Create(50m).Value!).Value!);
            await repository.AddAsync(GoldWallet.Create(user2, GoldWeight.Create(50m).Value!).Value!);

            var task1 = Task.Run(async () =>
            {
                for (int i = 0; i < 20; i++)
                {
                    var wallet = await repository.GetByUserIdAsync(user1);
                    wallet!.Deposit(GoldWeight.Create(1m).Value!);
                    repository.Update(wallet);
                }
            });

            var task2 = Task.Run(async () =>
            {
                for (int i = 0; i < 20; i++)
                {
                    var wallet = await repository.GetByUserIdAsync(user2);
                    wallet!.Deposit(GoldWeight.Create(1m).Value!);
                    repository.Update(wallet);
                }
            });

            await Task.WhenAll(task1, task2);

            var finalWallet1 = await repository.GetByUserIdAsync(user1);
            var finalWallet2 = await repository.GetByUserIdAsync(user2);

            Assert.Equal(70m, finalWallet1!.Balance.Value);
            Assert.Equal(70m, finalWallet2!.Balance.Value);
        }

        [Fact]
        public async Task Concurrent_Deposits_Without_Lock_Should_Fail_Due_To_Lost_Updates()
        {
            var repository = new InMemoryGoldWalletRepository();
            var userId = Guid.NewGuid();
            var initialWallet = GoldWallet.Create(userId, GoldWeight.Create(0m).Value!).Value!;
            await repository.AddAsync(initialWallet);

            var tasks = Enumerable.Range(0, 5000).Select(async _ =>
            {
                await Task.Yield();

                var wallet = await repository.GetByUserIdAsync(userId);
                if (wallet != null)
                {
                    wallet.Deposit(GoldWeight.Create(1m).Value!);
                    repository.Update(wallet);
                }
            });

            await Task.WhenAll(tasks);

            var finalWallet = await repository.GetByUserIdAsync(userId);
            Assert.NotNull(finalWallet);

            Assert.Equal(5000m, finalWallet.Balance.Value);
        }

        [Fact]
        public async Task Concurrent_Heavy_Withdrawals_Should_Never_Drop_Balance_Below_Zero()
        {
            var repository = new InMemoryGoldWalletRepository();
            var userId = Guid.NewGuid();
            var initialWallet = GoldWallet.Create(userId, GoldWeight.Create(5m).Value!).Value!;
            await repository.AddAsync(initialWallet);

            var tasks = Enumerable.Range(0, 5000).Select(async _ =>
            {
                var wallet = await repository.GetByUserIdAsync(userId);
                if (wallet != null)
                {
                    var withdrawResult = wallet.Withdraw(GoldWeight.Create(2m).Value!);
                    if (withdrawResult.IsSuccess)
                    {
                        repository.Update(wallet);
                    }
                }
            });

            await Task.WhenAll(tasks);

            var finalWallet = await repository.GetByUserIdAsync(userId);
            Assert.NotNull(finalWallet);

            Assert.True(finalWallet.Balance.Value >= 0m, "فاجعه! موجودی کیف پول منفی شد!");

            Assert.Equal(1m, finalWallet.Balance.Value);
        }

        [Fact]
        public async Task Concurrent_Withdrawals_Without_Lock_Should_Fail_Due_To_Race_Condition()
        {
            var repository = new InMemoryGoldWalletRepository();
            var userId = Guid.NewGuid();
            var initialWallet = GoldWallet.Create(userId, GoldWeight.Create(10m).Value!).Value!;
            await repository.AddAsync(initialWallet);


            var tasks = Enumerable.Range(0, 5000).Select(async _ =>
            {
                await Task.Yield();

                var wallet = await repository.GetByUserIdAsync(userId);
                if (wallet != null)
                {
                    var withdrawResult = wallet.Withdraw(GoldWeight.Create(2m).Value!);
                    if (withdrawResult.IsSuccess)
                    {
                        repository.Update(wallet);
                    }
                }
            });

            await Task.WhenAll(tasks);

            var finalWallet = await repository.GetByUserIdAsync(userId);
            Assert.NotNull(finalWallet);

            Assert.Equal(0m, finalWallet.Balance.Value);
        }
    }
}
