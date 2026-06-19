namespace OrionIrcd.IRC.Interfaces;

/// <summary>
/// Stores explicit IRC command registrations.
/// </summary>
public interface IIrcCommandRegistry
{
    /// <summary>
    /// Registers a command type.
    /// </summary>
    /// <typeparam name="TCommand">Concrete IRC command type.</typeparam>
    void RegisterCommand<TCommand>()
        where TCommand : IIrcCommand, new();

    /// <summary>
    /// Attempts to create a command instance from a registered IRC command code.
    /// </summary>
    /// <param name="code">IRC command code.</param>
    /// <param name="command">Created command when found.</param>
    /// <returns><see langword="true" /> when a registered command exists.</returns>
    bool TryCreate(string code, out IIrcCommand? command);
}
