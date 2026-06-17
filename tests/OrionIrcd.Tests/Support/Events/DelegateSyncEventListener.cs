using OrionIrcd.Core.Interfaces.Events;

namespace OrionIrcd.Tests.Support.Events;

public sealed class DelegateSyncEventListener<TEvent> : ISyncEventListener<TEvent> where TEvent : IEvent
{
    private readonly Action<TEvent> _handler;

    public DelegateSyncEventListener(Action<TEvent> handler)
    {
        _handler = handler;
    }

    public void Handle(TEvent eventData)
        => _handler(eventData);
}
