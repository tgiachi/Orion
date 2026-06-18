namespace OrionIrcd.IRC.Commands.Base;

public sealed class QuitCommand : BaseIrcCommand
{
    private const string CommandCode = "QUIT";

    public override string Code => CommandCode;

    public string Reason { get; set; } = string.Empty;
}
