using OrionIrcd.Core.Interfaces.Events;
using OrionIrcd.Server.Data.Sessions;

namespace OrionIrcd.Server.Data.Events;

public sealed class NetworkSessionDataReceivedEvent : IEvent
{
    public NetworkSession Session { get; }

    public ReadOnlyMemory<byte> Data { get; }

    public NetworkSessionDataReceivedEvent(NetworkSession session, ReadOnlyMemory<byte> data)
    {
        ArgumentNullException.ThrowIfNull(session);

        Session = session;
        Data = data.ToArray();
    }
}
