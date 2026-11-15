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

public sealed class WalletsControllerIntegrationTests : IClassFixture<LedgerApiFactory>
{
    private readonly HttpClient httpClient;
    public WalletsControllerIntegrationTests(LedgerApiFactory factory)
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

    private async Task DepositAsync(Guid userId, decimal amount)
    {
        var response = await httpClient.PostAsJsonAsync($"/api/wallets/user/{userId}/deposit", new DepositRequestDto(amount));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
