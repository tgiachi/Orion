namespace OrionIrcd.IRC.Interfaces;

/// <summary>
/// Defines the base contract for typed IRC commands.
/// Each command type (PRIVMSG, JOIN, NICK, etc.) implements this interface
/// to expose its IRC command code.
/// </summary>
public interface IIrcCommand
{
    /// <summary>
    /// Gets the IRC command code (e.g., "PRIVMSG", "JOIN", "NICK", "MODE").
    /// This is case-insensitive per IRC specification.
    /// </summary>
    string Code { get; }
}
