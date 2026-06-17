using System.Net;
using System.Net.WebSockets;
using OrionIrcd.Network.Client;
using OrionIrcd.Network.Server;
using OrionIrcd.Tests.Support.Network;

namespace OrionIrcd.Tests.Network.Server;

public class OrionWebSocketServerTests
{
    [Fact]
    public async Task Start_AcceptsWebSocketClient()
    {
        await using var server = new OrionWebSocketServer(new IPEndPoint(IPAddress.Loopback, 0));
        var connectedSignal = new TaskCompletionSource<OrionWebSocketClient>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        server.OnClientConnect += (_, args) => connectedSignal.TrySetResult(args.Client);

        await server.StartAsync(CancellationToken.None);

        using var client = new ClientWebSocket();
        await client.ConnectAsync(new Uri($"ws://localhost:{server.Port}/"), CancellationToken.None);

        var connectedClient = await connectedSignal.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(connectedClient.IsConnected);

        await server.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Start_ReceivesTextMessageAsBytes()
    {
        await using var server = new OrionWebSocketServer(new IPEndPoint(IPAddress.Loopback, 0));
        var receivedSignal = new TaskCompletionSource<byte[]>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        server.OnDataReceived += (_, args) => receivedSignal.TrySetResult(args.Data.ToArray());

        await server.StartAsync(CancellationToken.None);

        using var client = new ClientWebSocket();
        await client.ConnectAsync(new Uri($"ws://localhost:{server.Port}/"), CancellationToken.None);
        await client.SendAsync(
            "PING :server\r\n"u8.ToArray(),
            WebSocketMessageType.Text,
            true,
            CancellationToken.None
        );

        var received = await receivedSignal.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal("PING :server\r\n"u8.ToArray(), received);

        await server.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Start_WithTlsOptions_AcceptsSecureWebSocketClient()
    {
        using var certificate = TestCertificateFactory.CreateSelfSignedCertificate();
        await using var server = new OrionWebSocketServer(
            new IPEndPoint(IPAddress.Loopback, 0),
            new OrionWebSocketServerTlsOptions(certificate)
        );
        var expectedThumbprint = certificate.Thumbprint;
        var receivedSignal = new TaskCompletionSource<byte[]>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        server.OnDataReceived += (_, args) => receivedSignal.TrySetResult(args.Data.ToArray());

        await server.StartAsync(CancellationToken.None);

        using var client = new ClientWebSocket();
        client.Options.RemoteCertificateValidationCallback = (_, serverCertificate, _, _) =>
            string.Equals(
                expectedThumbprint,
                serverCertificate?.GetCertHashString(),
                StringComparison.OrdinalIgnoreCase
            );

        await client.ConnectAsync(new Uri($"wss://localhost:{server.Port}/"), CancellationToken.None);
        await client.SendAsync(
            "CAP LS 302\r\n"u8.ToArray(),
            WebSocketMessageType.Text,
            true,
            CancellationToken.None
        );

        var received = await receivedSignal.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal("CAP LS 302\r\n"u8.ToArray(), received);

        await server.StopAsync(CancellationToken.None);
    }
}
