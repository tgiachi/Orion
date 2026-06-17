namespace OrionIrcd.Core.Interfaces.Services;

public interface IOrionIrcdService
{
    Task StartAsync(CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);
}
