using DryIoc;
using OrionIrcd.IRC.Interfaces;
using OrionIrcd.Server.Data.Sessions;
using OrionIrcd.Server.Interfaces.Listeners;

namespace OrionIrcd.Server.Data.Listeners;

public sealed class IrcCommandDispatchRegistration
{
    private readonly Func<IIrcCommand, bool> _canDispatch;
    private readonly Func<IContainer, NetworkSession, IIrcCommand, IReadOnlyList<IrcCommandDispatchInvocation>>
        _createInvocations;

    private IrcCommandDispatchRegistration(
        Type commandType,
        Type listenerType,
        Func<IIrcCommand, bool> canDispatch,
        Func<IContainer, NetworkSession, IIrcCommand, IReadOnlyList<IrcCommandDispatchInvocation>> createInvocations
    )
    {
        CommandType = commandType;
        ListenerType = listenerType;
        _canDispatch = canDispatch;
        _createInvocations = createInvocations;
    }

    public Type CommandType { get; }

    public Type ListenerType { get; }

    public static IrcCommandDispatchRegistration Create<TCommand, TListener>()
        where TCommand : IIrcCommand
        where TListener : IIrcCommandListener<TCommand>
        => new(
            typeof(TCommand),
            typeof(TListener),
            static command => command is TCommand,
            static (container, session, command) => CreateInvocations<TCommand, TListener>(container, session, command)
        );

    internal bool CanDispatch(IIrcCommand command)
        => _canDispatch(command);

    internal IReadOnlyList<IrcCommandDispatchInvocation> CreateInvocations(
        IContainer container,
        NetworkSession session,
        IIrcCommand command
    )
        => _createInvocations(container, session, command);

    private static IReadOnlyList<IrcCommandDispatchInvocation> CreateInvocations<TCommand, TListener>(
        IContainer container,
        NetworkSession session,
        IIrcCommand command
    )
        where TCommand : IIrcCommand
        where TListener : IIrcCommandListener<TCommand>
    {
        if (command is not TCommand typedCommand)
        {
            return [];
        }

        var context = new IrcCommandListenerContext<TCommand>(session, typedCommand);
        var listener = container.Resolve<IIrcCommandListener<TCommand>>(serviceKey: typeof(TListener));

        return
        [
            new(
                listener,
                typeof(TCommand),
                cancellationToken => listener.HandleCommandAsync(context, cancellationToken)
            )
        ];
    }
}
