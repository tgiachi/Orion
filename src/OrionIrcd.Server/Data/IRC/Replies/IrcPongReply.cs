using OrionIrcd.Server.Interfaces.IRC.Replies;

namespace OrionIrcd.Server.Data.IRC.Replies;

public sealed class IrcPongReply : IIrcReply
{
    public string Token { get; }

    public IrcPongReply(string token)
    {
        Token = token;
    }

    public string Format(IrcReplyContext context)
        => $":{context.ServerName} PONG {context.ServerName} :{Token}";
}
