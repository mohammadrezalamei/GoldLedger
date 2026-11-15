using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GoldLedger.Application.DTOs.Orders;
using GoldLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GoldLedger.Tests.API;

public sealed class IdempotencyEndpointTests(LedgerApiFactory factory) : IClassFixture<LedgerApiFactory>
{
    [Fact]
    public async Task Mutation_without_key_is_rejected_without_creating_wallet()
    {
        using var client = factory.CreateClient();
        var userId = Guid.NewGuid();

        var response = await SendAsync(client, HttpMethod.Post, DepositPath(userId), new { amount = 3m });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(WalletPath(userId))).StatusCode);
    }

    [Theory]
    [InlineData("invalid key")]
    [InlineData("key/part")]
    [InlineData("first,second")]
    public async Task Invalid_key_is_rejected_before_business_operation(string key)
    {
        using var client = factory.CreateClient();
        var response = await SendAsync(client, HttpMethod.Post, DepositPath(Guid.NewGuid()), new { amount = 3m }, key);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Repeated_deposit_returns_identical_response_and_changes_balance_once()
    {
        using var client = factory.CreateClient();
        var userId = Guid.NewGuid();
        var key = NewKey();
        var first = await SendAsync(client, HttpMethod.Post, DepositPath(userId), new { amount = 3m }, key);
        var repeated = await SendAsync(client, HttpMethod.Post, DepositPath(userId), new { amount = 3m }, key);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(first.StatusCode, repeated.StatusCode);
        Assert.Equal(await first.Content.ReadAsStringAsync(), await repeated.Content.ReadAsStringAsync());
        Assert.Equal("false", first.Headers.GetValues("Idempotency-Replayed").Single());
        Assert.Equal("true", repeated.Headers.GetValues("Idempotency-Replayed").Single());
        Assert.Equal(3m, await BalanceAsync(client, userId));
    }

    [Fact]
    public async Task Repeated_withdrawal_debits_only_once()
    {
        using var client = factory.CreateClient();
        var userId = Guid.NewGuid();
        await DepositAsync(client, userId, 5m);
        var key = NewKey();
        var path = $"{WalletPath(userId)}/withdraw";

        var first = await SendAsync(client, HttpMethod.Post, path, new { amount = 2m }, key);
        var repeated = await SendAsync(client, HttpMethod.Post, path, new { amount = 2m }, key);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(await first.Content.ReadAsStringAsync(), await repeated.Content.ReadAsStringAsync());
        Assert.Equal(3m, await BalanceAsync(client, userId));
    }

    [Fact]
    public async Task Repeated_create_returns_same_order_and_reserves_gold_once()
    {
        using var client = factory.CreateClient();
        var userId = Guid.NewGuid();
        await DepositAsync(client, userId, 5m);
        var request = OrderRequest(userId);
        var key = NewKey();

        var first = await SendAsync(client, HttpMethod.Post, "/api/orders", request, key);
        var repeated = await SendAsync(client, HttpMethod.Post, "/api/orders", request, key);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(await first.Content.ReadAsStringAsync(), await repeated.Content.ReadAsStringAsync());
        var orders = await client.GetFromJsonAsync<JsonElement>($"/api/orders/user/{userId}");
        Assert.Equal(1, orders.GetProperty("value").GetArrayLength());
        var wallet = await client.GetFromJsonAsync<JsonElement>(WalletPath(userId));
        Assert.Equal(1m, wallet.GetProperty("value").GetProperty("lockedBalanceValue").GetDecimal());
    }

    [Theory]
    [InlineData("complete", 4)]
    [InlineData("cancel", 5)]
    public async Task Repeated_order_transition_returns_original_success_without_reapplying_effects(
        string transition, decimal expectedBalance)
    {
        using var client = factory.CreateClient();
        var userId = Guid.NewGuid();
        await DepositAsync(client, userId, 5m);
        var created = await SendAsync(client, HttpMethod.Post, "/api/orders", OrderRequest(userId), NewKey());
        var body = await created.Content.ReadFromJsonAsync<JsonElement>();
        var orderId = body.GetProperty("value").GetGuid();
        var key = NewKey();
        var path = $"/api/orders/{orderId}/{transition}";

        var first = await SendAsync(client, HttpMethod.Patch, path, null, key);
        var repeated = await SendAsync(client, HttpMethod.Patch, path, null, key);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(first.StatusCode, repeated.StatusCode);
        Assert.Equal(await first.Content.ReadAsStringAsync(), await repeated.Content.ReadAsStringAsync());
        Assert.Equal(expectedBalance, await BalanceAsync(client, userId));
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GoldLedgerDbContext>();
        Assert.Equal(transition == "complete" ? 1 : 0,
            await context.PaymentSettlements.CountAsync(settlement => settlement.OrderId == orderId));
    }

    [Fact]
    public async Task Same_key_with_changed_amount_returns_conflict_without_second_deposit()
    {
        using var client = factory.CreateClient();
        var userId = Guid.NewGuid();
        var key = NewKey();
        await SendAsync(client, HttpMethod.Post, DepositPath(userId), new { amount = 3m }, key);

        var conflict = await SendAsync(client, HttpMethod.Post, DepositPath(userId), new { amount = 4m }, key);

        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal(3m, await BalanceAsync(client, userId));
    }

    [Fact]
    public async Task Same_key_for_another_user_does_not_replay_or_create_their_wallet()
    {
        using var client = factory.CreateClient();
        var firstUser = Guid.NewGuid();
        var otherUser = Guid.NewGuid();
        var key = NewKey();
        await SendAsync(client, HttpMethod.Post, DepositPath(firstUser), new { amount = 3m }, key);

        var conflict = await SendAsync(client, HttpMethod.Post, DepositPath(otherUser), new { amount = 3m }, key);

        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(WalletPath(otherUser))).StatusCode);
    }

    [Fact]
    public async Task Failed_request_is_not_cached_and_can_succeed_after_balance_is_funded()
    {
        using var client = factory.CreateClient();
        var userId = Guid.NewGuid();
        await DepositAsync(client, userId, 1m);
        var key = NewKey();
        var path = $"{WalletPath(userId)}/withdraw";
        var failed = await SendAsync(client, HttpMethod.Post, path, new { amount = 2m }, key);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, failed.StatusCode);
        await DepositAsync(client, userId, 3m);

        var retried = await SendAsync(client, HttpMethod.Post, path, new { amount = 2m }, key);

        Assert.Equal(HttpStatusCode.OK, retried.StatusCode);
        Assert.Equal("false", retried.Headers.GetValues("Idempotency-Replayed").Single());
        Assert.Equal(2m, await BalanceAsync(client, userId));
    }

    [Fact]
    public async Task Parallel_requests_with_same_key_commit_exactly_once()
    {
        using var client = factory.CreateClient();
        var userId = Guid.NewGuid();
        var key = NewKey();
        var requests = Enumerable.Range(0, 6).Select(requestNumber => Task.Run(async () =>
        {
            return await SendAsync(client, HttpMethod.Post, DepositPath(userId), new { amount = 3m }, key);
        }));

        var responses = await Task.WhenAll(requests);

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Created, response.StatusCode));
        Assert.Single(responses, response => response.Headers.GetValues("Idempotency-Replayed").Single() == "false");
        Assert.Equal(3m, await BalanceAsync(client, userId));
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GoldLedgerDbContext>();
        Assert.Equal(1, await context.IdempotencyRecords.CountAsync(record => record.Key == key));
    }

    [Fact]
    public async Task Parallel_independent_deposits_do_not_lose_updates()
    {
        using var client = factory.CreateClient();
        var userId = Guid.NewGuid();
        var requests = Enumerable.Range(0, 6).Select(requestNumber => Task.Run(async () =>
        {
            return await SendAsync(client, HttpMethod.Post, DepositPath(userId), new { amount = 1m }, NewKey());
        }));

        var responses = await Task.WhenAll(requests);

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Created, response.StatusCode));
        Assert.Equal(6m, await BalanceAsync(client, userId));
    }

    [Fact]
    public async Task Swagger_declares_required_key_for_mutations_only()
    {
        using var client = factory.CreateClient();
        var document = await client.GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");
        var paths = document.GetProperty("paths");
        var create = paths.GetProperty("/api/orders").GetProperty("post");
        var header = create.GetProperty("parameters").EnumerateArray()
            .Single(parameter => parameter.GetProperty("name").GetString() == "Idempotency-Key");

        Assert.True(header.GetProperty("required").GetBoolean());
        Assert.True(create.GetProperty("responses").TryGetProperty("409", out _));
        var getOrder = paths.GetProperty("/api/orders/{id}").GetProperty("get");
        Assert.DoesNotContain(getOrder.GetProperty("parameters").EnumerateArray(),
            parameter => parameter.GetProperty("name").GetString() == "Idempotency-Key");
    }

    [Fact]
    public async Task Parallel_withdrawals_cannot_spend_the_same_available_gold()
    {
        using var client = factory.CreateClient();
        var userId = Guid.NewGuid();
        await DepositAsync(client, userId, 1m);
        var requests = Enumerable.Range(0, 4).Select(requestNumber => Task.Run(async () =>
        {
            return await SendAsync(client, HttpMethod.Post, $"{WalletPath(userId)}/withdraw", new { amount = 1m }, NewKey());
        }));

        var responses = await Task.WhenAll(requests);

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Equal(3, responses.Count(response => response.StatusCode == HttpStatusCode.UnprocessableEntity));
        Assert.Equal(0m, await BalanceAsync(client, userId));
    }

    [Fact]
    public async Task Parallel_same_key_with_different_payloads_commits_one_and_rejects_the_other()
    {
        using var client = factory.CreateClient();
        var userId = Guid.NewGuid();
        var key = NewKey();
        var responses = await Task.WhenAll(
            Task.Run(() => SendAsync(client, HttpMethod.Post, DepositPath(userId), new { amount = 2m }, key)),
            Task.Run(() => SendAsync(client, HttpMethod.Post, DepositPath(userId), new { amount = 3m }, key)));

        var accepted = Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
        var body = await accepted.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(body.GetProperty("value").GetProperty("balanceValue").GetDecimal(), await BalanceAsync(client, userId));
    }

    private static async Task<HttpResponseMessage> SendAsync(
        HttpClient client, HttpMethod method, string path, object? body, string? key = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (key is not null)
            request.Headers.Add("Idempotency-Key", key);
        if (body is not null)
            request.Content = JsonContent.Create(body);
        return await client.SendAsync(request);
    }

    private static async Task DepositAsync(HttpClient client, Guid userId, decimal amount)
    {
        var response = await SendAsync(client, HttpMethod.Post, DepositPath(userId), new { amount }, NewKey());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task<decimal> BalanceAsync(HttpClient client, Guid userId)
    {
        var body = await client.GetFromJsonAsync<JsonElement>(WalletPath(userId));
        return body.GetProperty("value").GetProperty("balanceValue").GetDecimal();
    }

    private static string NewKey()
    {
        return Guid.NewGuid().ToString("N");
    }

    private static string WalletPath(Guid userId)
    {
        return $"/api/wallets/user/{userId}";
    }

    private static string DepositPath(Guid userId)
    {
        return $"{WalletPath(userId)}/deposit";
    }

    private static CreateOrderRequestDto OrderRequest(Guid userId)
    {
        return new CreateOrderRequestDto(userId, OrderType.Sell, 1m, 100m, 1m, [], null);
    }
}
