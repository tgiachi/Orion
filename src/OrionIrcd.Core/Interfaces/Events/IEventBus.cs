using OrionIrcd.Core.Interfaces.Services;

namespace OrionIrcd.Core.Interfaces.Events;

/// <summary>
/// Dispatches synchronous and asynchronous events to registered listeners.
/// </summary>
public interface IEventBus : IOrionIrcdService
{
    /// <summary>
    /// Dispatches an event to synchronous listeners without waiting for listener completion.
    /// </summary>
    /// <param name="eventData">The event payload.</param>
    /// <typeparam name="TEvent">The event type.</typeparam>
    void Publish<TEvent>(TEvent eventData) where TEvent : IEvent;

    /// <summary>
    /// Dispatches an event to asynchronous listeners and waits until every listener has completed.
    /// </summary>
    /// <param name="eventData">The event payload.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <returns>A task that completes after all asynchronous listeners finish.</returns>
    Task PublishAsync<TEvent>(TEvent eventData, CancellationToken cancellationToken) where TEvent : IEvent;
}
