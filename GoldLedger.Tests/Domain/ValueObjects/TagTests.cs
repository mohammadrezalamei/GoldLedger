using GoldLedger.Domain.Common;
using GoldLedger.Domain.ValueObjects;

namespace GoldLedger.Tests.Domain.ValueObjects;

public class TagTests
{
    [Fact]
    public void Create_WithValidValue_ShouldReturnSuccessResultWithNormalizedTag()
    {
        var rawValue = "  نوسان‌گیری   روزانه  ";

        var result = Tag.Create(rawValue);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("نوسان‌گیری روزانه", result.Value.Value);
        Assert.Equal(ErrorType.None, result.ErrorType);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhitespace_ShouldReturnFailureResult(string? invalidValue)
    {
        var result = Tag.Create(invalidValue!);

        Assert.False(result.IsSuccess);
        Assert.Equal("مقدار تگ نمی‌تواند خالی یا نامعتبر باشد.", result.Message);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public void Create_ExceedingMaxLength_ShouldReturnFailureResult()
    {
        var longValue = new string('ا', Tag.MaxLength + 1);

        var result = Tag.Create(longValue);

        Assert.False(result.IsSuccess);
        Assert.Equal($"طول تگ نمی‌تواند بیشتر از {Tag.MaxLength} کاراکتر باشد.", result.Message);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public void Equals_WithSameValues_ShouldBeEqualAndHaveSameHashCode()
    {
        var tag1 = Tag.Create("پس‌انداز").Value!;
        var tag2 = Tag.Create("پس‌انداز").Value!;

        Assert.Equal(tag1, tag2);
        Assert.True(tag1 == tag2);
        Assert.Equal(tag1.GetHashCode(), tag2.GetHashCode());
    }

    [Fact]
    public void Equals_WithDifferentValues_ShouldNotBeEqual()
    {
        var tag1 = Tag.Create("تگ اول").Value!;
        var tag2 = Tag.Create("تگ دوم").Value!;

        Assert.NotEqual(tag1, tag2);
        Assert.True(tag1 != tag2);
    }

    [Fact]
    public void ImplicitOperator_ShouldReturnStringValue()
    {
        var text = "تست عملگر";
        var tag = Tag.Create(text).Value!;

        string converted = tag;

        Assert.Equal(text, converted);
    }

    [Fact]
    public void ToString_ShouldReturnStringValue()
    {
        var text = "متن تگ";
        var tag = Tag.Create(text).Value!;

        Assert.Equal(text, tag.ToString());
    }
}