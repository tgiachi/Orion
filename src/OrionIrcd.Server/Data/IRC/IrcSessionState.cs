namespace OrionIrcd.Server.Data.IRC;

public sealed class IrcSessionState
{
    public long SessionId { get; init; }

    public string Nickname { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string RealName { get; set; } = string.Empty;

    public bool IsPassAccepted { get; set; }

    public bool IsRegistered { get; set; }
}
