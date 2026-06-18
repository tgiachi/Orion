using OrionIrcd.IRC.Data;

namespace OrionIrcd.IRC.Interfaces;

/// <summary>
/// Writes a typed IRC command into its wire-format command line.
/// </summary>
/// <typeparam name="TCommand">Typed IRC command to serialize.</typeparam>
public interface IIrcCommandWriter<in TCommand>
    where TCommand : IIrcCommand
{
    /// <summary>
    /// Serializes the command to an IRC command line.
    /// </summary>
    /// <param name="command">Command instance to serialize.</param>
    /// <returns>Structured write result with either the line or an error.</returns>
    IrcCommandResult<string> Write(TCommand command);
}
