using DryIoc;
using OrionIrcd.Core.Interfaces.Events;
using Serilog;

namespace OrionIrcd.Server.Services.Events;

public sealed class EventBus : IEventBus
{
    private readonly IContainer _container;
    private readonly ILogger _logger = Log.ForContext<EventBus>();

    public EventBus(IContainer container)
    {
        _container = container;
    }

    public void Publish<TEvent>(TEvent eventData) where TEvent : IEvent
    {
        ArgumentNullException.ThrowIfNull(eventData);

        var listeners = ResolveListeners<ISyncEventListener<TEvent>>();

        foreach (var listener in listeners)
        {
            _ = Task.Run(() => DispatchSync(listener, eventData));
        }
    }

    public async Task PublishAsync<TEvent>(TEvent eventData, CancellationToken cancellationToken) where TEvent : IEvent
    {
        ArgumentNullException.ThrowIfNull(eventData);
        cancellationToken.ThrowIfCancellationRequested();

        var listeners = ResolveListeners<IAsyncEventListener<TEvent>>();
        var tasks = listeners.Select(
            listener => Task.Run(() => DispatchAsync(listener, eventData, cancellationToken), cancellationToken)
        );

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    public Task StartAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    private IReadOnlyList<TListener> ResolveListeners<TListener>()
        => _container.ResolveMany<TListener>(behavior: ResolveManyBehavior.AsFixedArray).ToArray();

    private void DispatchSync<TEvent>(ISyncEventListener<TEvent> listener, TEvent eventData) where TEvent : IEvent
    {
        try
        {
            listener.Handle(eventData);
        }
        catch (Exception exception)
        {
            LogListenerException(exception, listener, eventData);
        }
    }

    private async Task DispatchAsync<TEvent>(
        IAsyncEventListener<TEvent> listener,
        TEvent eventData,
        CancellationToken cancellationToken
    ) where TEvent : IEvent
    {
        try
        {
            await listener.HandleAsync(eventData, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            LogListenerException(exception, listener, eventData);
        }
    }

    private void LogListenerException<TEvent>(Exception exception, object listener, TEvent eventData) where TEvent : IEvent
        => _logger.Error(
            exception,
            "Event listener {ListenerType} failed while handling event {EventType}",
            listener.GetType().FullName,
            eventData.GetType().FullName
        );
}
