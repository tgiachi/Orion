using OrionIrcd.Server.Interfaces.IRC.Replies;

namespace OrionIrcd.Server.Data.IRC.Replies;

public sealed class IrcMotdStartReply : IIrcReply
{
    public string Target { get; }

    public IrcMotdStartReply(string target)
    {
        Target = target;
    }

    public string Format(IrcReplyContext context)
        => $":{context.ServerName} 375 {Target} :- {context.ServerName} Message of the day -";
}
