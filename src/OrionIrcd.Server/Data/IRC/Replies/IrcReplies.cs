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

    public static IIrcReply Created(string target, string message)
        => new IrcNumericReply("003", target, message);

    public static IIrcReply EndOfMotd(string target)
        => new IrcNumericReply("376", target, "End of /MOTD command.");

    public static IIrcReply ISupport(string target)
        => new IrcNumericReply("005", target, "are supported by this server", ["CHANTYPES=#", "NICKLEN=30"]);

    public static IIrcReply NeedMoreParameters(string command)
        => new IrcNumericReply("461", "*", "Not enough parameters", [command]);

    public static IIrcReply NicknameInUse(string nickname)
        => new IrcNumericReply("433", "*", "Nickname is already in use", [nickname]);

    public static IIrcReply NoNicknameGiven()
        => new IrcNumericReply("431", "*", "No nickname given");

    public static IIrcReply NoMotd(string target)
        => new IrcNumericReply("422", target, "MOTD File is missing");

    public static IIrcReply MotdLine(string target, string line)
        => new IrcNumericReply("372", target, $"- {line}");

    public static IIrcReply MotdStart(string target)
        => new IrcMotdStartReply(target);

    public static IIrcReply MyInfo(string target, string version)
        => new IrcMyInfoReply(target, version);

    public static IIrcReply PasswordMismatch()
        => new IrcNumericReply("464", "*", "Password incorrect");

    public static IIrcReply Pong(string token)
        => new IrcPongReply(token);

    public static IIrcReply Welcome(string nickname)
        => new IrcNumericReply("001", nickname, $"Welcome to OrionIRCd {nickname}");

    public static IIrcReply YourHost(string target, string version)
        => new IrcYourHostReply(target, version);
}
