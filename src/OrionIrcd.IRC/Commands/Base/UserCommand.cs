using OrionIrcd.IRC.Interfaces;
using OrionIrcd.IRC.Message;

namespace OrionIrcd.IRC.Commands.Base;

public sealed class UserCommand : BaseIrcCommand, IIrcCommandParser, IIrcCommandProducer
{
    private const string CommandCode = "USER";

    public override string Code => CommandCode;

    public string Username { get; set; } = string.Empty;

    public string Mode { get; set; } = string.Empty;

    public string Unused { get; set; } = string.Empty;

    public string RealName { get; set; } = string.Empty;

    public void Parse(RawIrcMessage rawMessage)
    {
        Username = rawMessage.Params.Count > 0 ? rawMessage.Params[0] : string.Empty;
        Mode = rawMessage.Params.Count > 1 ? rawMessage.Params[1] : string.Empty;
        Unused = rawMessage.Params.Count > 2 ? rawMessage.Params[2] : string.Empty;
        RealName = rawMessage.Trailing ?? string.Empty;
    }

    public RawIrcMessage Produce()
    {
        var parameters = new List<string>();

        if (!string.IsNullOrWhiteSpace(Username))
        {
            parameters.Add(Username);
        }

        if (!string.IsNullOrWhiteSpace(Mode))
        {
            parameters.Add(Mode);
        }

        if (!string.IsNullOrWhiteSpace(Unused))
        {
            parameters.Add(Unused);
        }

        return CreateMessage(parameters, string.IsNullOrWhiteSpace(RealName) ? null : RealName);
    }
}
