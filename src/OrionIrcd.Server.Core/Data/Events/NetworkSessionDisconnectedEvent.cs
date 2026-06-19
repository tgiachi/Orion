using OrionIrcd.Core.Interfaces.Events;
using OrionIrcd.Server.Core.Data.Sessions;

namespace OrionIrcd.Server.Core.Data.Events;

public sealed class NetworkSessionDisconnectedEvent : IEvent
{
    public NetworkSession Session { get; }

    public NetworkSessionDisconnectedEvent(NetworkSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        Session = session;
    }
}
