using System.Net;
using System.Net.Sockets;
using OrionIrcd.Network.Data.Events;
using OrionIrcd.Network.Server;

namespace OrionIrcd.Tests.Network.Framing;

public class NetFramerIntegrationTests
{
    [Fact]
    public async Task ReceiveLoop_WithFramer_EmitsOneEventPerCompleteFrame()
    {
        var framer = new LengthPrefixedFramer();
        await using var server = new OrionTcpServer(new IPEndPoint(IPAddress.Loopback, 0), framer);

        var received = new List<byte[]>();
        var receivedSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        server.OnDataReceived += (_, args) =>
        {
            lock (received)
            {
                received.Add(args.Data.ToArray());

                if (received.Count >= 3)
                {
                    receivedSignal.TrySetResult(true);
                }
            }
        };

        await server.StartAsync(CancellationToken.None);

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, server.Port);

        // Three back-to-back frames of declared lengths 4, 5, 6 bytes.
        var packet = new byte[]
        {
            0x00, 0x04, 0xAA, 0xBB,
            0x00, 0x05, 0xCC, 0xDD, 0xEE,
            0x00, 0x06, 0x11, 0x22, 0x33, 0x44
        };

        await client.GetStream().WriteAsync(packet);
        await receivedSignal.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(3, received.Count);
        Assert.Equal(new byte[] { 0x00, 0x04, 0xAA, 0xBB }, received[0]);
        Assert.Equal(new byte[] { 0x00, 0x05, 0xCC, 0xDD, 0xEE }, received[1]);
        Assert.Equal(new byte[] { 0x00, 0x06, 0x11, 0x22, 0x33, 0x44 }, received[2]);

        await server.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ReceiveLoop_WithFramer_HoldsPartialTailUntilCompleted()
    {
        var framer = new LengthPrefixedFramer();
        await using var server = new OrionTcpServer(new IPEndPoint(IPAddress.Loopback, 0), framer);

        var received = new List<byte[]>();
        var firstFrameSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondFrameSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        server.OnDataReceived += (_, args) =>
        {
            lock (received)
            {
                received.Add(args.Data.ToArray());

                if (received.Count == 1)
                {
                    firstFrameSignal.TrySetResult(true);
                }
                else if (received.Count == 2)
                {
                    secondFrameSignal.TrySetResult(true);
                }
            }
        };

        await server.StartAsync(CancellationToken.None);

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, server.Port);

        var stream = client.GetStream();

        // Send one complete frame plus the beginning of a second one.
        await stream.WriteAsync(new byte[] { 0x00, 0x04, 0xAA, 0xBB, 0x00, 0x06, 0x11, 0x22 });

        await firstFrameSignal.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Single(received);
        Assert.Equal(new byte[] { 0x00, 0x04, 0xAA, 0xBB }, received[0]);

        // Now send the remaining bytes of the second frame.
        await stream.WriteAsync(new byte[] { 0x33, 0x44 });

        await secondFrameSignal.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(2, received.Count);
        Assert.Equal(new byte[] { 0x00, 0x06, 0x11, 0x22, 0x33, 0x44 }, received[1]);

        await server.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ReceiveLoop_WithoutFramer_EmitsRawChunks()
    {
        await using var server = new OrionTcpServer(new IPEndPoint(IPAddress.Loopback, 0));

        OrionTcpDataReceivedEventArgs? captured = null;
        var signal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        server.OnDataReceived += (_, args) =>
        {
            captured = args;
            signal.TrySetResult(true);
        };

        await server.StartAsync(CancellationToken.None);

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, server.Port);
        await client.GetStream().WriteAsync(new byte[] { 1, 2, 3, 4, 5 });

        await signal.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.NotNull(captured);
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, captured!.Data.ToArray());

        await server.StopAsync(CancellationToken.None);
    }
}
