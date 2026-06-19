using OrionIrcd.IRC.Commands.Base;
using OrionIrcd.IRC.Interfaces;
using OrionIrcd.IRC.Message;

namespace OrionIrcd.Tests.Support.IRC;

public sealed class TestBaseCommand : BaseIrcCommand, IIrcCommandParser
{
    public override string Code => "TEST";

    public string? BoundTrailing { get; set; }

    public void Parse(RawIrcMessage rawMessage)
    {
        BoundTrailing = rawMessage.Trailing;
    }
}
