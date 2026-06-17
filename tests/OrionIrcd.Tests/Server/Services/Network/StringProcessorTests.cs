using System.Net;
using System.Net.Sockets;
using OrionIrcd.Network.Client;
using OrionIrcd.Server.Services.Network;

namespace OrionIrcd.Tests.Server.Services.Network;

public class StringProcessorTests
{
    [Fact]
    public async Task ProcessAsync_CrlfTerminatedUtf8Frame_ReturnsCommandWithoutTerminator()
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        await using var client = new OrionTcpClient(socket, Stream.Null);
        var processor = new StringProcessor();

        var result = await processor.ProcessAsync(
            client,
            "NICK squid\r\n"u8.ToArray(),
            CancellationToken.None
        );

        Assert.Equal("NICK squid", result);
    }

    [Fact]
    public async Task ProcessAsync_LfTerminatedUtf8Frame_ReturnsCommandWithoutTerminator()
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        await using var client = new OrionTcpClient(socket, Stream.Null);
        var processor = new StringProcessor();

        var result = await processor.ProcessAsync(
            client,
            "PING :server\n"u8.ToArray(),
            CancellationToken.None
        );

        Assert.Equal("PING :server", result);
    }

    [Fact]
    public async Task ProcessAsync_CommandWithTrailingSpaces_PreservesCommandContent()
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        await using var client = new OrionTcpClient(socket, Stream.Null);
        var processor = new StringProcessor();

        var result = await processor.ProcessAsync(
            client,
            "PRIVMSG #chan :hello  \r\n"u8.ToArray(),
            CancellationToken.None
        );

        Assert.Equal("PRIVMSG #chan :hello  ", result);
    }

    [Fact]
    public async Task ProcessAsync_EmptyFrame_ReturnsEmptyString()
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        await using var client = new OrionTcpClient(socket, Stream.Null);
        var processor = new StringProcessor();

        var result = await processor.ProcessAsync(
            client,
            ReadOnlyMemory<byte>.Empty,
            CancellationToken.None
        );

        Assert.Equal(string.Empty, result);
    }
}
