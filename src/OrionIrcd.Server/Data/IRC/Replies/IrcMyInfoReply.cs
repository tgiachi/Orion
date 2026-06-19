using OrionIrcd.Server.Interfaces.IRC.Replies;

namespace OrionIrcd.Server.Data.IRC.Replies;

public sealed class IrcMyInfoReply : IIrcReply
{
    public string Target { get; }

    public string Version { get; }

    public IrcMyInfoReply(string target, string version)
    {
        Target = target;
        Version = version;
    }

    public string Format(IrcReplyContext context)
        => $":{context.ServerName} 004 {Target} {context.ServerName} OrionIRCd {Version} o o";
}
