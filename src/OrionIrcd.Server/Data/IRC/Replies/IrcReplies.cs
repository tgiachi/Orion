using OrionIrcd.Server.Interfaces.IRC.Replies;

namespace OrionIrcd.Server.Data.IRC.Replies;

public static class IrcReplies
{
    public static IIrcReply CapabilityList()
        => new IrcServerCommandReply("CAP", string.Empty, ["*", "LS"]);

    public static IIrcReply CapabilityNak(IEnumerable<string> capabilities)
        => new IrcServerCommandReply("CAP", string.Join(' ', capabilities), ["*", "NAK"]);

    public static IIrcReply ClosingLink(string reason)
        => new IrcErrorReply($"Closing Link: {reason}");

    public static IIrcReply NeedMoreParameters(string command)
        => new IrcNumericReply("461", "*", "Not enough parameters", [command]);

    public static IIrcReply NicknameInUse(string nickname)
        => new IrcNumericReply("433", "*", "Nickname is already in use", [nickname]);

    public static IIrcReply NoNicknameGiven()
        => new IrcNumericReply("431", "*", "No nickname given");

    public static IIrcReply PasswordMismatch()
        => new IrcNumericReply("464", "*", "Password incorrect");

    public static IIrcReply Pong(string token)
        => new IrcPongReply(token);

    public static IIrcReply Welcome(string nickname)
        => new IrcNumericReply("001", nickname, $"Welcome to OrionIRCd {nickname}");
}
