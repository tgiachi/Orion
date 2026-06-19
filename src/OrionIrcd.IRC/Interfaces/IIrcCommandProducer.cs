using OrionIrcd.IRC.Message;

namespace OrionIrcd.IRC.Interfaces;

/// <summary>
/// Produces a raw IRC message from command-specific state.
/// </summary>
public interface IIrcCommandProducer
{
    /// <summary>
    /// Produces a raw IRC message from the current command state.
    /// </summary>
    /// <returns>Raw IRC message representation of this command.</returns>
    RawIrcMessage Produce();
}
