using OrionIrcd.IRC.Interfaces;

namespace OrionIrcd.Tests.Support.IRC;

public sealed class WhitespaceCodeCommand : IIrcCommand
{
    public string Code => " ";
}
