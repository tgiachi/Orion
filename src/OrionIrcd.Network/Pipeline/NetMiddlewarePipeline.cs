using OrionIrcd.Network.Client;
using OrionIrcd.Network.Interfaces.Middleware;

namespace OrionIrcd.Network.Pipeline;

/// <summary>
///     Executes <see cref="INetMiddleware" /> components in registration order over a byte payload.
/// </summary>
/// <remarks>
///     The pipeline is a byte transformer: each middleware sees a <see cref="ReadOnlyMemory{T}" />
///     of bytes and produces a <see cref="ReadOnlyMemory{T}" /> of bytes. There is no concept of
///     message, packet, or frame at this layer — protocol-specific framing must happen on top of
///     the client's <c>OnDataReceived</c> output. Returning <see cref="ReadOnlyMemory{T}.Empty" />
///     from a middleware drops the payload and stops the chain.
/// </remarks>
public sealed class NetMiddlewarePipeline
{
    private readonly Lock _middlewareSync = new();
    private INetMiddleware[] _middlewares;

    /// <summary>
    ///     Initializes the middleware pipeline.
    /// </summary>
    /// <param name="middlewares">Optional initial middleware sequence.</param>
    public NetMiddlewarePipeline(IEnumerable<INetMiddleware>? middlewares = null)
    {
        _middlewares = [.. middlewares ?? []];
    }

    /// <summary>
    ///     Adds a middleware component at the end of the execution chain.
    /// </summary>
    /// <param name="middleware">Middleware to register.</param>
    public void AddMiddleware(INetMiddleware middleware)
    {
        lock (_middlewareSync)
        {
            _middlewares = [.. _middlewares, middleware];
        }
    }

    /// <summary>
    ///     Checks whether at least one middleware component of the specified type is registered.
    /// </summary>
    /// <typeparam name="TMiddleware">Middleware type to check.</typeparam>
    /// <returns><c>true</c> when a matching middleware is registered; otherwise <c>false</c>.</returns>
    public bool ContainsMiddleware<TMiddleware>()
        where TMiddleware : INetMiddleware
    {
        lock (_middlewareSync)
        {
            return _middlewares.Any(static middleware => middleware is TMiddleware);
        }
    }

    /// <summary>
    ///     Processes the payload through all registered middleware components.
    /// </summary>
    /// <param name="client">Client associated with the payload, if available.</param>
    /// <param name="data">Incoming payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The processed payload, or empty when dropped by middleware.</returns>
    public async ValueTask<ReadOnlyMemory<byte>> ExecuteAsync(
        OrionTcpClient? client,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken
    )
    {
        INetMiddleware[] middlewares;

        lock (_middlewareSync)
        {
            middlewares = _middlewares;
        }

        var current = data;

        for (var i = 0; i < middlewares.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            current = await middlewares[i].ProcessAsync(client, current, cancellationToken);

            if (current.IsEmpty)
            {
                return ReadOnlyMemory<byte>.Empty;
            }
        }

        return current;
    }

    /// <summary>
    ///     Processes the outgoing payload through all registered middleware components.
    /// </summary>
    /// <param name="client">Client associated with the payload, if available.</param>
    /// <param name="data">Outgoing payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The processed payload, or empty when dropped by middleware.</returns>
    public async ValueTask<ReadOnlyMemory<byte>> ExecuteSendAsync(
        OrionTcpClient? client,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken
    )
    {
        INetMiddleware[] middlewares;

        lock (_middlewareSync)
        {
            middlewares = _middlewares;
        }

        var current = data;

        for (var i = 0; i < middlewares.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            current = await middlewares[i].ProcessSendAsync(client, current, cancellationToken);

            if (current.IsEmpty)
            {
                return ReadOnlyMemory<byte>.Empty;
            }
        }

        return current;
    }

    /// <summary>
    ///     Removes all middleware components of the specified type.
    /// </summary>
    /// <typeparam name="TMiddleware">Middleware type to remove.</typeparam>
    /// <returns><c>true</c> when at least one middleware was removed; otherwise <c>false</c>.</returns>
    public bool RemoveMiddleware<TMiddleware>()
        where TMiddleware : INetMiddleware
    {
        lock (_middlewareSync)
        {
            var originalLength = _middlewares.Length;

            if (originalLength == 0)
            {
                return false;
            }

            _middlewares = _middlewares.Where(static middleware => middleware is not TMiddleware).ToArray();

            return _middlewares.Length != originalLength;
        }
    }
}
