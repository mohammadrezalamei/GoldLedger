using GoldLedger.Domain.ValueObjects;
namespace GoldLedger.Tests.Domain.ValueObjects;
public sealed class TagBoundaryTests
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
}

