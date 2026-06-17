namespace OrionIrcd.Core.Interfaces.Events;

/// <summary>
/// Handles an event asynchronously.
/// </summary>
/// <typeparam name="TEvent">The event type.</typeparam>
public interface IAsyncEventListener<in TEvent> where TEvent : IEvent
{
    /// <summary>
    /// Handles the event.
    /// </summary>
    /// <param name="eventData">The event payload.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the listener finishes handling the event.</returns>
    Task HandleAsync(TEvent eventData, CancellationToken cancellationToken);
}
