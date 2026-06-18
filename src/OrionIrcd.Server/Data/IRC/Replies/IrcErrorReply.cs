using OrionIrcd.Server.Interfaces.IRC.Replies;

namespace OrionIrcd.Server.Data.IRC.Replies;

public sealed class IrcErrorReply : IIrcReply
{
    public string Message { get; }

    public IrcErrorReply(string message)
    {
        Message = message;
    }

    public string Format(IrcReplyContext context)
    {
        _ = context;

        return $"ERROR :{Message}";
    }
}
