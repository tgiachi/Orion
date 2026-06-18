using OrionIrcd.IRC.Message;

namespace OrionIrcd.IRC.Interfaces;

/// <summary>
/// Stores explicit IRC command registrations and optional type-specific binders.
/// </summary>
public interface IIrcCommandRegistry
{
    /// <summary>
    /// Registers a command type and an optional binder for structured raw-message binding.
    /// </summary>
    /// <typeparam name="TCommand">Concrete IRC command type.</typeparam>
    /// <param name="binder">Optional type-specific binder.</param>
    void RegisterCommand<TCommand>(Action<TCommand, RawIrcMessage>? binder = null)
        where TCommand : IIrcCommand, new();

    /// <summary>
    /// Attempts to create a command instance from a registered IRC command code.
    /// </summary>
    /// <param name="code">IRC command code.</param>
    /// <param name="command">Created command when found.</param>
    /// <returns><see langword="true" /> when a registered command exists.</returns>
    bool TryCreate(string code, out IIrcCommand? command);

    /// <summary>
    /// Attempts to get a previously registered type-specific binder.
    /// </summary>
    /// <param name="commandType">Concrete command type.</param>
    /// <param name="binder">Registered binder when found.</param>
    /// <returns><see langword="true" /> when a binder exists for the type.</returns>
    bool TryGetBinder(Type commandType, out Action<IIrcCommand, RawIrcMessage>? binder);
}
