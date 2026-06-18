using OrionIrcd.IRC.Message;

namespace OrionIrcd.IRC.Interfaces;

/// <summary>
/// Parses raw IRC message bytes into typed RawIrcMessage objects.
/// Handles TCP streaming with incomplete message buffering across multiple calls.
/// </summary>
public interface IIrcMessageParser
{
    /// <summary>
    /// Parses IRC messages from a byte span, handling incomplete messages across buffer boundaries.
    /// </summary>
    /// <param name="raw">Raw bytes containing one or more IRC messages separated by the separator</param>
    /// <param name="separator">Message separator (default: "\r\n" per IRC spec)</param>
    /// <returns>List of complete parsed messages. Incomplete messages are buffered internally.</returns>
    /// <remarks>
    /// The parser maintains internal state across calls for TCP streaming scenarios.
    /// Only complete messages are returned. Incomplete messages are buffered and combined
    /// with subsequent input automatically.
    /// </remarks>
    List<RawIrcMessage> ParseMessages(Span<byte> raw, string separator = "\r\n");
}
