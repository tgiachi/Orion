using OrionIrcd.Server.Interfaces.IRC.Replies;

namespace OrionIrcd.Server.Data.IRC.Replies;

public sealed class IrcServerCommandReply : IIrcReply
{
    public string Command { get; }

    public IReadOnlyList<string> Parameters { get; }

    public string Trailing { get; }

    public IrcServerCommandReply(string command, string trailing, IReadOnlyList<string>? parameters = null)
    {
        Command = command;
        Parameters = parameters ?? [];
        Trailing = trailing;
    }

    public string Format(IrcReplyContext context)
    {
        var parts = new List<string>
        {
            $":{context.ServerName}",
            Command
        };

        parts.AddRange(Parameters.Where(parameter => !string.IsNullOrWhiteSpace(parameter)));
        parts.Add($":{Trailing}");

        return string.Join(' ', parts);
    }
}
