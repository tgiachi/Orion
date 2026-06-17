using System.Net;
using System.Net.Sockets;
using OrionIrcd.Network.Server;

namespace OrionIrcd.Tests.Network.Server;

public class OrionTcpServerTests
{
    [Fact]
    public async Task Start_AcceptsClient()
    {
        await using var server = new OrionTcpServer(new IPEndPoint(IPAddress.Loopback, 0));
        await server.StartAsync(CancellationToken.None);

        var connectedSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        server.OnClientConnect += (_, _) => connectedSignal.TrySetResult(true);

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, server.Port);

        var connected = await connectedSignal.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(connected);

        await server.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Start_BindsAndListens()
    {
        await using var server = new OrionTcpServer(new IPEndPoint(IPAddress.Loopback, 0));

        await server.StartAsync(CancellationToken.None);

        Assert.True(server.IsRunning);
        Assert.True(server.Port > 0);

        await server.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StopThenStart_RebindsListener()
    {
        // Regression: previous implementation kept a single socket field, so Stop closed
        // it and a subsequent Start tried to listen on a disposed socket.
        await using var server = new OrionTcpServer(new IPEndPoint(IPAddress.Loopback, 0));

        await server.StartAsync(CancellationToken.None);
        var firstPort = server.Port;
        Assert.True(firstPort > 0);

        await server.StopAsync(CancellationToken.None);
        Assert.False(server.IsRunning);
        Assert.Equal(0, server.Port);

        await server.StartAsync(CancellationToken.None);

        Assert.True(server.IsRunning);
        Assert.True(server.Port > 0);

        await server.StopAsync(CancellationToken.None);
    }
}
