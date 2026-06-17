using System.Net;
using System.Security.Cryptography.X509Certificates;
using OrionIrcd.Core.Data.Config.Sections;
using OrionIrcd.Core.Directories;
using OrionIrcd.Core.Extensions.Directories;
using OrionIrcd.Core.Interfaces.Events;
using OrionIrcd.Core.Interfaces.Services;
using OrionIrcd.Core.Types;
using OrionIrcd.Core.Utils;
using OrionIrcd.Network.Data.Options;
using OrionIrcd.Network.Data.Events;
using OrionIrcd.Network.Interfaces.Client;
using OrionIrcd.Network.Interfaces.Processing;
using OrionIrcd.Network.Server;
using OrionIrcd.Server.Data.Events;
using Serilog;

namespace OrionIrcd.Server.Services.Network;

public sealed class NetworkServerService : IOrionIrcdService
{
    private readonly ILogger _logger = Log.ForContext<NetworkServerService>();
    private readonly DirectoriesConfig? _directoriesConfig;
    private readonly IEventBus? _eventBus;
    private readonly NetworkConfigSection _networkConfigSection;
    private readonly IResultProcessor<string> _resultProcessor;
    private readonly Lock _sync = new();
    private readonly List<OrionTcpServer> _tcpServers = [];
    private readonly List<OrionWebSocketServer> _webSocketServers = [];

    private int _started;

    public NetworkServerService(
        NetworkConfigSection networkConfigSection,
        DirectoriesConfig? directoriesConfig = null,
        IResultProcessor<string>? resultProcessor = null,
        IEventBus? eventBus = null
    )
    {
        _networkConfigSection = networkConfigSection;
        _directoriesConfig = directoriesConfig;
        _resultProcessor = resultProcessor ?? new StringProcessor();
        _eventBus = eventBus;
    }

    public bool IsRunning => Volatile.Read(ref _started) != 0;

    public int ListenerCount
    {
        get
        {
            lock (_sync)
            {
                return _tcpServers.Count + _webSocketServers.Count;
            }
        }
    }

    public IReadOnlyList<int> ListeningPorts
    {
        get
        {
            lock (_sync)
            {
                return _tcpServers.Select(server => server.Port)
                                  .Concat(_webSocketServers.Select(server => server.Port))
                                  .ToArray();
            }
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _started, 1) != 0)
        {
            return;
        }

        var startedTcpServers = new List<OrionTcpServer>();
        var startedWebSocketServers = new List<OrionWebSocketServer>();

        try
        {
            foreach (var entry in _networkConfigSection.Entries)
            {
                switch (entry.Type)
                {
                    case ServerType.TCP:
                        foreach (var server in CreateTcpServers(entry))
                        {
                            WireTcpServerEvents(server);
                            await server.StartAsync(cancellationToken).ConfigureAwait(false);
                            startedTcpServers.Add(server);

                            _logger.Information(
                                "Network listener started on port {Port}",
                                server.Port
                            );
                        }

                        break;

                    case ServerType.WebSocket:
                        foreach (var server in CreateWebSocketServers(entry))
                        {
                            WireWebSocketServerEvents(server);
                            await server.StartAsync(cancellationToken).ConfigureAwait(false);
                            startedWebSocketServers.Add(server);

                            _logger.Information(
                                "WebSocket network listener started on port {Port}",
                                server.Port
                            );
                        }

                        break;

                    default:
                        throw new NotSupportedException($"Network server type '{entry.Type}' is not supported yet.");
                }
            }

            lock (_sync)
            {
                _tcpServers.AddRange(startedTcpServers);
                _webSocketServers.AddRange(startedWebSocketServers);
            }
        }
        catch
        {
            try
            {
                await Task.WhenAll(
                              StopStartedTcpServersAsync(startedTcpServers, CancellationToken.None),
                              StopStartedWebSocketServersAsync(startedWebSocketServers, CancellationToken.None)
                          )
                          .ConfigureAwait(false);
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

        OrionTcpServer[] servers;
        OrionWebSocketServer[] webSocketServers;

        lock (_sync)
        {
            servers = [.. _tcpServers];
            webSocketServers = [.. _webSocketServers];
            _tcpServers.Clear();
            _webSocketServers.Clear();
        }

        await Task.WhenAll(
                      StopStartedTcpServersAsync(servers, cancellationToken),
                      StopStartedWebSocketServersAsync(webSocketServers, cancellationToken)
                  )
                  .ConfigureAwait(false);
    }

    private IEnumerable<OrionTcpServer> CreateTcpServers(NetworkSectionEntry entry)
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

    private IEnumerable<OrionWebSocketServer> CreateWebSocketServers(NetworkSectionEntry entry)
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

        if (certificate is null)
        {
            return null;
        }

        return new OrionTcpServerTlsOptions(certificate);
    }

    private OrionWebSocketServerTlsOptions? CreateWebSocketTlsOptions(NetworkSectionEntry entry)
    {
        var certificate = LoadCertificate(entry);

        if (certificate is null)
        {
            return null;
        }

        return new OrionWebSocketServerTlsOptions(certificate);
    }

    private X509Certificate2? LoadCertificate(NetworkSectionEntry entry)
    {
        if (entry.Protocol == ServerProtocolType.Plain)
        {
            return null;
        }

        var certificatePath = ResolveCertificatePath(_networkConfigSection.SSLCertFile);
        var password = _networkConfigSection.SSLCertPassword ?? string.Empty;

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

        if (_directoriesConfig is not null)
        {
            return Path.Combine(_directoriesConfig[DirectoryType.Certs], resolvedCertificateFile);
        }

        return Path.GetFullPath(resolvedCertificateFile);
    }

    private static async Task StopTcpServerAsync(OrionTcpServer server, CancellationToken cancellationToken)
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

    private static async Task StopWebSocketServerAsync(
        OrionWebSocketServer server,
        CancellationToken cancellationToken
    )
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

    private static Task StopStartedTcpServersAsync(
        IEnumerable<OrionTcpServer> servers,
        CancellationToken cancellationToken
    )
        => Task.WhenAll(servers.Select(server => StopTcpServerAsync(server, cancellationToken)));

    private static Task StopStartedWebSocketServersAsync(
        IEnumerable<OrionWebSocketServer> servers,
        CancellationToken cancellationToken
    )
        => Task.WhenAll(servers.Select(server => StopWebSocketServerAsync(server, cancellationToken)));

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

    private void OnTcpClientConnect(object? sender, OrionTcpClientEventArgs args)
        => _logger.Information(
            "Client connected. SessionId={SessionId}, RemoteEndPoint={RemoteEndPoint}",
            args.Client.SessionId,
            args.Client.RemoteEndPoint
        );

    private void OnTcpClientDisconnect(object? sender, OrionTcpClientEventArgs args)
        => _logger.Information(
            "Client disconnected. SessionId={SessionId}, RemoteEndPoint={RemoteEndPoint}",
            args.Client.SessionId,
            args.Client.RemoteEndPoint
        );

    private void OnTcpDataReceived(object? sender, OrionTcpDataReceivedEventArgs args)
    {
        _logger.Verbose(
            "Client data received. SessionId={SessionId}, Bytes={Bytes}",
            args.Client.SessionId,
            args.Data.Length
        );

        QueueReceivedData(args.Client, args.Data);
    }

    private void OnTcpException(object? sender, OrionTcpExceptionEventArgs args)
        => _logger.Error(args.Exception, "Network listener failed");

    private void OnWebSocketClientConnect(object? sender, OrionWebSocketClientEventArgs args)
        => _logger.Information(
            "WebSocket client connected. SessionId={SessionId}, RemoteEndPoint={RemoteEndPoint}",
            args.Client.SessionId,
            args.Client.RemoteEndPoint
        );

    private void OnWebSocketClientDisconnect(object? sender, OrionWebSocketClientEventArgs args)
        => _logger.Information(
            "WebSocket client disconnected. SessionId={SessionId}, RemoteEndPoint={RemoteEndPoint}",
            args.Client.SessionId,
            args.Client.RemoteEndPoint
        );

    private void OnWebSocketDataReceived(object? sender, OrionWebSocketDataReceivedEventArgs args)
    {
        _logger.Verbose(
            "WebSocket client data received. SessionId={SessionId}, Bytes={Bytes}",
            args.Client.SessionId,
            args.Data.Length
        );

        QueueReceivedData(args.Client, args.Data);
    }

    private void OnWebSocketException(object? sender, OrionWebSocketExceptionEventArgs args)
        => _logger.Error(
            args.Exception,
            "WebSocket network listener failed. SessionId={SessionId}",
            args.Client?.SessionId
        );

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
