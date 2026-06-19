using OrionIrcd.Server.Core.Data.Sessions;

namespace OrionIrcd.Server.Interfaces.Services;

/// <summary>
/// Completes IRC registration and sends the post-registration welcome sequence.
/// </summary>
public interface IIrcRegistrationService
{
    /// <summary>
    /// Completes registration when the session has satisfied NICK, USER, and PASS requirements.
    /// </summary>
    /// <param name="session">Network session.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True when registration completed during this call.</returns>
    Task<bool> TryCompleteRegistrationAsync(NetworkSession session, CancellationToken cancellationToken);
}
