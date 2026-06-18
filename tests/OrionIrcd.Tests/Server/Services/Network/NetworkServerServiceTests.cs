using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using OrionIrcd.Core.Data.Config.Sections;
using OrionIrcd.Core.Directories;
using OrionIrcd.Core.Types;
using OrionIrcd.Server.Data.Events;
using OrionIrcd.Server.Services.Network;
using OrionIrcd.Server.Services.Sessions;
using OrionIrcd.Server.Types;
using OrionIrcd.Tests.Support.Events;
using OrionIrcd.Tests.Support.Io;
using OrionIrcd.Tests.Support.Network;

namespace OrionIrcd.Tests.Server.Services.Network;

public class NetworkServerServiceTests
{
    [Fact]
    public async Task StartAsync_ConfiguredTcpEntry_StartsListener()
    {
        var service = new NetworkServerService(CreateNetworkConfig("0"));

        await service.StartAsync(CancellationToken.None);

        try
        {
            Assert.True(service.IsRunning);
            Assert.Equal(1, service.ListenerCount);
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task StartAsync_DuplicatePorts_StartsSingleListener()
    {
        var service = new NetworkServerService(CreateNetworkConfig("0,0"));

        await service.StartAsync(CancellationToken.None);

        try
        {
            Assert.Equal(1, service.ListenerCount);
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task StopAsync_AfterStart_StopsAndClearsListeners()
    {
        var service = new NetworkServerService(CreateNetworkConfig("0"));

        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        Assert.False(service.IsRunning);
        Assert.Equal(0, service.ListenerCount);
    }

    [Fact]
    public async Task StartAsync_SslEntry_StartsListenerFromCertificateFile()
    {
        using var temporaryRoot = new TemporaryDirectory();
        var directoriesConfig = new DirectoriesConfig(temporaryRoot.Path, Enum.GetNames<DirectoryType>());
        const string password = "password";
        var certificateFileName = "server.pfx";
        WriteCertificate(Path.Combine(directoriesConfig[DirectoryType.Certs], certificateFileName), password);
        var config = CreateNetworkConfig("0");
        config.SSLCertFile = certificateFileName;
        config.SSLCertPassword = password;
        config.Entries[0].Protocol = ServerProtocolType.SSL;
        var service = new NetworkServerService(config, directoriesConfig);

        await service.StartAsync(CancellationToken.None);

        try
        {
            Assert.True(service.IsRunning);
            Assert.Equal(1, service.ListenerCount);
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task StartAsync_SslEntryWithoutCertificate_ThrowsAndResetsState()
    {
        var config = CreateNetworkConfig("0");
        config.Entries[0].Protocol = ServerProtocolType.SSL;
        var service = new NetworkServerService(config);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.StartAsync(CancellationToken.None));

        Assert.False(service.IsRunning);
        Assert.Equal(0, service.ListenerCount);
    }

    [Fact]
    public async Task StartAsync_WebSocketEntry_StartsListener()
    {
        var config = CreateNetworkConfig("0");
        config.Entries[0].Type = ServerType.WebSocket;
        var service = new NetworkServerService(config);

        await service.StartAsync(CancellationToken.None);

        try
        {
            Assert.True(service.IsRunning);
            Assert.Equal(1, service.ListenerCount);
            Assert.True(service.ListeningPorts.Single() > 0);
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task StartAsync_UdpEntry_StartsListenerThroughCommonServerList()
    {
        var config = CreateNetworkConfig("0");
        config.Entries[0].Type = ServerType.UDP;
        var service = new NetworkServerService(config);

        await service.StartAsync(CancellationToken.None);

        try
        {
            Assert.True(service.IsRunning);
            Assert.Equal(1, service.ListenerCount);
            Assert.True(service.ListeningPorts.Single() > 0);
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task StartAsync_UdpSslEntry_ThrowsAndResetsState()
    {
        var config = CreateNetworkConfig("0");
        config.Entries[0].Type = ServerType.UDP;
        config.Entries[0].Protocol = ServerProtocolType.SSL;
        var service = new NetworkServerService(config);

        await Assert.ThrowsAsync<NotSupportedException>(() => service.StartAsync(CancellationToken.None));

        Assert.False(service.IsRunning);
        Assert.Equal(0, service.ListenerCount);
    }

    [Fact]
    public async Task StartAsync_ReceivesIrcLine_PublishesProcessedStringResult()
    {
        var eventBus = new RecordingEventBus();
        var service = new NetworkServerService(
            CreateNetworkConfig("0"),
            resultProcessor: new StringProcessor(),
            eventBus: eventBus
        );

        await service.StartAsync(CancellationToken.None);

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, service.ListeningPorts.Single());
            await client.GetStream().WriteAsync("NICK squid\r\n"u8.ToArray());

            var publishedEvent = await eventBus.WaitForEventAsync<NetworkResultReceivedEvent<string>>(
                TimeSpan.FromSeconds(5)
            );

            Assert.Equal("NICK squid", publishedEvent.Result);
            Assert.NotNull(publishedEvent.Connection);
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task StartAsync_TcpClientLifecycle_PublishesSessionEvents()
    {
        var eventBus = new RecordingEventBus();
        var sessionManager = new SessionManagerService(eventBus, TimeProvider.System);
        var service = new NetworkServerService(
            CreateNetworkConfig("0"),
            resultProcessor: new StringProcessor(),
            eventBus: eventBus,
            sessionManagerService: sessionManager
        );

        await service.StartAsync(CancellationToken.None);

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, service.ListeningPorts.Single());

            var connectedEvent = await eventBus.WaitForEventAsync<NetworkSessionConnectedEvent>(
                TimeSpan.FromSeconds(5)
            );

            await client.GetStream().WriteAsync("NICK squid\r\n"u8.ToArray());

            var dataEvent = await eventBus.WaitForEventAsync<NetworkSessionDataReceivedEvent>(
                TimeSpan.FromSeconds(5)
            );

            client.Close();

            var disconnectedEvent = await eventBus.WaitForEventAsync<NetworkSessionDisconnectedEvent>(
                TimeSpan.FromSeconds(5)
            );

            Assert.Equal(connectedEvent.Session.SessionId, dataEvent.Session.SessionId);
            Assert.Equal(connectedEvent.Session.SessionId, disconnectedEvent.Session.SessionId);
            Assert.Equal("NICK squid\r\n"u8.Length, dataEvent.Session.BytesReceived);
            Assert.Equal(NetworkSessionStatusType.Disconnected, disconnectedEvent.Session.Status);
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task StartAsync_WebSocketEntry_ReceivesIrcLine_PublishesProcessedStringResult()
    {
        var eventBus = new RecordingEventBus();
        var config = CreateNetworkConfig("0");
        config.Entries[0].Type = ServerType.WebSocket;
        var service = new NetworkServerService(
            config,
            resultProcessor: new StringProcessor(),
            eventBus: eventBus
        );

        await service.StartAsync(CancellationToken.None);

        try
        {
            using var client = new ClientWebSocket();
            await client.ConnectAsync(
                new Uri($"ws://127.0.0.1:{service.ListeningPorts.Single()}/"),
                CancellationToken.None
            );
            await client.SendAsync(
                "NICK squid\r\n"u8.ToArray(),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );

            var publishedEvent = await eventBus.WaitForEventAsync<NetworkResultReceivedEvent<string>>(
                TimeSpan.FromSeconds(5)
            );

            Assert.Equal("NICK squid", publishedEvent.Result);
            Assert.NotNull(publishedEvent.Connection);

            await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task StartAsync_WebSocketClientLifecycle_PublishesSessionEvents()
    {
        var eventBus = new RecordingEventBus();
        var sessionManager = new SessionManagerService(eventBus, TimeProvider.System);
        var config = CreateNetworkConfig("0");
        config.Entries[0].Type = ServerType.WebSocket;
        var service = new NetworkServerService(
            config,
            resultProcessor: new StringProcessor(),
            eventBus: eventBus,
            sessionManagerService: sessionManager
        );

        await service.StartAsync(CancellationToken.None);

        try
        {
            using var client = new ClientWebSocket();
            await client.ConnectAsync(
                new Uri($"ws://127.0.0.1:{service.ListeningPorts.Single()}/"),
                CancellationToken.None
            );

            var connectedEvent = await eventBus.WaitForEventAsync<NetworkSessionConnectedEvent>(
                TimeSpan.FromSeconds(5)
            );

            await client.SendAsync(
                "NICK squid\r\n"u8.ToArray(),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );

            var dataEvent = await eventBus.WaitForEventAsync<NetworkSessionDataReceivedEvent>(
                TimeSpan.FromSeconds(5)
            );

            await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);

            var disconnectedEvent = await eventBus.WaitForEventAsync<NetworkSessionDisconnectedEvent>(
                TimeSpan.FromSeconds(5)
            );

            Assert.Equal(connectedEvent.Session.SessionId, dataEvent.Session.SessionId);
            Assert.Equal(connectedEvent.Session.SessionId, disconnectedEvent.Session.SessionId);
            Assert.Equal("NICK squid\r\n"u8.Length, dataEvent.Session.BytesReceived);
            Assert.Equal(NetworkSessionStatusType.Disconnected, disconnectedEvent.Session.Status);
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task StartAsync_SslWebSocketEntry_ReceivesIrcLine_PublishesProcessedStringResult()
    {
        using var temporaryRoot = new TemporaryDirectory();
        var directoriesConfig = new DirectoriesConfig(temporaryRoot.Path, Enum.GetNames<DirectoryType>());
        const string password = "password";
        var certificateFileName = "server.pfx";
        var expectedThumbprint = WriteCertificate(
            Path.Combine(directoriesConfig[DirectoryType.Certs], certificateFileName),
            password
        );
        var eventBus = new RecordingEventBus();
        var config = CreateNetworkConfig("0");
        config.SSLCertFile = certificateFileName;
        config.SSLCertPassword = password;
        config.Entries[0].Protocol = ServerProtocolType.SSL;
        config.Entries[0].Type = ServerType.WebSocket;
        var service = new NetworkServerService(
            config,
            directoriesConfig,
            resultProcessor: new StringProcessor(),
            eventBus: eventBus
        );

        await service.StartAsync(CancellationToken.None);

        try
        {
            using var client = new ClientWebSocket();
            client.Options.RemoteCertificateValidationCallback = (_, serverCertificate, _, _) =>
                string.Equals(
                    expectedThumbprint,
                    serverCertificate?.GetCertHashString(),
                    StringComparison.OrdinalIgnoreCase
                );
            await client.ConnectAsync(
                new Uri($"wss://127.0.0.1:{service.ListeningPorts.Single()}/"),
                CancellationToken.None
            );
            await client.SendAsync(
                "CAP LS 302\r\n"u8.ToArray(),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );

            var publishedEvent = await eventBus.WaitForEventAsync<NetworkResultReceivedEvent<string>>(
                TimeSpan.FromSeconds(5)
            );

            Assert.Equal("CAP LS 302", publishedEvent.Result);
            Assert.NotNull(publishedEvent.Connection);

            await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task StartAsync_ReceivesBlankLine_DoesNotPublishProcessedStringResult()
    {
        var eventBus = new RecordingEventBus();
        var service = new NetworkServerService(
            CreateNetworkConfig("0"),
            resultProcessor: new StringProcessor(),
            eventBus: eventBus
        );

        await service.StartAsync(CancellationToken.None);

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, service.ListeningPorts.Single());
            await client.GetStream().WriteAsync("\r\n"u8.ToArray());

            await Task.Delay(200);

            Assert.Empty(eventBus.Events);
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }
    }

    private static NetworkConfigSection CreateNetworkConfig(string ports)
    {
        var config = new NetworkConfigSection();

        config.Entries.Add(
            new()
            {
                IpAddress = "127.0.0.1",
                Mode = ServerModeType.Server,
                Ports = ports,
                Protocol = ServerProtocolType.Plain,
                Type = ServerType.TCP
            }
        );

        return config;
    }

    private static string WriteCertificate(string path, string password)
    {
        using var certificate = TestCertificateFactory.CreateSelfSignedCertificate();
        File.WriteAllBytes(path, certificate.Export(X509ContentType.Pfx, password));

        return certificate.Thumbprint;
    }
}
