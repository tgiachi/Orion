using OrionIrcd.IRC.Interfaces;
using Serilog;

namespace OrionIrcd.IRC.Services;

public sealed class IrcCommandRegistry : IIrcCommandRegistry
{
    private readonly ILogger _logger = Log.ForContext<IrcCommandRegistry>();
    private readonly Dictionary<string, Func<IIrcCommand>> _factories = new(StringComparer.OrdinalIgnoreCase);

    public void RegisterCommand<TCommand>()
        where TCommand : IIrcCommand, new()
    {
        var prototype = new TCommand();
        var code = prototype.Code;

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new InvalidOperationException("Command code cannot be null or whitespace.");
        }

        if (!_factories.TryAdd(code, static () => new TCommand()))
        {
            throw new InvalidOperationException($"Command '{code}' is already registered.");
        }

        _logger.Debug("Registering command '{code}'", code);
    }

    public bool TryCreate(string code, out IIrcCommand? command)
    {
        if (_factories.TryGetValue(code, out var factory))
        {
            command = factory();

            return true;
        }

        command = null;

        return false;
    }
}
