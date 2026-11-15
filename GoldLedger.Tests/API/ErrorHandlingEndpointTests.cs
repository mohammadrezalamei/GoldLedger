using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using GoldLedger.Application.Contracts.Services;
using GoldLedger.Domain.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace GoldLedger.Tests.API;

public sealed class ErrorHandlingEndpointTests
{
    [Theory]
    [InlineData("unexpected", 500, ErrorType.ServerError)]
    [InlineData("concurrency", 409, ErrorType.Conflict)]
    [InlineData("locked", 409, ErrorType.Conflict)]
    [InlineData("constraint", 409, ErrorType.Conflict)]
    public async Task Read_failures_return_safe_consistent_envelope(
        string failureKind, int expectedStatus, ErrorType expectedErrorType)
    {
        Exception failure = failureKind switch
        {
            "concurrency" => new DbUpdateConcurrencyException("private details"),
            "locked" => new SqliteException("private details", 5),
            "constraint" => new DbUpdateException("private details", new SqliteException("private details", 19, 2067)),
            _ => new InvalidOperationException("private details")
        };
        var walletService = new Mock<IWalletService>();
        walletService.Setup(service => service.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(failure);
        using var factory = new LedgerApiFactory();
        using var failingApplication = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IWalletService>();
                services.AddSingleton(walletService.Object);
            });
        });
        using var client = failingApplication.CreateClient();

        var response = await client.GetAsync($"/api/wallets/user/{Guid.NewGuid()}");

        Assert.Equal(expectedStatus, (int)response.StatusCode);
        var bodyText = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("private details", bodyText);
        Assert.DoesNotContain("StackTrace", bodyText);
        var body = JsonSerializer.Deserialize<JsonElement>(bodyText);
        Assert.False(body.GetProperty("isSuccess").GetBoolean());
        Assert.Equal((int)expectedErrorType, body.GetProperty("errorType").GetInt32());
        Assert.True(response.Headers.Contains("X-Trace-Id"));
    }

    [Fact]
    public async Task Malformed_json_returns_validation_envelope_and_does_not_persist_key()
    {
        using var factory = new LedgerApiFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/wallets/user/{Guid.NewGuid()}/deposit");
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        request.Content = new StringContent("{ invalid", Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(body.GetProperty("isSuccess").GetBoolean());
        Assert.Equal((int)ErrorType.Validation, body.GetProperty("errorType").GetInt32());
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GoldLedger.Infrastructure.Persistence.GoldLedgerDbContext>();
        Assert.Empty(await context.IdempotencyRecords.ToListAsync());
    }
}
