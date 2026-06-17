using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrionIrcd.Network.Client;
using OrionIrcd.Network.Data.Options;
using OrionIrcd.Network.Data.Events;
using Serilog;

namespace OrionIrcd.Network.Server;

/// <summary>
///     Kestrel-backed WebSocket server with client lifecycle events and raw message dispatch.
/// </summary>
public sealed class OrionWebSocketServer : IAsyncDisposable, IDisposable
{
    private static readonly TimeSpan _acceptedSocketCloseTimeout = TimeSpan.FromSeconds(2);

    private readonly ConcurrentDictionary<long, OrionWebSocketClient> _clients = new();
    private readonly IPEndPoint _endPoint;
    private readonly SemaphoreSlim _lifecycleLock = new(1, 1);
    private readonly Serilog.ILogger _logger = Log.ForContext<OrionWebSocketServer>();
    private readonly OrionWebSocketServerTlsOptions? _tlsOptions;
    private WebApplication? _application;
    private int _port;
    private int _started;
    private CancellationTokenSource? _shutdownCancellationTokenSource;

    /// <summary>
    ///     Current listening port. Returns 0 when the server is stopped.
    /// </summary>
    public int Port => Volatile.Read(ref _port);

    /// <summary>
    ///     True when the server is currently accepting WebSocket connections.
    /// </summary>
    public bool IsRunning => Volatile.Read(ref _started) != 0;

    /// <summary>
    ///     Raised when a client connects.
    /// </summary>
    public event EventHandler<OrionWebSocketClientEventArgs>? OnClientConnect;

    /// <summary>
    ///     Raised when a client disconnects.
    /// </summary>
    public event EventHandler<OrionWebSocketClientEventArgs>? OnClientDisconnect;

    /// <summary>
    ///     Raised when a client sends a complete WebSocket message.
    /// </summary>
    public event EventHandler<OrionWebSocketDataReceivedEventArgs>? OnDataReceived;

    /// <summary>
    ///     Raised when an exception happens in accept or client loops.
    /// </summary>
    public event EventHandler<OrionWebSocketExceptionEventArgs>? OnException;

    public OrionWebSocketServer(
        IPEndPoint endPoint,
        OrionWebSocketServerTlsOptions? tlsOptions = null
    )
    {
        ArgumentNullException.ThrowIfNull(endPoint);

        _endPoint = endPoint;
        _tlsOptions = tlsOptions;
    }

    /// <summary>
    ///     Starts accepting WebSocket clients.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _lifecycleLock.WaitAsync(cancellationToken);

        try
        {
            if (IsRunning)
            {
                return;
            }

            var application = CreateApplication();
            var shutdownCancellationTokenSource = new CancellationTokenSource();

            try
            {
                await application.StartAsync(cancellationToken);
                _application = application;
                _shutdownCancellationTokenSource = shutdownCancellationTokenSource;
                Volatile.Write(ref _port, ResolveBoundPort(application));
                Volatile.Write(ref _started, 1);

                _logger.Information("WebSocket server listening on port {Port}", Port);
            }
            catch
            {
                shutdownCancellationTokenSource.Dispose();
                await application.DisposeAsync();
                Volatile.Write(ref _port, 0);
                Volatile.Write(ref _started, 0);

                throw;
            }
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    /// <summary>
    ///     Stops accepting new clients and closes all active clients.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _lifecycleLock.WaitAsync(cancellationToken);

        try
        {
            if (!IsRunning && _application is null && _clients.IsEmpty)
            {
                return;
            }

            Volatile.Write(ref _started, 0);
            Volatile.Write(ref _port, 0);

            var application = _application;
            _application = null;
            var shutdownCancellationTokenSource = _shutdownCancellationTokenSource;
            _shutdownCancellationTokenSource = null;
            shutdownCancellationTokenSource?.Cancel();

            try
            {
                if (application is not null)
                {
                    try
                    {
                        await application.StopAsync(cancellationToken);
                    }
                    finally
                    {
                        await application.DisposeAsync();
                    }
                }
            }
            finally
            {
                await DisposeClientsAsync();
                shutdownCancellationTokenSource?.Dispose();
            }
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    private WebApplication CreateApplication()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(OrionWebSocketServer).Assembly.FullName,
            EnvironmentName = Environments.Production
        });
        builder.Logging.ClearProviders();
        builder.WebHost.UseKestrel(options =>
        {
            options.Listen(_endPoint.Address, _endPoint.Port, listenOptions =>
            {
                if (_tlsOptions is not null)
                {
                    listenOptions.UseHttps(_tlsOptions.ServerCertificate);
                }
            });
        });

        var application = builder.Build();
        application.UseWebSockets();
        application.Run(HandleRequestAsync);

        return application;
    }

    private async Task HandleRequestAsync(HttpContext context)
    {
        if (context.Request.Path != "/" || !context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;

            return;
        }

        if (!TryGetShutdownToken(out _))
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;

            return;
        }

        OrionWebSocketClient? client = null;

        try
        {
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();

            if (!TryGetShutdownToken(out var shutdownToken))
            {
                await CloseAcceptedWebSocketAsync(webSocket);

                return;
            }

            using var clientCancellationTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(
                    context.RequestAborted,
                    shutdownToken
                );
            client = new OrionWebSocketClient(webSocket, CreateRemoteEndPoint(context));
            WireClientEvents(client);
            _clients[client.SessionId] = client;

            await client.RunAsync(clientCancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected when the request is aborted during shutdown.
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "WebSocket request failed");
            OnException?.Invoke(this, new OrionWebSocketExceptionEventArgs(ex, client));
        }
        finally
        {
            if (client is not null)
            {
                _clients.TryRemove(client.SessionId, out var _);
            }
        }
    }

    private async Task DisposeClientsAsync()
    {
        var clients = _clients.Values.ToArray();
        var disposeTasks = new Task[clients.Length];

        for (var i = 0; i < clients.Length; i++)
        {
            disposeTasks[i] = clients[i].DisposeAsync().AsTask();
        }

        if (disposeTasks.Length > 0)
        {
            await Task.WhenAll(disposeTasks);
        }

        _clients.Clear();
    }

    private static EndPoint? CreateRemoteEndPoint(HttpContext context)
    {
        var remoteAddress = context.Connection.RemoteIpAddress;

        if (remoteAddress is null)
        {
            return null;
        }

        return new IPEndPoint(remoteAddress, context.Connection.RemotePort);
    }

    private static int ResolveBoundPort(WebApplication application)
    {
        var server = application.Services.GetRequiredService<IServer>();
        var addressFeature = server.Features.Get<IServerAddressesFeature>();
        var address = addressFeature?.Addresses.FirstOrDefault();

        if (address is not null && Uri.TryCreate(address, UriKind.Absolute, out var uri))
        {
            return uri.Port;
        }

        return 0;
    }

    private static async Task CloseAcceptedWebSocketAsync(WebSocket webSocket)
    {
        try
        {
            if (webSocket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                using var cancellationTokenSource = new CancellationTokenSource(
                    _acceptedSocketCloseTimeout
                );
                await webSocket.CloseOutputAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Server shutting down",
                    cancellationTokenSource.Token
                );
            }
        }
        catch (OperationCanceledException)
        {
            // Close timed out; aborting below will release the socket.
        }
        catch (WebSocketException)
        {
            // Peer may already be gone.
        }
        catch (ObjectDisposedException)
        {
            // Socket is already disposed.
        }
        finally
        {
            webSocket.Abort();
            webSocket.Dispose();
        }
    }

    private bool TryGetShutdownToken(out CancellationToken shutdownToken)
    {
        shutdownToken = default;

        if (!IsRunning)
        {
            return false;
        }

        var shutdownCancellationTokenSource = Volatile.Read(
            ref _shutdownCancellationTokenSource
        );

        if (shutdownCancellationTokenSource is null)
        {
            return false;
        }

        try
        {
            if (shutdownCancellationTokenSource.IsCancellationRequested)
            {
                return false;
            }

            shutdownToken = shutdownCancellationTokenSource.Token;

            return true;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }

    private void WireClientEvents(OrionWebSocketClient client)
    {
        client.OnConnected += (_, args) =>
        {
            _logger.Information(
                "OnWebSocketClientConnect. SessionId={SessionId}, RemoteEndPoint={RemoteEndPoint}",
                args.Client.SessionId,
                args.Client.RemoteEndPoint
            );
            OnClientConnect?.Invoke(this, args);
        };
        client.OnDataReceived += (_, args) =>
        {
            _logger.Verbose(
                "OnWebSocketDataReceived. SessionId={SessionId}, Bytes={Bytes}",
                args.Client.SessionId,
                args.Data.Length
            );
            OnDataReceived?.Invoke(this, args);
        };
        client.OnException += (_, args) =>
        {
            _logger.Error(
                args.Exception,
                "OnWebSocketException. SessionId={SessionId}",
                args.Client?.SessionId
            );
            OnException?.Invoke(this, args);
        };
        client.OnDisconnected += (_, args) =>
        {
            _clients.TryRemove(args.Client.SessionId, out var _);
            _logger.Information(
                "OnWebSocketClientDisconnect. SessionId={SessionId}, RemoteEndPoint={RemoteEndPoint}",
                args.Client.SessionId,
                args.Client.RemoteEndPoint
            );
            OnClientDisconnect?.Invoke(this, args);
        };
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
        _lifecycleLock.Dispose();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
