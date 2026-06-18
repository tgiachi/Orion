using OrionIrcd.IRC.Types;

namespace OrionIrcd.IRC.Data;

public sealed record IrcCommandError
{
    public IrcCommandErrorType Type { get; init; }

    public string Message { get; init; } = "";
}
