using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Reflection;
using OrionIrcd.Network.Client;
using OrionIrcd.Network.Data.Options;
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
    public async Task Start_ReceivesBinaryMessageAsBytes()
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
            "NICK binary-test\r\n"u8.ToArray(),
            WebSocketMessageType.Binary,
            true,
            CancellationToken.None
        );

        var received = await receivedSignal.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal("NICK binary-test\r\n"u8.ToArray(), received);

        await server.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Start_RejectsNonWebSocketHttpRequest()
    {
        await using var server = new OrionWebSocketServer(new IPEndPoint(IPAddress.Loopback, 0));

        await server.StartAsync(CancellationToken.None);

        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(
            new Uri($"http://localhost:{server.Port}/"),
            CancellationToken.None
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

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

    [Fact]
    public async Task Stop_WithIdleClient_ReturnsPromptly()
    {
        await using var server = new OrionWebSocketServer(new IPEndPoint(IPAddress.Loopback, 0));
        var connectedSignal = new TaskCompletionSource<OrionWebSocketClient>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        server.OnClientConnect += (_, args) => connectedSignal.TrySetResult(args.Client);

        await server.StartAsync(CancellationToken.None);

        using var client = new ClientWebSocket();
        await client.ConnectAsync(new Uri($"ws://localhost:{server.Port}/"), CancellationToken.None);
        await connectedSignal.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var stopwatch = Stopwatch.StartNew();

        await server.StopAsync(CancellationToken.None);

        stopwatch.Stop();

        await AssertClientClosedAsync(client);

        Assert.True(
            stopwatch.Elapsed < TimeSpan.FromMilliseconds(1500),
            $"StopAsync took {stopwatch.Elapsed}."
        );
    }

    [Fact]
    public async Task Stop_RejectsWebSocketAcceptedAfterShutdownBegins()
    {
        await using var server = new OrionWebSocketServer(new IPEndPoint(IPAddress.Loopback, 0));

        await server.StartAsync(CancellationToken.None);

        var port = server.Port;
        var blockingSocket = new BlockingCloseWebSocket();
        var blockingClient = new OrionWebSocketClient(blockingSocket);
        GetServerClients(server)[blockingClient.SessionId] = blockingClient;

        var stopTask = server.StopAsync(CancellationToken.None);
        await blockingSocket.CloseStarted.WaitAsync(TimeSpan.FromSeconds(5));

        using var lateClient = new ClientWebSocket();
        Exception? connectException = null;

        try
        {
            using var connectTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await lateClient.ConnectAsync(new Uri($"ws://localhost:{port}/"), connectTimeout.Token);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            connectException = ex;
        }
        finally
        {
            if (lateClient.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                lateClient.Abort();
            }

            blockingSocket.ReleaseClose();
            await stopTask.WaitAsync(TimeSpan.FromSeconds(5));
        }

        Assert.NotNull(connectException);
    }

    private static ConcurrentDictionary<long, OrionWebSocketClient> GetServerClients(
        OrionWebSocketServer server
    )
    {
        var field = typeof(OrionWebSocketServer).GetField(
            "_clients",
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        Assert.NotNull(field);

        return Assert.IsType<ConcurrentDictionary<long, OrionWebSocketClient>>(
            field.GetValue(server)
        );
    }

    private static async Task AssertClientClosedAsync(ClientWebSocket client)
    {
        var closeObserved = client.State is not WebSocketState.Open;

        if (client.State == WebSocketState.Open)
        {
            var buffer = new byte[1];
            using var receiveTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            try
            {
                var result = await client.ReceiveAsync(buffer, receiveTimeout.Token);
                closeObserved = result.MessageType == WebSocketMessageType.Close;
            }
            catch (WebSocketException)
            {
                closeObserved = true;
            }
        }

        Assert.True(
            closeObserved,
            $"Expected closed client connection, got {client.State}."
        );
    }
}
