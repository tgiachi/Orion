using OrionIrcd.Server.Services.Network;

namespace OrionIrcd.Tests.Server.Services.Network;

public class IrcLineFramerTests
{
    [Fact]
    public void TryReadFrame_BufferWithoutLineFeed_ReturnsFalse()
    {
        var framer = new IrcLineFramer();

        var result = framer.TryReadFrame("NICK squid"u8, out var frameLength);

        Assert.False(result);
        Assert.Equal(0, frameLength);
    }

    [Fact]
    public void TryReadFrame_CrlfLine_ReturnsFrameIncludingLineFeed()
    {
        var framer = new IrcLineFramer();

        var result = framer.TryReadFrame("NICK squid\r\n"u8, out var frameLength);

        Assert.True(result);
        Assert.Equal("NICK squid\r\n"u8.Length, frameLength);
    }

    [Fact]
    public void TryReadFrame_MultipleLines_ReturnsFirstFrameLength()
    {
        var framer = new IrcLineFramer();

        var result = framer.TryReadFrame("PING :one\nPING :two\n"u8, out var frameLength);

        Assert.True(result);
        Assert.Equal("PING :one\n"u8.Length, frameLength);
    }
}
