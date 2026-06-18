namespace OrionIrcd.IRC.Commands.Base;

public sealed class PongCommand : BaseIrcCommand
{
    private const string CommandCode = "PONG";

    public override string Code => CommandCode;

    public string Token { get; set; } = string.Empty;
}
