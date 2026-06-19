using OrionIrcd.IRC.Interfaces;
using OrionIrcd.IRC.Message;

namespace OrionIrcd.IRC.Commands.Base;

public sealed class QuitCommand : BaseIrcCommand, IIrcCommandParser, IIrcCommandProducer
{
    private const string CommandCode = "QUIT";

    public override string Code => CommandCode;

    public string Reason { get; set; } = string.Empty;

    public void Parse(RawIrcMessage rawMessage)
    {
        Reason = rawMessage.Trailing ?? string.Empty;
    }

    public RawIrcMessage Produce()
        => CreateMessage(trailing: string.IsNullOrWhiteSpace(Reason) ? null : Reason);
}
