using GoldLedger.API;
using GoldLedger.Application.DTOs.Orders;
using GoldLedger.Application.DTOs.Wallets;
using GoldLedger.Domain.Aggregates.GoldOrder;
using GoldLedger.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net.Http.Json;
using System.Net;
using System.Text.Json;

namespace GoldLedger.Tests.API;

public sealed class GoldOrdersControllerIntegrationTests : IClassFixture<LedgerApiFactory>
{
    private readonly HttpClient httpClient;
    public GoldOrdersControllerIntegrationTests(LedgerApiFactory factory)
    {
        httpClient = factory.CreateDefaultClient(new IdempotencyKeyHandler());
    }

    [Fact]
    public async Task Create_buy_order_returns_created_and_can_be_read()
    {
        var userId = Guid.NewGuid();
        await DepositAsync(userId, 1m);
        var orderId = await CreateOrderAsync(userId, OrderType.Buy, 1m);
        var response = await httpClient.GetAsync($"/api/orders/{orderId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(orderId, body.GetProperty("value").GetProperty("id").GetGuid());
        Assert.Equal((int)OrderStatus.Pending, body.GetProperty("value").GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Create_sell_order_without_wallet_returns_not_found()
    {
        var request = CreateOrderRequest(Guid.NewGuid(), OrderType.Sell, 1m);
        var response = await httpClient.PostAsJsonAsync("/api/orders", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_sell_order_locks_gold_until_completion()
    {
        var userId = Guid.NewGuid();
        await DepositAsync(userId, 5m);
        var orderId = await CreateOrderAsync(userId, OrderType.Sell, 2m);
        var walletResponse = await httpClient.GetAsync($"/api/wallets/user/{userId}");
        var walletBody = await walletResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(3m, walletBody.GetProperty("value").GetProperty("availableBalanceValue").GetDecimal());
        Assert.Equal(HttpStatusCode.OK, (await httpClient.PatchAsync($"/api/orders/{orderId}/complete", null)).StatusCode);
    }

    [Fact]
    public async Task Cancel_sell_order_releases_locked_gold()
    {
        var userId = Guid.NewGuid();
        await DepositAsync(userId, 5m);
        var orderId = await CreateOrderAsync(userId, OrderType.Sell, 2m);
        Assert.Equal(HttpStatusCode.OK, (await httpClient.PatchAsync($"/api/orders/{orderId}/cancel", null)).StatusCode);
        var walletBody = await (await httpClient.GetAsync($"/api/wallets/user/{userId}")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(5m, walletBody.GetProperty("value").GetProperty("availableBalanceValue").GetDecimal());
    }

    [Fact]
    public async Task Complete_unknown_order_returns_not_found()
    {
        var response = await httpClient.PatchAsync($"/api/orders/{Guid.NewGuid()}/complete", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_user_orders_returns_created_order()
    {
        var userId = Guid.NewGuid();
        await DepositAsync(userId, 2m);
        await CreateOrderAsync(userId, OrderType.Buy, 1m);
        var response = await httpClient.GetAsync($"/api/orders/user/{userId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("value").GetArrayLength() >= 1);
    }

    private async Task DepositAsync(Guid userId, decimal amount)
    {
        var response = await httpClient.PostAsJsonAsync($"/api/wallets/user/{userId}/deposit", new DepositRequestDto(amount));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private async Task<Guid> CreateOrderAsync(Guid userId, OrderType orderType, decimal weight)
    {
        var response = await httpClient.PostAsJsonAsync("/api/orders", CreateOrderRequest(userId, orderType, weight));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("value").GetGuid();
    }

    private static CreateOrderRequestDto CreateOrderRequest(Guid userId, OrderType orderType, decimal weight)
    {
        return new CreateOrderRequestDto(userId, orderType, weight, 1000m, 1m, ["api-test"], "endpoint test");
    }
}
