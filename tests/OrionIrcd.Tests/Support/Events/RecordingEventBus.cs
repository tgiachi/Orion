using System.Collections.Concurrent;
using OrionIrcd.Core.Interfaces.Events;

namespace OrionIrcd.Tests.Support.Events;

public sealed class RecordingEventBus : IEventBus
{
    private readonly ConcurrentQueue<IEvent> _events = new();
    private readonly Lock _waitersSync = new();
    private readonly List<Action<IEvent>> _waiters = [];

    public IReadOnlyList<IEvent> Events => _events.ToArray();

    public void Publish<TEvent>(TEvent eventData) where TEvent : IEvent
    {
        _events.Enqueue(eventData);

        Action<IEvent>[] waiters;

        lock (_waitersSync)
        {
            waiters = [.. _waiters];
        }

        foreach (var waiter in waiters)
        {
            waiter(eventData);
        }
    }

    public Task PublishAsync<TEvent>(TEvent eventData, CancellationToken cancellationToken) where TEvent : IEvent
    {
        cancellationToken.ThrowIfCancellationRequested();
        Publish(eventData);

        return Task.CompletedTask;
    }

    public async Task<TEvent> WaitForEventAsync<TEvent>(TimeSpan timeout) where TEvent : IEvent
    {
        foreach (var eventData in _events)
        {
            if (eventData is TEvent matchedEvent)
            {
                return matchedEvent;
            }
        }

        var completionSource = new TaskCompletionSource<TEvent>(TaskCreationOptions.RunContinuationsAsynchronously);

        void Waiter(IEvent eventData)
        {
            if (eventData is TEvent matchedEvent)
            {
                completionSource.TrySetResult(matchedEvent);
            }
        }

        lock (_waitersSync)
        {
            _waiters.Add(Waiter);
        }

        try
        {
            return await completionSource.Task.WaitAsync(timeout).ConfigureAwait(false);
        }
        finally
        {
            lock (_waitersSync)
            {
                _waiters.Remove(Waiter);
            }
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}
