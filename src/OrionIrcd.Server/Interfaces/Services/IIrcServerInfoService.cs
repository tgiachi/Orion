namespace OrionIrcd.Server.Interfaces.Services;

/// <summary>
/// Provides server identity values used by IRC protocol responses.
/// </summary>
public interface IIrcServerInfoService
{
    /// <summary>
    /// Gets the IRC server name used in reply prefixes and server-originated commands.
    /// </summary>
    string ServerName { get; }
}
