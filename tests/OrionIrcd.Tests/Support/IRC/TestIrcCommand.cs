using OrionIrcd.IRC.Interfaces;

namespace OrionIrcd.Tests.Support.IRC;

public sealed class TestIrcCommand : IIrcCommand
{
    public string Code => "TEST";

    public string? BoundTrailing { get; set; }
}
