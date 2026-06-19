using OrionIrcd.Core.Interfaces.Events;
using OrionIrcd.Server.Data.IRC;
using OrionIrcd.Server.Core.Data.Sessions;

namespace OrionIrcd.Server.Data.Events;

public sealed class IrcSessionRegisteredEvent : IEvent
{
    public NetworkSession Session { get; }

    public IrcSessionStateSnapshot State { get; }

    public IrcSessionRegisteredEvent(NetworkSession session, IrcSessionStateSnapshot state)
    {
        Session = session;
        State = state;
    }
}
