using OrionIrcd.IRC.Message;

namespace OrionIrcd.IRC.Interfaces;

/// <summary>
/// Parses command-specific fields from a raw IRC message.
/// </summary>
public interface IIrcCommandParser
{
    /// <summary>
    /// Parses command-specific values from a raw IRC message.
    /// </summary>
    /// <param name="rawMessage">Raw IRC message parsed from the wire protocol.</param>
    void Parse(RawIrcMessage rawMessage);
}
