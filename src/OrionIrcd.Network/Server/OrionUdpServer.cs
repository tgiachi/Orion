using System.Net;
using System.Net.Sockets;
using OrionIrcd.Core.Types;
using OrionIrcd.Core.Utils;
using OrionIrcd.Network.Data.Events;
using OrionIrcd.Network.Interfaces.Server;
using Serilog;

namespace OrionIrcd.Network.Server;

/// <summary>
/// Connectionless UDP server that binds one socket per local interface address and processes
/// each received datagram. By default it echoes the payload back to the sender (the behaviour the
/// UO launcher expects from a shard ping server); supply <see cref="OnDatagram" /> to customise the
/// response. Supports Start/Stop/Start cycles by recreating the sockets on each Start.
/// </summary>
public sealed class OrionUdpServer : INetworkServer, IAsyncDisposable, IDisposable
{
    private readonly bool _bindAllInterfaces;
    private readonly IPEndPoint _endPoint;
    private readonly List<UdpClient> _listeners = [];
    private readonly ILogger _logger = Log.ForContext<OrionUdpServer>();
    private readonly List<Task> _receiveLoops = [];
    private readonly Lock _sync = new();

    private CancellationTokenSource? _cancellationTokenSource;
    private int _started;

    /// <summary>
    /// Transport type exposed by this server.
    /// </summary>
    public ServerType ServerType => ServerType.UDP;

    /// <summary>
    /// Current listening port. Returns 0 when configured for an ephemeral port and stopped.
    /// </summary>
    public int Port
    {
        get
        {
            lock (_sync)
            {
                var listener = _listeners.FirstOrDefault();

                if (listener?.Client.LocalEndPoint is IPEndPoint localEndPoint)
                {
                    return localEndPoint.Port;
                }

                return _endPoint.Port;
            }
        }
    }

    /// <summary>
    /// Optional response factory. Receives the datagram payload and the sender endpoint and returns
    /// the bytes to send back; return <see cref="ReadOnlyMemory{T}.Empty" /> to send no reply.
    /// When <c>null</c>, the server echoes the payload unchanged.
    /// </summary>
    public Func<ReadOnlyMemory<byte>, IPEndPoint, ReadOnlyMemory<byte>>? OnDatagram { get; set; }

    /// <summary>
    /// True when the server is currently listening.
    /// </summary>
    public bool IsRunning => Volatile.Read(ref _started) != 0;

    /// <summary>
    /// Number of bound listening sockets.
    /// </summary>
    public int ListenerCount
    {
        get
        {
            lock (_sync)
            {
                return _listeners.Count;
            }
        }
    }

    /// <summary>
    /// Raised when receive loops throw an unexpected exception.
    /// </summary>
    public event EventHandler<OrionTcpExceptionEventArgs>? OnException;

    /// <summary>
    /// Initializes a UDP server bound to the given endpoint on every <see cref="StartAsync" />.
    /// </summary>
    /// <param name="endPoint">Endpoint supplying the port (and address when not binding all interfaces).</param>
    /// <param name="bindAllInterfaces">
    /// When <c>true</c> (default), binds one socket per local unicast address matching the endpoint's
    /// address family. When <c>false</c>, binds only <paramref name="endPoint" />.
    /// </param>
    public OrionUdpServer(IPEndPoint endPoint, bool bindAllInterfaces = true)
    {
        ArgumentNullException.ThrowIfNull(endPoint);

        _endPoint = endPoint;
        _bindAllInterfaces = bindAllInterfaces;
    }

    /// <inheritdoc />
    public void Dispose()
        => DisposeAsync().AsTask().GetAwaiter().GetResult();

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
        => await StopAsync(CancellationToken.None);

    /// <summary>
    /// Starts listening, binding sockets and launching a receive loop per socket. Recreates the
    /// sockets on every call, so Stop/Start cycles are supported.
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _started, 1) != 0)
        {
            return Task.CompletedTask;
        }

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        lock (_sync)
        {
            _cancellationTokenSource = cts;

            foreach (var endPoint in ResolveBindEndPoints())
            {
                var listener = CreateListener(endPoint);

                if (listener is null)
                {
                    continue;
                }

                _listeners.Add(listener);
                _receiveLoops.Add(Task.Run(() => ReceiveLoopAsync(listener, cts.Token), CancellationToken.None));
                _logger.Information("UDP server listening on {LocalEndPoint}", listener.Client.LocalEndPoint);
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops listening, closing every socket and awaiting the receive loops.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _started, 0) == 0)
        {
            return;
        }

        CancellationTokenSource? cts;
        UdpClient[] listeners;
        Task[] loops;

        lock (_sync)
        {
            cts = _cancellationTokenSource;
            _cancellationTokenSource = null;
            listeners = [.. _listeners];
            loops = [.. _receiveLoops];
            _listeners.Clear();
            _receiveLoops.Clear();
        }

        if (cts is not null)
        {
            await cts.CancelAsync();
            cts.Dispose();
        }

        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].Close();
            listeners[i].Dispose();
        }

        if (loops.Length > 0)
        {
            try
            {
                await Task.WhenAll(loops).WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Stop was cancelled by caller, or loops cancelled during shutdown.
            }
            catch (ObjectDisposedException)
            {
                // Socket disposed mid-flight during shutdown.
            }
            catch (SocketException)
            {
                // Socket faulted during shutdown.
            }
        }
    }

    private UdpClient? CreateListener(IPEndPoint endPoint)
    {
        var socket = new Socket(endPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp)
        {
            ExclusiveAddressUse = false
        };

        try
        {
            socket.Bind(endPoint);

            return new()
            {
                Client = socket
            };
        }
        catch (SocketException ex)
        {
            _logger.Warning(
                ex,
                "Failed to bind UDP listener on {Address}:{Port}",
                endPoint.Address,
                endPoint.Port
            );
            socket.Dispose();

            return null;
        }
    }

    private async Task ReceiveLoopAsync(UdpClient listener, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = await listener.ReceiveAsync(cancellationToken);
                var response = OnDatagram is null
                                   ? result.Buffer
                                   : OnDatagram(result.Buffer, result.RemoteEndPoint);

                if (!response.IsEmpty)
                {
                    await listener.SendAsync(response, result.RemoteEndPoint, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (SocketException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "UDP receive loop failed");
                OnException?.Invoke(this, new(ex));
            }
        }
    }

    private IEnumerable<IPEndPoint> ResolveBindEndPoints()
    {
        if (!_bindAllInterfaces)
        {
            return [_endPoint];
        }

        return NetworkUtils.GetListeningAddresses(_endPoint)
                           .Select(address => new IPEndPoint(address.Address, _endPoint.Port))
                           .ToArray();
    }
}
