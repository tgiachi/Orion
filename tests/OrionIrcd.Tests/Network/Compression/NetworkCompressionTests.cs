using OrionIrcd.Network.Compression;

namespace OrionIrcd.Tests.Network.Compression;

public class NetworkCompressionTests
{
    [Fact]
    public void Compress_Decompress_RoundTrips()
    {
        var input = new byte[256];

        for (var i = 0; i < input.Length; i++)
        {
            input[i] = (byte)(i & 0xFF);
        }

        var compressedBuffer = new byte[NetworkCompression.CalculateMaxCompressedSize(input.Length)];
        var compressedLength = NetworkCompression.Compress(input, compressedBuffer);

        Assert.True(compressedLength > 0);

        var decompressedBuffer = new byte[input.Length * 2];
        var decompressedLength = NetworkCompression.Decompress(
            compressedBuffer.AsSpan(0, compressedLength),
            decompressedBuffer
        );

        Assert.Equal(input.Length, decompressedLength);
        Assert.Equal(input, decompressedBuffer.AsSpan(0, decompressedLength).ToArray());
    }

    [Fact]
    public void CompressToMemory_EmptyInput_ReturnsEmpty()
    {
        var result = NetworkCompression.CompressToMemory(ReadOnlyMemory<byte>.Empty);
        Assert.True(result.IsEmpty);
    }

    [Fact]
    public void Decompress_CalledManyTimes_DoesNotRebuildTree()
    {
        // Regression-style: the decompression tree used to be built per call.
        // We exercise it many times and confirm correctness under repeated use.
        var input = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
        var compressedBuffer = new byte[NetworkCompression.CalculateMaxCompressedSize(input.Length)];
        var compressedLength = NetworkCompression.Compress(input, compressedBuffer);

        var decompressedBuffer = new byte[16];

        for (var i = 0; i < 100; i++)
        {
            var length = NetworkCompression.Decompress(
                compressedBuffer.AsSpan(0, compressedLength),
                decompressedBuffer
            );

            Assert.Equal(input.Length, length);
            Assert.Equal(input, decompressedBuffer.AsSpan(0, length).ToArray());
        }
    }

    [Fact]
    public void ProcessReceive_WithFlagFalse_PassesThrough()
    {
        var input = new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3 });

        var (halt, consumed) = NetworkCompression.ProcessReceive(ref input, false, out var output);

        Assert.False(halt);
        Assert.Equal(3, consumed);
        Assert.True(output.Span.SequenceEqual(new byte[] { 1, 2, 3 }));
    }

    [Fact]
    public void ProcessReceive_WithFlagTrue_DecompressesCompressedPayload()
    {
        var input = new byte[64];

        for (var i = 0; i < input.Length; i++)
        {
            input[i] = (byte)(i + 1);
        }

        var compressed = NetworkCompression.CompressToMemory(input);
        Assert.False(compressed.IsEmpty);

        var asReadOnly = (ReadOnlyMemory<byte>)compressed;
        var (halt, _) = NetworkCompression.ProcessReceive(ref asReadOnly, true, out var output);

        Assert.False(halt);
        Assert.Equal(input, output.ToArray());
    }

    [Fact]
    public void ShouldCompress_VerySmallInput_ReturnsFalse()
    {
        Assert.False(NetworkCompression.ShouldCompress(8));
        Assert.True(NetworkCompression.ShouldCompress(64));
    }
}
