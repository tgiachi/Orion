namespace OrionIrcd.IRC.Commands.Base;

public sealed class UserCommand : BaseIrcCommand
{
    private const string CommandCode = "USER";

    public override string Code => CommandCode;

    public string Username { get; set; } = string.Empty;

    public string Mode { get; set; } = string.Empty;

    public string Unused { get; set; } = string.Empty;

    public string RealName { get; set; } = string.Empty;
}
