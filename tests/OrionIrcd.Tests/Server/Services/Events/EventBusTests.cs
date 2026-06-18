using DryIoc;
using OrionIrcd.Core.Interfaces.Events;
using OrionIrcd.Server.Services.Events;
using OrionIrcd.Tests.Support.Events;

namespace OrionIrcd.Tests.Server.Services.Events;

public class EventBusTests
{
    [Fact]
    public async Task Publish_SlowSyncListener_ReturnsBeforeListenerCompletes()
    {
        using var container = new Container();
        var listenerStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var listenerCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseListener = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var eventBus = new EventBus(container);

        container.RegisterInstance<ISyncEventListener<EventBusTestEvent>>(
            new DelegateSyncEventListener<EventBusTestEvent>(
                _ =>
                {
                    listenerStarted.SetResult();
                    releaseListener.Task.GetAwaiter().GetResult();
                    listenerCompleted.SetResult();
                }
            )
        );

        await Task.Run(() => eventBus.Publish(new EventBusTestEvent())).WaitAsync(TimeSpan.FromSeconds(1));
        await listenerStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.False(listenerCompleted.Task.IsCompleted);

        releaseListener.SetResult();
        await listenerCompleted.Task.WaitAsync(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Publish_SyncListenerThrows_DispatchesRemainingListeners()
    {
        using var container = new Container();
        using var completedListeners = new CountdownEvent(2);
        var eventBus = new EventBus(container);

        container.RegisterInstance<ISyncEventListener<EventBusTestEvent>>(
            new DelegateSyncEventListener<EventBusTestEvent>(_ => completedListeners.Signal()),
            serviceKey: "first"
        );
        container.RegisterInstance<ISyncEventListener<EventBusTestEvent>>(
            new DelegateSyncEventListener<EventBusTestEvent>(_ => throw new InvalidOperationException("listener failed")),
            serviceKey: "throwing"
        );
        container.RegisterInstance<ISyncEventListener<EventBusTestEvent>>(
            new DelegateSyncEventListener<EventBusTestEvent>(_ => completedListeners.Signal()),
            serviceKey: "second"
        );

        eventBus.Publish(new EventBusTestEvent());

        Assert.True(completedListeners.Wait(TimeSpan.FromSeconds(2)));
    }

    [Fact]
    public async Task PublishAsync_AsyncListenerThrows_DispatchesRemainingListeners()
    {
        using var container = new Container();
        using var completedListeners = new CountdownEvent(2);
        var eventBus = new EventBus(container);

        container.RegisterInstance<IAsyncEventListener<EventBusTestEvent>>(
            new DelegateAsyncEventListener<EventBusTestEvent>(
                async (_, cancellationToken) =>
                {
                    await Task.Yield();
                    completedListeners.Signal();
                }
            ),
            serviceKey: "first"
        );
        container.RegisterInstance<IAsyncEventListener<EventBusTestEvent>>(
            new DelegateAsyncEventListener<EventBusTestEvent>(
                async (_, cancellationToken) =>
                {
                    await Task.Yield();

                    throw new InvalidOperationException("listener failed");
                }
            ),
            serviceKey: "throwing"
        );
        container.RegisterInstance<IAsyncEventListener<EventBusTestEvent>>(
            new DelegateAsyncEventListener<EventBusTestEvent>(
                async (_, cancellationToken) =>
                {
                    await Task.Yield();
                    completedListeners.Signal();
                }
            ),
            serviceKey: "second"
        );

        await eventBus.PublishAsync(new EventBusTestEvent(), CancellationToken.None);

        Assert.True(completedListeners.Wait(TimeSpan.FromSeconds(2)));
    }

    [Fact]
    public async Task PublishAsync_MultipleAsyncListeners_RunsListenersInParallel()
    {
        using var container = new Container();
        using var startedListeners = new CountdownEvent(2);
        var releaseListeners = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var eventBus = new EventBus(container);

        container.RegisterInstance<IAsyncEventListener<EventBusTestEvent>>(
            new DelegateAsyncEventListener<EventBusTestEvent>(
                async (_, cancellationToken) =>
                {
                    startedListeners.Signal();
                    await releaseListeners.Task.WaitAsync(cancellationToken);
                }
            ),
            serviceKey: "first"
        );
        container.RegisterInstance<IAsyncEventListener<EventBusTestEvent>>(
            new DelegateAsyncEventListener<EventBusTestEvent>(
                async (_, cancellationToken) =>
                {
                    startedListeners.Signal();
                    await releaseListeners.Task.WaitAsync(cancellationToken);
                }
            ),
            serviceKey: "second"
        );

        var publishTask = eventBus.PublishAsync(new EventBusTestEvent(), CancellationToken.None);
        var startedInParallel = await Task.Run(() => startedListeners.Wait(TimeSpan.FromSeconds(1)));

        Assert.True(startedInParallel);
        Assert.False(publishTask.IsCompleted);

        releaseListeners.SetResult();
        await publishTask.WaitAsync(TimeSpan.FromSeconds(1));
    }
}
