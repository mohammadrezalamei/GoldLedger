using GoldLedger.Domain.Aggregates.GoldOrder;
using GoldLedger.Domain.Common;
using GoldLedger.Domain.ValueObjects;

namespace GoldLedger.Tests.Domain.ValueObjects;

public sealed class IdempotencyKeyTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("key with space")]
    [InlineData("کلید")]
    [InlineData("key/part")]
    [InlineData("key,other")]
    [InlineData("key\n")]
    public void Invalid_request_key_is_rejected_in_domain(string? value)
    {
        var result = IdempotencyKey.Create(value);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("Order-123_test.v1")]
    public void Valid_request_key_preserves_value(string value)
    {
        var result = IdempotencyKey.Create(value);

        Assert.True(result.IsSuccess);
        Assert.Equal(value, result.Value!.Value);
    }

    [Fact]
    public void Request_key_accepts_maximum_length_and_rejects_one_more_character()
    {
        Assert.True(IdempotencyKey.Create(new string('a', 128)).IsSuccess);
        Assert.False(IdempotencyKey.Create(new string('a', 129)).IsSuccess);
    }
}
