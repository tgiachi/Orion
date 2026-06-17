using OrionIrcd.Core.Interfaces.Services;

namespace OrionIrcd.Tests.Support.Services;

public sealed class RecordingOrionIrcdService<TMarker> : IOrionIrcdService
{
    private readonly List<string> _calls;
    private readonly string _name;

    public RecordingOrionIrcdService(List<string> calls, string name)
    {
        _calls = calls;
        _name = name;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _calls.Add("start:" + _name);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _calls.Add("stop:" + _name);

        return Task.CompletedTask;
    }
}
