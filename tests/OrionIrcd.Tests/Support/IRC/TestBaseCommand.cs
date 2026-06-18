using OrionIrcd.IRC.Commands.Base;

namespace OrionIrcd.Tests.Support.IRC;

public sealed class TestBaseCommand : BaseIrcCommand
{
    public override string Code => "TEST";

    public string? BoundTrailing { get; set; }
}
