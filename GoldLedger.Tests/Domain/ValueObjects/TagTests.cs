using GoldLedger.Domain.ValueObjects;
namespace GoldLedger.Tests.Domain.ValueObjects;
public sealed class TagTests
{
 [Fact] public void Valid_tag_succeeds()=>Assert.True(Tag.Create("savings").IsSuccess);
 [Fact] public void Blank_tag_fails()=>Assert.False(Tag.Create(" ").IsSuccess);
}
