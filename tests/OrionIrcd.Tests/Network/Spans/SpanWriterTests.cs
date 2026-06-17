using System.Text;
using OrionIrcd.Network.Spans;

namespace OrionIrcd.Tests.Network.Spans;

public class SpanWriterTests
{
    [Fact]
    public void Clear_AdvancesPositionWithZeroes()
    {
        Span<byte> backing = stackalloc byte[8];
        backing.Fill(0xFF);
        var writer = new SpanWriter(backing);

        writer.Clear(3);

        Assert.Equal(3, writer.Position);
        Assert.Equal(0, backing[0]);
        Assert.Equal(0, backing[1]);
        Assert.Equal(0, backing[2]);
        Assert.Equal(0xFF, backing[3]);
    }

    [Fact]
    public void EnsureCapacity_ResizeDisabled_TooSmall_Throws()
    {
        Span<byte> backing = stackalloc byte[4];
        var writer = new SpanWriter(backing);

        var threw = false;

        try
        {
            writer.EnsureCapacity(64);
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }

        Assert.True(threw);
    }

    [Fact]
    public void Grow_WhenResizeDisabled_Throws()
    {
        Span<byte> backing = stackalloc byte[2];
        var writer = new SpanWriter(backing);

        writer.Write((byte)0xAA);
        writer.Write((byte)0xBB);

        var threw = false;

        try
        {
            writer.Write((byte)0xCC);
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }

        Assert.True(threw);
    }

    [Fact]
    public void Grow_WhenResizeEnabled_ExpandsBuffer()
    {
        var writer = new SpanWriter(4, true);

        for (var i = 0; i < 20; i++)
        {
            writer.Write((byte)i);
        }

        Assert.Equal(20, writer.BytesWritten);

        for (var i = 0; i < 20; i++)
        {
            Assert.Equal((byte)i, writer.Span[i]);
        }

        writer.Dispose();
    }

    [Fact]
    public void Position_DoesNotShrinkBytesWritten_AfterRewind()
    {
        Span<byte> backing = stackalloc byte[16];
        var writer = new SpanWriter(backing);
        writer.Write(0x12345678);
        Assert.Equal(4, writer.BytesWritten);

        writer.Seek(2, SeekOrigin.Begin);
        Assert.Equal(4, writer.BytesWritten);

        writer.Write((byte)0xFF);

        // BytesWritten still 4 because we wrote within the already-written region.
        Assert.Equal(4, writer.BytesWritten);
    }

    [Fact]
    public void RoundTrip_AllPrimitives_ThroughReader()
    {
        var writer = new SpanWriter(64, true);

        try
        {
            writer.Write(true);
            writer.Write((byte)0xAB);
            writer.Write((sbyte)-7);
            writer.Write((short)-1234);
            writer.WriteLE((short)-1234);
            writer.Write((ushort)0xBEEF);
            writer.Write(0x11223344);
            writer.WriteLE(0x11223344);
            writer.Write(0xCAFEBABE);
            writer.Write(0x1122334455667788L);
            writer.Write(0xDEADBEEFCAFEBABEUL);

            var written = writer.Span.ToArray();
            var reader = new SpanReader(written);

            Assert.True(reader.ReadBoolean());
            Assert.Equal(0xAB, reader.ReadByte());
            Assert.Equal((sbyte)-7, reader.ReadSByte());
            Assert.Equal((short)-1234, reader.ReadInt16());
            Assert.Equal((short)-1234, reader.ReadInt16LE());
            Assert.Equal((ushort)0xBEEF, reader.ReadUInt16());
            Assert.Equal(0x11223344, reader.ReadInt32());
            Assert.Equal(0x11223344, reader.ReadInt32LE());
            Assert.Equal(0xCAFEBABE, reader.ReadUInt32());
            Assert.Equal(0x1122334455667788L, reader.ReadInt64());
            Assert.Equal(0xDEADBEEFCAFEBABEUL, reader.ReadUInt64());
        }
        finally
        {
            writer.Dispose();
        }
    }

    [Fact]
    public void Seek_BeyondCapacity_WithoutResize_Throws()
    {
        Span<byte> backing = stackalloc byte[4];
        var writer = new SpanWriter(backing);

        var threw = false;

        try
        {
            writer.Seek(99, SeekOrigin.Begin);
        }
        catch (IOException)
        {
            threw = true;
        }

        Assert.True(threw);
    }

    [Fact]
    public void Seek_End_ResolvesRelativeToBytesWritten()
    {
        Span<byte> backing = stackalloc byte[16];
        var writer = new SpanWriter(backing);
        writer.Write((byte)0xAA);
        writer.Write((byte)0xBB);
        writer.Write((byte)0xCC);

        writer.Seek(0, SeekOrigin.Begin);
        Assert.Equal(0, writer.Position);

        writer.Seek(0, SeekOrigin.End);
        Assert.Equal(3, writer.Position);
    }

    [Fact]
    public void ToArray_EmptyWriter_ReturnsEmpty()
    {
        Span<byte> backing = stackalloc byte[16];
        var writer = new SpanWriter(backing);

        Assert.Empty(writer.ToArray());
    }

    [Fact]
    public void ToArray_ReturnsExactlyWrittenBytes()
    {
        Span<byte> backing = stackalloc byte[16];
        var writer = new SpanWriter(backing);
        writer.Write((byte)1);
        writer.Write((byte)2);
        writer.Write((byte)3);

        var arr = writer.ToArray();

        Assert.Equal(new byte[] { 1, 2, 3 }, arr);
    }

    [Fact]
    public void ToSpan_Empty_ReturnsEmptyOwner()
    {
        var writer = new SpanWriter(8, true);

        var owner = writer.ToSpan();

        try
        {
            Assert.Equal(0, owner.Span.Length);
        }
        finally
        {
            owner.Dispose();
        }
    }

    [Fact]
    public void ToSpan_PooledBufferTransfersOwnershipToCaller()
    {
        var writer = new SpanWriter(8, true);
        writer.Write((byte)1);
        writer.Write((byte)2);
        writer.Write((byte)3);

        var owner = writer.ToSpan();

        try
        {
            Assert.Equal(3, owner.Span.Length);
            Assert.Equal(new byte[] { 1, 2, 3 }, owner.Span.ToArray());
        }
        finally
        {
            owner.Dispose();
        }
    }

    [Fact]
    public void WriteAscii_FixedLengthPadsWithNulls()
    {
        Span<byte> backing = stackalloc byte[8];
        var writer = new SpanWriter(backing);
        writer.WriteAscii("hi", 5);

        Assert.Equal(5, writer.Position);
        Assert.Equal((byte)'h', backing[0]);
        Assert.Equal((byte)'i', backing[1]);
        Assert.Equal(0, backing[2]);
        Assert.Equal(0, backing[3]);
        Assert.Equal(0, backing[4]);
    }

    [Fact]
    public void WriteAscii_FixedLengthTruncatesLongString()
    {
        Span<byte> backing = stackalloc byte[8];
        var writer = new SpanWriter(backing);
        writer.WriteAscii("hello world", 5);

        Assert.Equal(5, writer.Position);
        Assert.Equal("hello", Encoding.ASCII.GetString(backing[..5]));
    }

    [Fact]
    public void WriteAscii_StringRoundTripsThroughReader()
    {
        Span<byte> backing = stackalloc byte[16];
        var writer = new SpanWriter(backing);
        writer.WriteAsciiNull("hello");

        var reader = new SpanReader(backing[..writer.Position]);
        Assert.Equal("hello", reader.ReadAscii());
    }

    [Fact]
    public void WriteBigUni_StringRoundTripsThroughReader()
    {
        Span<byte> backing = stackalloc byte[32];
        var writer = new SpanWriter(backing);
        writer.WriteBigUniNull("ciao");

        var reader = new SpanReader(backing[..writer.Position]);
        Assert.Equal("ciao", reader.ReadBigUni());
    }

    [Fact]
    public void WriteBool_EncodesAsByte()
    {
        Span<byte> backing = stackalloc byte[2];
        var writer = new SpanWriter(backing);

        writer.Write(true);
        writer.Write(false);

        Assert.Equal(1, backing[0]);
        Assert.Equal(0, backing[1]);
    }

    [Fact]
    public void WriteByte_AdvancesPositionAndBytesWritten()
    {
        Span<byte> backing = stackalloc byte[4];
        var writer = new SpanWriter(backing);

        writer.Write((byte)0x42);
        writer.Write((byte)0x43);

        Assert.Equal(2, writer.Position);
        Assert.Equal(2, writer.BytesWritten);
        Assert.Equal(0x42, backing[0]);
        Assert.Equal(0x43, backing[1]);
    }

    [Fact]
    public void WriteInt16_BigEndian()
    {
        Span<byte> backing = stackalloc byte[2];
        var writer = new SpanWriter(backing);
        writer.Write((short)0x1234);

        Assert.Equal(0x12, backing[0]);
        Assert.Equal(0x34, backing[1]);
    }

    [Fact]
    public void WriteInt32_BigEndian_RoundTripsThroughReader()
    {
        Span<byte> backing = stackalloc byte[4];
        var writer = new SpanWriter(backing);
        writer.Write(0x12345678);

        var reader = new SpanReader(backing);
        Assert.Equal(0x12345678, reader.ReadInt32());
    }

    [Fact]
    public void WriteInt64_BigEndian_RoundTripsThroughReader()
    {
        Span<byte> backing = stackalloc byte[8];
        var writer = new SpanWriter(backing);
        writer.Write(0x1122334455667788L);

        var reader = new SpanReader(backing);
        Assert.Equal(0x1122334455667788L, reader.ReadInt64());
    }

    [Fact]
    public void WriteLE_Int16_LittleEndian()
    {
        Span<byte> backing = stackalloc byte[2];
        var writer = new SpanWriter(backing);
        writer.WriteLE((short)0x1234);

        Assert.Equal(0x34, backing[0]);
        Assert.Equal(0x12, backing[1]);
    }

    [Fact]
    public void WriteLE_Int32_RoundTripsThroughReaderLE()
    {
        Span<byte> backing = stackalloc byte[4];
        var writer = new SpanWriter(backing);
        writer.WriteLE(0x12345678);

        var reader = new SpanReader(backing);
        Assert.Equal(0x12345678, reader.ReadInt32LE());
    }

    [Fact]
    public void WriteLittleUni_FixedLengthPadsWithUnicodeNulls()
    {
        Span<byte> backing = stackalloc byte[16];
        var writer = new SpanWriter(backing);
        writer.WriteLittleUni("hi", 5);

        // Padding fills (5-2)*2 = 6 zero bytes after the 4 written bytes for "hi".
        Assert.Equal(10, writer.Position);
        Assert.Equal((byte)'h', backing[0]);
        Assert.Equal(0, backing[1]);
        Assert.Equal((byte)'i', backing[2]);
        Assert.Equal(0, backing[3]);

        for (var i = 4; i < 10; i++)
        {
            Assert.Equal(0, backing[i]);
        }
    }

    [Fact]
    public void WriteLittleUni_StringRoundTripsThroughReader_WithCustomEncoding()
    {
        // Use a separate UnicodeEncoding instance to confirm GetTerminatorWidth handles
        // custom UnicodeEncoding values correctly.
        var unicode = new UnicodeEncoding(false, false);
        Span<byte> backing = stackalloc byte[32];
        var writer = new SpanWriter(backing);
        writer.Write("test".AsSpan(), unicode);
        writer.Write((ushort)0);

        var reader = new SpanReader(backing[..writer.Position]);
        Assert.Equal("test", reader.ReadString(unicode));
    }

    [Fact]
    public void WriteSByte_RoundTripsThroughReader()
    {
        Span<byte> backing = stackalloc byte[1];
        var writer = new SpanWriter(backing);
        writer.Write((sbyte)-128);

        var reader = new SpanReader(backing);
        Assert.Equal((sbyte)-128, reader.ReadSByte());
    }

    [Fact]
    public void WriteSpan_CopiesBytes()
    {
        Span<byte> backing = stackalloc byte[8];
        var writer = new SpanWriter(backing);
        ReadOnlySpan<byte> payload = [1, 2, 3, 4];

        writer.Write(payload);

        Assert.Equal(4, writer.Position);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, backing[..4].ToArray());
    }

    [Fact]
    public void WriteUInt32_BigEndian_RoundTripsThroughReader()
    {
        Span<byte> backing = stackalloc byte[4];
        var writer = new SpanWriter(backing);
        writer.Write(0xDEADBEEF);

        var reader = new SpanReader(backing);
        Assert.Equal(0xDEADBEEF, reader.ReadUInt32());
    }

    [Fact]
    public void WriteUTF8_StringRoundTripsThroughReader()
    {
        Span<byte> backing = stackalloc byte[32];
        var writer = new SpanWriter(backing);
        writer.WriteUTF8("héllo");
        writer.Write((byte)0);

        var reader = new SpanReader(backing[..writer.Position]);
        Assert.Equal("héllo", reader.ReadUTF8());
    }
}
