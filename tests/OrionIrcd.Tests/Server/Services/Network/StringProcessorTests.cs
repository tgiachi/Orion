using OrionIrcd.Server.Services.Network;
using OrionIrcd.Tests.Support.Network;

namespace OrionIrcd.Tests.Server.Services.Network;

public class StringProcessorTests
{
    [Fact]
    public async Task ProcessAsync_CrlfTerminatedUtf8Frame_ReturnsCommandWithoutTerminator()
    {
        var connection = new TestNetworkConnection();
        var processor = new StringProcessor();

        var result = await processor.ProcessAsync(
            connection,
            "NICK squid\r\n"u8.ToArray(),
            CancellationToken.None
        );

        Assert.Equal("NICK squid", result);
    }

    [Fact]
    public async Task ProcessAsync_LfTerminatedUtf8Frame_ReturnsCommandWithoutTerminator()
    {
        var connection = new TestNetworkConnection();
        var processor = new StringProcessor();

        var result = await processor.ProcessAsync(
            connection,
            "PING :server\n"u8.ToArray(),
            CancellationToken.None
        );

        Assert.Equal("PING :server", result);
    }

    [Fact]
    public async Task ProcessAsync_CommandWithTrailingSpaces_PreservesCommandContent()
    {
        var connection = new TestNetworkConnection();
        var processor = new StringProcessor();

        var result = await processor.ProcessAsync(
            connection,
            "PRIVMSG #chan :hello  \r\n"u8.ToArray(),
            CancellationToken.None
        );

        Assert.Equal("PRIVMSG #chan :hello  ", result);
    }

    [Fact]
    public async Task ProcessAsync_EmptyFrame_ReturnsEmptyString()
    {
        var connection = new TestNetworkConnection();
        var processor = new StringProcessor();

        var result = await processor.ProcessAsync(
            connection,
            ReadOnlyMemory<byte>.Empty,
            CancellationToken.None
        );

        Assert.Equal(string.Empty, result);
    }
}
