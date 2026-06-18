using DryIoc;
using OrionIrcd.Core.Container;
using OrionIrcd.Server.Data.Listeners;
using OrionIrcd.Server.Data.Sessions;
using OrionIrcd.Server.Extensions.Listeners;
using OrionIrcd.Server.Interfaces.Listeners;
using OrionIrcd.Server.Services.Listeners;
using OrionIrcd.Tests.Support.IRC;
using OrionIrcd.Tests.Support.Network;

namespace OrionIrcd.Tests.Server.Services.Listeners;

public class IrcCommandDispatcherServiceTests
{
    [Fact]
    public async Task DispatchAsync_MatchingCommand_DispatchesListenersInParallel()
    {
        using var container = new Container();
        using var startedListeners = new CountdownEvent(2);
        var releaseListeners = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        FirstParallelTestIrcCommandListener.Contexts.Clear();
        FirstParallelTestIrcCommandListener.StartedListeners = startedListeners;
        FirstParallelTestIrcCommandListener.ReleaseListeners = releaseListeners;
        SecondParallelTestIrcCommandListener.Contexts.Clear();
        SecondParallelTestIrcCommandListener.StartedListeners = startedListeners;
        SecondParallelTestIrcCommandListener.ReleaseListeners = releaseListeners;
        container.RegisterIrcCommandList<TestIrcCommand, FirstParallelTestIrcCommandListener>();
        container.RegisterIrcCommandList<TestIrcCommand, SecondParallelTestIrcCommandListener>();
        var service = new IrcCommandDispatcherService(
            container.Resolve<List<IrcCommandDispatchRegistration>>(),
            container
        );
        var session = CreateSession();
        var command = new TestIrcCommand();

        var dispatchTask = service.DispatchAsync(session, command, CancellationToken.None);
        var listenersStarted = await Task.Run(() => startedListeners.Wait(TimeSpan.FromSeconds(1)));

        Assert.True(listenersStarted);
        Assert.False(dispatchTask.IsCompleted);

        releaseListeners.SetResult();
        await dispatchTask.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.Same(session, Assert.Single(FirstParallelTestIrcCommandListener.Contexts).Session);
        Assert.Same(command, Assert.Single(FirstParallelTestIrcCommandListener.Contexts).Command);
        Assert.Same(session, Assert.Single(SecondParallelTestIrcCommandListener.Contexts).Session);
        Assert.Same(command, Assert.Single(SecondParallelTestIrcCommandListener.Contexts).Command);
    }

    [Fact]
    public async Task DispatchAsync_ListenerThrows_DispatchesRemainingListeners()
    {
        using var container = new Container();
        using var completedListeners = new CountdownEvent(1);
        SuccessfulTestIrcCommandListener.CompletedListeners = completedListeners;
        container.RegisterIrcCommandList<TestIrcCommand, ThrowingTestIrcCommandListener>();
        container.RegisterIrcCommandList<TestIrcCommand, SuccessfulTestIrcCommandListener>();
        var service = new IrcCommandDispatcherService(
            container.Resolve<List<IrcCommandDispatchRegistration>>(),
            container
        );

        await service.DispatchAsync(CreateSession(), new TestIrcCommand(), CancellationToken.None);

        Assert.True(completedListeners.Wait(TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void RegisterIrcCommandList_CommandAndListener_RegistersListenerAndDispatchRegistration()
    {
        using var container = new Container();

        container.RegisterIrcCommandList<TestIrcCommand, RecordingTestIrcCommandListener>();

        var registration = Assert.Single(container.Resolve<List<IrcCommandDispatchRegistration>>());
        var listener = Assert.Single(
            container.ResolveMany<IIrcCommandListener<TestIrcCommand>>(behavior: ResolveManyBehavior.AsFixedArray)
        );

        Assert.Equal(typeof(TestIrcCommand), registration.CommandType);
        Assert.Equal(typeof(RecordingTestIrcCommandListener), registration.ListenerType);
        Assert.IsType<RecordingTestIrcCommandListener>(listener);
    }

    [Fact]
    public async Task RegisterIrcCommandList_TwoListenersForSameCommand_DispatchesEachListenerOnce()
    {
        using var container = new Container();
        FirstRecordingTestIrcCommandListener.HandleCount = 0;
        SecondRecordingTestIrcCommandListener.HandleCount = 0;

        container.RegisterIrcCommandList<TestIrcCommand, FirstRecordingTestIrcCommandListener>();
        container.RegisterIrcCommandList<TestIrcCommand, SecondRecordingTestIrcCommandListener>();

        var service = new IrcCommandDispatcherService(
            container.Resolve<List<IrcCommandDispatchRegistration>>(),
            container
        );

        await service.DispatchAsync(CreateSession(), new TestIrcCommand(), CancellationToken.None);

        Assert.Equal(1, FirstRecordingTestIrcCommandListener.HandleCount);
        Assert.Equal(1, SecondRecordingTestIrcCommandListener.HandleCount);
    }

    private static NetworkSession CreateSession()
    {
        var connection = new TestNetworkConnection { SessionId = 10 };

        return new NetworkSession(
            connection.SessionId,
            connection,
            connection.RemoteEndPoint,
            DateTimeOffset.UnixEpoch
        );
    }

    private sealed class FirstParallelTestIrcCommandListener : IIrcCommandListener<TestIrcCommand>
    {
        public static List<IrcCommandListenerContext<TestIrcCommand>> Contexts { get; } = [];

        public static TaskCompletionSource ReleaseListeners { get; set; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public static CountdownEvent StartedListeners { get; set; } = new(1);

        public async ValueTask HandleCommandAsync(
            IrcCommandListenerContext<TestIrcCommand> context,
            CancellationToken cancellationToken = default
        )
        {
            Contexts.Add(context);
            StartedListeners.Signal();

            await ReleaseListeners.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed class SecondParallelTestIrcCommandListener : IIrcCommandListener<TestIrcCommand>
    {
        public static List<IrcCommandListenerContext<TestIrcCommand>> Contexts { get; } = [];

        public static TaskCompletionSource ReleaseListeners { get; set; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public static CountdownEvent StartedListeners { get; set; } = new(1);

        public async ValueTask HandleCommandAsync(
            IrcCommandListenerContext<TestIrcCommand> context,
            CancellationToken cancellationToken = default
        )
        {
            Contexts.Add(context);
            StartedListeners.Signal();

            await ReleaseListeners.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed class RecordingTestIrcCommandListener : IIrcCommandListener<TestIrcCommand>
    {
        public ValueTask HandleCommandAsync(
            IrcCommandListenerContext<TestIrcCommand> context,
            CancellationToken cancellationToken = default
        )
            => ValueTask.CompletedTask;
    }

    private sealed class SuccessfulTestIrcCommandListener : IIrcCommandListener<TestIrcCommand>
    {
        public static CountdownEvent CompletedListeners { get; set; } = new(1);

        public ValueTask HandleCommandAsync(
            IrcCommandListenerContext<TestIrcCommand> context,
            CancellationToken cancellationToken = default
        )
        {
            CompletedListeners.Signal();

            return ValueTask.CompletedTask;
        }
    }

    private sealed class ThrowingTestIrcCommandListener : IIrcCommandListener<TestIrcCommand>
    {
        public ValueTask HandleCommandAsync(
            IrcCommandListenerContext<TestIrcCommand> context,
            CancellationToken cancellationToken = default
        )
            => throw new InvalidOperationException("listener failed");
    }

    private sealed class FirstRecordingTestIrcCommandListener : IIrcCommandListener<TestIrcCommand>
    {
        public static int HandleCount { get; set; }

        public ValueTask HandleCommandAsync(
            IrcCommandListenerContext<TestIrcCommand> context,
            CancellationToken cancellationToken = default
        )
        {
            HandleCount++;

            return ValueTask.CompletedTask;
        }
    }

    private sealed class SecondRecordingTestIrcCommandListener : IIrcCommandListener<TestIrcCommand>
    {
        public static int HandleCount { get; set; }

        public ValueTask HandleCommandAsync(
            IrcCommandListenerContext<TestIrcCommand> context,
            CancellationToken cancellationToken = default
        )
        {
            HandleCount++;

            return ValueTask.CompletedTask;
        }
    }
}
