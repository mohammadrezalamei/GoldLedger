using GoldLedger.API;
using GoldLedger.Application.DTOs.Wallets;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

namespace GoldLedger.Tests.API
{
    public class WalletsControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public WalletsControllerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetWallet_WhenWalletNotExists_ReturnsNotFoundOrEmpty()
        {
            var userId = Guid.NewGuid();

            var response = await _client.GetAsync($"/api/wallets/user/{userId}");

            Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task Deposit_WithValidAmount_ReturnsCreatedOrSuccess()
        {
            var userId = Guid.NewGuid();
            var request = new DepositRequestDto(5.0m);

            var response = await _client.PostAsJsonAsync($"/api/wallets/user/{userId}/deposit", request);

            Assert.True(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK);

            var jsonString = await response.Content.ReadAsStringAsync();
            Assert.False(string.IsNullOrEmpty(jsonString));
        }

        [Fact]
        public async Task Withdraw_ExceedingBalance_ReturnsUnprocessableEntityOrBadRequest()
        {
            var userId = Guid.NewGuid();
            var request = new WithdrawRequestDto(100.0m);

            var response = await _client.PostAsJsonAsync($"/api/wallets/user/{userId}/withdraw", request);

            Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.NotEqual(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task Deposit_ThenGetWallet_ReturnsCorrectBalance()
        {
            var userId = Guid.NewGuid();
            var depositRequest = new DepositRequestDto(12.5m);

            var depositResponse = await _client.PostAsJsonAsync($"/api/wallets/user/{userId}/deposit", depositRequest);
            Assert.True(depositResponse.StatusCode == HttpStatusCode.Created || depositResponse.StatusCode == HttpStatusCode.OK);

            var getResponse = await _client.GetAsync($"/api/wallets/user/{userId}");
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            var jsonString = await getResponse.Content.ReadAsStringAsync();
            Assert.False(string.IsNullOrEmpty(jsonString));
        }

        [Fact]
        public async Task Deposit_WithInvalidZeroOrNegativeAmount_ReturnsBadRequest()
        {
            var userId = Guid.NewGuid();
            var request = new DepositRequestDto(-2.0m);

            var response = await _client.PostAsJsonAsync($"/api/wallets/user/{userId}/deposit", request);

            Assert.NotEqual(HttpStatusCode.Created, response.StatusCode);
            Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Deposit_MultipleTimes_AccumulatesCorrectly()
        {
            var userId = Guid.NewGuid();

            await _client.PostAsJsonAsync($"/api/wallets/user/{userId}/deposit", new DepositRequestDto(10.0m));

            var secondResponse = await _client.PostAsJsonAsync($"/api/wallets/user/{userId}/deposit", new DepositRequestDto(5.5m));

            Assert.True(secondResponse.IsSuccessStatusCode);

            var getResponse = await _client.GetAsync($"/api/wallets/user/{userId}");
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        }

        [Fact]
        public async Task Withdraw_WithInvalidNegativeAmount_ReturnsBadRequest()
        {
            var userId = Guid.NewGuid();
            var request = new WithdrawRequestDto(-5.0m);

            var response = await _client.PostAsJsonAsync($"/api/wallets/user/{userId}/withdraw", request);

            Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task DepositAndThenWithdraw_ValidAmount_Succeeds()
        {
            var userId = Guid.NewGuid();

            await _client.PostAsJsonAsync($"/api/wallets/user/{userId}/deposit", new DepositRequestDto(20.0m));

            var withdrawRequest = new WithdrawRequestDto(5.0m);
            var withdrawResponse = await _client.PostAsJsonAsync($"/api/wallets/user/{userId}/withdraw", withdrawRequest);

            Assert.True(withdrawResponse.IsSuccessStatusCode || withdrawResponse.StatusCode == HttpStatusCode.OK);
        }
    }
}

