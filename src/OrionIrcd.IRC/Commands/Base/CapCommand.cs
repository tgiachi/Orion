using OrionIrcd.IRC.Interfaces;
using OrionIrcd.IRC.Message;

namespace OrionIrcd.IRC.Commands.Base;

public sealed class CapCommand : BaseIrcCommand, IIrcCommandParser, IIrcCommandProducer
{
    private const string CommandCode = "CAP";

    public override string Code => CommandCode;

    public string Subcommand { get; set; } = string.Empty;

    public IReadOnlyList<string> Capabilities { get; set; } = [];

    public void Parse(RawIrcMessage rawMessage)
    {
        Subcommand = rawMessage.Params.Count > 0 ? rawMessage.Params[0] : string.Empty;
        Capabilities = rawMessage.Params.Skip(1).ToArray();
    }

    public RawIrcMessage Produce()
    {
        var parameters = new List<string>();

        if (!string.IsNullOrWhiteSpace(Subcommand))
        {
            parameters.Add(Subcommand);
        }

        parameters.AddRange(Capabilities);

        return CreateMessage(parameters);
    }
}
