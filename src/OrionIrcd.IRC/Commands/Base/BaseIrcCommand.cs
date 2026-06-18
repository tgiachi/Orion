using OrionIrcd.IRC.Interfaces;
using OrionIrcd.IRC.Message;

namespace OrionIrcd.IRC.Commands.Base;

/// <summary>
/// Abstract base class for IRC command implementations providing common parsing and validation logic.
/// This class implements <see cref="IIrcCommand" /> and provides helper methods for:
/// - Validating command codes against expected IRC command names
/// - Extracting source information (nick, user, host) from message prefixes
/// Derived classes should implement:
/// - <see cref="IIrcCommand.Code" /> property with the specific IRC command code
/// - <see cref="IIrcCommand.Parse(string)" /> to parse command-specific parameters
/// - <see cref="IIrcCommand.TryWrite(out string?)" /> to serialize back to IRC format
/// </summary>
/// <remarks>
/// IRC commands are case-insensitive per RFC 2812. This base class provides utilities
/// for common parsing patterns but allows derived classes to implement command-specific logic.
/// </remarks>
public abstract class BaseIrcCommand : IIrcCommand
{
    /// <summary>
    /// Gets the IRC command code (e.g., "PRIVMSG", "JOIN", "NICK", "MODE").
    /// This must be implemented by derived classes to return the appropriate command code.
    /// </summary>
    public abstract string Code { get; }

    public string? Raw { get; set; }

    public IrcMessagePrefix? Prefix { get; set; }

    public IReadOnlyDictionary<string, string?> Tags { get; set; }
        = new Dictionary<string, string?>(StringComparer.Ordinal);

    public IReadOnlyList<string> Params { get; set; } = [];

    public string? Trailing { get; set; }

    /// <summary>
    /// Parses a raw IRC message line into the command's typed properties.
    /// This method must be implemented by derived classes to handle command-specific parsing.
    /// </summary>
    /// <param name="line">Raw IRC message line (with or without source prefix)</param>
    /// <returns>
    /// A tuple with Success=true and Error=null on successful parse.
    /// On failure: Success=false and Error contains the error message.
    /// </returns>
    public abstract (bool Success, string? Error) Parse(string line);

    /// <summary>
    /// Converts this command back to its raw IRC string representation.
    /// This method must be implemented by derived classes to handle command-specific serialization.
    /// </summary>
    /// <param name="output">
    /// If successful, contains the formatted IRC message (e.g., "PRIVMSG #channel :hello").
    /// If parsing fails, this is null.
    /// </param>
    /// <returns>
    /// A tuple with Success=true and Error=null on successful serialization.
    /// On failure: Success=false and Error contains the error message.
    /// </returns>
    public abstract (bool Success, string? Error) TryWrite(out string? output);

    /// <summary>
    /// Extracts the source (nick!user@host or server name) from an IRC message prefix.
    /// </summary>
    /// <param name="prefix">The IRC message prefix (e.g., "nick!user@host" or "server.name")</param>
    /// <returns>
    /// A tuple containing:
    /// - Nick: The nickname (null if source is a server)
    /// - User: The username (null if source is a server)
    /// - Host: The hostname (null if source is a server)
    /// - Server: The server name (null if source is a user)
    /// All values are null if the prefix is null or invalid.
    /// </returns>
    /// <remarks>
    /// IRC prefix format can be either:
    /// - User format: nick!user@host (e.g., "alice!alice@example.com")
    /// - Server format: servername (e.g., "irc.example.com")
    /// This method parses both formats and returns the appropriate parts.
    /// Example:
    /// <code>
    /// var (nick, user, host, server) = ExtractSource("alice!alice@example.com");
    /// // nick="alice", user="alice", host="example.com", server=null
    ///
    /// var (n, u, h, s) = ExtractSource("irc.example.com");
    /// // n=null, u=null, h=null, s="irc.example.com"
    /// </code>
    /// </remarks>
    protected static (string? Nick, string? User, string? Host, string? Server) ExtractSource(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return (null, null, null, null);
        }

        // Check if it's a user prefix (contains '!' which indicates nick!user@host format)
        if (prefix.Contains('!'))
        {
            // User prefix format: nick!user@host
            var exclamationIndex = prefix.IndexOf('!');
            var atIndex = prefix.IndexOf('@', exclamationIndex);

            if (exclamationIndex > 0 && atIndex > exclamationIndex)
            {
                var nick = prefix[..exclamationIndex];
                var user = prefix[(exclamationIndex + 1)..atIndex];
                var host = prefix[(atIndex + 1)..];

                return (nick, user, host, null);
            }
        }

        // It's a server prefix (no '!' means it's a server name)
        return (null, null, null, prefix);
    }

    /// <summary>
    /// Validates that a given command code matches the expected IRC command.
    /// IRC commands are case-insensitive per specification.
    /// </summary>
    /// <param name="actualCode">The command code to validate (from parsed message)</param>
    /// <param name="expectedCode">The expected command code (typically the implementation's Code property)</param>
    /// <returns>true if the codes match (case-insensitive), false otherwise</returns>
    /// <remarks>
    /// Example:
    /// <code>
    /// var isPrivmsg = ValidateCommandCode("PRIVMSG", "PRIVMSG"); // true
    /// var isPrivmsg2 = ValidateCommandCode("privmsg", "PRIVMSG"); // true (case-insensitive)
    /// var isJoin = ValidateCommandCode("JOIN", "PRIVMSG");        // false
    /// </code>
    /// </remarks>
    protected static bool ValidateCommandCode(string? actualCode, string expectedCode)
    {
        if (string.IsNullOrWhiteSpace(actualCode))
        {
            return false;
        }

        return string.Equals(actualCode, expectedCode, StringComparison.OrdinalIgnoreCase);
    }
}
