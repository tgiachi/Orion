using OrionIrcd.Network.Interfaces.Framing;

namespace OrionIrcd.Server.Services.Network;

public sealed class IrcLineFramer : INetFramer
{
    public bool TryReadFrame(ReadOnlySpan<byte> buffer, out int frameLength)
    {
        var lineFeedIndex = buffer.IndexOf((byte)'\n');

        if (lineFeedIndex < 0)
        {
            frameLength = 0;

            return false;
        }

        frameLength = lineFeedIndex + 1;

        return true;
    }
}
