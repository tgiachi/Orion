using System.Text;
using OrionIrcd.IRC.Interfaces;
using OrionIrcd.IRC.Message;

namespace OrionIrcd.IRC.Services;

/// <summary>
/// High-throughput, zero-allocation IRC message parser designed for TCP streaming.
/// This parser is optimized for parsing incoming IRC messages from TCP streams where
/// messages may arrive incomplete or grouped together. It maintains internal state
/// across calls to handle incomplete messages, accumulating bytes until a complete
/// message (terminated by separator, typically CRLF) is available.
/// Key features:
/// - Handles IRCv3 tags with proper escaping/unescaping
/// - Parses message prefix (nick!user@host or server name)
/// - Extracts command and parameters
/// - Supports trailing parameter (content after ':')
/// - Maintains 64KB accumulation buffer for incomplete messages
/// - Zero-allocation design with Span&lt;T&gt; for performance
/// - Thread-safe per-instance (not across instances)
/// Typical usage:
/// <code>
/// var parser = new IrcMessageParser();
/// var messages = parser.ParseMessages(incomingData);
/// foreach (var msg in messages)
/// {
///     ProcessMessage(msg);
/// }
/// </code>
/// The parser maintains state between calls, so the same instance should be used
/// for parsing a continuous TCP stream to properly handle incomplete messages.
/// </summary>
public class IrcMessageParser : IIrcMessageParser
{
    private readonly ParserState _state = new();

    /// <summary>
    /// Parses IRC messages from raw byte data.
    /// This method handles incomplete messages by maintaining parser state across calls.
    /// It accumulates incoming bytes, searches for complete messages (delimited by separator),
    /// and returns only fully parsed messages. Incomplete data is retained for the next call.
    /// The parser is designed for streaming scenarios where TCP packets may contain:
    /// - Multiple complete messages
    /// - One complete message and partial next message
    /// - Only a partial message
    /// - Empty data
    /// All complete messages are parsed and returned, while incomplete data is buffered
    /// until the next call provides additional bytes.
    /// </summary>
    /// <param name="raw">Raw byte data from TCP stream. May be incomplete message(s).</param>
    /// <param name="separator">Message separator, typically "\r\n" for IRC. Defaults to "\r\n".</param>
    /// <returns>List of fully parsed IRC messages. May be empty if no complete messages were available.</returns>
    /// <exception cref="InvalidOperationException">Thrown if accumulated buffer exceeds 64KB capacity (message too large).</exception>
    public List<RawIrcMessage> ParseMessages(Span<byte> raw, string separator = "\r\n")
    {
        // Append new data to the accumulation buffer
        if (!_state.TryAppendData(raw))
        {
            throw new InvalidOperationException("Parser buffer overflow. Message too large.");
        }

        var messages = new List<RawIrcMessage>();
        var separatorBytes = Encoding.UTF8.GetBytes(separator);
        var accumulated = _state.AccumulatedData;

        var processed = 0;

        // Process complete messages
        while (true)
        {
            var remaining = accumulated[processed..];
            var separatorIndex = FindSeparator(remaining, separatorBytes);

            if (separatorIndex == -1)
            {
                // No complete message found
                break;
            }

            // Extract the message line
            var messageLine = remaining.Slice(0, separatorIndex);
            var messageText = Encoding.UTF8.GetString(messageLine);

            // Parse the message
            if (!string.IsNullOrWhiteSpace(messageText))
            {
                var parsed = ParseSingleMessage(messageText);

                if (parsed != null)
                {
                    messages.Add(parsed);
                }
            }

            // Move past the separator
            processed += separatorIndex + separatorBytes.Length;
        }

        // Compact the buffer, removing processed bytes
        _state.RemoveProcessedBytes(processed);

        return messages;
    }

    /// <summary>
    /// Searches for the message separator in a byte span.
    /// This is a low-level utility method that performs a byte-by-byte search for the separator
    /// sequence (typically CRLF, which is 2 bytes: 0x0D 0x0A). The search is case-sensitive and
    /// matches the entire separator sequence.
    /// Used internally by ParseMessages to identify complete messages in the accumulation buffer.
    /// </summary>
    /// <param name="span">Byte span to search within.</param>
    /// <param name="separator">Separator byte sequence to find (e.g., [0x0D, 0x0A] for CRLF).</param>
    /// <returns>Zero-based index of the first byte of the separator, or -1 if not found.</returns>
    private static int FindSeparator(Span<byte> span, byte[] separator)
    {
        if (span.Length < separator.Length)
        {
            return -1;
        }

        for (var i = 0; i <= span.Length - separator.Length; i++)
        {
            var match = true;

            for (var j = 0; j < separator.Length; j++)
            {
                if (span[i + j] != separator[j])
                {
                    match = false;

                    break;
                }
            }

            if (match)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Parses space-separated parameters and optional trailing content.
    /// Parameters are extracted from the remaining message content after command extraction.
    /// The method processes parameters until either:
    /// 1. A trailing marker (:) is found - all remaining content becomes the trailing parameter
    /// 2. The string is exhausted - the last part becomes a parameter
    /// Trailing content (prefixed with :) can contain spaces and is stored separately.
    /// This is used for message content like PRIVMSG where the actual text may contain spaces.
    /// Example: "param1 param2 :This is trailing content with spaces"
    /// Results in parameters=[param1, param2] and trailing="This is trailing content with spaces"
    /// </summary>
    /// <param name="remaining">The remaining message content after command extraction.</param>
    /// <param name="parameters">Output list where extracted parameters are added.</param>
    /// <param name="trailing">Output parameter containing trailing content if present, null otherwise.</param>
    private static void ParseParameters(string remaining, List<string> parameters, out string? trailing)
    {
        trailing = null;

        while (remaining.Length > 0)
        {
            // Check for trailing (starts with colon)
            if (remaining[0] == ':')
            {
                trailing = remaining.Substring(1);

                break;
            }

            // Find next space
            var spaceIndex = remaining.IndexOf(' ');

            if (spaceIndex == -1)
            {
                // Last parameter without trailing
                parameters.Add(remaining);

                break;
            }

            // Add parameter
            parameters.Add(remaining[..spaceIndex]);
            remaining = remaining[(spaceIndex + 1)..].TrimStart();
        }
    }

    /// <summary>
    /// Parses the message prefix into its components.
    /// A message prefix identifies the source of a message. There are two formats:
    /// 1. User prefix (from a client): nick!user@host
    /// - nick: user's nickname
    /// - user: username (optional on some networks)
    /// - host: hostname or IP address
    /// Example: "alice!alice@192.168.1.1"
    /// 2. Server prefix (from a server): servername
    /// - Just a server name, no ! or @ characters
    /// Example: "irc.example.com"
    /// The method distinguishes between the two by checking for ! and @ characters.
    /// </summary>
    /// <param name="prefixString">The prefix string without the leading ':' character.</param>
    /// <returns>IrcMessagePrefix object with appropriate fields populated. Nick may be empty for malformed input.</returns>
    private static IrcMessagePrefix? ParsePrefix(string prefixString)
    {
        // Check if it's a server name (no ! or @)
        if (!prefixString.Contains('!') && !prefixString.Contains('@'))
        {
            return new() { ServerName = prefixString };
        }

        // Parse user prefix (nick!user@host)
        var parts = prefixString.Split('!');
        var nick = parts[0];

        var userHost = parts.Length > 1 ? parts[1] : "";
        var userHostParts = userHost.Split('@');
        var user = userHostParts.Length > 0 ? userHostParts[0] : "";
        var host = userHostParts.Length > 1 ? userHostParts[1] : "";

        return new()
        {
            Nick = nick,
            User = user,
            Host = host
        };
    }

    /// <summary>
    /// Parses a single complete IRC message line into structured components.
    /// A complete IRC message line may contain:
    /// 1. IRCv3 tags (prefixed with @): @tag1=val1;tag2=val2
    /// 2. Prefix (prefixed with :): :nick!user@host or :servername
    /// 3. Command: PRIVMSG, JOIN, PART, etc.
    /// 4. Parameters: space-separated arguments
    /// 5. Trailing: content after ':' in parameters, can contain spaces
    /// Example: @time=2024-01-01T00:00:00.000Z :nick!user@host PRIVMSG #channel :Hello world
    /// </summary>
    /// <param name="line">A complete message line as a string (separator already removed).</param>
    /// <returns>Parsed IrcMessage object, or null if the line is empty/whitespace or lacks a command.</returns>
    private RawIrcMessage? ParseSingleMessage(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        var parts = line;
        var tags = new Dictionary<string, string?>(StringComparer.Ordinal);
        var prefix = (IrcMessagePrefix?)null;
        var command = "";
        var parameters = new List<string>();
        var trailing = (string?)null;

        // Parse tags (if present)
        if (parts.Length > 0 && parts[0] == '@')
        {
            var spaceIndex = parts.IndexOf(' ');

            if (spaceIndex == -1)
            {
                return null;
            }

            if (spaceIndex != -1)
            {
                var tagsString = parts.Substring(1, spaceIndex - 1);
                tags = ParseTags(tagsString);
                parts = parts[(spaceIndex + 1)..];
            }
        }

        // Parse prefix (if present)
        if (parts.Length > 0 && parts[0] == ':')
        {
            var spaceIndex = parts.IndexOf(' ');

            if (spaceIndex == -1)
            {
                return null;
            }

            if (spaceIndex != -1)
            {
                var prefixString = parts.Substring(1, spaceIndex - 1);
                prefix = ParsePrefix(prefixString);
                parts = parts[(spaceIndex + 1)..];
            }
        }

        // Parse command and parameters
        if (parts.Length > 0)
        {
            // Find the command (first word)
            var spaceIndex = parts.IndexOf(' ');

            if (spaceIndex == -1)
            {
                // Only command, no parameters
                command = parts;
            }
            else
            {
                command = parts[..spaceIndex];
                parts = parts[(spaceIndex + 1)..].TrimStart();

                // Parse parameters and trailing
                ParseParameters(parts, parameters, out trailing);
            }
        }

        if (string.IsNullOrEmpty(command))
        {
            return null;
        }

        return new()
        {
            Prefix = prefix,
            Tags = tags,
            Command = command,
            Params = parameters,
            Trailing = trailing,
            Raw = line
        };
    }

    /// <summary>
    /// Parses IRCv3 tags from a semicolon-separated tag string.
    /// IRCv3 tags are optional metadata attached to messages, prefixed with @ in the message.
    /// Format: @key1=value1;key2=value2;key3 (values are optional)
    /// Tag values may contain escaped characters that are unescaped during parsing:
    /// - \: becomes ;
    /// - \s becomes space
    /// - \\ becomes \
    /// - \r becomes CR
    /// - \n becomes LF
    /// Example: @time=2024-01-01T00:00:00.000Z;client-only;custom-tag=hello\sworld
    /// </summary>
    /// <param name="tagsString">The tag string without the leading '@' character.</param>
    /// <returns>Dictionary mapping tag keys to values. Values may be null for valueless tags.</returns>
    private Dictionary<string, string?> ParseTags(string tagsString)
    {
        var tags = new Dictionary<string, string?>(StringComparer.Ordinal);
        var tagParts = tagsString.Split(';');

        foreach (var tag in tagParts)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                continue;
            }

            var keyValue = tag.Split('=', 2);
            var key = keyValue[0];
            var value = keyValue.Length > 1 ? UnescapeTagValue(keyValue[1]) : null;

            tags[key] = value;
        }

        return tags;
    }

    /// <summary>
    /// Unescapes IRCv3 tag values according to the IRC Message Tags specification.
    /// The IRCv3 specification requires specific escape sequences in tag values:
    /// - \: (backslash colon) becomes ; (semicolon)
    /// - \s (backslash s) becomes space
    /// - \\ (double backslash) becomes \ (single backslash)
    /// - \r (backslash r) becomes CR (carriage return, 0x0D)
    /// - \n (backslash n) becomes LF (line feed, 0x0A)
    /// Unknown escape sequences (backslash followed by unrecognized character)
    /// are preserved as-is (backslash is kept).
    /// Example: "hello\\sworld\\:test" becomes "hello world;test"
    /// </summary>
    /// <param name="value">The escaped tag value, or null/empty string.</param>
    /// <returns>The unescaped value, preserving null/empty inputs.</returns>
    private static string? UnescapeTagValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var result = new StringBuilder();

        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] == '\\' && i + 1 < value.Length)
            {
                switch (value[i + 1])
                {
                    case ':':
                        result.Append(';');
                        i++; // Skip the next character

                        break;
                    case 's':
                        result.Append(' ');
                        i++; // Skip the next character

                        break;
                    case '\\':
                        result.Append('\\');
                        i++; // Skip the next character

                        break;
                    case 'r':
                        result.Append('\r');
                        i++; // Skip the next character

                        break;
                    case 'n':
                        result.Append('\n');
                        i++; // Skip the next character

                        break;
                    default:
                        // Unknown escape sequence, keep the backslash
                        result.Append('\\');

                        break;
                }
            }
            else
            {
                result.Append(value[i]);
            }
        }

        return result.ToString();
    }
}
