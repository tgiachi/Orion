using OrionIrcd.IRC.Message;

namespace OrionIrcd.IRC.Interfaces;

/// <summary>
/// Creates concrete IRC command instances from parsed raw IRC messages.
/// </summary>
public interface IIrcCommandFactory
{
    /// <summary>
    /// Creates the best matching command for the provided raw IRC message,
    /// falling back when no concrete command is registered.
    /// </summary>
    /// <param name="rawMessage">Parsed raw IRC message to transform.</param>
    /// <returns>A concrete command instance or a fallback command.</returns>
    IIrcCommand CreateOrFallback(RawIrcMessage rawMessage);
}
