using OrionIrcd.Server.Interfaces.IRC.Replies;

namespace OrionIrcd.Server.Data.IRC.Replies;

public sealed class IrcYourHostReply : IIrcReply
{
    public string Target { get; }

    public string Version { get; }

    public IrcYourHostReply(string target, string version)
    {
        Target = target;
        Version = version;
    }

    public string Format(IrcReplyContext context)
        => $":{context.ServerName} 002 {Target} :Your host is {context.ServerName}, running version {Version}";
}
