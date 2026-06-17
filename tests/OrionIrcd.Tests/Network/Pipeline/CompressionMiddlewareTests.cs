using OrionIrcd.Network.Compression;
using OrionIrcd.Network.Middlewares;

namespace OrionIrcd.Tests.Network.Pipeline;

public class CompressionMiddlewareTests
{
    [Fact]
    public async Task ProcessAsync_Inbound_IsPassthrough()
    {
        var middleware = new CompressionMiddleware();
        var data = new byte[] { 0x73, 0x00, 0x01, 0x02 };

        var result = await middleware.ProcessAsync(null, data);

        Assert.Equal(data, result.ToArray());
    }

    [Fact]
    public async Task ProcessSendAsync_EmptyPayload_ReturnsEmpty()
    {
        var middleware = new CompressionMiddleware();

        var compressed = await middleware.ProcessSendAsync(null, ReadOnlyMemory<byte>.Empty);

        Assert.True(compressed.IsEmpty);
    }

    [Fact]
    public async Task ProcessSendAsync_Outbound_RoundTripsThroughDecompression()
    {
        var middleware = new CompressionMiddleware();
        var data = new byte[] { 0xB9, 0x00, 0x03, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0x10, 0x20, 0x30 };

        var compressed = await middleware.ProcessSendAsync(null, data);

        Assert.False(compressed.IsEmpty);
        Assert.Equal(data, Decompress(compressed));
    }

    [Fact]
    public async Task ProcessSendAsync_SmallPayload_IsStillCompressed()
    {
        // Once transport compression is on, the client decompresses everything: even tiny packets
        // must go through the compressor (no "too small to be worth it" bypass).
        var middleware = new CompressionMiddleware();
        var data = new byte[] { 0x73, 0x00 };

        var compressed = await middleware.ProcessSendAsync(null, data);

        Assert.False(compressed.IsEmpty);
        Assert.NotEqual(data, compressed.ToArray());
        Assert.Equal(data, Decompress(compressed));
    }

    private static byte[] Decompress(ReadOnlyMemory<byte> compressed)
    {
        var output = new byte[NetworkCompression.BufferSize];
        var length = NetworkCompression.Decompress(compressed.Span, output);

        return output[..length];
    }
}
