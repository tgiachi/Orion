using OrionIrcd.Core.Interfaces.Events;

namespace OrionIrcd.Tests.Support.Events;

public sealed class DelegateAsyncEventListener<TEvent> : IAsyncEventListener<TEvent> where TEvent : IEvent
{
    private readonly Func<TEvent, CancellationToken, Task> _handler;

    public DelegateAsyncEventListener(Func<TEvent, CancellationToken, Task> handler)
    {
        _handler = handler;
    }

    public Task HandleAsync(TEvent eventData, CancellationToken cancellationToken)
        => _handler(eventData, cancellationToken);
}
