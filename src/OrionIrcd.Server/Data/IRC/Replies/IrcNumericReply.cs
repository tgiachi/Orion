using OrionIrcd.Server.Interfaces.IRC.Replies;

namespace OrionIrcd.Server.Data.IRC.Replies;

public sealed class IrcNumericReply : IIrcReply
{
    public string Code { get; }

    public IReadOnlyList<string> Parameters { get; }

    public string Target { get; }

    public string Trailing { get; }

    public IrcNumericReply(
        string code,
        string target,
        string trailing,
        IReadOnlyList<string>? parameters = null
    )
    {
        Code = code;
        Target = target;
        Trailing = trailing;
        Parameters = parameters ?? [];
    }

    public string Format(IrcReplyContext context)
    {
        var parts = new List<string>
        {
            $":{context.ServerName}",
            Code,
            Target
        };

        parts.AddRange(Parameters.Where(parameter => !string.IsNullOrWhiteSpace(parameter)));
        parts.Add($":{Trailing}");

        return string.Join(' ', parts);
    }
}
