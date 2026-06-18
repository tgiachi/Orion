namespace OrionIrcd.Server.Data.IRC.Replies;

public sealed class IrcReplyContext
{
    public string ServerName { get; }

    public IrcReplyContext(string serverName)
    {
        ServerName = serverName;
    }
}
