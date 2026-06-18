using System.Net;
using System.Security.Cryptography.X509Certificates;
using OrionIrcd.Core.Data.Config.Sections;
using OrionIrcd.Core.Directories;
using OrionIrcd.Core.Extensions.Directories;
using OrionIrcd.Core.Interfaces.Events;
using OrionIrcd.Core.Interfaces.Services;
using OrionIrcd.Core.Types;
using OrionIrcd.Core.Utils;
using OrionIrcd.Network.Data.Events;
using OrionIrcd.Network.Data.Options;
using OrionIrcd.Network.Interfaces.Client;
using OrionIrcd.Network.Interfaces.Processing;
using OrionIrcd.Network.Interfaces.Server;
using OrionIrcd.Network.Server;
using OrionIrcd.Server.Data.Events;
using OrionIrcd.Server.Interfaces.Services;
using Serilog;

namespace OrionIrcd.Server.Services.Network;

public sealed class NetworkServerService : IOrionIrcdService
{
    private readonly ILogger _logger = Log.ForContext<NetworkServerService>();
    private readonly DirectoriesConfig? _directoriesConfig;
    private readonly IEventBus? _eventBus;
    private readonly NetworkConfigSection _networkConfigSection;
    private readonly IResultProcessor<string> _resultProcessor;
    private readonly ISessionManagerService? _sessionManagerService;
    private readonly Lock _sync = new();
    private readonly List<INetworkServer> _servers = [];

    private int _started;

    public NetworkServerService(
        NetworkConfigSection networkConfigSection,
        DirectoriesConfig? directoriesConfig = null,
        IResultProcessor<string>? resultProcessor = null,
        IEventBus? eventBus = null,
        ISessionManagerService? sessionManagerService = null
    )
    {
        _networkConfigSection = networkConfigSection;
        _directoriesConfig = directoriesConfig;
        _resultProcessor = resultProcessor ?? new StringProcessor();
        _eventBus = eventBus;
        _sessionManagerService = sessionManagerService;
    }

    public bool IsRunning => Volatile.Read(ref _started) != 0;

    public int ListenerCount
    {
        get
        {
            lock (_sync)
            {
                return _servers.Count;
            }
        }
    }

    public IReadOnlyList<int> ListeningPorts
    {
        get
        {
            lock (_sync)
            {
                return _servers.Select(server => server.Port).ToArray();
            }
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _started, 1) != 0)
        {
            return;
        }

        var startedServers = new List<INetworkServer>();

        try
        {
            foreach (var entry in _networkConfigSection.Entries)
            {
                foreach (var server in CreateServers(entry))
                {
                    WireServerEvents(server);
                    await server.StartAsync(cancellationToken).ConfigureAwait(false);
                    startedServers.Add(server);

                    _logger.Information(
                        "Network listener started. Type={ServerType}, Port={Port}",
                        server.ServerType,
                        server.Port
                    );
                }
            }

            lock (_sync)
            {
                _servers.AddRange(startedServers);
            }
        }
        catch
        {
            try
            {
                await StopStartedServersAsync(startedServers, CancellationToken.None).ConfigureAwait(false);
            }
            finally
            {
                Interlocked.Exchange(ref _started, 0);
            }

            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _started, 0) == 0)
        {
            return;
        }

        INetworkServer[] servers;

        lock (_sync)
        {
            servers = [.. _servers];
            _servers.Clear();
        }

        await StopStartedServersAsync(servers, cancellationToken).ConfigureAwait(false);
    }

    private IEnumerable<INetworkServer> CreateServers(NetworkSectionEntry entry)
    {
        return entry.Type switch
        {
            ServerType.TCP => CreateTcpServers(entry),
            ServerType.WebSocket => CreateWebSocketServers(entry),
            ServerType.UDP => CreateUdpServers(entry),
            _ => throw new NotSupportedException($"Network server type '{entry.Type}' is not supported yet.")
        };
    }

    private IEnumerable<INetworkServer> CreateTcpServers(NetworkSectionEntry entry)
    {
        EnsureSupportedModeAndProtocol(entry);

        if (entry.Type != ServerType.TCP)
        {
            throw new NotSupportedException($"Network server type '{entry.Type}' is not supported yet.");
        }

        var ipAddress = NetworkUtils.ParseIpAddress(entry.IpAddress);
        var ports = NetworkUtils.ParsePorts(entry.Ports).Distinct();
        var tlsOptions = CreateTcpTlsOptions(entry);

        foreach (var port in ports)
        {
            yield return new OrionTcpServer(
                new IPEndPoint(ipAddress, port),
                new IrcLineFramer(),
                tlsOptions: tlsOptions
            );
        }
    }

    private IEnumerable<INetworkServer> CreateWebSocketServers(NetworkSectionEntry entry)
    {
        EnsureSupportedModeAndProtocol(entry);

        if (entry.Type != ServerType.WebSocket)
        {
            throw new NotSupportedException($"Network server type '{entry.Type}' is not supported yet.");
        }

        var ipAddress = NetworkUtils.ParseIpAddress(entry.IpAddress);
        var ports = NetworkUtils.ParsePorts(entry.Ports).Distinct();
        var tlsOptions = CreateWebSocketTlsOptions(entry);

        foreach (var port in ports)
        {
            yield return new OrionWebSocketServer(
                new IPEndPoint(ipAddress, port),
                tlsOptions
            );
        }
    }

    private IEnumerable<INetworkServer> CreateUdpServers(NetworkSectionEntry entry)
    {
        EnsureSupportedModeAndProtocol(entry);

        if (entry.Protocol != ServerProtocolType.Plain)
        {
            throw new NotSupportedException($"UDP protocol '{entry.Protocol}' is not supported yet.");
        }

        var ipAddress = NetworkUtils.ParseIpAddress(entry.IpAddress);
        var ports = NetworkUtils.ParsePorts(entry.Ports).Distinct();
        var bindAllInterfaces = ipAddress.Equals(IPAddress.Any) || ipAddress.Equals(IPAddress.IPv6Any);

        foreach (var port in ports)
        {
            yield return new OrionUdpServer(
                new IPEndPoint(ipAddress, port),
                bindAllInterfaces
            );
        }
    }

    private static void EnsureSupportedModeAndProtocol(NetworkSectionEntry entry)
    {
        if (entry.Mode != ServerModeType.Server)
        {
            throw new NotSupportedException($"Network mode '{entry.Mode}' is not supported yet.");
        }

        if (entry.Protocol is not ServerProtocolType.Plain and not ServerProtocolType.SSL)
        {
            throw new NotSupportedException($"Network protocol '{entry.Protocol}' is not supported yet.");
        }
    }

    private OrionTcpServerTlsOptions? CreateTcpTlsOptions(NetworkSectionEntry entry)
    {
        var certificate = LoadCertificate(entry);

        return certificate is null ? null : new OrionTcpServerTlsOptions(certificate);
    }

    private OrionWebSocketServerTlsOptions? CreateWebSocketTlsOptions(NetworkSectionEntry entry)
    {
        var certificate = LoadCertificate(entry);

        return certificate is null ? null : new OrionWebSocketServerTlsOptions(certificate);
    }

    private X509Certificate2? LoadCertificate(NetworkSectionEntry entry)
    {
        if (entry.Protocol == ServerProtocolType.Plain)
        {
            return null;
        }

        var certificatePath = ResolveCertificatePath(_networkConfigSection.SSLCertFile);
        var password = _networkConfigSection.SSLCertPassword ?? "";

        return X509CertificateLoader.LoadPkcs12FromFile(
            certificatePath,
            password,
            X509KeyStorageFlags.EphemeralKeySet
        );
    }

    private string ResolveCertificatePath(string certificateFile)
    {
        if (string.IsNullOrWhiteSpace(certificateFile))
        {
            throw new InvalidOperationException("SSL certificate file must be configured for SSL listeners.");
        }

        var resolvedCertificateFile = certificateFile.ResolvePathAndEnvs();

        if (Path.IsPathRooted(resolvedCertificateFile))
        {
            return resolvedCertificateFile;
        }

        return _directoriesConfig is not null ? Path.Combine(_directoriesConfig[DirectoryType.Certs], resolvedCertificateFile) : Path.GetFullPath(resolvedCertificateFile);
    }

    private static async Task StopServerAsync(INetworkServer server, CancellationToken cancellationToken)
    {
        try
        {
            await server.StopAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await server.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static Task StopStartedServersAsync(
        IEnumerable<INetworkServer> servers,
        CancellationToken cancellationToken
    )
        => Task.WhenAll(servers.Select(server => StopServerAsync(server, cancellationToken)));

    private void WireServerEvents(INetworkServer server)
    {
        switch (server)
        {
            case OrionTcpServer tcpServer:
                WireTcpServerEvents(tcpServer);

                break;

            case OrionWebSocketServer webSocketServer:
                WireWebSocketServerEvents(webSocketServer);

                break;

            case OrionUdpServer udpServer:
                WireUdpServerEvents(udpServer);

                break;

            default:
                throw new NotSupportedException($"Network server '{server.GetType().Name}' is not supported yet.");
        }
    }

    private void WireTcpServerEvents(OrionTcpServer server)
    {
        server.OnClientConnect += OnTcpClientConnect;
        server.OnClientDisconnect += OnTcpClientDisconnect;
        server.OnDataReceived += OnTcpDataReceived;
        server.OnException += OnTcpException;
    }

    private void WireWebSocketServerEvents(OrionWebSocketServer server)
    {
        server.OnClientConnect += OnWebSocketClientConnect;
        server.OnClientDisconnect += OnWebSocketClientDisconnect;
        server.OnDataReceived += OnWebSocketDataReceived;
        server.OnException += OnWebSocketException;
    }

    private void WireUdpServerEvents(OrionUdpServer server)
    {
        server.OnException += OnUdpException;
    }

    private void OnTcpClientConnect(object? sender, OrionTcpClientEventArgs args)
    {
        _logger.Information(
            "Client connected. SessionId={SessionId}, RemoteEndPoint={RemoteEndPoint}",
            args.Client.SessionId,
            args.Client.RemoteEndPoint
        );

        _sessionManagerService?.Register(args.Client);
    }

    private void OnTcpClientDisconnect(object? sender, OrionTcpClientEventArgs args)
    {
        _logger.Information(
            "Client disconnected. SessionId={SessionId}, RemoteEndPoint={RemoteEndPoint}",
            args.Client.SessionId,
            args.Client.RemoteEndPoint
        );

        _sessionManagerService?.Unregister(args.Client);
    }

    private void OnTcpDataReceived(object? sender, OrionTcpDataReceivedEventArgs args)
    {
        _logger.Verbose(
            "Client data received. SessionId={SessionId}, Bytes={Bytes}",
            args.Client.SessionId,
            args.Data.Length
        );

        _sessionManagerService?.RecordActivity(args.Client, args.Data);
        QueueReceivedData(args.Client, args.Data);
    }

    private void OnTcpException(object? sender, OrionTcpExceptionEventArgs args)
        => _logger.Error(args.Exception, "Network listener failed");

    private void OnWebSocketClientConnect(object? sender, OrionWebSocketClientEventArgs args)
    {
        _logger.Information(
            "WebSocket client connected. SessionId={SessionId}, RemoteEndPoint={RemoteEndPoint}",
            args.Client.SessionId,
            args.Client.RemoteEndPoint
        );

        _sessionManagerService?.Register(args.Client);
    }

    private void OnWebSocketClientDisconnect(object? sender, OrionWebSocketClientEventArgs args)
    {
        _logger.Information(
            "WebSocket client disconnected. SessionId={SessionId}, RemoteEndPoint={RemoteEndPoint}",
            args.Client.SessionId,
            args.Client.RemoteEndPoint
        );

        _sessionManagerService?.Unregister(args.Client);
    }

    private void OnWebSocketDataReceived(object? sender, OrionWebSocketDataReceivedEventArgs args)
    {
        _logger.Verbose(
            "WebSocket client data received. SessionId={SessionId}, Bytes={Bytes}",
            args.Client.SessionId,
            args.Data.Length
        );

        _sessionManagerService?.RecordActivity(args.Client, args.Data);
        QueueReceivedData(args.Client, args.Data);
    }

    private void OnWebSocketException(object? sender, OrionWebSocketExceptionEventArgs args)
        => _logger.Error(
            args.Exception,
            "WebSocket network listener failed. SessionId={SessionId}",
            args.Client?.SessionId
        );

    private void OnUdpException(object? sender, OrionTcpExceptionEventArgs args)
        => _logger.Error(args.Exception, "UDP network listener failed");

    private void QueueReceivedData(INetworkConnection connection, ReadOnlyMemory<byte> data)
        => _ = Task.Run(() => ProcessReceivedDataAsync(connection, data, CancellationToken.None));

    private async Task ProcessReceivedDataAsync(
        INetworkConnection connection,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var result = await _resultProcessor.ProcessAsync(
                                                   connection,
                                                   data,
                                                   cancellationToken
                                               )
                                               .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(result))
            {
                return;
            }

            await _eventBus?.PublishAsync(new NetworkResultReceivedEvent<string>(connection, result), cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.Error(
                exception,
                "Network result processor failed. SessionId={SessionId}",
                connection.SessionId
            );
        }
    }
}
