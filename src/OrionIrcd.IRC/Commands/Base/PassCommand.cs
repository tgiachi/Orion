namespace OrionIrcd.IRC.Commands.Base;

public sealed class PassCommand : BaseIrcCommand
{
    private const string CommandCode = "PASS";

    public override string Code => CommandCode;

    public string Password { get; set; } = string.Empty;
}
