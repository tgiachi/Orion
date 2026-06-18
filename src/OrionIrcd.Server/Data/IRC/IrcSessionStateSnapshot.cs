namespace OrionIrcd.Server.Data.IRC;

public sealed class IrcSessionStateSnapshot
{
    public long SessionId { get; init; }

    public string Nickname { get; init; } = string.Empty;

    public string Username { get; init; } = string.Empty;

    public string RealName { get; init; } = string.Empty;

    public bool IsPassAccepted { get; init; }

    public bool IsRegistered { get; init; }
}
