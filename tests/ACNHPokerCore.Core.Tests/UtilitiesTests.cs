using System.Text;
using ACNHPokerCore.Core;
using Xunit;

namespace ACNHPokerCore.Core.Tests;

/// <summary>
/// Covers the pure, no-socket-needed parts of Utilities.cs: address arithmetic and the
/// byte/hex/UTF-16 helpers the sys-botbase protocol methods build their commands from.
/// The protocol methods themselves (PeekAddress, SpawnItem, etc.) need a live sys-botbase
/// connection and are exercised manually against a real Switch instead - see README.
/// </summary>
public class UtilitiesTests
{
    [Fact]
    public void GetItemSlotAddress_FirstSlot_EqualsItemSlotBase()
    {
        Assert.Equal("0x" + Utilities.ItemSlotBase.ToString("X"), Utilities.GetItemSlotAddress(1));
    }

    [Fact]
    public void GetItemSlotAddress_Slot21_UsesSecondBase()
    {
        Assert.Equal("0x" + Utilities.ItemSlot21Base.ToString("X"), Utilities.GetItemSlotAddress(21));
    }

    [Fact]
    public void GetItemSlotAddress_OutOfRangeSlot_IsClampedNotThrown()
    {
        // Slot 999 clamps to 40 rather than throwing - matches the original's Clamp() guard.
        var ex = Record.Exception(() => Utilities.GetItemSlotAddress(999));
        Assert.Null(ex);
    }

    [Fact]
    public void StringToByte_SingleHexDigit_ReturnsOneByte()
    {
        Assert.Equal(new byte[] { 0x0A }, Utilities.StringToByte("A"));
    }

    [Fact]
    public void StringToByte_HexPairs_ReturnsMatchingBytes()
    {
        Assert.Equal(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }, Utilities.StringToByte("DEADBEEF"));
    }

    [Fact]
    public void ByteToHexString_RoundTripsWithStringToByte()
    {
        byte[] original = [0x12, 0x34, 0xFF, 0x00];
        Assert.Equal("1234FF00", Utilities.ByteToHexString(original));
    }

    [Fact]
    public void TrimFromZero_CutsAtFirstNullTerminator()
    {
        Assert.Equal("Tom Nook", Utilities.TrimFromZero("Tom Nook\0\0\0\0"));
    }

    [Fact]
    public void GetString_DecodesUtf16LittleEndianAndTrimsPadding()
    {
        byte[] data = Encoding.Unicode.GetBytes("Isabelle\0\0");
        Assert.Equal("Isabelle", Utilities.GetString(data, 0, 10));
    }

    [Fact]
    public void GetBytes_PadsShortStringWithNulls()
    {
        byte[] result = Utilities.GetBytes("Hi", 4);
        Assert.Equal(Encoding.Unicode.GetBytes("Hi\0\0"), result);
    }

    [Fact]
    public void GetBytes_TruncatesLongString()
    {
        byte[] result = Utilities.GetBytes("Animal Crossing", 6);
        Assert.Equal(Encoding.Unicode.GetBytes("Animal"), result);
    }
}
