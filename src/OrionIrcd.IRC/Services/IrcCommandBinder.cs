using OrionIrcd.IRC.Commands.Base;
using OrionIrcd.IRC.Commands.Internal;
using OrionIrcd.IRC.Interfaces;
using OrionIrcd.IRC.Message;

namespace OrionIrcd.IRC.Services;

public sealed class IrcCommandBinder
{
    public void Bind(IIrcCommand command, RawIrcMessage rawMessage)
    {
        if (command is BaseIrcCommand baseCommand)
        {
            baseCommand.Raw = rawMessage.Raw;
            baseCommand.Prefix = rawMessage.Prefix;
            baseCommand.Tags = rawMessage.Tags;
            baseCommand.Params = rawMessage.Params;
            baseCommand.Trailing = rawMessage.Trailing;
        }
        else if (command is NotParsedCommand notParsed)
        {
            notParsed.Code = rawMessage.Command;
            notParsed.Raw = rawMessage.Raw;
            notParsed.Prefix = rawMessage.Prefix;
            notParsed.Tags = rawMessage.Tags;
            notParsed.Params = rawMessage.Params;
            notParsed.Trailing = rawMessage.Trailing;
            notParsed.Message = BuildFallbackMessage(rawMessage);
        }

        if (command is IIrcCommandParser parser)
        {
            parser.Parse(rawMessage);
        }
    }

    private static string BuildFallbackMessage(RawIrcMessage rawMessage)
        => rawMessage.Raw ?? rawMessage.ToString();
}
