using OrionIrcd.IRC.Interfaces;
using OrionIrcd.IRC.Message;

namespace OrionIrcd.IRC.Commands.Base;

public sealed class PassCommand : BaseIrcCommand, IIrcCommandParser, IIrcCommandProducer
{
    private const string CommandCode = "PASS";

    public override string Code => CommandCode;

    public string Password { get; set; } = string.Empty;

    public void Parse(RawIrcMessage rawMessage)
    {
        Password = rawMessage.Trailing ?? (rawMessage.Params.Count > 0 ? rawMessage.Params[0] : string.Empty);
    }

    public RawIrcMessage Produce()
        => string.IsNullOrWhiteSpace(Password) ? CreateMessage() : CreateMessage([Password]);
}
