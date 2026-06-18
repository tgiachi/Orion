using OrionIrcd.Core.Interfaces.Events;
using OrionIrcd.Server.Data.Sessions;

namespace OrionIrcd.Server.Data.Events;

public sealed class NetworkSessionConnectedEvent : IEvent
{
    public NetworkSession Session { get; }

    public NetworkSessionConnectedEvent(NetworkSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        Session = session;
    }
}
