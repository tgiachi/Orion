using System.Buffers;
using System.Net;
using System.Net.WebSockets;
using OrionIrcd.Network.Data.Events;
using OrionIrcd.Network.Interfaces.Client;
using Serilog;

namespace OrionIrcd.Network.Client;

/// <summary>
///     Represents a connected WebSocket client with async send/receive lifecycle events.
/// </summary>
public sealed class OrionWebSocketClient : INetworkConnection, IAsyncDisposable, IDisposable
{
    private const int DefaultReceiveBufferSize = 8192;

    private static readonly TimeSpan _closeTimeout = TimeSpan.FromSeconds(2);

    private readonly ILogger _logger = Log.ForContext<OrionWebSocketClient>();
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly WebSocket _webSocket;
    private static long _sessionIdSequence;
    private int _closed;
    private int _disposed;
    private int _started;

    /// <summary>
    ///     Unique session identifier for this client connection.
    /// </summary>
    public long SessionId { get; }

    /// <summary>
    ///     Client remote endpoint, when available.
    /// </summary>
    public EndPoint? RemoteEndPoint { get; }

    /// <summary>
    ///     True when the underlying WebSocket is open and the client is not closed.
    /// </summary>
    public bool IsConnected => CanSend();

    /// <summary>
    ///     Raised when the client receive loop starts.
    /// </summary>
    public event EventHandler<OrionWebSocketClientEventArgs>? OnConnected;

    /// <summary>
    ///     Raised when the client is disconnected.
    /// </summary>
    public event EventHandler<OrionWebSocketClientEventArgs>? OnDisconnected;

    /// <summary>
    ///     Raised when a complete WebSocket message is received.
    /// </summary>
    public event EventHandler<OrionWebSocketDataReceivedEventArgs>? OnDataReceived;

    /// <summary>
    ///     Raised when receive/send loops throw an exception.
    /// </summary>
    public event EventHandler<OrionWebSocketExceptionEventArgs>? OnException;

    public OrionWebSocketClient(WebSocket webSocket, EndPoint? remoteEndPoint = null)
    {
        ArgumentNullException.ThrowIfNull(webSocket);

        _webSocket = webSocket;
        RemoteEndPoint = remoteEndPoint;
        SessionId = Interlocked.Increment(ref _sessionIdSequence);
    }

    /// <summary>
    ///     Runs the WebSocket receive loop and raises lifecycle events.
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _started, 1) != 0)
        {
            return;
        }

        var buffer = ArrayPool<byte>.Shared.Rent(DefaultReceiveBufferSize);

        try
        {
            RaiseConnected();

            while (!cancellationToken.IsCancellationRequested && IsConnected)
            {
                var message = await ReceiveMessageAsync(buffer, cancellationToken);

                if (message is null)
                {
                    break;
                }

                OnDataReceived?.Invoke(this, new OrionWebSocketDataReceivedEventArgs(this, message));
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during controlled shutdown.
        }
        catch (ObjectDisposedException)
        {
            // Expected when the socket is disposed during shutdown.
        }
        catch (WebSocketException ex)
        {
            RaiseException(ex);
        }
        catch (Exception ex)
        {
            RaiseException(ex);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
            await CloseAsync(CancellationToken.None);
        }
    }

    /// <summary>
    ///     Sends a non-empty binary WebSocket message.
    /// </summary>
    public async Task SendAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
    {
        if (payload.IsEmpty || !CanSend())
        {
            return;
        }

        await _sendLock.WaitAsync(cancellationToken);

        try
        {
            if (!CanSend())
            {
                return;
            }

            await _webSocket.SendAsync(
                payload,
                WebSocketMessageType.Binary,
                true,
                cancellationToken
            );
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            RaiseException(ex);
            await CloseAsyncCore(false, CancellationToken.None);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    /// <summary>
    ///     Closes the WebSocket connection and raises disconnect event once.
    /// </summary>
    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        await CloseAsyncCore(true, cancellationToken);
    }

    private bool CanSend()
    {
        return Volatile.Read(ref _disposed) == 0
            && Volatile.Read(ref _closed) == 0
            && TryGetState() == WebSocketState.Open;
    }

    private async Task CloseAsyncCore(bool acquireSendLock, CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _closed, 1) != 0)
        {
            return;
        }

        if (acquireSendLock)
        {
            await _sendLock.WaitAsync(CancellationToken.None);
        }

        try
        {
            if (TryGetState() is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                using var timeoutCancellationTokenSource = new CancellationTokenSource(_closeTimeout);
                using var closeCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                    timeoutCancellationTokenSource.Token,
                    cancellationToken
                );
                await _webSocket.CloseOutputAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Normal closure",
                    closeCancellationTokenSource.Token
                );
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _webSocket.Abort();
        }
        catch (OperationCanceledException)
        {
            // Close timed out; disposal will release the socket.
        }
        catch (WebSocketException)
        {
            // Peer may have already closed the socket.
        }
        catch (ObjectDisposedException)
        {
            // Socket is already disposed.
        }
        finally
        {
            if (acquireSendLock)
            {
                _sendLock.Release();
            }

            RaiseDisconnected();
        }
    }

    private async Task<byte[]?> ReceiveMessageAsync(byte[] buffer, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream();
        WebSocketMessageType? messageType = null;

        while (true)
        {
            var result = await _webSocket.ReceiveAsync(
                buffer.AsMemory(0, DefaultReceiveBufferSize),
                cancellationToken
            );

            if (result.MessageType == WebSocketMessageType.Close)
            {
                return null;
            }

            messageType ??= result.MessageType;

            if (result.Count > 0)
            {
                stream.Write(buffer.AsSpan(0, result.Count));
            }

            if (result.EndOfMessage)
            {
                break;
            }
        }

        if (messageType is not WebSocketMessageType.Text and not WebSocketMessageType.Binary)
        {
            return [];
        }

        return stream.ToArray();
    }

    private void RaiseConnected()
    {
        _logger.Information(
            "WebSocket client connected. SessionId={SessionId}, RemoteEndPoint={RemoteEndPoint}",
            SessionId,
            RemoteEndPoint
        );
        OnConnected?.Invoke(this, new OrionWebSocketClientEventArgs(this));
    }

    private void RaiseDisconnected()
    {
        _logger.Information(
            "WebSocket client disconnected. SessionId={SessionId}, RemoteEndPoint={RemoteEndPoint}",
            SessionId,
            RemoteEndPoint
        );
        OnDisconnected?.Invoke(this, new OrionWebSocketClientEventArgs(this));
    }

    private void RaiseException(Exception exception)
    {
        _logger.Error(
            exception,
            "WebSocket client exception. SessionId={SessionId}, RemoteEndPoint={RemoteEndPoint}",
            SessionId,
            RemoteEndPoint
        );
        OnException?.Invoke(this, new OrionWebSocketExceptionEventArgs(exception, this));
    }

    private WebSocketState? TryGetState()
    {
        try
        {
            return _webSocket.State;
        }
        catch (ObjectDisposedException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        await CloseAsync(CancellationToken.None);

        await _sendLock.WaitAsync(CancellationToken.None);

        try
        {
            _webSocket.Abort();
            _webSocket.Dispose();
        }
        finally
        {
            _sendLock.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose() // Sync-over-async: best effort. Prefer DisposeAsync.
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
