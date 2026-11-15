using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GoldLedger.Application.DTOs.Orders;
using GoldLedger.Application.DTOs.Wallets;
using GoldLedger.Domain.Aggregates.GoldOrder;
using GoldLedger.API;
using Microsoft.AspNetCore.Mvc.Testing;
using GoldLedger.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
namespace GoldLedger.Tests.API;
public sealed class LedgerApiEndpointTests : IClassFixture<LedgerApiFactory>
{
    private readonly HttpClient httpClient;
    public LedgerApiEndpointTests(LedgerApiFactory factory)
    {
        httpClient = factory.CreateDefaultClient(new IdempotencyKeyHandler());
    }
    [Fact]
    public async Task Get_missing_wallet_returns_not_found()
    {
        var userId = Guid.NewGuid();
        var response = await httpClient.GetAsync($"/api/wallets/user/{userId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
    [Fact]
    public async Task Deposit_creates_wallet_and_returns_created()
    {
        var userId = Guid.NewGuid();
        var response = await httpClient.PostAsJsonAsync($"/api/wallets/user/{userId}/deposit", new DepositRequestDto(10m));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("isSuccess").GetBoolean());
        Assert.Equal(10m, body.GetProperty("value").GetProperty("balanceValue").GetDecimal());
    }
    [Fact]
    public async Task Deposit_negative_amount_returns_unprocessable_entity()
    {
        var response = await httpClient.PostAsJsonAsync($"/api/wallets/user/{Guid.NewGuid()}/deposit", new DepositRequestDto(-1m));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
    [Fact]
    public async Task Withdraw_updates_wallet_balance()
    {
        var userId = Guid.NewGuid();
        await DepositAsync(userId, 10m);
        var response = await httpClient.PostAsJsonAsync($"/api/wallets/user/{userId}/withdraw", new WithdrawRequestDto(4m));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(6m, body.GetProperty("value").GetProperty("availableBalanceValue").GetDecimal());
    }
    [Fact]
    public async Task Withdraw_without_sufficient_balance_returns_unprocessable_entity()
    {
        var userId = Guid.NewGuid();
        await DepositAsync(userId, 2m);
        var response = await httpClient.PostAsJsonAsync($"/api/wallets/user/{userId}/withdraw", new WithdrawRequestDto(3m));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
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

public sealed class LedgerApiFactory : WebApplicationFactory<Program>
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"goldledger-api-{Guid.NewGuid():N}.db");
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:GoldLedger", $"Data Source={databasePath};Pooling=False");
    }
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GoldLedgerDbContext>();
        context.Database.Migrate();
        return host;
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && File.Exists(databasePath))
            File.Delete(databasePath);
    }
}



internal sealed class IdempotencyKeyHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.Method != HttpMethod.Get && !request.Headers.Contains("Idempotency-Key"))
            request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));

        return base.SendAsync(request, cancellationToken);
    }
}
