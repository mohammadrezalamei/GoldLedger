using GoldLedger.Domain.Common;
using GoldLedger.Domain.ValueObjects;
using Xunit;

namespace GoldLedger.Tests.Domain.ValueObjects;

public sealed class TagTests
{
    [Fact]
    public void Tag_normalizes_whitespace()
    {
        var result = Tag.Create("  long   term  ");
        Assert.True(result.IsSuccess);
        Assert.Equal("long term", result.Value!.Value);
    }

    [Fact]
    public void Tag_over_maximum_length_fails()
    {
        var result = Tag.Create(new string('x', Tag.MaxLength + 1));
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Create_with_valid_value_succeeds()
    {
        var result = Tag.Create("savings");
        Assert.True(result.IsSuccess);
        Assert.Equal("savings", result.Value!.Value);
    }

    [Fact]
    public void Create_with_blank_value_fails()
    {
        var result = Tag.Create(" ");
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public void Create_normalizes_whitespace()
    {
        var result = Tag.Create("  long   term  ");
        Assert.Equal("long term", result.Value!.Value);
    }

    [Fact]
    public void Create_above_maximum_length_fails()
    {
        var oversizedValue = new string('x', Tag.MaxLength + 1);
        var result = Tag.Create(oversizedValue);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Equal_values_are_equal()
    {
        var firstTag = Tag.Create("gold").Value!;
        var secondTag = Tag.Create("gold").Value!;
        Assert.Equal(firstTag, secondTag);
    }

    [Fact]
    public void Tag_rejects_null_value()
    {
        var result = Tag.Create(null!);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public void Tag_normalization_collapses_internal_whitespace()
    {
        var result = Tag.Create("  long    term  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("long term", result.Value!.Value);
    }
}
