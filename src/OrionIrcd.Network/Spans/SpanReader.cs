using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace OrionIrcd.Network.Spans;

public ref struct SpanReader : IDisposable
{
    private ReadOnlySpan<byte> _buffer;

    public int Length { get; private set; }
    public int Position { get; private set; }
    public readonly int Remaining => Length - Position;
    public readonly ReadOnlySpan<byte> Buffer => _buffer;

    public SpanReader(ReadOnlySpan<byte> span)
    {
        _buffer = span;
        Position = 0;
        Length = span.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Read(scoped Span<byte> bytes)
    {
        if (bytes.Length == 0)
        {
            return 0;
        }

        var bytesWritten = Math.Min(bytes.Length, Remaining);
        _buffer.Slice(Position, bytesWritten).CopyTo(bytes);
        Position += bytesWritten;

        return bytesWritten;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadAscii(int fixedLength)
    {
        return ReadString(Encoding.ASCII, fixedLength: fixedLength);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadAscii()
    {
        return ReadString(Encoding.ASCII);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadAsciiSafe(int fixedLength)
    {
        return ReadString(Encoding.ASCII, true, fixedLength);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadAsciiSafe()
    {
        return ReadString(Encoding.ASCII, true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadBigUni(int fixedLength)
    {
        return ReadString(Encoding.BigEndianUnicode, fixedLength: fixedLength);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadBigUni()
    {
        return ReadString(Encoding.BigEndianUnicode);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadBigUniSafe(int fixedLength)
    {
        return ReadString(Encoding.BigEndianUnicode, true, fixedLength);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadBigUniSafe()
    {
        return ReadString(Encoding.BigEndianUnicode, true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ReadBoolean()
    {
        return ReadByte() > 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadByte()
    {
        if (Position >= Length)
        {
            ThrowInsufficientData();
        }

        return _buffer[Position++];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte[] ReadBytes(int length)
    {
        if (length > Remaining)
        {
            ThrowInsufficientData();
        }

        var bytes = _buffer.Slice(Position, length).ToArray();
        Position += length;

        return bytes;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short ReadInt16()
    {
        if (!BinaryPrimitives.TryReadInt16BigEndian(_buffer[Position..], out var value))
        {
            ThrowInsufficientData();
        }

        Position += 2;

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short ReadInt16LE()
    {
        if (!BinaryPrimitives.TryReadInt16LittleEndian(_buffer[Position..], out var value))
        {
            ThrowInsufficientData();
        }

        Position += 2;

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadInt32()
    {
        if (!BinaryPrimitives.TryReadInt32BigEndian(_buffer[Position..], out var value))
        {
            ThrowInsufficientData();
        }

        Position += 4;

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadInt32LE()
    {
        if (!BinaryPrimitives.TryReadInt32LittleEndian(_buffer[Position..], out var value))
        {
            ThrowInsufficientData();
        }

        Position += 4;

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadInt64()
    {
        if (!BinaryPrimitives.TryReadInt64BigEndian(_buffer[Position..], out var value))
        {
            ThrowInsufficientData();
        }

        Position += 8;

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadInt64LE()
    {
        if (!BinaryPrimitives.TryReadInt64LittleEndian(_buffer[Position..], out var value))
        {
            ThrowInsufficientData();
        }

        Position += 8;

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadLittleUni(int fixedLength)
    {
        return ReadString(Encoding.Unicode, fixedLength: fixedLength);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadLittleUni()
    {
        return ReadString(Encoding.Unicode);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadLittleUniSafe(int fixedLength)
    {
        return ReadString(Encoding.Unicode, true, fixedLength);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadLittleUniSafe()
    {
        return ReadString(Encoding.Unicode, true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public sbyte ReadSByte()
    {
        return (sbyte)ReadByte();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadString(Encoding encoding, bool safeString = false, int fixedLength = -1)
    {
        if (fixedLength == 0)
        {
            return string.Empty;
        }

        var terminatorWidth = GetTerminatorWidth(encoding);
        var isFixedLength = fixedLength > -1;
        var remaining = Remaining;
        int size;

        if (isFixedLength)
        {
            size = fixedLength * terminatorWidth;

            if (size > remaining)
            {
                ThrowInsufficientData();
            }
        }
        else
        {
            size = remaining - (remaining & (terminatorWidth - 1));
        }

        var span = _buffer.Slice(Position, size);
        var index = IndexOfTerminator(span, terminatorWidth);

        if (index > -1)
        {
            span = span[..index];
        }

        Position += isFixedLength || index < 0 ? size : index + terminatorWidth;

        var value = encoding.GetString(span);

        return safeString ? value.Replace('\0', ' ') : value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadUInt16()
    {
        if (!BinaryPrimitives.TryReadUInt16BigEndian(_buffer[Position..], out var value))
        {
            ThrowInsufficientData();
        }

        Position += 2;

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadUInt16LE()
    {
        if (!BinaryPrimitives.TryReadUInt16LittleEndian(_buffer[Position..], out var value))
        {
            ThrowInsufficientData();
        }

        Position += 2;

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadUInt32()
    {
        if (!BinaryPrimitives.TryReadUInt32BigEndian(_buffer[Position..], out var value))
        {
            ThrowInsufficientData();
        }

        Position += 4;

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadUInt32LE()
    {
        if (!BinaryPrimitives.TryReadUInt32LittleEndian(_buffer[Position..], out var value))
        {
            ThrowInsufficientData();
        }

        Position += 4;

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadUInt64()
    {
        if (!BinaryPrimitives.TryReadUInt64BigEndian(_buffer[Position..], out var value))
        {
            ThrowInsufficientData();
        }

        Position += 8;

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadUInt64LE()
    {
        if (!BinaryPrimitives.TryReadUInt64LittleEndian(_buffer[Position..], out var value))
        {
            ThrowInsufficientData();
        }

        Position += 8;

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadUTF8()
    {
        return ReadString(Encoding.UTF8);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadUTF8Safe(int fixedLength)
    {
        return ReadString(Encoding.UTF8, true, fixedLength);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadUTF8Safe()
    {
        return ReadString(Encoding.UTF8, true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Seek(int offset, SeekOrigin origin)
    {
        var newPosition = Math.Max(
            0,
            origin switch
            {
                SeekOrigin.Current => Position + offset,
                SeekOrigin.End => _buffer.Length + offset,
                _ => offset
            }
        );

        if (newPosition > _buffer.Length)
        {
            throw new IOException("Attempted to seek beyond the available buffer.");
        }

        Position = newPosition;

        return Position;
    }

    private static int GetTerminatorWidth(Encoding encoding)
    {
        return encoding switch
        {
            UnicodeEncoding => 2,
            UTF32Encoding => 4,
            _ => 1
        };
    }

    private static int IndexOfTerminator(ReadOnlySpan<byte> span, int terminatorWidth)
    {
        if (terminatorWidth == 1)
        {
            return span.IndexOf((byte)0);
        }

        for (var i = 0; i + terminatorWidth <= span.Length; i += terminatorWidth)
        {
            var allZero = true;

            for (var j = 0; j < terminatorWidth; j++)
            {
                if (span[i + j] != 0)
                {
                    allZero = false;

                    break;
                }
            }

            if (allZero)
            {
                return i;
            }
        }

        return -1;
    }

    [DoesNotReturn]
    private static void ThrowInsufficientData()
    {
        throw new InvalidOperationException("Insufficient data in buffer.");
    }

    public void Dispose()
    {
        _buffer = default;
        Position = 0;
        Length = 0;
    }
}
