using OrionIrcd.IRC.Interfaces;
using OrionIrcd.IRC.Message;

namespace OrionIrcd.IRC.Commands.Internal;

public sealed class NotParsedCommand : IIrcCommand
{
    public string Code { get; set; } = "";

    public string Message { get; set; } = "";

    public string? Raw { get; set; }

    public IrcMessagePrefix? Prefix { get; set; }

    public IReadOnlyDictionary<string, string?> Tags { get; set; }
        = new Dictionary<string, string?>(StringComparer.Ordinal);

    public IReadOnlyList<string> Params { get; set; } = [];

    public string? Trailing { get; set; }
}
