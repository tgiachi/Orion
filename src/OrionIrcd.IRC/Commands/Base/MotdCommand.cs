namespace OrionIrcd.IRC.Commands.Base;

public sealed class MotdCommand : BaseIrcCommand
{
    private const string CommandCode = "MOTD";

    public override string Code => CommandCode;
}
