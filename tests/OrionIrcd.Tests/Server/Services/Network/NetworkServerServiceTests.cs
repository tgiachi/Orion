using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using OrionIrcd.Core.Data.Config.Sections;
using OrionIrcd.Core.Directories;
using OrionIrcd.Core.Types;
using OrionIrcd.Server.Data.Events;
using OrionIrcd.Server.Services.Network;
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
    public async Task StartAsync_UnsupportedServerType_ThrowsAndResetsState()
    {
        var config = CreateNetworkConfig("0");
        config.Entries[0].Type = ServerType.WebSocket;
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
            Assert.NotNull(publishedEvent.Client);
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

    private static void WriteCertificate(string path, string password)
    {
        using var certificate = TestCertificateFactory.CreateSelfSignedCertificate();
        File.WriteAllBytes(path, certificate.Export(X509ContentType.Pfx, password));
    }
}
