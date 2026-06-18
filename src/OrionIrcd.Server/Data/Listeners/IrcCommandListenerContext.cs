using OrionIrcd.IRC.Interfaces;
using OrionIrcd.Server.Data.Sessions;

namespace OrionIrcd.Server.Data.Listeners;

public sealed class IrcCommandListenerContext<TCommand> where TCommand : IIrcCommand
{
    public IrcCommandListenerContext(NetworkSession session, TCommand command)
    {
        Session = session;
        Command = command;
    }

    public NetworkSession Session { get; }

    public TCommand Command { get; }
}
