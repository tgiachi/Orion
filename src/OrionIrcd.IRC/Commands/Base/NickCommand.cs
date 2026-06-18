namespace OrionIrcd.IRC.Commands.Base;

public sealed class NickCommand : BaseIrcCommand
{
    private const string CommandCode = "NICK";

    public override string Code => CommandCode;

    public string Nickname { get; set; } = string.Empty;
}
