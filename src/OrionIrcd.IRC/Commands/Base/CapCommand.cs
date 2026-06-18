namespace OrionIrcd.IRC.Commands.Base;

public sealed class CapCommand : BaseIrcCommand
{
    private const string CommandCode = "CAP";

    public override string Code => CommandCode;

    public string Subcommand { get; set; } = string.Empty;

    public IReadOnlyList<string> Capabilities { get; set; } = [];
}
