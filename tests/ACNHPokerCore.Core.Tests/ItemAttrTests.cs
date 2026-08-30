using ACNHPokerCore.Core;
using Xunit;

namespace ACNHPokerCore.Core.Tests;

public class ItemAttrTests
{
    [Fact]
    public void HasDurability_KnownTool_ReturnsTrue()
    {
        Assert.True(ItemAttr.HasDurability(0x0833)); // shovel
    }

    [Fact]
    public void HasDurability_UnrelatedItem_ReturnsFalse()
    {
        Assert.False(ItemAttr.HasDurability(0x0001));
    }

    [Fact]
    public void HasUse_KnownConsumable_ReturnsTrue()
    {
        Assert.True(ItemAttr.HasUse(0x0144)); // rainbow soft serve
    }

    [Fact]
    public void Empty_Constant_MatchesGameSentinelValue()
    {
        Assert.Equal(0xFFFE, ItemAttr.empty);
    }
}
