namespace OrionIrcd.Core.Interfaces.Events;

/// <summary>
/// Handles an event synchronously.
/// </summary>
/// <typeparam name="TEvent">The event type.</typeparam>
public interface ISyncEventListener<in TEvent> where TEvent : IEvent
{
    /// <summary>
    /// Handles the event.
    /// </summary>
    /// <param name="eventData">The event payload.</param>
    void Handle(TEvent eventData);
}
