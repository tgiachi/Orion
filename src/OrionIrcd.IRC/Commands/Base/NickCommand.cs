using OrionIrcd.IRC.Interfaces;
using OrionIrcd.IRC.Message;

namespace OrionIrcd.IRC.Commands.Base;

public sealed class NickCommand : BaseIrcCommand, IIrcCommandParser, IIrcCommandProducer
{
    private const string CommandCode = "NICK";

    public override string Code => CommandCode;

    public string Nickname { get; set; } = string.Empty;

    public void Parse(RawIrcMessage rawMessage)
    {
        Nickname = rawMessage.Params.Count > 0 ? rawMessage.Params[0] : string.Empty;
    }

    public RawIrcMessage Produce()
        => string.IsNullOrWhiteSpace(Nickname) ? CreateMessage() : CreateMessage([Nickname]);
}
