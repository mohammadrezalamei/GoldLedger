using GoldLedger.API;
using GoldLedger.Application.DTOs.Orders;
using GoldLedger.Application.DTOs.Wallets;
using GoldLedger.Domain.Common;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

namespace GoldLedger.Tests.API
{
    public class GoldOrdersControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public GoldOrdersControllerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task CreateOrder_WithValidData_ReturnsCreatedOrSuccess()
        {
            var userId = Guid.NewGuid();

            // ۱. ابتدا باید برای کاربر کیف پول ایجاد شود (با یک واریز اولیه)
            var walletRequest = new DepositRequestDto(10.0m);
            var walletResponse = await _client.PostAsJsonAsync($"/api/wallets/user/{userId}/deposit", walletRequest);
            Assert.True(walletResponse.IsSuccessStatusCode);

            // ۲. حالا سفارش خرید را ثبت می‌کنیم
            var request = new CreateOrderRequestDto(
                UserId: userId,
                Type: OrderType.Buy,
                WeightValue: 2.5m,
                UnitPriceAmount: 4500000m,
                Currency: "IRR",
                FeePercent: 1.0m,
                Tags: new[] { "خرید-آنلاین" },
                UserNote: "تست ثبت سفارش خرید طلا"
            );

            var response = await _client.PostAsJsonAsync("/api/orders", request);

            Assert.True(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK);

            var jsonString = await response.Content.ReadAsStringAsync();
            Assert.False(string.IsNullOrEmpty(jsonString));
        }

        [Fact]
        public async Task GetOrder_WhenOrderNotExists_ReturnsNotFoundOrBadRequest()
        {
            var nonExistentId = Guid.NewGuid();

            var response = await _client.GetAsync($"/api/orders/{nonExistentId}");

            Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetUserOrders_WithValidUserId_ReturnsOkOrEmpty()
        {
            var userId = Guid.NewGuid();

            var response = await _client.GetAsync($"/api/orders/user/{userId}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var jsonString = await response.Content.ReadAsStringAsync();
            Assert.False(string.IsNullOrEmpty(jsonString));
        }

        [Fact]
        public async Task CancelOrder_WhenOrderNotExists_ReturnsNotFoundOrBadRequest()
        {
            var nonExistentId = Guid.NewGuid();

            var response = await _client.PatchAsync($"/api/orders/{nonExistentId}/cancel", null);

            Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateOrder_Sell_WithSufficientBalance_ReturnsCreated()
        {
            var userId = Guid.NewGuid();

            // ۱. ابتدا واریز طلا تا کیف پول و موجودی کافی ایجاد شود
            await _client.PostAsJsonAsync($"/api/wallets/user/{userId}/deposit", new DepositRequestDto(10.0m));

            // ۲. ثبت سفارش فروش معتبر (چون موجودی داریم باید موفق باشد)
            var request = new CreateOrderRequestDto(
                UserId: userId,
                Type: OrderType.Sell,
                WeightValue: 3.0m,
                UnitPriceAmount: 4500000m,
                Currency: "IRR",
                FeePercent: 1.0m,
                Tags: new[] { "فروش-طلا" },
                UserNote: "تست فروش طلا با موجودی کافی"
            );

            var response = await _client.PostAsJsonAsync("/api/orders", request);

            Assert.True(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK);
        }

        [Fact]
        public async Task CancelOrder_ExistingOrder_ReturnsSuccess()
        {
            var userId = Guid.NewGuid();

            // ۱. ساخت والت
            await _client.PostAsJsonAsync($"/api/wallets/user/{userId}/deposit", new DepositRequestDto(5.0m));

            // ۲. ایجاد سفارش خرید جدید
            var createRequest = new CreateOrderRequestDto(
                UserId: userId,
                Type: OrderType.Buy,
                WeightValue: 1.0m,
                UnitPriceAmount: 4000000m,
                Currency: "IRR",
                FeePercent: 1.0m,
                Tags: null,
                UserNote: "سفارش برای لغو"
            );

            var createResponse = await _client.PostAsJsonAsync("/api/orders", createRequest);
            Assert.True(createResponse.IsSuccessStatusCode);

            // ۳. استخراج آیدی سفارش از پاسخ Result<Guid> با استفاده از JsonDocument
            var jsonContent = await createResponse.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(jsonContent);
            var orderId = doc.RootElement.GetProperty("value").GetGuid();

            // ۴. لغو سفارش
            var cancelResponse = await _client.PatchAsync($"/api/orders/{orderId}/cancel", null);
            Assert.True(cancelResponse.IsSuccessStatusCode);
        }

        [Fact]
        public async Task CompleteOrder_ExistingOrder_ReturnsSuccess()
        {
            var userId = Guid.NewGuid();

            // ۱. ساخت والت با موجودی اولیه
            await _client.PostAsJsonAsync($"/api/wallets/user/{userId}/deposit", new DepositRequestDto(10.0m));

            // ۲. ایجاد سفارش جدید
            var createRequest = new CreateOrderRequestDto(
                UserId: userId,
                Type: OrderType.Buy,
                WeightValue: 2.0m,
                UnitPriceAmount: 4000000m,
                Currency: "IRR",
                FeePercent: 1.0m,
                Tags: null,
                UserNote: "سفارش برای تکمیل"
            );

            var createResponse = await _client.PostAsJsonAsync("/api/orders", createRequest);
            Assert.True(createResponse.IsSuccessStatusCode);

            // ۳. استخراج آیدی سفارش از پاسخ Result<Guid> با استفاده از JsonDocument
            var jsonContent = await createResponse.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(jsonContent);
            var orderId = doc.RootElement.GetProperty("value").GetGuid();

            // ۴. تکمیل سفارش
            var completeResponse = await _client.PatchAsync($"/api/orders/{orderId}/complete", null);
            Assert.True(completeResponse.IsSuccessStatusCode);
        }
        [Fact]
        public async Task CreateOrder_WithInvalidNegativeWeight_ReturnsBadRequestOrUnprocessableEntity()
        {
            var userId = Guid.NewGuid();
            // ساخت والت اولیه
            await _client.PostAsJsonAsync($"/api/wallets/user/{userId}/deposit", new DepositRequestDto(10.0m));

            var request = new CreateOrderRequestDto(
                UserId: userId,
                Type: OrderType.Buy,
                WeightValue: -1.5m, // وزن منفی (نامعتبر)
                UnitPriceAmount: 4000000m,
                Currency: "IRR",
                FeePercent: 1.0m,
                Tags: null,
                UserNote: "تست وزن منفی"
            );

            var response = await _client.PostAsJsonAsync("/api/orders", request);

            Assert.NotEqual(HttpStatusCode.Created, response.StatusCode);
            Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CreateOrder_Sell_WithoutSufficientWalletBalance_ReturnsUnprocessableEntity()
        {
            var userId = Guid.NewGuid();
            // والت با موجودی صفر یا کم (بدون واریز)

            var request = new CreateOrderRequestDto(
                UserId: userId,
                Type: OrderType.Sell,
                WeightValue: 10.0m, // درخواست فروش مقدار زیاد بدون موجودی
                UnitPriceAmount: 4000000m,
                Currency: "IRR",
                FeePercent: 1.0m,
                Tags: null,
                UserNote: "فروش بدون موجودی در والت"
            );

            var response = await _client.PostAsJsonAsync("/api/orders", request);

            // انتظار داریم سیستم به دلیل نداشتن موجودی کافی خطا بدهد
            Assert.NotEqual(HttpStatusCode.Created, response.StatusCode);
            Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetUserOrders_AfterCreatingOrders_ReturnsList()
        {
            var userId = Guid.NewGuid();
            await _client.PostAsJsonAsync($"/api/wallets/user/{userId}/deposit", new DepositRequestDto(20.0m));

            // ثبت یک سفارش
            var request = new CreateOrderRequestDto(
                UserId: userId,
                Type: OrderType.Buy,
                WeightValue: 2.0m,
                UnitPriceAmount: 4000000m,
                Currency: "IRR",
                FeePercent: 1.0m,
                Tags: new[] { "تست-لیست" },
                UserNote: "ثبت سفارش برای تست لیست"
            );
            await _client.PostAsJsonAsync("/api/orders", request);

            // استعلام لیست سفارش‌های کاربر
            var response = await _client.GetAsync($"/api/orders/user/{userId}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var jsonString = await response.Content.ReadAsStringAsync();
            Assert.False(string.IsNullOrEmpty(jsonString));
        }
    }
}
