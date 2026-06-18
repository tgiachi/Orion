namespace OrionIrcd.IRC.Message;

/// <summary>
/// Represents a low-level raw IRC message as parsed from the wire protocol.
/// This is the direct representation of IRC protocol messages including tags,
/// prefix, command, parameters, and trailing content. For higher-level command
/// handling, use IrcCommand derivatives.
/// </summary>
public class RawIrcMessage
{
    public IrcMessagePrefix? Prefix { get; init; }

    public IReadOnlyDictionary<string, string?> Tags { get; init; }
        = new Dictionary<string, string?>(StringComparer.Ordinal);

    public string Command { get; init; }

    public IReadOnlyList<string> Params { get; init; } = [];

    public string? Trailing { get; init; }

    public string? Raw { get; init; }

    public override string ToString()
    {
        if (Raw is not null)
        {
            return Raw;
        }

        var message = Prefix is not null ? $":{Prefix} {Command}" : Command;

        if (Params.Count > 0)
        {
            message = $"{message} {string.Join(" ", Params)}";
        }

        if (Trailing is not null)
        {
            message = $"{message} :{Trailing}";
        }

        return message;
    }
}
