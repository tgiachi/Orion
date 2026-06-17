using OrionIrcd.Network.Interfaces.Framing;

namespace OrionIrcd.Tests.Network.Framing;

/// <summary>
///     Test framer: each frame starts with a 2-byte big-endian length that includes the prefix itself.
/// </summary>
internal sealed class LengthPrefixedFramer : INetFramer
{
    public bool TryReadFrame(ReadOnlySpan<byte> buffer, out int frameLength)
    {
        frameLength = 0;

        if (buffer.Length < 2)
        {
            return false;
        }

        var declared = (buffer[0] << 8) | buffer[1];

        if (declared <= 0 || declared > buffer.Length)
        {
            return false;
        }

        frameLength = declared;

        return true;
    }
}
