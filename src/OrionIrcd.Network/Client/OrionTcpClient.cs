using System.Buffers;
using System.Net;
using System.Net.Sockets;
using OrionIrcd.Network.Buffers;
using OrionIrcd.Network.Data.Events;
using OrionIrcd.Network.Interfaces.Client;
using OrionIrcd.Network.Interfaces.Framing;
using OrionIrcd.Network.Interfaces.Middleware;
using OrionIrcd.Network.Pipeline;
using Serilog;

namespace OrionIrcd.Network.Client;

/// <summary>
///     Represents a connected TCP client with async send/receive loops,
///     middleware processing, lifecycle events, and recent byte history.
/// </summary>
public sealed class OrionTcpClient : INetworkConnection, IAsyncDisposable, IDisposable
{
    private const int DefaultReceiveBufferSize = 8192;
    private const int DefaultHistoryBufferCapacity = 65536;

    private readonly INetFramer? _framer;
    private readonly CancellationTokenSource _internalCancellationTokenSource = new();

    private readonly ILogger _logger = Log.ForContext<OrionTcpClient>();
    private readonly NetMiddlewarePipeline _middlewarePipeline;
    private readonly CircularBuffer<byte> _receiveBuffer;
    private readonly Lock _receiveBufferSync = new();
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly Socket _socket;
    private readonly Stream _stream;
    private static long _sessionIdSequence;
    private int _closed;

    private CancellationTokenRegistration _externalCancellationTokenRegistration;
    private byte[]? _pendingBuffer;
    private int _pendingLength;
    private Task? _receiveLoopTask;
    private int _started;

    /// <summary>
    ///     Unique session identifier for this client connection.
    /// </summary>
    public long SessionId { get; }

    /// <summary>
    ///     Receives payload chunk size in bytes.
    /// </summary>
    public int ReceiveBufferSize { get; }

    /// <summary>
    ///     Client remote endpoint, when connected.
    /// </summary>
    public EndPoint? RemoteEndPoint
    {
        get
        {
            try
            {
                return _socket.RemoteEndPoint;
            }
            catch (ObjectDisposedException)
            {
                return null;
            }
        }
    }

    /// <summary>
    ///     Local endpoint used for this connection, when available.
    /// </summary>
    public EndPoint? LocalEndPoint
    {
        get
        {
            try
            {
                return _socket.LocalEndPoint;
            }
            catch (ObjectDisposedException)
            {
                return null;
            }
        }
    }

    /// <summary>
    ///     Gets the number of bytes currently available in the receive circular buffer.
    /// </summary>
    public int AvailableBytes
    {
        get
        {
            lock (_receiveBufferSync)
            {
                return _receiveBuffer.Size;
            }
        }
    }

    /// <summary>
    ///     Gets whether the receive circular buffer is full.
    /// </summary>
    public bool IsReceiveBufferFull
    {
        get
        {
            lock (_receiveBufferSync)
            {
                return _receiveBuffer.IsFull;
            }
        }
    }

    /// <summary>
    ///     True when the underlying socket is connected and client not closed.
    /// </summary>
    public bool IsConnected => _socket.Connected && Volatile.Read(ref _closed) == 0;

    /// <summary>
    ///     Raised when the client is fully connected and receive loop starts.
    /// </summary>
    public event EventHandler<OrionTcpClientEventArgs>? OnConnected;

    /// <summary>
    ///     Raised when the client is disconnected.
    /// </summary>
    public event EventHandler<OrionTcpClientEventArgs>? OnDisconnected;

    /// <summary>
    ///     Raised when data is received (after middleware pipeline).
    /// </summary>
    public event EventHandler<OrionTcpDataReceivedEventArgs>? OnDataReceived;

    /// <summary>
    ///     Raised when receive/send loops throw an exception.
    /// </summary>
    public event EventHandler<OrionTcpExceptionEventArgs>? OnException;

    /// <summary>
    ///     Creates a client wrapper for an accepted socket.
    /// </summary>
    /// <param name="socket">Connected socket.</param>
    /// <param name="middlewares">Optional middleware list.</param>
    /// <param name="framer">
    ///     Optional framer. When supplied, the receive loop accumulates middleware output and
    ///     emits <see cref="OnDataReceived" /> once per complete frame instead of once per socket read.
    /// </param>
    /// <param name="receiveBufferSize">Receive chunk size in bytes.</param>
    /// <param name="historyBufferCapacity">Max number of received bytes to keep in history.</param>
    public OrionTcpClient(
        Socket socket,
        IEnumerable<INetMiddleware>? middlewares = null,
        INetFramer? framer = null,
        int receiveBufferSize = DefaultReceiveBufferSize,
        int historyBufferCapacity = DefaultHistoryBufferCapacity
    ) : this(
        socket,
        new NetworkStream(socket, false),
        middlewares,
        framer,
        receiveBufferSize,
        historyBufferCapacity
    )
    {
    }

    /// <summary>
    ///     Creates a client wrapper for an accepted socket using the supplied transport stream.
    /// </summary>
    public OrionTcpClient(
        Socket socket,
        Stream stream,
        IEnumerable<INetMiddleware>? middlewares = null,
        INetFramer? framer = null,
        int receiveBufferSize = DefaultReceiveBufferSize,
        int historyBufferCapacity = DefaultHistoryBufferCapacity
    )
    {
        ArgumentNullException.ThrowIfNull(socket);
        ArgumentNullException.ThrowIfNull(stream);

        _socket = socket;
        _stream = stream;
        _middlewarePipeline = new NetMiddlewarePipeline(middlewares);
        _framer = framer;
        _receiveBuffer = new CircularBuffer<byte>(historyBufferCapacity);
        ReceiveBufferSize = receiveBufferSize;
        SessionId = Interlocked.Increment(ref _sessionIdSequence);
    }

    /// <summary>
    ///     Adds a middleware component to this client pipeline.
    /// </summary>
    public OrionTcpClient AddMiddleware(INetMiddleware middleware)
    {
        _middlewarePipeline.AddMiddleware(middleware);

        return this;
    }

    /// <summary>
    ///     Closes the client connection and raises disconnect event once.
    /// </summary>
    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.Exchange(ref _closed, 1) != 0)
        {
            return;
        }

        try
        {
            await _internalCancellationTokenSource.CancelAsync().WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Once close has started, still tear down the socket below.
        }

        try
        {
            if (_socket.Connected)
            {
                try
                {
                    _socket.Shutdown(SocketShutdown.Both);
                }
                catch (SocketException)
                {
                    // Socket might already be closed by peer.
                }
            }
        }
        finally
        {
            _socket.Close();
            _externalCancellationTokenRegistration.Dispose();
            RaiseDisconnected();
        }
    }

    /// <summary>
    ///     Creates an outbound client and connects to the specified endpoint.
    /// </summary>
    public static async Task<OrionTcpClient> ConnectAsync(
        IPEndPoint endPoint,
        IEnumerable<INetMiddleware>? middlewares = null,
        INetFramer? framer = null,
        CancellationToken cancellationToken = default
    )
    {
        var socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        await socket.ConnectAsync(endPoint, cancellationToken);

        var client = new OrionTcpClient(socket, middlewares, framer);
        await client.StartAsync(cancellationToken);

        return client;
    }

    /// <summary>
    ///     Consumes bytes from the front of the receive circular buffer.
    /// </summary>
    public int ConsumeBytes(int count)
    {
        if (count <= 0)
        {
            return 0;
        }

        lock (_receiveBufferSync)
        {
            var bytesToConsume = Math.Min(count, _receiveBuffer.Size);

            for (var i = 0; i < bytesToConsume; i++)
            {
                _receiveBuffer.PopFront();
            }

            return bytesToConsume;
        }
    }

    /// <summary>
    ///     Checks whether this client pipeline contains at least one middleware instance of the specified type.
    /// </summary>
    public bool ContainsMiddleware<TMiddleware>()
        where TMiddleware : INetMiddleware
    {
        return _middlewarePipeline.ContainsMiddleware<TMiddleware>();
    }

    /// <summary>
    ///     Returns a snapshot of recent received bytes from the circular history buffer.
    /// </summary>
    public byte[] GetRecentReceivedBytes()
    {
        return PeekData();
    }

    /// <summary>
    ///     Peeks at data in the receive circular buffer without consuming it.
    /// </summary>
    public byte[] PeekData(int count = 0)
    {
        lock (_receiveBufferSync)
        {
            if (_receiveBuffer.IsEmpty)
            {
                return [];
            }

            var bytesToPeek = count <= 0 ? _receiveBuffer.Size : Math.Min(count, _receiveBuffer.Size);
            var result = new byte[bytesToPeek];

            for (var i = 0; i < bytesToPeek; i++)
            {
                result[i] = _receiveBuffer[i];
            }

            return result;
        }
    }

    /// <summary>
    ///     Removes all middleware components of the specified type from this client pipeline.
    /// </summary>
    public bool RemoveMiddleware<TMiddleware>()
        where TMiddleware : INetMiddleware
    {
        return _middlewarePipeline.RemoveMiddleware<TMiddleware>();
    }

    /// <summary>
    ///     Sends a payload to the connected socket.
    /// </summary>
    public async Task SendAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
    {
        if (payload.IsEmpty || !IsConnected)
        {
            return;
        }

        var processedPayload = await _middlewarePipeline.ExecuteSendAsync(this, payload, cancellationToken);

        if (processedPayload.IsEmpty)
        {
            return;
        }

        await _sendLock.WaitAsync(cancellationToken);

        try
        {
            await _stream.WriteAsync(processedPayload, cancellationToken);
            await _stream.FlushAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            RaiseException(ex);
            await CloseAsync(CancellationToken.None);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    /// <summary>
    ///     Starts the receive loop and raises connect event.
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _started, 1) != 0)
        {
            return Task.CompletedTask;
        }

        if (cancellationToken.CanBeCanceled)
        {
            _externalCancellationTokenRegistration = cancellationToken.Register(() => _ = CloseAsync(CancellationToken.None));
        }

        RaiseConnected();
        _receiveLoopTask = Task.Run(ReceiveLoopAsync, CancellationToken.None);

        return Task.CompletedTask;
    }

    private void AppendPending(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
        {
            return;
        }

        if (_pendingBuffer is null)
        {
            _pendingBuffer = ArrayPool<byte>.Shared.Rent(Math.Max(ReceiveBufferSize, data.Length));
        }

        var required = _pendingLength + data.Length;

        if (required > _pendingBuffer.Length)
        {
            var newCapacity = Math.Max(required, _pendingBuffer.Length * 2);
            var newBuffer = ArrayPool<byte>.Shared.Rent(newCapacity);
            _pendingBuffer.AsSpan(0, _pendingLength).CopyTo(newBuffer);
            ArrayPool<byte>.Shared.Return(_pendingBuffer);
            _pendingBuffer = newBuffer;
        }

        data.CopyTo(_pendingBuffer.AsSpan(_pendingLength));
        _pendingLength += data.Length;
    }

    private void ConsumePending(int count)
    {
        var remaining = _pendingLength - count;

        if (remaining > 0 && _pendingBuffer is not null)
        {
            _pendingBuffer.AsSpan(count, remaining).CopyTo(_pendingBuffer);
        }

        _pendingLength = remaining;
    }

    private void EmitFrames()
    {
        if (_framer is null || _pendingBuffer is null)
        {
            return;
        }

        while (_pendingLength > 0)
        {
            var view = _pendingBuffer.AsSpan(0, _pendingLength);

            if (!_framer.TryReadFrame(view, out var frameLength))
            {
                break;
            }

            if (frameLength <= 0 || frameLength > _pendingLength)
            {
                // Malformed framer report; abandon the remaining buffer to avoid an infinite loop.
                _pendingLength = 0;

                break;
            }

            // Fresh copy so handlers can safely retain the payload.
            var frame = new byte[frameLength];
            view[..frameLength].CopyTo(frame);

            ConsumePending(frameLength);

            OnDataReceived?.Invoke(this, new OrionTcpDataReceivedEventArgs(this, frame));
        }
    }

    private void RaiseConnected()
    {
        _logger.Information(
            "Client connected. SessionId={SessionId}, RemoteEndPoint={RemoteEndPoint}",
            SessionId,
            RemoteEndPoint
        );
        OnConnected?.Invoke(this, new OrionTcpClientEventArgs(this));
    }

    private void RaiseDisconnected()
    {
        _logger.Information(
            "Client disconnected. SessionId={SessionId}, RemoteEndPoint={RemoteEndPoint}",
            SessionId,
            RemoteEndPoint
        );
        OnDisconnected?.Invoke(this, new OrionTcpClientEventArgs(this));
    }

    private void RaiseException(Exception exception)
    {
        _logger.Error(
            exception,
            "Client exception. SessionId={SessionId}, RemoteEndPoint={RemoteEndPoint}",
            SessionId,
            RemoteEndPoint
        );
        OnException?.Invoke(this, new OrionTcpExceptionEventArgs(exception, this));
    }

    private async Task ReceiveLoopAsync()
    {
        var buffer = ArrayPool<byte>.Shared.Rent(ReceiveBufferSize);

        try
        {
            while (!_internalCancellationTokenSource.IsCancellationRequested && IsConnected)
            {
                var received = await _stream.ReadAsync(
                    buffer.AsMemory(0, ReceiveBufferSize),
                    _internalCancellationTokenSource.Token
                );

                if (received <= 0)
                {
                    break;
                }

                lock (_receiveBufferSync)
                {
                    _receiveBuffer.PushBackRange(buffer.AsSpan(0, received));
                }

                var chunk = ArrayPool<byte>.Shared.Rent(received);

                try
                {
                    buffer.AsSpan(0, received).CopyTo(chunk);

                    var chunkMemory = new ReadOnlyMemory<byte>(chunk, 0, received);
                    var processed = await _middlewarePipeline.ExecuteAsync(
                        this,
                        chunkMemory,
                        _internalCancellationTokenSource.Token
                    );

                    if (processed.IsEmpty)
                    {
                        continue;
                    }

                    if (_framer is null)
                    {
                        // Fresh copy so the event handler can outlive the pooled chunk.
                        var payload = new byte[processed.Length];
                        processed.CopyTo(payload);
                        OnDataReceived?.Invoke(this, new OrionTcpDataReceivedEventArgs(this, payload));
                    }
                    else
                    {
                        AppendPending(processed.Span);
                        EmitFrames();
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(chunk);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during controlled shutdown.
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Receive loop failed for session {SessionId}", SessionId);
            RaiseException(ex);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
            ReleasePendingBuffer();
            await CloseAsync(CancellationToken.None);
        }
    }

    private void ReleasePendingBuffer()
    {
        if (_pendingBuffer is null)
        {
            return;
        }

        ArrayPool<byte>.Shared.Return(_pendingBuffer);
        _pendingBuffer = null;
        _pendingLength = 0;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await CloseAsync(CancellationToken.None);

        // Drain the receive loop before disposing the resources it relies on.
        if (_receiveLoopTask is not null)
        {
            try
            {
                await _receiveLoopTask;
            }
            catch
            {
                // Loop failures are already surfaced via OnException.
            }
        }

        await _stream.DisposeAsync();
        _sendLock.Dispose();
        _internalCancellationTokenSource.Dispose();
        _socket.Dispose();
    }

    /// <inheritdoc />
    public void Dispose() // Sync-over-async: best effort. Prefer DisposeAsync.
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
