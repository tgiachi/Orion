using System.Buffers.Binary;
using System.Text;
using OrionIrcd.Network.Spans;

namespace OrionIrcd.Tests.Network.Spans;

public class SpanReaderTests
{
    [Fact]
    public void Read_CopiesIntoDestination_AdvancesByCount()
    {
        ReadOnlySpan<byte> data = [1, 2, 3, 4, 5];
        var reader = new SpanReader(data);
        Span<byte> dest = stackalloc byte[3];

        var written = reader.Read(dest);

        Assert.Equal(3, written);
        Assert.Equal(new byte[] { 1, 2, 3 }, dest.ToArray());
        Assert.Equal(3, reader.Position);
    }

    [Fact]
    public void Read_DestinationLargerThanRemaining_DoesShortRead()
    {
        ReadOnlySpan<byte> data = [1, 2];
        var reader = new SpanReader(data);
        Span<byte> dest = stackalloc byte[8];

        var written = reader.Read(dest);

        Assert.Equal(2, written);
        Assert.Equal(2, reader.Position);
    }

    [Fact]
    public void ReadBoolean_NonZeroByte_ReturnsTrue()
    {
        ReadOnlySpan<byte> data = [0x01, 0x00, 0x7F];
        var reader = new SpanReader(data);

        Assert.True(reader.ReadBoolean());
        Assert.False(reader.ReadBoolean());
        Assert.True(reader.ReadBoolean());
    }

    [Fact]
    public void ReadByte_AdvancesPosition()
    {
        ReadOnlySpan<byte> data = [0x01, 0x02, 0x03];
        var reader = new SpanReader(data);

        Assert.Equal(0x01, reader.ReadByte());
        Assert.Equal(0x02, reader.ReadByte());
        Assert.Equal(2, reader.Position);
        Assert.Equal(1, reader.Remaining);
    }

    [Fact]
    public void ReadByte_PastEnd_Throws()
    {
        ReadOnlySpan<byte> data = [];
        var reader = new SpanReader(data);

        var threw = false;

        try
        {
            reader.ReadByte();
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }

        Assert.True(threw);
    }

    [Fact]
    public void ReadBytes_NotEnoughData_Throws()
    {
        ReadOnlySpan<byte> data = [1, 2];
        var reader = new SpanReader(data);

        var threw = false;

        try
        {
            reader.ReadBytes(5);
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }

        Assert.True(threw);
    }

    [Fact]
    public void ReadBytes_ReturnsFreshAllocation()
    {
        ReadOnlySpan<byte> data = [1, 2, 3, 4, 5];
        var reader = new SpanReader(data);

        var bytes = reader.ReadBytes(3);

        Assert.Equal(new byte[] { 1, 2, 3 }, bytes);
        Assert.Equal(3, reader.Position);
    }

    [Fact]
    public void ReadInt16_BigEndian()
    {
        ReadOnlySpan<byte> data = [0x12, 0x34];
        var reader = new SpanReader(data);

        Assert.Equal((short)0x1234, reader.ReadInt16());
    }

    [Fact]
    public void ReadInt16LE_LittleEndian()
    {
        ReadOnlySpan<byte> data = [0x34, 0x12];
        var reader = new SpanReader(data);

        Assert.Equal((short)0x1234, reader.ReadInt16LE());
    }

    [Fact]
    public void ReadInt32_BigEndian()
    {
        ReadOnlySpan<byte> data = [0x12, 0x34, 0x56, 0x78];
        var reader = new SpanReader(data);

        Assert.Equal(0x12345678, reader.ReadInt32());
    }

    [Fact]
    public void ReadInt32_PastEnd_Throws()
    {
        ReadOnlySpan<byte> data = [0x01, 0x02];
        var reader = new SpanReader(data);

        var threw = false;

        try
        {
            reader.ReadInt32();
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }

        Assert.True(threw);
    }

    [Fact]
    public void ReadInt32LE_LittleEndian()
    {
        ReadOnlySpan<byte> data = [0x78, 0x56, 0x34, 0x12];
        var reader = new SpanReader(data);

        Assert.Equal(0x12345678, reader.ReadInt32LE());
    }

    [Fact]
    public void ReadInt64_BigEndian()
    {
        Span<byte> buf = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(buf, 0x1122334455667788L);

        var reader = new SpanReader(buf);

        Assert.Equal(0x1122334455667788L, reader.ReadInt64());
    }

    [Fact]
    public void ReadInt64LE_RoundTripsThroughLittleEndian()
    {
        Span<byte> buffer = stackalloc byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(buffer, 0x1122334455667788L);

        var reader = new SpanReader(buffer);

        Assert.Equal(0x1122334455667788L, reader.ReadInt64LE());
    }

    [Fact]
    public void ReadSByte_RoundTripsSignedValue()
    {
        ReadOnlySpan<byte> data = [0xFF];
        var reader = new SpanReader(data);

        Assert.Equal((sbyte)-1, reader.ReadSByte());
    }

    [Fact]
    public void ReadString_Ascii_NullTerminated()
    {
        ReadOnlySpan<byte> data = [(byte)'h', (byte)'i', 0x00, (byte)'x', (byte)'y'];
        var reader = new SpanReader(data);

        var read = reader.ReadString(Encoding.ASCII);

        Assert.Equal("hi", read);
        Assert.Equal(3, reader.Position);
    }

    [Fact]
    public void ReadString_AsciiFixedLength_StopsAtFixedSize()
    {
        ReadOnlySpan<byte> data = [(byte)'a', (byte)'b', (byte)'c', (byte)'d', (byte)'e'];
        var reader = new SpanReader(data);

        var read = reader.ReadString(Encoding.ASCII, fixedLength: 3);

        Assert.Equal("abc", read);
        Assert.Equal(3, reader.Position);
    }

    [Fact]
    public void ReadString_FixedLengthWithEarlyTerminator_AdvancesFullWindow()
    {
        // Fixed length 5 with terminator at byte 2: returned string truncates at the
        // terminator but the cursor still advances past the entire window.
        ReadOnlySpan<byte> data = [(byte)'a', (byte)'b', 0x00, (byte)'d', (byte)'e'];
        var reader = new SpanReader(data);

        var read = reader.ReadString(Encoding.ASCII, fixedLength: 5);

        Assert.Equal("ab", read);
        Assert.Equal(5, reader.Position);
    }

    [Fact]
    public void ReadString_FixedLengthZero_ReturnsEmptyAndDoesNotAdvance()
    {
        ReadOnlySpan<byte> data = [1, 2, 3];
        var reader = new SpanReader(data);

        var read = reader.ReadString(Encoding.ASCII, fixedLength: 0);

        Assert.Equal("", read);
        Assert.Equal(0, reader.Position);
    }

    [Fact]
    public void ReadString_UTF8_NullTerminated()
    {
        var bytes = new List<byte>();
        bytes.AddRange(Encoding.UTF8.GetBytes("hello"));
        bytes.Add(0x00);
        bytes.AddRange(Encoding.UTF8.GetBytes("after"));

        var reader = new SpanReader(bytes.ToArray());
        var read = reader.ReadString(Encoding.UTF8);

        Assert.Equal("hello", read);
    }

    [Fact]
    public void ReadString_WithBclUnicodeEncoding_UsesTwoByteTerminator()
    {
        var bytes = new List<byte>();
        bytes.AddRange(Encoding.Unicode.GetBytes("ab"));
        bytes.Add(0x00);
        bytes.Add(0x00);
        bytes.AddRange(Encoding.Unicode.GetBytes("xy"));

        var reader = new SpanReader(bytes.ToArray());
        var read = reader.ReadString(Encoding.Unicode);

        Assert.Equal("ab", read);
    }

    [Fact]
    public void ReadString_WithCustomUnicodeEncoding_StillUsesTwoByteTerminator()
    {
        // Regression: GetTerminatorWidth used ReferenceEquals against Encoding.Unicode,
        // so another UnicodeEncoding instance was treated as 1-byte terminator and corrupted reads.
        var unicode = new UnicodeEncoding(false, false);
        var bytes = new List<byte>();
        bytes.AddRange(unicode.GetBytes("ab"));
        bytes.Add(0x00);
        bytes.Add(0x00);
        bytes.AddRange(unicode.GetBytes("xy"));

        var reader = new SpanReader(bytes.ToArray());
        var read = reader.ReadString(unicode);

        Assert.Equal("ab", read);
    }

    [Fact]
    public void ReadUInt16_BigEndian()
    {
        ReadOnlySpan<byte> data = [0xFF, 0xFE];
        var reader = new SpanReader(data);

        Assert.Equal((ushort)0xFFFE, reader.ReadUInt16());
    }

    [Fact]
    public void ReadUInt32_BigEndian()
    {
        Span<byte> buf = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(buf, 0xDEADBEEF);

        var reader = new SpanReader(buf);

        Assert.Equal(0xDEADBEEF, reader.ReadUInt32());
    }

    [Fact]
    public void ReadUInt64LE_RoundTrips()
    {
        Span<byte> buf = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(buf, 0xCAFEBABEDEADBEEFUL);

        var reader = new SpanReader(buf);

        Assert.Equal(0xCAFEBABEDEADBEEFUL, reader.ReadUInt64LE());
    }

    [Fact]
    public void Seek_AbsoluteAndCurrent()
    {
        ReadOnlySpan<byte> data = [1, 2, 3, 4, 5];
        var reader = new SpanReader(data);

        reader.Seek(2, SeekOrigin.Begin);
        Assert.Equal(3, reader.ReadByte());
        reader.Seek(-1, SeekOrigin.Current);
        Assert.Equal(3, reader.ReadByte());
    }

    [Fact]
    public void Seek_BeyondEnd_Throws()
    {
        ReadOnlySpan<byte> data = [1, 2, 3];
        var reader = new SpanReader(data);

        var threw = false;

        try
        {
            reader.Seek(10, SeekOrigin.Begin);
        }
        catch (IOException)
        {
            threw = true;
        }

        Assert.True(threw);
    }

    [Fact]
    public void Seek_End_PositionsRelativeToBufferEnd()
    {
        ReadOnlySpan<byte> data = [1, 2, 3, 4, 5];
        var reader = new SpanReader(data);

        reader.Seek(-2, SeekOrigin.End);

        Assert.Equal(3, reader.Position);
        Assert.Equal(4, reader.ReadByte());
    }

    [Fact]
    public void Seek_NegativeAbsolute_ClampsToZero()
    {
        ReadOnlySpan<byte> data = [1, 2, 3];
        var reader = new SpanReader(data);
        reader.Seek(2, SeekOrigin.Begin);

        reader.Seek(-10, SeekOrigin.Begin);

        Assert.Equal(0, reader.Position);
    }
}
