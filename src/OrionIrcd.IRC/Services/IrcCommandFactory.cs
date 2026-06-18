using OrionIrcd.IRC.Commands.Internal;
using OrionIrcd.IRC.Interfaces;
using OrionIrcd.IRC.Message;

namespace OrionIrcd.IRC.Services;

internal sealed class IrcCommandFactory : IIrcCommandFactory
{
    private readonly IIrcCommandRegistry _registry;
    private readonly IrcCommandBinder _binder;

    public IrcCommandFactory(IIrcCommandRegistry registry, IrcCommandBinder binder)
    {
        _registry = registry;
        _binder = binder;
    }

    public IIrcCommand CreateOrFallback(RawIrcMessage rawMessage)
    {
        if (_registry.TryCreate(rawMessage.Command, out var command) && command is not null)
        {
            _binder.Bind(command, rawMessage);

            return command;
        }

        var fallback = new NotParsedCommand();
        _binder.Bind(fallback, rawMessage);

        return fallback;
    }
}
