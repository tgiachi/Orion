using OrionIrcd.Server.Data.IRC.Replies;

namespace OrionIrcd.Server.Interfaces.IRC.Replies;

/// <summary>
/// Formats one IRC reply line without the trailing CRLF.
/// </summary>
public interface IIrcReply
{
    /// <summary>
    /// Formats the reply using server-side reply context.
    /// </summary>
    /// <param name="context">Reply formatting context.</param>
    /// <returns>IRC line without trailing CRLF.</returns>
    string Format(IrcReplyContext context);
}
