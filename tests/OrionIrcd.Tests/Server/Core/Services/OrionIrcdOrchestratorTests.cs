using DryIoc;
using OrionIrcd.Core.Data.Internal;
using OrionIrcd.Server.Core.Services;
using OrionIrcd.Tests.Support.Services;

namespace OrionIrcd.Tests.Server.Core.Services;

public class OrionIrcdOrchestratorTests
{
    [Fact]
    public async Task RunAsync_CanceledAfterStart_StartsByPriorityAndStopsInReversePriority()
    {
        using var container = new Container();
        using var cancellationTokenSource = new CancellationTokenSource();
        var calls = new List<string>();
        var firstService = new RecordingOrionIrcdService<int>(calls, "first");
        var secondService = new RecordingOrionIrcdService<string>(calls, "second");
        var registrations = new List<ServiceRegistrationObject>
        {
            new(typeof(RecordingOrionIrcdService<string>), typeof(RecordingOrionIrcdService<string>), 20),
            new(typeof(RecordingOrionIrcdService<int>), typeof(RecordingOrionIrcdService<int>), 10)
        };
        var orchestrator = new OrionIrcdOrchestrator(registrations, container);

        container.RegisterInstance(firstService);
        container.RegisterInstance(secondService);

        var runTask = orchestrator.RunAsync(cancellationTokenSource.Token);

        try
        {
            await WaitUntilAsync(() => calls.Count >= 2);

            await cancellationTokenSource.CancelAsync();
            await runTask.WaitAsync(TimeSpan.FromSeconds(1));

            Assert.Equal(["start:first", "start:second", "stop:second", "stop:first"], calls);
        }
        finally
        {
            await cancellationTokenSource.CancelAsync();

            try
            {
                await runTask.WaitAsync(TimeSpan.FromSeconds(1));
            }
            catch (OperationCanceledException) { }
        }
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(50);
        }

        throw new TimeoutException("The condition was not met before the timeout expired.");
    }
}
