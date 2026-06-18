namespace OrionIrcd.IRC.Interfaces;

/// <summary>
/// Defines the contract for IRC commands with Result-based error handling.
/// Each command type (PRIVMSG, JOIN, NICK, etc.) implements this interface
/// to provide typed parsing and serialization of IRC protocol messages.
/// </summary>
public interface IIrcCommand
{
    /// <summary>
    /// Gets the IRC command code (e.g., "PRIVMSG", "JOIN", "NICK", "MODE").
    /// This is case-insensitive per IRC specification.
    /// </summary>
    string Code { get; }

    /// <summary>
    /// Parses a raw IRC message line into this command's typed properties.
    /// </summary>
    /// <param name="line">Raw IRC message line (with or without source prefix)</param>
    /// <returns>
    /// A tuple with Success=true and Error=null on successful parse.
    /// On failure: Success=false and Error contains the error message.
    /// </returns>
    /// <remarks>
    /// This method should NOT throw exceptions for expected parsing failures.
    /// Use the (bool, string?) result to communicate errors to the caller.
    /// </remarks>
    (bool Success, string? Error) Parse(string line);

    /// <summary>
    /// Converts this command back to its raw IRC string representation.
    /// </summary>
    /// <param name="output">
    /// If successful, contains the formatted IRC message (e.g., "PRIVMSG #channel :hello").
    /// If parsing fails, this is null.
    /// </param>
    /// <returns>
    /// A tuple with Success=true and Error=null on successful serialization.
    /// On failure: Success=false and Error contains the error message.
    /// </returns>
    /// <remarks>
    /// Example outputs:
    /// - PRIVMSG #channel :message
    /// - :nick!user@host PRIVMSG #channel :message
    /// - JOIN #channel1,#channel2 key1,key2
    /// </remarks>
    (bool Success, string? Error) TryWrite(out string? output);
}
