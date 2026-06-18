namespace OrionIrcd.IRC.Message;

public class IrcMessagePrefix
{
    public string? ServerName { get; init; }

    public string? Nick { get; init; }
    public string? User { get; init; }
    public string? Host { get; init; }

    public bool IsServer => ServerName is not null;
    public bool IsUser => Nick is not null;

    public override string ToString()
        => IsServer && ServerName is not null ? ServerName : $"{Nick}!{User}@{Host}";
}
