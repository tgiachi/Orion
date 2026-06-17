using System.Net;
using System.Security.Cryptography.X509Certificates;
using OrionIrcd.Core.Data.Config.Sections;
using OrionIrcd.Core.Directories;
using OrionIrcd.Core.Extensions.Directories;
using OrionIrcd.Core.Interfaces.Events;
using OrionIrcd.Core.Interfaces.Services;
using OrionIrcd.Core.Types;
using OrionIrcd.Core.Utils;
using OrionIrcd.Network.Events;
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
                return _tcpServers.Count;
            }
        }
    }

    public IReadOnlyList<int> ListeningPorts
    {
        get
        {
            lock (_sync)
            {
                return _tcpServers.Select(server => server.Port).ToArray();
            }
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _started, 1) != 0)
        {
            return;
        }

        var startedServers = new List<OrionTcpServer>();

        try
        {
            foreach (var entry in _networkConfigSection.Entries)
            {
                foreach (var server in CreateTcpServers(entry))
                {
                    WireServerEvents(server);
                    await server.StartAsync(cancellationToken).ConfigureAwait(false);
                    startedServers.Add(server);

                    _logger.Information(
                        "Network listener started on port {Port}",
                        server.Port
                    );
                }
            }

            lock (_sync)
            {
                _tcpServers.AddRange(startedServers);
            }
        }
        catch
        {
            await StopStartedServersAsync(startedServers, CancellationToken.None).ConfigureAwait(false);
            Interlocked.Exchange(ref _started, 0);

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

        lock (_sync)
        {
            servers = [.. _tcpServers];
            _tcpServers.Clear();
        }

        await StopStartedServersAsync(servers, cancellationToken).ConfigureAwait(false);
    }

    private IEnumerable<OrionTcpServer> CreateTcpServers(NetworkSectionEntry entry)
    {
        if (entry.Mode != ServerModeType.Server)
        {
            throw new NotSupportedException($"Network mode '{entry.Mode}' is not supported yet.");
        }

        if (entry.Type != ServerType.TCP)
        {
            throw new NotSupportedException($"Network server type '{entry.Type}' is not supported yet.");
        }

        if (entry.Protocol is not ServerProtocolType.Plain and not ServerProtocolType.SSL)
        {
            throw new NotSupportedException($"Network protocol '{entry.Protocol}' is not supported yet.");
        }

        var ipAddress = NetworkUtils.ParseIpAddress(entry.IpAddress);
        var ports = NetworkUtils.ParsePorts(entry.Ports).Distinct();
        var tlsOptions = CreateTlsOptions(entry);

        foreach (var port in ports)
        {
            yield return new OrionTcpServer(
                new IPEndPoint(ipAddress, port),
                new IrcLineFramer(),
                tlsOptions: tlsOptions
            );
        }
    }

    private OrionTcpServerTlsOptions? CreateTlsOptions(NetworkSectionEntry entry)
    {
        if (entry.Protocol == ServerProtocolType.Plain)
        {
            return null;
        }

        var certificatePath = ResolveCertificatePath(_networkConfigSection.SSLCertFile);
        var password = _networkConfigSection.SSLCertPassword ?? string.Empty;
        var certificate = X509CertificateLoader.LoadPkcs12FromFile(
            certificatePath,
            password,
            X509KeyStorageFlags.EphemeralKeySet
        );

        return new OrionTcpServerTlsOptions(certificate);
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

    private static async Task StopServerAsync(OrionTcpServer server, CancellationToken cancellationToken)
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

    private static Task StopStartedServersAsync(IEnumerable<OrionTcpServer> servers, CancellationToken cancellationToken)
        => Task.WhenAll(servers.Select(server => StopServerAsync(server, cancellationToken)));

    private void WireServerEvents(OrionTcpServer server)
    {
        server.OnClientConnect += OnClientConnect;
        server.OnClientDisconnect += OnClientDisconnect;
        server.OnDataReceived += OnDataReceived;
        server.OnException += OnException;
    }

    private void OnClientConnect(object? sender, OrionTcpClientEventArgs args)
        => _logger.Information(
            "Client connected. SessionId={SessionId}, RemoteEndPoint={RemoteEndPoint}",
            args.Client.SessionId,
            args.Client.RemoteEndPoint
        );

    private void OnClientDisconnect(object? sender, OrionTcpClientEventArgs args)
        => _logger.Information(
            "Client disconnected. SessionId={SessionId}, RemoteEndPoint={RemoteEndPoint}",
            args.Client.SessionId,
            args.Client.RemoteEndPoint
        );

    private void OnDataReceived(object? sender, OrionTcpDataReceivedEventArgs args)
    {
        _logger.Verbose(
            "Client data received. SessionId={SessionId}, Bytes={Bytes}",
            args.Client.SessionId,
            args.Data.Length
        );

        _ = Task.Run(() => ProcessReceivedDataAsync(args, CancellationToken.None));
    }

    private async Task ProcessReceivedDataAsync(
        OrionTcpDataReceivedEventArgs args,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var result = await _resultProcessor.ProcessAsync(
                args.Client,
                args.Data,
                cancellationToken
            ).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(result))
            {
                return;
            }

            _eventBus?.Publish(new NetworkResultReceivedEvent<string>(args.Client, result));
        }
        catch (Exception exception)
        {
            _logger.Error(
                exception,
                "Network result processor failed. SessionId={SessionId}",
                args.Client.SessionId
            );
        }
    }

    private void OnException(object? sender, OrionTcpExceptionEventArgs args)
        => _logger.Error(args.Exception, "Network listener failed");
}
