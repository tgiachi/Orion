namespace OrionIrcd.IRC.Commands.Base;

public sealed class PingCommand : BaseIrcCommand
{
    private const string CommandCode = "PING";

    public override string Code => CommandCode;

    public string Token { get; set; } = string.Empty;
}
