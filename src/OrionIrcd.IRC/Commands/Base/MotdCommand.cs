using OrionIrcd.IRC.Interfaces;
using OrionIrcd.IRC.Message;

namespace OrionIrcd.IRC.Commands.Base;

public sealed class MotdCommand : BaseIrcCommand, IIrcCommandProducer
{
    private const string CommandCode = "MOTD";

    public override string Code => CommandCode;

    public RawIrcMessage Produce()
        => CreateMessage();
}
