using System.Net;
using System.Net.Sockets;
using System.Text;
using OrionIrcd.Core.Types;
using OrionIrcd.Network.Server;

namespace OrionIrcd.Tests.Network.Server;

public class OrionUdpServerTests
{
    [Fact]
    public async Task Metadata_ReportsUdpServerTypeAndPort()
    {
        await using var server = new OrionUdpServer(new IPEndPoint(IPAddress.Loopback, 0), false);

        Assert.Equal(ServerType.UDP, server.ServerType);
        Assert.False(server.IsRunning);
        Assert.Equal(0, server.Port);

        await server.StartAsync(CancellationToken.None);

        Assert.True(server.IsRunning);
        Assert.True(server.Port > 0);

        await server.StopAsync(CancellationToken.None);

        Assert.False(server.IsRunning);
        Assert.Equal(0, server.Port);
    }

    [Fact]
    public async Task Receive_DefaultBehaviour_EchoesPayloadBackToSender()
    {
        var port = GetFreeUdpPort();
        await using var server = new OrionUdpServer(new IPEndPoint(IPAddress.Loopback, port), false);
        await server.StartAsync(CancellationToken.None);

        using var client = new UdpClient();
        var payload = Encoding.ASCII.GetBytes("ping");
        await client.SendAsync(payload, payload.Length, new IPEndPoint(IPAddress.Loopback, port));

        var received = await client.ReceiveAsync().WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(payload, received.Buffer);

        await server.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Receive_WithCustomHandler_SendsHandlerResponse()
    {
        var port = GetFreeUdpPort();
        await using var server = new OrionUdpServer(new IPEndPoint(IPAddress.Loopback, port), false)
        {
            OnDatagram = (data, _) =>
            {
                var reply = new byte[data.Length];
                data.Span.CopyTo(reply);

                for (var i = 0; i < reply.Length; i++)
                {
                    reply[i] = (byte)(reply[i] + 1);
                }

                return reply;
            }
        };
        await server.StartAsync(CancellationToken.None);

        using var client = new UdpClient();
        await client.SendAsync([1, 2, 3], 3, new IPEndPoint(IPAddress.Loopback, port));

        var received = await client.ReceiveAsync().WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(new byte[] { 2, 3, 4 }, received.Buffer);

        await server.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Start_BindsAndReportsRunning()
    {
        await using var server = new OrionUdpServer(new IPEndPoint(IPAddress.Loopback, 0), false);

        await server.StartAsync(CancellationToken.None);

        Assert.True(server.IsRunning);
        Assert.Equal(1, server.ListenerCount);

        await server.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StopThenStart_RebindsListener()
    {
        var port = GetFreeUdpPort();
        await using var server = new OrionUdpServer(new IPEndPoint(IPAddress.Loopback, port), false);

        await server.StartAsync(CancellationToken.None);
        Assert.True(server.IsRunning);

        await server.StopAsync(CancellationToken.None);
        Assert.False(server.IsRunning);
        Assert.Equal(0, server.ListenerCount);

        await server.StartAsync(CancellationToken.None);
        Assert.True(server.IsRunning);
        Assert.Equal(1, server.ListenerCount);

        await server.StopAsync(CancellationToken.None);
    }

    private static int GetFreeUdpPort()
    {
        using var probe = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));

        return ((IPEndPoint)probe.Client.LocalEndPoint!).Port;
    }
}
