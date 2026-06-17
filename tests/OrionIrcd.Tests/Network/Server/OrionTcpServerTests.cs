using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using OrionIrcd.Network.Client;
using OrionIrcd.Network.Server;
using OrionIrcd.Tests.Support.Network;

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

    [Fact]
    public async Task Start_WithTlsOptions_AuthenticatesSslClientAndReceivesData()
    {
        using var certificate = TestCertificateFactory.CreateSelfSignedCertificate();
        await using var server = new OrionTcpServer(
            new IPEndPoint(IPAddress.Loopback, 0),
            tlsOptions: new OrionTcpServerTlsOptions(certificate)
        );
        var expectedThumbprint = certificate.Thumbprint;
        var receivedSignal = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        server.OnDataReceived += (_, args) => receivedSignal.TrySetResult(args.Data.ToArray());

        await server.StartAsync(CancellationToken.None);

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, server.Port);
        await using var sslStream = new SslStream(
            client.GetStream(),
            false,
            (_, serverCertificate, _, _) => string.Equals(
                expectedThumbprint,
                serverCertificate?.GetCertHashString(),
                StringComparison.OrdinalIgnoreCase
            )
        );
        await sslStream.AuthenticateAsClientAsync("localhost");
        await sslStream.WriteAsync("PING :localhost\r\n"u8.ToArray());

        var received = await receivedSignal.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal("PING :localhost\r\n"u8.ToArray(), received);

        await server.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task SendAsync_WithTlsOptions_WritesEncryptedDataToSslClient()
    {
        using var certificate = TestCertificateFactory.CreateSelfSignedCertificate();
        await using var server = new OrionTcpServer(
            new IPEndPoint(IPAddress.Loopback, 0),
            tlsOptions: new OrionTcpServerTlsOptions(certificate)
        );
        var expectedThumbprint = certificate.Thumbprint;
        var connectedSignal = new TaskCompletionSource<OrionTcpClient>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        server.OnClientConnect += (_, args) => connectedSignal.TrySetResult(args.Client);

        await server.StartAsync(CancellationToken.None);

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, server.Port);
        await using var sslStream = new SslStream(
            client.GetStream(),
            false,
            (_, serverCertificate, _, _) => string.Equals(
                expectedThumbprint,
                serverCertificate?.GetCertHashString(),
                StringComparison.OrdinalIgnoreCase
            )
        );
        await sslStream.AuthenticateAsClientAsync("localhost");

        var connectedClient = await connectedSignal.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await connectedClient.SendAsync("NOTICE AUTH :hello\r\n"u8.ToArray(), CancellationToken.None);

        var buffer = new byte[64];
        var bytesRead = await sslStream.ReadAsync(buffer).AsTask().WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal("NOTICE AUTH :hello\r\n"u8.ToArray(), buffer.AsSpan(0, bytesRead).ToArray());

        await server.StopAsync(CancellationToken.None);
    }
}
