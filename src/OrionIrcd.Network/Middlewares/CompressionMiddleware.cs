using OrionIrcd.Network.Client;
using OrionIrcd.Network.Compression;
using OrionIrcd.Network.Interfaces.Middleware;

namespace OrionIrcd.Network.Middlewares;

/// <summary>
///     Compresses every outgoing payload with the UO Huffman scheme. Inbound payloads pass through
///     untouched (UO transport compression is server-to-client only). Once enabled the client
///     decompresses everything, so even tiny payloads must be compressed.
/// </summary>
public sealed class CompressionMiddleware : INetMiddleware
{
    /// <inheritdoc />
    public ValueTask<ReadOnlyMemory<byte>> ProcessAsync(
        OrionTcpClient? client,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default
    )
    {
        _ = client;
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromResult(data);
    }

    /// <inheritdoc />
    public ValueTask<ReadOnlyMemory<byte>> ProcessSendAsync(
        OrionTcpClient? client,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default
    )
    {
        _ = client;
        cancellationToken.ThrowIfCancellationRequested();

        if (data.IsEmpty)
        {
            return ValueTask.FromResult(ReadOnlyMemory<byte>.Empty);
        }

        var maxSize = NetworkCompression.CalculateMaxCompressedSize(data.Length);

        if (maxSize <= 0)
        {
            return ValueTask.FromResult(ReadOnlyMemory<byte>.Empty);
        }

        var buffer = new byte[maxSize];
        var compressedLength = NetworkCompression.Compress(data.Span, buffer.AsSpan());

        if (compressedLength <= 0)
        {
            return ValueTask.FromResult(ReadOnlyMemory<byte>.Empty);
        }

        return ValueTask.FromResult<ReadOnlyMemory<byte>>(buffer.AsMemory(0, compressedLength));
    }
}
