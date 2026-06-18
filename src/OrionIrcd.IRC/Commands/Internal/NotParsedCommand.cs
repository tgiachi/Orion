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

    public (bool Success, string? Error) Parse(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return (false, "Line is empty.");
        }

        var trimmed = line.Trim();
        var spaceIndex = trimmed.IndexOf(' ');

        if (spaceIndex == -1)
        {
            Code = trimmed;
            Message = "";

            return (true, null);
        }

        Code = trimmed[..spaceIndex];
        Message = trimmed[(spaceIndex + 1)..].TrimStart();

        return (true, null);
    }

    public (bool Success, string? Error) TryWrite(out string? output)
    {
        if (string.IsNullOrWhiteSpace(Code))
        {
            output = null;

            return (false, "Command code is required.");
        }

        output = string.IsNullOrWhiteSpace(Message)
                     ? Code
                     : $"{Code} {Message}";

        return (true, null);
    }
}
