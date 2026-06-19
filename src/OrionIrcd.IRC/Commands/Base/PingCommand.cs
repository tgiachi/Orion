using OrionIrcd.IRC.Interfaces;
using OrionIrcd.IRC.Message;

namespace OrionIrcd.IRC.Commands.Base;

public sealed class PingCommand : BaseIrcCommand, IIrcCommandParser, IIrcCommandProducer
{
    private const string CommandCode = "PING";

    public override string Code => CommandCode;

    public string Token { get; set; } = string.Empty;

    public void Parse(RawIrcMessage rawMessage)
    {
        Token = rawMessage.Trailing ?? (rawMessage.Params.Count > 0 ? rawMessage.Params[0] : string.Empty);
    }

    public RawIrcMessage Produce()
        => CreateMessage(trailing: string.IsNullOrWhiteSpace(Token) ? null : Token);
}
