using GoldLedger.Domain.Aggregates.GoldOrder;
using GoldLedger.Domain.ValueObjects;
using GoldLedger.Infrastructure.Repositories;

namespace GoldLedger.Tests.Infrastructure
{
    public class InMemoryGoldOrderRepositoryTests
    {
        [Fact]
        public async Task Add_And_GetByIdAsync_Should_Work_Correctly()
        {
            var repository = new InMemoryGoldOrderRepository();
            var userId = Guid.NewGuid();

            var orderResult = GoldOrder.Create(
                userId,
                OrderType.Buy,
                GoldWeight.Create(5m).Value!,
                Money.Create(1000m, "IRR").Value!,
                Percentage.Create(1m).Value!,
                Enumerable.Empty<Tag>(),
                "Test Order"
            );
            var order = orderResult.Value!;

            await repository.AddAsync(order);
            var retrievedOrder = await repository.GetByIdAsync(order.Id);

            Assert.NotNull(retrievedOrder);
            Assert.Equal(order.Id, retrievedOrder.Id);
            Assert.Equal(userId, retrievedOrder.UserId);
        }

        [Fact]
        public async Task GetByUserIdAsync_Should_Return_Only_User_Orders()
        {
            var repository = new InMemoryGoldOrderRepository();
            var user1 = Guid.NewGuid();
            var user2 = Guid.NewGuid();

            var order1 = GoldOrder.Create(user1, OrderType.Buy, GoldWeight.Create(2m).Value!, Money.Create(500m, "IRR").Value!, Percentage.Create(1m).Value!, Enumerable.Empty<Tag>()).Value!;
            var order2 = GoldOrder.Create(user1, OrderType.Sell, GoldWeight.Create(3m).Value!, Money.Create(600m, "IRR").Value!, Percentage.Create(1m).Value!, Enumerable.Empty<Tag>()).Value!;
            var order3 = GoldOrder.Create(user2, OrderType.Buy, GoldWeight.Create(4m).Value!, Money.Create(700m, "IRR").Value!, Percentage.Create(1m).Value!, Enumerable.Empty<Tag>()).Value!;

            await repository.AddAsync(order1);
            await repository.AddAsync(order2);
            await repository.AddAsync(order3);

            var user1Orders = (await repository.GetByUserIdAsync(user1)).ToList();

            Assert.Equal(2, user1Orders.Count);
            Assert.Contains(user1Orders, o => o.Id == order1.Id);
            Assert.Contains(user1Orders, o => o.Id == order2.Id);
            Assert.DoesNotContain(user1Orders, o => o.Id == order3.Id);
        }

        [Fact]
        public async Task Update_Should_Modify_Existing_Order()
        {
            var repository = new InMemoryGoldOrderRepository();
            var userId = Guid.NewGuid();
            var order = GoldOrder.Create(userId, OrderType.Buy, GoldWeight.Create(5m).Value!, Money.Create(1000m, "IRR").Value!, Percentage.Create(1m).Value!, Enumerable.Empty<Tag>()).Value!;

            await repository.AddAsync(order);

            order.Cancel();
            repository.Update(order);

            var updatedOrder = await repository.GetByIdAsync(order.Id);

            Assert.NotNull(updatedOrder);
            Assert.Equal(OrderStatus.Cancelled, updatedOrder.Status);
        }
    }
}
